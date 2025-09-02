// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Pidgeon.Core.Domain.Configuration.Entities;

namespace Pidgeon.Core.Application.Services.Configuration;

/// <summary>
/// Service responsible for detecting format deviations in healthcare messages.
/// Identifies variations from standard specifications such as encoding differences,
/// structural variations, and vendor-specific formatting patterns.
/// Single responsibility: "What format variations exist in these messages?"
/// </summary>
public interface IFormatDeviationDetectionService
{
    /// <summary>
    /// Detects encoding deviations across a set of messages for a specific healthcare standard.
    /// Analyzes field separators, component separators, escape characters, and other encoding elements.
    /// </summary>
    /// <param name="messages">Collection of messages to analyze</param>
    /// <param name="standard">Healthcare standard name (HL7v23, FHIRv4, etc.)</param>
    /// <returns>Result containing detected encoding deviations</returns>
    Task<Result<IReadOnlyList<FormatDeviation>>> DetectEncodingDeviationsAsync(
        IEnumerable<string> messages, 
        string standard);

    /// <summary>
    /// Detects structural deviations in message format such as extra segments,
    /// non-standard segment ordering, or missing required segments.
    /// </summary>
    /// <param name="messages">Collection of messages to analyze</param>
    /// <param name="standard">Healthcare standard name</param>
    /// <param name="messageType">Specific message type (ADT^A01, etc.)</param>
    /// <returns>Result containing detected structural deviations</returns>
    Task<Result<IReadOnlyList<FormatDeviation>>> DetectStructuralDeviationsAsync(
        IEnumerable<string> messages,
        string standard,
        string messageType);

    /// <summary>
    /// Detects field-level formatting deviations such as non-standard date formats,
    /// custom field patterns, or vendor-specific data representations.
    /// </summary>
    /// <param name="messages">Collection of messages to analyze</param>
    /// <param name="standard">Healthcare standard name</param>
    /// <param name="segmentType">Specific segment type (MSH, PID, etc.)</param>
    /// <returns>Result containing detected field format deviations</returns>
    Task<Result<IReadOnlyList<FormatDeviation>>> DetectFieldFormatDeviationsAsync(
        IEnumerable<string> messages,
        string standard,
        string segmentType);

    /// <summary>
    /// Performs comprehensive deviation analysis combining encoding, structural,
    /// and field format deviation detection for complete message analysis.
    /// </summary>
    /// <param name="messages">Collection of messages to analyze</param>
    /// <param name="standard">Healthcare standard name</param>
    /// <param name="messageType">Specific message type (optional)</param>
    /// <returns>Result containing all detected format deviations</returns>
    Task<Result<IReadOnlyList<FormatDeviation>>> DetectAllDeviationsAsync(
        IEnumerable<string> messages,
        string standard,
        string? messageType = null);
}