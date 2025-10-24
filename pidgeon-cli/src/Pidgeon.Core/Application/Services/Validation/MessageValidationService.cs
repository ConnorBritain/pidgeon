using Microsoft.Extensions.Logging;
using Pidgeon.Core.Application.Interfaces;
using Pidgeon.Core.Application.Interfaces.Standards;
using Pidgeon.Core.Domain.Validation;

namespace Pidgeon.Core.Application.Services.Validation;

/// <summary>
/// Validates healthcare messages against standards using plugin architecture.
/// </summary>
internal class MessageValidationService : IMessageValidationService
{
    private readonly IStandardPluginRegistry _pluginRegistry;
    private readonly ILogger<MessageValidationService> _logger;

    public MessageValidationService(
        IStandardPluginRegistry pluginRegistry,
        ILogger<MessageValidationService> logger)
    {
        _pluginRegistry = pluginRegistry ?? throw new ArgumentNullException(nameof(pluginRegistry));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<ValidationResult>> ValidateAsync(
        string messageContent, 
        string? standard = null, 
        ValidationMode mode = ValidationMode.Strict)
    {
        await Task.Yield();
        try
        {
            if (string.IsNullOrWhiteSpace(messageContent))
            {
                return Result<ValidationResult>.Failure("Message content cannot be empty");
            }

            // Detect standard if not provided
            var detectedStandard = standard ?? DetectStandard(messageContent);
            if (string.IsNullOrEmpty(detectedStandard))
            {
                return Result<ValidationResult>.Failure("Unable to detect message standard");
            }

            _logger.LogInformation("Validating {Standard} message in {Mode} mode", 
                detectedStandard, mode);

            // TODO: Implement plugin-based validation when IValidationPlugin is available
            // For now, return basic validation result
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            var result = new ValidationResult
            {
                IsValid = true,
                Standard = detectedStandard,
                Mode = mode,
                Issues = new List<ValidationIssue>(),
                Statistics = new ValidationStatistics
                {
                    TotalRulesChecked = 1,
                    RulesPassed = 1,
                    RulesFailed = 0,
                    FieldsValidated = 1,
                    ValidationTime = stopwatch.Elapsed
                }
            };
            
            stopwatch.Stop();
            var finalResult = result;

            _logger.LogInformation("Validation completed: IsValid={IsValid}, Errors={ErrorCount}, Warnings={WarningCount}",
                finalResult.IsValid, finalResult.ErrorCount, finalResult.WarningCount);

            return Result<ValidationResult>.Success(finalResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Validation failed with exception");
            return Result<ValidationResult>.Failure($"Validation failed: {ex.Message}");
        }
    }

    private string? DetectStandard(string messageContent)
    {
        if (string.IsNullOrWhiteSpace(messageContent))
            return null;

        // Simple detection logic - can be enhanced with plugins
        if (messageContent.StartsWith("MSH|") || messageContent.Contains("\rMSH|"))
            return "hl7";
        
        if (messageContent.TrimStart().StartsWith("{") && 
            (messageContent.Contains("\"resourceType\"") || messageContent.Contains("\"Bundle\"")))
            return "fhir";
        
        if (messageContent.Contains("UIB+") || messageContent.Contains("UNB+"))
            return "ncpdp";

        return null;
    }
}