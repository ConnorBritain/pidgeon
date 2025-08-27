// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

namespace Pidgeon.Core.Domain.Messaging.HL7v2.DataTypes;

/// <summary>
/// CWE - Coded With Exceptions
/// Enhanced version of CE that supports original text and additional coding systems.
/// Components: Identifier^Text^NameOfCodingSystem^AlternateIdentifier^AlternateText^NameOfAlternateCodingSystem^CodingSystemVersionID^AlternateCodingSystemVersionID^OriginalText^SecondAlternateIdentifier^SecondAlternateText^NameOfSecondAlternateCodingSystem^SecondAlternateCodingSystemVersionID^CodingSystemOID^ValueSetOID^ValueSetVersionID^AlternateCodingSystemOID^AlternateValueSetOID^AlternateValueSetVersionID^SecondAlternateCodingSystemOID^SecondAlternateValueSetOID^SecondAlternateValueSetVersionID
/// </summary>
public record CWE_CodedWithExceptions
{
    /// <summary>
    /// CWE.1 - Identifier (ST)
    /// The primary code value.
    /// </summary>
    public string? Identifier { get; init; }

    /// <summary>
    /// CWE.2 - Text (ST)
    /// The descriptive text for the primary code.
    /// </summary>
    public string? Text { get; init; }

    /// <summary>
    /// CWE.3 - Name of Coding System (ID)
    /// The coding system that defines the primary code.
    /// </summary>
    public string? NameOfCodingSystem { get; init; }

    /// <summary>
    /// CWE.4 - Alternate Identifier (ST)
    /// An alternative code for the same concept.
    /// </summary>
    public string? AlternateIdentifier { get; init; }

    /// <summary>
    /// CWE.5 - Alternate Text (ST)
    /// The descriptive text for the alternate code.
    /// </summary>
    public string? AlternateText { get; init; }

    /// <summary>
    /// CWE.6 - Name of Alternate Coding System (ID)
    /// The coding system for the alternate code.
    /// </summary>
    public string? NameOfAlternateCodingSystem { get; init; }

    /// <summary>
    /// CWE.7 - Coding System Version ID (ST)
    /// Version of the primary coding system.
    /// </summary>
    public string? CodingSystemVersionId { get; init; }

    /// <summary>
    /// CWE.8 - Alternate Coding System Version ID (ST)
    /// Version of the alternate coding system.
    /// </summary>
    public string? AlternateCodingSystemVersionId { get; init; }

    /// <summary>
    /// CWE.9 - Original Text (ST)
    /// The original text as entered by the user.
    /// </summary>
    public string? OriginalText { get; init; }

    /// <summary>
    /// CWE.10 - Second Alternate Identifier (ST)
    /// A second alternative code.
    /// </summary>
    public string? SecondAlternateIdentifier { get; init; }

    /// <summary>
    /// CWE.11 - Second Alternate Text (ST)
    /// Text for the second alternate code.
    /// </summary>
    public string? SecondAlternateText { get; init; }

    /// <summary>
    /// CWE.12 - Name of Second Alternate Coding System (ID)
    /// The coding system for the second alternate code.
    /// </summary>
    public string? NameOfSecondAlternateCodingSystem { get; init; }

    /// <summary>
    /// CWE.13 - Second Alternate Coding System Version ID (ST)
    /// Version of the second alternate coding system.
    /// </summary>
    public string? SecondAlternateCodingSystemVersionId { get; init; }

    /// <summary>
    /// CWE.14 - Coding System OID (ST)
    /// Object identifier for the primary coding system.
    /// </summary>
    public string? CodingSystemOID { get; init; }

    /// <summary>
    /// CWE.15 - Value Set OID (ST)
    /// Object identifier for the value set.
    /// </summary>
    public string? ValueSetOID { get; init; }

    /// <summary>
    /// CWE.16 - Value Set Version ID (DTM)
    /// Version identifier for the value set.
    /// </summary>
    public DateTime? ValueSetVersionId { get; init; }

    /// <summary>
    /// CWE.17 - Alternate Coding System OID (ST)
    /// Object identifier for the alternate coding system.
    /// </summary>
    public string? AlternateCodingSystemOID { get; init; }

    /// <summary>
    /// CWE.18 - Alternate Value Set OID (ST)
    /// Object identifier for the alternate value set.
    /// </summary>
    public string? AlternateValueSetOID { get; init; }

    /// <summary>
    /// CWE.19 - Alternate Value Set Version ID (DTM)
    /// Version identifier for the alternate value set.
    /// </summary>
    public DateTime? AlternateValueSetVersionId { get; init; }

