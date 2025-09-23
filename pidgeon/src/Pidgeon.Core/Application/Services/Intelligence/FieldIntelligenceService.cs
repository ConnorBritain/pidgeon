// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Microsoft.Extensions.Logging;
using Pidgeon.Core.Application.Interfaces.Generation;
using Pidgeon.Core.Application.Interfaces.Reference;
using Pidgeon.Core.Common.Types;
using System.Text.Json;

namespace Pidgeon.Core.Application.Services.Intelligence;

/// <summary>
/// Provides healthcare-intelligent analysis of field differences using constraint resolution data.
/// Combines field semantics, clinical impact analysis, and data validation for diff insights.
/// </summary>
public class FieldIntelligenceService
{
    private readonly IConstraintResolver _constraintResolver;
    private readonly IDemographicsDataService _demographicsService;
    private readonly ILogger<FieldIntelligenceService> _logger;
    private readonly string _dataBasePath;

    public IDemographicsDataService DemographicsService => _demographicsService;

    public FieldIntelligenceService(
        IConstraintResolver constraintResolver,
        IDemographicsDataService demographicsService,
        ILogger<FieldIntelligenceService> logger)
    {
        _constraintResolver = constraintResolver;
        _demographicsService = demographicsService;
        _logger = logger;
        _dataBasePath = Path.Combine(AppContext.BaseDirectory, "data", "standards", "hl7v23");
    }

