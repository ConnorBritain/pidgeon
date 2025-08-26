// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System.Text.Json.Serialization;

namespace Pidgeon.Core.Types;

/// <summary>
/// Represents a configuration inferred from analyzing sample messages.
/// Contains vendor signatures, field patterns, and statistical confidence metrics.
/// </summary>
public record InferredConfiguration
{
    /// <summary>
    /// Detected vendor signature with application and facility information.
    /// </summary>
    [JsonPropertyName("vendor")]
    public VendorSignature Vendor { get; init; } = default!;

    /// <summary>
    /// Field usage patterns across different segment types.
    /// </summary>
    [JsonPropertyName("fieldPatterns")]
    public FieldPatterns FieldPatterns { get; init; } = default!;

    /// <summary>
    /// Message type patterns and frequencies observed in the sample set.
    /// </summary>
    [JsonPropertyName("messagePatterns")]
    public Dictionary<string, MessagePattern> MessagePatterns { get; init; } = new();

    /// <summary>
    /// Overall confidence level in the inferred configuration (0.0 to 1.0).
    /// </summary>
    [JsonPropertyName("confidence")]
    public double Confidence { get; init; }

    /// <summary>
    /// Timestamp when this configuration was analyzed.
    /// </summary>
    [JsonPropertyName("analysisTimestamp")]
    public DateTime AnalysisTimestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Number of sample messages used for this analysis.
    /// </summary>
    [JsonPropertyName("sampleCount")]
    public int SampleCount { get; init; }

    /// <summary>
    /// Version of the configuration inference algorithm used.
    /// </summary>
    [JsonPropertyName("version")]
    public string Version { get; init; } = "1.0";
}