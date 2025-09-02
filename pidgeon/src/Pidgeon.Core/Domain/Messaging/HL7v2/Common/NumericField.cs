// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System.Globalization;
using Pidgeon.Core;

namespace Pidgeon.Core.Domain.Messaging.HL7v2.Common;

/// <summary>
/// Represents an HL7 Numeric (NM) field - decimal number data type.
/// Implements Result<T> pattern per sacred architectural principles.
/// </summary>
public class NumericField : HL7Field<decimal?>
{
    /// <inheritdoc />
    public override string DataType => "NM";

    /// <summary>
    /// Initializes the NumericField with HL7 v2.3 standard max length.
    /// </summary>
    static NumericField()
    {
        // HL7 v2.3 standard for NM fields
    }

    /// <inheritdoc />
    public override int? MaxLength { get; protected set; } = 16;

    /// <summary>
    /// Initializes a new instance of the NumericField class.
    /// </summary>
    public NumericField() : base()
    {
    }

    /// <summary>
    /// Initializes a new instance of the NumericField class with a decimal value.
    /// </summary>
    /// <param name="value">The initial decimal value</param>
    public NumericField(decimal? value) : base(value)
    {
    }

    /// <summary>
    /// Initializes a new instance of the NumericField class with constraints.
    /// </summary>
    /// <param name="value">The initial decimal value</param>
    /// <param name="isRequired">Whether this field is required</param>
    public NumericField(decimal? value, bool isRequired) : base(value, isRequired)
    {
    }

    /// <summary>
    /// Parses the raw HL7 string into a decimal value.
    /// Uses Result<T> pattern per sacred architectural principles.
    /// Supports standard numeric formats including integers and decimals.
    /// </summary>
    /// <param name="hl7Value">The HL7-formatted numeric string</param>
    /// <returns>Result containing the parsed decimal or error</returns>
    protected override Result<decimal?> ParseFromHL7String(string hl7Value)
    {
        if (string.IsNullOrWhiteSpace(hl7Value))
            return Result<decimal?>.Success(null);

        if (decimal.TryParse(hl7Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var result))
        {
            return Result<decimal?>.Success(result);
        }

        return Result<decimal?>.Failure(
            Error.Validation($"Invalid numeric format: '{hl7Value}'. Expected a valid decimal number.", nameof(hl7Value)));
    }

    /// <summary>
    /// Formats the decimal value into an HL7 numeric string.
    /// Pure function with no side effects.
    /// Uses invariant culture to ensure consistent formatting.
    /// </summary>
    /// <param name="value">The decimal value to format</param>
    /// <returns>The HL7-formatted numeric string</returns>
    protected override string FormatValue(decimal? value)
    {
        return value?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
    }
}