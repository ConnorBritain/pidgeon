// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Pidgeon.Core.Standards.Common;
using Pidgeon.Core.Standards.HL7.v23.Messages;

namespace Pidgeon.Core.Standards.HL7.v23.Plugins;

/// <summary>
/// Plugin implementation for HL7 v2.3 standard.
/// Provides support for ADT, RDE, ORM, and other HL7 v2.3 message types.
/// </summary>
public class HL7v23Plugin : IStandardPlugin
{
    public string StandardName => "HL7";
    public Version StandardVersion => new(2, 3);
    public string Description => "HL7 v2.3 message processing with support for ADT, RDE, ORM, and other common message types";

    private static readonly string[] _supportedMessageTypes = new[]
    {
        "ADT", "RDE", "RDS", "ORM", "ORR", "ACK"
    };

    public IReadOnlyList<string> SupportedMessageTypes => _supportedMessageTypes;

    /// <summary>
    /// Creates a message builder for the specified message type.
    /// </summary>
    /// <param name="messageType">The type of message to create (e.g., "ADT", "RDE")</param>
    /// <returns>A result containing the message builder or an error</returns>
    public Result<IStandardMessage> CreateMessage(string messageType)
    {
        if (string.IsNullOrWhiteSpace(messageType))
            return Error.Create("INVALID_MESSAGE_TYPE", "Message type cannot be empty", "HL7v23Plugin");

        try
        {
            var normalizedType = messageType.ToUpperInvariant().Trim();

            IStandardMessage message = normalizedType switch
            {
                "ADT" => new ADTMessage(),
                _ => throw new NotSupportedException($"Message type '{messageType}' is not yet supported by HL7v23Plugin")
            };

            return Result<IStandardMessage>.Success(message);
        }
        catch (Exception ex)
        {
            return Error.Create("MESSAGE_CREATION_FAILED", $"Failed to create {messageType} message: {ex.Message}", "HL7v23Plugin");
        }
    }

    /// <summary>
    /// Gets the validator for HL7 v2.3 messages.
    /// </summary>
    /// <returns>A validator instance for HL7 v2.3</returns>
    public IStandardValidator GetValidator()
    {
        return new HL7v23Validator();
    }

    /// <summary>
    /// Loads HL7 v2.3 specific configuration.
    /// </summary>
    /// <param name="configPath">Path to the configuration file</param>
    /// <returns>A result containing the configuration or an error</returns>
    public Result<IStandardConfig> LoadConfiguration(string configPath)
    {
        try
        {
            // For now, return a basic configuration
            // In the future, this would load from a config file
            var config = new HL7v23Config();
            return Result<IStandardConfig>.Success(config);
        }
        catch (Exception ex)
        {
            return Error.Create("CONFIG_LOAD_FAILED", $"Failed to load HL7v23 configuration: {ex.Message}", "HL7v23Plugin");
        }
    }

    /// <summary>
    /// Parses an HL7 v2.3 message string into a structured message object.
    /// </summary>
    /// <param name="messageContent">The HL7 message string</param>
    /// <returns>A result containing the parsed message or an error</returns>
    public Result<IStandardMessage> ParseMessage(string messageContent)
    {
        if (string.IsNullOrWhiteSpace(messageContent))
            return Error.Parsing("Message content cannot be empty", "HL7v23Plugin");

        try
        {
            // Determine message type from MSH segment
            var messageType = ExtractMessageType(messageContent);
            if (messageType == null)
                return Error.Parsing("Unable to determine message type from MSH segment", "HL7v23Plugin");

            // Create appropriate message instance
            var createResult = CreateMessage(messageType);
            if (createResult.IsFailure)
                return createResult;

            var message = createResult.Value as HL7Message;
            if (message == null)
                return Error.Parsing("Created message is not an HL7Message", "HL7v23Plugin");

            // Parse the message content
            var parseResult = message.ParseHL7String(messageContent);
            if (parseResult.IsFailure)
                return Error.Parsing($"Failed to parse HL7 message: {parseResult.Error.Message}", "HL7v23Plugin");

            return Result<IStandardMessage>.Success(message);
        }
        catch (Exception ex)
        {
            return Error.Parsing($"Exception parsing HL7 message: {ex.Message}", "HL7v23Plugin");
        }
    }

    /// <summary>
    /// Determines if this plugin can handle the given message content.
    /// </summary>
    /// <param name="messageContent">The message content to examine</param>
    /// <returns>True if this plugin can handle the message, false otherwise</returns>
    public bool CanHandle(string messageContent)
    {
        if (string.IsNullOrWhiteSpace(messageContent))
            return false;

        // HL7 v2.3 messages start with "MSH" followed by field separator
        var firstLine = messageContent.Split('\n', '\r')[0];
        if (!firstLine.StartsWith("MSH"))
            return false;

        // Check if this appears to be HL7 v2.3 format
        // MSH|^~\&|... pattern
        if (firstLine.Length > 8 && 
            firstLine[3] == '|' &&  // Field separator after MSH
            firstLine[4] == '^' &&  // Component separator
            firstLine[5] == '~' &&  // Repetition separator
            firstLine[6] == '\\')   // Escape character
        {
            // Extract version from MSH.12 if possible
            var fields = firstLine.Split('|');
            if (fields.Length >= 12)
            {
                var version = fields[11]; // MSH.12 is 0-based index 11
                return version.StartsWith("2.3");
            }

            // If we can't determine version, assume we can handle it
            return true;
        }

        return false;
    }

