// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.


using Segmint.Core.Performance;
using Segmint.Core.Standards.HL7.v23.Types;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text.Json.Serialization;
using System.Text;
using System;
namespace Segmint.Core.HL7;

/// <summary>
/// Represents an HL7 segment containing multiple fields.
/// </summary>
public abstract class HL7Segment : IEnumerable<HL7Field>
{
    private readonly List<HL7Field> _fields = new();

    /// <summary>
    /// Gets the three-character segment identifier (e.g., "MSH", "PID", "RXE").
    /// </summary>
    public abstract string SegmentId { get; }

    /// <summary>
    /// Gets the number of fields in this segment.
    /// </summary>
    [JsonIgnore]
    public int FieldCount => _fields.Count;

    /// <summary>
    /// Gets or sets the field at the specified index (1-based indexing to match HL7 conventions).
    /// </summary>
    /// <param name="index">The 1-based field index.</param>
    /// <returns>The field at the specified index.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the index is out of range.</exception>
    public HL7Field this[int index]
    {
        get
        {
            if (index < 1 || index > _fields.Count)
                throw new ArgumentOutOfRangeException(nameof(index), $"Field index must be between 1 and {_fields.Count}.");
            
            return _fields[index - 1];
        }
        set
        {
            if (index < 1)
                throw new ArgumentOutOfRangeException(nameof(index), "Field index must be 1 or greater.");
            
            // Expand the list if necessary
            while (_fields.Count < index)
            {
                _fields.Add(CreateDefaultField(_fields.Count + 1));
            }
            
            _fields[index - 1] = value ?? throw new ArgumentNullException(nameof(value));
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HL7Segment"/> class.
    /// </summary>
    protected HL7Segment()
    {
        InitializeFields();
    }

    /// <summary>
    /// Initializes the segment fields with their default types and requirements.
    /// Derived classes should override this method to set up their specific field structure.
    /// </summary>
    protected abstract void InitializeFields();

    /// <summary>
    /// Creates a default field for the specified position.
    /// </summary>
    /// <param name="position">The 1-based field position.</param>
    /// <returns>A default field instance.</returns>
    protected virtual HL7Field CreateDefaultField(int position)
    {
        return new StringField();
    }

    /// <summary>
    /// Adds a field to this segment.
    /// </summary>
    /// <param name="field">The field to add.</param>
    protected void AddField(HL7Field field)
    {
        _fields.Add(field ?? throw new ArgumentNullException(nameof(field)));
    }

    /// <summary>
    /// Sets the value of a field at the specified position.
    /// </summary>
    /// <param name="position">The 1-based field position.</param>
    /// <param name="value">The value to set.</param>
    public void SetField(int position, string? value)
    {
        this[position].SetValue(value);
    }

    /// <summary>
    /// Gets the value of a field at the specified position.
    /// </summary>
    /// <param name="position">The 1-based field position.</param>
    /// <returns>The field value.</returns>
    public string GetField(int position)
    {
        return this[position].RawValue;
    }

    /// <summary>
    /// Validates all fields in this segment.
    /// </summary>
    /// <returns>A list of validation errors, empty if all fields are valid.</returns>
    public virtual List<string> Validate()
    {
        var errors = new List<string>();

        for (int i = 0; i < _fields.Count; i++)
        {
            var field = _fields[i];
            if (field.IsRequired && field.IsEmpty)
            {
                errors.Add($"{SegmentId}.{i + 1}: Required field is empty");
            }
        }

        return errors;
    }

    /// <summary>
    /// Converts this segment to its HL7 string representation.
    /// </summary>
    /// <returns>The HL7-formatted segment string.</returns>
    public virtual string ToHL7String()
    {
        return StringBuilderPool.Execute(sb =>
        {
            sb.Append(SegmentId);

            for (int i = 0; i < _fields.Count; i++)
            {
                sb.Append('|');
                sb.Append(_fields[i].ToHL7String());
            }
        });
    }

    /// <summary>
    /// Parses an HL7 segment string into this segment instance.
    /// </summary>
    /// <param name="hl7String">The HL7 segment string to parse.</param>
    /// <exception cref="ArgumentException">Thrown when the segment string is invalid.</exception>
    public virtual void FromHL7String(string hl7String)
    {
        if (string.IsNullOrEmpty(hl7String))
            throw new ArgumentException("HL7 string cannot be null or empty.", nameof(hl7String));

        var parts = ComponentCache.GetComponents(hl7String, '|');
        if (parts.Length == 0 || parts[0] != SegmentId)
            throw new ArgumentException($"Expected segment ID '{SegmentId}' but found '{parts.FirstOrDefault()}'.", nameof(hl7String));

        // Parse fields (skip the segment ID)
        for (int i = 1; i < parts.Length; i++)
        {
            SetField(i, parts[i]);
        }
    }

    /// <summary>
    /// Creates a copy of this segment.
    /// </summary>
    /// <returns>A new segment instance with the same field values.</returns>
    public abstract HL7Segment Clone();

    /// <summary>
    /// Returns an enumerator that iterates through the fields.
    /// </summary>
    public IEnumerator<HL7Field> GetEnumerator()
    {
        return _fields.GetEnumerator();
    }

    /// <summary>
    /// Returns an enumerator that iterates through the fields.
    /// </summary>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <summary>
    /// Returns a string representation of this segment.
    /// </summary>
    public override string ToString()
    {
        return ToHL7String();
    }
}
