// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.


using Segmint.Core.HL7;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System;
namespace Segmint.Core.Standards.HL7.v23.Types;

/// <summary>
/// Represents an HL7 Extended Address (XAD) field.
/// Format: street_address^other_designation^city^state_or_province^zip_or_postal_code^country^address_type^other_geographic_designation^county_parish_code^census_tract^address_representation_code^address_validity_range
/// </summary>
public class AddressField : HL7Field
{
    /// <inheritdoc />
    public override string DataType => "XAD";

    /// <inheritdoc />
    public override int? MaxLength => 250; // Standard maximum for XAD fields

    /// <summary>
    /// Initializes a new instance of the <see cref="AddressField"/> class.
    /// </summary>
    /// <param name="value">The initial address value.</param>
    /// <param name="isRequired">Whether this field is required.</param>
    public AddressField(string? value = null, bool isRequired = false)
        : base(value, isRequired)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AddressField"/> class with address components.
    /// </summary>
    /// <param name="streetAddress">The street address.</param>
    /// <param name="city">The city.</param>
    /// <param name="stateOrProvince">The state or province.</param>
    /// <param name="zipOrPostalCode">The ZIP or postal code.</param>
    /// <param name="country">The country.</param>
    /// <param name="addressType">The address type (H=Home, B=Business, etc.).</param>
    /// <param name="otherDesignation">Other designation (apartment, suite, etc.).</param>
    /// <param name="isRequired">Whether this field is required.</param>
    public AddressField(
        string streetAddress,
        string? city,
        string? stateOrProvince = null,
        string? zipOrPostalCode = null,
        string? country = null,
        string? addressType = null,
        string? otherDesignation = null,
        bool isRequired = false)
        : base(null, isRequired)
    {
        SetComponents(streetAddress, otherDesignation, city, stateOrProvince, zipOrPostalCode, country, addressType);
    }

    /// <summary>
    /// Gets or sets the street address component.
    /// </summary>
    public string StreetAddress
    {
        get => GetComponent(1);
        set => SetComponent(1, value);
    }

    /// <summary>
    /// Gets or sets the other designation component (apartment, suite, etc.).
    /// </summary>
    public string OtherDesignation
    {
        get => GetComponent(2);
        set => SetComponent(2, value);
    }

    /// <summary>
    /// Gets or sets the city component.
    /// </summary>
    public string City
    {
        get => GetComponent(3);
        set => SetComponent(3, value);
    }

    /// <summary>
    /// Gets or sets the state or province component.
    /// </summary>
    public string StateOrProvince
    {
        get => GetComponent(4);
        set => SetComponent(4, value);
    }

    /// <summary>
    /// Gets or sets the ZIP or postal code component.
    /// </summary>
    public string ZipOrPostalCode
    {
        get => GetComponent(5);
        set => SetComponent(5, value);
    }

    /// <summary>
    /// Gets or sets the country component.
    /// </summary>
    public string Country
    {
        get => GetComponent(6);
        set => SetComponent(6, value);
    }

    /// <summary>
    /// Gets or sets the address type component (H=Home, B=Business, etc.).
    /// </summary>
    public string AddressType
    {
        get => GetComponent(7);
        set => SetComponent(7, value);
    }

    /// <summary>
    /// Gets or sets the other geographic designation component.
    /// </summary>
    public string OtherGeographicDesignation
    {
        get => GetComponent(8);
        set => SetComponent(8, value);
    }

    /// <summary>
    /// Gets or sets the county/parish code component.
    /// </summary>
    public string CountyParishCode
    {
        get => GetComponent(9);
        set => SetComponent(9, value);
    }

    /// <summary>
    /// Gets or sets the census tract component.
    /// </summary>
    public string CensusTract
    {
        get => GetComponent(10);
        set => SetComponent(10, value);
    }

