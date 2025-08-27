// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Pidgeon.Core.Domain.Clinical.Entities;

namespace Pidgeon.Core.Domain.Messaging.HL7v2.DataTypes;

/// <summary>
/// XPN - Extended Person Name
/// Used throughout HL7 for patient names, provider names, and other person names.
/// Components: FamilyName^GivenName^SecondGivenName^Suffix^Prefix^Degree^NameTypeCode^NameRepresentationCode^NameContext^NameValidityRange^NameAssemblyOrder^EffectiveDate^ExpirationDate^ProfessionalSuffix
/// </summary>
public record XPN_ExtendedPersonName
{
    /// <summary>
    /// XPN.1 - Family Name (FN)
    /// The person's surname/family name. Can be complex (Own Surname^Own Surname Prefix^Partner Surname^Partner Surname Prefix^Surname From Partner/Spouse).
    /// </summary>
    public FN_FamilyName? FamilyName { get; init; }

    /// <summary>
    /// XPN.2 - Given Name (ST)
    /// The person's first name.
    /// </summary>
    public string? GivenName { get; init; }

    /// <summary>
    /// XPN.3 - Second and Further Given Names or Initials Thereof (ST)
    /// Middle name(s) or initials.
    /// </summary>
    public string? SecondGivenName { get; init; }

    /// <summary>
    /// XPN.4 - Suffix (e.g., JR or III) (ST)
    /// Name suffix like Jr., Sr., III, etc.
    /// </summary>
    public string? Suffix { get; init; }

    /// <summary>
    /// XPN.5 - Prefix (e.g., DR) (ST)
    /// Name prefix like Dr., Mr., Ms., etc.
    /// </summary>
    public string? Prefix { get; init; }

    /// <summary>
    /// XPN.6 - Degree (e.g., MD) (IS)
    /// Academic or professional degree like MD, PhD, RN, etc.
    /// </summary>
    public string? Degree { get; init; }

    /// <summary>
    /// XPN.7 - Name Type Code (ID)
    /// Type of name: L=Legal, D=Display, M=Maiden, C=Adopted, etc.
    /// </summary>
    public string? NameTypeCode { get; init; }

    /// <summary>
    /// XPN.8 - Name Representation Code (ID)
    /// How the name is represented: A=Alphabetic, I=Ideographic, P=Phonetic
    /// </summary>
    public string? NameRepresentationCode { get; init; }

    /// <summary>
    /// XPN.9 - Name Context (CE)
    /// Context or use of the name.
    /// </summary>
    public CE_CodedElement? NameContext { get; init; }

    /// <summary>
    /// XPN.10 - Name Validity Range (DR)
    /// Date range when this name is valid.
    /// </summary>
    public DR_DateRange? NameValidityRange { get; init; }

    /// <summary>
    /// XPN.11 - Name Assembly Order (ID)
    /// Order to assemble name components: G=Given Family, F=Family Given
    /// </summary>
    public string? NameAssemblyOrder { get; init; }

    /// <summary>
    /// XPN.12 - Effective Date (TS)
    /// Date when this name becomes effective.
    /// </summary>
    public DateTime? EffectiveDate { get; init; }

    /// <summary>
    /// XPN.13 - Expiration Date (TS)
    /// Date when this name expires.
    /// </summary>
    public DateTime? ExpirationDate { get; init; }

    /// <summary>
    /// XPN.14 - Professional Suffix (ST)
    /// Professional suffix like FACS, FACP, etc.
    /// </summary>
    public string? ProfessionalSuffix { get; init; }

    /// <summary>
    /// Creates an XPN from a simple PersonName.
    /// </summary>
    /// <param name="personName">The universal person name</param>
    /// <param name="nameTypeCode">The type of name (optional, defaults to Legal)</param>
    /// <returns>An XPN instance</returns>
    public static XPN_ExtendedPersonName FromPersonName(PersonName personName, string? nameTypeCode = NameTypes.Legal)
    {
        return new XPN_ExtendedPersonName
        {
            FamilyName = !string.IsNullOrWhiteSpace(personName.Family) ? FN_FamilyName.Create(personName.Family) : null,
            GivenName = personName.Given,
            SecondGivenName = personName.Middle,
            Prefix = personName.Prefix,
            Suffix = personName.Suffix,
            NameTypeCode = nameTypeCode,
            NameRepresentationCode = NameRepresentations.Alphabetic
        };
    }

    /// <summary>
    /// Converts this XPN to a universal PersonName.
    /// </summary>
    /// <returns>A PersonName instance</returns>
    public PersonName ToPersonName()
    {
        return new PersonName
        {
            Family = FamilyName?.OwnSurname,
            Given = GivenName,
            Middle = SecondGivenName,
            Prefix = Prefix,
            Suffix = Suffix
        };
    }

