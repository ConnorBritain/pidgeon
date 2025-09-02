// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Pidgeon.Core;

namespace Pidgeon.Core.Domain.Messaging.HL7v2.Common;

/// <summary>
/// Represents an HL7 String (ST) field - basic string data type.
/// Implements Result<T> pattern per sacred architectural principles.
/// </summary>
public class StringField : HL7Field<string?>
{
    /// <inheritdoc />
    public override string DataType => "ST";

    /// <summary>
    /// Initializes a new instance of the StringField class.
    /// </summary>
    public StringField() : base()
    {
    }

    /// <summary>
    /// Initializes a new instance of the StringField class with a value.
    /// </summary>
    /// <param name="value">The initial string value</param>
    public StringField(string? value) : base(value)
    {
    }

    /// <summary>
    /// Initializes a new instance of the StringField class with constraints.
    /// </summary>
    /// <param name="value">The initial string value</param>
    /// <param name="maxLength">Maximum allowed length</param>
    /// <param name="isRequired">Whether this field is required</param>
    public StringField(string? value, int? maxLength, bool isRequired = false) : base(value, maxLength, isRequired)
    {
    }

    /// <summary>
    /// Parses the raw HL7 string into a string value.
    /// Uses Result<T> pattern per sacred architectural principles.
    /// </summary>
    /// <param name="hl7Value">The HL7-formatted string</param>
    /// <returns>Result containing the parsed string or error</returns>
    protected override Result<string?> ParseFromHL7String(string hl7Value)
    {
        // String fields are essentially pass-through, but we validate length
        if (MaxLength.HasValue && hl7Value.Length > MaxLength.Value)
        {
            return Result<string?>.Failure(
                Error.Validation($"String field exceeds maximum length of {MaxLength.Value}", nameof(hl7Value)));
        }

        return Result<string?>.Success(hl7Value);
    }

    /// <summary>
    /// Formats the string value into an HL7 string.
    /// Pure function with no side effects.
    /// </summary>
    /// <param name="value">The string value to format</param>
    /// <returns>The HL7-formatted string</returns>
    protected override string FormatValue(string? value) => value ?? string.Empty;

    /// <summary>
    /// Creates a required StringField with the specified maximum length.
    /// </summary>
    /// <param name="maxLength">Maximum allowed length for the field</param>
    /// <returns>A new StringField instance marked as required</returns>
    public static StringField Required(int maxLength)
    {
        return new StringField(null, maxLength, isRequired: true);
    }

    /// <summary>
    /// Creates an optional StringField with the specified maximum length.
    /// </summary>
    /// <param name="maxLength">Maximum allowed length for the field</param>
    /// <returns>A new StringField instance marked as optional</returns>
    public static StringField Optional(int maxLength)
    {
        return new StringField(null, maxLength, isRequired: false);
    }

    /// <summary>
    /// Creates a required StringField with no maximum length constraint.
    /// </summary>
    /// <returns>A new StringField instance marked as required</returns>
    public static StringField Required()
    {
        return new StringField(null, null, isRequired: true);
    }

    /// <summary>
    /// Creates an optional StringField with no maximum length constraint.
    /// </summary>
    /// <returns>A new StringField instance marked as optional</returns>
    public static StringField Optional()
    {
        return new StringField(null, null, isRequired: false);
    }
}