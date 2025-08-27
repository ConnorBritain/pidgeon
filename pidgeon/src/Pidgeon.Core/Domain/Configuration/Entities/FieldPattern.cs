// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System.Text.Json.Serialization;

namespace Pidgeon.Core.Domain.Configuration.Entities;

/// <summary>
/// Represents a single field pattern used in vendor configuration analysis.
/// Simplified pattern representation for vendor-specific field usage rules.
/// </summary>
public record FieldPattern
{
    /// <summary>
    /// Field path or identifier (e.g., "PID.5", "Patient.name").
    /// </summary>
    [JsonPropertyName("path")]
    public string Path { get; init; } = string.Empty;

    /// <summary>
    /// Population rate indicating how often this field is used (0.0 to 1.0).
    /// </summary>
    [JsonPropertyName("populationRate")]
    public double PopulationRate { get; init; }

    /// <summary>
    /// Common values observed in this field.
    /// </summary>
    [JsonPropertyName("commonValues")]
    public List<string> CommonValues { get; init; } = new();

    /// <summary>
    /// Regular expression pattern for validation (if applicable).
    /// </summary>
    [JsonPropertyName("regexPattern")]
    public string? RegexPattern { get; init; }

    /// <summary>
    /// Field cardinality information.
    /// </summary>
    [JsonPropertyName("cardinality")]
    public Cardinality Cardinality { get; init; } = Cardinality.Optional;
}