// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Pidgeon.Core.Application.Interfaces.Standards;
using Pidgeon.Core;
using Pidgeon.Core.Standards.HL7v24Plugin;
using Pidgeon.Core.Application.Common;

namespace Pidgeon.Core.Standards.HL7v24Plugin;

/// <summary>
/// HL7 v2.4 standard plugin implementation.
/// Handles v2.4-specific parsing, validation, and serialization logic.
/// </summary>
public class HL7v24Plugin : IStandardPlugin
{
    /// <inheritdoc />
    public string StandardName => "HL7";

    /// <inheritdoc />
    public Version StandardVersion => new(2, 4);

    /// <inheritdoc />
    public string Description => "HL7 v2.4 healthcare messaging standard support";

    /// <inheritdoc />
    public IReadOnlyList<string> SupportedMessageTypes => new[]
    {
        "ADT", // Admit/Discharge/Transfer
        "ORM", // Order Entry
        "RDE", // Pharmacy Encoded Order
        "ORU", // Observation Result
        "ACK", // General Acknowledgment
        "QBP", // Query by Parameter
        "RSP"  // Response to Query
    }.AsReadOnly();

    /// <summary>
    /// Gets the message factory for creating HL7 v2.4 messages.
    /// </summary>
    public IStandardMessageFactory MessageFactory => new HL7v24MessageFactory(this);

    /// <inheritdoc />
    public Result<IStandardMessage> CreateMessage(string messageType)
    {
        // TODO: Implement message creation based on messageType
        return Result<IStandardMessage>.Failure($"Message creation for type '{messageType}' not yet implemented in HL7v24Plugin");
    }

    /// <inheritdoc />
    public IStandardValidator GetValidator()
    {
        return new HL7v24Validator();
    }

    /// <inheritdoc />
    public Result<IStandardConfig> LoadConfiguration(string configPath)
    {
        // TODO: Load HL7 v2.4 specific configuration
        return Result<IStandardConfig>.Failure($"Configuration loading not yet implemented in HL7v24Plugin");
    }

    /// <inheritdoc />
    public Result<IStandardMessage> ParseMessage(string messageContent)
    {
        // TODO: Parse HL7 v2.4 message content
        return Result<IStandardMessage>.Failure($"Message parsing not yet implemented in HL7v24Plugin");
    }

    /// <inheritdoc />
    public bool CanHandle(string messageContent)
    {
        // Basic HL7 v2.4 detection - messages start with MSH
        if (string.IsNullOrWhiteSpace(messageContent))
            return false;

        var firstLine = messageContent.Split('\n', '\r')[0];
        return firstLine.StartsWith("MSH|");
    }
}

/// <summary>
/// Validator implementation for HL7 v2.4 messages.
/// </summary>
public class HL7v24Validator : IStandardValidator
{
    public string StandardName => "HL7";
    public Version StandardVersion => new(2, 4);

    public Result<ValidationResult> ValidateMessage(string messageContent, ValidationMode validationMode = ValidationMode.Strict)
    {
        var plugin = new HL7v24Plugin();
        
        if (!plugin.CanHandle(messageContent))
        {
            return Result<ValidationResult>.Success(ValidationResult.Failure(new[]
            {
                new ValidationError
                {
                    Code = "INVALID_FORMAT",
                    Message = "Message does not appear to be valid HL7 v2.4 format",
                    Severity = ValidationSeverity.Error
                }
            }));
        }

        // Basic HL7 v2.4 structure validation
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
            
            // Check required MSH fields
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
            
            // Validate version field (MSH.12)
            if (fields.Length > 12)
            {
                var versionField = fields[12];
                if (!string.IsNullOrWhiteSpace(versionField) && !versionField.StartsWith("2.4"))
                {
                    if (validationMode == ValidationMode.Strict)
                    {
                        errors.Add(new ValidationError
                        {
                            Code = "MSH_VERSION_MISMATCH",
                            Message = $"MSH.12 indicates version '{versionField}', expected v2.4",
                            Severity = ValidationSeverity.Error
                        });
                    }
                    else
                    {
                        errors.Add(new ValidationError
                        {
                            Code = "MSH_VERSION_MISMATCH",
                            Message = $"MSH.12 indicates version '{versionField}', expected v2.4",
                            Severity = ValidationSeverity.Warning
                        });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            errors.Add(new ValidationError
            {
                Code = "VALIDATION_ERROR",
                Message = $"Error during validation: {ex.Message}",
                Severity = ValidationSeverity.Error
            });
        }
        
        var result = errors.Count == 0 ? ValidationResult.Success() : ValidationResult.Failure(errors);
        return Result<ValidationResult>.Success(result);
    }

    public Result<ValidationResult> ValidateMessage(IStandardMessage message, ValidationMode validationMode = ValidationMode.Strict)
    {
        if (message == null)
            return Error.Create("NULL_MESSAGE", "Message cannot be null", "HL7v24Validator");

        return message.Validate(validationMode);
    }

    public IReadOnlyList<ValidationRuleInfo> GetValidationRules()
    {
        return new[]
        {
            new ValidationRuleInfo
            {
                Id = "HL7v24_MSH_REQUIRED",
                Name = "MSH Segment Required",
                Description = "All HL7 v2.4 messages must start with an MSH segment",
                Category = "Structure",
                Severity = ValidationSeverity.Error
            },
            new ValidationRuleInfo
            {
                Id = "HL7v24_MESSAGE_TYPE",
                Name = "Valid Message Type",
                Description = "MSH.9 must contain a valid HL7 v2.4 message type",
                Category = "Content",
                Severity = ValidationSeverity.Error
            },
            new ValidationRuleInfo
            {
                Id = "HL7v24_REQUIRED_FIELDS",
                Name = "Required Fields",
                Description = "Required MSH fields must not be empty",
                Category = "Content",
                Severity = ValidationSeverity.Error
            },
            new ValidationRuleInfo
            {
                Id = "HL7v24_VERSION_CHECK",
                Name = "Version Validation",
                Description = "MSH.12 should indicate HL7 v2.4",
                Category = "Content",
                Severity = ValidationSeverity.Warning
            }
        };
    }
}