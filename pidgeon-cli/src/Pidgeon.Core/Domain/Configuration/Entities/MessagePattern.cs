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
    /// Number of samples analyzed for this pattern.
    /// </summary>
    [JsonPropertyName("sampleSize")]
    public int SampleSize { get; init; }

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
    /// Ordered sequence of segments in this message pattern.
    /// </summary>
    [JsonPropertyName("segmentSequence")]
    public List<string> SegmentSequence { get; init; } = new();

    /// <summary>
    /// Required segments for this message type.
    /// </summary>
    [JsonPropertyName("requiredSegments")]
    public List<string> RequiredSegments { get; init; } = new();

    /// <summary>
    /// Optional segments for this message type.
    /// </summary>
    [JsonPropertyName("optionalSegments")]
    public List<string> OptionalSegments { get; init; } = new();

    // TODO: Remove this method - merge logic moved to IMessagePatternMergeService for SRP compliance
}

