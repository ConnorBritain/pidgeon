// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using Segmint.Core.Performance;

namespace Segmint.Core.Configuration.Inference;

/// <summary>
/// Analyzes HL7 messages to identify structural patterns and generate vendor-specific configurations.
/// </summary>
public class MessageAnalyzer
{
    private readonly Dictionary<string, FieldPattern> _fieldPatterns = new();
    private readonly Dictionary<string, SegmentPattern> _segmentPatterns = new();
    private readonly List<string> _analyzedMessages = new();
    private readonly Dictionary<string, int> _vendorSignatures = new();

    /// <summary>
    /// Analyzes a collection of HL7 messages to build pattern understanding.
    /// </summary>
    /// <param name="messages">Collection of HL7 message strings.</param>
    /// <returns>Analysis summary with pattern statistics.</returns>
    public AnalysisSummary AnalyzeMessages(IEnumerable<string> messages)
    {
        var messageList = messages.ToList();
        var startTime = DateTime.UtcNow;

        foreach (var message in messageList)
        {
            AnalyzeMessage(message);
        }

        return new AnalysisSummary
        {
            TotalMessages = messageList.Count,
            SegmentPatterns = _segmentPatterns.Count,
            FieldPatterns = _fieldPatterns.Count,
            AnalysisDate = startTime,
            VendorSignatures = _vendorSignatures.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
        };
    }

    /// <summary>
    /// Generates a vendor configuration based on analyzed patterns.
    /// </summary>
    /// <param name="vendorName">Name of the vendor/system.</param>
    /// <param name="messageType">HL7 message type (e.g., "ORM^O01").</param>
    /// <param name="confidenceThreshold">Minimum confidence level for including patterns (0.0-1.0).</param>
    /// <returns>Inferred vendor configuration.</returns>
    public VendorConfiguration GenerateConfiguration(string vendorName, string messageType, double confidenceThreshold = 0.7)
    {
        var config = new VendorConfiguration
        {
            Vendor = vendorName,
            MessageType = messageType,
            InferredFrom = new InferenceMetadata
            {
                SampleCount = _analyzedMessages.Count,
                DateRange = $"{DateTime.UtcNow.AddDays(-30):yyyy-MM-dd} to {DateTime.UtcNow:yyyy-MM-dd}",
                Confidence = CalculateOverallConfidence()
            },
            Segments = new Dictionary<string, Dictionary<string, object>>()
        };

        // Build segment configurations based on patterns
        foreach (var segmentPattern in _segmentPatterns.Values)
        {
            if (segmentPattern.Confidence >= confidenceThreshold)
            {
                config.Segments[segmentPattern.SegmentId] = BuildSegmentConfig(segmentPattern, confidenceThreshold);
            }
        }

        // Add patterns and validation rules
        config.Patterns = InferMessagePatterns();
        config.ValidationRules = GenerateValidationRules(confidenceThreshold);

        return config;
    }

    /// <summary>
    /// Analyzes a single HL7 message to extract patterns.
    /// </summary>
    /// <param name="message">HL7 message string.</param>
    private void AnalyzeMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return;

        _analyzedMessages.Add(message);

        // Extract vendor signatures from MSH segment
        ExtractVendorSignatures(message);

