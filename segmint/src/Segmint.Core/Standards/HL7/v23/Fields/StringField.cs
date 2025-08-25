// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

namespace Segmint.Core.Standards.HL7.v23.Fields;

/// <summary>
/// Represents a simple string field in HL7 v2.3.
/// Used for basic text fields with optional length constraints.
/// </summary>
public class StringField : HL7Field<string>
{
    public StringField() : base()
    {
    }

    public StringField(string? value) : base(value)
    {
    }

    public StringField(string? value, int? maxLength, bool isRequired = false)
        : base(value)
    {
        _maxLength = maxLength;
        _isRequired = isRequired;
    }

    private readonly int? _maxLength;
    private readonly bool _isRequired;

    public override int? MaxLength => _maxLength;
    public override bool IsRequired => _isRequired;

    protected override Result<string> ParseStringValue(string stringValue)
    {
        return Result<string>.Success(stringValue);
    }

    protected override string? FormatTypedValue(string? value)
    {
        return value;
    }

    public override HL7Field Clone()
    {
        return new StringField(Value, MaxLength, IsRequired);
    }

    /// <summary>
    /// Creates a required string field with maximum length.
    /// </summary>
    /// <param name="maxLength">Maximum allowed length</param>
    /// <returns>A configured StringField</returns>
    public static StringField Required(int maxLength)
    {
        return new StringField(null, maxLength, true);
    }

    /// <summary>
    /// Creates an optional string field with maximum length.
    /// </summary>
    /// <param name="maxLength">Maximum allowed length</param>
    /// <returns>A configured StringField</returns>
    public static StringField Optional(int maxLength)
    {
        return new StringField(null, maxLength, false);
    }

    /// <summary>
    /// Creates a required string field without length constraint.
    /// </summary>
    /// <returns>A configured StringField</returns>
    public static StringField Required()
    {
        return new StringField(null, null, true);
    }

    /// <summary>
    /// Creates an optional string field without length constraint.
    /// </summary>
    /// <returns>A configured StringField</returns>
    public static StringField Optional()
    {
        return new StringField(null, null, false);
    }
}

/// <summary>
/// Represents a numeric field in HL7 v2.3.
/// Handles integer and decimal numeric values.
/// </summary>
public class NumericField : HL7Field<decimal?>
{
    public NumericField() : base()
    {
    }

    public NumericField(decimal? value) : base(value)
    {
    }

    public NumericField(string? stringValue) : base(stringValue)
    {
    }

    protected override Result<decimal?> ParseStringValue(string stringValue)
    {
        if (string.IsNullOrWhiteSpace(stringValue))
            return Result<decimal?>.Success(null);

        if (decimal.TryParse(stringValue, out var result))
            return Result<decimal?>.Success(result);

        return Error.Parsing($"Invalid numeric value: {stringValue}", "NumericField");
    }

    protected override string? FormatTypedValue(decimal? value)
    {
        return value?.ToString("0.##########"); // Remove trailing zeros
    }

    public override HL7Field Clone()
    {
        return new NumericField(TypedValue);
    }

    /// <summary>
    /// Gets the value as an integer (truncated).
    /// </summary>
    /// <returns>Integer value or null</returns>
    public int? AsInteger()
    {
        return TypedValue.HasValue ? (int)Math.Truncate(TypedValue.Value) : null;
    }

    /// <summary>
    /// Gets the value as a double.
    /// </summary>
    /// <returns>Double value or null</returns>
    public double? AsDouble()
    {
        return TypedValue.HasValue ? (double)TypedValue.Value : null;
    }
}

/// <summary>
/// Represents a date field in HL7 v2.3.
/// Supports various HL7 date formats (YYYY, YYYYMM, YYYYMMDD).
/// </summary>
public class DateField : HL7Field<DateTime?>
{
    public DateField() : base()
    {
    }

    public DateField(DateTime? value) : base(value)
    {
    }