    /// <summary>
    /// Gets the formatted address display (Street, City, State ZIP).
    /// </summary>
    public string DisplayAddress
    {
        get
        {
            var parts = new List<string>();

            // Street address line
            var streetLine = StreetAddress;
            if (!string.IsNullOrEmpty(OtherDesignation))
            {
                streetLine += $" {OtherDesignation}";
            }
            if (!string.IsNullOrEmpty(streetLine))
            {
                parts.Add(streetLine);
            }

            // City, State ZIP line
            var cityStateZip = new List<string>();
            if (!string.IsNullOrEmpty(City))
                cityStateZip.Add(City);

            var stateZip = StateOrProvince;
            if (!string.IsNullOrEmpty(ZipOrPostalCode))
            {
                stateZip += $" {ZipOrPostalCode}";
            }
            if (!string.IsNullOrEmpty(stateZip))
            {
                cityStateZip.Add(stateZip);
            }

            if (cityStateZip.Count > 0)
            {
                parts.Add(string.Join(", ", cityStateZip));
            }

            // Country (if not US)
            if (!string.IsNullOrEmpty(Country) && !Country.Equals("US", StringComparison.OrdinalIgnoreCase) && !Country.Equals("USA", StringComparison.OrdinalIgnoreCase))
            {
                parts.Add(Country);
            }

            return string.Join(Environment.NewLine, parts);
        }
    }

    /// <summary>
    /// Gets the single-line formatted address.
    /// </summary>
    public string SingleLineAddress
    {
        get
        {
            var parts = new List<string>();

            if (!string.IsNullOrEmpty(StreetAddress))
                parts.Add(StreetAddress);

            if (!string.IsNullOrEmpty(OtherDesignation))
                parts.Add(OtherDesignation);

            if (!string.IsNullOrEmpty(City))
                parts.Add(City);

            if (!string.IsNullOrEmpty(StateOrProvince))
                parts.Add(StateOrProvince);

            if (!string.IsNullOrEmpty(ZipOrPostalCode))
                parts.Add(ZipOrPostalCode);

            if (!string.IsNullOrEmpty(Country) && !Country.Equals("US", StringComparison.OrdinalIgnoreCase))
                parts.Add(Country);

            return string.Join(", ", parts);
        }
    }

    /// <summary>
    /// Sets the primary address components.
    /// </summary>
    /// <param name="streetAddress">The street address.</param>
    /// <param name="otherDesignation">Other designation (apartment, suite, etc.).</param>
    /// <param name="city">The city.</param>
    /// <param name="stateOrProvince">The state or province.</param>
    /// <param name="zipOrPostalCode">The ZIP or postal code.</param>
    /// <param name="country">The country.</param>
    /// <param name="addressType">The address type.</param>
    public void SetComponents(
        string? streetAddress = null,
        string? otherDesignation = null,
        string? city = null,
        string? stateOrProvince = null,
        string? zipOrPostalCode = null,
        string? country = null,
        string? addressType = null)
    {
        var components = new[]
        {
            streetAddress ?? string.Empty,
            otherDesignation ?? string.Empty,
            city ?? string.Empty,
            stateOrProvince ?? string.Empty,
            zipOrPostalCode ?? string.Empty,
            country ?? string.Empty,
            addressType ?? string.Empty,
            string.Empty, // other_geographic_designation
            string.Empty, // county_parish_code
            string.Empty, // census_tract
            string.Empty, // address_representation_code
            string.Empty  // address_validity_range
        };

        // Remove trailing empty components
        var lastNonEmpty = -1;
        for (int i = components.Length - 1; i >= 0; i--)
        {
            if (!string.IsNullOrEmpty(components[i]))
            {
                lastNonEmpty = i;
                break;
            }
        }

        if (lastNonEmpty == -1)
        {
            RawValue = string.Empty;
        }
        else
        {
            var result = new string[lastNonEmpty + 1];
            Array.Copy(components, result, lastNonEmpty + 1);
            RawValue = string.Join("^", result);
        }
    }

    /// <summary>
    /// Gets a component by its 1-based index.
    /// </summary>
    /// <param name="index">The 1-based component index (1-12).</param>
    /// <returns>The component value, or empty string if not present.</returns>
    public string GetComponent(int index)
    {
        if (index < 1 || index > 12)
            throw new ArgumentOutOfRangeException(nameof(index), "Component index must be between 1 and 12.");

        if (IsEmpty)
            return string.Empty;

        var components = RawValue.Split('^');
        return index <= components.Length ? components[index - 1] : string.Empty;
    }

