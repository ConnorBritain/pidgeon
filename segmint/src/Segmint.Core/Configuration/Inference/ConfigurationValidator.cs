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
/// Validates HL7 messages against vendor-specific configurations to detect deviations.
/// </summary>
public class ConfigurationValidator
{
    private readonly VendorConfiguration _configuration;
    private readonly Dictionary<string, Regex> _compiledPatterns = new();

    /// <summary>
    /// Initializes a new instance of the ConfigurationValidator class.
    /// </summary>
    /// <param name="configuration">The vendor configuration to validate against.</param>
    public ConfigurationValidator(VendorConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        PrecompilePatterns();
    }

    /// <summary>
    /// Initializes a new instance from a JSON configuration file.
    /// </summary>
    /// <param name="configurationJson">JSON string containing the vendor configuration.</param>
    public ConfigurationValidator(string configurationJson)
    {
        if (string.IsNullOrWhiteSpace(configurationJson))
            throw new ArgumentException("Configuration JSON cannot be empty", nameof(configurationJson));

        _configuration = JsonSerializer.Deserialize<VendorConfiguration>(configurationJson) 
            ?? throw new ArgumentException("Invalid configuration JSON", nameof(configurationJson));
        
        PrecompilePatterns();
    }

    /// <summary>
    /// Validates an HL7 message against the vendor configuration.
    /// </summary>
    /// <param name="hl7Message">The HL7 message to validate.</param>
    /// <returns>Validation result with conformance score and detected deviations.</returns>
    public ConfigurationValidationResult ValidateMessage(string hl7Message)
    {
        if (string.IsNullOrWhiteSpace(hl7Message))
        {
            return new ConfigurationValidationResult
            {
                IsValid = false,
                OverallConformance = 0.0,
                ConfigurationId = _configuration.ConfigurationId,
                Deviations = new List<PatternDeviation>
                {
                    new PatternDeviation
                    {
                        Field = "MESSAGE",
                        Expected = "non-empty message",
                        Actual = "empty or null",
                        DeviationType = DeviationType.MissingField,
                        Severity = ValidationSeverity.Critical,
                        Confidence = 1.0,
                        Description = "Message cannot be empty"
                    }
                }
            };
        }

        var result = new ConfigurationValidationResult
        {
            ConfigurationId = _configuration.ConfigurationId,
            Deviations = new List<PatternDeviation>()
        };

        // Parse message into segments
        var messageSegments = ParseMessageSegments(hl7Message);
        
        // Validate each configured segment
        var totalChecks = 0;
        var passedChecks = 0;

        foreach (var segmentConfig in _configuration.Segments)
        {
            var segmentId = segmentConfig.Key;
            var expectedFields = segmentConfig.Value;

            if (messageSegments.TryGetValue(segmentId, out var actualSegment))
            {
                var segmentResult = ValidateSegment(segmentId, actualSegment, expectedFields);
                result.Deviations.AddRange(segmentResult.Deviations);
                totalChecks += segmentResult.TotalChecks;
                passedChecks += segmentResult.PassedChecks;
            }
            else
            {
                // Segment missing - check if it's critical
                var isCritical = IsSegmentCritical(segmentId);
                result.Deviations.Add(new PatternDeviation
                {
                    Field = segmentId,
                    Expected = "segment present",
                    Actual = "segment missing",
                    DeviationType = DeviationType.MissingField,
                    Severity = isCritical ? ValidationSeverity.Error : ValidationSeverity.Warning,
                    Confidence = 0.9,
                    Description = $"Expected {segmentId} segment is missing"
                });
                totalChecks++;
            }
        }

        // Apply vendor-specific validation rules
        var ruleResults = ApplyValidationRules(hl7Message);
        result.Deviations.AddRange(ruleResults.Deviations);
        totalChecks += ruleResults.TotalChecks;
        passedChecks += ruleResults.PassedChecks;

        // Calculate overall conformance
        result.OverallConformance = totalChecks > 0 ? (double)passedChecks / totalChecks : 1.0;
        result.IsValid = result.OverallConformance >= 0.8 && !result.Deviations.Any(d => d.Severity == ValidationSeverity.Critical);

        return result;
    }

