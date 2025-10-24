// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

namespace Pidgeon.Core.Domain.Messaging.HL7v2.DataTypes;

/// <summary>
/// FN - Family Name
/// Used within XPN to represent complex family name structures.
/// Components: OwnSurname^OwnSurnamePrefix^PartnerSurname^PartnerSurnamePrefix^SurnameFromPartner
/// </summary>
public record FN_FamilyName
{
    /// <summary>
    /// FN.1 - Own Surname (ST)
    /// The person's own surname.
    /// </summary>
    public string? OwnSurname { get; init; }

    /// <summary>
    /// FN.2 - Own Surname Prefix (ST)
    /// Prefix to the person's own surname (van, de, etc.).
    /// </summary>
    public string? OwnSurnamePrefix { get; init; }

    /// <summary>
    /// FN.3 - Partner Surname (ST)
    /// The partner's surname if used.
    /// </summary>
    public string? PartnerSurname { get; init; }

    /// <summary>
    /// FN.4 - Partner Surname Prefix (ST)
    /// Prefix to the partner's surname.
    /// </summary>
    public string? PartnerSurnamePrefix { get; init; }

    /// <summary>
    /// FN.5 - Surname From Partner/Spouse (ST)
    /// Surname derived from partner or spouse.
    /// </summary>
    public string? SurnameFromPartner { get; init; }

    /// <summary>
    /// Creates a simple FN with just the own surname.
    /// </summary>
    /// <param name="surname">The person's surname</param>
    /// <returns>An FN instance</returns>
    public static FN_FamilyName Create(string surname)
    {
        return new FN_FamilyName { OwnSurname = surname };
    }

    /// <summary>
    /// Creates an FN with own surname and prefix.
    /// </summary>
    /// <param name="surname">The person's surname</param>
    /// <param name="prefix">The surname prefix</param>
    /// <returns>An FN instance</returns>
    public static FN_FamilyName Create(string surname, string prefix)
    {
        return new FN_FamilyName 
        { 
            OwnSurname = surname,
            OwnSurnamePrefix = prefix
        };
    }

    /// <summary>
    /// Determines if this FN is effectively empty (no meaningful components).
    /// </summary>
    public bool IsEmpty => 
        string.IsNullOrWhiteSpace(OwnSurname) && 
        string.IsNullOrWhiteSpace(PartnerSurname) &&
        string.IsNullOrWhiteSpace(SurnameFromPartner);

    /// <summary>
    /// Gets the display value of the family name.
    /// </summary>
    public string DisplayValue
    {
        get
        {
            if (IsEmpty) return "Unknown";

            var parts = new List<string>();

            // Add own surname with prefix
            var ownName = new List<string>();
            if (!string.IsNullOrWhiteSpace(OwnSurnamePrefix))
                ownName.Add(OwnSurnamePrefix);
            if (!string.IsNullOrWhiteSpace(OwnSurname))
                ownName.Add(OwnSurname);
            
            if (ownName.Any())
                parts.Add(string.Join(" ", ownName));

            // Add partner surname with prefix
            var partnerName = new List<string>();
            if (!string.IsNullOrWhiteSpace(PartnerSurnamePrefix))
                partnerName.Add(PartnerSurnamePrefix);
            if (!string.IsNullOrWhiteSpace(PartnerSurname))
                partnerName.Add(PartnerSurname);
            
            if (partnerName.Any())
                parts.Add(string.Join(" ", partnerName));

            // Add surname from partner
            if (!string.IsNullOrWhiteSpace(SurnameFromPartner))
                parts.Add(SurnameFromPartner);

            return parts.Any() ? string.Join("-", parts) : "Unknown";
        }
    }

    /// <summary>
    /// Validates the FN structure.
    /// </summary>
    public Result<FN_FamilyName> Validate()
    {
        if (IsEmpty)
            return Error.Validation("At least one surname component must be provided", nameof(OwnSurname));

        return Result<FN_FamilyName>.Success(this);
    }
}