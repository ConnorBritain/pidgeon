// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System.Text.Json.Serialization;

namespace Pidgeon.Core.Application.DTOs.Data;

/// <summary>
/// LOINC lab test data from free tier datasets.
/// Standard-agnostic format for use across HL7, FHIR, and other standards.
/// </summary>
public record LabTestData
{
    [JsonPropertyName("loincCode")]
    public required string LoincCode { get; init; }

    [JsonPropertyName("component")]
    public required string Component { get; init; }

    [JsonPropertyName("property")]
    public required string Property { get; init; }

    [JsonPropertyName("timeAspect")]
    public string? TimeAspect { get; init; }

    [JsonPropertyName("system")]
    public required string System { get; init; }

    [JsonPropertyName("scale")]
    public required string Scale { get; init; }

    [JsonPropertyName("method")]
    public string? Method { get; init; }

    [JsonPropertyName("commonName")]
    public required string CommonName { get; init; }

    [JsonPropertyName("longCommonName")]
    public required string LongCommonName { get; init; }

    [JsonPropertyName("shortName")]
    public string? ShortName { get; init; }
}
