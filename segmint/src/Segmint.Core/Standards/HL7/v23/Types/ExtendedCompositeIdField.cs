// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.


using Segmint.Core.HL7;
using System.Linq;
using System.Text.RegularExpressions;
using System;
namespace Segmint.Core.Standards.HL7.v23.Types;

/// <summary>
/// Represents an HL7 Extended Composite ID (CX) field.
/// Used for patient identifiers, account numbers, and other composite identifiers.
/// </summary>
public partial class ExtendedCompositeIdField : HL7Field
{
    [GeneratedRegex(@"^[A-Za-z0-9\-\.]+$")]
    private static partial Regex IdentifierRegex();

    /// <inheritdoc />
    public override string DataType => "CX";

    /// <inheritdoc />
    public override int? MaxLength => 250;

    /// <summary>
    /// Gets or sets the ID number.
    /// </summary>
    public string? IdNumber { get; set; }

    /// <summary>
    /// Gets or sets the check digit.
    /// </summary>
    public string? CheckDigit { get; set; }

    /// <summary>
    /// Gets or sets the code identifying the check digit scheme employed.
    /// </summary>
    public string? CheckDigitScheme { get; set; }

    /// <summary>
    /// Gets or sets the assigning authority.
    /// </summary>
    public string? AssigningAuthority { get; set; }

    /// <summary>
    /// Gets or sets the identifier type code.
    /// </summary>
    public string? IdentifierTypeCode { get; set; }

    /// <summary>
    /// Gets or sets the assigning facility.
    /// </summary>
    public string? AssigningFacility { get; set; }

    /// <summary>
    /// Gets or sets the effective date.
    /// </summary>
    public string? EffectiveDate { get; set; }

