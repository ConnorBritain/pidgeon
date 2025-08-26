// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Segmint.Core.Standards.HL7.v23.Fields;

namespace Segmint.Core.Standards.HL7.v23;

/// <summary>
/// Base class for all HL7 v2.3 segments.
/// Provides common functionality for field management, serialization, and validation.
/// </summary>
public abstract class HL7Segment
{
    /// <summary>
    /// Gets the segment ID (e.g., "MSH", "PID", "RXE").
    /// </summary>
    public abstract string SegmentId { get; }

    /// <summary>
    /// Gets the display name of the segment for human-readable output.
    /// </summary>
    public virtual string DisplayName => SegmentId;

    /// <summary>
    /// Gets or sets the sequence number for this segment instance.
    /// </summary>
    public int? SequenceNumber { get; set; }

    /// <summary>
    /// Collection of fields in this segment.
    /// </summary>
    protected readonly List<HL7Field> Fields = new();

    /// <summary>
    /// HL7 field separator character.
    /// </summary>
    public const char FieldSeparator = '|';

    /// <summary>
    /// HL7 component separator character.
    /// </summary>
    public const char ComponentSeparator = '^';

    /// <summary>
    /// HL7 repetition separator character.
    /// </summary>
    public const char RepetitionSeparator = '~';

    /// <summary>
    /// HL7 subcomponent separator character.
    /// </summary>
    public const char SubcomponentSeparator = '&';

    /// <summary>
    /// HL7 escape character.
    /// </summary>
    public const char EscapeCharacter = '\\';

    protected HL7Segment()
    {
        InitializeFields();
    }

    /// <summary>
    /// Initializes the fields for this segment type.
    /// Must be implemented by derived classes.
    /// </summary>
    protected abstract void InitializeFields();

    /// <summary>
    /// Gets the field at the specified index (1-based).
    /// </summary>
    /// <param name="index">1-based field index</param>
    /// <returns>The field at the index, or null if not found</returns>
    public HL7Field? GetField(int index)
    {
        if (index < 1 || index > Fields.Count)
            return null;

        return Fields[index - 1];
    }

    /// <summary>
    /// Gets the field at the specified index as a specific type.
    /// </summary>
    /// <typeparam name="T">The field type</typeparam>
    /// <param name="index">1-based field index</param>
    /// <returns>The field as the specified type, or null if not found or wrong type</returns>
    public T? GetField<T>(int index) where T : HL7Field
    {
        return GetField(index) as T;
    }

    /// <summary>
    /// Sets the field at the specified index (1-based).
    /// </summary>
    /// <param name="index">1-based field index</param>
    /// <param name="value">The value to set</param>
    /// <returns>A result indicating success or failure</returns>
    public Result<HL7Segment> SetField(int index, string? value)
    {
        var field = GetField(index);
        if (field == null)
            return Error.Validation($"Field {index} does not exist in segment {SegmentId}", $"Field{index}");

        var result = field.SetValue(value);
        if (result.IsFailure)
            return Error.Validation($"Failed to set field {index} in segment {SegmentId}: {result.Error.Message}", $"Field{index}");

        return Result<HL7Segment>.Success(this);
    }

    /// <summary>
    /// Sets the field at the specified index using a field object.
    /// </summary>
    /// <param name="index">1-based field index</param>
    /// <param name="field">The field to set</param>
    /// <returns>A result indicating success or failure</returns>
    public Result<HL7Segment> SetField(int index, HL7Field field)
    {
        if (index < 1 || index > Fields.Count)
            return Error.Validation($"Field index {index} is out of range for segment {SegmentId}", $"Field{index}");

        Fields[index - 1] = field;
        return Result<HL7Segment>.Success(this);
    }

    /// <summary>
    /// Adds a field to the segment.
    /// </summary>
    /// <param name="field">The field to add</param>
    protected void AddField(HL7Field field)
    {
        Fields.Add(field);
    }

    /// <summary>
    /// Validates the segment and all its fields.
    /// </summary>
    /// <returns>A result containing validation results</returns>
    public virtual Result<HL7Segment> Validate()
    {
        var errors = new List<string>();

        for (int i = 0; i < Fields.Count; i++)
        {
            var field = Fields[i];
            var validation = field.ValidateValue(field.Value);
            
            if (validation.IsFailure)
            {
                errors.Add($"Field {i + 1}: {validation.Error.Message}");
            }
        }

        if (errors.Any())
        {
            var combinedError = string.Join("; ", errors);
            return Error.Validation($"Segment {SegmentId} validation failed: {combinedError}", SegmentId);
        }

        return Result<HL7Segment>.Success(this);
    }

    /// <summary>
    /// Serializes the segment to its HL7 string representation.
    /// </summary>
    /// <returns>The HL7 string representation</returns>
    public virtual string ToHL7String()
    {
        var fieldStrings = new List<string> { SegmentId };
        
        foreach (var field in Fields)
        {
            fieldStrings.Add(field.ToHL7String());
        }

        return string.Join(FieldSeparator, fieldStrings);
    }

    /// <summary>
    /// Parses an HL7 segment string into this segment.
    /// </summary>
    /// <param name="hl7String">The HL7 segment string to parse</param>
    /// <returns>A result indicating success or failure</returns>
    public virtual Result<HL7Segment> ParseHL7String(string hl7String)
    {
        if (string.IsNullOrWhiteSpace(hl7String))
            return Error.Parsing("HL7 segment string cannot be empty", "HL7Segment");

        var parts = hl7String.Split(FieldSeparator);
        
        // First part should be the segment ID
        if (parts.Length == 0 || parts[0] != SegmentId)
            return Error.Parsing($"Expected segment ID {SegmentId}, got {parts.FirstOrDefault()}", "HL7Segment");

        // Parse fields (skip segment ID at index 0)
        for (int i = 1; i < parts.Length && i - 1 < Fields.Count; i++)
        {
            var field = Fields[i - 1];
            var parseResult = field.ParseHL7String(parts[i]);
            
            if (parseResult.IsFailure)
                return Error.Parsing($"Failed to parse field {i} in segment {SegmentId}: {parseResult.Error.Message}", "HL7Segment");
        }

        return Result<HL7Segment>.Success(this);
    }

    /// <summary>
    /// Gets a human-readable display of the segment.
    /// </summary>
    /// <returns>Display string</returns>
    public virtual string GetDisplayValue()
    {
        var nonEmptyFields = Fields.Where(f => f.HasValue).ToList();
        if (!nonEmptyFields.Any())
            return $"{SegmentId} (empty)";

        var fieldDisplays = nonEmptyFields.Select(f => f.GetDisplayValue()).Take(3);
        var display = string.Join(", ", fieldDisplays);
        
        if (nonEmptyFields.Count > 3)
            display += "...";

        return $"{SegmentId}: {display}";
    }

    /// <summary>
    /// Clears all field values in the segment.
    /// </summary>
    public virtual void Clear()
    {
        foreach (var field in Fields)
        {
            field.Clear();
        }
    }

    /// <summary>
    /// Gets the number of fields in the segment.
    /// </summary>
    public int FieldCount => Fields.Count;

    /// <summary>
    /// Gets whether the segment has any populated fields.
    /// </summary>
    public bool HasData => Fields.Any(f => f.HasValue);

    /// <summary>
    /// String representation of the segment.
    /// </summary>
    /// <returns>HL7 string representation</returns>
    public override string ToString()
    {
        return ToHL7String();
    }
}