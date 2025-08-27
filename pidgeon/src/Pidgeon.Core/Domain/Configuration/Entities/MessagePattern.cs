// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System.Text.Json.Serialization;

namespace Pidgeon.Core.Domain.Configuration.Entities;

/// <summary>
/// Represents a message type pattern observed during analysis.
/// </summary>
public record MessagePattern
{
    /// <summary>
    /// HL7 message type (e.g., "ADT^A01", "RDE^O01").
    /// </summary>
    [JsonPropertyName("messageType")]
    public string MessageType { get; init; } = default!;

    /// <summary>
    /// Frequency of this message type in the analyzed sample.
    /// </summary>
    [JsonPropertyName("frequency")]
    public int Frequency { get; init; }

    /// <summary>
    /// Segment patterns within this message type.
    /// </summary>
    [JsonPropertyName("segmentPatterns")]
    public Dictionary<string, SegmentPattern> SegmentPatterns { get; init; } = new();

    /// <summary>
    /// Healthcare standard this pattern applies to.
    /// </summary>
    [JsonPropertyName("standard")]
    public string Standard { get; init; } = string.Empty;

    /// <summary>
    /// Total number of samples analyzed for this pattern.
    /// </summary>
    [JsonPropertyName("totalSamples")]
    public int TotalSamples { get; init; }

    /// <summary>
    /// Field frequency analysis for this message pattern.
    /// </summary>
    [JsonPropertyName("fieldFrequencies")]
    public Dictionary<string, FieldFrequency> FieldFrequencies { get; init; } = new();

    /// <summary>
    /// Component patterns found in this message.
    /// </summary>
    [JsonPropertyName("componentPatterns")]
    public Dictionary<string, ComponentPattern> ComponentPatterns { get; init; } = new();

    /// <summary>
    /// Null tolerance threshold for this pattern.
    /// </summary>
    [JsonPropertyName("nullTolerance")]
    public double NullTolerance { get; init; }

    /// <summary>
    /// Confidence score for this pattern (0.0 to 1.0).
    /// </summary>
    [JsonPropertyName("confidence")]
    public double Confidence { get; init; }

    /// <summary>
    /// When this analysis was performed.
    /// </summary>
    [JsonPropertyName("analysisDate")]
    public DateTime AnalysisDate { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Merges this message pattern with another pattern of the same type.
    /// Combines frequencies and segment patterns.
    /// </summary>
    /// <param name="other">Other message pattern to merge</param>
    /// <returns>Merged message pattern</returns>
    public MessagePattern MergeWith(MessagePattern other)
    {
        if (MessageType != other.MessageType)
            throw new ArgumentException($"Cannot merge patterns of different message types: {MessageType} vs {other.MessageType}");

        // Merge segment patterns
        var mergedSegmentPatterns = new Dictionary<string, SegmentPattern>(SegmentPatterns);
        foreach (var kvp in other.SegmentPatterns)
        {
            if (mergedSegmentPatterns.ContainsKey(kvp.Key))
            {
                // Merge field frequencies by combining dictionaries
                var mergedFields = new Dictionary<int, FieldFrequency>(mergedSegmentPatterns[kvp.Key].Fields);
                foreach (var fieldKvp in kvp.Value.Fields)
                {
                    if (mergedFields.ContainsKey(fieldKvp.Key))
                    {
                        // Merge field frequencies by adding counts
                        var existing = mergedFields[fieldKvp.Key];
                        mergedFields[fieldKvp.Key] = new FieldFrequency
                        {
                            FieldIndex = existing.FieldIndex,
                            PopulatedCount = existing.PopulatedCount + fieldKvp.Value.PopulatedCount,
                            TotalCount = existing.TotalCount + fieldKvp.Value.TotalCount,
                            PopulationRate = (double)(existing.PopulatedCount + fieldKvp.Value.PopulatedCount) / (existing.TotalCount + fieldKvp.Value.TotalCount),
                            CommonValues = existing.CommonValues.Concat(fieldKvp.Value.CommonValues).GroupBy(x => x.Key).ToDictionary(g => g.Key, g => g.Sum(x => x.Value))
                        };
                    }
                    else
                    {
                        mergedFields[fieldKvp.Key] = fieldKvp.Value;
                    }
                }
                mergedSegmentPatterns[kvp.Key] = kvp.Value with { Fields = mergedFields };
            }
            else
            {
                mergedSegmentPatterns[kvp.Key] = kvp.Value;
            }
        }

        return this with
        {
            Frequency = Frequency + other.Frequency,
            SegmentPatterns = mergedSegmentPatterns
        };
    }
}

