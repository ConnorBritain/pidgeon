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
/// Represents an HL7 Date (DT) field.
/// Format: YYYYMMDD
/// </summary>
public class DateField : HL7Field
{
    private static readonly Regex DatePattern = new(
        @"^(\d{4}|\d{6}|\d{8})$",
        RegexOptions.Compiled);

    /// <inheritdoc />
    public override string DataType => "DT";

    /// <inheritdoc />
    public override int? MaxLength => 8;

    private DateTime? _dateTimeValue;

    /// <summary>
    /// Gets or sets the value of this date field.
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
                SetValue(dt.ToString("yyyyMMdd", CultureInfo.InvariantCulture));
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
    /// Initializes a new instance of the <see cref="DateField"/> class.
    /// </summary>
    /// <param name="value">The initial date value.</param>
    /// <param name="isRequired">Whether this field is required.</param>
    public DateField(string? value = null, bool isRequired = false)
        : base(value, isRequired)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DateField"/> class with a DateTime value.
    /// </summary>
    /// <param name="dateTime">The DateTime value to format as HL7 date.</param>
    /// <param name="isRequired">Whether this field is required.</param>
    public DateField(DateTime dateTime, bool isRequired = false)
        : base(dateTime.ToString("yyyyMMdd", CultureInfo.InvariantCulture), isRequired)
    {
        _dateTimeValue = dateTime;
    }

    /// <summary>
    /// Gets the DateTime value of this date field.
    /// </summary>
    /// <returns>The parsed DateTime, or null if the value cannot be parsed.</returns>
    public DateTime? ToDateTime()
    {
        if (IsEmpty)
            return null;

        return ParseDateTime(RawValue);
    }

    /// <summary>
    /// Parses a date string into a DateTime value.
    /// </summary>
    /// <param name="value">The date string to parse.</param>
    /// <returns>The parsed DateTime, or null if the value cannot be parsed.</returns>
    private DateTime? ParseDateTime(string value)
    {
        if (string.IsNullOrEmpty(value))
            return null;

        var match = DatePattern.Match(value);
        if (!match.Success)
            return null;

        try
        {
            // Parse based on string length
            int year, month = 1, day = 1;
            
            if (value.Length == 4) // YYYY
            {
                year = int.Parse(value);
            }
            else if (value.Length == 6) // YYYYMM
            {
                year = int.Parse(value.Substring(0, 4));
                month = int.Parse(value.Substring(4, 2));
            }
            else if (value.Length == 8) // YYYYMMDD
            {
                year = int.Parse(value.Substring(0, 4));
                month = int.Parse(value.Substring(4, 2));
                day = int.Parse(value.Substring(6, 2));
            }
            else
            {
                return null;
            }

            // Create the DateTime - this will throw if invalid
            return new DateTime(year, month, day);
        }
        catch (Exception ex)
        {
            // Debug: Log the exception for troubleshooting
            System.Diagnostics.Debug.WriteLine($"DateField parsing error for '{value}': {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Sets this field to the current date.
    /// </summary>
    public void SetToToday()
    {
        SetValue(DateTime.Today.ToString("yyyyMMdd", CultureInfo.InvariantCulture));
    }

    /// <inheritdoc />
    protected override void ValidateValue(string value)
    {
        if (string.IsNullOrEmpty(value))
            return;

        if (!DatePattern.IsMatch(value))
        {
            throw new ArgumentException($"Invalid HL7 date format: '{value}'. Expected format: YYYYMMDD");
        }

        // Attempt to parse to verify the date is valid
        var dateTime = ParseDateTime(value);
        if (dateTime == null)
        {
            throw new ArgumentException($"Invalid date values in date field: '{value}'");
        }
    }

    /// <inheritdoc />
    public override HL7Field Clone()
    {
        return new DateField(RawValue, IsRequired);
    }

    /// <summary>
    /// Implicitly converts a DateTime to a DateField.
    /// </summary>
    public static implicit operator DateField(DateTime dateTime)
    {
        return new DateField(dateTime);
    }

    /// <summary>
    /// Implicitly converts a DateField to a DateTime (may be null).
    /// </summary>
    public static implicit operator DateTime?(DateField field)
    {
        return field.ToDateTime();
    }

    /// <summary>
    /// Implicitly converts a string to a DateField.
    /// </summary>
    public static implicit operator DateField(string value)
    {
        return new DateField(value);
    }

    /// <summary>
    /// Implicitly converts a DateField to a string.
    /// </summary>
    public static implicit operator string(DateField field)
    {
        return field.RawValue;
    }
}
