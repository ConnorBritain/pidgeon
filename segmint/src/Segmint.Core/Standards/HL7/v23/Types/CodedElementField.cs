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
/// Represents an HL7 Coded Element (CE) field.
/// Format: identifier^text^name_of_coding_system^alternate_identifier^alternate_text^name_of_alternate_coding_system
/// </summary>
public class CodedElementField : HL7Field
{
    /// <inheritdoc />
    public override string DataType => "CE";

    /// <inheritdoc />
    public override int? MaxLength => 250; // Standard maximum for CE fields

    /// <summary>
    /// Initializes a new instance of the <see cref="CodedElementField"/> class.
    /// </summary>
    /// <param name="value">The initial coded element value.</param>
    /// <param name="isRequired">Whether this field is required.</param>
    public CodedElementField(string? value = null, bool isRequired = false)
        : base(value, isRequired)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CodedElementField"/> class with individual components.
    /// </summary>
    /// <param name="identifier">The primary identifier/code.</param>
    /// <param name="text">The text description.</param>
    /// <param name="codingSystem">The name of the coding system.</param>
    /// <param name="alternateIdentifier">The alternate identifier/code.</param>
    /// <param name="alternateText">The alternate text description.</param>
    /// <param name="alternateCodingSystem">The name of the alternate coding system.</param>
    /// <param name="isRequired">Whether this field is required.</param>
    public CodedElementField(
        string identifier,
        string? text,
        string? codingSystem = null,
        string? alternateIdentifier = null,
        string? alternateText = null,
        string? alternateCodingSystem = null,
        bool isRequired = false)
        : base(null, isRequired)
    {
        SetComponents(identifier, text, codingSystem, alternateIdentifier, alternateText, alternateCodingSystem);
    }

    /// <summary>
    /// Gets the primary identifier/code component.
    /// </summary>
    public string Identifier
    {
        get => GetComponent(1);
        set => SetComponent(1, value);
    }

    /// <summary>
    /// Gets the text description component.
    /// </summary>
    public string Text
    {
        get => GetComponent(2);
        set => SetComponent(2, value);
    }

    /// <summary>
    /// Gets the coding system name component.
    /// </summary>
    public string CodingSystem
    {
        get => GetComponent(3);
        set => SetComponent(3, value);
    }

    /// <summary>
    /// Gets the alternate identifier/code component.
    /// </summary>
    public string AlternateIdentifier
    {
        get => GetComponent(4);
        set => SetComponent(4, value);
    }

    /// <summary>
    /// Gets the alternate text description component.
    /// </summary>
    public string AlternateText
    {
        get => GetComponent(5);
        set => SetComponent(5, value);
    }

    /// <summary>
    /// Gets the alternate coding system name component.
    /// </summary>
    public string AlternateCodingSystem
    {
        get => GetComponent(6);
        set => SetComponent(6, value);
    }

    /// <summary>
    /// Gets or sets the value of this coded element field.
    /// </summary>
    public string Value
    {
        get => RawValue;
        set => SetValue(value);
    }

    /// <summary>
    /// Gets the primary value (identifier) of this coded element.
    /// </summary>
    /// <returns>The primary identifier/code, or empty string if not set.</returns>
    public string GetPrimaryValue()
    {
        return Identifier;
    }

