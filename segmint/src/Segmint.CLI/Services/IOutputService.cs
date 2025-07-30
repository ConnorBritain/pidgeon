// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Segmint.Core.Standards.HL7.v23.Messages;
using Segmint.Core.HL7.Validation;

namespace Segmint.CLI.Services;

/// <summary>
/// Service for handling output operations.
/// </summary>
public interface IOutputService
{
    /// <summary>
    /// Saves HL7 messages to the specified output path.
    /// </summary>
    /// <param name="messages">Messages to save.</param>
    /// <param name="outputPath">Output file or directory path.</param>
    /// <param name="format">Output format (hl7, json, xml).</param>
    /// <param name="batchMode">Enable batch processing mode.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveMessagesAsync(
        IEnumerable<HL7Message> messages,
        string outputPath,
        string? format = null,
        bool batchMode = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a validation report to file.
    /// </summary>
    /// <param name="validationSummary">Validation summary to save.</param>
    /// <param name="reportPath">Output report file path.</param>
    /// <param name="format">Report format (text, json, xml, html).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveValidationReportAsync(
        ValidationSummary validationSummary,
        string reportPath,
        string format = "text",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a configuration comparison report to file.
    /// </summary>
    /// <param name="comparisonResult">Comparison result to save.</param>
    /// <param name="reportPath">Output report file path.</param>
    /// <param name="format">Report format (text, json, xml, html).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveComparisonReportAsync(
        ConfigurationComparisonResult comparisonResult,
        string reportPath,
        string format = "text",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Displays validation summary to console.
    /// </summary>
    /// <param name="validationSummary">Validation summary to display.</param>
    /// <param name="summaryOnly">Show summary only without detailed results.</param>
    Task DisplayValidationSummaryAsync(
        ValidationSummary validationSummary,
        bool summaryOnly = false);

    /// <summary>
    /// Displays configuration comparison results to console.
    /// </summary>
    /// <param name="comparisonResult">Comparison result to display.</param>
    /// <param name="summaryOnly">Show summary only without detailed differences.</param>
    Task DisplayComparisonResultAsync(
        ConfigurationComparisonResult comparisonResult,
        bool summaryOnly = false);

    /// <summary>
    /// Displays configuration templates to console.
    /// </summary>
    /// <param name="templates">Templates to display.</param>
    /// <param name="detailed">Show detailed information.</param>
    Task DisplayTemplatesAsync(
        IEnumerable<ConfigurationTemplate> templates,
        bool detailed = false);

    /// <summary>
    /// Gets supported output formats for messages.
    /// </summary>
    /// <returns>Collection of supported output formats.</returns>
    IEnumerable<string> GetSupportedOutputFormats();

    /// <summary>
    /// Gets supported report formats.
    /// </summary>
    /// <returns>Collection of supported report formats.</returns>
    IEnumerable<string> GetSupportedReportFormats();
}