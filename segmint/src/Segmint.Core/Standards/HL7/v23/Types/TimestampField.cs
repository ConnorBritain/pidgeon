// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.


using Segmint.Core.HL7.Validation;
using Segmint.Core.HL7;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System;
namespace Segmint.Core.Standards.HL7.v23.Types;

/// <summary>
/// Represents an HL7 Timestamp (TS) field.
/// Format: YYYY[MM[DD[HHMM[SS[.SSSS]]]]][+/-ZZZZ]
/// </summary>
public class TimestampField : HL7Field
{
    private static readonly Regex TimestampPattern = new(
        @"^(?<year>\d{4})(?:(?<month>\d{2})(?:(?<day>\d{2})(?:(?<hour>\d{2})(?:(?<minute>\d{2})(?:(?<second>\d{2})(?<fraction>\.\d{1,4})?)?)?)?)?)?(?<timezone>[+-]\d{4})?$",
        RegexOptions.Compiled);

    /// <inheritdoc />
    public override string DataType => "TS";

    /// <inheritdoc />
    public override int? MaxLength => 26; // Full format with timezone and fraction

    private DateTime? _dateTimeValue;

    /// <summary>
    /// Gets the timezone offset portion of the timestamp (e.g., "+0500", "-0300").
    /// Returns null if no timezone is specified in the timestamp.
    /// </summary>
    public string? Timezone
    {
        get
        {
            if (string.IsNullOrEmpty(RawValue))
                return null;

            var match = TimestampPattern.Match(RawValue);
            if (!match.Success || !match.Groups["timezone"].Success)
                return null;

            return match.Groups["timezone"].Value;
        }
    }

