// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Segmint.Core.Standards.Common;

/// <summary>
/// Universal validator interface for all healthcare standards.
/// Provides consistent validation capabilities across HL7, FHIR, NCPDP, and other standards.
/// </summary>
public interface IStandardValidator
{
    /// <summary>
    /// Gets the healthcare standard this validator supports.
    /// </summary>
    string StandardName { get; }

    /// <summary>
    /// Gets the supported validation levels for this standard.
    /// </summary>
    ValidationLevel[] SupportedLevels { get; }

    /// <summary>
    /// Validates a single message at the specified validation level.
    /// </summary>
    /// <param name="message">The message to validate.</param>
    /// <param name="level">The validation level to apply.</param>
    /// <returns>Validation result with errors, warnings, and information.</returns>
    ValidationResult ValidateMessage(IStandardMessage message, ValidationLevel level = ValidationLevel.Semantic);

    /// <summary>
    /// Validates a collection of related messages for workflow consistency.
    /// </summary>
    /// <param name="messages">Related messages in a healthcare workflow.</param>
    /// <param name="level">The validation level to apply.</param>
    /// <returns>Validation result covering all messages and their relationships.</returns>
    ValidationResult ValidateWorkflow(IEnumerable<IStandardMessage> messages, ValidationLevel level = ValidationLevel.Semantic);

    /// <summary>
    /// Validates a message against a specific configuration or profile.
    /// </summary>
    /// <param name="message">The message to validate.</param>
    /// <param name="configurationPath">Path to the configuration or profile file.</param>
    /// <param name="level">The validation level to apply.</param>
    /// <returns>Configuration-specific validation result.</returns>
    Task<ValidationResult> ValidateAgainstConfigurationAsync(IStandardMessage message, string configurationPath, ValidationLevel level = ValidationLevel.Semantic);

    /// <summary>
    /// Performs cross-standard validation for message transformation scenarios.
    /// </summary>
    /// <param name="sourceMessage">Original message in source standard.</param>
    /// <param name="targetMessage">Transformed message in target standard.</param>
    /// <param name="mappingRules">Transformation mapping rules to validate against.</param>
    /// <returns>Cross-standard validation result.</returns>
    ValidationResult ValidateCrossStandard(IStandardMessage sourceMessage, IStandardMessage targetMessage, object mappingRules);
}

/// <summary>
/// Defines the different levels of validation that can be applied to healthcare messages.
/// </summary>
public enum ValidationLevel
{
    /// <summary>
    /// Basic syntax and format validation only.
    /// Checks that the message follows the basic structure of the standard.
    /// </summary>
    Syntax = 1,

    /// <summary>
    /// Semantic validation including field requirements, data types, and cardinality.
    /// Validates that required fields are present and data types are correct.
    /// </summary>
    Semantic = 2,

    /// <summary>
    /// Clinical validation including medical appropriateness and code validity.
    /// Checks that clinical codes are valid and combinations make medical sense.
    /// </summary>
    Clinical = 3,

    /// <summary>
    /// Regulatory and compliance validation for legal requirements.
    /// Validates against regulatory requirements like HIPAA, FDA, DEA rules.
    /// </summary>
    Regulatory = 4,

    /// <summary>
    /// Cross-standard consistency validation for multi-standard workflows.
    /// Ensures consistency when messages are transformed between standards.
    /// </summary>
    CrossStandard = 5
}

/// <summary>
/// Represents the result of validating a healthcare standard message or workflow.
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// Gets or sets whether the validation passed without critical errors.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Gets or sets the validation level that was applied.
    /// </summary>
    public ValidationLevel Level { get; set; }

    /// <summary>
    /// Gets or sets the list of validation errors (critical issues that prevent processing).
    /// </summary>
    public List<ValidationIssue> Errors { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of validation warnings (issues that should be addressed but don't prevent processing).
    /// </summary>
    public List<ValidationIssue> Warnings { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of informational messages about the validation.
    /// </summary>
    public List<ValidationIssue> Information { get; set; } = new();

    /// <summary>
    /// Gets or sets the total time taken to perform the validation.
    /// </summary>
    public TimeSpan ValidationTime { get; set; }

    /// <summary>
    /// Gets or sets custom properties specific to the healthcare standard or validation context.
    /// </summary>
    public Dictionary<string, object> Properties { get; set; } = new();

    /// <summary>
    /// Gets the total count of all issues (errors + warnings + information).
    /// </summary>
    public int TotalIssueCount => Errors.Count + Warnings.Count + Information.Count;
}

/// <summary>
/// Represents a single validation issue found during healthcare message validation.
/// </summary>
public class ValidationIssue
{
    /// <summary>
    /// Gets or sets the severity level of the issue.
    /// </summary>
    public ValidationSeverity Severity { get; set; }

    /// <summary>
    /// Gets or sets the validation rule or check that generated this issue.
    /// </summary>
    public string RuleId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the human-readable description of the issue.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the location within the message where the issue was found.
    /// </summary>
    public string Location { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the suggested fix or remediation for the issue.
    /// </summary>
    public string? Suggestion { get; set; }

    /// <summary>
    /// Gets or sets additional context or details about the issue.
    /// </summary>
    public Dictionary<string, object> Context { get; set; } = new();
}

/// <summary>
/// Defines the severity levels for validation issues.
/// </summary>
public enum ValidationSeverity
{
    /// <summary>
    /// Informational message that doesn't indicate a problem.
    /// </summary>
    Information,

    /// <summary>
    /// Warning about a potential issue that should be reviewed.
    /// </summary>
    Warning,

    /// <summary>
    /// Error that prevents the message from being processed correctly.
    /// </summary>
    Error,

    /// <summary>
    /// Critical error that indicates a serious compliance or safety issue.
    /// </summary>
    Critical
}