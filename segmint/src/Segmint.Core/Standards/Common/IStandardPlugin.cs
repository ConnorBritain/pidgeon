// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

namespace Segmint.Core.Standards.Common;

/// <summary>
/// Defines the contract for healthcare standard plugins.
/// Each standard (HL7, FHIR, NCPDP) implements this interface to provide
/// a consistent API for message generation, parsing, and validation.
/// </summary>
public interface IStandardPlugin
{
    /// <summary>
    /// Gets the name of the healthcare standard.
    /// </summary>
    string StandardName { get; }

    /// <summary>
    /// Gets the version of the healthcare standard supported.
    /// </summary>
    Version StandardVersion { get; }

    /// <summary>
    /// Gets a human-readable description of what this plugin supports.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Gets the supported message types for this standard.
    /// </summary>
    IReadOnlyList<string> SupportedMessageTypes { get; }

    /// <summary>
    /// Creates a message builder for the specified message type.
    /// </summary>
    /// <param name="messageType">The type of message to create (e.g., "ADT", "RDE", "Patient")</param>
    /// <returns>A result containing the message builder or an error</returns>
    Result<IStandardMessage> CreateMessage(string messageType);

    /// <summary>
    /// Gets the validator for this standard.
    /// </summary>
    /// <returns>A validator instance for this standard</returns>
    IStandardValidator GetValidator();

    /// <summary>
    /// Loads configuration specific to this standard.
    /// </summary>
    /// <param name="configPath">Path to the configuration file or data</param>
    /// <returns>A result containing the configuration or an error</returns>
    Result<IStandardConfig> LoadConfiguration(string configPath);

    /// <summary>
    /// Parses a message string into a structured message object.
    /// </summary>
    /// <param name="messageContent">The raw message content</param>
    /// <returns>A result containing the parsed message or an error</returns>
    Result<IStandardMessage> ParseMessage(string messageContent);

    /// <summary>
    /// Determines if this plugin can handle the given message content.
    /// </summary>
    /// <param name="messageContent">The message content to examine</param>
    /// <returns>True if this plugin can handle the message, false otherwise</returns>
    bool CanHandle(string messageContent);
}

/// <summary>
/// Represents a message in a healthcare standard.
/// This is the base interface that all standard-specific messages implement.
/// </summary>
public interface IStandardMessage
{
    /// <summary>
    /// Gets the message type (e.g., "ADT^A01", "Patient", "NewRx").
    /// </summary>
    string MessageType { get; }

    /// <summary>
    /// Gets the standard this message belongs to.
    /// </summary>
    string Standard { get; }

    /// <summary>
    /// Gets the version of the standard.
    /// </summary>
    Version StandardVersion { get; }

    /// <summary>
    /// Serializes the message to its string representation.
    /// </summary>
    /// <param name="options">Serialization options</param>
    /// <returns>A result containing the serialized message or an error</returns>
    Result<string> Serialize(SerializationOptions? options = null);

    /// <summary>
    /// Validates the message structure and content.
    /// </summary>
    /// <param name="validationMode">The validation mode to use</param>
    /// <returns>A result containing validation results</returns>
    Result<ValidationResult> Validate(ValidationMode validationMode = ValidationMode.Strict);

    /// <summary>
    /// Gets metadata about the message.
    /// </summary>
    /// <returns>Message metadata</returns>
    MessageMetadata GetMetadata();
}

/// <summary>
/// Defines the contract for validators that can validate messages
/// according to healthcare standard specifications.
/// </summary>
public interface IStandardValidator
{
    /// <summary>
    /// Gets the standard this validator supports.
    /// </summary>
    string StandardName { get; }

    /// <summary>
    /// Gets the version of the standard supported.
    /// </summary>
    Version StandardVersion { get; }

    /// <summary>
    /// Validates a message string according to the standard.
    /// </summary>
    /// <param name="messageContent">The message content to validate</param>
    /// <param name="validationMode">The validation mode to use</param>
    /// <returns>A result containing validation results</returns>
    Result<ValidationResult> ValidateMessage(string messageContent, ValidationMode validationMode = ValidationMode.Strict);

