// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System.Text.Json.Serialization;

namespace Pidgeon.Core.Types;

/// <summary>
/// Represents field usage patterns analyzed across a set of healthcare messages.
/// Standard-agnostic container for field population patterns.
/// </summary>
public record FieldPatterns
{
    /// <summary>
    /// Healthcare standard these patterns apply to (HL7v23, FHIRv4, NCPDP, etc.).
    /// </summary>
    [JsonPropertyName("standard")]
    public string Standard { get; init; } = string.Empty;

    /// <summary>
    /// Message type these patterns apply to (ADT^A01, RDE^O01, Patient, etc.).
    /// </summary>
    [JsonPropertyName("messageType")]
    public string MessageType { get; init; } = string.Empty;

    /// <summary>
    /// Field patterns organized by segment/resource type.
    /// For HL7: segment patterns; For FHIR: resource patterns; For NCPDP: section patterns.
    /// </summary>
    [JsonPropertyName("segmentPatterns")]
    public Dictionary<string, SegmentFieldPatterns> SegmentPatterns { get; init; } = new();

    /// <summary>
    /// Overall confidence level in the field pattern analysis (0.0 to 1.0).
    /// </summary>
    [JsonPropertyName("confidence")]
    public double Confidence { get; init; }

    /// <summary>
    /// Timestamp when the field pattern analysis was performed.
    /// </summary>
    [JsonPropertyName("analysisDate")]
    public DateTime AnalysisDate { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Represents field patterns for a specific segment or resource type.
/// </summary>
public record SegmentFieldPatterns
{
    /// <summary>
    /// Segment or resource identifier (e.g., "PID", "MSH", "Patient").
    /// </summary>
    [JsonPropertyName("segmentId")]
    public string SegmentId { get; init; } = string.Empty;

    /// <summary>
    /// Field frequency analysis by field position or path.
    /// </summary>
    [JsonPropertyName("fieldFrequencies")]
    public Dictionary<int, FieldFrequency> FieldFrequencies { get; init; } = new();

    /// <summary>
    /// Component patterns for composite fields.
    /// </summary>
    [JsonPropertyName("componentPatterns")]
    public Dictionary<string, ComponentPattern> ComponentPatterns { get; init; } = new();

    /// <summary>
    /// Number of segments analyzed.
    /// </summary>
    [JsonPropertyName("sampleSize")]
    public int SampleSize { get; init; }
}