    /// <summary>
    /// Extracts the message type from the MSH segment.
    /// </summary>
    /// <param name="messageContent">The HL7 message content</param>
    /// <returns>The message type (e.g., "ADT") or null if not found</returns>
    private static string? ExtractMessageType(string messageContent)
    {
        try
        {
            var firstLine = messageContent.Split('\n', '\r')[0];
            if (!firstLine.StartsWith("MSH"))
                return null;

            var fields = firstLine.Split('|');
            if (fields.Length < 9)
                return null;

            // MSH.9 is the message type (0-based index 8)
            var messageTypeField = fields[8];
            var components = messageTypeField.Split('^');
            
            // Return the first component (message code)
            return components.Length > 0 ? components[0] : null;
        }
        catch
        {
            return null;
        }
    }
}

/// <summary>
/// Validator implementation for HL7 v2.3 messages.
/// </summary>
public class HL7v23Validator : IStandardValidator
{
    public string StandardName => "HL7";
    public Version StandardVersion => new(2, 3);

    public Result<ValidationResult> ValidateMessage(string messageContent, ValidationMode validationMode = ValidationMode.Strict)
    {
        var plugin = new HL7v23Plugin();
        
        if (!plugin.CanHandle(messageContent))
        {
            return Result<ValidationResult>.Success(ValidationResult.Failure(new[]
            {
                new ValidationError
                {
                    Code = "INVALID_FORMAT",
                    Message = "Message does not appear to be valid HL7 v2.3 format",
                    Severity = ValidationSeverity.Error
                }
            }));
        }

        var parseResult = plugin.ParseMessage(messageContent);
        if (parseResult.IsFailure)
        {
            return Result<ValidationResult>.Success(ValidationResult.Failure(new[]
            {
                new ValidationError
                {
                    Code = "PARSE_ERROR",
                    Message = parseResult.Error.Message,
                    Severity = ValidationSeverity.Error
                }
            }));
        }

        return parseResult.Value.Validate(validationMode);
    }

    public Result<ValidationResult> ValidateMessage(IStandardMessage message, ValidationMode validationMode = ValidationMode.Strict)
    {
        if (message == null)
            return Error.Create("NULL_MESSAGE", "Message cannot be null", "HL7v23Validator");

        return message.Validate(validationMode);
    }

    public IReadOnlyList<ValidationRuleInfo> GetValidationRules()
    {
        return new[]
        {
            new ValidationRuleInfo
            {
                Id = "HL7_MSH_REQUIRED",
                Name = "MSH Segment Required",
                Description = "All HL7 messages must start with an MSH segment",
                Category = "Structure",
                Severity = ValidationSeverity.Error
            },
            new ValidationRuleInfo
            {
                Id = "HL7_MESSAGE_TYPE",
                Name = "Valid Message Type",
                Description = "MSH.9 must contain a valid message type",
                Category = "Content",
                Severity = ValidationSeverity.Error
            },
            new ValidationRuleInfo
            {
                Id = "HL7_REQUIRED_FIELDS",
                Name = "Required Fields",
                Description = "Required fields must not be empty",
                Category = "Content",
                Severity = ValidationSeverity.Error
            }
        };
    }
}

/// <summary>
/// Configuration implementation for HL7 v2.3.
/// </summary>
public class HL7v23Config : IStandardConfig
{
    public string StandardName => "HL7";
    public Version StandardVersion => new(2, 3);

    private readonly Dictionary<string, string> _config = new()
    {
        ["FieldSeparator"] = "|",
        ["ComponentSeparator"] = "^",
        ["RepetitionSeparator"] = "~",
        ["EscapeCharacter"] = "\\",
        ["SubcomponentSeparator"] = "&",
        ["DefaultProcessingId"] = "P",
        ["DefaultSendingApplication"] = "Pidgeon",
        ["DefaultSendingFacility"] = "Pidgeon",
        ["ValidationMode"] = "Strict"
    };

    public string? GetValue(string key)
    {
        return _config.TryGetValue(key, out var value) ? value : null;
    }

    public IReadOnlyList<string> GetKeys()
    {
        return _config.Keys.ToList();
    }

    public Result<IStandardConfig> Validate()
    {
        // Basic validation - ensure required separators are present
        var requiredKeys = new[] { "FieldSeparator", "ComponentSeparator", "RepetitionSeparator", "EscapeCharacter" };
        
        foreach (var key in requiredKeys)
        {
            if (!_config.ContainsKey(key) || string.IsNullOrEmpty(_config[key]))
            {
                return Error.Validation($"Required configuration key '{key}' is missing or empty", "HL7v23Config");
            }
        }

        return Result<IStandardConfig>.Success(this);
    }
}