    public DateField(string? stringValue) : base(stringValue)
    {
    }

    protected override Result<DateTime?> ParseStringValue(string stringValue)
    {
        if (string.IsNullOrWhiteSpace(stringValue))
            return Result<DateTime?>.Success(null);

        // Try different HL7 date formats
        var formats = new[]
        {
            "yyyyMMdd",        // YYYYMMDD
            "yyyyMM",          // YYYYMM (first day of month)
            "yyyy",            // YYYY (January 1st)
            "yyyyMMddHHmm",    // YYYYMMDDHHMM
            "yyyyMMddHHmmss"   // YYYYMMDDHHMMSS
        };

        foreach (var format in formats)
        {
            if (DateTime.TryParseExact(stringValue, format, null, 
                System.Globalization.DateTimeStyles.None, out var result))
            {
                return Result<DateTime?>.Success(result);
            }
        }

        // Try standard parsing as fallback
        if (DateTime.TryParse(stringValue, out var fallbackResult))
            return Result<DateTime?>.Success(fallbackResult);

        return Error.Parsing($"Invalid date format: {stringValue}", "DateField");
    }

    protected override string? FormatTypedValue(DateTime? value)
    {
        return value?.ToString("yyyyMMdd");
    }

    public override HL7Field Clone()
    {
        return new DateField(TypedValue);
    }

    /// <summary>
    /// Formats the date with time components.
    /// </summary>
    /// <returns>Date with time in YYYYMMDDHHMMSS format</returns>
    public string ToHL7DateTime()
    {
        return TypedValue?.ToString("yyyyMMddHHmmss") ?? string.Empty;
    }

    /// <summary>
    /// Formats the date as YYYYMMDD.
    /// </summary>
    /// <returns>Date in YYYYMMDD format</returns>
    public string ToHL7Date()
    {
        return TypedValue?.ToString("yyyyMMdd") ?? string.Empty;
    }
}

/// <summary>
/// Represents a timestamp field in HL7 v2.3.
/// Handles full date/time values with optional timezone.
/// </summary>
public class TimestampField : HL7Field<DateTime?>
{
    public TimestampField() : base()
    {
    }

    public TimestampField(DateTime? value) : base(value)
    {
    }

    public TimestampField(string? stringValue) : base(stringValue)
    {
    }

    protected override Result<DateTime?> ParseStringValue(string stringValue)
    {
        if (string.IsNullOrWhiteSpace(stringValue))
            return Result<DateTime?>.Success(null);

        // Remove timezone info for parsing (HL7 format: YYYYMMDDHHMMSS+ZZZZ)
        var cleanValue = stringValue;
        if (cleanValue.Contains('+') || cleanValue.Contains('-'))
        {
            var timezoneIndex = Math.Max(cleanValue.LastIndexOf('+'), cleanValue.LastIndexOf('-'));
            if (timezoneIndex > 8) // Ensure it's likely a timezone, not a date component
            {
                cleanValue = cleanValue.Substring(0, timezoneIndex);
            }
        }

        var formats = new[]
        {
            "yyyyMMddHHmmss",   // YYYYMMDDHHMMSS
            "yyyyMMddHHmm",     // YYYYMMDDHHMM
            "yyyyMMddHH",       // YYYYMMDDHH
            "yyyyMMdd",         // YYYYMMDD
            "yyyyMM",           // YYYYMM
            "yyyy"              // YYYY
        };

        foreach (var format in formats)
        {
            if (DateTime.TryParseExact(cleanValue, format, null,
                System.Globalization.DateTimeStyles.None, out var result))
            {
                return Result<DateTime?>.Success(result);
            }
        }

        return Error.Parsing($"Invalid timestamp format: {stringValue}", "TimestampField");
    }

    protected override string? FormatTypedValue(DateTime? value)
    {
        return value?.ToString("yyyyMMddHHmmss");
    }

    public override HL7Field Clone()
    {
        return new TimestampField(TypedValue);
    }
}