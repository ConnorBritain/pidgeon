// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Microsoft.Extensions.Logging;
using Pidgeon.Core.Domain.Configuration.Entities;

namespace Pidgeon.Core.Application.Services.Configuration;

/// <summary>
/// Standard-agnostic configuration inference service that orchestrates vendor pattern analysis.
/// Pure coordinator that delegates ALL analysis operations to appropriate plugin-based services.
/// Single responsibility: "Coordinate the complete configuration inference workflow."
/// Contains ZERO hardcoded standard logic - follows sacred plugin architecture principle.
/// </summary>
internal class ConfigurationInferenceService : IConfigurationInferenceService
{
    private readonly IVendorDetectionService _vendorDetector;
    private readonly IFieldPatternAnalysisService _fieldAnalyzer;
    private readonly IFormatDeviationDetectionService _deviationDetector;
    private readonly IConfidenceCalculationService _confidenceCalculator;
    private readonly IMessagePatternAnalysisService _messagePatternAnalyzer;
    private readonly ILogger<ConfigurationInferenceService> _logger;

    public ConfigurationInferenceService(
        IVendorDetectionService vendorDetector,
        IFieldPatternAnalysisService fieldAnalyzer,
        IFormatDeviationDetectionService deviationDetector,
        IConfidenceCalculationService confidenceCalculator,
        IMessagePatternAnalysisService messagePatternAnalyzer,
        ILogger<ConfigurationInferenceService> logger)
    {
        _vendorDetector = vendorDetector ?? throw new ArgumentNullException(nameof(vendorDetector));
        _fieldAnalyzer = fieldAnalyzer ?? throw new ArgumentNullException(nameof(fieldAnalyzer));
        _deviationDetector = deviationDetector ?? throw new ArgumentNullException(nameof(deviationDetector));
        _confidenceCalculator = confidenceCalculator ?? throw new ArgumentNullException(nameof(confidenceCalculator));
        _messagePatternAnalyzer = messagePatternAnalyzer ?? throw new ArgumentNullException(nameof(messagePatternAnalyzer));
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<VendorConfiguration>> InferConfigurationAsync(
        IEnumerable<string> sampleMessages, 
        ConfigurationAddress address, 
        InferenceOptions? options = null)
    {
        try
        {
            var messageList = sampleMessages.ToList();
            _logger.LogInformation("Starting configuration inference for {Address} with {MessageCount} sample messages",
                address, messageList.Count);

            if (messageList.Count == 0)
                return Result<VendorConfiguration>.Failure("No sample messages provided for configuration inference");

            // Step 1: Vendor signature detection (delegates to plugins)
            _logger.LogDebug("Step 1: Detecting vendor signature for {Standard}", address.Standard);
            var vendorResult = await DetectVendorSignatureFromSamples(messageList, address.Standard);
            if (vendorResult.IsFailure)
            {
                _logger.LogWarning("Vendor detection failed: {Error}", vendorResult.Error);
                // Continue with unknown vendor - don't fail the entire process
            }

            // Step 2: Field pattern analysis (delegates to plugins)
            _logger.LogDebug("Step 2: Analyzing field patterns for {Standard} {MessageType}", 
                address.Standard, address.MessageType);
            var patternsResult = await _fieldAnalyzer.AnalyzeAsync(
                messageList, address.Standard, address.MessageType);
            if (patternsResult.IsFailure)
                return Result<VendorConfiguration>.Failure($"Field pattern analysis failed: {patternsResult.Error}");

            // Step 3: Format deviation detection (delegates to plugins)
            _logger.LogDebug("Step 3: Detecting format deviations for {Standard}", address.Standard);
            var deviationsResult = await _deviationDetector.DetectEncodingDeviationsAsync(
                messageList, address.Standard);
            if (deviationsResult.IsFailure)
            {
                _logger.LogWarning("Format deviation detection failed: {Error}", deviationsResult.Error);
                // Continue without deviations - don't fail the entire process
            }

            // Step 4: Statistical confidence calculation (delegates to plugins)
            _logger.LogDebug("Step 4: Calculating confidence score for {MessageCount} samples", messageList.Count);
            var confidenceResult = await _confidenceCalculator.CalculateFieldPatternConfidenceAsync(
                patternsResult.Value, messageList.Count);
            if (confidenceResult.IsFailure)
            {
                _logger.LogWarning("Confidence calculation failed: {Error}", confidenceResult.Error);
                // Continue with default confidence - don't fail the entire process
            }

            // Step 5: Overall confidence calculation if we have vendor confidence
            double overallConfidence = confidenceResult.IsSuccess ? confidenceResult.Value : 0.5;
            if (vendorResult.IsSuccess && confidenceResult.IsSuccess)
            {
                var overallResult = await _confidenceCalculator.CalculateOverallConfidenceAsync(
                    vendorResult.Value.Confidence,
                    confidenceResult.Value,
                    messageList.Count);
                
                if (overallResult.IsSuccess)
                    overallConfidence = overallResult.Value;
            }

            // Step 6: Assemble the final vendor configuration
            var configuration = new VendorConfiguration
            {
                Address = address,
                Signature = vendorResult.IsSuccess ? vendorResult.Value : CreateUnknownVendorSignature(address.Standard),
                FieldPatterns = patternsResult.Value,
                FormatDeviations = deviationsResult.IsSuccess ? deviationsResult.Value.ToList() : new List<FormatDeviation>(),
                Metadata = new ConfigurationMetadata
                {
                    MessagesSampled = messageList.Count,
                    Confidence = overallConfidence,
                    FirstSeen = DateTime.UtcNow,
                    LastUpdated = DateTime.UtcNow,
                    Version = "1.0",
                    Changes = new List<ConfigurationChange>
                    {
                        new ConfigurationChange
                        {
                            ChangeType = ConfigurationChangeType.Created,
                            Description = $"Initial configuration inference from {messageList.Count} {address.Standard} {address.MessageType} messages",
                            ChangeDate = DateTime.UtcNow,
                            ConfidenceImpact = overallConfidence
                        }
                    }
                }
            };

            _logger.LogInformation("Configuration inference completed for {Address} with {Confidence:P1} confidence",
                address, configuration.Metadata.Confidence);

            return Result<VendorConfiguration>.Success(configuration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during configuration inference for {Address}", address);
            return Result<VendorConfiguration>.Failure($"Configuration inference failed: {ex.Message}");
        }
    }

    #region Private Helper Methods - Standard Agnostic Only

    /// <summary>
    /// Detects vendor signature from the first valid message in the samples.
    /// Pure delegation to vendor detection service (which uses plugins).
    /// </summary>
    private async Task<Result<VendorSignature>> DetectVendorSignatureFromSamples(
        IEnumerable<string> messages,
        string standard)
    {
        try
        {
            foreach (var message in messages)
            {
                if (string.IsNullOrWhiteSpace(message))
                    continue;

                // Delegate to vendor detection service (which delegates to plugins)
                var result = await _vendorDetector.DetectFromMessageAsync(message, standard);
                if (result.IsSuccess)
                {
                    _logger.LogDebug("Vendor signature detected from sample message");
                    return result;
                }
            }

            _logger.LogWarning("No vendor signature could be detected from {Standard} sample messages", standard);
            return Result<VendorSignature>.Failure("No valid messages found for vendor signature detection");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting vendor signature from samples");
            return Result<VendorSignature>.Failure($"Vendor signature detection failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Creates an unknown vendor signature when vendor detection fails.
    /// Standard-agnostic fallback.
    /// </summary>
    private VendorSignature CreateUnknownVendorSignature(string standard)
    {
        return new VendorSignature
        {
            Name = "Unknown",
            Version = string.Empty,
            SendingApplication = string.Empty,
            SendingFacility = string.Empty,
            Confidence = 0.3, // Low confidence for unknown vendor
            DetectionMethod = $"Fallback - No {standard} Vendor Detected",
            Deviations = new List<FormatDeviation>()
        };
    }

    #endregion
}