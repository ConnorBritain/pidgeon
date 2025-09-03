// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Pidgeon.Core.Application.Interfaces.Standards;
using Pidgeon.Core.Application.Common;
using Pidgeon.Core;

namespace Pidgeon.Core.Infrastructure.Standards.Common.HL7;

/// <summary>
/// Base class for HL7 plugin implementations across different versions.
/// Provides common functionality while allowing version-specific customization.
/// </summary>
public abstract class HL7PluginBase : IStandardPlugin
{
    /// <inheritdoc />
    public string StandardName => "HL7";

    /// <inheritdoc />
    public abstract Version StandardVersion { get; }

    /// <inheritdoc />
    public abstract string Description { get; }

    /// <inheritdoc />
    public abstract IReadOnlyList<string> SupportedMessageTypes { get; }

    /// <inheritdoc />
    public abstract IStandardMessageFactory MessageFactory { get; }

    /// <inheritdoc />
    public abstract Result<IStandardMessage> CreateMessage(string messageType);

    /// <inheritdoc />
    public abstract IStandardValidator GetValidator();

    /// <inheritdoc />
    public abstract Result<IStandardConfig> LoadConfiguration(string configPath);

    /// <inheritdoc />
    public abstract Result<IStandardMessage> ParseMessage(string messageContent);

    /// <inheritdoc />
    public virtual bool CanHandle(string messageContent)
    {
        // Common HL7 detection logic - all versions start with MSH
        if (string.IsNullOrWhiteSpace(messageContent))
            return false;

        var firstLine = messageContent.Split('\n', '\r')[0];
        return firstLine.StartsWith("MSH|");
    }

    /// <summary>
    /// Creates a base HL7 message with common fields populated.
    /// Version-specific plugins can override for customization.
    /// </summary>
    /// <param name="messageType">The HL7 message type</param>
    /// <returns>Base message configuration</returns>
    protected virtual Dictionary<string, object> CreateBaseMessageConfig(string messageType)
    {
        return new Dictionary<string, object>
        {
            ["MessageControlId"] = Guid.NewGuid().ToString(),
            ["Timestamp"] = DateTime.UtcNow,
            ["SendingSystem"] = "PIDGEON",
            ["ReceivingSystem"] = "UNKNOWN",
            ["Standard"] = "HL7",
            ["Version"] = StandardVersion.ToString(2)
        };
    }

    /// <summary>
    /// Validates that message content is valid HL7 format.
    /// Provides common validation logic that version-specific validators can extend.
    /// </summary>
    /// <param name="messageContent">Message content to validate</param>
    /// <returns>List of validation errors found</returns>
    protected virtual List<ValidationError> ValidateHL7Structure(string messageContent)
    {
        var errors = new List<ValidationError>();

        try
        {
            var firstLine = messageContent.Split('\n', '\r')[0];
            var fields = firstLine.Split('|');

            // MSH segment validation
            if (fields.Length < 12)
            {
                errors.Add(new ValidationError
                {
                    Code = "MSH_INCOMPLETE",
                    Message = "MSH segment must contain at least 12 fields",
                    Severity = ValidationSeverity.Error
                });
            }

            // Check required MSH fields that are common across HL7 versions
            if (fields.Length > 3 && string.IsNullOrWhiteSpace(fields[3]))
            {
                errors.Add(new ValidationError
                {
                    Code = "MSH_SENDING_APPLICATION_MISSING",
                    Message = "MSH.3 (Sending Application) is required",
                    Severity = ValidationSeverity.Error
                });
            }

            if (fields.Length > 5 && string.IsNullOrWhiteSpace(fields[5]))
            {
                errors.Add(new ValidationError
                {
                    Code = "MSH_RECEIVING_APPLICATION_MISSING", 
                    Message = "MSH.5 (Receiving Application) is required",
                    Severity = ValidationSeverity.Error
                });
            }

            // Validate message type field (MSH.9)
            if (fields.Length > 9 && string.IsNullOrWhiteSpace(fields[9]))
            {
                errors.Add(new ValidationError
                {
                    Code = "MSH_MESSAGE_TYPE_MISSING",
                    Message = "MSH.9 (Message Type) is required",
                    Severity = ValidationSeverity.Error
                });
            }
        }
        catch (Exception ex)
        {
            errors.Add(new ValidationError
            {
                Code = "STRUCTURE_VALIDATION_ERROR",
                Message = $"Error during structure validation: {ex.Message}",
                Severity = ValidationSeverity.Error
            });
        }

        return errors;
    }

    /// <summary>
    /// Gets common validation rules that apply to all HL7 versions.
    /// Version-specific validators can extend this list.
    /// </summary>
    protected virtual IReadOnlyList<ValidationRuleInfo> GetCommonValidationRules()
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
                Description = "MSH.9 must contain a valid HL7 message type",
                Category = "Content",
                Severity = ValidationSeverity.Error
            },
            new ValidationRuleInfo
            {
                Id = "HL7_REQUIRED_FIELDS",
                Name = "Required Fields",
                Description = "Required MSH fields must not be empty",
                Category = "Content", 
                Severity = ValidationSeverity.Error
            }
        };
    }
}