    /// <summary>
    /// Compares two vendor configurations to identify differences.
    /// </summary>
    /// <param name="otherConfiguration">Configuration to compare against.</param>
    /// <returns>Configuration diff result.</returns>
    public ConfigurationDiff CompareConfigurations(VendorConfiguration otherConfiguration)
    {
        var diff = new ConfigurationDiff
        {
            BaselineConfigId = _configuration.ConfigurationId,
            TargetConfigId = otherConfiguration.ConfigurationId,
            Differences = new List<ConfigurationDifference>()
        };

        // Compare segments
        CompareSegments(_configuration.Segments, otherConfiguration.Segments, diff.Differences);

        // Compare patterns
        ComparePatterns(_configuration.Patterns, otherConfiguration.Patterns, diff.Differences);

        // Compare validation rules
        CompareValidationRules(_configuration.ValidationRules, otherConfiguration.ValidationRules, diff.Differences);

        // Calculate similarity score
        diff.Similarity = CalculateSimilarity(diff.Differences);

        return diff;
    }

    /// <summary>
    /// Parses an HL7 message into a dictionary of segments.
    /// </summary>
    /// <param name="hl7Message">The HL7 message to parse.</param>
    /// <returns>Dictionary mapping segment IDs to segment data.</returns>
    private static Dictionary<string, string> ParseMessageSegments(string hl7Message)
    {
        var segments = new Dictionary<string, string>();
        var lines = hl7Message.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            if (line.Length >= 3)
            {
                var segmentId = line.Substring(0, 3);
                segments[segmentId] = line;
            }
        }

