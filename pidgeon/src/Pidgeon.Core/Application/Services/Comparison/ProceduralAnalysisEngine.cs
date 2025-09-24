// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Microsoft.Extensions.Logging;
using Pidgeon.Core.Application.Interfaces.Comparison;
using Pidgeon.Core.Application.Interfaces.Generation;
using Pidgeon.Core.Application.Interfaces.Reference;
using Pidgeon.Core.Common.Types;
using Pidgeon.Core.Domain.Comparison.Entities;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Pidgeon.Core.Application.Services.Comparison;

/// <summary>
/// Advanced procedural analysis engine for healthcare message differences.
/// Implements smart field-level analysis using existing data sources and validation services.
/// </summary>
public class ProceduralAnalysisEngine : IProceduralAnalysisEngine
{
    private readonly IStandardReferenceService _referenceService;
    private readonly IConstraintResolver _constraintResolver;
    private readonly IDemographicsDataService _demographicsService;
    private readonly ILogger<ProceduralAnalysisEngine> _logger;

    // Field categorization based on HL7 segments
    private static readonly Dictionary<string, FieldCategory> SegmentCategories = new()
    {
        ["MSH"] = FieldCategory.System,           // Message Header
        ["EVN"] = FieldCategory.System,           // Event Type
        ["PID"] = FieldCategory.Demographics,     // Patient Identification
        ["NK1"] = FieldCategory.Demographics,     // Next of Kin
        ["AL1"] = FieldCategory.Critical,         // Patient Allergy Information
        ["DG1"] = FieldCategory.Clinical,         // Diagnosis
        ["PR1"] = FieldCategory.Clinical,         // Procedures
        ["RXE"] = FieldCategory.Clinical,         // Pharmacy/Treatment Encoded Order
        ["RXA"] = FieldCategory.Clinical,         // Pharmacy/Treatment Administration
        ["RXO"] = FieldCategory.Clinical,         // Pharmacy/Treatment Order
        ["OBX"] = FieldCategory.Clinical,         // Observation/Result
        ["OBR"] = FieldCategory.Clinical,         // Observation Request
        ["PV1"] = FieldCategory.Administrative,   // Patient Visit
        ["PV2"] = FieldCategory.Administrative,   // Patient Visit - Additional Info
        ["IN1"] = FieldCategory.Administrative,   // Insurance
        ["IN2"] = FieldCategory.Administrative,   // Insurance Additional Info
        ["GT1"] = FieldCategory.Administrative,   // Guarantor
    };

    // Fields that commonly contain timestamps
    private static readonly string[] TimestampFields =
    {
        "MSH.7",   // Date/Time of Message
        "PID.7",   // Date/Time of Birth
        "PV1.44",  // Admit Date/Time
        "PV1.45",  // Discharge Date/Time
        "OBR.7",   // Observation Date/Time
        "EVN.2",   // Recorded Date/Time
        "EVN.6",   // Event Occurred Date/Time
        "AL1.6"    // Identification Date
    };

    public ProceduralAnalysisEngine(
        IStandardReferenceService referenceService,
        IConstraintResolver constraintResolver,
        IDemographicsDataService demographicsService,
        ILogger<ProceduralAnalysisEngine> logger)
    {
        _referenceService = referenceService;
        _constraintResolver = constraintResolver;
        _demographicsService = demographicsService;
        _logger = logger;
    }

