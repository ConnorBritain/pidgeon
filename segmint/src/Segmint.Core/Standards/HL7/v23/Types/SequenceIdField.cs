// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.


using Segmint.Core.HL7;
using System.Globalization;
using System;
namespace Segmint.Core.Standards.HL7.v23.Types;

/// <summary>
/// Represents an HL7 Sequence ID (SI) field.
/// Non-negative integer for sequence numbering.
/// </summary>
public class SequenceIdField : HL7Field
{
    /// <inheritdoc />
    public override string DataType => "SI";

    /// <inheritdoc />
    public override int? MaxLength => 4;

    /// <summary>
    /// Gets or sets the value of this sequence ID field.
    /// </summary>
    public string Value
    {
        get => RawValue;
        set => SetValue(value);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SequenceIdField"/> class.
    /// </summary>
    /// <param name="value">The initial sequence ID value.</param>
    /// <param name="isRequired">Whether this field is required.</param>
    public SequenceIdField(string? value = null, bool isRequired = false)
        : base(value, isRequired)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SequenceIdField"/> class with an integer value.
    /// </summary>
    /// <param name="value">The sequence ID value.</param>
    /// <param name="isRequired">Whether this field is required.</param>
    public SequenceIdField(int value, bool isRequired = false)
        : base(value.ToString(CultureInfo.InvariantCulture), isRequired)
    {
        if (value < 0)
            throw new ArgumentException("Sequence ID must be non-negative.", nameof(value));
    }

    /// <summary>
    /// Gets the integer value of this sequence ID field.
    /// </summary>
    /// <returns>The parsed integer, or null if the value cannot be parsed.</returns>
    public int? ToInt()
    {
        if (IsEmpty)
            return null;

        return int.TryParse(RawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result) 
            ? result 
            : null;
    }

    /// <inheritdoc />
    protected override void ValidateValue(string value)
    {
        if (string.IsNullOrEmpty(value))
            return;

        // Must be a valid non-negative integer
        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
        {
            throw new ArgumentException($"Invalid sequence ID value: '{value}'. Must be a valid integer.");
        }

        if (result < 0)
        {
            throw new ArgumentException($"Sequence ID must be non-negative: '{value}'.");
        }
    }

    /// <inheritdoc />
    public override HL7Field Clone()
    {
        return new SequenceIdField(RawValue, IsRequired);
    }

    /// <summary>
    /// Implicitly converts an integer to a SequenceIdField.
    /// </summary>
    public static implicit operator SequenceIdField(int value)
    {
        return new SequenceIdField(value);
    }

    /// <summary>
    /// Implicitly converts a SequenceIdField to an integer (may be null).
    /// </summary>
    public static implicit operator int?(SequenceIdField field)
    {
        return field.ToInt();
    }

    /// <summary>
    /// Implicitly converts a string to a SequenceIdField.
    /// </summary>
    public static implicit operator SequenceIdField(string value)
    {
        return new SequenceIdField(value);
    }

    /// <summary>
    /// Implicitly converts a SequenceIdField to a string.
    /// </summary>
    public static implicit operator string(SequenceIdField field)
    {
        return field.RawValue;
    }
}
