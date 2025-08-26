// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System.Text.Json.Serialization;

namespace Pidgeon.Core.Types;

/// <summary>
/// Represents field usage patterns analyzed across a set of HL7 messages.
/// </summary>
public record FieldPatterns
{
    /// <summary>
    /// Field patterns organized by segment type.
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