    /// <summary>
    /// Validates a structured message object.
    /// </summary>
    /// <param name="message">The message object to validate</param>
    /// <param name="validationMode">The validation mode to use</param>
    /// <returns>A result containing validation results</returns>
    Result<ValidationResult> ValidateMessage(IStandardMessage message, ValidationMode validationMode = ValidationMode.Strict);

    /// <summary>
    /// Gets available validation rules for this standard.
    /// </summary>
    /// <returns>A list of validation rule information</returns>
    IReadOnlyList<ValidationRuleInfo> GetValidationRules();
}

/// <summary>
/// Defines the contract for standard-specific configuration.
/// </summary>
public interface IStandardConfig
{
    /// <summary>
    /// Gets the standard this configuration applies to.
    /// </summary>
    string StandardName { get; }

    /// <summary>
    /// Gets the version of the standard.
    /// </summary>
    Version StandardVersion { get; }

    /// <summary>
    /// Gets configuration values by key.
    /// </summary>
    /// <param name="key">The configuration key</param>
    /// <returns>The configuration value, or null if not found</returns>
    string? GetValue(string key);

    /// <summary>
    /// Gets all configuration keys.
    /// </summary>
    /// <returns>A list of all configuration keys</returns>
    IReadOnlyList<string> GetKeys();

    /// <summary>
    /// Validates the configuration.
    /// </summary>
    /// <returns>A result indicating whether the configuration is valid</returns>
    Result<IStandardConfig> Validate();
}

/// <summary>
/// Contains options for message serialization.
/// </summary>
public record SerializationOptions
{
    /// <summary>
    /// Gets whether to include optional fields.
    /// </summary>
    public bool IncludeOptionalFields { get; init; } = true;

    /// <summary>
    /// Gets whether to format the output for readability.
    /// </summary>
    public bool FormatForReadability { get; init; } = false;

    /// <summary>
    /// Gets the encoding to use for the output.
    /// </summary>
    public string Encoding { get; init; } = "UTF-8";

    /// <summary>
    /// Gets custom serialization options specific to the standard.
    /// </summary>
    public Dictionary<string, object> CustomOptions { get; init; } = new();

    /// <summary>
    /// Creates default serialization options.
    /// </summary>
    public static SerializationOptions Default => new();

    /// <summary>
    /// Creates options optimized for compatibility with specific systems.
    /// </summary>
    /// <param name="system">The target system (e.g., "Epic", "Cerner")</param>
    public static SerializationOptions ForSystem(string system) => new()
    {
        CustomOptions = new Dictionary<string, object> { ["TargetSystem"] = system }
    };
}

/// <summary>
/// Validation modes for different levels of strictness.
/// </summary>
public enum ValidationMode
{
    /// <summary>
    /// Strict validation according to the official specification.
    /// Rejects any deviations from the standard.
    /// </summary>
    Strict,

    /// <summary>
    /// Compatibility mode that accepts common real-world deviations.
    /// Uses vendor-specific patterns and configurations.
    /// </summary>
    Compatibility,

    /// <summary>
    /// Lenient validation that only checks for critical issues.
    /// Allows most variations as long as basic structure is intact.
    /// </summary>
    Lenient
}

/// <summary>
/// Contains the results of a validation operation.
/// </summary>
public record ValidationResult
{
    /// <summary>
    /// Gets whether the validation passed.
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// Gets the list of validation errors.
    /// </summary>
    public IReadOnlyList<ValidationError> Errors { get; init; } = Array.Empty<ValidationError>();

    /// <summary>
    /// Gets the list of validation warnings.
    /// </summary>
    public IReadOnlyList<ValidationWarning> Warnings { get; init; } = Array.Empty<ValidationWarning>();

    /// <summary>
    /// Gets information about which validation rules were applied.
    /// </summary>
    public ValidationContext Context { get; init; } = new();

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    public static ValidationResult Success(ValidationContext? context = null) => new()
    {
        IsValid = true,
        Context = context ?? new()
    };