    /// <summary>
    /// Gets or sets the value of this timestamp field.
    /// Returns DateTime if set with DateTime, otherwise returns string.
    /// </summary>
    public object Value
    {
        get 
        {
            if (_dateTimeValue.HasValue)
                return _dateTimeValue.Value;
            
            // Try to parse the raw value if we don't have a cached DateTime
            var parsed = ParseDateTime(RawValue);
            if (parsed.HasValue)
            {
                _dateTimeValue = parsed.Value;
                return parsed.Value;
            }
            
            return RawValue;
        }
        set 
        {
            if (value is DateTime dt)
            {
                _dateTimeValue = dt;
                SetValue(dt);
            }
            else if (value is string str)
            {
                _dateTimeValue = null;
                SetValue(str);
            }
            else if (value == null)
            {
                _dateTimeValue = null;
                SetValue("");
            }
            else
            {
                throw new ArgumentException("Value must be DateTime or string", nameof(value));
            }
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TimestampField"/> class.
    /// </summary>
    /// <param name="value">The initial timestamp value.</param>
    /// <param name="isRequired">Whether this field is required.</param>
    public TimestampField(string? value = null, bool isRequired = false)
        : base(value, isRequired)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TimestampField"/> class with a DateTime value.
    /// </summary>
    /// <param name="dateTime">The DateTime value to format as HL7 timestamp.</param>
    public TimestampField(DateTime dateTime)
        : base(FormatDateTime(dateTime, false, false), false)
    {
        _dateTimeValue = dateTime;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TimestampField"/> class with a DateTime value.
    /// </summary>
    /// <param name="dateTime">The DateTime value to format as HL7 timestamp.</param>
    /// <param name="isRequired">Whether this field is required.</param>
    public TimestampField(DateTime dateTime, bool isRequired)
        : base(FormatDateTime(dateTime, false, false), isRequired)
    {
        _dateTimeValue = dateTime;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TimestampField"/> class with a DateTime value.
    /// </summary>
    /// <param name="dateTime">The DateTime value to format as HL7 timestamp.</param>
    /// <param name="includeFraction">Whether to include fractional seconds.</param>
    /// <param name="includeTimezone">Whether to include timezone information.</param>
    /// <param name="isRequired">Whether this field is required.</param>
    public TimestampField(DateTime dateTime, bool includeFraction = false, bool includeTimezone = false, bool isRequired = false)
        : base(FormatDateTime(dateTime, includeFraction, includeTimezone), isRequired)
    {
        _dateTimeValue = dateTime;
    }

    /// <summary>
    /// Gets the DateTime value of this timestamp field.
    /// </summary>
    /// <returns>The parsed DateTime, or null if the value cannot be parsed.</returns>
    public DateTime? ToDateTime()
    {
        if (IsEmpty)
            return null;

        return ParseDateTime(RawValue);
    }

    /// <summary>
    /// Parses a timestamp string into a DateTime value.
    /// </summary>
    /// <param name="value">The timestamp string to parse.</param>
    /// <returns>The parsed DateTime, or null if the value cannot be parsed.</returns>
    private DateTime? ParseDateTime(string value)
    {
        if (string.IsNullOrEmpty(value))
            return null;

        var match = TimestampPattern.Match(value);
        if (!match.Success)
            return null;

        try
        {
            var year = int.Parse(match.Groups["year"].Value);
            var month = match.Groups["month"].Success ? int.Parse(match.Groups["month"].Value) : 1;
            var day = match.Groups["day"].Success ? int.Parse(match.Groups["day"].Value) : 1;
            var hour = match.Groups["hour"].Success ? int.Parse(match.Groups["hour"].Value) : 0;
            var minute = match.Groups["minute"].Success ? int.Parse(match.Groups["minute"].Value) : 0;
            var second = match.Groups["second"].Success ? int.Parse(match.Groups["second"].Value) : 0;
            
            var millisecond = 0;
            if (match.Groups["fraction"].Success)
            {
                var fractionStr = match.Groups["fraction"].Value[1..]; // Remove the dot
                fractionStr = fractionStr.PadRight(3, '0')[..3]; // Pad to 3 digits or truncate
                millisecond = int.Parse(fractionStr);
            }

            var dateTime = new DateTime(year, month, day, hour, minute, second, millisecond, DateTimeKind.Unspecified);

            // Handle timezone if present
            if (match.Groups["timezone"].Success)
            {
                var timezoneStr = match.Groups["timezone"].Value;
                var sign = timezoneStr[0] == '+' ? 1 : -1;
                var offsetHours = int.Parse(timezoneStr[1..3]);
                var offsetMinutes = int.Parse(timezoneStr[3..5]);
                var offset = new TimeSpan(sign * offsetHours, sign * offsetMinutes, 0);
                
                // Convert to UTC and then to local time
                var utcDateTime = dateTime - offset;
                return utcDateTime.ToLocalTime();
            }

            return dateTime;
        }
        catch (Exception ex)
        {
            // Debug: Log the exception for troubleshooting
            System.Diagnostics.Debug.WriteLine($"TimestampField parsing error for '{value}': {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Sets this field to the current date and time.
    /// </summary>
    /// <param name="includeFraction">Whether to include fractional seconds.</param>
    /// <param name="includeTimezone">Whether to include timezone information.</param>
    public void SetToNow(bool includeFraction = false, bool includeTimezone = false)
    {
        SetValue(FormatDateTime(DateTime.Now, includeFraction, includeTimezone));
    }

    /// <summary>
    /// Sets this field to the current UTC date and time.
    /// </summary>
    /// <param name="includeFraction">Whether to include fractional seconds.</param>
    /// <param name="includeTimezone">Whether to include timezone information.</param>
    public void SetToUtcNow(bool includeFraction = false, bool includeTimezone = true)
    {
        SetValue(FormatDateTime(DateTime.UtcNow, includeFraction, includeTimezone));
    }

    /// <summary>
    /// Sets this field to a DateTime value.
    /// </summary>
    /// <param name="dateTime">The DateTime value to set.</param>
    /// <param name="includeFraction">Whether to include fractional seconds.</param>
    /// <param name="includeTimezone">Whether to include timezone information.</param>
    public void SetValue(DateTime dateTime, bool includeFraction = false, bool includeTimezone = false)
    {
        _dateTimeValue = dateTime;
        SetValue(FormatDateTime(dateTime, includeFraction, includeTimezone));
    }

    /// <inheritdoc />
    protected override void ValidateValue(string value)
    {
        if (string.IsNullOrEmpty(value))
            return; // Empty values are handled by the base class

        if (!TimestampPattern.IsMatch(value))
        {
            throw new ArgumentException($"Invalid HL7 timestamp format: '{value}'. Expected format: YYYY[MM[DD[HHMM[SS[.SSSS]]]]][+/-ZZZZ]");
        }

        // Attempt to parse to verify the date is valid
        var dateTime = ParseDateTime(value);
        if (dateTime == null)
        {
            throw new ArgumentException($"Invalid date/time values in timestamp: '{value}'");
        }
    }

    /// <inheritdoc />
    public override HL7Field Clone()
    {
        return new TimestampField(RawValue, IsRequired);
    }

    /// <summary>
    /// Formats a DateTime as an HL7 timestamp string.
    /// </summary>
    /// <param name="dateTime">The DateTime to format.</param>
    /// <param name="includeFraction">Whether to include fractional seconds.</param>
    /// <param name="includeTimezone">Whether to include timezone information.</param>
    /// <returns>The HL7-formatted timestamp string.</returns>
    private static string FormatDateTime(DateTime dateTime, bool includeFraction, bool includeTimezone)
    {
        var format = "yyyyMMddHHmmss";
        
        if (includeFraction)
        {
            format += ".fff";
        }

        var result = dateTime.ToString(format, CultureInfo.InvariantCulture);

        if (includeTimezone)
        {
            var offset = dateTime.Kind == DateTimeKind.Utc ? 
                TimeSpan.Zero : 
                TimeZoneInfo.Local.GetUtcOffset(dateTime);
            
            var sign = offset >= TimeSpan.Zero ? "+" : "-";
            var absOffset = offset.Duration();
            result += $"{sign}{absOffset.Hours:D2}{absOffset.Minutes:D2}";
        }

        return result;
    }

    /// <summary>
    /// Returns a formatted display string for this timestamp.
    /// </summary>
    /// <returns>A human-readable representation of the timestamp.</returns>
    public string ToDisplayString()
    {
        if (IsEmpty)
            return string.Empty;
            
        var dateTime = ToDateTime();
        if (dateTime.HasValue)
        {
            return dateTime.Value.ToString("M/d/yyyy h:mm:ss tt");
        }
        
        return RawValue; // Fallback to raw value if parsing fails
    }

    /// <summary>
    /// Creates a new TimestampField with the current date and time.
    /// </summary>
    /// <returns>A new TimestampField set to the current date and time.</returns>
    public static TimestampField Now()
    {
        return new TimestampField(DateTime.Now);
    }

    /// <summary>
    /// Creates a new TimestampField with today's date (time set to midnight).
    /// </summary>
    /// <returns>A new TimestampField set to today's date.</returns>
    public static TimestampField Today()
    {
        return new TimestampField(DateTime.Today);
    }

    /// <summary>
    /// Creates a new TimestampField from a specific date.
    /// </summary>
    /// <param name="date">The date to create the timestamp from.</param>
    /// <returns>A new TimestampField set to the specified date.</returns>
    public static TimestampField FromDate(DateTime date)
    {
        return new TimestampField(date);
    }

    /// <summary>
    /// Implicitly converts a DateTime to a TimestampField.
    /// </summary>
    public static implicit operator TimestampField(DateTime dateTime)
    {
        return new TimestampField(dateTime);
    }

    /// <summary>
    /// Implicitly converts a TimestampField to a DateTime (may be null).
    /// </summary>
    public static implicit operator DateTime?(TimestampField field)
    {
        return field.ToDateTime();
    }

    /// <summary>
    /// Implicitly converts a string to a TimestampField.
    /// </summary>
    public static implicit operator TimestampField(string value)
    {
        return new TimestampField(value);
    }

    /// <summary>
    /// Implicitly converts a TimestampField to a string.
    /// </summary>
    public static implicit operator string(TimestampField field)
    {
        return field.RawValue;
    }
}
