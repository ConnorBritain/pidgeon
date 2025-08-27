// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

namespace Pidgeon.Core.Domain.Messaging.HL7v2.DataTypes;

/// <summary>
/// CE - Coded Element
/// Used for coded values throughout HL7 messages.
/// Components: Identifier^Text^NameOfCodingSystem^AlternateIdentifier^AlternateText^NameOfAlternateCodingSystem
/// </summary>
public record CE_CodedElement
{
    /// <summary>
    /// CE.1 - Identifier (ST)
    /// The code value.
    /// </summary>
    public string? Identifier { get; init; }

    /// <summary>
    /// CE.2 - Text (ST)
    /// The descriptive text for the code.
    /// </summary>
    public string? Text { get; init; }

    /// <summary>
    /// CE.3 - Name of Coding System (ID)
    /// The coding system that defines this code.
    /// </summary>
    public string? NameOfCodingSystem { get; init; }

    /// <summary>
    /// CE.4 - Alternate Identifier (ST)
    /// An alternative code for the same concept.
    /// </summary>
    public string? AlternateIdentifier { get; init; }

    /// <summary>
    /// CE.5 - Alternate Text (ST)
    /// The descriptive text for the alternate code.
    /// </summary>
    public string? AlternateText { get; init; }

    /// <summary>
    /// CE.6 - Name of Alternate Coding System (ID)
    /// The coding system for the alternate code.
    /// </summary>
    public string? NameOfAlternateCodingSystem { get; init; }

    /// <summary>
    /// Creates a simple CE with code and text.
    /// </summary>
    /// <param name="code">The code value</param>
    /// <param name="text">The descriptive text</param>
    /// <param name="codingSystem">The coding system (optional)</param>
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
    /// Determines if this CE is effectively empty (no meaningful components).
    /// </summary>
    public bool IsEmpty => 
        string.IsNullOrWhiteSpace(Identifier) && 
        string.IsNullOrWhiteSpace(Text) &&
        string.IsNullOrWhiteSpace(AlternateIdentifier);

    /// <summary>
    /// Gets the display value of the coded element.
    /// </summary>
    public string DisplayValue
    {
        get
        {
            if (IsEmpty) return "Unknown";

            var parts = new List<string>();

            // Primary code and text
            if (!string.IsNullOrWhiteSpace(Identifier))
            {
                var primary = Identifier;
                if (!string.IsNullOrWhiteSpace(Text))
                    primary += $" ({Text})";
                if (!string.IsNullOrWhiteSpace(NameOfCodingSystem))
                    primary += $" [{NameOfCodingSystem}]";
                parts.Add(primary);
            }
            else if (!string.IsNullOrWhiteSpace(Text))
            {
                parts.Add(Text);
            }

            // Alternate code and text
            if (!string.IsNullOrWhiteSpace(AlternateIdentifier))
            {
                var alternate = AlternateIdentifier;
                if (!string.IsNullOrWhiteSpace(AlternateText))
                    alternate += $" ({AlternateText})";
                if (!string.IsNullOrWhiteSpace(NameOfAlternateCodingSystem))
                    alternate += $" [{NameOfAlternateCodingSystem}]";
                parts.Add($"Alt: {alternate}");
            }

            return parts.Any() ? string.Join(" | ", parts) : "Unknown";
        }
    }

    /// <summary>
    /// Validates the CE structure.
    /// </summary>
    public Result<CE_CodedElement> Validate()
    {
        if (IsEmpty)
            return Error.Validation("At least identifier or text must be provided", nameof(Identifier));

        return Result<CE_CodedElement>.Success(this);
    }

    /// <summary>
    /// Common coding systems used in healthcare.
    /// </summary>
    public static class CodingSystems
    {
        public const string ICD10CM = "I10";
        public const string ICD9CM = "I9";
        public const string SNOMED = "SNM";
        public const string LOINC = "LN";
        public const string CPT = "CPT";
        public const string HCPCS = "HCPCS";
        public const string NDC = "NDC";
        public const string RXNORM = "RXNORM";
        public const string HL7 = "HL7";
        public const string Local = "L";
    }

    /// <summary>
    /// Common coded elements for quick reference.
    /// </summary>
    public static class Common
    {
        // Gender codes
        public static CE_CodedElement Male => Create("M", "Male", CodingSystems.HL7);
        public static CE_CodedElement Female => Create("F", "Female", CodingSystems.HL7);
        public static CE_CodedElement Other => Create("O", "Other", CodingSystems.HL7);
        public static CE_CodedElement Unknown => Create("U", "Unknown", CodingSystems.HL7);

        // Marital status codes
        public static CE_CodedElement Single => Create("S", "Single", CodingSystems.HL7);
        public static CE_CodedElement Married => Create("M", "Married", CodingSystems.HL7);
        public static CE_CodedElement Divorced => Create("D", "Divorced", CodingSystems.HL7);
        public static CE_CodedElement Widowed => Create("W", "Widowed", CodingSystems.HL7);

        // Race codes (simplified)
        public static CE_CodedElement White => Create("2106-3", "White", "CDCREC");
        public static CE_CodedElement Black => Create("2054-5", "Black or African American", "CDCREC");
        public static CE_CodedElement Asian => Create("2028-9", "Asian", "CDCREC");
        public static CE_CodedElement AmericanIndian => Create("1002-5", "American Indian or Alaska Native", "CDCREC");
        public static CE_CodedElement PacificIslander => Create("2076-8", "Native Hawaiian or Other Pacific Islander", "CDCREC");

        // Language codes
        public static CE_CodedElement English => Create("en", "English", "ISO639");
        public static CE_CodedElement Spanish => Create("es", "Spanish", "ISO639");
        public static CE_CodedElement French => Create("fr", "French", "ISO639");
    }
}