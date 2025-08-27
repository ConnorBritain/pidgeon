// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Microsoft.Extensions.Logging;
using Pidgeon.Core.Standards.Common;
using Pidgeon.Core.Domain.Configuration.Entities;
using System.Text.RegularExpressions;

namespace Pidgeon.Core.Standards.HL7.v23.Configuration;

/// <summary>
/// HL7 v2.3 specific field analysis plugin.
/// Implements MSH.3/MSH.4 fingerprinting algorithms, segment field frequency analysis,
/// and component pattern detection for HL7-specific structures like XPN, XAD, CE.
/// All HL7-specific field analysis logic lives here.
/// </summary>
internal class HL7FieldAnalysisPlugin : IStandardFieldAnalysisPlugin
{
    private readonly ILogger<HL7FieldAnalysisPlugin> _logger;

    // HL7 parsing patterns
    private static readonly Regex SegmentPattern = new(@"^([A-Z0-9]{3})\|", RegexOptions.Compiled);
    private static readonly Regex FieldSeparatorPattern = new(@"\|", RegexOptions.Compiled);
    private static readonly Regex ComponentSeparatorPattern = new(@"\^", RegexOptions.Compiled);
    private static readonly Regex SubComponentSeparatorPattern = new(@"&", RegexOptions.Compiled);