    /// <summary>
    /// Creates a failed validation result with errors.
    /// </summary>
    public static ValidationResult Failure(IEnumerable<ValidationError> errors, ValidationContext? context = null) => new()
    {
        IsValid = false,
        Errors = errors.ToList(),
        Context = context ?? new()
    };
}

/// <summary>
/// Represents a validation error.
/// </summary>
public record ValidationError
{
    /// <summary>
    /// Gets the error code.
    /// </summary>
    public required string Code { get; init; }

    /// <summary>
    /// Gets the error message.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Gets the severity level of the error.
    /// </summary>
    public ValidationSeverity Severity { get; init; } = ValidationSeverity.Error;

    /// <summary>
    /// Gets the location where the error occurred.
    /// </summary>
    public string? Location { get; init; }

    /// <summary>
    /// Gets the field or element that caused the error.
    /// </summary>
    public string? Field { get; init; }

    /// <summary>
    /// Gets the expected value or format.
    /// </summary>
    public string? Expected { get; init; }

    /// <summary>
    /// Gets the actual value that caused the error.
    /// </summary>
    public string? Actual { get; init; }
}

/// <summary>
/// Represents a validation warning.
/// </summary>
public record ValidationWarning
{
    /// <summary>
    /// Gets the warning code.
    /// </summary>
    public required string Code { get; init; }

    /// <summary>
    /// Gets the warning message.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Gets the location where the warning occurred.
    /// </summary>
    public string? Location { get; init; }

    /// <summary>
    /// Gets the field or element that triggered the warning.
    /// </summary>
    public string? Field { get; init; }

    /// <summary>
    /// Gets a suggestion for resolving the warning.
    /// </summary>
    public string? Suggestion { get; init; }
}

/// <summary>
/// Provides context about validation operations.
/// </summary>
public record ValidationContext
{
    /// <summary>
    /// Gets the validation mode that was used.
    /// </summary>
    public ValidationMode Mode { get; init; } = ValidationMode.Strict;

    /// <summary>
    /// Gets the list of validation rules that were applied.
    /// </summary>
    public IReadOnlyList<string> AppliedRules { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets the configuration that was used for validation.
    /// </summary>
    public string? ConfigurationUsed { get; init; }

    /// <summary>
    /// Gets the timestamp when validation was performed.
    /// </summary>
    public DateTime ValidationTimestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Information about a validation rule.
/// </summary>
public record ValidationRuleInfo
{
    /// <summary>
    /// Gets the unique identifier for the rule.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the human-readable name of the rule.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the description of what the rule validates.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Gets the category of the rule.
    /// </summary>
    public string Category { get; init; } = "General";

    /// <summary>
    /// Gets the severity level of violations of this rule.
    /// </summary>
    public ValidationSeverity Severity { get; init; } = ValidationSeverity.Error;

    /// <summary>
    /// Gets whether the rule is enabled by default.
    /// </summary>
    public bool EnabledByDefault { get; init; } = true;
}

/// <summary>
/// Message metadata container.
/// </summary>
public record MessageMetadata
{
    /// <summary>
    /// Gets when the message was created.
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets who or what created the message.
    /// </summary>
    public string? CreatedBy { get; init; }

    /// <summary>
    /// Gets the message size in bytes.
    /// </summary>
    public int SizeInBytes { get; init; }

    /// <summary>
    /// Gets custom metadata properties.
    /// </summary>
    public Dictionary<string, object> Properties { get; init; } = new();
}

/// <summary>
/// Validation severity levels.
/// </summary>
public enum ValidationSeverity
{
    /// <summary>
    /// Informational message, no action needed.
    /// </summary>
    Info,

    /// <summary>
    /// Warning that should be addressed but doesn't prevent processing.
    /// </summary>
    Warning,

    /// <summary>
    /// Error that prevents successful processing.
    /// </summary>
    Error,

    /// <summary>
    /// Critical error that indicates a serious problem.
    /// </summary>
    Critical
}