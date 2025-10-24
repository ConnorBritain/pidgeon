// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Pidgeon.Core;

namespace Pidgeon.Core.Domain.Messaging.HL7v2.DataTypes;

/// <summary>
/// CE - Coded Element
/// Represents a coded value with text and coding system information.
/// Components: Identifier^Text^NameOfCodingSystem^AlternateIdentifier^AlternateText^NameOfAlternateCodingSystem
/// 
/// Note: CE is the predecessor to CWE (Coded With Exceptions). CE supports the first 6 components
/// while CWE extends this to 22+ components for more complex coding scenarios.
/// </summary>
public record CE_CodedElement
{
    /// <summary>
    /// CE.1 - Identifier (ST)
    /// The primary code value from the coding system.
    /// </summary>
    public string? Identifier { get; init; }

    /// <summary>
    /// CE.2 - Text (ST)
    /// The descriptive text associated with the code.
    /// </summary>
    public string? Text { get; init; }

    /// <summary>
    /// CE.3 - Name of Coding System (ID)
    /// The coding system that defines the code (e.g., "ICD10", "SNOMED", "LOINC").
    /// </summary>
    public string? NameOfCodingSystem { get; init; }

    /// <summary>
    /// CE.4 - Alternate Identifier (ST)
    /// An alternative code for the same concept from a different coding system.
    /// </summary>
    public string? AlternateIdentifier { get; init; }

    /// <summary>
    /// CE.5 - Alternate Text (ST)
    /// The descriptive text for the alternate code.
    /// </summary>
    public string? AlternateText { get; init; }

    /// <summary>
    /// CE.6 - Name of Alternate Coding System (ID)
    /// The alternate coding system (e.g., if primary is ICD10, alternate might be SNOMED).
    /// </summary>
    public string? NameOfAlternateCodingSystem { get; init; }

    /// <summary>
    /// Creates a simple CE with just a code and text.
    /// </summary>
    /// <param name="code">The code value</param>
    /// <param name="text">The descriptive text (optional)</param>
    /// <param name="codingSystem">The coding system name (optional)</param>
    /// <returns>A CE instance</returns>
    public static CE_CodedElement Create(string code, string? text = null, string? codingSystem = null)
    {
        return new CE_CodedElement
        {
            Identifier = code,
            Text = text,
            NameOfCodingSystem = codingSystem
        };
    }

    /// <summary>
    /// Creates a CE with both primary and alternate coding.
    /// </summary>
    /// <param name="primaryCode">The primary code</param>
    /// <param name="primaryText">The primary text</param>
    /// <param name="primarySystem">The primary coding system</param>
    /// <param name="alternateCode">The alternate code</param>
    /// <param name="alternateText">The alternate text</param>
    /// <param name="alternateSystem">The alternate coding system</param>
    /// <returns>A CE instance</returns>
    public static CE_CodedElement CreateWithAlternate(
        string primaryCode, string? primaryText, string? primarySystem,
        string alternateCode, string? alternateText, string? alternateSystem)
    {
        return new CE_CodedElement
        {
            Identifier = primaryCode,
            Text = primaryText,
            NameOfCodingSystem = primarySystem,
            AlternateIdentifier = alternateCode,
            AlternateText = alternateText,
            NameOfAlternateCodingSystem = alternateSystem
        };
    }

    /// <summary>
    /// Parses an HL7 CE string into a CE_CodedElement instance.
    /// </summary>
    /// <param name="hl7Value">The HL7 CE string (components separated by ^)</param>
    /// <returns>A Result containing the parsed CE or an error</returns>
    public static Result<CE_CodedElement> FromHL7String(string hl7Value)
    {
        if (string.IsNullOrWhiteSpace(hl7Value))
            return Result<CE_CodedElement>.Success(new CE_CodedElement());

        try
        {
            var components = hl7Value.Split('^');
            
            // Ensure we don't go out of bounds when accessing components
            string? GetComponent(int index) => 
                index < components.Length && !string.IsNullOrWhiteSpace(components[index]) 
                    ? components[index].Trim() 
                    : null;

            var ce = new CE_CodedElement
            {
                Identifier = GetComponent(0),
                Text = GetComponent(1),
                NameOfCodingSystem = GetComponent(2),
                AlternateIdentifier = GetComponent(3),
                AlternateText = GetComponent(4),
                NameOfAlternateCodingSystem = GetComponent(5)
            };

            return Result<CE_CodedElement>.Success(ce);
        }
        catch (Exception ex)
        {
            return Result<CE_CodedElement>.Failure($"Failed to parse CE from HL7 string '{hl7Value}': {ex.Message}");
        }
    }

