// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Pidgeon.Core;

namespace Pidgeon.Core.Domain.Messaging.HL7v2.Common;

/// <summary>
/// Represents a single HL7 field with type-safe value handling and validation.
/// Base class for all HL7 field types in the infrastructure layer.
/// </summary>
public abstract class HL7Field
{
    /// <summary>
    /// Gets the raw string value of the field.
    /// </summary>
    public string RawValue { get; protected set; } = string.Empty;

    /// <summary>
    /// Gets a value indicating whether this field is required.
    /// </summary>
    public bool IsRequired { get; protected set; }

    /// <summary>
    /// Gets a value indicating whether this field is empty or null.
    /// </summary>
    public bool IsEmpty => string.IsNullOrWhiteSpace(RawValue);

    /// <summary>
    /// Gets the HL7 data type identifier for this field (e.g., "ST", "TS", "NM").
    /// </summary>
    public abstract string DataType { get; }

    /// <summary>
    /// Gets the maximum length allowed for this field type, if any.
    /// </summary>
    public virtual int? MaxLength { get; protected set; }

    /// <summary>
    /// Sets the value of this field from an HL7 string representation.
    /// </summary>
    /// <param name="value">The HL7-formatted string value</param>
    /// <returns>A result indicating success or validation errors</returns>
    public virtual Result<HL7Field> SetValue(string? value)
    {
        RawValue = value ?? string.Empty;
        return Result<HL7Field>.Success(this);
    }

    /// <summary>
    /// Gets the HL7-formatted string representation of this field.
    /// </summary>
    /// <returns>The HL7 string representation</returns>
    public virtual string ToHL7String() => RawValue;

    /// <summary>
    /// Validates this field according to HL7 specifications and field-specific rules.
    /// Uses Result<T> pattern per sacred architectural principles.
    /// </summary>
    /// <returns>A result indicating whether the field is valid</returns>
    public virtual Result<HL7Field> Validate()
    {
        if (IsRequired && IsEmpty)
            return Result<HL7Field>.Failure(Error.Validation($"Required {DataType} field cannot be empty", "RawValue"));

        if (MaxLength.HasValue && RawValue.Length > MaxLength.Value)
            return Result<HL7Field>.Failure(Error.Validation($"{DataType} field exceeds maximum length of {MaxLength}", "RawValue"));

        return Result<HL7Field>.Success(this);
    }

    /// <summary>
    /// Returns the display value for this field.
    /// </summary>
    /// <returns>A human-readable representation of the field value</returns>
    public virtual string GetDisplayValue() => RawValue;

    public override string ToString() => GetDisplayValue();
}

/// <summary>
/// Generic base class for typed HL7 fields.
/// Follows sacred Result<T> pattern for all validation and parsing operations.
/// Provides consolidated constructor patterns to eliminate duplication.
/// </summary>
/// <typeparam name="T">The .NET type that represents this field's value</typeparam>
public abstract class HL7Field<T> : HL7Field
{
    /// <summary>
    /// Gets the typed value of this field.
    /// </summary>
    public T? Value { get; protected set; }

    /// <summary>
    /// Gets the typed value of this field (alias for Value for backward compatibility).
    /// </summary>
    public T? TypedValue => Value;

    /// <summary>
    /// Initializes a new instance of the HL7Field with default settings.
    /// </summary>
    protected HL7Field()
    {
    }

    /// <summary>
    /// Initializes a new instance of the HL7Field with a value.
    /// </summary>
    /// <param name="value">The initial typed value</param>
    protected HL7Field(T? value)
    {
        SetTypedValue(value);
    }

    /// <summary>
    /// Initializes a new instance of the HL7Field with value and constraints.
    /// </summary>
    /// <param name="value">The initial typed value</param>
    /// <param name="isRequired">Whether this field is required</param>
    protected HL7Field(T? value, bool isRequired)
    {
        IsRequired = isRequired;
        SetTypedValue(value);
    }

    /// <summary>
    /// Initializes a new instance of the HL7Field with value and length constraints.
    /// </summary>
    /// <param name="value">The initial typed value</param>
    /// <param name="maxLength">Maximum allowed length</param>
    /// <param name="isRequired">Whether this field is required</param>
    protected HL7Field(T? value, int? maxLength, bool isRequired)
    {
        IsRequired = isRequired;
        MaxLength = maxLength;
        SetTypedValue(value);
    }

    /// <summary>
    /// Sets the typed value of this field and updates the raw HL7 representation.
    /// Uses Result<T> pattern per sacred architectural principles.
    /// </summary>
    /// <param name="value">The typed value to set</param>
    /// <returns>A result indicating success or validation errors</returns>
    public virtual Result<HL7Field> SetTypedValue(T? value)
    {
        Value = value;
        RawValue = FormatValue(value);
        return Result<HL7Field>.Success(this);
    }

    /// <summary>
    /// Parses the raw HL7 string into the typed value.
    /// Returns Result<T> to handle parsing failures without exceptions.
    /// </summary>
    /// <param name="hl7Value">The HL7-formatted string</param>
    /// <returns>Result containing the parsed typed value or error</returns>
    protected abstract Result<T?> ParseFromHL7String(string hl7Value);

    /// <summary>
    /// Formats the typed value into an HL7 string.
    /// Pure function with no side effects.
    /// </summary>
    /// <param name="value">The typed value to format</param>
    /// <returns>The HL7-formatted string</returns>
    protected abstract string FormatValue(T? value);

    /// <inheritdoc />
    /// <remarks>
    /// Uses Result<T> pattern throughout per sacred architectural principles.
    /// No exceptions thrown for business logic control flow.
    /// </remarks>
    public override Result<HL7Field> SetValue(string? value)
    {
        var baseResult = base.SetValue(value);
        if (baseResult.IsFailure)
            return baseResult;

        var parseResult = ParseFromHL7String(RawValue);
        if (parseResult.IsFailure)
            return Result<HL7Field>.Failure(parseResult.Error);

        Value = parseResult.Value;
        return Result<HL7Field>.Success(this);
    }
}