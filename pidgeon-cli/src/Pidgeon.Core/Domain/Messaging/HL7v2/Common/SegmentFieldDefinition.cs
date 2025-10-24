// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Pidgeon.Core.Domain.Messaging.HL7v2.Common;

namespace Pidgeon.Core.Domain.Messaging.HL7v2.Common;

/// <summary>
/// Defines a field within an HL7 segment including its position and initialization.
/// </summary>
public class SegmentFieldDefinition
{
    /// <summary>
    /// Gets the field position within the segment (1-based).
    /// </summary>
    public int Position { get; }

    /// <summary>
    /// Gets the field name for documentation purposes.
    /// </summary>
    public string FieldName { get; }

    /// <summary>
    /// Gets the function that creates the field instance.
    /// </summary>
    public Func<HL7Field> CreateField { get; }

    /// <summary>
    /// Gets whether this field is required.
    /// </summary>
    public bool IsRequired { get; }

    /// <summary>
    /// Creates a new field definition.
    /// </summary>
    /// <param name="position">Field position (1-based)</param>
    /// <param name="fieldName">Field name for documentation</param>
    /// <param name="createField">Function to create the field</param>
    /// <param name="isRequired">Whether the field is required</param>
    public SegmentFieldDefinition(int position, string fieldName, Func<HL7Field> createField, bool isRequired = false)
    {
        Position = position;
        FieldName = fieldName;
        CreateField = createField ?? throw new ArgumentNullException(nameof(createField));
        IsRequired = isRequired;
    }

    /// <summary>
    /// Creates a required string field definition.
    /// </summary>
    public static SegmentFieldDefinition RequiredString(int position, string fieldName, int maxLength = 250) =>
        new(position, fieldName, () => StringField.Required(maxLength), true);

    /// <summary>
    /// Creates an optional string field definition.
    /// </summary>
    public static SegmentFieldDefinition OptionalString(int position, string fieldName, int maxLength = 250) =>
        new(position, fieldName, () => StringField.Optional(maxLength), false);

    /// <summary>
    /// Creates a string field definition with a default value.
    /// </summary>
    public static SegmentFieldDefinition StringWithDefault(int position, string fieldName, string defaultValue, int maxLength = 250, bool isRequired = true) =>
        new(position, fieldName, () => new StringField(defaultValue, maxLength, isRequired), isRequired);

    /// <summary>
    /// Creates a numeric field definition.
    /// </summary>
    public static SegmentFieldDefinition Numeric(int position, string fieldName, bool isRequired = false) =>
        new(position, fieldName, () => new NumericField(), isRequired);

    /// <summary>
    /// Creates a timestamp field definition.
    /// </summary>
    public static SegmentFieldDefinition Timestamp(int position, string fieldName, bool isRequired = false) =>
        new(position, fieldName, () => new TimestampField(), isRequired);

    /// <summary>
    /// Creates a timestamp field definition with current time as default.
    /// </summary>
    public static SegmentFieldDefinition TimestampNow(int position, string fieldName, bool isRequired = true) =>
        new(position, fieldName, () => new TimestampField(DateTime.UtcNow), isRequired);

    /// <summary>
    /// Creates a date field definition.
    /// </summary>
    public static SegmentFieldDefinition Date(int position, string fieldName, bool isRequired = false) =>
        new(position, fieldName, () => new DateField(), isRequired);
}