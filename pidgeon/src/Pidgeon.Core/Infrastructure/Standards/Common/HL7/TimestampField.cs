// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System.Globalization;
using Pidgeon.Core;

namespace Pidgeon.Core.Infrastructure.Standards.Common.HL7;

/// <summary>
/// Represents an HL7 Timestamp (TS) field - date/time data type.
/// Implements Result<T> pattern per sacred architectural principles.
/// </summary>
public class TimestampField : HL7Field<DateTime?>
{
    /// <inheritdoc />
    public override string DataType => "TS";

    /// <inheritdoc />
    public override int? MaxLength => 26; // YYYYMMDDHHMMSS.SSSS+ZZZZ

    /// <summary>
    /// Initializes a new instance of the TimestampField class.
    /// </summary>
    public TimestampField()
    {
    }

    /// <summary>
    /// Initializes a new instance of the TimestampField class with a DateTime value.
    /// </summary>
    /// <param name="value">The initial DateTime value</param>
    public TimestampField(DateTime? value)
    {
        SetTypedValue(value);
    }

    /// <summary>
    /// Initializes a new instance of the TimestampField class with constraints.
    /// </summary>
    /// <param name="value">The initial DateTime value</param>
    /// <param name="isRequired">Whether this field is required</param>
    public TimestampField(DateTime? value, bool isRequired)
    {
        IsRequired = isRequired;
        SetTypedValue(value);
    }

    /// <summary>
    /// Parses the raw HL7 string into a DateTime value.
    /// Uses Result<T> pattern per sacred architectural principles.
    /// Supports HL7 timestamp formats: YYYY, YYYYMM, YYYYMMDD, YYYYMMDDHHMM, YYYYMMDDHHMMSS, etc.
    /// </summary>
    /// <param name="hl7Value">The HL7-formatted timestamp string</param>
    /// <returns>Result containing the parsed DateTime or error</returns>
    protected override Result<DateTime?> ParseFromHL7String(string hl7Value)
    {
        if (string.IsNullOrWhiteSpace(hl7Value))
            return Result<DateTime?>.Success(null);

        // Remove any timezone info for now (simple implementation)
        var cleanValue = hl7Value.Split('+', '-')[0];

        // Try different HL7 timestamp formats
        var formats = new[]
        {
            "yyyyMMddHHmmss.ffff",
            "yyyyMMddHHmmss",
            "yyyyMMddHHmm",
            "yyyyMMddHH",
            "yyyyMMdd",
            "yyyyMM",
            "yyyy"
        };

        foreach (var format in formats)
        {
            if (DateTime.TryParseExact(cleanValue, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
            {
                return Result<DateTime?>.Success(result);
            }
        }

        return Result<DateTime?>.Failure(
            Error.Validation($"Invalid HL7 timestamp format: '{hl7Value}'. Expected formats: YYYY[MM[DD[HH[MM[SS[.SSSS]]]]]]", nameof(hl7Value)));
    }

    /// <summary>
    /// Formats the DateTime value into an HL7 timestamp string.
    /// Pure function with no side effects.
    /// Uses YYYYMMDDHHMMSS format (HL7 v2.3 standard).
    /// </summary>
    /// <param name="value">The DateTime value to format</param>
    /// <returns>The HL7-formatted timestamp string</returns>
    protected override string FormatValue(DateTime? value)
    {
        return value?.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture) ?? string.Empty;
    }
}