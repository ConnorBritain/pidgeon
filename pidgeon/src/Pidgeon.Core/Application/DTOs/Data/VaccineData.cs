// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System.Text.Json.Serialization;

namespace Pidgeon.Core.Application.DTOs.Data;

/// <summary>
/// CVX vaccine code data from free tier datasets.
/// Standard-agnostic format for use across HL7, FHIR, and other standards.
/// </summary>
public record VaccineData
{
    [JsonPropertyName("cvxCode")]
    public required string CvxCode { get; init; }

    [JsonPropertyName("shortDescription")]
    public required string ShortDescription { get; init; }

    [JsonPropertyName("fullName")]
    public required string FullName { get; init; }

    [JsonPropertyName("notes")]
    public required string Notes { get; init; }

    [JsonPropertyName("status")]
    public required string Status { get; init; }

    [JsonPropertyName("lastUpdated")]
    public required string LastUpdated { get; init; }
}
