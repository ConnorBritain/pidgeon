// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Microsoft.Extensions.Logging;
using Pidgeon.Core.Application.Interfaces.Configuration;
using Pidgeon.Core.Domain.Configuration.Entities;
using Pidgeon.Core.Generation;
using System.Text.RegularExpressions;

namespace Pidgeon.Core.Infrastructure.Standards.HL7.Vendor;

/// <summary>
/// HL7-specific vendor pattern analysis and detection plugin.
/// Implements vendor intelligence for HL7 v2.x messages across all versions.
/// Handles vendor signature extraction, pattern analysis, and confidence scoring.
/// </summary>
internal class HL7VendorPlugin : IStandardVendorPlugin
{
    private readonly ILogger<HL7VendorPlugin> _logger;

    public HL7VendorPlugin(ILogger<HL7VendorPlugin> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public string Standard => "HL7v2";

    /// <inheritdoc />
    public string DisplayName => "HL7 v2.x Vendor Intelligence";

    /// <inheritdoc />
    public int Priority => 100; // High priority for HL7 messages

    /// <inheritdoc />
    public bool CanAnalyze(string messageContent)
    {
        if (string.IsNullOrWhiteSpace(messageContent))
            return false;

        // HL7 messages start with MSH segment
        var lines = messageContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        if (!lines.Any())
            return false;

        var firstLine = lines[0].Trim();
        
        // Check for MSH segment with proper field separator structure
        if (!firstLine.StartsWith("MSH", StringComparison.OrdinalIgnoreCase))
            return false;

        // Must have field separator and encoding characters
        if (firstLine.Length < 8) // MSH|^~\&|
            return false;

        // Field separator should be | (position 3)
        if (firstLine[3] != '|')
            return false;

        // Encoding characters should follow (position 4-7): ^~\&
        var encodingChars = firstLine.Substring(4, Math.Min(4, firstLine.Length - 4));
        
        return encodingChars.StartsWith("^~");
    }

    /// <inheritdoc />
    public async Task<Result<VendorConfiguration>> AnalyzeVendorPatternsAsync(
        IEnumerable<string> messages, 
        InferenceOptions options)
    {
        if (messages == null)
            return Result<VendorConfiguration>.Failure("Messages collection cannot be null");

        var messageList = messages.Where(m => CanAnalyze(m)).ToList();
        if (!messageList.Any())
            return Result<VendorConfiguration>.Failure("No valid HL7 messages found for analysis");

        try
        {
            _logger.LogInformation("Analyzing {MessageCount} HL7 messages for vendor patterns", messageList.Count);

            // Extract vendor signatures from all messages
            var signatures = new List<VendorSignature>();
            var messageTypes = new Dictionary<string, MessagePattern>();
            var fieldPatterns = new Dictionary<string, int>();

            foreach (var message in messageList)
            {
                var signatureResult = await ExtractVendorSignatureAsync(message);
                if (signatureResult.IsSuccess)
                {
                    signatures.Add(signatureResult.Value);
                }

                // Extract message type patterns
                var messageType = ExtractMessageType(message);
                if (!string.IsNullOrEmpty(messageType))
                {
                    if (!messageTypes.ContainsKey(messageType))
                    {
                        messageTypes[messageType] = new MessagePattern
                        {
                            MessageType = messageType,
                            Frequency = 0,
                            Standard = Standard
                        };
                    }
                    messageTypes[messageType] = messageTypes[messageType] with { Frequency = messageTypes[messageType].Frequency + 1 };
                }

                // Extract field usage patterns
                AnalyzeFieldPatterns(message, fieldPatterns);
            }

            // Determine predominant vendor from signatures
            var vendorGroups = signatures.GroupBy(s => s.Name).ToList();
            if (!vendorGroups.Any())
            {
                return Result<VendorConfiguration>.Failure("Could not identify vendor from message signatures");
            }

            var primaryVendor = vendorGroups.OrderByDescending(g => g.Count()).First();
            var vendorName = primaryVendor.Key;
            var representativeSignature = primaryVendor.First();

            // Calculate confidence based on consistency
            var consistency = (double)primaryVendor.Count() / signatures.Count;
            var confidence = Math.Min(0.95, 0.5 + (consistency * 0.45));

            // Create configuration address
            var address = new ConfigurationAddress(
                Vendor: vendorName,
                Standard: Standard,
                MessageType: messageTypes.Keys.FirstOrDefault() ?? "Unknown"
            );

            // Create field patterns using actual structure
            var fieldPatternsEntity = new FieldPatterns
            {
                Standard = Standard,
                MessageType = messageTypes.Keys.FirstOrDefault() ?? "Unknown",
                SegmentPatterns = CreateSegmentPatterns(fieldPatterns),
                Confidence = confidence
            };

            var vendorConfig = VendorConfiguration.Create(
                address: address,
                signature: representativeSignature,
                fieldPatterns: fieldPatternsEntity,
                messagePatterns: messageTypes,
                confidence: confidence,
                sampleCount: messageList.Count
            );

            _logger.LogInformation("HL7 vendor analysis completed: {Vendor} with {Confidence:P1} confidence from {MessageCount} messages", 
                vendorName, confidence, messageList.Count);

            return Result<VendorConfiguration>.Success(vendorConfig);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during HL7 vendor pattern analysis");
            return Result<VendorConfiguration>.Failure($"HL7 vendor analysis failed: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result<double>> CalculateVendorConfidenceAsync(
        string messageContent, 
        VendorConfiguration vendorConfig)
    {
        if (!CanAnalyze(messageContent))
            return Result<double>.Failure("Message is not a valid HL7 message");

        if (vendorConfig.Address.Standard != Standard)
            return Result<double>.Failure($"Vendor configuration is not for {Standard}");

        try
        {
            var signatureResult = await ExtractVendorSignatureAsync(messageContent);
            if (signatureResult.IsFailure)
                return Result<double>.Success(0.0);

            var messageSignature = signatureResult.Value;
            var configSignature = vendorConfig.Signature;

            double confidence = 0.0;

            // Compare vendor names (40% weight)
            if (string.Equals(messageSignature.Name, configSignature.Name, StringComparison.OrdinalIgnoreCase))
            {
                confidence += 0.4;
            }

            // Compare application names (30% weight)
            if (string.Equals(messageSignature.SendingApplication, configSignature.SendingApplication, StringComparison.OrdinalIgnoreCase))
            {
                confidence += 0.3;
            }

            // Compare facility patterns (20% weight)
            if (!string.IsNullOrEmpty(messageSignature.SendingFacility) && 
                !string.IsNullOrEmpty(configSignature.SendingFacility))
            {
                if (messageSignature.SendingFacility.Contains(configSignature.SendingFacility, StringComparison.OrdinalIgnoreCase) ||
                    configSignature.SendingFacility.Contains(messageSignature.SendingFacility, StringComparison.OrdinalIgnoreCase))
                {
                    confidence += 0.2;
                }
            }

            // Compare message type compatibility (10% weight)
            var messageType = ExtractMessageType(messageContent);
            if (!string.IsNullOrEmpty(messageType) && vendorConfig.MessagePatterns.ContainsKey(messageType))
            {
                confidence += 0.1;
            }

            return Result<double>.Success(Math.Min(1.0, confidence));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating vendor confidence for HL7 message");
            return Result<double>.Failure($"Confidence calculation failed: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<VendorMatch>>> DetectVendorCandidatesAsync(
        string messageContent, 
        IReadOnlyList<VendorConfiguration> knownVendors)
    {
        if (!CanAnalyze(messageContent))
            return Result<IReadOnlyList<VendorMatch>>.Failure("Message is not a valid HL7 message");

        try
        {
            var matches = new List<VendorMatch>();

            foreach (var vendor in knownVendors.Where(v => v.Address.Standard == Standard))
            {
                var confidenceResult = await CalculateVendorConfidenceAsync(messageContent, vendor);
                if (confidenceResult.IsSuccess && confidenceResult.Value > 0.1) // Minimum threshold
                {
                    var match = new VendorMatch
                    {
                        VendorConfiguration = vendor,
                        Confidence = confidenceResult.Value,
                        Standard = Standard,
                        MatchReasons = GenerateMatchReasons(messageContent, vendor, confidenceResult.Value),
                        MatchDetails = new Dictionary<string, object>
                        {
                            ["MessageType"] = ExtractMessageType(messageContent) ?? "Unknown",
                            ["MSHApplication"] = ExtractMSHField(messageContent, 3) ?? "",
                            ["MSHFacility"] = ExtractMSHField(messageContent, 4) ?? ""
                        }
                    };
                    matches.Add(match);
                }
            }

            var sortedMatches = matches.OrderByDescending(m => m.Confidence).ToList();
            
            _logger.LogDebug("HL7 vendor detection found {MatchCount} candidates with confidence > 0.1", sortedMatches.Count);

            return Result<IReadOnlyList<VendorMatch>>.Success(sortedMatches);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting HL7 vendor candidates");
            return Result<IReadOnlyList<VendorMatch>>.Failure($"Vendor detection failed: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result<ConfigurationValidationResult>> ValidateVendorConfigurationAsync(
        VendorConfiguration vendorConfig)
    {
        var issues = new List<string>();

        // Validate standard compatibility
        if (vendorConfig.Address.Standard != Standard)
        {
            issues.Add($"Configuration standard '{vendorConfig.Address.Standard}' does not match plugin standard '{Standard}'");
        }

        // Validate signature completeness
        if (string.IsNullOrWhiteSpace(vendorConfig.Signature.Name))
        {
            issues.Add("Vendor signature must have a name");
        }

        // Validate message patterns
        if (!vendorConfig.MessagePatterns.Any())
        {
            issues.Add("At least one message pattern is required");
        }

        var isValid = !issues.Any();
        var result = isValid 
            ? ConfigurationValidationResult.Success(0.95)
            : ConfigurationValidationResult.Failure(issues);

        return await Task.FromResult(Result<ConfigurationValidationResult>.Success(result));
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<VendorConfiguration>>> GetBaselineVendorConfigurationsAsync()
    {
        try
        {
            // FIXME: Replace hardcoded configurations with JSON file loading
            // Should load from vendor/ directory: vendor/hl7/epic.json, vendor/hl7/cerner.json, etc.
            // Each JSON file should match VendorDetectionPattern schema with DetectionRules
            // This allows runtime configuration updates without code changes
            var configurations = new List<VendorConfiguration>
            {
                CreateEpicConfiguration(),     // FIXME: Load from vendor/hl7/epic.json
                CreateCernerConfiguration(),   // FIXME: Load from vendor/hl7/cerner.json  
                CreateAllScriptsConfiguration() // FIXME: Load from vendor/hl7/allscripts.json
            };

            return await Task.FromResult(Result<IReadOnlyList<VendorConfiguration>>.Success(configurations));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating baseline HL7 vendor configurations");
            return Result<IReadOnlyList<VendorConfiguration>>.Failure($"Failed to create baseline configurations: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result<VendorSignature>> ExtractVendorSignatureAsync(string messageContent)
    {
        if (!CanAnalyze(messageContent))
            return Result<VendorSignature>.Failure("Message is not a valid HL7 message");

        try
        {
            var application = ExtractMSHField(messageContent, 3) ?? "Unknown";
            var facility = ExtractMSHField(messageContent, 4) ?? "Unknown";
            var version = ExtractMSHField(messageContent, 12) ?? "2.3";

            // Infer vendor name from application or facility
            var vendorName = InferVendorName(application, facility);

            var signature = new VendorSignature
            {
                Name = vendorName,
                SendingApplication = application,
                SendingFacility = facility,
                Version = version,
                Confidence = CalculateSignatureConfidence(application, facility)
            };

            return await Task.FromResult(Result<VendorSignature>.Success(signature));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting vendor signature from HL7 message");
            return Result<VendorSignature>.Failure($"Signature extraction failed: {ex.Message}");
        }
    }

    #region Private Helper Methods

    private string? ExtractMSHField(string messageContent, int fieldPosition)
    {
        var lines = messageContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        var mshLine = lines.FirstOrDefault(l => l.StartsWith("MSH"));
        if (mshLine == null) return null;

        var fields = mshLine.Split('|');
        if (fields.Length <= fieldPosition) return null;

        return fields[fieldPosition]?.Trim();
    }

    private string? ExtractMessageType(string messageContent)
    {
        var messageTypeField = ExtractMSHField(messageContent, 9);
        if (string.IsNullOrEmpty(messageTypeField)) return null;

        // Message type is in format: MessageCode^TriggerEvent^MessageStructure
        var components = messageTypeField.Split('^');
        if (components.Length >= 2)
        {
            return $"{components[0]}^{components[1]}";
        }
        
        return components[0];
    }

    private string InferVendorName(string application, string facility)
    {
        var combined = $"{application} {facility}".ToUpperInvariant();

        if (combined.Contains("EPIC")) return "Epic";
        if (combined.Contains("CERNER") || combined.Contains("MILLENNIUM")) return "Cerner";
        if (combined.Contains("ALLSCRIPTS") || combined.Contains("SUNRISE")) return "AllScripts";
        if (combined.Contains("MEDITECH")) return "Meditech";
        if (combined.Contains("ATHENA")) return "Athena";

        // Fallback to application name
        return string.IsNullOrWhiteSpace(application) ? "Unknown" : application;
    }

    private double CalculateSignatureConfidence(string application, string facility)
    {
        double confidence = 0.5; // Base confidence

        if (!string.IsNullOrWhiteSpace(application) && application != "Unknown")
            confidence += 0.3;

        if (!string.IsNullOrWhiteSpace(facility) && facility != "Unknown")
            confidence += 0.2;

        return Math.Min(1.0, confidence);
    }

    private void AnalyzeFieldPatterns(string message, Dictionary<string, int> fieldPatterns)
    {
        var lines = message.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var line in lines)
        {
            if (line.Length < 3) continue;
            
            var segmentType = line.Substring(0, 3);
            var fields = line.Split('|');
            
            for (int i = 1; i < fields.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(fields[i]))
                {
                    var fieldKey = $"{segmentType}.{i}";
                    fieldPatterns[fieldKey] = fieldPatterns.GetValueOrDefault(fieldKey, 0) + 1;
                }
            }
        }
    }

    private Dictionary<int, FieldFrequency> CreateFieldFrequencyMap(Dictionary<string, int> fieldPatterns, string segmentType)
    {
        var segmentFields = fieldPatterns
            .Where(kvp => kvp.Key.StartsWith($"{segmentType}."))
            .ToDictionary(
                kvp => int.Parse(kvp.Key.Split('.')[1]),
                kvp => new FieldFrequency 
                { 
                    FieldName = kvp.Key,
                    FieldIndex = int.Parse(kvp.Key.Split('.')[1])
                }
            );

        return segmentFields;
    }

    private List<string> GenerateMatchReasons(string messageContent, VendorConfiguration vendor, double confidence)
    {
        var reasons = new List<string>();

        var messageSignature = ExtractVendorSignatureAsync(messageContent).Result;
        if (messageSignature.IsSuccess)
        {
            var sig = messageSignature.Value;
            if (string.Equals(sig.Name, vendor.Signature.Name, StringComparison.OrdinalIgnoreCase))
            {
                reasons.Add($"Vendor name match: {sig.Name}");
            }
            if (string.Equals(sig.SendingApplication, vendor.Signature.SendingApplication, StringComparison.OrdinalIgnoreCase))
            {
                reasons.Add($"Application name match: {sig.SendingApplication}");
            }
        }

        if (confidence > 0.8)
            reasons.Add("High confidence match");
        else if (confidence > 0.6)
            reasons.Add("Medium confidence match");
        else
            reasons.Add("Low confidence match");

        return reasons;
    }

    private VendorConfiguration CreateEpicConfiguration()
    {
        var address = new ConfigurationAddress("Epic", Standard, "ADT^A01");
        var signature = new VendorSignature
        {
            Name = "Epic",
            SendingApplication = "EPIC",
            SendingFacility = "EPIC_PROD",
            Version = "2.3",
            Confidence = 0.9
        };

        var mshSegment = new SegmentPattern
        {
            SegmentId = "MSH",
            SegmentType = "MSH",
            Fields = new Dictionary<int, FieldFrequency>
            {
                [3] = new FieldFrequency { FieldName = "MSH.3", FieldIndex = 3 },
                [4] = new FieldFrequency { FieldName = "MSH.4", FieldIndex = 4 }
            }
        };

        var fieldPatterns = new FieldPatterns
        {
            Standard = Standard,
            MessageType = "ADT^A01",
            SegmentPatterns = new Dictionary<string, SegmentPattern>
            {
                ["MSH"] = mshSegment
            }
        };

        return VendorConfiguration.Create(address, signature, fieldPatterns, new Dictionary<string, MessagePattern>(), 0.9, 100);
    }

    private VendorConfiguration CreateCernerConfiguration()
    {
        var address = new ConfigurationAddress("Cerner", Standard, "ADT^A01");
        var signature = new VendorSignature
        {
            Name = "Cerner",
            SendingApplication = "CERNER",
            SendingFacility = "MILLENNIUM",
            Version = "2.5",
            Confidence = 0.9
        };

        var mshSegment = new SegmentPattern
        {
            SegmentId = "MSH",
            SegmentType = "MSH",
            Fields = new Dictionary<int, FieldFrequency>
            {
                [3] = new FieldFrequency { FieldName = "MSH.3", FieldIndex = 3 },
                [4] = new FieldFrequency { FieldName = "MSH.4", FieldIndex = 4 }
            }
        };

        var fieldPatterns = new FieldPatterns
        {
            Standard = Standard,
            MessageType = "ADT^A01",
            SegmentPatterns = new Dictionary<string, SegmentPattern>
            {
                ["MSH"] = mshSegment
            }
        };

        return VendorConfiguration.Create(address, signature, fieldPatterns, new Dictionary<string, MessagePattern>(), 0.9, 100);
    }

    private VendorConfiguration CreateAllScriptsConfiguration()
    {
        var address = new ConfigurationAddress("AllScripts", Standard, "ADT^A01");
        var signature = new VendorSignature
        {
            Name = "AllScripts",
            SendingApplication = "ALLSCRIPTS",
            SendingFacility = "SUNRISE",
            Version = "2.4",
            Confidence = 0.85
        };

        var mshSegment = new SegmentPattern
        {
            SegmentId = "MSH",
            SegmentType = "MSH",
            Fields = new Dictionary<int, FieldFrequency>
            {
                [3] = new FieldFrequency { FieldName = "MSH.3", FieldIndex = 3 },
                [4] = new FieldFrequency { FieldName = "MSH.4", FieldIndex = 4 }
            }
        };

        var fieldPatterns = new FieldPatterns
        {
            Standard = Standard,
            MessageType = "ADT^A01",
            SegmentPatterns = new Dictionary<string, SegmentPattern>
            {
                ["MSH"] = mshSegment
            }
        };

        return VendorConfiguration.Create(address, signature, fieldPatterns, new Dictionary<string, MessagePattern>(), 0.85, 100);
    }

    private Dictionary<string, SegmentPattern> CreateSegmentPatterns(Dictionary<string, int> fieldPatterns)
    {
        var segmentPatterns = new Dictionary<string, SegmentPattern>();
        
        // Group field patterns by segment
        var segmentGroups = fieldPatterns
            .Where(kvp => kvp.Key.Contains('.'))
            .GroupBy(kvp => kvp.Key.Split('.')[0])
            .ToList();
        
        foreach (var group in segmentGroups)
        {
            var segmentId = group.Key;
            var fieldFrequencies = new Dictionary<int, FieldFrequency>();
            
            foreach (var field in group)
            {
                var fieldNumStr = field.Key.Split('.').Length > 1 ? field.Key.Split('.')[1] : "0";
                if (int.TryParse(fieldNumStr, out var fieldNum))
                {
                    fieldFrequencies[fieldNum] = new FieldFrequency
                    {
                        FieldName = $"{segmentId}.{fieldNum}",
                        FieldIndex = fieldNum
                    };
                }
            }
            
            if (fieldFrequencies.Any())
            {
                segmentPatterns[segmentId] = new SegmentPattern
                {
                    SegmentId = segmentId,
                    SegmentType = segmentId,
                    Fields = fieldFrequencies
                };
            }
        }
        
        return segmentPatterns;
    }

    #endregion
}