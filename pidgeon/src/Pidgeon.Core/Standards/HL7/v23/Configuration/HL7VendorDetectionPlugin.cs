// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Microsoft.Extensions.Logging;
using Pidgeon.Core.Domain.Configuration.Services;
using Pidgeon.Core.Standards.Common;
using Pidgeon.Core.Domain.Configuration.Entities;
using System.Text.RegularExpressions;
using ConfigMatchType = Pidgeon.Core.Domain.Configuration.Entities.MatchType;

namespace Pidgeon.Core.Standards.HL7.v23.Configuration;

/// <summary>
/// HL7 v2.3 specific vendor detection plugin.
/// Handles MSH segment parsing and vendor signature detection for HL7 messages.
/// All HL7-specific logic lives here, not in core services.
/// </summary>
internal class HL7VendorDetectionPlugin : IStandardVendorDetectionPlugin
{
    private readonly ILogger<HL7VendorDetectionPlugin> _logger;

    // HL7-specific message parsing patterns
    private static readonly Regex MshSegmentPattern = new(@"^MSH\|", RegexOptions.Compiled);
    private static readonly Regex SendingApplicationPattern = new(@"MSH\|[^|]*\|([^|]*)\|", RegexOptions.Compiled);
    private static readonly Regex SendingFacilityPattern = new(@"MSH\|[^|]*\|[^|]*\|([^|]*)\|", RegexOptions.Compiled);
    private static readonly Regex ReceivingApplicationPattern = new(@"MSH\|[^|]*\|[^|]*\|[^|]*\|([^|]*)\|", RegexOptions.Compiled);
    private static readonly Regex ReceivingFacilityPattern = new(@"MSH\|[^|]*\|[^|]*\|[^|]*\|[^|]*\|([^|]*)\|", RegexOptions.Compiled);
    private static readonly Regex MessageTypePattern = new(@"MSH\|[^|]*\|[^|]*\|[^|]*\|[^|]*\|[^|]*\|[^|]*\|[^|]*\|([^|]*)\|", RegexOptions.Compiled);

    public HL7VendorDetectionPlugin(ILogger<HL7VendorDetectionPlugin> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public string StandardName => "HL7v23";

    /// <inheritdoc />
    public bool CanHandle(string standard)
    {
        return standard?.Equals("HL7v23", StringComparison.OrdinalIgnoreCase) == true ||
               standard?.Equals("HL7", StringComparison.OrdinalIgnoreCase) == true ||
               standard?.StartsWith("HL7v2", StringComparison.OrdinalIgnoreCase) == true;
    }

    /// <inheritdoc />
    public async Task<Result<MessageHeaders>> ExtractMessageHeadersAsync(string message)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(message))
                return Result<MessageHeaders>.Failure("Message cannot be null or empty");

            if (!IsValidMessageFormat(message))
                return Result<MessageHeaders>.Failure("Not a valid HL7 message - missing MSH segment");

            var sendingAppMatch = SendingApplicationPattern.Match(message);
            var sendingFacilityMatch = SendingFacilityPattern.Match(message);
            var receivingAppMatch = ReceivingApplicationPattern.Match(message);
            var receivingFacilityMatch = ReceivingFacilityPattern.Match(message);
            var messageTypeMatch = MessageTypePattern.Match(message);

            var headers = new MessageHeaders
            {
                Standard = StandardName,
                SendingApplication = sendingAppMatch.Success && sendingAppMatch.Groups.Count > 1 
                    ? sendingAppMatch.Groups[1].Value.Trim() 
                    : string.Empty,
                SendingFacility = sendingFacilityMatch.Success && sendingFacilityMatch.Groups.Count > 1 
                    ? sendingFacilityMatch.Groups[1].Value.Trim() 
                    : string.Empty,
                ReceivingApplication = receivingAppMatch.Success && receivingAppMatch.Groups.Count > 1 
                    ? receivingAppMatch.Groups[1].Value.Trim() 
                    : string.Empty,
                ReceivingFacility = receivingFacilityMatch.Success && receivingFacilityMatch.Groups.Count > 1 
                    ? receivingFacilityMatch.Groups[1].Value.Trim() 
                    : string.Empty,
                MessageType = messageTypeMatch.Success && messageTypeMatch.Groups.Count > 1 
                    ? messageTypeMatch.Groups[1].Value.Trim() 
                    : string.Empty
            };

            _logger.LogDebug("Extracted HL7 headers: SendingApp={SendingApp}, SendingFacility={SendingFacility}, MessageType={MessageType}",
                headers.SendingApplication, headers.SendingFacility, headers.MessageType);

