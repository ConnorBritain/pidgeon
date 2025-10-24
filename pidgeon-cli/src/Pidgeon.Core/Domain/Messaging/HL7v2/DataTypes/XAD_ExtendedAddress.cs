// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Pidgeon.Core;
using Pidgeon.Core.Application.DTOs;
using Pidgeon.Core.Domain.Messaging.HL7v2.Common;

namespace Pidgeon.Core.Domain.Messaging.HL7v2.DataTypes;

/// <summary>
/// Represents an address field (XAD data type) in HL7 v2.3.
/// Format: Street^OtherDesignation^City^State^Zip^Country^AddressType^OtherGeographicDesignation
/// </summary>
public class AddressField : HL7Field<AddressDto?>
{
    /// <inheritdoc />
    public override string DataType => "XAD";

    public AddressField()
    {
    }

    public AddressField(AddressDto? value)
    {
        Value = value;
        RawValue = FormatValue(value);
    }

    public AddressField(string? stringValue)
    {
        var parseResult = ParseFromHL7String(stringValue ?? "");
        if (parseResult.IsSuccess)
        {
            Value = parseResult.Value;
            RawValue = stringValue ?? "";
        }
    }

    protected override Result<AddressDto?> ParseFromHL7String(string hl7Value)
    {
        if (string.IsNullOrWhiteSpace(hl7Value))
            return Result<AddressDto?>.Success(null);

        try
        {
            // Split on component separator (^)
            var components = hl7Value.Split('^');
            
            // XAD fields: Street^OtherDesignation^City^State^Zip^Country^AddressType^OtherGeographicDesignation
            var street1 = GetComponent(components, 0);
            var street2 = GetComponent(components, 1); // Other designation can be street2
            var city = GetComponent(components, 2);
            var state = GetComponent(components, 3);
            var postalCode = GetComponent(components, 4);
            var country = GetComponent(components, 5);
            
            var address = new AddressDto
            {
                Street1 = street1,
                Street2 = street2,
                City = city,
                State = state,
                PostalCode = postalCode,
                Country = country
            };

            return Result<AddressDto?>.Success(address);
        }
        catch (Exception ex)
        {
            return Result<AddressDto?>.Failure($"Invalid address format: {hl7Value} - {ex.Message}");
        }
    }

    protected override string FormatValue(AddressDto? value)
    {
        if (value == null)
            return "";

        var components = new[]
        {
            value.Street1 ?? "",
            value.Street2 ?? "",
            value.City ?? "",
            value.State ?? "",
            value.PostalCode ?? "",
            value.Country ?? "",
            "", // Address Type - typically empty
            ""  // Other Geographic Designation - typically empty
        };

        // Trim trailing empty components
        var lastNonEmptyIndex = -1;
        for (int i = components.Length - 1; i >= 0; i--)
        {
            if (!string.IsNullOrEmpty(components[i]))
            {
                lastNonEmptyIndex = i;
                break;
            }
        }

        if (lastNonEmptyIndex == -1)
            return "";

        return string.Join("^", components.Take(lastNonEmptyIndex + 1));
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
    /// Creates an AddressField from separate address components.
    /// </summary>
    public static AddressField Create(string? street1, string? city, string? state, 
        string? postalCode, string? street2 = null, string? country = null)
    {
        var address = new AddressDto
        {
            Street1 = street1,
            Street2 = street2,
            City = city,
            State = state,
            PostalCode = postalCode,
            Country = country
        };
        
        return new AddressField(address);
    }
}