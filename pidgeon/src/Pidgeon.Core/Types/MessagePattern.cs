// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System.Text.Json.Serialization;

namespace Pidgeon.Core.Types;

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
                // For now, keep the one with higher frequency
                // TODO: Implement proper SegmentPattern.MergeWith when SegmentPattern is fully designed
                if (kvp.Value.Frequency > mergedSegmentPatterns[kvp.Key].Frequency)
                    mergedSegmentPatterns[kvp.Key] = kvp.Value;
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

/// <summary>
/// Represents a segment pattern within a message type.
/// TODO: This is a placeholder - needs proper design in Phase 1B.
/// </summary>
public record SegmentPattern
{
    /// <summary>
    /// Segment type (e.g., "PID", "MSH", "ORC").
    /// </summary>
    [JsonPropertyName("segmentType")]
    public string SegmentType { get; init; } = default!;

    /// <summary>
    /// Frequency of this segment pattern.
    /// </summary>
    [JsonPropertyName("frequency")]
    public int Frequency { get; init; }

    /// <summary>
    /// Field patterns within this segment.
    /// TODO: Define proper field pattern structure.
    /// </summary>
    [JsonPropertyName("fieldPatterns")]
    public Dictionary<string, object> FieldPatterns { get; init; } = new();
}