    /// <summary>
    /// Sets all components of the coded element.
    /// </summary>
    /// <param name="identifier">The primary identifier/code.</param>
    /// <param name="text">The text description.</param>
    /// <param name="codingSystem">The name of the coding system.</param>
    /// <param name="alternateIdentifier">The alternate identifier/code.</param>
    /// <param name="alternateText">The alternate text description.</param>
    /// <param name="alternateCodingSystem">The name of the alternate coding system.</param>
    public void SetComponents(
        string? identifier = null,
        string? text = null,
        string? codingSystem = null,
        string? alternateIdentifier = null,
        string? alternateText = null,
        string? alternateCodingSystem = null)
    {
        var components = new[]
        {
            identifier ?? string.Empty,
            text ?? string.Empty,
            codingSystem ?? string.Empty,
            alternateIdentifier ?? string.Empty,
            alternateText ?? string.Empty,
            alternateCodingSystem ?? string.Empty
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
    /// <param name="index">The 1-based component index (1-6).</param>
    /// <returns>The component value, or empty string if not present.</returns>
    public string GetComponent(int index)
    {
        if (index < 1 || index > 6)
            throw new ArgumentOutOfRangeException(nameof(index), "Component index must be between 1 and 6.");

        if (IsEmpty)
            return string.Empty;

        var components = RawValue.Split('^');
        return index <= components.Length ? components[index - 1] : string.Empty;
    }

    /// <summary>
    /// Sets a component by its 1-based index.
    /// </summary>
    /// <param name="index">The 1-based component index (1-6).</param>
    /// <param name="value">The component value to set.</param>
    public void SetComponent(int index, string? value)
    {
        if (index < 1 || index > 6)
            throw new ArgumentOutOfRangeException(nameof(index), "Component index must be between 1 and 6.");

        var components = IsEmpty ? new string[6] : RawValue.Split('^');
        
        // Expand array if necessary
        if (components.Length < 6)
        {
            var newComponents = new string[6];
            Array.Copy(components, newComponents, components.Length);
            for (int i = components.Length; i < 6; i++)
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

        // Check that we don't have more than 6 components
        var components = value.Split('^');
        if (components.Length > 6)
        {
            throw new ArgumentException($"Coded element cannot have more than 6 components. Found {components.Length} components.");
        }

        // Validate that components don't contain HL7 control characters
        foreach (var component in components)
        {
            if (component.Contains('|') || component.Contains('&') || component.Contains('~') || component.Contains('\\'))
            {
                throw new ArgumentException("Coded element components cannot contain HL7 control characters (|&~\\).");
            }
        }
    }

    /// <inheritdoc />
    public override HL7Field Clone()
    {
        return new CodedElementField(RawValue, IsRequired);
    }

    /// <summary>
    /// Creates a coded element with just an identifier and text.
    /// </summary>
    /// <param name="identifier">The code identifier.</param>
    /// <param name="text">The text description.</param>
    /// <param name="codingSystem">The coding system name.</param>
    /// <param name="isRequired">Whether this field is required.</param>
    /// <returns>A new CodedElementField instance.</returns>
    public static CodedElementField Create(string identifier, string? text = null, string? codingSystem = null, bool isRequired = false)
    {
        return new CodedElementField(identifier, text, codingSystem, isRequired: isRequired);
    }

    /// <summary>
    /// Creates a coded element for common HL7 message types.
    /// </summary>
    /// <param name="messageType">The message type (e.g., "RDE", "ADT", "ACK").</param>
    /// <param name="isRequired">Whether this field is required.</param>
    /// <returns>A new CodedElementField instance.</returns>
    public static CodedElementField MessageType(string messageType, bool isRequired = true)
    {
        var text = messageType switch
        {
            "RDE" => "Pharmacy/Treatment Encoded Order",
            "RDS" => "Pharmacy/Treatment Dispense",
            "ADT" => "Admit/Discharge/Transfer",
            "ORM" => "Order Message",
            "ACK" => "General Acknowledgment",
            "RXE" => "Pharmacy/Treatment Encoded Order",
            "RXD" => "Pharmacy/Treatment Dispense",
            _ => messageType
        };

        return new CodedElementField(messageType, text, "HL7", isRequired: isRequired);
    }

    /// <summary>
    /// Creates a coded element for medication routes.
    /// </summary>
    /// <param name="routeCode">The route code (e.g., "PO", "IV", "IM").</param>
    /// <param name="routeText">The route description.</param>
    /// <param name="codingSystem">The coding system.</param>
    /// <param name="isRequired">Whether this field is required.</param>
    /// <returns>A new CodedElementField instance.</returns>
    public static CodedElementField CreateRoute(string routeCode, string? routeText = null, string? codingSystem = null, bool isRequired = false)
    {
        return new CodedElementField(routeCode, routeText, codingSystem, isRequired: isRequired);
    }

    /// <summary>
    /// Creates a coded element for administration sites.
    /// </summary>
    /// <param name="siteCode">The site code.</param>
    /// <param name="siteText">The site description.</param>
    /// <param name="codingSystem">The coding system.</param>
    /// <param name="isRequired">Whether this field is required.</param>
    /// <returns>A new CodedElementField instance.</returns>
    public static CodedElementField CreateSite(string siteCode, string? siteText = null, string? codingSystem = null, bool isRequired = false)
    {
        return new CodedElementField(siteCode, siteText, codingSystem, isRequired: isRequired);
    }

    /// <summary>
    /// Creates a coded element for administration devices.
    /// </summary>
    /// <param name="deviceCode">The device code.</param>
    /// <param name="deviceText">The device description.</param>
    /// <param name="codingSystem">The coding system.</param>
    /// <param name="isRequired">Whether this field is required.</param>
    /// <returns>A new CodedElementField instance.</returns>
    public static CodedElementField CreateDevice(string deviceCode, string? deviceText = null, string? codingSystem = null, bool isRequired = false)
    {
        return new CodedElementField(deviceCode, deviceText, codingSystem, isRequired: isRequired);
    }

    /// <summary>
    /// Creates a coded element for administration methods.
    /// </summary>
    /// <param name="methodCode">The method code.</param>
    /// <param name="methodText">The method description.</param>
    /// <param name="codingSystem">The coding system.</param>
    /// <param name="isRequired">Whether this field is required.</param>
    /// <returns>A new CodedElementField instance.</returns>
    public static CodedElementField CreateMethod(string methodCode, string? methodText = null, string? codingSystem = null, bool isRequired = false)
    {
        return new CodedElementField(methodCode, methodText, codingSystem, isRequired: isRequired);
    }

    /// <summary>
    /// Gets a formatted display string for this coded element.
    /// </summary>
    /// <returns>A human-readable representation of the coded element.</returns>
    public string ToDisplayString()
    {
        if (!string.IsNullOrEmpty(Text))
        {
            return !string.IsNullOrEmpty(Identifier) ? $"{Identifier} - {Text}" : Text;
        }
        
        return !string.IsNullOrEmpty(Identifier) ? Identifier : RawValue;
    }

    /// <summary>
    /// Implicitly converts a string to a CodedElementField.
    /// </summary>
    public static implicit operator CodedElementField(string value)
    {
        return new CodedElementField(value);
    }

    /// <summary>
    /// Implicitly converts a CodedElementField to a string.
    /// </summary>
    public static implicit operator string(CodedElementField field)
    {
        return field.RawValue;
    }
}
