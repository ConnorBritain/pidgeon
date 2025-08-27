// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Microsoft.Extensions.Logging;
using Pidgeon.Core.Domain.Configuration.Services;
using Pidgeon.Core.Domain.Configuration.Entities;

namespace Pidgeon.Core.Standards.HL7.v23.Configuration;

/// <summary>
/// HL7 v2.3 configuration analysis plugin.
/// Thin orchestration layer that coordinates specialized domain services
/// for HL7-specific configuration analysis workflows.
/// Follows sacred principle: Plugin orchestrates, services implement.
/// </summary>
internal class HL7ConfigurationPlugin : IConfigurationPlugin
{
    private readonly IVendorDetectionService _vendorDetector;
    private readonly IFieldPatternAnalyzer _fieldAnalyzer;
    private readonly IFormatDeviationDetector _deviationDetector;
    private readonly IConfidenceCalculator _confidenceCalculator;
    private readonly IConfigurationValidator _validator;
    private readonly ILogger<HL7ConfigurationPlugin> _logger;

    public HL7ConfigurationPlugin(
        IVendorDetectionService vendorDetector,
        IFieldPatternAnalyzer fieldAnalyzer,
        IFormatDeviationDetector deviationDetector,
        IConfidenceCalculator confidenceCalculator,
        IConfigurationValidator validator,
        ILogger<HL7ConfigurationPlugin> logger)
    {
        _vendorDetector = vendorDetector;
        _fieldAnalyzer = fieldAnalyzer;
        _deviationDetector = deviationDetector;
        _confidenceCalculator = confidenceCalculator;
        _validator = validator;
        _logger = logger;
    }

    /// <inheritdoc />
    public string StandardName => "HL7v23";

    /// <inheritdoc />
    public bool CanHandle(ConfigurationAddress address)
    {
        return address.Standard.Equals("HL7v23", StringComparison.OrdinalIgnoreCase) ||
               address.Standard.Equals("HL7", StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public async Task<Result<VendorConfiguration>> AnalyzeMessagesAsync(
        IEnumerable<string> messages,
        ConfigurationAddress address,
        InferenceOptions? options = null)
    {
        try
        {
            var messageList = messages.ToList();
            _logger.LogInformation("Starting HL7 configuration analysis for {MessageCount} messages at {Address}", 
                messageList.Count, address);

            // Step 1: Detect vendor signature from first valid message
            var vendorResult = await DetectVendorSignatureFromMessages(messageList);
            if (vendorResult.IsFailure)
                return Result<VendorConfiguration>.Failure($"Vendor detection failed: {vendorResult.Error}");

            // Step 2: Analyze field patterns across all messages
            var patternsResult = await _fieldAnalyzer.AnalyzeAsync(messageList, address.Standard, address.MessageType);
            if (patternsResult.IsFailure)
                return Result<VendorConfiguration>.Failure($"Field pattern analysis failed: {patternsResult.Error}");

            // Step 3: Detect format deviations
            var deviationsResult = await _deviationDetector.DetectEncodingDeviationsAsync(messageList, address.Standard);
            if (deviationsResult.IsFailure)
                _logger.LogWarning("Format deviation detection failed: {Error}", deviationsResult.Error);

            // Step 4: Calculate confidence score
            var confidenceResult = await _confidenceCalculator.CalculateFieldPatternConfidenceAsync(
                patternsResult.Value, messageList.Count);
            if (confidenceResult.IsFailure)
                _logger.LogWarning("Confidence calculation failed: {Error}", confidenceResult.Error);

            // Step 5: Create vendor configuration
            var configuration = new VendorConfiguration
            {
                Address = address,
                Signature = vendorResult.Value,
                FieldPatterns = patternsResult.Value,
                Metadata = new ConfigurationMetadata
                {
                    MessagesSampled = messageList.Count,
                    Confidence = confidenceResult.IsSuccess ? confidenceResult.Value : 0.5,
                    FirstSeen = DateTime.UtcNow,
                    LastUpdated = DateTime.UtcNow,
                    Version = "1.0",
                    Changes = new List<ConfigurationChange>
                    {
                        new ConfigurationChange
                        {
                            ChangeType = ConfigurationChangeType.Created,
                            Description = $"Initial configuration analysis of {messageList.Count} messages",
                            ChangeDate = DateTime.UtcNow,
                            ConfidenceImpact = confidenceResult.IsSuccess ? confidenceResult.Value : 0.0
                        }
                    }
                }
            };

            _logger.LogInformation("HL7 configuration analysis completed successfully for {Address} with confidence {Confidence:P1}",
                address, configuration.Metadata.Confidence);

            return Result<VendorConfiguration>.Success(configuration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during HL7 configuration analysis for {Address}", address);
            return Result<VendorConfiguration>.Failure($"Configuration analysis failed: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result<ConfigurationValidationResult>> ValidateMessageAsync(
        string message,
        VendorConfiguration configuration)
    {
        try
        {
            _logger.LogDebug("Validating HL7 message against configuration {Address}", configuration.Address);

            // Delegate to the configuration validator service
            var result = await _validator.ValidateAsync(message, configuration, ValidationMode.Compatibility);
            
            if (result.IsSuccess)
            {
                _logger.LogDebug("HL7 message validation completed with result: {IsValid}", result.Value.IsValid);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during HL7 message validation");
            return Result<ConfigurationValidationResult>.Failure($"Message validation failed: {ex.Message}");
        }
    }

    #region Private Helper Methods

    /// <summary>
    /// Detects vendor signature from the first valid HL7 message in the collection.
    /// </summary>
    private async Task<Result<VendorSignature>> DetectVendorSignatureFromMessages(IEnumerable<string> messages)
    {
        foreach (var message in messages)
        {
            if (string.IsNullOrWhiteSpace(message) || !IsValidHL7Message(message))
                continue;

            var result = await _vendorDetector.DetectFromMessageAsync(message, StandardName);
            if (result.IsSuccess)
                return result;
        }

        return Result<VendorSignature>.Failure("No valid HL7 messages found for vendor detection");
    }

    /// <summary>
    /// Simple check to determine if a message appears to be HL7 format.
    /// </summary>
    private static bool IsValidHL7Message(string message)
    {
        return !string.IsNullOrWhiteSpace(message) && 
               message.TrimStart().StartsWith("MSH|", StringComparison.OrdinalIgnoreCase);
    }

    #endregion
}