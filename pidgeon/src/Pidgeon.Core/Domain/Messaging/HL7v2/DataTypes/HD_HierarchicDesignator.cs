// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

namespace Pidgeon.Core.Domain.Messaging.HL7v2.DataTypes;

/// <summary>
/// HD - Hierarchic Designator
/// Used to identify an organization, system, or authority that assigns identifiers.
/// Components: NamespaceID^UniversalID^UniversalIDType
/// </summary>
public record HD_HierarchicDesignator
{
    /// <summary>
    /// HD.1 - Namespace ID (IS)
    /// A user-defined string that identifies the namespace.
    /// </summary>
    public string? NamespaceId { get; init; }

    /// <summary>
    /// HD.2 - Universal ID (ST)
    /// A globally unique identifier for the namespace.
    /// </summary>
    public string? UniversalId { get; init; }

    /// <summary>
    /// HD.3 - Universal ID Type (ID)
    /// The type of universal ID (DNS, GUID, HCD, HL7, ISO, L, M, N, Random, UUID, x400, x500)
    /// </summary>
    public string? UniversalIdType { get; init; }

    /// <summary>
    /// Creates a simple HD with just a namespace ID.
    /// </summary>
    /// <param name="namespaceId">The namespace identifier</param>
    /// <returns>An HD instance</returns>
    public static HD_HierarchicDesignator Create(string namespaceId)
    {
        return new HD_HierarchicDesignator { NamespaceId = namespaceId };
    }

    /// <summary>
    /// Creates an HD with namespace ID and universal ID.
    /// </summary>
    /// <param name="namespaceId">The namespace identifier</param>
    /// <param name="universalId">The universal identifier</param>
    /// <param name="universalIdType">The type of universal ID</param>
    /// <returns>An HD instance</returns>
    public static HD_HierarchicDesignator Create(string namespaceId, string universalId, string universalIdType)
    {
        return new HD_HierarchicDesignator 
        { 
            NamespaceId = namespaceId,
            UniversalId = universalId,
            UniversalIdType = universalIdType
        };
    }

    /// <summary>
    /// Creates an HD with just a universal ID (typically for well-known authorities).
    /// </summary>
    /// <param name="universalId">The universal identifier</param>
    /// <param name="universalIdType">The type of universal ID</param>
    /// <returns>An HD instance</returns>
    public static HD_HierarchicDesignator CreateUniversal(string universalId, string universalIdType)
    {
        return new HD_HierarchicDesignator 
        { 
            UniversalId = universalId,
            UniversalIdType = universalIdType
        };
    }

    /// <summary>
    /// Determines if this HD is effectively empty (no meaningful components).
    /// </summary>
    public bool IsEmpty => 
        string.IsNullOrWhiteSpace(NamespaceId) && 
        string.IsNullOrWhiteSpace(UniversalId);

    /// <summary>
    /// Gets a display representation of this hierarchic designator.
    /// </summary>
    public string DisplayValue
    {
        get
        {
            if (IsEmpty) return "Unknown";

            var parts = new List<string>();

            if (!string.IsNullOrWhiteSpace(NamespaceId))
                parts.Add(NamespaceId);

            if (!string.IsNullOrWhiteSpace(UniversalId))
            {
                var universalPart = UniversalId;
                if (!string.IsNullOrWhiteSpace(UniversalIdType))
                    universalPart += $"({UniversalIdType})";
                parts.Add(universalPart);
            }

            return string.Join(" | ", parts);
        }
    }

    /// <summary>
    /// Validates the HD structure and business rules.
    /// </summary>
    public Result<HD_HierarchicDesignator> Validate()
    {
        if (IsEmpty)
            return Error.Validation("Either NamespaceID or UniversalID must be provided", nameof(NamespaceId));

        // If UniversalID is provided, UniversalIDType should also be provided
        if (!string.IsNullOrWhiteSpace(UniversalId) && string.IsNullOrWhiteSpace(UniversalIdType))
            return Error.Validation("Universal ID Type must be specified when Universal ID is provided", nameof(UniversalIdType));

        // Validate Universal ID Type if provided
        if (!string.IsNullOrWhiteSpace(UniversalIdType) && !UniversalIdTypes.IsValid(UniversalIdType))
            return Error.Validation($"Invalid Universal ID Type: {UniversalIdType}", nameof(UniversalIdType));

        return Result<HD_HierarchicDesignator>.Success(this);
    }

    /// <summary>
    /// Common Universal ID types used in healthcare.
    /// </summary>
    public static class UniversalIdTypes
    {
        /// <summary>
        /// DNS (Domain Name System) - Internet domain name
        /// </summary>
        public const string DNS = "DNS";

        /// <summary>
        /// GUID (Globally Unique Identifier) - 128-bit number
        /// </summary>
        public const string GUID = "GUID";

        /// <summary>
        /// HCD (CEN Healthcare Coding Scheme Designator) - Healthcare coding scheme
        /// </summary>
        public const string HCD = "HCD";

        /// <summary>
        /// HL7 - Reserved for HL7 registration schemes
        /// </summary>
        public const string HL7 = "HL7";

        /// <summary>
        /// ISO - ISO Object Identifier
        /// </summary>
        public const string ISO = "ISO";

        /// <summary>
        /// L - Local - defined by the local system
        /// </summary>
        public const string Local = "L";

        /// <summary>
        /// M - M - Object identifier
        /// </summary>
        public const string M = "M";

        /// <summary>
        /// N - N - These are reserved
        /// </summary>
        public const string N = "N";

        /// <summary>
        /// Random - Usually a base64 encoded string of random bits
        /// </summary>
        public const string Random = "Random";

        /// <summary>
        /// UUID - Universal Unique Identifier
        /// </summary>
        public const string UUID = "UUID";

        /// <summary>
        /// x400 - X.400 MHS identifier
        /// </summary>
        public const string X400 = "x400";

        /// <summary>
        /// x500 - X.500 directory name
        /// </summary>
        public const string X500 = "x500";

        private static readonly HashSet<string> ValidTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            DNS, GUID, HCD, HL7, ISO, Local, M, N, Random, UUID, X400, X500
        };

        /// <summary>
        /// Checks if the given Universal ID type is valid.
        /// </summary>
        /// <param name="type">The Universal ID type to validate</param>
        /// <returns>True if valid, false otherwise</returns>
        public static bool IsValid(string type) => ValidTypes.Contains(type);
    }

    /// <summary>
    /// Common healthcare assigning authorities.
    /// </summary>
    public static class CommonAuthorities
    {
        /// <summary>
        /// Social Security Administration
        /// </summary>
        public static HD_HierarchicDesignator SSA => Create("SSA");

        /// <summary>
        /// Centers for Medicare & Medicaid Services (National Provider Identifier)
        /// </summary>
        public static HD_HierarchicDesignator CMS_NPI => CreateUniversal("2.16.840.1.113883.4.6", "ISO");

        /// <summary>
        /// Drug Enforcement Administration
        /// </summary>
        public static HD_HierarchicDesignator DEA => Create("DEA");

        /// <summary>
        /// State driver's license authority (generic)
        /// </summary>
        public static HD_HierarchicDesignator StateDL => Create("DL");

        /// <summary>
        /// Local facility medical record number authority
        /// </summary>
        public static HD_HierarchicDesignator LocalMRN(string facilityCode) => Create(facilityCode);

        /// <summary>
        /// HL7 OID for patient identifiers
        /// </summary>
        public static HD_HierarchicDesignator HL7_PatientId => CreateUniversal("2.16.840.1.113883.19.5", "ISO");
    }
}