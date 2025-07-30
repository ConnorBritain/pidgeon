// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.


using Segmint.Core.HL7;
using System.Linq;
using System.Text.RegularExpressions;
using System;
namespace Segmint.Core.Standards.HL7.v23.Types;

/// <summary>
/// Represents an HL7 Extended Telecommunication Number (XTN) field.
/// Supports phone numbers, fax numbers, email addresses, and other telecommunications.
/// </summary>
public partial class TelephoneField : HL7Field
{
    [GeneratedRegex(@"^[+]?[\d\s\-\(\)\.]+$")]
    private static partial Regex PhoneNumberRegex();

    [GeneratedRegex(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$")]
    private static partial Regex EmailRegex();

    /// <inheritdoc />
    public override string DataType => "XTN";

    /// <inheritdoc />
    public override int? MaxLength => 250;

    /// <summary>
    /// Gets or sets the telecommunications use code (PH, FX, MD, CP, etc.).
    /// </summary>
    public string? UseCode { get; set; }

    /// <summary>
    /// Gets or sets the telecommunications equipment type (PH, FX, MD, CP, etc.).
    /// </summary>
    public string? EquipmentType { get; set; }

    /// <summary>
    /// Gets or sets the email address.
    /// </summary>
    public string? EmailAddress { get; set; }

    /// <summary>
    /// Gets or sets the country code.
    /// </summary>
    public string? CountryCode { get; set; }

    /// <summary>
    /// Gets or sets the area/city code.
    /// </summary>
    public string? AreaCode { get; set; }

    /// <summary>
    /// Gets or sets the phone number.
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Gets or sets the extension.
    /// </summary>
    public string? Extension { get; set; }

    /// <summary>
    /// Gets or sets any text representation.
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TelephoneField"/> class.
    /// </summary>
    /// <param name="value">The initial HL7 formatted value.</param>
    /// <param name="isRequired">Whether this field is required.</param>
    public TelephoneField(string? value = null, bool isRequired = false)
        : base(value, isRequired)
    {
        if (!string.IsNullOrEmpty(value))
        {
            ParseFromHL7String(value);
        }
    }

    /// <summary>
    /// Creates a telephone field for a phone number.
    /// </summary>
    /// <param name="phoneNumber">The phone number.</param>
    /// <param name="areaCode">The area code (optional).</param>
    /// <param name="extension">The extension (optional).</param>
    /// <param name="isRequired">Whether this field is required.</param>
    /// <returns>A new <see cref="TelephoneField"/> instance.</returns>
    public static TelephoneField CreatePhone(string phoneNumber, string? areaCode = null, string? extension = null, bool isRequired = false)
    {
        var field = new TelephoneField(isRequired: isRequired)
        {
            UseCode = "PRN",
            EquipmentType = "PH",
            PhoneNumber = phoneNumber,
            AreaCode = areaCode,
            Extension = extension
        };
        
        field.UpdateRawValue();
        return field;
    }

    /// <summary>
    /// Creates a telephone field for an email address.
    /// </summary>
    /// <param name="emailAddress">The email address.</param>
    /// <param name="isRequired">Whether this field is required.</param>
    /// <returns>A new <see cref="TelephoneField"/> instance.</returns>
    public static TelephoneField CreateEmail(string emailAddress, bool isRequired = false)
    {
        var field = new TelephoneField(isRequired: isRequired)
        {
            UseCode = "NET",
            EquipmentType = "Internet",
            EmailAddress = emailAddress
        };
        
        field.UpdateRawValue();
        return field;
    }

    /// <summary>
    /// Creates a telephone field for a fax number.
    /// </summary>
    /// <param name="faxNumber">The fax number.</param>
    /// <param name="areaCode">The area code (optional).</param>
    /// <param name="isRequired">Whether this field is required.</param>
    /// <returns>A new <see cref="TelephoneField"/> instance.</returns>
    public static TelephoneField CreateFax(string faxNumber, string? areaCode = null, bool isRequired = false)
    {
        var field = new TelephoneField(isRequired: isRequired)
        {
            UseCode = "WPN",
            EquipmentType = "FX",
            PhoneNumber = faxNumber,
            AreaCode = areaCode
        };
        
        field.UpdateRawValue();
        return field;
    }

    /// <summary>
    /// Parses the components from an HL7 formatted string.
    /// </summary>
    /// <param name="value">The HL7 formatted string.</param>
    private void ParseFromHL7String(string value)
    {
        if (string.IsNullOrEmpty(value))
            return;

        var components = value.Split('^');
        
        if (components.Length >= 1) UseCode = components[0];
        if (components.Length >= 2) EquipmentType = components[1];
        if (components.Length >= 3) EmailAddress = components[2];
        if (components.Length >= 4) CountryCode = components[3];
        if (components.Length >= 5) AreaCode = components[4];
        if (components.Length >= 6) PhoneNumber = components[5];
        if (components.Length >= 7) Extension = components[6];
        if (components.Length >= 8) Text = components[7];
    }

    /// <summary>
    /// Updates the raw HL7 value from the component values.
    /// </summary>
    private void UpdateRawValue()
    {
        var components = new[]
        {
            UseCode ?? "",
            EquipmentType ?? "",
            EmailAddress ?? "",
            CountryCode ?? "",
            AreaCode ?? "",
            PhoneNumber ?? "",
            Extension ?? "",
            Text ?? ""
        };

        // Find the last non-empty component
        var lastNonEmpty = -1;
        for (var i = components.Length - 1; i >= 0; i--)
        {
            if (!string.IsNullOrEmpty(components[i]))
            {
                lastNonEmpty = i;
                break;
            }
        }

        if (lastNonEmpty >= 0)
        {
            RawValue = string.Join("^", components.Take(lastNonEmpty + 1));
        }
        else
        {
            RawValue = string.Empty;
        }
    }

    /// <inheritdoc />
    protected override void ValidateValue(string value)
    {
        if (string.IsNullOrEmpty(value))
            return;

        var components = value.Split('^');
        
        // Validate email if present
        if (components.Length >= 3 && !string.IsNullOrEmpty(components[2]))
        {
            if (!EmailRegex().IsMatch(components[2]))
            {
                throw new ArgumentException($"Invalid email address format: {components[2]}");
            }
        }

        // Validate phone number if present
        if (components.Length >= 6 && !string.IsNullOrEmpty(components[5]))
        {
            if (!PhoneNumberRegex().IsMatch(components[5]))
            {
                throw new ArgumentException($"Invalid phone number format: {components[5]}");
            }
        }

        // Validate area code if present
        if (components.Length >= 5 && !string.IsNullOrEmpty(components[4]))
        {
            if (!PhoneNumberRegex().IsMatch(components[4]))
            {
                throw new ArgumentException($"Invalid area code format: {components[4]}");
            }
        }
    }

    /// <inheritdoc />
    public override string ToHL7String()
    {
        return RawValue;
    }

    /// <inheritdoc />
    public override HL7Field Clone()
    {
        return new TelephoneField(RawValue, IsRequired);
    }

    /// <summary>
    /// Implicitly converts a string to a TelephoneField.
    /// </summary>
    /// <param name="value">The string value.</param>
    public static implicit operator TelephoneField(string value)
    {
        return new TelephoneField(value);
    }

    /// <summary>
    /// Implicitly converts a TelephoneField to a string.
    /// </summary>
    /// <param name="field">The TelephoneField.</param>
    public static implicit operator string(TelephoneField field)
    {
        return field.RawValue;
    }

    /// <summary>
    /// Gets a formatted display string for this telephone number.
    /// </summary>
    /// <returns>A human-readable representation of the telephone number.</returns>
    public string ToDisplayString()
    {
        if (!string.IsNullOrEmpty(EmailAddress))
        {
            return EmailAddress;
        }

        if (!string.IsNullOrEmpty(PhoneNumber))
        {
            var display = PhoneNumber;
            
            if (!string.IsNullOrEmpty(AreaCode))
            {
                display = $"({AreaCode}) {display}";
            }
            
            if (!string.IsNullOrEmpty(Extension))
            {
                display = $"{display} ext. {Extension}";
            }
            
            return display;
        }

        return RawValue;
    }
}