    /// <summary>
    /// Analyzes a field difference and provides healthcare-intelligent context.
    /// </summary>
    public async Task<FieldIntelligenceResult> AnalyzeFieldDifferenceAsync(string fieldPath, string leftValue, string rightValue)
    {
        try
        {
            var fieldInfo = await GetFieldInformationAsync(fieldPath);
            var validation = await ValidateFieldValuesAsync(fieldPath, leftValue, rightValue, fieldInfo);
            var impact = AnalyzeClinicalImpact(fieldPath, leftValue, rightValue, fieldInfo);

            return new FieldIntelligenceResult
            {
                FieldPath = fieldPath,
                FieldName = fieldInfo?.Description ?? "Unknown Field",
                DataType = fieldInfo?.DataType ?? "Unknown",
                LeftValue = leftValue,
                RightValue = rightValue,
                ValidationResult = validation,
                ClinicalImpact = impact,
                Confidence = CalculateConfidence(fieldPath, validation, impact)
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to analyze field difference for {FieldPath}", fieldPath);
            return FieldIntelligenceResult.CreateFallback(fieldPath, leftValue, rightValue);
        }
    }

    /// <summary>
    /// Gets field information from our HL7 standards data.
    /// </summary>
    private async Task<FieldInformation?> GetFieldInformationAsync(string fieldPath)
    {
        if (!fieldPath.Contains('.'))
            return null;

        var parts = fieldPath.Split('.');
        if (parts.Length < 2)
            return null;

        var segmentCode = parts[0].ToUpper();
        var fieldPosition = parts[1];

        try
        {
            var segmentFile = Path.Combine(_dataBasePath, "segments", $"{segmentCode.ToLower()}.json");
            if (!File.Exists(segmentFile))
                return null;

            var segmentJson = await File.ReadAllTextAsync(segmentFile);
            var segmentData = JsonSerializer.Deserialize<SegmentDefinition>(segmentJson);

            var field = segmentData?.Fields?.FirstOrDefault(f => f.Position.ToString() == fieldPosition);
            if (field == null)
                return null;

            return new FieldInformation
            {
                Path = fieldPath,
                Description = field.FieldDescription,
                DataType = field.DataType,
                Length = field.Length,
                Optionality = field.Optionality,
                Table = field.Table
            };
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not load field information for {FieldPath}", fieldPath);
            return null;
        }
    }

    /// <summary>
    /// Validates field values against constraint datasets.
    /// </summary>
    private async Task<ValidationResult> ValidateFieldValuesAsync(string fieldPath, string leftValue, string rightValue, FieldInformation? fieldInfo)
    {
        var results = new List<string>();

        // Demographic validation for name fields
        if (IsNameField(fieldPath, fieldInfo))
        {
            var nameValidation = await ValidateNameFieldAsync(leftValue, rightValue);
            results.AddRange(nameValidation);
        }

        // Date/timestamp validation
        if (IsDateTimeField(fieldPath, fieldInfo))
        {
            var dateValidation = ValidateDateTimeField(leftValue, rightValue, fieldInfo);
            results.AddRange(dateValidation);
        }

        return new ValidationResult
        {
            IsValid = results.Count == 0 || results.All(r => !r.Contains("Invalid")),
            Messages = results
        };
    }

    /// <summary>
    /// Analyzes the clinical and operational impact of field differences.
    /// </summary>
    private ClinicalImpact AnalyzeClinicalImpact(string fieldPath, string leftValue, string rightValue, FieldInformation? fieldInfo)
    {
        return fieldPath.ToUpper() switch
        {
            "PID.5" => new ClinicalImpact
            {
                Level = ImpactLevel.Administrative,
                Description = "Patient name change - administrative update, no clinical significance",
                Recommendation = "Verify name change is intentional and update patient records accordingly"
            },
            "PV1.44" => AnalyzeAdmitDateImpact(leftValue, rightValue),
            "MSH.7" => AnalyzeTimestampImpact(leftValue, rightValue),
            "PID.3" => new ClinicalImpact
            {
                Level = ImpactLevel.High,
                Description = "Patient ID change - critical for patient safety and record integrity",
                Recommendation = "Verify patient identity immediately - potential patient matching issue"
            },
            "OBX.5" => new ClinicalImpact
            {
                Level = ImpactLevel.Clinical,
                Description = "Observation value change - may impact clinical decision making",
                Recommendation = "Review observation values for clinical significance"
            },
            _ => new ClinicalImpact
            {
                Level = ImpactLevel.Unknown,
                Description = $"Field {fieldPath} change detected",
                Recommendation = "Review change context and clinical significance"
            }
        };
    }

    private async Task<List<string>> ValidateNameFieldAsync(string leftValue, string rightValue)
    {
        var results = new List<string>();

        try
        {
            var firstNames = await _demographicsService.GetFirstNamesAsync();
            var lastNames = await _demographicsService.GetLastNamesAsync();

            var leftNames = ExtractNames(leftValue);
            var rightNames = ExtractNames(rightValue);

            var leftValid = ValidateNames(leftNames, firstNames, lastNames);
            var rightValid = ValidateNames(rightNames, firstNames, lastNames);

            if (leftValid && rightValid)
            {
                results.Add("Both names validated against demographic datasets");
            }
            else if (!leftValid && !rightValid)
            {
                results.Add("Neither name found in demographic datasets - possible data quality issue");
            }
            else
            {
                results.Add("One name validated, one not found - mixed data quality");
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to validate name fields");
            results.Add("Name validation unavailable");
        }

        return results;
    }

    private static List<string> ValidateDateTimeField(string leftValue, string rightValue, FieldInformation? fieldInfo)
    {
        var results = new List<string>();

        // Basic TS format validation (YYYYMMDDHHMMSS)
        var leftValid = IsValidHL7Timestamp(leftValue);
        var rightValid = IsValidHL7Timestamp(rightValue);

        if (leftValid && rightValid)
        {
            results.Add("Both timestamps pass HL7 TS format validation");
        }
        else
        {
            results.Add("Invalid timestamp format detected");
        }

        return results;
    }

    private static ClinicalImpact AnalyzeAdmitDateImpact(string leftValue, string rightValue)
    {
        if (DateTime.TryParseExact(leftValue, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out var leftDate) &&
            DateTime.TryParseExact(rightValue, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out var rightDate))
        {
            var daysDiff = Math.Abs((rightDate - leftDate).Days);

            return daysDiff switch
            {
                0 => new ClinicalImpact
                {
                    Level = ImpactLevel.None,
                    Description = "Same admit date - no impact",
                    Recommendation = "No action needed"
                },
                <= 1 => new ClinicalImpact
                {
                    Level = ImpactLevel.Low,
                    Description = "1-day admit date difference - minor scheduling variance",
                    Recommendation = "Verify correct admission date for accurate episode tracking"
                },
                <= 7 => new ClinicalImpact
                {
                    Level = ImpactLevel.Moderate,
                    Description = $"{daysDiff}-day admit date difference - significant for length of stay calculations",
                    Recommendation = "Review admission date accuracy - impacts LOS metrics and billing"
                },
                _ => new ClinicalImpact
                {
                    Level = ImpactLevel.High,
                    Description = $"{daysDiff}-day admit date difference - major discrepancy affecting episode continuity",
                    Recommendation = "Investigate admit date discrepancy immediately - may indicate different episodes of care"
                }
            };
        }

        return new ClinicalImpact
        {
            Level = ImpactLevel.Unknown,
            Description = "Admit date format issue - unable to calculate impact",
            Recommendation = "Verify date format and correct admission date"
        };
    }

    private static ClinicalImpact AnalyzeTimestampImpact(string leftValue, string rightValue)
    {
        if (DateTime.TryParseExact(leftValue, "yyyyMMddHHmmss", null, System.Globalization.DateTimeStyles.None, out var leftTime) &&
            DateTime.TryParseExact(rightValue, "yyyyMMddHHmmss", null, System.Globalization.DateTimeStyles.None, out var rightTime))
        {
            var timeDiff = Math.Abs((rightTime - leftTime).TotalSeconds);

            return timeDiff switch
            {
                <= 60 => new ClinicalImpact
                {
                    Level = ImpactLevel.None,
                    Description = $"{timeDiff:F0}-second timestamp difference - normal message processing variance",
                    Recommendation = "No action needed - expected timestamp variation"
                },
                <= 3600 => new ClinicalImpact
                {
                    Level = ImpactLevel.Low,
                    Description = $"{timeDiff/60:F0}-minute timestamp difference - processing delay possible",
                    Recommendation = "Monitor for message processing delays"
                },
                _ => new ClinicalImpact
                {
                    Level = ImpactLevel.Moderate,
                    Description = $"{timeDiff/3600:F1}-hour timestamp difference - significant processing delay",
                    Recommendation = "Investigate message processing pipeline for delays"
                }
            };
        }

        return new ClinicalImpact
        {
            Level = ImpactLevel.Unknown,
            Description = "Timestamp format issue",
            Recommendation = "Verify timestamp format"
        };
    }

    private static bool IsNameField(string fieldPath, FieldInformation? fieldInfo)
    {
        return fieldPath.ToUpper() == "PID.5" ||
               fieldInfo?.DataType == "XPN" ||
               fieldInfo?.Description?.ToLower().Contains("name") == true;
    }

    private static bool IsDateTimeField(string fieldPath, FieldInformation? fieldInfo)
    {
        return fieldInfo?.DataType == "TS" ||
               fieldInfo?.DataType == "DT" ||
               fieldInfo?.Description?.ToLower().Contains("date") == true ||
               fieldInfo?.Description?.ToLower().Contains("time") == true;
    }

    private static List<string> ExtractNames(string nameValue)
    {
        // Simple HL7 XPN parsing: LastName^FirstName^MiddleName
        return nameValue.Split('^').Where(n => !string.IsNullOrWhiteSpace(n)).ToList();
    }

    private static bool ValidateNames(List<string> names, List<string> firstNames, List<string> lastNames)
    {
        if (names.Count == 0) return false;

        // Check if any name components match our datasets
        return names.Any(name =>
            firstNames.Contains(name, StringComparer.OrdinalIgnoreCase) ||
            lastNames.Contains(name, StringComparer.OrdinalIgnoreCase));
    }

    private static bool IsValidHL7Timestamp(string timestamp)
    {
        if (string.IsNullOrWhiteSpace(timestamp)) return false;

        // HL7 TS format: YYYY[MM[DD[HHMM[SS[.SSSS]]]]][+/-ZZZZ]
        return DateTime.TryParseExact(timestamp, "yyyyMMddHHmmss", null, System.Globalization.DateTimeStyles.None, out _) ||
               DateTime.TryParseExact(timestamp, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out _);
    }

    private static double CalculateConfidence(string fieldPath, ValidationResult validation, ClinicalImpact impact)
    {
        var baseConfidence = 0.7;

        // Higher confidence for well-known fields
        if (new[] { "PID.5", "PV1.44", "MSH.7", "PID.3" }.Contains(fieldPath.ToUpper()))
            baseConfidence += 0.2;

        // Higher confidence for successful validation
        if (validation.IsValid)
            baseConfidence += 0.1;

        return Math.Min(0.95, baseConfidence);
    }
}

// Supporting data structures
public record FieldInformation
{
    public string Path { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string DataType { get; init; } = string.Empty;
    public string Length { get; init; } = string.Empty;
    public string Optionality { get; init; } = string.Empty;
    public string Table { get; init; } = string.Empty;
}

public record ValidationResult
{
    public bool IsValid { get; init; }
    public List<string> Messages { get; init; } = new();
}

public record ClinicalImpact
{
    public ImpactLevel Level { get; init; }
    public string Description { get; init; } = string.Empty;
    public string Recommendation { get; init; } = string.Empty;
}

public enum ImpactLevel
{
    None,
    Low,
    Moderate,
    Administrative,
    Clinical,
    High,
    Unknown
}

public record FieldIntelligenceResult
{
    public string FieldPath { get; init; } = string.Empty;
    public string FieldName { get; init; } = string.Empty;
    public string DataType { get; init; } = string.Empty;
    public string LeftValue { get; init; } = string.Empty;
    public string RightValue { get; init; } = string.Empty;
    public ValidationResult ValidationResult { get; init; } = new();
    public ClinicalImpact ClinicalImpact { get; init; } = new();
    public double Confidence { get; init; }

    public static FieldIntelligenceResult CreateFallback(string fieldPath, string leftValue, string rightValue)
    {
        return new FieldIntelligenceResult
        {
            FieldPath = fieldPath,
            FieldName = "Unknown Field",
            LeftValue = leftValue,
            RightValue = rightValue,
            ValidationResult = new ValidationResult { IsValid = false, Messages = new List<string> { "Analysis unavailable" } },
            ClinicalImpact = new ClinicalImpact
            {
                Level = ImpactLevel.Unknown,
                Description = "Field difference detected",
                Recommendation = "Manual review recommended"
            },
            Confidence = 0.3
        };
    }
}

// JSON deserialization structures
public record SegmentDefinition
{
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public List<FieldDefinition>? Fields { get; init; }
}

public record FieldDefinition
{
    public int Position { get; init; }
    public string FieldName { get; init; } = string.Empty;
    public string FieldDescription { get; init; } = string.Empty;
    public string Length { get; init; } = string.Empty;
    public string DataType { get; init; } = string.Empty;
    public string Optionality { get; init; } = string.Empty;
    public string Repeatability { get; init; } = string.Empty;
    public string Table { get; init; } = string.Empty;
}