    /// <summary>
    /// Sets a component by its 1-based index.
    /// </summary>
    /// <param name="index">The 1-based component index (1-12).</param>
    /// <param name="value">The component value to set.</param>
    public void SetComponent(int index, string? value)
    {
        if (index < 1 || index > 12)
            throw new ArgumentOutOfRangeException(nameof(index), "Component index must be between 1 and 12.");

        var components = IsEmpty ? new string[12] : RawValue.Split('^');
        
        // Expand array if necessary
        if (components.Length < 12)
        {
            var newComponents = new string[12];
            Array.Copy(components, newComponents, components.Length);
            for (int i = components.Length; i < 12; i++)
            {
                newComponents[i] = string.Empty;
            }
            components = newComponents;
        }

        components[index - 1] = value ?? string.Empty;

        // Remove trailing empty components
        var lastNonEmpty = -1;
        for (int i = components.Length - 1; i >= 0; i--)
        {
            if (!string.IsNullOrEmpty(components[i]))
            {
                lastNonEmpty = i;
                break;
            }
        }

        if (lastNonEmpty == -1)
        {
            RawValue = string.Empty;
        }
        else
        {
            var result = new string[lastNonEmpty + 1];
            Array.Copy(components, result, lastNonEmpty + 1);
            RawValue = string.Join("^", result);
        }
    }

    /// <inheritdoc />
    protected override void ValidateValue(string value)
    {
        if (string.IsNullOrEmpty(value))
            return; // Empty values are handled by the base class

        // Check that we don't have more than 12 components
        var components = value.Split('^');
        if (components.Length > 12)
        {
            throw new ArgumentException($"Address cannot have more than 12 components. Found {components.Length} components.");
        }

        // Validate that components don't contain HL7 control characters
        foreach (var component in components)
        {
            if (component.Contains('|') || component.Contains('&') || component.Contains('~') || component.Contains('\\'))
            {
                throw new ArgumentException("Address components cannot contain HL7 control characters (|&~\\).");
            }
        }
    }

    /// <inheritdoc />
    public override HL7Field Clone()
    {
        return new AddressField(RawValue, IsRequired);
    }

    /// <summary>
    /// Creates a standard US address.
    /// </summary>
    /// <param name="streetAddress">The street address.</param>
    /// <param name="city">The city.</param>
    /// <param name="state">The state (2-letter abbreviation recommended).</param>
    /// <param name="zipCode">The ZIP code.</param>
    /// <param name="addressType">The address type (H=Home, B=Business, etc.).</param>
    /// <param name="isRequired">Whether this field is required.</param>
    /// <returns>A new AddressField instance.</returns>
    public static AddressField CreateUSAddress(
        string streetAddress,
        string city,
        string state,
        string zipCode,
        string? addressType = null,
        bool isRequired = false)
    {
        return new AddressField(streetAddress, city, state, zipCode, "US", addressType, isRequired: isRequired);
    }

    /// <summary>
    /// Creates an international address.
    /// </summary>
    /// <param name="streetAddress">The street address.</param>
    /// <param name="city">The city.</param>
    /// <param name="stateOrProvince">The state or province.</param>
    /// <param name="postalCode">The postal code.</param>
    /// <param name="country">The country.</param>
    /// <param name="addressType">The address type (H=Home, B=Business, etc.).</param>
    /// <param name="isRequired">Whether this field is required.</param>
    /// <returns>A new AddressField instance.</returns>
    public static AddressField CreateInternationalAddress(
        string streetAddress,
        string city,
        string? stateOrProvince,
        string? postalCode,
        string country,
        string? addressType = null,
        bool isRequired = false)
    {
        return new AddressField(streetAddress, city, stateOrProvince, postalCode, country, addressType, isRequired: isRequired);
    }

    /// <summary>
    /// Implicitly converts a string to an AddressField.
    /// </summary>
    public static implicit operator AddressField(string value)
    {
        return new AddressField(value);
    }

    /// <summary>
    /// Implicitly converts an AddressField to a string.
    /// </summary>
    public static implicit operator string(AddressField field)
    {
        return field.RawValue;
    }
}
