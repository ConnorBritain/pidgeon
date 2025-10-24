using System;
using System.Collections.Generic;

namespace Pidgeon.Core.Domain.Validation;

/// <summary>
/// Vendor-specific or custom validation profile with additional rules beyond base standards.
/// </summary>
public record ValidationProfile
{
    public required string ProfileId { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string BaseStandard { get; init; }  // e.g., "HL7v23", "FHIR-R4"
    public string? Vendor { get; init; }  // e.g., "Epic", "Cerner", "Meditech"
    public required Version Version { get; init; }
    public required List<ValidationRule> Rules { get; init; }
    public Dictionary<string, object> Configuration { get; init; } = new();
}

/// <summary>
/// Collection of validation profiles organized by vendor and standard.
/// </summary>
public record ValidationProfileLibrary
{
    public required Dictionary<string, ValidationProfile> Profiles { get; init; }
    
    public ValidationProfile? GetProfile(string profileId)
        => Profiles.TryGetValue(profileId, out var profile) ? profile : null;
    
    public IEnumerable<ValidationProfile> GetProfilesForStandard(string standard)
        => Profiles.Values.Where(p => p.BaseStandard.Equals(standard, StringComparison.OrdinalIgnoreCase));
    
    public IEnumerable<ValidationProfile> GetProfilesForVendor(string vendor)
        => Profiles.Values.Where(p => p.Vendor?.Equals(vendor, StringComparison.OrdinalIgnoreCase) == true);
}