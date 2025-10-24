// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System.Text.Json.Serialization;

namespace Pidgeon.Core.Application.DTOs.Data;

/// <summary>
/// ICD-10 diagnosis code data from free tier datasets.
/// Standard-agnostic format for use across HL7, FHIR, and other standards.
/// </summary>
public record DiagnosisData
{
    [JsonPropertyName("code")]
    public required string Code { get; init; }

    [JsonPropertyName("description")]
    public required string Description { get; init; }

    [JsonPropertyName("isBillable")]
    public required bool IsBillable { get; init; }

    [JsonPropertyName("category")]
    public required string Category { get; init; }
}
