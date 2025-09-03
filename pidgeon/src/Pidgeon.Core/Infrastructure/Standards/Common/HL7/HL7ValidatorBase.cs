// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Pidgeon.Core.Application.Interfaces.Standards;
using Pidgeon.Core.Application.Common;
using Pidgeon.Core;

namespace Pidgeon.Core.Infrastructure.Standards.Common.HL7;

/// <summary>
/// Base class for HL7 validator implementations across different versions.
/// Provides common validation logic while allowing version-specific customization.
/// </summary>
public abstract class HL7ValidatorBase : IStandardValidator
{
    /// <inheritdoc />
    public string StandardName => "HL7";

    /// <inheritdoc />
    public abstract Version StandardVersion { get; }

    /// <inheritdoc />
    public virtual Result<ValidationResult> ValidateMessage(string messageContent, ValidationMode validationMode = ValidationMode.Strict)
    {
        if (string.IsNullOrWhiteSpace(messageContent))
            return Result<ValidationResult>.Success(ValidationResult.Failure(new[]
            {
                new ValidationError
                {
                    Code = "EMPTY_MESSAGE",
                    Message = "Message content cannot be empty",
                    Severity = ValidationSeverity.Error
                }
            }));

        if (!CanHandle(messageContent))
        {
            return Result<ValidationResult>.Success(ValidationResult.Failure(new[]
            {
                new ValidationError
                {
                    Code = "INVALID_FORMAT",
                    Message = $"Message does not appear to be valid HL7 v{StandardVersion.Major}.{StandardVersion.Minor} format",
                    Severity = ValidationSeverity.Error
                }
            }));
        }

        var errors = new List<ValidationError>();

        // Common HL7 structure validation
        errors.AddRange(ValidateHL7Structure(messageContent));

        // Version-specific validation
        var versionSpecificResult = ValidateVersionSpecific(messageContent, validationMode);
        if (versionSpecificResult.IsFailure)
        {
            return Result<ValidationResult>.Failure(versionSpecificResult.Error);
        }
        errors.AddRange(versionSpecificResult.Value);

        var result = errors.Count == 0 ? ValidationResult.Success() : ValidationResult.Failure(errors);
        return Result<ValidationResult>.Success(result);
    }

    /// <inheritdoc />
    public virtual Result<ValidationResult> ValidateMessage(IStandardMessage message, ValidationMode validationMode = ValidationMode.Strict)
    {
        if (message == null)
            return Error.Create("NULL_MESSAGE", "Message cannot be null", GetType().Name);

        return message.Validate(validationMode);
    }

    /// <inheritdoc />
    public virtual IReadOnlyList<ValidationRuleInfo> GetValidationRules()
    {
        var commonRules = GetCommonValidationRules().ToList();
        var versionSpecificRules = GetVersionSpecificValidationRules();
        
        commonRules.AddRange(versionSpecificRules);
        return commonRules;
    }

    /// <summary>
    /// Determines whether this validator can handle the given message content.
    /// Default implementation checks for MSH segment start.
    /// </summary>
    protected virtual bool CanHandle(string messageContent)
    {
        if (string.IsNullOrWhiteSpace(messageContent))
            return false;

        var firstLine = messageContent.Split('\n', '\r')[0];
        return firstLine.StartsWith("MSH|");
    }

    /// <summary>
    /// Validates HL7 message structure common to all versions.
    /// </summary>
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
    /// Performs version-specific validation logic while maintaining Result<T> pattern.
    /// Override in derived classes to implement version-specific rules.
    /// </summary>
    /// <param name="messageContent">Message content to validate</param>
    /// <param name="validationMode">Validation mode (strict/compatibility)</param>
    /// <returns>Result containing version-specific validation errors</returns>
    protected virtual Result<List<ValidationError>> ValidateVersionSpecific(string messageContent, ValidationMode validationMode)
    {
        // Default implementation - no version-specific validation
        return Result<List<ValidationError>>.Success(new List<ValidationError>());
    }

    /// <summary>
    /// Gets validation rules specific to this HL7 version.
    /// Override in derived classes to provide version-specific rules.
    /// </summary>
    protected virtual IReadOnlyList<ValidationRuleInfo> GetVersionSpecificValidationRules()
    {
        // Default implementation - no version-specific rules
        return Array.Empty<ValidationRuleInfo>();
    }

    /// <summary>
    /// Gets common validation rules that apply to all HL7 versions.
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