    /// <summary>
    /// CWE.20 - Second Alternate Coding System OID (ST)
    /// Object identifier for the second alternate coding system.
    /// </summary>
    public string? SecondAlternateCodingSystemOID { get; init; }

    /// <summary>
    /// CWE.21 - Second Alternate Value Set OID (ST)
    /// Object identifier for the second alternate value set.
    /// </summary>
    public string? SecondAlternateValueSetOID { get; init; }

    /// <summary>
    /// CWE.22 - Second Alternate Value Set Version ID (DTM)
    /// Version identifier for the second alternate value set.
    /// </summary>
    public DateTime? SecondAlternateValueSetVersionId { get; init; }

    /// <summary>
    /// Creates a simple CWE with code and text.
    /// </summary>
    /// <param name="code">The code value</param>
    /// <param name="text">The descriptive text</param>
    /// <param name="codingSystem">The coding system (optional)</param>
    /// <param name="originalText">The original text (optional)</param>
    /// <returns>A CWE instance</returns>
    public static CWE_CodedWithExceptions Create(string code, string? text = null, string? codingSystem = null, string? originalText = null)
    {
        return new CWE_CodedWithExceptions
        {
            Identifier = code,
            Text = text,
            NameOfCodingSystem = codingSystem,
            OriginalText = originalText
        };
    }

    /// <summary>
    /// Creates a CWE from a CE (Coded Element).
    /// </summary>
    /// <param name="ce">The CE to convert</param>
    /// <returns>A CWE instance</returns>
    public static CWE_CodedWithExceptions FromCE(CE_CodedElement ce)
    {
        return new CWE_CodedWithExceptions
        {
            Identifier = ce.Identifier,
            Text = ce.Text,
            NameOfCodingSystem = ce.NameOfCodingSystem,
            AlternateIdentifier = ce.AlternateIdentifier,
            AlternateText = ce.AlternateText,
            NameOfAlternateCodingSystem = ce.NameOfAlternateCodingSystem
        };
    }

    /// <summary>
    /// Converts this CWE to a simpler CE (loses some information).
    /// </summary>
    /// <returns>A CE instance</returns>
    public CE_CodedElement ToCE()
    {
        return new CE_CodedElement
        {
            Identifier = Identifier,
            Text = Text,
            NameOfCodingSystem = NameOfCodingSystem,
            AlternateIdentifier = AlternateIdentifier,
            AlternateText = AlternateText,
            NameOfAlternateCodingSystem = NameOfAlternateCodingSystem
        };
    }

    /// <summary>
    /// Determines if this CWE is effectively empty (no meaningful components).
    /// </summary>
    public bool IsEmpty => 
        string.IsNullOrWhiteSpace(Identifier) && 
        string.IsNullOrWhiteSpace(Text) &&
        string.IsNullOrWhiteSpace(AlternateIdentifier) &&
        string.IsNullOrWhiteSpace(OriginalText);

    /// <summary>
    /// Gets the display value of the coded element with exceptions.
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

            // Original text if different from coded text
            if (!string.IsNullOrWhiteSpace(OriginalText) && !string.Equals(OriginalText, Text, StringComparison.OrdinalIgnoreCase))
            {
                parts.Add($"Original: \"{OriginalText}\"");
            }

            // Alternate codes
            if (!string.IsNullOrWhiteSpace(AlternateIdentifier))
            {
                var alternate = AlternateIdentifier;
                if (!string.IsNullOrWhiteSpace(AlternateText))
                    alternate += $" ({AlternateText})";
                if (!string.IsNullOrWhiteSpace(NameOfAlternateCodingSystem))
                    alternate += $" [{NameOfAlternateCodingSystem}]";
                parts.Add($"Alt: {alternate}");
            }

            if (!string.IsNullOrWhiteSpace(SecondAlternateIdentifier))
            {
                var secondAlt = SecondAlternateIdentifier;
                if (!string.IsNullOrWhiteSpace(SecondAlternateText))
                    secondAlt += $" ({SecondAlternateText})";
                if (!string.IsNullOrWhiteSpace(NameOfSecondAlternateCodingSystem))
                    secondAlt += $" [{NameOfSecondAlternateCodingSystem}]";
                parts.Add($"Alt2: {secondAlt}");
            }

            return parts.Any() ? string.Join(" | ", parts) : "Unknown";
        }
    }

    /// <summary>
    /// Validates the CWE structure.
    /// </summary>
    public Result<CWE_CodedWithExceptions> Validate()
    {
        if (IsEmpty)
            return Error.Validation("At least identifier, text, or original text must be provided", nameof(Identifier));

        return Result<CWE_CodedWithExceptions>.Success(this);
    }
}