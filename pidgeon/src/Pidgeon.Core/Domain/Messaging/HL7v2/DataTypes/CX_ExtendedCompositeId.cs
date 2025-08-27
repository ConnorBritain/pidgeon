// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

namespace Pidgeon.Core.Domain.Messaging.HL7v2.DataTypes;

/// <summary>
/// CX - Extended Composite ID With Check Digit
/// Used throughout HL7 for patient IDs, account numbers, and other identifiers.
/// Components: ID^CheckDigit^CheckDigitScheme^AssigningAuthority^IdentifierTypeCode^AssigningFacility^EffectiveDate^ExpirationDate^AssigningJurisdiction^AssigningAgencyOrDepartment
/// </summary>
public record CX_ExtendedCompositeId
{
    /// <summary>
    /// CX.1 - ID Number (ST)
    /// The actual identifier value.
    /// </summary>
    public string? IdNumber { get; init; }

    /// <summary>
    /// CX.2 - Check Digit (ST)
    /// Check digit used to verify the ID number.
    /// </summary>
    public string? CheckDigit { get; init; }

    /// <summary>
    /// CX.3 - Check Digit Scheme (ID)
    /// Algorithm used to generate the check digit.
    /// Values: MOD10, MOD11, ISO, M10, M11
    /// </summary>
    public string? CheckDigitScheme { get; init; }

    /// <summary>
    /// CX.4 - Assigning Authority (HD)
    /// The organization that assigned the identifier.
    /// </summary>
    public HD_HierarchicDesignator? AssigningAuthority { get; init; }

    /// <summary>
    /// CX.5 - Identifier Type Code (ID)
    /// Type of identifier (MR, SS, DL, etc.)
    /// </summary>
    public string? IdentifierTypeCode { get; init; }

    /// <summary>
    /// CX.6 - Assigning Facility (HD)
    /// The facility that assigned the identifier.
    /// </summary>
    public HD_HierarchicDesignator? AssigningFacility { get; init; }

    /// <summary>
    /// CX.7 - Effective Date (DT)
    /// Date the identifier becomes effective.
    /// </summary>
    public DateTime? EffectiveDate { get; init; }

    /// <summary>
    /// CX.8 - Expiration Date (DT)
    /// Date the identifier expires.
    /// </summary>
    public DateTime? ExpirationDate { get; init; }

    /// <summary>
    /// CX.9 - Assigning Jurisdiction (CWE)
    /// Geographic or organizational jurisdiction.
    /// </summary>
    public CWE_CodedWithExceptions? AssigningJurisdiction { get; init; }

    /// <summary>
    /// CX.10 - Assigning Agency or Department (CWE)
    /// Specific agency or department within jurisdiction.
    /// </summary>
    public CWE_CodedWithExceptions? AssigningAgencyOrDepartment { get; init; }

    /// <summary>
    /// Creates a simple CX with just an ID number and assigning authority.
    /// </summary>
    /// <param name="idNumber">The identifier value</param>
    /// <param name="assigningAuthority">The assigning authority</param>
    /// <param name="identifierTypeCode">The type of identifier (optional)</param>
    /// <returns>A CX instance</returns>
    public static CX_ExtendedCompositeId Create(string idNumber, HD_HierarchicDesignator assigningAuthority, string? identifierTypeCode = null)
    {
        return new CX_ExtendedCompositeId
        {
            IdNumber = idNumber,
            AssigningAuthority = assigningAuthority,
            IdentifierTypeCode = identifierTypeCode
        };
    }

    /// <summary>
    /// Creates a simple CX with just an ID number and assigning authority namespace.
    /// </summary>
    /// <param name="idNumber">The identifier value</param>
    /// <param name="namespaceId">The assigning authority namespace</param>
    /// <param name="identifierTypeCode">The type of identifier (optional)</param>
    /// <returns>A CX instance</returns>
    public static CX_ExtendedCompositeId Create(string idNumber, string namespaceId, string? identifierTypeCode = null)
    {
        return new CX_ExtendedCompositeId
        {
            IdNumber = idNumber,
            AssigningAuthority = HD_HierarchicDesignator.Create(namespaceId),
            IdentifierTypeCode = identifierTypeCode
        };
    }

    /// <summary>
    /// Determines if this CX is effectively empty (no meaningful components).
    /// </summary>
    public bool IsEmpty => string.IsNullOrWhiteSpace(IdNumber);

    /// <summary>
    /// Gets a display representation of this identifier.
    /// </summary>
    public string DisplayValue
    {
        get
        {
            if (IsEmpty) return "Unknown";

            var parts = new List<string>();
            
            if (!string.IsNullOrWhiteSpace(IdNumber))
                parts.Add(IdNumber);

            if (AssigningAuthority != null && !AssigningAuthority.IsEmpty)
                parts.Add($"({AssigningAuthority.DisplayValue})");

            if (!string.IsNullOrWhiteSpace(IdentifierTypeCode))
                parts.Add($"[{IdentifierTypeCode}]");

            return string.Join(" ", parts);
        }
    }

    /// <summary>
    /// Validates the CX structure and business rules.
    /// </summary>
    public Result<CX_ExtendedCompositeId> Validate()
    {
        if (IsEmpty)
            return Error.Validation("CX ID Number is required", nameof(IdNumber));

        // If check digit is provided, check digit scheme should also be provided
        if (!string.IsNullOrWhiteSpace(CheckDigit) && string.IsNullOrWhiteSpace(CheckDigitScheme))
            return Error.Validation("Check digit scheme must be specified when check digit is provided", nameof(CheckDigitScheme));

        // Validate date ranges
        if (EffectiveDate.HasValue && ExpirationDate.HasValue && EffectiveDate > ExpirationDate)
            return Error.Validation("Effective date cannot be after expiration date", nameof(EffectiveDate));

        return Result<CX_ExtendedCompositeId>.Success(this);
    }

    /// <summary>
    /// Common identifier type codes used in healthcare.
    /// </summary>
    public static class IdentifierTypes
    {
        public const string MedicalRecordNumber = "MR";
        public const string AccountNumber = "AN";
        public const string SocialSecurityNumber = "SS";
        public const string DriversLicense = "DL";
        public const string PatientInternalId = "PI";
        public const string PatientExternalId = "PT";
        public const string VisitNumber = "VN";
        public const string EmployeeNumber = "EI";
        public const string NationalProviderIdentifier = "NPI";
        public const string UniversalId = "U";
    }
}