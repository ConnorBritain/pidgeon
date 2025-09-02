// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System.Globalization;
using Pidgeon.Core;

namespace Pidgeon.Core.Domain.Messaging.HL7v2.Common;

/// <summary>
/// Represents an HL7 Date (DT) field - date-only data type.
/// Implements Result<T> pattern per sacred architectural principles.
/// Format: YYYYMMDD (8 digits, no separators)
/// </summary>
public class DateField : HL7Field<DateTime?>
{
    /// <inheritdoc />
    public override string DataType => "DT";

    /// <inheritdoc />
    public override int? MaxLength { get; protected set; } = 8; // YYYYMMDD

    /// <summary>
    /// Initializes a new instance of the DateField class.
    /// </summary>
    public DateField() : base()
    {
    }

    /// <summary>
    /// Initializes a new instance of the DateField class with a DateTime value.
    /// Only the date portion is used; time is ignored.
    /// </summary>
    /// <param name="value">The date value</param>
    public DateField(DateTime? value) : base(value)
    {
    }

    /// <summary>
    /// Initializes a new instance of the DateField class with constraints.
    /// </summary>
    /// <param name="value">The initial DateTime value</param>
    /// <param name="isRequired">Whether this field is required</param>
    public DateField(DateTime? value, bool isRequired) : base(value, isRequired)
    {
    }

    /// <summary>
    /// Parses an HL7 date string into a DateTime value.
    /// </summary>
    /// <param name="hl7Value">The HL7 date string in YYYYMMDD format</param>
    /// <returns>A result containing the parsed DateTime or an error</returns>
    protected override Result<DateTime?> ParseFromHL7String(string hl7Value)
    {
        if (string.IsNullOrWhiteSpace(hl7Value))
            return Result<DateTime?>.Success(null);

        // Remove any whitespace
        hl7Value = hl7Value.Trim();

        // HL7 date format: YYYYMMDD (exactly 8 digits)
        if (hl7Value.Length != 8)
            return Result<DateTime?>.Failure($"Invalid HL7 date format: '{hl7Value}'. Expected YYYYMMDD (8 digits).");

        if (!hl7Value.All(char.IsDigit))
            return Result<DateTime?>.Failure($"Invalid HL7 date format: '{hl7Value}'. All characters must be digits.");

        // Parse using exact format to ensure correct interpretation
        if (DateTime.TryParseExact(hl7Value, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            return Result<DateTime?>.Success(date);
        }

        return Result<DateTime?>.Failure($"Could not parse HL7 date: '{hl7Value}'. Ensure date is valid (e.g., 20250828).");
    }

    /// <summary>
    /// Formats a DateTime value as an HL7 date string.
    /// </summary>
    /// <param name="value">The DateTime value to format</param>
    /// <returns>The HL7 date string in YYYYMMDD format</returns>
    protected override string FormatValue(DateTime? value)
    {
        return value?.ToString("yyyyMMdd", CultureInfo.InvariantCulture) ?? "";
    }

    /// <summary>
    /// Creates a DateField for today's date.
    /// </summary>
    /// <returns>A DateField containing today's date</returns>
    public static DateField Today() => new(DateTime.Today);

    /// <summary>
    /// Creates a DateField for a specific date.
    /// </summary>
    /// <param name="year">Year (e.g., 2025)</param>
    /// <param name="month">Month (1-12)</param>
    /// <param name="day">Day (1-31)</param>
    /// <returns>A Result containing the DateField or an error if the date is invalid</returns>
    public static Result<DateField> Create(int year, int month, int day)
    {
        try
        {
            var date = new DateTime(year, month, day);
            return Result<DateField>.Success(new DateField(date));
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return Result<DateField>.Failure($"Invalid date: {year:0000}-{month:00}-{day:00}. {ex.Message}");
        }
    }

    /// <summary>
    /// Creates a DateField from an HL7 date string with validation.
    /// </summary>
    /// <param name="hl7Date">The HL7 date string (YYYYMMDD)</param>
    /// <returns>A Result containing the DateField or an error</returns>
    public static Result<DateField> FromHL7String(string hl7Date)
    {
        try
        {
            var field = new DateField();
            var setResult = field.SetValue(hl7Date);
            
            if (setResult.IsFailure)
                return Result<DateField>.Failure(setResult.Error);
                
            return Result<DateField>.Success(field);
        }
        catch (Exception ex)
        {
            return Result<DateField>.Failure($"Failed to create DateField from HL7 string '{hl7Date}': {ex.Message}");
        }
    }

    /// <summary>
    /// Gets a display-friendly representation of the date.
    /// </summary>
    /// <returns>A human-readable date string</returns>
    public override string GetDisplayValue()
    {
        if (!Value.HasValue)
            return "[No Date]";
            
        return Value.Value.ToString("yyyy-MM-dd");
    }

    /// <summary>
    /// Validates that the date is within reasonable healthcare bounds.
    /// </summary>
    /// <returns>A validation result</returns>
    public Result<bool> ValidateHealthcareBounds()
    {
        if (!Value.HasValue)
            return Result<bool>.Success(true); // Null dates are valid
            
        var date = Value.Value;
        var now = DateTime.Now;
        
        // Healthcare date validation rules
        if (date.Year < 1900)
            return Result<bool>.Failure($"Date {GetDisplayValue()} is before 1900, which is unlikely for healthcare data.");
            
        if (date > now.Date.AddDays(1))
            return Result<bool>.Failure($"Date {GetDisplayValue()} is in the future beyond tomorrow.");
            
        return Result<bool>.Success(true);
    }
}