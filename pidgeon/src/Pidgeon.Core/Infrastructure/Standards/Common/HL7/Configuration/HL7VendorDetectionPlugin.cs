// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Microsoft.Extensions.Logging;
using Pidgeon.Core.Application.Interfaces.Configuration;
using Pidgeon.Core.Application.Interfaces.Standards;
using Pidgeon.Core.Domain.Configuration.Entities;
using Pidgeon.Core.Infrastructure.Standards.Common.HL7.Utilities;
using System.Text.RegularExpressions;
using ConfigMatchType = Pidgeon.Core.Domain.Configuration.Entities.MatchType;

namespace Pidgeon.Core.Standards.Common.HL7.Configuration;

/// <summary>
/// Universal HL7 vendor detection plugin.
/// Handles MSH segment parsing and vendor signature detection for all HL7v2 messages.
/// Works across all HL7v2 versions using standard field positions.
/// </summary>
internal class HL7VendorDetectionPlugin : IStandardVendorDetectionPlugin
{
    private readonly ILogger<HL7VendorDetectionPlugin> _logger;

    // HL7-specific message parsing pattern for validation only
    private static readonly Regex MshSegmentPattern = new(@"^MSH\|", RegexOptions.Compiled);

    public HL7VendorDetectionPlugin(ILogger<HL7VendorDetectionPlugin> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public string StandardName => "HL7v2";

    /// <inheritdoc />
    public bool CanHandle(string standard)
    {
        return standard?.Equals("HL7", StringComparison.OrdinalIgnoreCase) == true ||
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

            // Split message into segments for MshHeaderParser
            var segments = message.Split('\r', '\n')
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .ToList();

            // Use MshHeaderParser for consolidated field extraction
            var mshFields = MshHeaderParser.ExtractAllHeaderFields(segments);
            if (mshFields == null)
                return Result<MessageHeaders>.Failure("Failed to extract MSH header fields");

            // Extract message type from MSH.9 field (index 8)
            var messageTypeValue = MshHeaderParser.ExtractMshField(segments, 8) ?? string.Empty;

            var headers = new MessageHeaders
            {
                Standard = StandardName,
                SendingApplication = mshFields.SendingApplication?.Trim() ?? string.Empty,
                SendingFacility = mshFields.SendingFacility?.Trim() ?? string.Empty,
                ReceivingApplication = mshFields.ReceivingApplication?.Trim() ?? string.Empty,
                ReceivingFacility = mshFields.ReceivingFacility?.Trim() ?? string.Empty,
                MessageType = messageTypeValue.Trim()
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

            // Create rule sets for pattern evaluation
            var patternRuleSets = new List<PatternRuleSet>();

            if (!string.IsNullOrWhiteSpace(headers.SendingApplication))
            {
                patternRuleSets.Add(new PatternRuleSet
                {
                    RuleType = "application",
                    HeaderValue = headers.SendingApplication,
                    Rules = pattern.ApplicationPatterns
                });
            }

            if (!string.IsNullOrWhiteSpace(headers.SendingFacility))
            {
                patternRuleSets.Add(new PatternRuleSet
                {
                    RuleType = "facility", 
                    HeaderValue = headers.SendingFacility,
                    Rules = pattern.FacilityPatterns
                });
            }

            if (!string.IsNullOrWhiteSpace(headers.MessageType) && pattern.MessageTypePatterns != null)
            {
                patternRuleSets.Add(new PatternRuleSet
                {
                    RuleType = "message type",
                    HeaderValue = headers.MessageType,
                    Rules = pattern.MessageTypePatterns
                });
            }

            // Use consolidated pattern evaluation framework
            var confidenceBoost = PatternEvaluationFramework.EvaluatePatternRuleSets(patternRuleSets, headers, _logger);
            confidence += confidenceBoost;

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