// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.


using Segmint.Core.HL7.Validation;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json.Serialization;
using System;
namespace Segmint.Core.HL7;

/// <summary>
/// Represents a single HL7 field with type-safe value handling and validation.
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
    [JsonIgnore]
    public bool IsEmpty => string.IsNullOrEmpty(RawValue);

    /// <summary>
    /// Gets the HL7 data type name for this field.
    /// </summary>
    public abstract string DataType { get; }

    /// <summary>
    /// Gets the maximum length allowed for this field type.
    /// </summary>
    public virtual int? MaxLength => null;

    /// <summary>
    /// Initializes a new instance of the <see cref="HL7Field"/> class.
    /// </summary>
    /// <param name="value">The initial value.</param>
    /// <param name="isRequired">Whether this field is required.</param>
    protected HL7Field(string? value = null, bool isRequired = false)
    {
        RawValue = value ?? string.Empty;
        IsRequired = isRequired;
    }

    /// <summary>
    /// Sets the value of this field.
    /// </summary>
    /// <param name="value">The new value.</param>
    /// <exception cref="ArgumentException">Thrown when the value is invalid for this field type.</exception>
    public virtual void SetValue(string? value)
    {
        var newValue = value ?? string.Empty;
        
        if (IsRequired && string.IsNullOrEmpty(newValue))
        {
            throw new ArgumentException($"Required field of type {DataType} cannot be empty.");
        }

        if (MaxLength.HasValue && newValue.Length > MaxLength.Value)
        {
            throw new ArgumentException($"Value exceeds maximum length of {MaxLength} for {DataType} field.");
        }

        ValidateValue(newValue);
        RawValue = newValue;
    }

    /// <summary>
    /// Validates the given value for this field type.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <exception cref="ArgumentException">Thrown when the value is invalid for this field type.</exception>
    protected virtual void ValidateValue(string value)
    {
        // Base implementation does no validation
        // Derived classes should override for type-specific validation
    }

    /// <summary>
    /// Gets the formatted HL7 representation of this field.
    /// </summary>
    /// <returns>The HL7-formatted string.</returns>
    public virtual string ToHL7String()
    {
        return RawValue;
    }

    /// <summary>
    /// Creates a copy of this field.
    /// </summary>
    /// <returns>A new instance with the same value and properties.</returns>
    public abstract HL7Field Clone();

    /// <summary>
    /// Returns a string representation of this field.
    /// </summary>
    public override string ToString()
    {
        return $"{DataType}: {RawValue}";
    }

    /// <summary>
    /// Determines whether two field instances are equal.
    /// </summary>
    public override bool Equals(object? obj)
    {
        if (obj is not HL7Field other || other.GetType() != GetType())
            return false;

        return RawValue == other.RawValue && IsRequired == other.IsRequired;
    }

    /// <summary>
    /// Gets the hash code for this field.
    /// </summary>
    public override int GetHashCode()
    {
        return HashCode.Combine(GetType(), RawValue, IsRequired);
    }
}