    // HL7 data types that have components
    private static readonly HashSet<string> CompositeDataTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "XPN", "XAD", "CE", "CX", "PL", "XCN", "XON", "XTN", "TS", "HD", "EI", "DR", "TQ"
    };

    public HL7FieldAnalysisPlugin(ILogger<HL7FieldAnalysisPlugin> logger)
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
    public async Task<Result<FieldPatterns>> AnalyzeFieldPatternsAsync(
        IEnumerable<string> messages,
        string messageType)
    {
        try
        {
            var messageList = messages.ToList();
            _logger.LogInformation("Analyzing HL7 field patterns for {MessageCount} {MessageType} messages",
                messageList.Count, messageType);

            var segmentPatterns = new Dictionary<string, SegmentPattern>();

            // Analyze each message
            foreach (var message in messageList)
            {
                if (string.IsNullOrWhiteSpace(message))
                    continue;

                var segments = message.Split('\r', '\n')
                    .Where(line => !string.IsNullOrWhiteSpace(line))
                    .ToList();

                foreach (var segment in segments)
                {
                    var segmentResult = await AnalyzeHL7Segment(segment);
                    if (segmentResult.IsSuccess)
                    {
                        var pattern = segmentResult.Value;
                        if (segmentPatterns.ContainsKey(pattern.SegmentType))
                        {
                            // Merge patterns
                            segmentPatterns[pattern.SegmentType] = MergeSegmentPatterns(
                                segmentPatterns[pattern.SegmentType], pattern);
                        }
                        else
                        {
                            segmentPatterns[pattern.SegmentType] = pattern;
                        }
                    }
                }
            }

            // Convert to SegmentFieldPatterns format
            var segmentFieldPatterns = new Dictionary<string, SegmentFieldPatterns>();
            foreach (var kvp in segmentPatterns)
            {
                segmentFieldPatterns[kvp.Key] = new SegmentFieldPatterns
                {
                    FieldFrequencies = (kvp.Value.FieldFrequencies ?? new Dictionary<int, FieldFrequency>()).ToDictionary(
                        f => f.Key,
                        f => new FieldFrequency
                        {
                            FieldName = f.Key.ToString(),
                            Frequency = f.Value.Frequency,
                            TotalOccurrences = f.Value.TotalOccurrences,
                            UniqueValues = f.Value.UniqueValues,
                            CommonValues = f.Value.CommonValues,
                            AverageLength = f.Value.AverageLength,
                            ComponentPatterns = f.Value.ComponentPatterns
                        }) ?? new Dictionary<string, FieldFrequency>()
                };
            }

            var patterns = new FieldPatterns
            {
                Standard = StandardName,
                MessageType = messageType,
                SegmentPatterns = segmentFieldPatterns,
                Confidence = CalculatePatternConfidence(segmentFieldPatterns, messageList.Count),
                AnalysisDate = DateTime.UtcNow
            };

            _logger.LogInformation("HL7 field pattern analysis completed with {SegmentCount} segment types and {Confidence:P1} confidence",
                segmentPatterns.Count, patterns.Confidence);

            return await Task.FromResult(Result<FieldPatterns>.Success(patterns));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing HL7 field patterns");
            return Result<FieldPatterns>.Failure($"HL7 field pattern analysis failed: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result<SegmentPattern>> AnalyzeSegmentPatternsAsync(
        IEnumerable<string> segments,
        string segmentType)
    {
        try
        {
            var segmentList = segments.ToList();
            _logger.LogDebug("Analyzing HL7 segment patterns for {SegmentCount} {SegmentType} segments",
                segmentList.Count, segmentType);

            var fieldFrequencies = new Dictionary<string, FieldFrequency>();

            foreach (var segment in segmentList)
            {
                var segmentResult = await AnalyzeHL7Segment(segment);
                if (segmentResult.IsSuccess)
                {
                    foreach (var field in (segmentResult.Value.FieldFrequencies ?? new Dictionary<int, FieldFrequency>()).ToDictionary(f => f.Key.ToString(), f => f.Value))
                    {
                        if (fieldFrequencies.ContainsKey(field.Key))
                        {
                            // Merge field frequencies
                            fieldFrequencies[field.Key] = MergeFieldFrequencies(
                                fieldFrequencies[field.Key], field.Value);
                        }
                        else
                        {
                            fieldFrequencies[field.Key] = field.Value;
                        }
                    }
                }
            }

            var pattern = new SegmentPattern
            {
                SegmentType = segmentType,
                FieldFrequencies = fieldFrequencies.ToDictionary(f => int.Parse(f.Key), f => f.Value),
                TotalOccurrences = segmentList.Count,
                Confidence = CalculateSegmentConfidence(fieldFrequencies, segmentList.Count)
            };

            return await Task.FromResult(Result<SegmentPattern>.Success(pattern));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing HL7 segment patterns for {SegmentType}", segmentType);
            return Result<SegmentPattern>.Failure($"HL7 segment analysis failed: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result<ComponentPattern>> AnalyzeComponentPatternsAsync(
        IEnumerable<string> fieldValues,
        string fieldType)
    {
        try
        {
            var valueList = fieldValues.ToList();
            _logger.LogDebug("Analyzing HL7 component patterns for {ValueCount} {FieldType} values",
                valueList.Count, fieldType);

            var componentFrequencies = new Dictionary<string, ComponentFrequency>();

            foreach (var value in valueList.Where(v => !string.IsNullOrWhiteSpace(v)))
            {
                var components = value.Split('^');
                
                for (int i = 0; i < components.Length; i++)
                {
                    var componentKey = $"Component_{i + 1}";
                    var component = components[i].Trim();
                    
                    if (componentFrequencies.ContainsKey(componentKey))
                    {
                        var existing = componentFrequencies[componentKey];
                        componentFrequencies[componentKey] = new ComponentFrequency
                        {
                            ComponentName = existing.ComponentName,
                            Frequency = existing.Frequency + (string.IsNullOrEmpty(component) ? 0 : 1),
                            TotalOccurrences = existing.TotalOccurrences + 1,
                            UniqueValues = existing.UniqueValues.Union(new[] { component }).ToList(),
                            AverageLength = existing.AverageLength // Will recalculate later
                        };
                    }
                    else
                    {
                        componentFrequencies[componentKey] = new ComponentFrequency
                        {
                            ComponentName = GetComponentName(fieldType, i + 1),
                            Frequency = string.IsNullOrEmpty(component) ? 0 : 1,
                            TotalOccurrences = 1,
                            UniqueValues = new List<string> { component },
                            AverageLength = component.Length
                        };
                    }
                }
            }

            // Recalculate averages
            foreach (var kvp in componentFrequencies.ToList())
            {
                var freq = kvp.Value;
                freq.AverageLength = freq.UniqueValues
                    .Where(v => !string.IsNullOrEmpty(v))
                    .Average(v => (double)v.Length);
                componentFrequencies[kvp.Key] = freq;
            }

            var pattern = new ComponentPattern
            {
                FieldType = fieldType,
                ComponentFrequencies = componentFrequencies,
                TotalSamples = valueList.Count,
                StandardName = StandardName
            };

            return await Task.FromResult(Result<ComponentPattern>.Success(pattern));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing HL7 component patterns for {FieldType}", fieldType);
            return Result<ComponentPattern>.Failure($"HL7 component analysis failed: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result<FieldStatistics>> CalculateFieldStatisticsAsync(FieldPatterns patterns)
    {
        try
        {
            var totalFields = patterns.SegmentPatterns.Values
                .SelectMany(s => s.FieldFrequencies.Values)
                .Count();

            var populatedFields = patterns.SegmentPatterns.Values
                .SelectMany(s => s.FieldFrequencies.Values)
                .Count(f => f.Frequency > 0);

            var dataQualityScore = totalFields > 0 ? (double)populatedFields / totalFields : 0.0;

            var statistics = new FieldStatistics
            {
                TotalFields = totalFields,
                PopulatedFields = populatedFields,
                DataQualityScore = dataQualityScore,
                AverageFieldLength = patterns.SegmentPatterns.Values
                    .SelectMany(s => s.FieldFrequencies.Values)
                    .Where(f => f.AverageLength > 0)
                    .Average(f => f.AverageLength),
                MostCommonSegments = patterns.SegmentPatterns
                    .OrderByDescending(kvp => kvp.Value.FieldFrequencies.Values.Sum(f => f.TotalOccurrences))
                    .Take(5)
                    .Select(kvp => kvp.Key)
                    .ToList(),
                AnalysisDate = DateTime.UtcNow
            };

            return await Task.FromResult(Result<FieldStatistics>.Success(statistics));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating HL7 field statistics");
            return Result<FieldStatistics>.Failure($"HL7 statistics calculation failed: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result<double>> CalculateFieldCoverageAsync(FieldPatterns patterns)
    {
        try
        {
            // HL7-specific coverage calculation based on expected segments for message types
            var coverage = patterns.MessageType?.ToUpperInvariant() switch
            {
                var type when type?.StartsWith("ADT") == true => CalculateADTCoverage(patterns),
                var type when type?.StartsWith("RDE") == true => CalculateRDECoverage(patterns),
                var type when type?.StartsWith("ORM") == true => CalculateORMCoverage(patterns),
                var type when type?.StartsWith("ORU") == true => CalculateORUCoverage(patterns),
                _ => CalculateGenericHL7Coverage(patterns)
            };

            _logger.LogDebug("HL7 coverage calculated for {MessageType}: {Coverage:P1}",
                patterns.MessageType, coverage);

            return await Task.FromResult(Result<double>.Success(coverage));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating HL7 field coverage");
            return Result<double>.Failure($"HL7 coverage calculation failed: {ex.Message}");
        }
    }

    #region Private Helper Methods

    private async Task<Result<SegmentPattern>> AnalyzeHL7Segment(string segment)
    {
        try
        {
            var match = SegmentPattern.Match(segment);
            if (!match.Success)
                return Result<SegmentPattern>.Failure("Invalid HL7 segment format");

            var segmentType = match.Groups[1].Value;
            var fields = segment.Split('|');
            var fieldFrequencies = new Dictionary<string, FieldFrequency>();

            for (int i = 1; i < fields.Length; i++) // Skip segment type (index 0)
            {
                var field = fields[i];
                var fieldName = $"{segmentType}.{i}";
                
                fieldFrequencies[fieldName] = new FieldFrequency
                {
                    FieldName = fieldName,
                    Frequency = string.IsNullOrWhiteSpace(field) ? 0 : 1,
                    TotalOccurrences = 1,
                    UniqueValues = new List<string> { field },
                    CommonValues = new List<string> { field },
                    AverageLength = field?.Length ?? 0,
                    ComponentPatterns = await AnalyzeFieldComponents(field, fieldName)
                };
            }

            return Result<SegmentPattern>.Success(new SegmentPattern
            {
                SegmentType = segmentType,
                FieldFrequencies = StringKeysToIntKeys(fieldFrequencies),
                TotalOccurrences = 1,
                Confidence = 1.0
            });
        }
        catch (Exception ex)
        {
            return Result<SegmentPattern>.Failure($"Segment analysis failed: {ex.Message}");
        }
    }

    private async Task<List<ComponentPattern>> AnalyzeFieldComponents(string fieldValue, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(fieldValue) || !fieldValue.Contains('^'))
            return new List<ComponentPattern>();

        try
        {
            var components = fieldValue.Split('^');
            var patterns = new List<ComponentPattern>();

            var componentFreqs = new Dictionary<string, ComponentFrequency>();
            for (int i = 0; i < components.Length; i++)
            {
                var component = components[i];
                var componentName = $"Component_{i + 1}";
                
                componentFreqs[componentName] = new ComponentFrequency
                {
                    ComponentName = componentName,
                    Frequency = string.IsNullOrEmpty(component) ? 0 : 1,
                    TotalOccurrences = 1,
                    UniqueValues = new List<string> { component },
                    AverageLength = component?.Length ?? 0
                };
            }

            patterns.Add(new ComponentPattern
            {
                FieldType = fieldName,
                ComponentFrequencies = StringKeysToIntKeysComponents(componentFreqs),
                TotalSamples = 1,
                StandardName = StandardName
            });

            return await Task.FromResult(patterns);
        }
        catch (Exception)
        {
            return new List<ComponentPattern>();
        }
    }

    private string GetComponentName(string fieldType, int componentIndex)
    {
        // HL7 component naming based on common data types
        return fieldType?.ToUpperInvariant() switch
        {
            "XPN" => componentIndex switch // Person Name
            {
                1 => "Family Name",
                2 => "Given Name", 
                3 => "Second/Middle Name",
                4 => "Suffix",
                5 => "Prefix",
                6 => "Degree",
                _ => $"Component_{componentIndex}"
            },
            "XAD" => componentIndex switch // Address
            {
                1 => "Street Address",
                2 => "Other Designation",
                3 => "City",
                4 => "State/Province",
                5 => "Zip/Postal Code",
                6 => "Country",
                _ => $"Component_{componentIndex}"
            },
            "CE" => componentIndex switch // Coded Element
            {
                1 => "Identifier",
                2 => "Text",
                3 => "Name of Coding System",
                4 => "Alternate Identifier",
                5 => "Alternate Text",
                6 => "Name of Alternate Coding System",
                _ => $"Component_{componentIndex}"
            },
            _ => $"Component_{componentIndex}"
        };
    }

    private SegmentPattern MergeSegmentPatterns(SegmentPattern existing, SegmentPattern newPattern)
    {
        var mergedFrequencies = new Dictionary<string, FieldFrequency>(IntKeysToStringKeys(existing.FieldFrequencies));

        foreach (var field in IntKeysToStringKeys(newPattern.FieldFrequencies))
        {
            if (mergedFrequencies.ContainsKey(field.Key))
            {
                mergedFrequencies[field.Key] = MergeFieldFrequencies(mergedFrequencies[field.Key], field.Value);
            }
            else
            {
                mergedFrequencies[field.Key] = field.Value;
            }
        }

        return new SegmentPattern
        {
            SegmentType = existing.SegmentType,
            FieldFrequencies = StringKeysToIntKeys(mergedFrequencies),
            TotalOccurrences = existing.TotalOccurrences + newPattern.TotalOccurrences,
            Confidence = CalculateSegmentConfidence(mergedFrequencies, existing.TotalOccurrences + newPattern.TotalOccurrences)
        };
    }

    private FieldFrequency MergeFieldFrequencies(FieldFrequency existing, FieldFrequency newFreq)
    {
        var uniqueValues = existing.UniqueValues.Union(newFreq.UniqueValues).ToList();
        var totalOccurrences = existing.TotalOccurrences + newFreq.TotalOccurrences;
        var frequency = existing.Frequency + newFreq.Frequency;

        return new FieldFrequency
        {
            FieldName = existing.FieldName,
            Frequency = frequency,
            TotalOccurrences = totalOccurrences,
            UniqueValues = uniqueValues,
            CommonValues = uniqueValues.Take(10).ToList(), // Keep top 10 most common
            AverageLength = (existing.AverageLength * existing.TotalOccurrences + newFreq.AverageLength * newFreq.TotalOccurrences) / totalOccurrences,
            ComponentPatterns = existing.ComponentPatterns.Union(newFreq.ComponentPatterns).ToList()
        };
    }

    private double CalculatePatternConfidence(Dictionary<string, SegmentFieldPatterns> patterns, int sampleSize)
    {
        if (patterns.Count == 0 || sampleSize == 0)
            return 0.0;

        // Base confidence on sample size and pattern consistency
        var sampleConfidence = Math.Min(1.0, sampleSize / 50.0); // 50+ samples = full confidence
        var patternConfidence = Math.Min(1.0, patterns.Count / 5.0); // 5+ segments = good coverage

        return (sampleConfidence * 0.6) + (patternConfidence * 0.4);
    }

    private double CalculateSegmentConfidence(Dictionary<string, FieldFrequency> frequencies, int occurrences)
    {
        if (frequencies.Count == 0 || occurrences == 0)
            return 0.0;

        var populatedFields = frequencies.Values.Count(f => f.Frequency > 0);
        var coverageRatio = (double)populatedFields / frequencies.Count;
        var sampleRatio = Math.Min(1.0, occurrences / 10.0); // 10+ occurrences = good confidence

        return (coverageRatio * 0.7) + (sampleRatio * 0.3);
    }

    // HL7 Message Type Coverage Calculations

    private double CalculateADTCoverage(FieldPatterns patterns)
    {
        // ADT messages should have: MSH, EVN, PID, PV1
        var expectedSegments = new[] { "MSH", "EVN", "PID", "PV1" };
        var presentSegments = patterns.SegmentPatterns.Keys.ToHashSet();
        return (double)expectedSegments.Count(seg => presentSegments.Contains(seg)) / expectedSegments.Length;
    }

    private double CalculateRDECoverage(FieldPatterns patterns)
    {
        // RDE messages should have: MSH, PID, RXE, RXR
        var expectedSegments = new[] { "MSH", "PID", "RXE", "RXR" };
        var presentSegments = patterns.SegmentPatterns.Keys.ToHashSet();
        return (double)expectedSegments.Count(seg => presentSegments.Contains(seg)) / expectedSegments.Length;
    }

    private double CalculateORMCoverage(FieldPatterns patterns)
    {
        // ORM messages should have: MSH, PID, ORC, OBR
        var expectedSegments = new[] { "MSH", "PID", "ORC", "OBR" };
        var presentSegments = patterns.SegmentPatterns.Keys.ToHashSet();
        return (double)expectedSegments.Count(seg => presentSegments.Contains(seg)) / expectedSegments.Length;
    }

    private double CalculateORUCoverage(FieldPatterns patterns)
    {
        // ORU messages should have: MSH, PID, OBR, OBX
        var expectedSegments = new[] { "MSH", "PID", "OBR", "OBX" };
        var presentSegments = patterns.SegmentPatterns.Keys.ToHashSet();
        return (double)expectedSegments.Count(seg => presentSegments.Contains(seg)) / expectedSegments.Length;
    }

    private double CalculateGenericHL7Coverage(FieldPatterns patterns)
    {
        // Generic HL7 should at least have MSH
        var hasRequiredSegments = patterns.SegmentPatterns.ContainsKey("MSH");
        var segmentCount = patterns.SegmentPatterns.Count;
        
        // Base score for having MSH, bonus for additional segments
        return hasRequiredSegments ? 0.5 + Math.Min(0.5, (segmentCount - 1) / 10.0) : 0.1;
    }

    #endregion

    #region Dictionary Conversion Utilities

    /// <summary>
    /// Converts Dictionary with int keys (HL7 field positions) to string keys for API compatibility.
    /// Preserves healthcare domain semantics while enabling plugin boundary conversion.
    /// </summary>
    /// <param name="source">Source dictionary with int keys representing HL7 field positions</param>
    /// <returns>Dictionary with string keys for API compatibility</returns>
    private static Dictionary<string, FieldFrequency> IntKeysToStringKeys(Dictionary<int, FieldFrequency>? source)
        => source?.ToDictionary(kvp => kvp.Key.ToString(), kvp => kvp.Value) ?? new Dictionary<string, FieldFrequency>();

    /// <summary>
    /// Converts Dictionary with string keys back to int keys (HL7 field positions) for domain compatibility.
    /// Maintains healthcare domain semantics by preserving field position numbers.
    /// </summary>
    /// <param name="source">Source dictionary with string keys from API boundary</param>
    /// <returns>Dictionary with int keys representing HL7 field positions</returns>
    private static Dictionary<int, FieldFrequency> StringKeysToIntKeys(Dictionary<string, FieldFrequency>? source)
        => source?.ToDictionary(kvp => int.Parse(kvp.Key), kvp => kvp.Value) ?? new Dictionary<int, FieldFrequency>();

    /// <summary>
    /// Converts Dictionary with string keys to int keys for ComponentFrequency collections.
    /// Maintains healthcare domain semantics for component positions.
    /// </summary>
    /// <param name="source">Source dictionary with string keys</param>
    /// <returns>Dictionary with int keys representing component positions</returns>
    private static Dictionary<int, ComponentFrequency> StringKeysToIntKeysComponents(Dictionary<string, ComponentFrequency>? source)
        => source?.ToDictionary(kvp => int.Parse(kvp.Key), kvp => kvp.Value) ?? new Dictionary<int, ComponentFrequency>();

    #endregion
}