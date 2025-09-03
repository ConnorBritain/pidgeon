// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System.Text.Json.Serialization;

namespace Pidgeon.Core.Domain.Configuration.Entities;

/// <summary>
/// Statistics about the configuration catalog contents.
/// </summary>
public record ConfigurationCatalogStats
{
    /// <summary>
    /// Total number of configurations in catalog.
    /// </summary>
    [JsonPropertyName("totalConfigurations")]
    public int TotalConfigurations { get; init; }

    /// <summary>
    /// Number of unique vendors represented.
    /// </summary>
    [JsonPropertyName("uniqueVendors")]
    public int UniqueVendors { get; init; }

    /// <summary>
    /// Number of unique standards supported.
    /// </summary>
    [JsonPropertyName("uniqueStandards")]
    public int UniqueStandards { get; init; }

    /// <summary>
    /// Number of unique message types configured.
    /// </summary>
    [JsonPropertyName("uniqueMessageTypes")]
    public int UniqueMessageTypes { get; init; }

    /// <summary>
    /// Total number of messages analyzed across all configurations.
    /// </summary>
    [JsonPropertyName("totalMessagesAnalyzed")]
    public long TotalMessagesAnalyzed { get; init; }

    /// <summary>
    /// Average confidence score across all configurations.
    /// </summary>
    [JsonPropertyName("averageConfidence")]
    public double AverageConfidence { get; init; }

    /// <summary>
    /// Timestamp of most recent configuration update.
    /// </summary>
    [JsonPropertyName("lastUpdated")]
    public DateTime LastUpdated { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Configuration counts by vendor.
    /// </summary>
    [JsonPropertyName("vendorCounts")]
    public Dictionary<string, int> VendorCounts { get; init; } = new();

    /// <summary>
    /// Configuration counts by standard.
    /// </summary>
    [JsonPropertyName("standardCounts")]
    public Dictionary<string, int> StandardCounts { get; init; } = new();
}