// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.


using Segmint.Core.HL7;
using System.Linq;
using System.Text.RegularExpressions;
using System;
namespace Segmint.Core.Standards.HL7.v23.Types;

/// <summary>
/// Represents an HL7 Hierarchic Designator (HD) field.
/// Used for identifying organizations, systems, and applications.
/// </summary>
public partial class HierarchicDesignatorField : HL7Field
{
    [GeneratedRegex(@"^[A-Za-z0-9\-\._ ]+$")]
    private static partial Regex NamespaceRegex();

    [GeneratedRegex(@"^[0-9\.]+$")]
    private static partial Regex OidRegex();

    /// <inheritdoc />
    public override string DataType => "HD";

    /// <inheritdoc />
    public override int? MaxLength => 180;

    /// <summary>
    /// Gets or sets the namespace ID.
    /// </summary>
    public string? NamespaceId { get; set; }

    /// <summary>
    /// Gets or sets the universal ID.
    /// </summary>
    public string? UniversalId { get; set; }

    /// <summary>
    /// Gets or sets the universal ID type.
    /// </summary>
    public string? UniversalIdType { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="HierarchicDesignatorField"/> class.
    /// </summary>
    /// <param name="value">The initial HL7 formatted value.</param>
    /// <param name="isRequired">Whether this field is required.</param>
    public HierarchicDesignatorField(string? value = null, bool isRequired = false)
        : base(value, isRequired)
    {
        if (!string.IsNullOrEmpty(value))
        {
            ParseFromHL7String(value);
        }
    }

    /// <summary>
    /// Creates a Hierarchic Designator field with a namespace ID.
    /// </summary>
    /// <param name="namespaceId">The namespace identifier.</param>
    /// <param name="isRequired">Whether this field is required.</param>
    /// <returns>A new <see cref="HierarchicDesignatorField"/> instance.</returns>
    public static HierarchicDesignatorField CreateFromNamespace(string namespaceId, bool isRequired = false)
    {
        var field = new HierarchicDesignatorField(isRequired: isRequired)
        {
            NamespaceId = namespaceId
        };
        
        field.UpdateRawValue();
        return field;
    }

    /// <summary>
    /// Creates a Hierarchic Designator field with a universal ID (OID).
    /// </summary>
    /// <param name="universalId">The universal identifier (typically an OID).</param>
    /// <param name="universalIdType">The universal ID type (e.g., "ISO", "DNS").</param>
    /// <param name="isRequired">Whether this field is required.</param>
    /// <returns>A new <see cref="HierarchicDesignatorField"/> instance.</returns>
    public static HierarchicDesignatorField CreateFromUniversalId(string universalId, string universalIdType = "ISO", 
        bool isRequired = false)
    {
        var field = new HierarchicDesignatorField(isRequired: isRequired)
        {
            UniversalId = universalId,
            UniversalIdType = universalIdType
        };
        
        field.UpdateRawValue();
        return field;
    }

    /// <summary>
    /// Creates a Hierarchic Designator field for a healthcare facility.
    /// </summary>
    /// <param name="facilityName">The facility name.</param>
    /// <param name="facilityOid">The facility OID (optional).</param>
    /// <param name="isRequired">Whether this field is required.</param>
    /// <returns>A new <see cref="HierarchicDesignatorField"/> instance.</returns>
    public static HierarchicDesignatorField CreateFacility(string facilityName, string? facilityOid = null, 
        bool isRequired = false)
    {
        var field = new HierarchicDesignatorField(isRequired: isRequired)
        {
            NamespaceId = facilityName,
            UniversalId = facilityOid,
            UniversalIdType = !string.IsNullOrEmpty(facilityOid) ? "ISO" : null
        };
        
        field.UpdateRawValue();
        return field;
    }

    /// <summary>
    /// Creates a Hierarchic Designator field for an application.
    /// </summary>
    /// <param name="applicationName">The application name.</param>
    /// <param name="applicationOid">The application OID (optional).</param>
    /// <param name="isRequired">Whether this field is required.</param>
    /// <returns>A new <see cref="HierarchicDesignatorField"/> instance.</returns>
    public static HierarchicDesignatorField CreateApplication(string applicationName, string? applicationOid = null, 
        bool isRequired = false)
    {
        var field = new HierarchicDesignatorField(isRequired: isRequired)
        {
            NamespaceId = applicationName,
            UniversalId = applicationOid,
            UniversalIdType = !string.IsNullOrEmpty(applicationOid) ? "ISO" : null
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
        
        if (components.Length >= 1) NamespaceId = components[0];
        if (components.Length >= 2) UniversalId = components[1];
        if (components.Length >= 3) UniversalIdType = components[2];
    }

    /// <summary>
    /// Updates the raw HL7 value from the component values.
    /// </summary>
    private void UpdateRawValue()
    {
        var components = new[]
        {
            NamespaceId ?? "",
            UniversalId ?? "",
            UniversalIdType ?? ""
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
        
        // Validate namespace ID if present
        if (components.Length >= 1 && !string.IsNullOrEmpty(components[0]))
        {
            if (!NamespaceRegex().IsMatch(components[0]))
            {
                throw new ArgumentException($"Invalid namespace ID format: {components[0]}");
            }
        }

        // Validate universal ID if present
        if (components.Length >= 2 && !string.IsNullOrEmpty(components[1]))
        {
            // If universal ID type is ISO, validate as OID
            if (components.Length >= 3 && components[2] == "ISO")
            {
                if (!OidRegex().IsMatch(components[1]))
                {
                    throw new ArgumentException($"Invalid OID format: {components[1]}");
                }
            }
        }

        // Validate universal ID type if present
        if (components.Length >= 3 && !string.IsNullOrEmpty(components[2]))
        {
            var validTypes = new[] { "ISO", "DNS", "GUID", "HCD", "HL7", "L", "M", "N", "Random", "UUID", "x400", "x500" };
            if (!validTypes.Contains(components[2]))
            {
                throw new ArgumentException($"Invalid universal ID type: {components[2]}");
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
        return new HierarchicDesignatorField(RawValue, IsRequired);
    }

    /// <summary>
    /// Implicitly converts a string to a HierarchicDesignatorField.
    /// </summary>
    /// <param name="value">The string value.</param>
    public static implicit operator HierarchicDesignatorField(string value)
    {
        return new HierarchicDesignatorField(value);
    }

    /// <summary>
    /// Implicitly converts a HierarchicDesignatorField to a string.
    /// </summary>
    /// <param name="field">The HierarchicDesignatorField.</param>
    public static implicit operator string(HierarchicDesignatorField field)
    {
        return field.RawValue;
    }

    /// <summary>
    /// Gets a formatted display string for this hierarchic designator.
    /// </summary>
    /// <returns>A human-readable representation of the hierarchic designator.</returns>
    public string ToDisplayString()
    {
        if (!string.IsNullOrEmpty(NamespaceId))
        {
            var display = NamespaceId;
            
            if (!string.IsNullOrEmpty(UniversalId))
            {
                display = $"{display} ({UniversalId})";
            }
            
            return display;
        }

        if (!string.IsNullOrEmpty(UniversalId))
        {
            return UniversalId;
        }

        return RawValue;
    }
}
