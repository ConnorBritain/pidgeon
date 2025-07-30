// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Segmint.Core.HL7.Validation;

namespace Segmint.CLI.Services;

/// <summary>
/// Service for validating HL7 messages.
/// </summary>
public interface IValidationService
{
    /// <summary>
    /// Validates HL7 messages from input file or directory.
    /// </summary>
    /// <param name="inputPath">Path to file or directory containing HL7 messages.</param>
    /// <param name="validationLevels">Levels of validation to perform.</param>
    /// <param name="configurationPath">Optional configuration file for validation rules.</param>
    /// <param name="strictMode">Enable strict validation mode.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Validation results for all processed messages.</returns>
    Task<ValidationSummary> ValidateAsync(
        string inputPath,
        string[] validationLevels,
        string? configurationPath = null,
        bool strictMode = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a single HL7 message string.
    /// </summary>
    /// <param name="hl7Message">The HL7 message string to validate.</param>
    /// <param name="validationLevels">Levels of validation to perform.</param>
    /// <param name="configurationPath">Optional configuration file for validation rules.</param>
    /// <param name="strictMode">Enable strict validation mode.</param>
    /// <returns>Validation result for the message.</returns>
    Task<Segmint.Core.HL7.Validation.ValidationResult> ValidateMessageAsync(
        string hl7Message,
        string[] validationLevels,
        string? configurationPath = null,
        bool strictMode = false);

    /// <summary>
    /// Gets available validation levels.
    /// </summary>
    /// <returns>Collection of supported validation levels.</returns>
    IEnumerable<string> GetAvailableValidationLevels();
}

/// <summary>
/// Summary of validation results for multiple messages.
/// </summary>
public class ValidationSummary
{
    /// <summary>
    /// Gets or sets the total number of messages processed.
    /// </summary>
    public int TotalMessages { get; set; }

    /// <summary>
    /// Gets or sets the number of valid messages.
    /// </summary>
    public int ValidMessages { get; set; }

    /// <summary>
    /// Gets or sets the number of invalid messages.
    /// </summary>
    public int InvalidMessages { get; set; }

    /// <summary>
    /// Gets or sets the validation results for each message.
    /// </summary>
    public List<MessageValidationResult> Results { get; set; } = new();

    /// <summary>
    /// Gets or sets the elapsed time for validation.
    /// </summary>
    public TimeSpan ElapsedTime { get; set; }
}

/// <summary>
/// Validation result for a single message with metadata.
/// </summary>
public class MessageValidationResult
{
    /// <summary>
    /// Gets or sets the source file path or identifier.
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the message index within the source.
    /// </summary>
    public int MessageIndex { get; set; }

    /// <summary>
    /// Gets or sets the validation result.
    /// </summary>
    public ValidationResult ValidationResult { get; set; } = new();

    /// <summary>
    /// Gets or sets the processing time for this message.
    /// </summary>
    public TimeSpan ProcessingTime { get; set; }
}

/// <summary>
/// Represents a validation error for compatibility with CLI interface.
/// </summary>
public class ValidationError
{
    /// <summary>
    /// Gets or sets the validation level.
    /// </summary>
    public ValidationLevel Level { get; set; }

    /// <summary>
    /// Gets or sets the error code.
    /// </summary>
    public string? Code { get; set; }

    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the field location where the error occurred.
    /// </summary>
    public string Field { get; set; } = string.Empty;
}

/// <summary>
/// Represents the validation level.
/// </summary>
public enum ValidationLevel
{
    /// <summary>
    /// Syntax validation level.
    /// </summary>
    Syntax,

    /// <summary>
    /// Semantic validation level.
    /// </summary>
    Semantic,

    /// <summary>
    /// Clinical validation level.
    /// </summary>
    Clinical,

    /// <summary>
    /// Interface validation level.
    /// </summary>
    Interface
}