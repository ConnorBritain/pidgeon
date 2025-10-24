// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System.Text.Json.Serialization;

namespace Pidgeon.Core.Domain.Configuration.Entities;

/// <summary>
/// Represents comparison results between two vendor configurations.
/// </summary>
public record ConfigurationComparison
{
    /// <summary>
    /// Source configuration address.
    /// </summary>
    [JsonPropertyName("fromAddress")]
    public ConfigurationAddress FromAddress { get; init; } = new("", "", "");

    /// <summary>
    /// Target configuration address.
    /// </summary>
    [JsonPropertyName("toAddress")]
    public ConfigurationAddress ToAddress { get; init; } = new("", "", "");

    /// <summary>
    /// Similarity score between configurations (0.0 to 1.0).
    /// </summary>
    [JsonPropertyName("similarityScore")]
    public double SimilarityScore { get; init; }

    /// <summary>
    /// Differences found between configurations.
    /// </summary>
    [JsonPropertyName("differences")]
    public List<string> Differences { get; init; } = new();
}