// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.


using Segmint.Core.HL7;
using System.Collections.Generic;
using System;
namespace Segmint.Core.Standards.HL7.v23.Types;

/// <summary>
/// Represents an HL7 String (ST) field - basic string data type.
/// </summary>
public class StringField : HL7Field
{
    /// <inheritdoc />
    public override string DataType => "ST";

    /// <inheritdoc />
    public override int? MaxLength { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="StringField"/> class.
    /// </summary>
    /// <param name="value">The initial value.</param>
    /// <param name="maxLength">The maximum allowed length.</param>
    /// <param name="isRequired">Whether this field is required.</param>
    public StringField(string? value = null, int? maxLength = null, bool isRequired = false)
        : base(value, isRequired)
    {
        MaxLength = maxLength;
    }

    /// <summary>
    /// Gets the string value of this field.
    /// </summary>
    public string Value => RawValue;

    // Telephone number parsing functionality for HL7 phone fields
    /// <summary>
    /// Gets the phone number portion (excluding area code) if this field contains a phone number.
    /// Supports formats: (555) 123-4567, 555-123-4567, 5551234567
    /// </summary>
    public string PhoneNumber
    {
        get
        {
            var phone = ParsePhoneNumber();
            return phone.phoneNumber ?? RawValue;
        }
    }

    /// <summary>
    /// Gets the area code portion if this field contains a phone number.
    /// Supports formats: (555) 123-4567, 555-123-4567, 5551234567
    /// </summary>
    public string AreaCode
    {
        get
        {
            var phone = ParsePhoneNumber();
            return phone.areaCode ?? "";
        }
    }

    /// <summary>
    /// Parses the phone number into area code and phone number components.
    /// </summary>
    /// <returns>A tuple with area code and phone number, or nulls if not parseable.</returns>
    private (string? areaCode, string? phoneNumber) ParsePhoneNumber()
    {
        if (string.IsNullOrEmpty(RawValue))
            return (null, null);

        // Remove all non-digit characters for parsing
        var digitsOnly = System.Text.RegularExpressions.Regex.Replace(RawValue, @"[^\d]", "");
        
        if (digitsOnly.Length == 10)
        {
            // 10-digit US phone number: AAANNNNNNN -> (AAA) NNN-NNNN
            return (digitsOnly.Substring(0, 3), $"{digitsOnly.Substring(3, 3)}-{digitsOnly.Substring(6, 4)}");
        }
        else if (digitsOnly.Length == 7)
        {
            // 7-digit local number: NNNNNNN -> NNN-NNNN
            return (null, $"{digitsOnly.Substring(0, 3)}-{digitsOnly.Substring(3, 4)}");
        }
        else if (digitsOnly.Length > 10)
        {
            // International or long number - use last 10 digits for US format
            var last10 = digitsOnly.Substring(digitsOnly.Length - 10);
            return (last10.Substring(0, 3), $"{last10.Substring(3, 3)}-{last10.Substring(6, 4)}");
        }
        
        // Can't parse - return original
        return (null, RawValue);
    }

    /// <inheritdoc />
    protected override void ValidateValue(string value)
    {
        // ST fields have minimal validation - just check for HL7 control characters
        if (value.Contains('^') || value.Contains('|') || value.Contains('&') || value.Contains('~') || value.Contains('\\'))
        {
            throw new ArgumentException("String field cannot contain HL7 control characters (^|&~\\).");
        }
    }

    /// <inheritdoc />
    public override HL7Field Clone()
    {
        return new StringField(RawValue, MaxLength, IsRequired);
    }

    /// <summary>
    /// Implicitly converts a string to a StringField.
    /// </summary>
    public static implicit operator StringField(string value)
    {
        return new StringField(value);
    }

    /// <summary>
    /// Implicitly converts a StringField to a string.
    /// </summary>
    public static implicit operator string(StringField field)
    {
        return field.Value;
    }
}