    public async Task<Result<ProceduralAnalysisResult>> AnalyzeAsync(
        List<FieldDifference> fieldDifferences,
        DiffContext context,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Starting procedural analysis of {Count} field differences", fieldDifferences.Count);

            // Perform comprehensive analysis
            var constraintValidation = await ValidateConstraintsAsync(fieldDifferences, context, cancellationToken);
            var demographicAnalysis = await AnalyzeDemographicsAsync(fieldDifferences, context, cancellationToken);
            var clinicalImpact = await AssessClinicalImpactAsync(fieldDifferences, context, cancellationToken);

            // Calculate overall procedural score
            var proceduralScore = CalculateProceduralScore(
                constraintValidation.IsSuccess ? constraintValidation.Value : null,
                demographicAnalysis.IsSuccess ? demographicAnalysis.Value : null,
                clinicalImpact.IsSuccess ? clinicalImpact.Value : null);

            // Create analysis result
            var analysisResult = new ProceduralAnalysisResult
            {
                ProceduralScore = proceduralScore,
                ConstraintValidation = constraintValidation.IsSuccess ? constraintValidation.Value : new(),
                DemographicAnalysis = demographicAnalysis.IsSuccess ? demographicAnalysis.Value : new(),
                ClinicalImpact = clinicalImpact.IsSuccess ? clinicalImpact.Value : new(),
                Metadata = new ProceduralAnalysisMetadata
                {
                    ExecutionTime = stopwatch.Elapsed,
                    FieldsAnalyzed = fieldDifferences.Count,
                    ConstraintsEvaluated = constraintValidation.IsSuccess ? constraintValidation.Value.ViolationCount : 0,
                    DemographicValidations = demographicAnalysis.IsSuccess ? demographicAnalysis.Value.Issues.Count : 0,
                    EngineVersion = "1.0",
                    UsedCache = false
                }
            };

            // Generate recommendations
            var recommendations = await GenerateRecommendationsAsync(analysisResult, context);
            if (recommendations.IsSuccess)
            {
                analysisResult = analysisResult with { Recommendations = recommendations.Value };
            }

            _logger.LogInformation("Procedural analysis completed in {ElapsedMs}ms with score {Score:F2}",
                stopwatch.ElapsedMilliseconds, proceduralScore);

            return Result<ProceduralAnalysisResult>.Success(analysisResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during procedural analysis");
            return Result<ProceduralAnalysisResult>.Failure($"Procedural analysis failed: {ex.Message}");
        }
    }

