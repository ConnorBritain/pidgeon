// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Microsoft.Extensions.Logging;
using Pidgeon.Core.Adapters.Interfaces;
using Pidgeon.Core.Domain.Configuration.Entities;
using Pidgeon.Core.Domain.Messaging;
using Pidgeon.Core.Domain.Messaging.HL7v2.Messages;

namespace Pidgeon.Core.Adapters.Implementation;

/// <summary>
/// Adapter implementation for translating HL7 messaging structures to configuration domain types.
/// Converts int-based HL7 field positions to string-based configuration paths.
/// Translates message structures into vendor pattern analysis.
/// </summary>
public class HL7ToConfigurationAdapter : IMessagingToConfigurationAdapter
{
    private readonly ILogger<HL7ToConfigurationAdapter> _logger;

    public HL7ToConfigurationAdapter(ILogger<HL7ToConfigurationAdapter> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Analyzes HL7 messages to extract field patterns using configuration domain semantics.
    /// Converts from HL7's int-based field positions to configuration's string-based field paths.
    /// </summary>
    public async Task<FieldPatterns> AnalyzePatternsAsync(IEnumerable<HealthcareMessage> messages)
    {
        _logger.LogDebug("Starting HL7 field pattern analysis for {MessageCount} messages", messages.Count());

        var hl7Messages = messages.OfType<HL7Message>().ToList();
        if (!hl7Messages.Any())
        {
            _logger.LogWarning("No HL7 messages found in input collection");
            return new FieldPatterns
            {
                MessageType = "Unknown",
                Standard = "HL7v2",
                SegmentPatterns = new Dictionary<string, SegmentPattern>(),
                Confidence = 0.0,
                AnalysisDate = DateTime.UtcNow
            };
        }

        // Group messages by type for analysis
        var messageGroups = hl7Messages.GroupBy(m => m.MessageType?.MessageTypeCode ?? "Unknown");
        var allFieldFrequencies = new Dictionary<string, FieldFrequency>();
        int totalSamples = 0;

        foreach (var group in messageGroups)
        {
            var groupMessages = group.ToList();
            totalSamples += groupMessages.Count;

            // Analyze each segment type within the message group
            var segmentAnalysis = await AnalyzeSegmentPatternsAsync(groupMessages);
            
            // Convert segment analysis to field frequency format
            foreach (var segmentKvp in segmentAnalysis)
            {
                foreach (var fieldKvp in segmentKvp.Value)
                {
                    // Convert from messaging domain format (int field position) to configuration domain format (string field path)
                    var fieldPath = $"{segmentKvp.Key}.{fieldKvp.Key}"; // e.g., "PID.5"
                    allFieldFrequencies[fieldPath] = fieldKvp.Value;
                }
            }
        }

        var primaryMessageType = messageGroups.First().Key;
        
        // Convert field frequencies to segment patterns
        var segmentPatterns = new Dictionary<string, SegmentPattern>();
        foreach (var fieldPath in allFieldFrequencies.Keys)
        {
            var parts = fieldPath.Split('.');
            if (parts.Length >= 2)
            {
                var segmentId = parts[0];
                if (!segmentPatterns.ContainsKey(segmentId))
                {
                    segmentPatterns[segmentId] = new SegmentPattern
                    {
                        SegmentId = segmentId,
                        Fields = new Dictionary<int, FieldFrequency>(),
                        FieldFrequencies = new Dictionary<int, FieldFrequency>(), // TODO: Remove duplicate in Phase 2
                        SegmentType = segmentId, // Use segment ID as type for now
                        TotalCount = totalSamples,
                        Confidence = 1.0, // Default confidence
                        SampleSize = totalSamples
                    };
                }
                
                if (int.TryParse(parts[1], out var fieldIndex))
                {
                    segmentPatterns[segmentId].FieldFrequencies[fieldIndex] = allFieldFrequencies[fieldPath];
                    segmentPatterns[segmentId].Fields[fieldIndex] = allFieldFrequencies[fieldPath]; // TODO: Remove duplicate in Phase 2
                }
            }
        }
        
        return new FieldPatterns
        {
            MessageType = primaryMessageType,
            Standard = "HL7v2",
            SegmentPatterns = segmentPatterns,
            Confidence = CalculateOverallConfidence(allFieldFrequencies),
            AnalysisDate = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Infers vendor configuration from HL7 message samples.
    /// Combines field analysis, vendor signature detection, and format deviation analysis.
    /// </summary>
    public async Task<VendorConfiguration> InferConfigurationAsync(IEnumerable<HealthcareMessage> messages)
    {
        var fieldPatterns = await AnalyzePatternsAsync(messages);
        var vendorSignature = await DetectVendorSignatureAsync(messages);
        
        // Use VendorConfiguration.Create factory method to properly construct
        var messagePatterns = new Dictionary<string, MessagePattern>();
        messagePatterns[fieldPatterns.MessageType] = new MessagePattern
        {
            MessageType = fieldPatterns.MessageType,
            Frequency = 1,
            SegmentSequence = fieldPatterns.SegmentPatterns.Keys.ToList(),
            RequiredSegments = fieldPatterns.SegmentPatterns.Keys.ToList(),
            OptionalSegments = new List<string>()
        };
        
        var sampleCount = fieldPatterns.SegmentPatterns.Values
            .Select(s => s.SampleSize)
            .FirstOrDefault();
        
        return VendorConfiguration.Create(
            address: new ConfigurationAddress(
                Vendor: vendorSignature?.Name ?? "Unknown",
                Standard: "HL7v2",
                MessageType: fieldPatterns.MessageType
            ),
            signature: vendorSignature ?? new VendorSignature { Name = "Unknown", Version = null },
            fieldPatterns: fieldPatterns,
            messagePatterns: messagePatterns,
            confidence: fieldPatterns.Confidence,
            sampleCount: sampleCount
        );
    }

    /// <summary>
    /// Detects format deviations by comparing messages against expected configuration.
    /// </summary>
    public async Task<List<FormatDeviation>> DetectDeviationsAsync(HealthcareMessage message, VendorConfiguration configuration)
    {
        if (message is not HL7Message hl7Message)
        {
            return new List<FormatDeviation>();
        }

        var deviations = new List<FormatDeviation>();

        // Analyze message against expected patterns
        // This would contain the actual deviation detection logic
        await Task.CompletedTask; // Placeholder for async work

        return deviations;
    }

    /// <summary>
    /// Calculates field statistics from HL7 messages using configuration domain types.
    /// This method specifically handles the Dictionary<int, FieldFrequency> to Dictionary<string, FieldFrequency> conversion
    /// that was causing compilation errors in HL7FieldAnalysisPlugin.
    /// </summary>
    public async Task<Dictionary<string, FieldFrequency>> CalculateFieldStatisticsAsync(IEnumerable<HealthcareMessage> messages)
    {
        var patterns = await AnalyzePatternsAsync(messages);
        
        // Convert segment patterns back to flat field frequency dictionary
        var fieldFrequencies = new Dictionary<string, FieldFrequency>();
        foreach (var segmentPattern in patterns.SegmentPatterns.Values)
        {
            foreach (var fieldFreq in segmentPattern.FieldFrequencies)
            {
                var fieldPath = $"{segmentPattern.SegmentId}.{fieldFreq.Key}";
                fieldFrequencies[fieldPath] = fieldFreq.Value;
            }
        }
        
        return fieldFrequencies;
    }

    #region Private Helper Methods

    /// <summary>
    /// Analyzes segment patterns from HL7 messages.
    /// Returns Dictionary<SegmentId, Dictionary<FieldPosition, FieldFrequency>> 
    /// where field positions are int (messaging domain) but will be converted to string paths (configuration domain).
    /// </summary>
    private async Task<Dictionary<string, Dictionary<int, FieldFrequency>>> AnalyzeSegmentPatternsAsync(IList<HL7Message> messages)
    {
        var segmentAnalysis = new Dictionary<string, Dictionary<int, FieldFrequency>>();

        // This would contain the actual segment analysis logic
        // For now, return empty structure to resolve compilation
        await Task.CompletedTask; // Placeholder for async work

        return segmentAnalysis;
    }

    private async Task<VendorSignature?> DetectVendorSignatureAsync(IEnumerable<HealthcareMessage> messages)
    {
        // Placeholder for vendor signature detection
        await Task.CompletedTask;
        return null;
    }

    private static Dictionary<string, FieldPattern> ExtractRequiredFields(FieldPatterns patterns)
    {
        var requiredFields = new Dictionary<string, FieldPattern>();
        
        foreach (var segmentPattern in patterns.SegmentPatterns.Values)
        {
            foreach (var fieldFreq in segmentPattern.FieldFrequencies)
            {
                if (fieldFreq.Value.Frequency > 0.8) // Fields present in >80% of messages are considered required
                {
                    var fieldPath = $"{segmentPattern.SegmentId}.{fieldFreq.Key}";
                    requiredFields[fieldPath] = new FieldPattern
                    {
                        Path = fieldPath,
                        Frequency = fieldFreq.Value.Frequency,
                        CommonValues = fieldFreq.Value.CommonValues?.Keys.ToList() ?? new List<string>(),
                        RegexPattern = null,
                        Cardinality = Cardinality.Required
                    };
                }
            }
        }
        
        return requiredFields;
    }

    private static Dictionary<string, FieldPattern> ExtractOptionalFields(FieldPatterns patterns)
    {
        var optionalFields = new Dictionary<string, FieldPattern>();
        
        foreach (var segmentPattern in patterns.SegmentPatterns.Values)
        {
            foreach (var fieldFreq in segmentPattern.FieldFrequencies)
            {
                if (fieldFreq.Value.Frequency <= 0.8) // Fields present in ≤80% of messages are considered optional
                {
                    var fieldPath = $"{segmentPattern.SegmentId}.{fieldFreq.Key}";
                    optionalFields[fieldPath] = new FieldPattern
                    {
                        Path = fieldPath,
                        Frequency = fieldFreq.Value.Frequency,
                        CommonValues = fieldFreq.Value.CommonValues?.Keys.ToList() ?? new List<string>(),
                        RegexPattern = null,
                        Cardinality = Cardinality.Optional
                    };
                }
            }
        }
        
        return optionalFields;
    }

    private static double CalculateOverallConfidence(Dictionary<string, FieldFrequency> fieldFrequencies)
    {
        if (!fieldFrequencies.Any())
            return 0.0;

        // Simple confidence calculation based on average population rates
        var averagePopulation = fieldFrequencies.Values.Average(f => f.Frequency);
        return Math.Min(averagePopulation + 0.1, 1.0); // Boost confidence slightly, cap at 1.0
    }

    #endregion

    public async Task<SegmentPattern> AnalyzeSegmentPatternsAsync(
        IEnumerable<HealthcareMessage> messages,
        string segmentType)
    {
        var hl7Messages = messages.OfType<HL7Message>().ToList();
        
        if (!hl7Messages.Any())
        {
            _logger.LogWarning("No HL7 messages found for segment analysis of type {SegmentType}", segmentType);
            return new SegmentPattern
            {
                SegmentId = segmentType,
                FieldFrequencies = new Dictionary<int, FieldFrequency>(),
                SampleSize = 0
            };
        }

        var fieldFrequencies = new Dictionary<int, FieldFrequency>();
        var totalSegments = 0;

        foreach (var message in hl7Messages)
        {
            // TODO: Extract segments of specific type from HL7Message.Segments dictionary
            // Current HL7Message structure may need enhancement to support segment type filtering
            totalSegments++;
        }

        await Task.CompletedTask;
        return new SegmentPattern
        {
            SegmentId = segmentType,
            FieldFrequencies = fieldFrequencies,
            SampleSize = totalSegments
        };
    }

    public async Task<ComponentPattern> AnalyzeComponentPatternsAsync(
        IEnumerable<HealthcareMessage> messages,
        string fieldType)
    {
        var hl7Messages = messages.OfType<HL7Message>().ToList();
        
        if (!hl7Messages.Any())
        {
            _logger.LogWarning("No HL7 messages found for component analysis of type {FieldType}", fieldType);
            return new ComponentPattern
            {
                FieldType = fieldType,
                ComponentFrequencies = new Dictionary<int, ComponentFrequency>(),
                SampleSize = 0
            };
        }

        var componentFrequencies = new Dictionary<int, ComponentFrequency>();
        
        // TODO: Extract field values of specified type from HL7 messages
        // This requires field type mapping (XPN, CE, CX, etc.) and field extraction logic

        await Task.CompletedTask;
        return new ComponentPattern
        {
            FieldType = fieldType,
            ComponentFrequencies = componentFrequencies,
            SampleSize = 0
        };
    }
}