            return await Task.FromResult(Result<MessageHeaders>.Success(headers));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting HL7 message headers");
            return Result<MessageHeaders>.Failure($"HL7 header extraction failed: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public bool IsValidMessageFormat(string message)
    {
        return !string.IsNullOrWhiteSpace(message) && 
               MshSegmentPattern.IsMatch(message.TrimStart());
    }

    /// <inheritdoc />
    public async Task<Result<VendorSignature>> DetectVendorSignatureAsync(
        string message,
        IVendorPatternRepository patternRepository)
    {
        try
        {
            // Extract headers first
            var headersResult = await ExtractMessageHeadersAsync(message);
            if (headersResult.IsFailure)
                return Result<VendorSignature>.Failure($"Cannot detect vendor: {headersResult.Error}");

            var headers = headersResult.Value;

            // Load HL7-specific vendor patterns
            var patternsResult = await patternRepository.LoadPatternsForStandardAsync(StandardName);
            if (patternsResult.IsFailure)
            {
                _logger.LogWarning("Failed to load HL7 vendor patterns: {Error}", patternsResult.Error);
                return CreateUnknownVendorSignature(headers);
            }

            var patterns = patternsResult.Value;
            if (patterns.Count == 0)
            {
                _logger.LogWarning("No HL7 vendor detection patterns found");
                return CreateUnknownVendorSignature(headers);
            }

            // Try each pattern and find the best match
            VendorSignature? bestMatch = null;
            double bestConfidence = 0.0;

            foreach (var pattern in patterns)
            {
                var matchResult = EvaluatePattern(pattern, headers);
                if (matchResult.IsSuccess && matchResult.Value > bestConfidence)
                {
                    bestConfidence = matchResult.Value;
                    bestMatch = CreateVendorSignature(pattern, headers, matchResult.Value);
                    
                    _logger.LogDebug("Found better HL7 vendor match: {VendorName} with confidence {Confidence:P1}",
                        pattern.VendorName, matchResult.Value);
                }
            }

            if (bestMatch != null)
            {
                _logger.LogInformation("Detected HL7 vendor: {VendorName} with confidence {Confidence:P1}",
                    bestMatch.Name, bestMatch.Confidence);
                return Result<VendorSignature>.Success(bestMatch);
            }

            _logger.LogInformation("No specific HL7 vendor detected, using unknown vendor signature");
            return CreateUnknownVendorSignature(headers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting HL7 vendor signature");
            return Result<VendorSignature>.Failure($"HL7 vendor detection failed: {ex.Message}");
        }
    }

    #region Private Helper Methods

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
                        _logger.LogTrace("HL7 application rule matched: {Pattern} -> +{Boost}", 
                            rule.Pattern, rule.ConfidenceBoost);
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
                        _logger.LogTrace("HL7 facility rule matched: {Pattern} -> +{Boost}", 
                            rule.Pattern, rule.ConfidenceBoost);
                    }
                }
            }

            // Check message type patterns
            if (!string.IsNullOrWhiteSpace(headers.MessageType) && pattern.MessageTypePatterns != null)
            {
                foreach (var rule in pattern.MessageTypePatterns)
                {
                    if (EvaluateRule(rule, headers.MessageType))
                    {
                        confidence += rule.ConfidenceBoost;
                        _logger.LogTrace("HL7 message type rule matched: {Pattern} -> +{Boost}", 
                            rule.Pattern, rule.ConfidenceBoost);
                    }
                }
            }

            // Ensure confidence is between 0 and 1
            confidence = Math.Min(1.0, Math.Max(0.0, confidence));

            return Result<double>.Success(confidence);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating HL7 vendor pattern");
            return Result<double>.Failure($"Pattern evaluation failed: {ex.Message}");
        }
    }

    private bool EvaluateRule(DetectionRule rule, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var comparison = rule.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        
        return rule.MatchType switch
        {
            ConfigMatchType.Exact => value.Equals(rule.Pattern, comparison),
            ConfigMatchType.Contains => value.Contains(rule.Pattern, comparison),
            ConfigMatchType.StartsWith => value.StartsWith(rule.Pattern, comparison),
            ConfigMatchType.EndsWith => value.EndsWith(rule.Pattern, comparison),
            ConfigMatchType.Regex => EvaluateRegexRule(rule, value),
            _ => false
        };
    }

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
            _logger.LogWarning(ex, "Invalid regex pattern in HL7 vendor detection: {Pattern}", rule.Pattern);
            return false;
        }
    }

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
            DetectedValue = string.Empty,
            StandardValue = string.Empty
        }).ToList() ?? new List<FormatDeviation>();

        return new VendorSignature
        {
            Name = pattern.VendorName,
            Version = pattern.PatternVersion,
            SendingApplication = headers.SendingApplication,
            SendingFacility = headers.SendingFacility ?? string.Empty,
            Confidence = confidence,
            DetectionMethod = $"HL7 Pattern Matching ({pattern.Id})",
            Deviations = deviations
        };
    }

    private Result<VendorSignature> CreateUnknownVendorSignature(MessageHeaders headers)
    {
        var signature = new VendorSignature
        {
            Name = "Unknown",
            Version = "1.0",
            SendingApplication = headers.SendingApplication ?? string.Empty,
            SendingFacility = headers.SendingFacility ?? string.Empty,
            Confidence = 0.3,
            DetectionMethod = "HL7 Default (No Pattern Match)",
            Deviations = new List<FormatDeviation>()
        };

        return Result<VendorSignature>.Success(signature);
    }

    #endregion
}