    public async Task<Result<ConstraintValidationResult>> ValidateConstraintsAsync(
        List<FieldDifference> fieldDifferences,
        DiffContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var violations = new List<ConstraintViolation>();
            var requiredFieldIssues = new List<RequiredFieldIssue>();
            var dataTypeValidations = new List<DataTypeValidationResult>();

            foreach (var difference in fieldDifferences)
            {
                // Look up field metadata using existing reference service
                var fieldLookup = await _referenceService.LookupAsync(difference.FieldPath, cancellationToken);
                if (fieldLookup.IsFailure)
                    continue;

                var fieldMetadata = fieldLookup.Value;

                // Validate data types
                var dataTypeValidation = await ValidateDataType(difference, fieldMetadata);
                dataTypeValidations.Add(dataTypeValidation);

                // Check for constraint violations using existing constraint resolver
                var constraintsResult = await _constraintResolver.GetConstraintsAsync(difference.FieldPath);
                if (constraintsResult.IsSuccess && !string.IsNullOrEmpty(difference.RightValue))
                {
                    var validationResult = await _constraintResolver.ValidateValueAsync(
                        difference.FieldPath, difference.RightValue, constraintsResult.Value);

                    if (validationResult.IsFailure)
                    {
                        violations.Add(new ConstraintViolation
                        {
                            FieldPath = difference.FieldPath,
                            ViolationType = ConstraintViolationType.ReferenceConstraintViolation,
                            ConstraintDescription = "Field value failed constraint validation",
                            ActualValue = difference.RightValue,
                            Severity = ConstraintViolationSeverity.Warning,
                            SuggestedResolution = validationResult.Error.Message ?? "Verify field value format"
                        });
                    }
                }

                // Check required fields
                if (fieldMetadata.Usage?.ToLowerInvariant() == "required" || fieldMetadata.Usage?.ToLowerInvariant() == "r")
                {
                    if (string.IsNullOrEmpty(difference.RightValue))
                    {
                        requiredFieldIssues.Add(new RequiredFieldIssue
                        {
                            FieldPath = difference.FieldPath,
                            FieldName = fieldMetadata.Name,
                            IssueType = RequiredFieldIssueType.Missing,
                            RequirementReason = $"Field {fieldMetadata.Name} is required by {context.Standard} standard",
                            SuggestedAction = "Provide a valid value for this required field"
                        });
                    }
                }
            }

            // Calculate compliance score
            var totalFields = fieldDifferences.Count;
            var violationCount = violations.Count + requiredFieldIssues.Count + dataTypeValidations.Count(d => !d.IsValid);
            var complianceScore = totalFields > 0 ? Math.Max(0.0, 1.0 - (double)violationCount / totalFields) : 1.0;

            var result = new ConstraintValidationResult
            {
                ViolationCount = violations.Count,
                Violations = violations,
                RequiredFieldIssues = requiredFieldIssues,
                DataTypeValidation = dataTypeValidations,
                ComplianceScore = complianceScore
            };

            return Result<ConstraintValidationResult>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating constraints");
            return Result<ConstraintValidationResult>.Failure($"Constraint validation error: {ex.Message}");
        }
    }

    public async Task<Result<DemographicAnalysisResult>> AnalyzeDemographicsAsync(
        List<FieldDifference> fieldDifferences,
        DiffContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var issues = new List<DemographicIssue>();
            var nameValidation = new NameValidationResult { IsValid = true };
            var dateValidation = new DateValidationResult { IsValid = true };
            var addressValidation = new AddressValidationResult { IsValid = true };

            // Filter demographic fields
            var demographicFields = fieldDifferences
                .Where(d => CategorizeField(d.FieldPath) == FieldCategory.Demographics)
                .ToList();

            foreach (var field in demographicFields)
            {
                await AnalyzeDemographicField(field, issues, nameValidation, dateValidation, addressValidation);
            }

            // Calculate overall quality score
            var totalDemographicFields = demographicFields.Count;
            var issueCount = issues.Count;
            var qualityScore = totalDemographicFields > 0 ?
                Math.Max(0.0, 1.0 - (double)issueCount / totalDemographicFields) : 1.0;

            var result = new DemographicAnalysisResult
            {
                Issues = issues,
                NameValidation = nameValidation,
                DateValidation = dateValidation,
                AddressValidation = addressValidation,
                QualityScore = qualityScore
            };

            return Result<DemographicAnalysisResult>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing demographics");
            return Result<DemographicAnalysisResult>.Failure($"Demographic analysis error: {ex.Message}");
        }
    }

    public async Task<Result<ClinicalImpactAssessment>> AssessClinicalImpactAsync(
        List<FieldDifference> fieldDifferences,
        DiffContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await Task.Yield();

            var impactAreas = new List<ClinicalImpactArea>();
            var recommendations = new List<string>();

            // Analyze different categories of clinical impact
            double patientSafetyScore = CalculatePatientSafetyImpact(fieldDifferences);
            double careQualityScore = CalculateCareQualityImpact(fieldDifferences);
            double operationalScore = CalculateOperationalImpact(fieldDifferences);

            // Determine overall impact level
            var maxScore = Math.Max(patientSafetyScore, Math.Max(careQualityScore, operationalScore));
            var impactLevel = maxScore switch
            {
                >= 0.8 => ClinicalImpactLevel.Critical,
                >= 0.6 => ClinicalImpactLevel.High,
                >= 0.4 => ClinicalImpactLevel.Moderate,
                >= 0.2 => ClinicalImpactLevel.Low,
                _ => ClinicalImpactLevel.None
            };

            // Generate impact areas and recommendations
            GenerateImpactAreasAndRecommendations(fieldDifferences, impactAreas, recommendations);

            var assessment = new ClinicalImpactAssessment
            {
                ImpactLevel = impactLevel,
                PatientSafetyScore = patientSafetyScore,
                CareQualityScore = careQualityScore,
                OperationalScore = operationalScore,
                ImpactAreas = impactAreas,
                ClinicalRecommendations = recommendations
            };

            return Result<ClinicalImpactAssessment>.Success(assessment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assessing clinical impact");
            return Result<ClinicalImpactAssessment>.Failure($"Clinical impact assessment error: {ex.Message}");
        }
    }

    public async Task<Result<List<ProceduralRecommendation>>> GenerateRecommendationsAsync(
        ProceduralAnalysisResult analysisResult,
        DiffContext context)
    {
        await Task.Yield();

        var recommendations = new List<ProceduralRecommendation>();

        // Generate constraint-based recommendations
        if (analysisResult.ConstraintValidation.ViolationCount > 0)
        {
            recommendations.Add(new ProceduralRecommendation
            {
                Type = RecommendationType.ConstraintCompliance,
                Priority = RecommendationPriority.High,
                Title = "Resolve Constraint Violations",
                Description = $"Found {analysisResult.ConstraintValidation.ViolationCount} constraint violations that may cause message processing issues.",
                Actions = new List<string>
                {
                    "Review field values against standard constraints",
                    "Validate data types and formats",
                    "Update vendor configuration if needed"
                },
                AffectedFields = analysisResult.ConstraintValidation.Violations.Select(v => v.FieldPath).ToList(),
                ExpectedImpact = "Improved message compatibility and reduced processing errors"
            });
        }

        // Generate demographic quality recommendations
        if (analysisResult.DemographicAnalysis.QualityScore < 0.8)
        {
            recommendations.Add(new ProceduralRecommendation
            {
                Type = RecommendationType.DataQualityImprovement,
                Priority = RecommendationPriority.Medium,
                Title = "Improve Demographic Data Quality",
                Description = "Demographic fields have quality issues that may affect patient identification.",
                Actions = new List<string>
                {
                    "Validate patient names against demographics database",
                    "Verify date formats and logical consistency",
                    "Standardize address formatting"
                },
                AffectedFields = new List<string> { "PID.5", "PID.7", "PID.11" },
                ExpectedImpact = "Better patient matching and reduced demographic errors"
            });
        }

        // Generate clinical safety recommendations
        if (analysisResult.ClinicalImpact.PatientSafetyScore > 0.5)
        {
            recommendations.Add(new ProceduralRecommendation
            {
                Type = RecommendationType.ClinicalSafety,
                Priority = RecommendationPriority.Critical,
                Title = "Review Clinical Safety Impact",
                Description = "Changes may affect patient safety through medication or allergy information.",
                Actions = new List<string>
                {
                    "Verify allergy and medication information accuracy",
                    "Review clinical decision support impact",
                    "Test safety alert functionality with new data"
                },
                AffectedFields = new List<string> { "AL1.*", "RXE.*", "DG1.*" },
                ExpectedImpact = "Maintained patient safety and clinical decision support effectiveness"
            });
        }

        return Result<List<ProceduralRecommendation>>.Success(recommendations);
    }

    private FieldCategory CategorizeField(string fieldPath)
    {
        var segment = ExtractSegment(fieldPath);
        return SegmentCategories.GetValueOrDefault(segment, FieldCategory.Administrative);
    }

    private string ExtractSegment(string fieldPath)
    {
        var parts = fieldPath.Split('.');
        return parts.Length > 0 ? parts[0] : "";
    }

    private async Task<DataTypeValidationResult> ValidateDataType(FieldDifference difference,
        Core.Domain.Reference.Entities.StandardElement fieldMetadata)
    {
        await Task.Yield();

        var expectedType = fieldMetadata.DataType ?? "Unknown";
        var isValid = ValidateFieldDataType(difference.RightValue, expectedType);

        return new DataTypeValidationResult
        {
            FieldPath = difference.FieldPath,
            ExpectedType = expectedType,
            IsValid = isValid,
            ErrorMessage = isValid ? null : $"Value does not match expected type {expectedType}",
            SuggestedFormat = GetSuggestedFormat(expectedType)
        };
    }

    private bool ValidateFieldDataType(string? value, string dataType)
    {
        if (string.IsNullOrEmpty(value))
            return true; // Empty values are generally acceptable

        // Basic data type validation patterns
        return dataType switch
        {
            "NM" => decimal.TryParse(value, out _),
            "DT" => IsValidDate(value),
            "TM" => IsValidTime(value),
            "TS" => IsValidTimestamp(value),
            "ID" or "IS" => !string.IsNullOrWhiteSpace(value),
            "ST" or "TX" or "FT" => value.Length <= 65536, // Basic length check
            _ => true // For complex types, accept for now
        };
    }

    private bool IsValidDate(string value)
    {
        // HL7 date format: YYYYMMDD or YYYY[MM[DD]]
        var datePattern = @"^\d{4}(\d{2}(\d{2})?)?$";
        return Regex.IsMatch(value, datePattern) &&
               (value.Length == 4 || value.Length == 6 || value.Length == 8);
    }

    private bool IsValidTime(string value)
    {
        // HL7 time format: HH[MM[SS[.SSSS]]]
        var timePattern = @"^\d{2}(\d{2}(\d{2}(\.\d{1,4})?)?)?$";
        return Regex.IsMatch(value, timePattern);
    }

    private bool IsValidTimestamp(string value)
    {
        // HL7 timestamp format: YYYY[MM[DD[HH[MM[SS[.SSSS]]]]]]
        var timestampPattern = @"^\d{4}(\d{2}(\d{2}(\d{2}(\d{2}(\d{2}(\.\d{1,4})?)?)?)?)?)?$";
        return Regex.IsMatch(value, timestampPattern);
    }

    private string? GetSuggestedFormat(string dataType)
    {
        return dataType switch
        {
            "DT" => "YYYYMMDD (e.g., 20250115)",
            "TM" => "HHMM or HHMMSS (e.g., 1430 or 143045)",
            "TS" => "YYYYMMDDHHMMSS (e.g., 20250115143045)",
            "NM" => "Numeric value (e.g., 123.45)",
            _ => null
        };
    }

    private async Task AnalyzeDemographicField(
        FieldDifference field,
        List<DemographicIssue> issues,
        NameValidationResult nameValidation,
        DateValidationResult dateValidation,
        AddressValidationResult addressValidation)
    {
        var fieldPath = field.FieldPath.ToLowerInvariant();

        // Analyze patient name fields (PID.5)
        if (fieldPath.Contains("pid.5"))
        {
            await AnalyzeNameField(field, issues, nameValidation);
        }
        // Analyze date fields (PID.7, etc.)
        else if (fieldPath.Contains("pid.7") || fieldPath.Contains("date") || fieldPath.Contains("dob"))
        {
            await AnalyzeDateField(field, issues, dateValidation);
        }
        // Analyze address fields (PID.11)
        else if (fieldPath.Contains("pid.11") || fieldPath.Contains("address"))
        {
            await AnalyzeAddressField(field, issues, addressValidation);
        }
    }

    private async Task AnalyzeNameField(
        FieldDifference field,
        List<DemographicIssue> issues,
        NameValidationResult nameValidation)
    {
        await Task.Yield();

        var names = await _demographicsService.GetFirstNamesAsync();
        var lastNames = await _demographicsService.GetLastNamesAsync();

        // Parse HL7 name format: LAST^FIRST^MIDDLE
        var newName = field.RightValue ?? "";
        var nameParts = newName.Split('^');

        if (nameParts.Length >= 2)
        {
            var lastName = nameParts[0];
            var firstName = nameParts[1];

            // Check against demographics database
            var firstNameValid = names.Any(n => n.Equals(firstName, StringComparison.OrdinalIgnoreCase));
            var lastNameValid = lastNames.Any(n => n.Equals(lastName, StringComparison.OrdinalIgnoreCase));

            if (!firstNameValid && !string.IsNullOrEmpty(firstName))
            {
                issues.Add(new DemographicIssue
                {
                    FieldPath = field.FieldPath,
                    IssueType = DemographicIssueType.InvalidName,
                    Description = $"First name '{firstName}' not found in demographics database",
                    Severity = DemographicIssueSeverity.Warning
                });
                nameValidation.Issues.Add($"First name '{firstName}' may be uncommon or misspelled");
            }

            if (!lastNameValid && !string.IsNullOrEmpty(lastName))
            {
                issues.Add(new DemographicIssue
                {
                    FieldPath = field.FieldPath,
                    IssueType = DemographicIssueType.InvalidName,
                    Description = $"Last name '{lastName}' not found in demographics database",
                    Severity = DemographicIssueSeverity.Warning
                });
                nameValidation.Issues.Add($"Last name '{lastName}' may be uncommon or misspelled");
            }
        }
    }

    private async Task AnalyzeDateField(
        FieldDifference field,
        List<DemographicIssue> issues,
        DateValidationResult dateValidation)
    {
        await Task.Yield();

        var dateValue = field.RightValue ?? "";

        // Check HL7 date format
        if (!IsValidDate(dateValue))
        {
            issues.Add(new DemographicIssue
            {
                FieldPath = field.FieldPath,
                IssueType = DemographicIssueType.InvalidDate,
                Description = $"Date format '{dateValue}' is not valid HL7 format",
                Severity = DemographicIssueSeverity.Error
            });
            dateValidation.DateFormatIssues.Add($"Invalid date format: {dateValue}");
            dateValidation = dateValidation with { IsValid = false };
        }

        // Check logical date ranges
        if (dateValue.Length >= 4 && int.TryParse(dateValue[..4], out var year))
        {
            var currentYear = DateTime.Now.Year;
            if (year < 1900 || year > currentYear + 1)
            {
                issues.Add(new DemographicIssue
                {
                    FieldPath = field.FieldPath,
                    IssueType = DemographicIssueType.InvalidDate,
                    Description = $"Date year '{year}' is outside reasonable range",
                    Severity = DemographicIssueSeverity.Warning
                });
                dateValidation.LogicalIssues.Add($"Year {year} is outside reasonable range (1900-{currentYear + 1})");
            }
        }
    }

    private async Task AnalyzeAddressField(
        FieldDifference field,
        List<DemographicIssue> issues,
        AddressValidationResult addressValidation)
    {
        await Task.Yield();

        var address = field.RightValue ?? "";

        // Basic address validation
        if (string.IsNullOrWhiteSpace(address))
        {
            issues.Add(new DemographicIssue
            {
                FieldPath = field.FieldPath,
                IssueType = DemographicIssueType.MissingRequiredDemo,
                Description = "Address field is empty",
                Severity = DemographicIssueSeverity.Warning
            });
            addressValidation.Issues.Add("Address is empty or missing");
            addressValidation = addressValidation with { IsValid = false };
        }

        // TODO: Add more sophisticated address validation using existing services
    }

    private double CalculatePatientSafetyImpact(List<FieldDifference> fieldDifferences)
    {
        var safetyFields = fieldDifferences
            .Where(d => CategorizeField(d.FieldPath) == FieldCategory.Critical)
            .ToList();

        // High impact for allergy and medication fields
        var criticalAllergyFields = safetyFields
            .Where(d => d.FieldPath.StartsWith("AL1", StringComparison.OrdinalIgnoreCase))
            .Count();

        var medicationFields = safetyFields
            .Where(d => d.FieldPath.StartsWith("RX", StringComparison.OrdinalIgnoreCase))
            .Count();

        // Calculate safety score based on critical field changes
        var totalCriticalChanges = criticalAllergyFields + medicationFields;
        return totalCriticalChanges > 0 ? Math.Min(1.0, totalCriticalChanges * 0.3) : 0.0;
    }

    private double CalculateCareQualityImpact(List<FieldDifference> fieldDifferences)
    {
        var clinicalFields = fieldDifferences
            .Where(d => CategorizeField(d.FieldPath) == FieldCategory.Clinical)
            .Count();

        return clinicalFields > 0 ? Math.Min(1.0, clinicalFields * 0.1) : 0.0;
    }

    private double CalculateOperationalImpact(List<FieldDifference> fieldDifferences)
    {
        var administrativeFields = fieldDifferences
            .Where(d => CategorizeField(d.FieldPath) == FieldCategory.Administrative)
            .Count();

        return administrativeFields > 0 ? Math.Min(1.0, administrativeFields * 0.05) : 0.0;
    }

    private void GenerateImpactAreasAndRecommendations(
        List<FieldDifference> fieldDifferences,
        List<ClinicalImpactArea> impactAreas,
        List<string> recommendations)
    {
        // Identify specific impact areas
        var allergyChanges = fieldDifferences
            .Where(d => d.FieldPath.StartsWith("AL1", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (allergyChanges.Any())
        {
            impactAreas.Add(new ClinicalImpactArea
            {
                Area = "Patient Safety - Allergies",
                ImpactDescription = "Changes to allergy information may affect medication safety alerts",
                RiskLevel = RiskLevel.High
            });
            recommendations.Add("Verify allergy information accuracy with clinical staff");
        }

        var demographicChanges = fieldDifferences
            .Where(d => CategorizeField(d.FieldPath) == FieldCategory.Demographics)
            .ToList();

        if (demographicChanges.Any())
        {
            impactAreas.Add(new ClinicalImpactArea
            {
                Area = "Patient Identification",
                ImpactDescription = "Demographic changes may affect patient matching and identification",
                RiskLevel = RiskLevel.Medium
            });
            recommendations.Add("Update patient identification systems with new demographic data");
        }
    }

    private double CalculateProceduralScore(
        ConstraintValidationResult? constraintValidation,
        DemographicAnalysisResult? demographicAnalysis,
        ClinicalImpactAssessment? clinicalImpact)
    {
        var scores = new List<double>();

        if (constraintValidation != null)
            scores.Add(constraintValidation.ComplianceScore);

        if (demographicAnalysis != null)
            scores.Add(demographicAnalysis.QualityScore);

        if (clinicalImpact != null)
        {
            // Invert impact scores (lower impact = higher procedural score)
            var impactScore = 1.0 - Math.Max(clinicalImpact.PatientSafetyScore,
                Math.Max(clinicalImpact.CareQualityScore, clinicalImpact.OperationalScore));
            scores.Add(impactScore);
        }

        return scores.Any() ? scores.Average() : 0.0; // No score if no data available
    }
}