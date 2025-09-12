// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Pidgeon.Core;
using Pidgeon.Core.Domain.Comparison.Entities;

namespace Pidgeon.Core.Application.Interfaces.Comparison;

/// <summary>
/// Service for comparing healthcare messages and generating field-level differences.
/// Supports algorithmic analysis with optional AI enhancement.
/// </summary>
public interface IMessageDiffService
{
    /// <summary>
    /// Compares two healthcare messages and returns detailed field-level differences.
    /// </summary>
    /// <param name="leftMessage">First message content</param>
    /// <param name="rightMessage">Second message content</param>
    /// <param name="context">Comparison context and options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Detailed diff result with field differences and insights</returns>
    Task<Result<MessageDiff>> CompareMessagesAsync(
        string leftMessage,
        string rightMessage,
        DiffContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Compares two healthcare messages from file sources.
    /// </summary>
    /// <param name="leftFilePath">Path to first message file</param>
    /// <param name="rightFilePath">Path to second message file</param>
    /// <param name="context">Comparison context and options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Detailed diff result with field differences and insights</returns>
    Task<Result<MessageDiff>> CompareMessageFilesAsync(
        string leftFilePath,
        string rightFilePath,
        DiffContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Compares directories containing healthcare messages.
    /// </summary>
    /// <param name="leftDirectory">Path to first directory</param>
    /// <param name="rightDirectory">Path to second directory</param>
    /// <param name="context">Comparison context and options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of diff results for matched files</returns>
    Task<Result<IEnumerable<MessageDiff>>> CompareDirectoriesAsync(
        string leftDirectory,
        string rightDirectory,
        DiffContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that two messages can be meaningfully compared.
    /// </summary>
    /// <param name="leftMessage">First message content</param>
    /// <param name="rightMessage">Second message content</param>
    /// <param name="context">Comparison context</param>
    /// <returns>Validation result indicating if comparison is possible</returns>
    Task<Result<bool>> ValidateComparisonAsync(
        string leftMessage,
        string rightMessage,
        DiffContext context);

    /// <summary>
    /// Gets supported healthcare standards for comparison.
    /// </summary>
    /// <returns>List of supported standards (HL7v2, FHIR, NCPDP, etc.)</returns>
    Task<Result<IEnumerable<string>>> GetSupportedStandardsAsync();

    /// <summary>
    /// Detects the healthcare standard of a message for comparison context.
    /// </summary>
    /// <param name="messageContent">Message content to analyze</param>
    /// <returns>Detected standard and version</returns>
    Task<Result<(string Standard, string Version)>> DetectMessageStandardAsync(string messageContent);
}