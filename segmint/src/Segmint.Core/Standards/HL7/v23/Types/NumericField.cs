// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.


using Segmint.Core.HL7;
using System.Globalization;
using System;
namespace Segmint.Core.Standards.HL7.v23.Types;

/// <summary>
/// Represents an HL7 Numeric (NM) field.
/// Supports integer and decimal values.
/// </summary>
public class NumericField : HL7Field
{
    /// <inheritdoc />
    public override string DataType => "NM";

    /// <inheritdoc />
    public override int? MaxLength => 16;

    /// <summary>
    /// Initializes a new instance of the <see cref="NumericField"/> class.
    /// </summary>
    /// <param name="value">The initial numeric value.</param>
    /// <param name="isRequired">Whether this field is required.</param>
    public NumericField(string? value = null, bool isRequired = false)
        : base(value, isRequired)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NumericField"/> class with an integer value.
    /// </summary>
    /// <param name="value">The integer value.</param>
    /// <param name="isRequired">Whether this field is required.</param>
    public NumericField(int value, bool isRequired = false)
        : base(value.ToString(CultureInfo.InvariantCulture), isRequired)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NumericField"/> class with a decimal value.
    /// </summary>
    /// <param name="value">The decimal value.</param>
    /// <param name="isRequired">Whether this field is required.</param>
    public NumericField(decimal value, bool isRequired = false)
        : base(value.ToString(CultureInfo.InvariantCulture), isRequired)
    {
    }

    /// <summary>
    /// Gets the integer value of this numeric field.
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

    /// <summary>
    /// Gets the decimal value of this numeric field.
    /// </summary>
    /// <returns>The parsed decimal, or null if the value cannot be parsed.</returns>
    public decimal? ToDecimal()
    {
        if (IsEmpty)
            return null;

        return decimal.TryParse(RawValue, NumberStyles.Number, CultureInfo.InvariantCulture, out var result) 
            ? result 
            : null;
    }

    /// <summary>
    /// Gets the double value of this numeric field.
    /// </summary>
    /// <returns>The parsed double, or null if the value cannot be parsed.</returns>
    public double? ToDouble()
    {
        if (IsEmpty)
            return null;

        return double.TryParse(RawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var result) 
            ? result 
            : null;
    }


    /// <inheritdoc />
    protected override void ValidateValue(string value)
    {
        if (string.IsNullOrEmpty(value))
            return;

        // Must be a valid numeric value (integer or decimal)
        if (!decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out _))
        {
            throw new ArgumentException($"Invalid numeric value: '{value}'. Must be a valid integer or decimal number.");
        }
    }

    /// <inheritdoc />
    public override HL7Field Clone()
    {
        return new NumericField(RawValue, IsRequired);
    }

    /// <summary>
    /// Implicitly converts an integer to a NumericField.
    /// </summary>
    public static implicit operator NumericField(int value)
    {
        return new NumericField(value);
    }

    /// <summary>
    /// Implicitly converts a decimal to a NumericField.
    /// </summary>
    public static implicit operator NumericField(decimal value)
    {
        return new NumericField(value);
    }

    /// <summary>
    /// Implicitly converts a NumericField to an integer (may be null).
    /// </summary>
    public static implicit operator int?(NumericField field)
    {
        return field.ToInt();
    }

    /// <summary>
    /// Implicitly converts a NumericField to a decimal (may be null).
    /// </summary>
    public static implicit operator decimal?(NumericField field)
    {
        return field.ToDecimal();
    }

    /// <summary>
    /// Implicitly converts a string to a NumericField.
    /// </summary>
    public static implicit operator NumericField(string value)
    {
        return new NumericField(value);
    }

    /// <summary>
    /// Implicitly converts a NumericField to a string.
    /// </summary>
    public static implicit operator string(NumericField field)
    {
        return field.RawValue;
    }
}
