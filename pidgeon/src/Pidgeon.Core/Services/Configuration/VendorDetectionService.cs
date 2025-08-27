// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Microsoft.Extensions.Logging;
using Pidgeon.Core.Standards.Common;
using Pidgeon.Core.Types;

namespace Pidgeon.Core.Services.Configuration;

/// <summary>
/// Standard-agnostic service responsible for detecting vendor signatures from healthcare messages.
/// Orchestrates vendor detection by delegating to standard-specific plugins.
/// Single responsibility: Coordinate vendor detection across all standards.
/// </summary>
internal class VendorDetectionService : IVendorDetectionService
{
    private readonly IVendorPatternRepository _patternRepository;
    private readonly IStandardPluginRegistry _pluginRegistry;
    private readonly ILogger<VendorDetectionService> _logger;

    public VendorDetectionService(
        IVendorPatternRepository patternRepository,
        IStandardPluginRegistry pluginRegistry,
        ILogger<VendorDetectionService> logger)
    {
        _patternRepository = patternRepository ?? throw new ArgumentNullException(nameof(patternRepository));
        _pluginRegistry = pluginRegistry ?? throw new ArgumentNullException(nameof(pluginRegistry));
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<VendorSignature>> DetectFromHeadersAsync(MessageHeaders messageHeaders)
    {
        try
        {
            if (messageHeaders == null)
                return Result<VendorSignature>.Failure("Message headers cannot be null");

            _logger.LogDebug("Detecting vendor from headers: SendingApp={SendingApp}, SendingFacility={SendingFacility}",
                messageHeaders.SendingApplication, messageHeaders.SendingFacility);

            // Get patterns that support this standard
            var patternsResult = await _patternRepository.LoadPatternsForStandardAsync(messageHeaders.Standard);
            if (patternsResult.IsFailure)
            {
                _logger.LogWarning("Failed to load vendor patterns: {Error}", patternsResult.Error);
                return CreateUnknownVendorSignature(messageHeaders);
            }

            var patterns = patternsResult.Value;
            if (patterns.Count == 0)
            {
                _logger.LogWarning("No vendor detection patterns found for standard: {Standard}", messageHeaders.Standard);
                return CreateUnknownVendorSignature(messageHeaders);
            }

            // Try each pattern and find the best match
            VendorSignature? bestMatch = null;
            double bestConfidence = 0.0;

            foreach (var pattern in patterns)
            {
                var matchResult = EvaluatePattern(pattern, messageHeaders);
                if (matchResult.IsSuccess && matchResult.Value > bestConfidence)
                {
                    bestConfidence = matchResult.Value;
                    bestMatch = CreateVendorSignature(pattern, messageHeaders, matchResult.Value);
                    
                    _logger.LogDebug("Found better vendor match: {VendorName} with confidence {Confidence:P1}",
                        pattern.VendorName, matchResult.Value);
                }
            }

            if (bestMatch != null)
            {
                _logger.LogInformation("Detected vendor: {VendorName} with confidence {Confidence:P1}",
                    bestMatch.Name, bestMatch.Confidence);
                return Result<VendorSignature>.Success(bestMatch);
            }

            _logger.LogInformation("No specific vendor detected, using unknown vendor signature");
            return CreateUnknownVendorSignature(messageHeaders);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting vendor from message headers");
            return Result<VendorSignature>.Failure($"Vendor detection failed: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result<VendorSignature>> DetectFromMessageAsync(string message, string standard)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(message))
                return Result<VendorSignature>.Failure("Message cannot be null or empty");
                
            if (string.IsNullOrWhiteSpace(standard))
                return Result<VendorSignature>.Failure("Standard cannot be null or empty");

            _logger.LogDebug("Detecting vendor from {Standard} message", standard);

            // Get the appropriate vendor detection plugin for this standard
            var plugin = _pluginRegistry.GetVendorDetectionPlugin(standard);
            if (plugin == null)
            {
                _logger.LogWarning("No vendor detection plugin found for standard: {Standard}", standard);
                return CreateUnknownVendorSignature(standard);
            }

            // Validate message format using plugin
            if (!plugin.IsValidMessageFormat(message))
            {
                return Result<VendorSignature>.Failure($"Message is not in valid {standard} format");
            }

            // Delegate vendor detection to the standard-specific plugin
            var vendorResult = await plugin.DetectVendorSignatureAsync(message, _patternRepository);
            
            if (vendorResult.IsSuccess)
            {
                _logger.LogInformation("Detected vendor: {VendorName} with confidence {Confidence:P1}",
                    vendorResult.Value.Name, vendorResult.Value.Confidence);
            }
            else
            {
                _logger.LogWarning("Vendor detection failed: {Error}", vendorResult.Error);
            }
            
            return vendorResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting vendor from raw message");
            return Result<VendorSignature>.Failure($"Message-based vendor detection failed: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<VendorDetectionPattern>>> GetPatternsForStandardAsync(string standard)
    {
        return await _patternRepository.LoadPatternsForStandardAsync(standard);
    }

    #region Private Helper Methods

    /// <summary>
    /// Evaluates a vendor detection pattern against message headers.
    /// Standard-agnostic pattern matching based on configured rules.
    /// </summary>
    private Result<double> EvaluatePattern(VendorDetectionPattern pattern, MessageHeaders headers)
    {
        try
        {
            double confidence = pattern.BaseConfidence;

            // Check application patterns
            if (!string.IsNullOrWhiteSpace(headers.SendingApplication))
            {
                foreach (var rule in pattern.ApplicationPatterns)
                {
                    if (EvaluateRule(rule, headers.SendingApplication))
                    {
                        confidence += rule.ConfidenceBoost;
                        _logger.LogTrace("Application rule matched: {Pattern} -> +{Boost}", rule.Pattern, rule.ConfidenceBoost);
                    }
                }
            }

            // Check facility patterns
            if (!string.IsNullOrWhiteSpace(headers.SendingFacility))
            {
                foreach (var rule in pattern.FacilityPatterns)
                {
                    if (EvaluateRule(rule, headers.SendingFacility))
                    {
                        confidence += rule.ConfidenceBoost;
                        _logger.LogTrace("Facility rule matched: {Pattern} -> +{Boost}", rule.Pattern, rule.ConfidenceBoost);
                    }
                }
            }

            // Check message type patterns if available
            if (!string.IsNullOrWhiteSpace(headers.MessageType) && pattern.MessageTypePatterns != null)
            {
                foreach (var rule in pattern.MessageTypePatterns)
                {
                    if (EvaluateRule(rule, headers.MessageType))
                    {
                        confidence += rule.ConfidenceBoost;
                        _logger.LogTrace("Message type rule matched: {Pattern} -> +{Boost}", rule.Pattern, rule.ConfidenceBoost);
                    }
                }
            }

            // Ensure confidence is between 0 and 1
            confidence = Math.Min(1.0, Math.Max(0.0, confidence));

            return Result<double>.Success(confidence);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating vendor pattern");
            return Result<double>.Failure($"Pattern evaluation failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Evaluates a single detection rule against a value.
    /// Supports multiple match types in a standard-agnostic way.
    /// </summary>
    private bool EvaluateRule(DetectionRule rule, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var comparison = rule.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        
        return rule.MatchType switch
        {
            MatchType.Exact => value.Equals(rule.Pattern, comparison),
            MatchType.Contains => value.Contains(rule.Pattern, comparison),
            MatchType.StartsWith => value.StartsWith(rule.Pattern, comparison),
            MatchType.EndsWith => value.EndsWith(rule.Pattern, comparison),
            MatchType.Regex => EvaluateRegexRule(rule, value),
            _ => false
        };
    }

    /// <summary>
    /// Evaluates a regex detection rule.
    /// </summary>
    private bool EvaluateRegexRule(DetectionRule rule, string value)
    {
        try
        {
            var options = rule.CaseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase;
            var regex = new Regex(rule.Pattern, options | RegexOptions.Compiled);
            return regex.IsMatch(value);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Invalid regex pattern: {Pattern}", rule.Pattern);
            return false;
        }
    }

    /// <summary>
    /// Creates a vendor signature from a matched pattern.
    /// </summary>
    private VendorSignature CreateVendorSignature(VendorDetectionPattern pattern, MessageHeaders headers, double confidence)
    {
        // Convert common deviations to format deviations
        var deviations = pattern.CommonDeviations?.Select(cd => new FormatDeviation
        {
            Type = cd.DeviationType,
            Location = cd.Location,
            Description = cd.Description,
            Severity = cd.Severity,
            Frequency = (int)(cd.Frequency * 100), // Convert to percentage
            DetectedValue = string.Empty, // Will be filled during actual detection
            StandardValue = string.Empty
        }).ToList() ?? new List<FormatDeviation>();

        return new VendorSignature
        {
            Name = pattern.VendorName,
            Version = pattern.PatternVersion,
            SendingApplication = headers.SendingApplication,
            SendingFacility = headers.SendingFacility ?? string.Empty,
            Confidence = confidence,
            DetectionMethod = $"Pattern Matching ({pattern.Id})",
            Deviations = deviations
        };
    }

    /// <summary>
    /// Creates a generic vendor signature for unknown vendors from headers.
    /// </summary>
    private Result<VendorSignature> CreateUnknownVendorSignature(MessageHeaders headers)
    {
        var signature = new VendorSignature
        {
            Name = string.IsNullOrWhiteSpace(headers.SendingApplication) ? "Unknown" : headers.SendingApplication,
            Version = string.Empty,
            SendingApplication = headers.SendingApplication ?? string.Empty,
            SendingFacility = headers.SendingFacility ?? string.Empty,
            Confidence = 0.3, // Low confidence for unknown vendors
            DetectionMethod = $"Fallback - Unknown {headers.Standard} Vendor",
            Deviations = new List<FormatDeviation>()
        };

        return Result<VendorSignature>.Success(signature);
    }

    /// <summary>
    /// Creates a generic vendor signature for unknown vendors when no plugin available.
    /// </summary>
    private Result<VendorSignature> CreateUnknownVendorSignature(string standard)
    {
        var signature = new VendorSignature
        {
            Name = "Unknown",
            Version = string.Empty,
            SendingApplication = string.Empty,
            SendingFacility = string.Empty,
            Confidence = 0.1, // Very low confidence when no plugin available
            DetectionMethod = $"No Plugin - Unknown {standard} Vendor",
            Deviations = new List<FormatDeviation>()
        };

        return Result<VendorSignature>.Success(signature);
    }

    #endregion
}