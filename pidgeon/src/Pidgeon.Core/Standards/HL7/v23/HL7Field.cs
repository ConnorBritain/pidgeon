// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

namespace Pidgeon.Core.Standards.HL7.v23;

/// <summary>
/// Base class for all HL7 v2.3 field types.
/// Provides common functionality for field serialization, validation, and manipulation.
/// </summary>
public abstract class HL7Field
{
    /// <summary>
    /// Gets the raw value of the field.
    /// </summary>
    public string? Value { get; protected set; }

    /// <summary>
    /// Gets whether the field has a value.
    /// </summary>
    public bool HasValue => !string.IsNullOrEmpty(Value);

    /// <summary>
    /// Gets whether the field is empty.
    /// </summary>
    public bool IsEmpty => string.IsNullOrEmpty(Value);

    /// <summary>
    /// Gets the maximum length allowed for this field type.
    /// </summary>
    public virtual int? MaxLength => null;

    /// <summary>
    /// Gets whether this field is required.
    /// </summary>
    public virtual bool IsRequired => false;

    /// <summary>
    /// Gets the field's display name for validation messages.
    /// </summary>
    public virtual string DisplayName => GetType().Name.Replace("Field", "");

    protected HL7Field()
    {
    }

    protected HL7Field(string? value)
    {
        Value = value;
    }

    /// <summary>
    /// Sets the field value with validation.
    /// </summary>
    /// <param name="value">The value to set</param>
    /// <returns>A result indicating success or validation errors</returns>
    public virtual Result<HL7Field> SetValue(string? value)
    {
        var validation = ValidateValue(value);
        if (validation.IsFailure)
            return Error.Validation(validation.Error.Message, validation.Error.Context);

        Value = value;
        return Result<HL7Field>.Success(this);
    }

    /// <summary>
    /// Validates a value for this field type.
    /// </summary>
    /// <param name="value">The value to validate</param>
    /// <returns>A result indicating whether the value is valid</returns>
    public virtual Result<string?> ValidateValue(string? value)
    {
        // Check required field
        if (IsRequired && string.IsNullOrEmpty(value))
            return Error.Validation($"{DisplayName} is required", DisplayName);

        // Check maximum length
        if (MaxLength.HasValue && value?.Length > MaxLength.Value)
            return Error.Validation($"{DisplayName} exceeds maximum length of {MaxLength.Value}", DisplayName);

        return Result<string?>.Success(value);
    }

    /// <summary>
    /// Serializes the field to its HL7 string representation.
    /// </summary>
    /// <returns>The HL7 string representation of the field</returns>
    public virtual string ToHL7String()
    {
        return EscapeHL7Characters(Value ?? string.Empty);
    }

    /// <summary>
    /// Parses an HL7 string into this field type.
    /// </summary>
    /// <param name="hl7Value">The HL7 string to parse</param>
    /// <returns>A result containing the parsed field or an error</returns>
    public virtual Result<HL7Field> ParseHL7String(string hl7Value)
    {
        var unescapedValue = UnescapeHL7Characters(hl7Value);
        return SetValue(unescapedValue);
    }

    /// <summary>
    /// Gets the display value for human-readable output.
    /// </summary>
    /// <returns>The display value</returns>
    public virtual string GetDisplayValue()
    {
        return Value ?? string.Empty;
    }

    /// <summary>
    /// Clears the field value.
    /// </summary>
    public virtual void Clear()
    {
        Value = null;
    }

    /// <summary>
    /// Escapes HL7 special characters in a value.
    /// </summary>
    /// <param name="value">The value to escape</param>
    /// <returns>The escaped value</returns>
    protected static string EscapeHL7Characters(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        return value
            .Replace("\\", "\\E\\")    // Escape character must be first
            .Replace("|", "\\F\\")     // Field separator
            .Replace("^", "\\S\\")     // Component separator
            .Replace("&", "\\T\\")     // Subcomponent separator
            .Replace("~", "\\R\\")     // Repetition separator
            .Replace("\r", "\\X0D\\")  // Carriage return
            .Replace("\n", "\\X0A\\"); // Line feed
    }