        // Split into segments and analyze each
        var segments = FastHL7Parser.ExtractSegmentIds(message.AsSpan());
        var messageLines = message.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in messageLines)
        {
            if (line.Length < 4) continue;

            var segmentId = line.Substring(0, 3);
            AnalyzeSegment(segmentId, line);
        }
    }

    /// <summary>
    /// Analyzes a segment to identify field patterns.
    /// </summary>
    /// <param name="segmentId">The segment identifier (e.g., "PID", "ORC").</param>
    /// <param name="segmentData">The complete segment data.</param>
    private void AnalyzeSegment(string segmentId, string segmentData)
    {
        if (!_segmentPatterns.ContainsKey(segmentId))
        {
            _segmentPatterns[segmentId] = new SegmentPattern
            {
                SegmentId = segmentId,
                Occurrences = 0,
                FieldPatterns = new Dictionary<string, FieldPattern>()
            };
        }

        var segmentPattern = _segmentPatterns[segmentId];
        segmentPattern.Occurrences++;

        // Parse fields
        var fields = ComponentCache.GetComponents(segmentData, '|');
        
        for (int i = 1; i < fields.Length; i++) // Skip segment ID
        {
            var fieldKey = $"{segmentId}.{i}";
            var fieldValue = fields[i];

            if (!string.IsNullOrEmpty(fieldValue))
            {
                AnalyzeField(fieldKey, fieldValue, segmentPattern);
            }
        }

        // Update confidence based on consistency
        segmentPattern.Confidence = CalculateSegmentConfidence(segmentPattern);
    }

    /// <summary>
    /// Analyzes a field to identify value patterns and structures.
    /// </summary>
    /// <param name="fieldKey">The field identifier (e.g., "PID.3").</param>
    /// <param name="fieldValue">The field value.</param>
    /// <param name="segmentPattern">Parent segment pattern.</param>
    private void AnalyzeField(string fieldKey, string fieldValue, SegmentPattern segmentPattern)
    {
        if (!_fieldPatterns.ContainsKey(fieldKey))
        {
            _fieldPatterns[fieldKey] = new FieldPattern
            {
                FieldKey = fieldKey,
                Occurrences = 0,
                Values = new List<string>(),
                Patterns = new HashSet<string>(),
                ComponentStructure = new Dictionary<int, ComponentPattern>()
            };
        }

        var fieldPattern = _fieldPatterns[fieldKey];
        fieldPattern.Occurrences++;
        fieldPattern.Values.Add(fieldValue);

        // Analyze component structure if composite field
        if (fieldValue.Contains('^'))
        {
            AnalyzeCompositeField(fieldPattern, fieldValue);
        }

        // Identify patterns
        IdentifyValuePatterns(fieldPattern, fieldValue);

        // Update segment pattern
        segmentPattern.FieldPatterns[fieldKey] = fieldPattern;

        // Calculate field confidence
        fieldPattern.Confidence = CalculateFieldConfidence(fieldPattern);
    }

    /// <summary>
    /// Analyzes composite field structure (fields with ^ separators).
    /// </summary>
    /// <param name="fieldPattern">The field pattern to update.</param>
    /// <param name="fieldValue">The composite field value.</param>
    private void AnalyzeCompositeField(FieldPattern fieldPattern, string fieldValue)
    {
        var components = ComponentCache.GetComponents(fieldValue, '^');
        
        for (int i = 0; i < components.Length; i++)
        {
            var component = components[i];
            if (string.IsNullOrEmpty(component)) continue;

            if (!fieldPattern.ComponentStructure.ContainsKey(i + 1))
            {
                fieldPattern.ComponentStructure[i + 1] = new ComponentPattern
                {
                    Position = i + 1,
                    Occurrences = 0,
                    Values = new List<string>(),
                    Patterns = new HashSet<string>()
                };
            }

            var componentPattern = fieldPattern.ComponentStructure[i + 1];
            componentPattern.Occurrences++;
            componentPattern.Values.Add(component);
            
            // Identify component-specific patterns
            IdentifyValuePatterns(componentPattern, component);
        }
    }

    /// <summary>
    /// Identifies value patterns in field or component data.
    /// </summary>
    /// <param name="pattern">The pattern object to update.</param>
    /// <param name="value">The value to analyze.</param>
    private static void IdentifyValuePatterns(IPatternContainer pattern, string value)
    {
        // Numeric patterns
        if (Regex.IsMatch(value, @"^\d+$"))
            pattern.Patterns.Add("numeric");
        
        if (Regex.IsMatch(value, @"^\d+\.\d+$"))
            pattern.Patterns.Add("decimal");

        // Date/time patterns
        if (Regex.IsMatch(value, @"^\d{8}$"))
            pattern.Patterns.Add("date_yyyymmdd");
        
        if (Regex.IsMatch(value, @"^\d{14}$"))
            pattern.Patterns.Add("timestamp_yyyymmddhhmmss");

        if (Regex.IsMatch(value, @"^\d{4}-\d{2}-\d{2}$"))
            pattern.Patterns.Add("date_iso");

        // Identifier patterns
        if (Regex.IsMatch(value, @"^[A-Z]{2,3}\d+$"))
            pattern.Patterns.Add("alpha_prefix_numeric");
        
        if (Regex.IsMatch(value, @"^\d{7}$"))
            pattern.Patterns.Add("seven_digit_id");

        if (Regex.IsMatch(value, @"^\d{10}$"))
            pattern.Patterns.Add("ten_digit_id");

        // Name patterns
        if (value.Contains('^') && Regex.IsMatch(value, @"^[A-Za-z]+\^[A-Za-z]+"))
            pattern.Patterns.Add("name_last_first");

        // Length patterns
        pattern.Patterns.Add($"length_{value.Length}");
        
        // Character type patterns
        if (Regex.IsMatch(value, @"^[A-Z]+$"))
            pattern.Patterns.Add("uppercase_alpha");
        
        if (Regex.IsMatch(value, @"^[a-z]+$"))
            pattern.Patterns.Add("lowercase_alpha");
        
        if (Regex.IsMatch(value, @"^[A-Za-z0-9]+$"))
            pattern.Patterns.Add("alphanumeric");
    }

    /// <summary>
    /// Extracts vendor signatures from MSH segment.
    /// </summary>
    /// <param name="message">Complete HL7 message.</param>
    private void ExtractVendorSignatures(string message)
    {
        var mshLine = message.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault(line => line.StartsWith("MSH"));

        if (mshLine == null) return;

        var mshFields = ComponentCache.GetComponents(mshLine, '|');
        
        // Extract sending application (MSH.3)
        if (mshFields.Length > 3 && !string.IsNullOrEmpty(mshFields[3]))
        {
            var sendingApp = mshFields[3].ToUpperInvariant();
            _vendorSignatures[sendingApp] = _vendorSignatures.GetValueOrDefault(sendingApp, 0) + 1;
        }

        // Extract sending facility (MSH.4)
        if (mshFields.Length > 4 && !string.IsNullOrEmpty(mshFields[4]))
        {
            var sendingFacility = mshFields[4].ToUpperInvariant();
            _vendorSignatures[$"FACILITY_{sendingFacility}"] = _vendorSignatures.GetValueOrDefault($"FACILITY_{sendingFacility}", 0) + 1;
        }
    }

    /// <summary>
    /// Builds segment configuration from analyzed patterns.
    /// </summary>
    /// <param name="segmentPattern">The segment pattern.</param>
    /// <param name="confidenceThreshold">Minimum confidence threshold.</param>
    /// <returns>Segment configuration dictionary.</returns>
    private Dictionary<string, object> BuildSegmentConfig(SegmentPattern segmentPattern, double confidenceThreshold)
    {
        var config = new Dictionary<string, object>();

        foreach (var fieldPattern in segmentPattern.FieldPatterns.Values)
        {
            if (fieldPattern.Confidence >= confidenceThreshold)
            {
                var fieldConfig = BuildFieldConfig(fieldPattern);
                if (fieldConfig != null)
                {
                    // Extract field number from key (e.g., "PID.3" -> "3")
                    var fieldNumber = fieldPattern.FieldKey.Split('.').Last();
                    config[fieldNumber] = fieldConfig;
                }
            }
        }

        return config;
    }

    /// <summary>
    /// Builds field configuration from analyzed patterns.
    /// </summary>
    /// <param name="fieldPattern">The field pattern.</param>
    /// <returns>Field configuration object or null if insufficient data.</returns>
    private object? BuildFieldConfig(FieldPattern fieldPattern)
    {
        if (fieldPattern.Occurrences < 3) return null; // Need minimum occurrences

        // Determine most common pattern
        var dominantPattern = fieldPattern.Patterns
            .GroupBy(p => p)
            .OrderByDescending(g => g.Count())
            .FirstOrDefault()?.Key;

        if (dominantPattern == null) return "populated";

        // For composite fields, build component structure
        if (fieldPattern.ComponentStructure.Any())
        {
            var componentConfig = new Dictionary<string, object>();
            foreach (var comp in fieldPattern.ComponentStructure)
            {
                if (comp.Value.Occurrences >= fieldPattern.Occurrences * 0.5) // Present in at least 50% of cases
                {
                    var compPattern = comp.Value.Patterns.FirstOrDefault() ?? "populated";
                    componentConfig[comp.Key.ToString()] = compPattern;
                }
            }
            return componentConfig.Any() ? componentConfig : "composite_populated";
        }

        return dominantPattern;
    }

    /// <summary>
    /// Infers message-level patterns.
    /// </summary>
    /// <returns>Message patterns dictionary.</returns>
    private Dictionary<string, object> InferMessagePatterns()
    {
        var patterns = new Dictionary<string, object>();

        // Analyze timestamp formats across all timestamp fields
        var timestampPatterns = _fieldPatterns.Values
            .Where(fp => fp.Patterns.Any(p => p.Contains("timestamp") || p.Contains("date")))
            .SelectMany(fp => fp.Patterns)
            .Where(p => p.Contains("timestamp") || p.Contains("date"))
            .GroupBy(p => p)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .ToList();

        if (timestampPatterns.Any())
        {
            patterns["timestamp_format"] = timestampPatterns.First();
        }

        // Analyze identifier formats
        var identifierPatterns = _fieldPatterns.Values
            .Where(fp => fp.FieldKey.Contains(".3") || fp.FieldKey.Contains(".2")) // Common ID fields
            .SelectMany(fp => fp.Patterns)
            .Where(p => p.Contains("digit") || p.Contains("alpha"))
            .GroupBy(p => p)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .ToList();

        if (identifierPatterns.Any())
        {
            patterns["identifier_format"] = identifierPatterns.First();
        }

        return patterns;
    }

    /// <summary>
    /// Generates validation rules based on identified patterns.
    /// </summary>
    /// <param name="confidenceThreshold">Minimum confidence for rule generation.</param>
    /// <returns>List of validation rules.</returns>
    private List<ValidationRule> GenerateValidationRules(double confidenceThreshold)
    {
        var rules = new List<ValidationRule>();

        foreach (var fieldPattern in _fieldPatterns.Values)
        {
            if (fieldPattern.Confidence >= confidenceThreshold && fieldPattern.Occurrences >= 5)
            {
                // Generate length validation rules
                var lengths = fieldPattern.Values.Select(v => v.Length).ToList();
                if (lengths.All(l => l == lengths.First()))
                {
                    rules.Add(new ValidationRule
                    {
                        Field = fieldPattern.FieldKey,
                        Rule = $"length_exactly_{lengths.First()}",
                        Message = $"{fieldPattern.FieldKey} must be exactly {lengths.First()} characters",
                        Confidence = fieldPattern.Confidence
                    });
                }

                // Generate format validation rules
                if (fieldPattern.Patterns.Contains("numeric") && fieldPattern.Patterns.Count(p => p.StartsWith("length_")) == 1)
                {
                    var length = fieldPattern.Patterns.First(p => p.StartsWith("length_")).Replace("length_", "");
                    rules.Add(new ValidationRule
                    {
                        Field = fieldPattern.FieldKey,
                        Rule = $"numeric_length_{length}",
                        Message = $"{fieldPattern.FieldKey} must be {length} digits",
                        Confidence = fieldPattern.Confidence
                    });
                }
            }
        }

        return rules;
    }

    private double CalculateOverallConfidence()
    {
        if (!_fieldPatterns.Any()) return 0.0;
        return _fieldPatterns.Values.Average(fp => fp.Confidence);
    }

    private static double CalculateSegmentConfidence(SegmentPattern segmentPattern)
    {
        if (segmentPattern.Occurrences < 3) return 0.5;
        if (segmentPattern.FieldPatterns.Count < 2) return 0.6;
        
        var fieldConfidences = segmentPattern.FieldPatterns.Values.Select(fp => fp.Confidence);
        return fieldConfidences.Any() ? fieldConfidences.Average() : 0.7;
    }

    private static double CalculateFieldConfidence(FieldPattern fieldPattern)
    {
        if (fieldPattern.Occurrences < 2) return 0.4;
        if (fieldPattern.Occurrences < 5) return 0.6;
        
        // Higher confidence for consistent patterns
        var patternConsistency = fieldPattern.Patterns.Count <= 3 ? 0.9 : 0.7;
        var occurrenceScore = Math.Min(fieldPattern.Occurrences / 10.0, 1.0);
        
        return (patternConsistency + occurrenceScore) / 2.0;
    }
}