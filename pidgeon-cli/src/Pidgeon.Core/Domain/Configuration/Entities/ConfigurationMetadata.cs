// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System.Text.Json.Serialization;
using Pidgeon.Core.Domain.Configuration.Common;

namespace Pidgeon.Core.Domain.Configuration.Entities;

/// <summary>
/// Metadata about configuration analysis and evolution tracking.
/// </summary>
public record ConfigurationMetadata : TemporalConfigurationBase
{
    /// <summary>
    /// When this configuration was first created.
    /// </summary>
    [JsonPropertyName("firstSeen")]
    public DateTime FirstSeen { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Total number of messages sampled for this configuration.
    /// </summary>
    [JsonPropertyName("messagesSampled")]
    public int MessagesSampled { get; init; }

    /// <summary>
    /// List of configuration changes over time.
    /// </summary>
    [JsonPropertyName("changes")]
    public List<ConfigurationChange> Changes { get; init; } = new();

    /// <summary>
    /// Overall confidence level in the configuration (0.0 to 1.0).
    /// </summary>
    [JsonPropertyName("confidence")]
    public double Confidence { get; init; }

    /// <summary>
    /// Version of the analysis algorithm used.
    /// </summary>
    [JsonPropertyName("algorithmVersion")]
    public string AlgorithmVersion { get; init; } = "1.0";

    /// <summary>
    /// Creates updated metadata with new analysis information.
    /// </summary>
    /// <param name="additionalMessages">Number of new messages analyzed</param>
    /// <param name="newConfidence">Updated confidence score</param>
    /// <param name="changes">Any configuration changes detected</param>
    /// <returns>Updated metadata</returns>
    public ConfigurationMetadata WithUpdate(
        int additionalMessages, 
        double newConfidence, 
        IEnumerable<ConfigurationChange>? changes = null)
    {
        var newChanges = Changes.ToList();
        if (changes != null)
            newChanges.AddRange(changes);

        return this with
        {
            LastUpdated = DateTime.UtcNow,
            MessagesSampled = MessagesSampled + additionalMessages,
            Confidence = newConfidence,
            Changes = newChanges,
            Version = IncrementVersion(Version)
        };
    }

    /// <summary>
    /// Increments version string (1.0 -> 1.1, 1.9 -> 2.0, etc.).
    /// </summary>
    private static string IncrementVersion(string version)
    {
        if (System.Version.TryParse(version, out var v))
        {
            return v.Minor == 9 
                ? new System.Version(v.Major + 1, 0).ToString() 
                : new System.Version(v.Major, v.Minor + 1).ToString();
        }
        
        return version; // Return unchanged if not parseable
    }
}

/// <summary>
/// Types of configuration changes that can be detected.
/// </summary>
public enum ConfigurationChangeType
{
    /// <summary>
    /// Configuration created for the first time.
    /// </summary>
    Created,

    /// <summary>
    /// Field pattern added.
    /// </summary>
    FieldAdded,

    /// <summary>
    /// Field pattern changed.
    /// </summary>
    FieldChanged,

    /// <summary>
    /// Field pattern removed.
    /// </summary>
    FieldRemoved,

    /// <summary>
    /// Message pattern changed.
    /// </summary>
    PatternChanged,

    /// <summary>
    /// Format deviation detected.
    /// </summary>
    DeviationDetected,

    /// <summary>
    /// Vendor signature changed.
    /// </summary>
    VendorChanged
}

/// <summary>
/// Represents a change in configuration over time.
/// </summary>
public record ConfigurationChange
{
    /// <summary>
    /// When this change was detected.
    /// </summary>
    [JsonPropertyName("changeDate")]
    public DateTime ChangeDate { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Type of change (e.g., "FieldAdded", "PatternChanged", "DeviationDetected").
    /// </summary>
    [JsonPropertyName("changeType")]
    public ConfigurationChangeType ChangeType { get; init; }

    /// <summary>
    /// Human-readable description of the change.
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; init; } = default!;

    /// <summary>
    /// Confidence impact of this change (-1.0 to 1.0).
    /// </summary>
    [JsonPropertyName("confidenceImpact")]
    public double ConfidenceImpact { get; init; }

    /// <summary>
    /// Impact score of this change (0.0 = minor, 1.0 = major breaking change).
    /// </summary>
    [JsonPropertyName("impactScore")]
    public double ImpactScore { get; init; }

    /// <summary>
    /// Optional details about what specifically changed.
    /// </summary>
    [JsonPropertyName("details")]
    public Dictionary<string, object> Details { get; init; } = new();
}