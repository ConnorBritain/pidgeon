// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Pidgeon.Core;
using Pidgeon.Core.Application.Interfaces.Validation;
using Pidgeon.Core.Application.Common;
using Pidgeon.Core.Application.Interfaces.Standards;
using Microsoft.Extensions.Logging;

namespace Pidgeon.Core.Application.Services.Validation;

internal class ValidationService : IValidationService
{
    private readonly IStandardPluginRegistry _pluginRegistry;
    private readonly ILogger<ValidationService> _logger;

    public ValidationService(
        IStandardPluginRegistry pluginRegistry,
        ILogger<ValidationService> logger)
    {
        _pluginRegistry = pluginRegistry ?? throw new ArgumentNullException(nameof(pluginRegistry));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<ValidationResult>> ValidateAsync(string messageContent, ValidationMode validationMode = ValidationMode.Strict, string? standard = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(messageContent))
            {
                return Result<ValidationResult>.Success(ValidationResult.Failure(new[]
                {
                    new ValidationError
                    {
                        Code = "EMPTY_MESSAGE",
                        Message = "Message content cannot be empty",
                        Severity = ValidationSeverity.Error
                    }
                }));
            }

            _logger.LogDebug("Validating message with validation mode: {ValidationMode}", validationMode);

            // If specific standard is provided, use that plugin
            if (!string.IsNullOrWhiteSpace(standard))
            {
                var specificPlugin = _pluginRegistry.GetPlugin(standard);
                if (specificPlugin != null)
                {
                    return await ValidateWithPlugin(specificPlugin, messageContent, validationMode);
                }
                else
                {
                    _logger.LogWarning("No plugin found for specified standard: {Standard}", standard);
                    return Result<ValidationResult>.Success(ValidationResult.Failure(new[]
                    {
                        new ValidationError
                        {
                            Code = "PLUGIN_NOT_FOUND",
                            Message = $"No validation plugin available for standard: {standard}",
                            Severity = ValidationSeverity.Error
                        }
                    }));
                }
            }

            // Auto-detect standard by trying all available plugins
            var availablePlugins = _pluginRegistry.GetAllPlugins();
            foreach (var plugin in availablePlugins)
            {
                if (plugin.CanHandle(messageContent))
                {
                    _logger.LogDebug("Auto-detected standard: {Standard} v{Version}", plugin.StandardName, plugin.StandardVersion);
                    return await ValidateWithPlugin(plugin, messageContent, validationMode);
                }
            }

            // No plugin could handle the message
            return Result<ValidationResult>.Success(ValidationResult.Failure(new[]
            {
                new ValidationError
                {
                    Code = "UNSUPPORTED_FORMAT",
                    Message = "Message format is not supported by any available validation plugins",
                    Severity = ValidationSeverity.Error
                }
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during message validation");
            return Result<ValidationResult>.Failure($"Validation failed due to unexpected error: {ex.Message}");
        }
    }

    private async Task<Result<ValidationResult>> ValidateWithPlugin(IStandardPlugin plugin, string messageContent, ValidationMode validationMode)
    {
        try
        {
            var validator = plugin.GetValidator();
            var validationResult = validator.ValidateMessage(messageContent, validationMode);
            
            _logger.LogDebug("Validation completed for {Standard} v{Version} with {ErrorCount} errors", 
                plugin.StandardName, plugin.StandardVersion, 
                validationResult.IsSuccess ? validationResult.Value.Errors.Count : -1);

            return await Task.FromResult(validationResult);
        }
        catch (NotImplementedException)
        {
            _logger.LogWarning("Validator not implemented for {Standard} v{Version}", plugin.StandardName, plugin.StandardVersion);
            return Result<ValidationResult>.Success(ValidationResult.Failure(new[]
            {
                new ValidationError
                {
                    Code = "VALIDATOR_NOT_IMPLEMENTED",
                    Message = $"Validation not yet implemented for {plugin.StandardName} v{plugin.StandardVersion}",
                    Severity = ValidationSeverity.Warning
                }
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Plugin validation failed for {Standard} v{Version}", plugin.StandardName, plugin.StandardVersion);
            return Result<ValidationResult>.Failure($"Plugin validation failed: {ex.Message}");
        }
    }
}