    /// <summary>
    /// Unescapes HL7 special characters in a value.
    /// </summary>
    /// <param name="value">The value to unescape</param>
    /// <returns>The unescaped value</returns>
    protected static string UnescapeHL7Characters(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        return value
            .Replace("\\F\\", "|")     // Field separator
            .Replace("\\S\\", "^")     // Component separator
            .Replace("\\T\\", "&")     // Subcomponent separator
            .Replace("\\R\\", "~")     // Repetition separator
            .Replace("\\X0D\\", "\r")  // Carriage return
            .Replace("\\X0A\\", "\n")  // Line feed
            .Replace("\\E\\", "\\");   // Escape character must be last
    }

    /// <summary>
    /// Implicit conversion to string.
    /// </summary>
    /// <param name="field">The field to convert</param>
    public static implicit operator string?(HL7Field? field)
    {
        return field?.Value;
    }

    /// <summary>
    /// String representation of the field.
    /// </summary>
    /// <returns>The field value or empty string</returns>
    public override string ToString()
    {
        return Value ?? string.Empty;
    }

    /// <summary>
    /// Equality comparison based on value.
    /// </summary>
    /// <param name="obj">Object to compare</param>
    /// <returns>True if equal, false otherwise</returns>
    public override bool Equals(object? obj)
    {
        if (obj is HL7Field other)
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        
        if (obj is string stringValue)
            return string.Equals(Value, stringValue, StringComparison.Ordinal);

        return false;
    }

    /// <summary>
    /// Gets hash code based on value.
    /// </summary>
    /// <returns>Hash code</returns>
    public override int GetHashCode()
    {
        return Value?.GetHashCode() ?? 0;
    }
}

/// <summary>
/// Generic base class for strongly-typed HL7 fields.
/// </summary>
/// <typeparam name="T">The underlying value type</typeparam>
public abstract class HL7Field<T> : HL7Field
{
    /// <summary>
    /// Gets the strongly-typed value.
    /// </summary>
    public T? TypedValue { get; protected set; }

    /// <summary>
    /// Gets whether the typed value has a value.
    /// </summary>
    public bool HasTypedValue => TypedValue != null;

    protected HL7Field()
    {
    }

    protected HL7Field(T? value)
    {
        SetTypedValue(value);
    }

    protected HL7Field(string? stringValue)
    {
        if (!string.IsNullOrEmpty(stringValue))
        {
            var parseResult = ParseStringValue(stringValue);
            if (parseResult.IsSuccess)
            {
                SetTypedValue(parseResult.Value);
            }
            else
            {
                Value = stringValue; // Keep original string if parsing fails
            }
        }
    }

    /// <summary>
    /// Sets the typed value and updates the string representation.
    /// </summary>
    /// <param name="value">The typed value to set</param>
    /// <returns>A result indicating success or failure</returns>
    public virtual Result<HL7Field<T>> SetTypedValue(T? value)
    {
        TypedValue = value;
        Value = FormatTypedValue(value);
        return Result<HL7Field<T>>.Success(this);
    }

    /// <summary>
    /// Parses a string value into the typed value.
    /// </summary>
    /// <param name="stringValue">The string to parse</param>
    /// <returns>A result containing the parsed value or an error</returns>
    protected abstract Result<T> ParseStringValue(string stringValue);

    /// <summary>
    /// Formats the typed value into its string representation.
    /// </summary>
    /// <param name="value">The typed value to format</param>
    /// <returns>The string representation</returns>
    protected abstract string? FormatTypedValue(T? value);

    /// <summary>
    /// Implicit conversion to typed value.
    /// </summary>
    /// <param name="field">The field to convert</param>
    public static implicit operator T?(HL7Field<T>? field)
    {
        return field != null ? field.TypedValue : default;
    }
}