    /// <summary>
    /// Creates a simple XPN with basic name components.
    /// </summary>
    /// <param name="family">Family name</param>
    /// <param name="given">Given name</param>
    /// <param name="middle">Middle name (optional)</param>
    /// <param name="nameTypeCode">Name type (optional)</param>
    /// <returns>An XPN instance</returns>
    public static XPN_ExtendedPersonName Create(string family, string given, string? middle = null, string? nameTypeCode = NameTypes.Legal)
    {
        return new XPN_ExtendedPersonName
        {
            FamilyName = FN_FamilyName.Create(family),
            GivenName = given,
            SecondGivenName = middle,
            NameTypeCode = nameTypeCode,
            NameRepresentationCode = NameRepresentations.Alphabetic
        };
    }

    /// <summary>
    /// Determines if this XPN is effectively empty (no meaningful components).
    /// </summary>
    public bool IsEmpty => 
        (FamilyName == null || FamilyName.IsEmpty) && 
        string.IsNullOrWhiteSpace(GivenName) && 
        string.IsNullOrWhiteSpace(SecondGivenName);

    /// <summary>
    /// Gets the full display name in "Family, Given Middle" format.
    /// </summary>
    public string DisplayName
    {
        get
        {
            if (IsEmpty) return "Unknown";

            var parts = new List<string>();
            
            // Add prefix if present
            if (!string.IsNullOrWhiteSpace(Prefix))
                parts.Add(Prefix);

            // Add given names
            var givenParts = new List<string>();
            if (!string.IsNullOrWhiteSpace(GivenName))
                givenParts.Add(GivenName);
            if (!string.IsNullOrWhiteSpace(SecondGivenName))
                givenParts.Add(SecondGivenName);

            if (givenParts.Any())
                parts.Add(string.Join(" ", givenParts));

            // Add family name
            if (FamilyName != null && !FamilyName.IsEmpty)
                parts.Add(FamilyName.DisplayValue);

            // Add suffix if present
            if (!string.IsNullOrWhiteSpace(Suffix))
                parts.Add(Suffix);

            // Add degree if present
            if (!string.IsNullOrWhiteSpace(Degree))
                parts.Add(Degree);

            // Add professional suffix if present
            if (!string.IsNullOrWhiteSpace(ProfessionalSuffix))
                parts.Add(ProfessionalSuffix);

            return string.Join(" ", parts);
        }
    }

    /// <summary>
    /// Gets the name in "Last, First Middle" format (common for healthcare).
    /// </summary>
    public string LastFirstFormat
    {
        get
        {
            if (IsEmpty) return "Unknown";

            var parts = new List<string>();
            
            // Family name first
            if (FamilyName != null && !FamilyName.IsEmpty)
                parts.Add(FamilyName.DisplayValue);

            // Then given names
            var givenParts = new List<string>();
            if (!string.IsNullOrWhiteSpace(GivenName))
                givenParts.Add(GivenName);
            if (!string.IsNullOrWhiteSpace(SecondGivenName))
                givenParts.Add(SecondGivenName);

            if (givenParts.Any())
                parts.Add(string.Join(" ", givenParts));

            var result = parts.Any() ? string.Join(", ", parts) : "Unknown";

            // Add suffix if present
            if (!string.IsNullOrWhiteSpace(Suffix))
                result += $" {Suffix}";

            return result;
        }
    }

    /// <summary>
    /// Validates the XPN structure and business rules.
    /// </summary>
    public Result<XPN_ExtendedPersonName> Validate()
    {
        if (IsEmpty)
            return Error.Validation("At least family name or given name must be provided", nameof(GivenName));

        // Validate date ranges
        if (EffectiveDate.HasValue && ExpirationDate.HasValue && EffectiveDate > ExpirationDate)
            return Error.Validation("Effective date cannot be after expiration date", nameof(EffectiveDate));

        // Validate name validity range
        if (NameValidityRange != null)
        {
            var rangeValidation = NameValidityRange.Validate();
            if (!rangeValidation.IsSuccess)
                return Error.Validation($"Name validity range is invalid: {rangeValidation.Error?.Message}", nameof(NameValidityRange));
        }

        return Result<XPN_ExtendedPersonName>.Success(this);
    }

    /// <summary>
    /// Common name type codes.
    /// </summary>
    public static class NameTypes
    {
        public const string Legal = "L";
        public const string Display = "D";
        public const string Maiden = "M";
        public const string Nickname = "N";
        public const string Alias = "A";
        public const string Adopted = "C";
        public const string Birth = "B";
        public const string Temporary = "T";
    }

    /// <summary>
    /// Name representation codes.
    /// </summary>
    public static class NameRepresentations
    {
        public const string Alphabetic = "A";
        public const string Ideographic = "I";
        public const string Phonetic = "P";
    }

    /// <summary>
    /// Name assembly order codes.
    /// </summary>
    public static class AssemblyOrders
    {
        public const string GivenFamily = "G";
        public const string FamilyGiven = "F";
    }
}