    /// <summary>
    /// Gets or sets the expiration date.
    /// </summary>
    public string? ExpirationDate { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExtendedCompositeIdField"/> class.
    /// </summary>
    /// <param name="value">The initial HL7 formatted value.</param>
    /// <param name="isRequired">Whether this field is required.</param>
    public ExtendedCompositeIdField(string? value = null, bool isRequired = false)
        : base(value, isRequired)
    {
        if (!string.IsNullOrEmpty(value))
        {
            ParseFromHL7String(value);
        }
    }

    /// <summary>
    /// Creates an Extended Composite ID field for a patient identifier.
    /// </summary>
    /// <param name="idNumber">The patient ID number.</param>
    /// <param name="assigningAuthority">The assigning authority.</param>
    /// <param name="identifierTypeCode">The identifier type code (e.g., "MR", "PI", "AN").</param>
    /// <param name="isRequired">Whether this field is required.</param>
    /// <returns>A new <see cref="ExtendedCompositeIdField"/> instance.</returns>
    public static ExtendedCompositeIdField CreatePatientId(string idNumber, string? assigningAuthority = null, 
        string identifierTypeCode = "MR", bool isRequired = false)
    {
        var field = new ExtendedCompositeIdField(isRequired: isRequired)
        {
            IdNumber = idNumber,
            AssigningAuthority = assigningAuthority,
            IdentifierTypeCode = identifierTypeCode
        };
        
        field.UpdateRawValue();
        return field;
    }

    /// <summary>
    /// Creates an Extended Composite ID field for an account number.
    /// </summary>
    /// <param name="accountNumber">The account number.</param>
    /// <param name="assigningAuthority">The assigning authority.</param>
    /// <param name="isRequired">Whether this field is required.</param>
    /// <returns>A new <see cref="ExtendedCompositeIdField"/> instance.</returns>
    public static ExtendedCompositeIdField CreateAccountNumber(string accountNumber, string? assigningAuthority = null, 
        bool isRequired = false)
    {
        var field = new ExtendedCompositeIdField(isRequired: isRequired)
        {
            IdNumber = accountNumber,
            AssigningAuthority = assigningAuthority,
            IdentifierTypeCode = "AN"
        };
        
        field.UpdateRawValue();
        return field;
    }

    /// <summary>
    /// Creates an Extended Composite ID field for a visit number.
    /// </summary>
    /// <param name="visitNumber">The visit number.</param>
    /// <param name="assigningAuthority">The assigning authority.</param>
    /// <param name="isRequired">Whether this field is required.</param>
    /// <returns>A new <see cref="ExtendedCompositeIdField"/> instance.</returns>
    public static ExtendedCompositeIdField CreateVisitNumber(string visitNumber, string? assigningAuthority = null, 
        bool isRequired = false)
    {
        var field = new ExtendedCompositeIdField(isRequired: isRequired)
        {
            IdNumber = visitNumber,
            AssigningAuthority = assigningAuthority,
            IdentifierTypeCode = "VN"
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
        
        if (components.Length >= 1) IdNumber = components[0];
        if (components.Length >= 2) CheckDigit = components[1];
        if (components.Length >= 3) CheckDigitScheme = components[2];
        if (components.Length >= 4) AssigningAuthority = components[3];
        if (components.Length >= 5) IdentifierTypeCode = components[4];
        if (components.Length >= 6) AssigningFacility = components[5];
        if (components.Length >= 7) EffectiveDate = components[6];
        if (components.Length >= 8) ExpirationDate = components[7];
    }

    /// <summary>
    /// Updates the raw HL7 value from the component values.
    /// </summary>
    private void UpdateRawValue()
    {
        var components = new[]
        {
            IdNumber ?? "",
            CheckDigit ?? "",
            CheckDigitScheme ?? "",
            AssigningAuthority ?? "",
            IdentifierTypeCode ?? "",
            AssigningFacility ?? "",
            EffectiveDate ?? "",
            ExpirationDate ?? ""
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
        
        // Validate ID number if present
        if (components.Length >= 1 && !string.IsNullOrEmpty(components[0]))
        {
            if (!IdentifierRegex().IsMatch(components[0]))
            {
                throw new ArgumentException($"Invalid ID number format: {components[0]}");
            }
        }

        // Validate identifier type code if present
        if (components.Length >= 5 && !string.IsNullOrEmpty(components[4]))
        {
            var validTypes = new[] { "MR", "PI", "AN", "VN", "SS", "DL", "PPN", "PRN", "MC", "MA" };
            if (!validTypes.Contains(components[4]))
            {
                throw new ArgumentException($"Invalid identifier type code: {components[4]}");
            }
        }

        // Validate check digit scheme if present
        if (components.Length >= 3 && !string.IsNullOrEmpty(components[2]))
        {
            var validSchemes = new[] { "ISO", "M10", "M11", "NPI" };
            if (!validSchemes.Contains(components[2]))
            {
                throw new ArgumentException($"Invalid check digit scheme: {components[2]}");
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
        return new ExtendedCompositeIdField(RawValue, IsRequired);
    }

    /// <summary>
    /// Implicitly converts a string to an ExtendedCompositeIdField.
    /// </summary>
    /// <param name="value">The string value.</param>
    public static implicit operator ExtendedCompositeIdField(string value)
    {
        return new ExtendedCompositeIdField(value);
    }

    /// <summary>
    /// Implicitly converts an ExtendedCompositeIdField to a string.
    /// </summary>
    /// <param name="field">The ExtendedCompositeIdField.</param>
    public static implicit operator string(ExtendedCompositeIdField field)
    {
        return field.RawValue;
    }

    /// <summary>
    /// Gets a formatted display string for this identifier.
    /// </summary>
    /// <returns>A human-readable representation of the identifier.</returns>
    public string ToDisplayString()
    {
        if (string.IsNullOrEmpty(IdNumber))
            return RawValue;

        var display = IdNumber;
        
        if (!string.IsNullOrEmpty(IdentifierTypeCode))
        {
            display = $"{display} ({IdentifierTypeCode})";
        }
        
        if (!string.IsNullOrEmpty(AssigningAuthority))
        {
            display = $"{display} - {AssigningAuthority}";
        }
        
        return display;
    }
}
