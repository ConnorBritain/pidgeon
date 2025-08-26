// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

namespace Segmint.Core.Standards.HL7.v23.Fields;

/// <summary>
/// Represents a telephone number field (XTN data type) in HL7 v2.3.
/// Format: [NNN]NNN-NNNN[X....][B|H]^TelecommunicationUse^TelecommunicationEquipmentType^Email^CountryCode^AreaCode^PhoneNumber^Extension
/// Simplified implementation focusing on common phone number formats.
/// </summary>
public class TelephoneField : HL7Field<string?>
{
    public TelephoneField() : base()
    {
    }

    public TelephoneField(string? value) : base(value)
    {
    }

    protected override Result<string?> ParseStringValue(string stringValue)
    {
        if (string.IsNullOrWhiteSpace(stringValue))
            return Result<string?>.Success(null);

        try
        {
            // XTN can be complex, but often just contains phone number in first component
            var components = stringValue.Split('^');
            var phoneNumber = GetComponent(components, 0);
            
            // Clean up the phone number (remove common HL7 formatting)
            if (!string.IsNullOrEmpty(phoneNumber))
            {
                phoneNumber = CleanPhoneNumber(phoneNumber);
            }

            return Result<string?>.Success(phoneNumber);
        }
        catch (Exception ex)
        {
            return Error.Parsing($"Invalid telephone format: {stringValue} - {ex.Message}", "TelephoneField");
        }
    }

    protected override string? FormatTypedValue(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return null;

        // For HL7 output, we can use the phone number as-is in the first component
        // More sophisticated formatting could be added later
        return value;
    }

    /// <summary>
    /// Gets a component from the split array, returning null if index is out of bounds or empty.
    /// </summary>
    private static string? GetComponent(string[] components, int index)
    {
        if (index >= components.Length)
            return null;
            
        var component = components[index]?.Trim();
        return string.IsNullOrEmpty(component) ? null : component;
    }

    /// <summary>
    /// Cleans up phone number formatting for storage.
    /// </summary>
    private static string CleanPhoneNumber(string phoneNumber)
    {
        // Remove common HL7 phone number prefixes and suffixes
        phoneNumber = phoneNumber.Trim();
        
        // Remove business/home indicators that might be at the end
        if (phoneNumber.EndsWith("B") || phoneNumber.EndsWith("H"))
            phoneNumber = phoneNumber[..^1].Trim();
        
        return phoneNumber;
    }

    /// <summary>
    /// Creates a TelephoneField with formatted phone number for HL7.
    /// </summary>
    /// <param name="phoneNumber">The phone number</param>
    /// <param name="use">Telecommunication use (H=Home, B=Business, etc.)</param>
    /// <returns>A TelephoneField instance</returns>
    public static TelephoneField Create(string phoneNumber, string? use = null)
    {
        var formattedNumber = phoneNumber;
        if (!string.IsNullOrEmpty(use))
        {
            formattedNumber += use;
        }
        
        return new TelephoneField(formattedNumber);
    }

    /// <summary>
    /// Formats a phone number as a home phone for HL7.
    /// </summary>
    public static TelephoneField Home(string phoneNumber) => Create(phoneNumber, "H");

    /// <summary>
    /// Formats a phone number as a business phone for HL7.
    /// </summary>
    public static TelephoneField Business(string phoneNumber) => Create(phoneNumber, "B");
}