// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

namespace Pidgeon.Core.Domain.Configuration.Entities;

/// <summary>
/// Configuration options for vendor pattern inference.
/// </summary>
public record InferenceOptions
{
    /// <summary>
    /// Minimum confidence threshold for vendor detection (0.0-1.0).
    /// </summary>
    public double MinimumConfidence { get; init; } = 0.6;

    /// <summary>
    /// Optional deterministic seed for reproducible analysis.
    /// </summary>
    public int? Seed { get; init; }

    /// <summary>
    /// Maximum number of messages to analyze for performance.
    /// </summary>
    public int? MaxMessageCount { get; init; }

    /// <summary>
    /// Whether to include detailed field analysis.
    /// </summary>
    public bool IncludeFieldAnalysis { get; init; } = true;
}