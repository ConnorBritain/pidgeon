// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System.Text.Json.Serialization;

namespace Pidgeon.Core.Types;

/// <summary>
/// Represents a vendor-specific quirk or deviation from HL7 standards.
/// </summary>
public record VendorQuirk
{
    /// <summary>
    /// Type of quirk (e.g., "FieldFormat", "MissingField", "ExtraSegment").
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; init; } = default!;

    /// <summary>
    /// Human-readable description of the quirk.
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; init; } = default!;

    /// <summary>
    /// Location where the quirk occurs (e.g., "MSH.3", "PID.5.2").
    /// </summary>
    [JsonPropertyName("location")]
    public string Location { get; init; } = default!;

    /// <summary>
    /// Frequency of occurrence (0.0 to 1.0).
    /// </summary>
    [JsonPropertyName("frequency")]
    public double Frequency { get; init; }

    /// <summary>
    /// Severity level of the quirk.
    /// </summary>
    [JsonPropertyName("severity")]
    public QuirkSeverity Severity { get; init; } = QuirkSeverity.Info;
}

/// <summary>
/// Severity levels for vendor quirks.
/// </summary>
public enum QuirkSeverity
{
    /// <summary>
    /// Informational - minor deviation that doesn't impact processing.
    /// </summary>
    Info,

    /// <summary>
    /// Warning - deviation that might cause issues but is generally acceptable.
    /// </summary>
    Warning,

    /// <summary>
    /// Error - significant deviation that could cause processing failures.
    /// </summary>
    Error,

    /// <summary>
    /// Critical - severe deviation that will likely cause processing failures.
    /// </summary>
    Critical
}