        return segments;
    }

    /// <summary>
    /// Validates a single segment against its configuration.
    /// </summary>
    /// <param name="segmentId">The segment identifier.</param>
    /// <param name="segmentData">The actual segment data.</param>
    /// <param name="expectedFields">Expected field configuration.</param>
    /// <returns>Segment validation result.</returns>
    private SegmentValidationResult ValidateSegment(string segmentId, string segmentData, Dictionary<string, object> expectedFields)
    {
        var result = new SegmentValidationResult
        {
            Deviations = new List<PatternDeviation>(),
            TotalChecks = 0,
            PassedChecks = 0
        };

        var actualFields = ComponentCache.GetComponents(segmentData, '|');

        foreach (var expectedField in expectedFields)
        {
            if (!int.TryParse(expectedField.Key, out var fieldIndex))
                continue;

            result.TotalChecks++;
            var fieldKey = $"{segmentId}.{fieldIndex}";

            if (fieldIndex < actualFields.Length)
            {
                var actualValue = actualFields[fieldIndex];
                var expectedPattern = expectedField.Value;

                var fieldResult = ValidateField(fieldKey, actualValue, expectedPattern);
                if (fieldResult.IsValid)
                {
                    result.PassedChecks++;
                }
                else
                {
                    result.Deviations.AddRange(fieldResult.Deviations);
                }
            }
            else
            {
                // Field missing
                result.Deviations.Add(new PatternDeviation
                {
                    Field = fieldKey,
                    Expected = "field present",
                    Actual = "field missing",
                    DeviationType = DeviationType.MissingField,
                    Severity = ValidationSeverity.Warning,
                    Confidence = 0.8,
                    Description = $"Expected field {fieldKey} is missing"
                });
            }
        }

        return result;
    }

    /// <summary>
    /// Validates a single field against its expected pattern.
    /// </summary>
    /// <param name="fieldKey">The field identifier.</param>
    /// <param name="actualValue">The actual field value.</param>
    /// <param name="expectedPattern">The expected pattern or configuration.</param>
    /// <returns>Field validation result.</returns>
    private FieldValidationResult ValidateField(string fieldKey, string actualValue, object expectedPattern)
    {
        var result = new FieldValidationResult
        {
            IsValid = true,
            Deviations = new List<PatternDeviation>()
        };

        if (string.IsNullOrEmpty(actualValue))
        {
            // Field is empty - this might be acceptable depending on configuration
            return result;
        }

        switch (expectedPattern)
        {
            case string pattern:
                ValidateStringPattern(fieldKey, actualValue, pattern, result);
                break;
            
            case Dictionary<string, object> compositePattern:
                ValidateCompositeField(fieldKey, actualValue, compositePattern, result);
                break;
            
            case JsonElement jsonElement:
                ValidateJsonPattern(fieldKey, actualValue, jsonElement, result);
                break;
        }

        return result;
    }

    /// <summary>
    /// Validates a field against a string pattern.
    /// </summary>
    /// <param name="fieldKey">The field identifier.</param>
    /// <param name="actualValue">The actual field value.</param>
    /// <param name="pattern">The expected pattern.</param>
    /// <param name="result">The validation result to update.</param>
    private void ValidateStringPattern(string fieldKey, string actualValue, string pattern, FieldValidationResult result)
    {
        switch (pattern)
        {
            case "numeric":
                if (!Regex.IsMatch(actualValue, @"^\d+$"))
                {
                    AddPatternDeviation(result, fieldKey, "numeric value", actualValue, DeviationType.FormatMismatch, ValidationSeverity.Warning);
                }
                break;

            case "date_yyyymmdd":
                if (!Regex.IsMatch(actualValue, @"^\d{8}$"))
                {
                    AddPatternDeviation(result, fieldKey, "YYYYMMDD date format", actualValue, DeviationType.FormatMismatch, ValidationSeverity.Warning);
                }
                break;

            case "timestamp_yyyymmddhhmmss":
                if (!Regex.IsMatch(actualValue, @"^\d{14}$"))
                {
                    AddPatternDeviation(result, fieldKey, "YYYYMMDDHHMMSS timestamp format", actualValue, DeviationType.FormatMismatch, ValidationSeverity.Warning);
                }
                break;

            case "seven_digit_id":
                if (!Regex.IsMatch(actualValue, @"^\d{7}$"))
                {
                    AddPatternDeviation(result, fieldKey, "7-digit identifier", actualValue, DeviationType.FormatMismatch, ValidationSeverity.Error);
                }
                break;

            case "ten_digit_id":
                if (!Regex.IsMatch(actualValue, @"^\d{10}$"))
                {
                    AddPatternDeviation(result, fieldKey, "10-digit identifier", actualValue, DeviationType.FormatMismatch, ValidationSeverity.Error);
                }
                break;

            case var lengthPattern when lengthPattern.StartsWith("length_"):
                if (int.TryParse(lengthPattern.Replace("length_", ""), out var expectedLength))
                {
                    if (actualValue.Length != expectedLength)
                    {
                        AddPatternDeviation(result, fieldKey, $"length {expectedLength}", $"length {actualValue.Length}", DeviationType.LengthMismatch, ValidationSeverity.Warning);
                    }
                }
                break;
        }
    }

    /// <summary>
    /// Validates a composite field against its component patterns.
    /// </summary>
    /// <param name="fieldKey">The field identifier.</param>
    /// <param name="actualValue">The actual field value.</param>
    /// <param name="compositePattern">The expected composite pattern.</param>
    /// <param name="result">The validation result to update.</param>
    private void ValidateCompositeField(string fieldKey, string actualValue, Dictionary<string, object> compositePattern, FieldValidationResult result)
    {
        var components = ComponentCache.GetComponents(actualValue, '^');

        foreach (var expectedComponent in compositePattern)
        {
            if (int.TryParse(expectedComponent.Key, out var componentIndex) && componentIndex <= components.Length)
            {
                var actualComponent = componentIndex <= components.Length ? components[componentIndex - 1] : "";
                var expectedComponentPattern = expectedComponent.Value;

                var componentKey = $"{fieldKey}.{componentIndex}";
                var componentResult = ValidateField(componentKey, actualComponent, expectedComponentPattern);
                
                if (!componentResult.IsValid)
                {
                    result.Deviations.AddRange(componentResult.Deviations);
                    result.IsValid = false;
                }
            }
        }
    }

    /// <summary>
    /// Validates a field against a JSON element pattern.
    /// </summary>
    /// <param name="fieldKey">The field identifier.</param>
    /// <param name="actualValue">The actual field value.</param>
    /// <param name="jsonElement">The JSON pattern element.</param>
    /// <param name="result">The validation result to update.</param>
    private void ValidateJsonPattern(string fieldKey, string actualValue, JsonElement jsonElement, FieldValidationResult result)
    {
        switch (jsonElement.ValueKind)
        {
            case JsonValueKind.String:
                ValidateStringPattern(fieldKey, actualValue, jsonElement.GetString() ?? "", result);
                break;
            
            case JsonValueKind.Object:
                var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonElement.GetRawText());
                if (dict != null)
                {
                    ValidateCompositeField(fieldKey, actualValue, dict, result);
                }
                break;
        }
    }

    /// <summary>
    /// Applies vendor-specific validation rules to the message.
    /// </summary>
    /// <param name="hl7Message">The HL7 message to validate.</param>
    /// <returns>Rule validation result.</returns>
    private RuleValidationResult ApplyValidationRules(string hl7Message)
    {
        var result = new RuleValidationResult
        {
            Deviations = new List<PatternDeviation>(),
            TotalChecks = _configuration.ValidationRules.Count,
            PassedChecks = 0
        };

        var messageSegments = ParseMessageSegments(hl7Message);

        foreach (var rule in _configuration.ValidationRules)
        {
            var ruleResult = ApplyValidationRule(rule, messageSegments);
            if (ruleResult.IsValid)
            {
                result.PassedChecks++;
            }
            else
            {
                result.Deviations.AddRange(ruleResult.Deviations);
            }
        }

        return result;
    }

    /// <summary>
    /// Applies a single validation rule.
    /// </summary>
    /// <param name="rule">The validation rule to apply.</param>
    /// <param name="messageSegments">Parsed message segments.</param>
    /// <returns>Rule application result.</returns>
    private FieldValidationResult ApplyValidationRule(ValidationRule rule, Dictionary<string, string> messageSegments)
    {
        var result = new FieldValidationResult { IsValid = true, Deviations = new List<PatternDeviation>() };

        // Extract field value from message
        var fieldValue = ExtractFieldValue(rule.Field, messageSegments);

        // Apply rule logic
        var ruleIsValid = EvaluateRule(rule.Rule, fieldValue);

        if (!ruleIsValid)
        {
            result.IsValid = false;
            result.Deviations.Add(new PatternDeviation
            {
                Field = rule.Field,
                Expected = rule.Rule,
                Actual = fieldValue ?? "null",
                DeviationType = DeviationType.ValueMismatch,
                Severity = rule.Severity,
                Confidence = rule.Confidence,
                Description = rule.Message
            });
        }

        return result;
    }

    /// <summary>
    /// Extracts a field value from parsed message segments.
    /// </summary>
    /// <param name="fieldPath">The field path (e.g., "PID.3.1").</param>
    /// <param name="messageSegments">Parsed message segments.</param>
    /// <returns>The field value or null if not found.</returns>
    private static string? ExtractFieldValue(string fieldPath, Dictionary<string, string> messageSegments)
    {
        var pathParts = fieldPath.Split('.');
        if (pathParts.Length < 2) return null;

        var segmentId = pathParts[0];
        if (!messageSegments.TryGetValue(segmentId, out var segmentData)) return null;

        var fields = ComponentCache.GetComponents(segmentData, '|');
        if (!int.TryParse(pathParts[1], out var fieldIndex) || fieldIndex >= fields.Length) return null;

        var fieldValue = fields[fieldIndex];

        // Handle component access (e.g., PID.3.1)
        if (pathParts.Length > 2 && int.TryParse(pathParts[2], out var componentIndex))
        {
            var components = ComponentCache.GetComponents(fieldValue, '^');
            return componentIndex <= components.Length ? components[componentIndex - 1] : null;
        }

        return fieldValue;
    }

    /// <summary>
    /// Evaluates a validation rule against a field value.
    /// </summary>
    /// <param name="rule">The rule expression.</param>
    /// <param name="fieldValue">The field value to test.</param>
    /// <returns>True if the rule passes, false otherwise.</returns>
    private bool EvaluateRule(string rule, string? fieldValue)
    {
        switch (rule)
        {
            case "not_empty":
                return !string.IsNullOrEmpty(fieldValue);
            
            case var lengthRule when lengthRule.StartsWith("length_exactly_"):
                if (int.TryParse(lengthRule.Replace("length_exactly_", ""), out var expectedLength))
                {
                    return fieldValue?.Length == expectedLength;
                }
                break;
            
            case var numericRule when numericRule.StartsWith("numeric_length_"):
                if (int.TryParse(numericRule.Replace("numeric_length_", ""), out var expectedNumericLength))
                {
                    return fieldValue != null && 
                           Regex.IsMatch(fieldValue, @"^\d+$") && 
                           fieldValue.Length == expectedNumericLength;
                }
                break;
            
            case var regexRule when regexRule.StartsWith("matches_"):
                var pattern = regexRule.Replace("matches_", "");
                return fieldValue != null && _compiledPatterns.TryGetValue(pattern, out var regex) && regex.IsMatch(fieldValue);
        }

        return true; // Unknown rules pass by default
    }

    /// <summary>
    /// Pre-compiles regex patterns for performance.
    /// </summary>
    private void PrecompilePatterns()
    {
        foreach (var rule in _configuration.ValidationRules)
        {
            if (rule.Rule.StartsWith("matches_"))
            {
                var pattern = rule.Rule.Replace("matches_", "");
                try
                {
                    _compiledPatterns[pattern] = new Regex(pattern, RegexOptions.Compiled);
                }
                catch (ArgumentException)
                {
                    // Invalid regex pattern - skip
                }
            }
        }
    }

    private static void AddPatternDeviation(FieldValidationResult result, string field, string expected, string actual, DeviationType type, ValidationSeverity severity)
    {
        result.IsValid = false;
        result.Deviations.Add(new PatternDeviation
        {
            Field = field,
            Expected = expected,
            Actual = actual,
            DeviationType = type,
            Severity = severity,
            Confidence = 0.8,
            Description = $"Field {field} does not match expected pattern"
        });
    }

    private bool IsSegmentCritical(string segmentId)
    {
        // Critical segments that should always be present
        return segmentId is "MSH" or "PID";
    }

    private void CompareSegments(Dictionary<string, Dictionary<string, object>> baseline, Dictionary<string, Dictionary<string, object>> target, List<ConfigurationDifference> differences)
    {
        // Implementation for segment comparison
        foreach (var baselineSegment in baseline)
        {
            if (!target.ContainsKey(baselineSegment.Key))
            {
                differences.Add(new ConfigurationDifference
                {
                    Type = DifferenceType.Removed,
                    Path = $"segments.{baselineSegment.Key}",
                    BaselineValue = baselineSegment.Value,
                    TargetValue = null,
                    Impact = ImpactLevel.Medium,
                    Description = $"Segment {baselineSegment.Key} removed"
                });
            }
        }

        foreach (var targetSegment in target)
        {
            if (!baseline.ContainsKey(targetSegment.Key))
            {
                differences.Add(new ConfigurationDifference
                {
                    Type = DifferenceType.Added,
                    Path = $"segments.{targetSegment.Key}",
                    BaselineValue = null,
                    TargetValue = targetSegment.Value,
                    Impact = ImpactLevel.Medium,
                    Description = $"Segment {targetSegment.Key} added"
                });
            }
        }
    }

    private static void ComparePatterns(Dictionary<string, object> baseline, Dictionary<string, object> target, List<ConfigurationDifference> differences)
    {
        // Implementation for pattern comparison
        foreach (var baselinePattern in baseline)
        {
            if (target.TryGetValue(baselinePattern.Key, out var targetValue))
            {
                if (!Equals(baselinePattern.Value, targetValue))
                {
                    differences.Add(new ConfigurationDifference
                    {
                        Type = DifferenceType.Modified,
                        Path = $"patterns.{baselinePattern.Key}",
                        BaselineValue = baselinePattern.Value,
                        TargetValue = targetValue,
                        Impact = ImpactLevel.Low,
                        Description = $"Pattern {baselinePattern.Key} modified"
                    });
                }
            }
            else
            {
                differences.Add(new ConfigurationDifference
                {
                    Type = DifferenceType.Removed,
                    Path = $"patterns.{baselinePattern.Key}",
                    BaselineValue = baselinePattern.Value,
                    TargetValue = null,
                    Impact = ImpactLevel.Low,
                    Description = $"Pattern {baselinePattern.Key} removed"
                });
            }
        }
    }

    private static void CompareValidationRules(List<ValidationRule> baseline, List<ValidationRule> target, List<ConfigurationDifference> differences)
    {
        // Implementation for validation rule comparison
        var baselineRules = baseline.ToDictionary(r => r.Field + ":" + r.Rule);
        var targetRules = target.ToDictionary(r => r.Field + ":" + r.Rule);

        foreach (var baselineRule in baselineRules)
        {
            if (!targetRules.ContainsKey(baselineRule.Key))
            {
                differences.Add(new ConfigurationDifference
                {
                    Type = DifferenceType.Removed,
                    Path = $"validation_rules.{baselineRule.Key}",
                    BaselineValue = baselineRule.Value,
                    TargetValue = null,
                    Impact = ImpactLevel.High,
                    Description = $"Validation rule {baselineRule.Key} removed"
                });
            }
        }
    }

    private static double CalculateSimilarity(List<ConfigurationDifference> differences)
    {
        if (!differences.Any()) return 1.0;

        var weightedDifferences = differences.Sum(d => d.Impact switch
        {
            ImpactLevel.Low => 1,
            ImpactLevel.Medium => 3,
            ImpactLevel.High => 7,
            ImpactLevel.Critical => 15,
            _ => 1
        });

        // Normalize to 0-1 scale (assuming 100 is maximum reasonable difference score)
        return Math.Max(0, 1.0 - (weightedDifferences / 100.0));
    }
}

/// <summary>
/// Internal result class for segment validation.
/// </summary>
internal class SegmentValidationResult
{
    public List<PatternDeviation> Deviations { get; set; } = new();
    public int TotalChecks { get; set; }
    public int PassedChecks { get; set; }
}

/// <summary>
/// Internal result class for field validation.
/// </summary>
internal class FieldValidationResult
{
    public bool IsValid { get; set; }
    public List<PatternDeviation> Deviations { get; set; } = new();
}

/// <summary>
/// Internal result class for rule validation.
/// </summary>
internal class RuleValidationResult
{
    public List<PatternDeviation> Deviations { get; set; } = new();
    public int TotalChecks { get; set; }
    public int PassedChecks { get; set; }
}