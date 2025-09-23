// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Pidgeon.Core.Common.Types;

namespace Pidgeon.Core.Application.Interfaces.Intelligence;

/// <summary>
/// Service for AI-assisted healthcare message modification with constraint validation.
/// Enables natural language modification requests while maintaining message validity.
/// </summary>
public interface IAIMessageModificationService
{
    /// <summary>
    /// Modifies a healthcare message based on user intent using AI assistance.
    /// </summary>
    /// <param name="originalMessage">The original message to modify</param>
    /// <param name="userIntent">Natural language description of desired changes</param>
    /// <param name="options">Modification options and constraints</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing modified message and validation status</returns>
    Task<Result<MessageModificationResult>> ModifyMessageAsync(
        string originalMessage,
        string userIntent,
        ModificationOptions options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs an interactive wizard for guided message modification.
    /// </summary>
    /// <param name="originalMessage">The original message to modify</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing modified message and applied changes</returns>
    Task<Result<MessageModificationResult>> RunModificationWizardAsync(
        string originalMessage,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Suggests a realistic value for a specific field based on context.
    /// </summary>
    /// <param name="fieldPath">HL7 field path (e.g., "PID.5")</param>
    /// <param name="context">Context for value generation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Suggested value with confidence score</returns>
    Task<Result<FieldSuggestion>> SuggestFieldValueAsync(
        string fieldPath,
        string context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies a predefined template to modify a message.
    /// </summary>
    /// <param name="originalMessage">The original message to modify</param>
    /// <param name="templateName">Name of the template to apply</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing modified message</returns>
    Task<Result<MessageModificationResult>> ApplyTemplateAsync(
        string originalMessage,
        string templateName,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Options for message modification operations.
/// </summary>
public record ModificationOptions
{
    /// <summary>
    /// Whether to validate constraints after each change.
    /// </summary>
    public bool ValidateConstraints { get; init; } = true;

    /// <summary>
    /// Whether to explain each change made.
    /// </summary>
    public bool ExplainChanges { get; init; } = false;

    /// <summary>
    /// Fields that should not be modified (locked).
    /// </summary>
    public List<string> LockedFields { get; init; } = new();

    /// <summary>
    /// Whether to use demographic datasets for realistic values.
    /// </summary>
    public bool UseDemographicData { get; init; } = true;

    /// <summary>
    /// Maximum number of changes to apply.
    /// </summary>
    public int MaxChanges { get; init; } = 20;

    /// <summary>
    /// Validation mode to use for constraint checking.
    /// </summary>
    public string ValidationMode { get; init; } = "compatibility";
}

/// <summary>
/// Result of a message modification operation.
/// </summary>
public record MessageModificationResult
{
    /// <summary>
    /// The original message before modifications.
    /// </summary>
    public string OriginalMessage { get; init; } = string.Empty;

    /// <summary>
    /// The modified message after changes.
    /// </summary>
    public string ModifiedMessage { get; init; } = string.Empty;

    /// <summary>
    /// List of changes that were applied.
    /// </summary>
    public List<AppliedChange> AppliedChanges { get; init; } = new();

    /// <summary>
    /// Validation result of the modified message.
    /// </summary>
    public ValidationSummary ValidationResult { get; init; } = new();

    /// <summary>
    /// AI reasoning for the modifications.
    /// </summary>
    public string AIReasoning { get; init; } = string.Empty;

    /// <summary>
    /// Confidence score for the modifications (0.0 to 1.0).
    /// </summary>
    public double Confidence { get; init; }

    /// <summary>
    /// Any warnings or notes about the modifications.
    /// </summary>
    public List<string> Warnings { get; init; } = new();
}

/// <summary>
/// Represents a single change applied to a message.
/// </summary>
public record AppliedChange
{
    /// <summary>
    /// Field path that was modified.
    /// </summary>
    public string FieldPath { get; init; } = string.Empty;

    /// <summary>
    /// Human-readable field name.
    /// </summary>
    public string FieldName { get; init; } = string.Empty;

    /// <summary>
    /// Original value before change.
    /// </summary>
    public string? OriginalValue { get; init; }

    /// <summary>
    /// New value after change.
    /// </summary>
    public string NewValue { get; init; } = string.Empty;

    /// <summary>
    /// Type of change applied.
    /// </summary>
    public ChangeType ChangeType { get; init; }

    /// <summary>
    /// Reason for the change.
    /// </summary>
    public string Reason { get; init; } = string.Empty;

    /// <summary>
    /// Whether the new value was validated against constraints.
    /// </summary>
    public bool IsValidated { get; init; }

    /// <summary>
    /// Source of the new value (e.g., "Demographics", "AI Generated", "Template").
    /// </summary>
    public string ValueSource { get; init; } = string.Empty;
}

/// <summary>
/// Types of changes that can be applied to a message.
/// </summary>
public enum ChangeType
{
    /// <summary>
    /// Existing field value was modified.
    /// </summary>
    Modified,

    /// <summary>
    /// New field was added.
    /// </summary>
    Added,

    /// <summary>
    /// Field was removed.
    /// </summary>
    Removed,

    /// <summary>
    /// New segment was added.
    /// </summary>
    SegmentAdded,

    /// <summary>
    /// Segment was removed.
    /// </summary>
    SegmentRemoved
}

/// <summary>
/// Suggested value for a field with metadata.
/// </summary>
public record FieldSuggestion
{
    /// <summary>
    /// The suggested value.
    /// </summary>
    public string Value { get; init; } = string.Empty;

    /// <summary>
    /// Confidence score for the suggestion (0.0 to 1.0).
    /// </summary>
    public double Confidence { get; init; }

    /// <summary>
    /// Source of the suggestion.
    /// </summary>
    public string Source { get; init; } = string.Empty;

    /// <summary>
    /// Reasoning for the suggestion.
    /// </summary>
    public string Reasoning { get; init; } = string.Empty;

    /// <summary>
    /// Alternative suggestions if available.
    /// </summary>
    public List<string> Alternatives { get; init; } = new();

    /// <summary>
    /// Whether the suggestion passes constraint validation.
    /// </summary>
    public bool IsValid { get; init; }
}

/// <summary>
/// Summary of validation results.
/// </summary>
public record ValidationSummary
{
    /// <summary>
    /// Whether the message is valid overall.
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// Number of errors found.
    /// </summary>
    public int ErrorCount { get; init; }

    /// <summary>
    /// Number of warnings found.
    /// </summary>
    public int WarningCount { get; init; }

    /// <summary>
    /// Validation mode used.
    /// </summary>
    public string ValidationMode { get; init; } = string.Empty;

    /// <summary>
    /// Detailed validation messages.
    /// </summary>
    public List<string> Messages { get; init; } = new();
}