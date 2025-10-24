// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System.Text.Json.Serialization;

namespace Pidgeon.Core.Application.DTOs.Data;

/// <summary>
/// Patient demographic data from free tier datasets.
/// Standard-agnostic format for use across HL7, FHIR, and NCPDP.
/// </summary>
public record DemographicData
{
    [JsonPropertyName("firstNames")]
    public required FirstNameCollection FirstNames { get; init; }

    [JsonPropertyName("lastNames")]
    public required IReadOnlyList<string> LastNames { get; init; }

    [JsonPropertyName("_metadata")]
    public DemographicMetadata? Metadata { get; init; }
}

public record FirstNameCollection
{
    [JsonPropertyName("male")]
    public required IReadOnlyList<string> Male { get; init; }

    [JsonPropertyName("female")]
    public required IReadOnlyList<string> Female { get; init; }
}

public record DemographicMetadata
{
    [JsonPropertyName("source")]
    public string? Source { get; init; }

    [JsonPropertyName("lastUpdated")]
    public string? LastUpdated { get; init; }

    [JsonPropertyName("maleNamesCount")]
    public int MaleNamesCount { get; init; }

    [JsonPropertyName("femaleNamesCount")]
    public int FemaleNamesCount { get; init; }

    [JsonPropertyName("lastNamesCount")]
    public int LastNamesCount { get; init; }

    [JsonPropertyName("totalCombinations")]
    public int TotalCombinations { get; init; }
}

/// <summary>
/// Address data from free tier datasets.
/// </summary>
public record AddressCollection
{
    [JsonPropertyName("addresses")]
    public required IReadOnlyList<AddressData> Addresses { get; init; }

    [JsonPropertyName("_metadata")]
    public AddressMetadata? Metadata { get; init; }
}

public record AddressData
{
    [JsonPropertyName("street")]
    public required string Street { get; init; }

    [JsonPropertyName("city")]
    public required string City { get; init; }

    [JsonPropertyName("state")]
    public required string State { get; init; }

    [JsonPropertyName("stateName")]
    public required string StateName { get; init; }

    [JsonPropertyName("region")]
    public required string Region { get; init; }

    [JsonPropertyName("zipCode")]
    public required string ZipCode { get; init; }
}

public record AddressMetadata
{
    [JsonPropertyName("source")]
    public string? Source { get; init; }

    [JsonPropertyName("lastUpdated")]
    public string? LastUpdated { get; init; }

    [JsonPropertyName("count")]
    public int Count { get; init; }

    [JsonPropertyName("regions")]
    public IReadOnlyList<string>? Regions { get; init; }
}