    /// <summary>
    /// Converts this CE to an HL7 string representation.
    /// </summary>
    /// <returns>The HL7 string with components separated by ^</returns>
    public string ToHL7String()
    {
        // Build components array, always including at least the first component
        var components = new string[]
        {
            Identifier ?? "",
            Text ?? "",
            NameOfCodingSystem ?? "",
            AlternateIdentifier ?? "",
            AlternateText ?? "",
            NameOfAlternateCodingSystem ?? ""
        };

        // Find the last non-empty component to avoid trailing separators
        int lastIndex = -1;
        for (int i = components.Length - 1; i >= 0; i--)
        {
            if (!string.IsNullOrEmpty(components[i]))
            {
                lastIndex = i;
                break;
            }
        }

        // If all components are empty, return empty string
        if (lastIndex == -1)
            return "";

        // Return components up to the last non-empty one
        return string.Join("^", components.Take(lastIndex + 1));
    }

    /// <summary>
    /// Gets a display-friendly representation of the coded element.
    /// </summary>
    /// <returns>A human-readable string</returns>
    public string GetDisplayValue()
    {
        if (!string.IsNullOrEmpty(Text) && !string.IsNullOrEmpty(Identifier))
            return $"{Text} ({Identifier})";
        
        if (!string.IsNullOrEmpty(Text))
            return Text;
            
        if (!string.IsNullOrEmpty(Identifier))
            return Identifier;
            
        return "[Empty CE]";
    }

    /// <summary>
    /// Determines if this coded element is empty (no meaningful content).
    /// </summary>
    /// <returns>True if all components are null or empty</returns>
    public bool IsEmpty()
    {
        return string.IsNullOrWhiteSpace(Identifier) &&
               string.IsNullOrWhiteSpace(Text) &&
               string.IsNullOrWhiteSpace(NameOfCodingSystem) &&
               string.IsNullOrWhiteSpace(AlternateIdentifier) &&
               string.IsNullOrWhiteSpace(AlternateText) &&
               string.IsNullOrWhiteSpace(NameOfAlternateCodingSystem);
    }

    /// <summary>
    /// Validates that the coded element has sufficient content for healthcare use.
    /// </summary>
    /// <returns>A validation result</returns>
    public Result<bool> Validate()
    {
        if (IsEmpty())
            return Result<bool>.Success(true); // Empty CE is valid (optional fields)

        // If there's an identifier, there should ideally be a coding system
        if (!string.IsNullOrWhiteSpace(Identifier) && string.IsNullOrWhiteSpace(NameOfCodingSystem))
        {
            return Result<bool>.Failure($"Code '{Identifier}' should specify a coding system for proper interpretation.");
        }

        // If there's alternate coding, both code and system should be present
        if (!string.IsNullOrWhiteSpace(AlternateIdentifier) && string.IsNullOrWhiteSpace(NameOfAlternateCodingSystem))
        {
            return Result<bool>.Failure($"Alternate code '{AlternateIdentifier}' should specify an alternate coding system.");
        }

        return Result<bool>.Success(true);
    }

    /// <summary>
    /// Common healthcare coding system constants.
    /// </summary>
    public static class CodingSystems
    {
        public const string ICD10_CM = "ICD10CM";
        public const string ICD10_PCS = "ICD10PCS";
        public const string SNOMED_CT = "SCT";
        public const string LOINC = "LN";
        public const string CPT4 = "CPT4";
        public const string HCPCS = "HCPCS";
        public const string RXNORM = "RXNORM";
        public const string NDC = "NDC";
        public const string LOCAL = "L";
    }
}