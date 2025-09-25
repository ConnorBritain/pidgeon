// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System.Text.Json.Serialization;

namespace Pidgeon.Core.Domain.Comparison.Entities;

/// <summary>
/// Comprehensive result of procedural analysis on healthcare message differences.
/// Contains constraint validation, demographic analysis, and clinical impact assessment.
/// </summary>
public record ProceduralAnalysisResult
{
    /// <summary>
    /// Overall procedural analysis score (0.0 = major issues, 1.0 = procedurally sound).
    /// </summary>
    [JsonPropertyName("proceduralScore")]
    public double ProceduralScore { get; init; }

    /// <summary>
    /// Constraint validation results for field differences.
    /// </summary>
    [JsonPropertyName("constraintValidation")]
    public ConstraintValidationResult ConstraintValidation { get; init; } = new();

    /// <summary>
    /// Demographic data analysis results.
    /// </summary>
    [JsonPropertyName("demographicAnalysis")]
    public DemographicAnalysisResult DemographicAnalysis { get; init; } = new();

    /// <summary>
    /// Clinical impact assessment for the differences.
    /// </summary>
    [JsonPropertyName("clinicalImpact")]
    public ClinicalImpactAssessment ClinicalImpact { get; init; } = new();

    /// <summary>
    /// Procedural recommendations for resolving issues.
    /// </summary>
    [JsonPropertyName("recommendations")]
    public List<ProceduralRecommendation> Recommendations { get; init; } = new();

    /// <summary>
    /// Metadata about the procedural analysis execution.
    /// </summary>
    [JsonPropertyName("analysisMetadata")]
    public ProceduralAnalysisMetadata Metadata { get; init; } = new();
}

/// <summary>
/// Results of constraint validation against healthcare standards.
/// </summary>
public record ConstraintValidationResult
{
    /// <summary>
    /// Number of constraint violations found.
    /// </summary>
    [JsonPropertyName("violationCount")]
    public int ViolationCount { get; init; }

    /// <summary>
    /// Specific constraint violations detected.
    /// </summary>
    [JsonPropertyName("violations")]
    public List<ConstraintViolation> Violations { get; init; } = new();

    /// <summary>
    /// Required fields that are missing or have issues.
    /// </summary>
    [JsonPropertyName("requiredFieldIssues")]
    public List<RequiredFieldIssue> RequiredFieldIssues { get; init; } = new();

    /// <summary>
    /// Data type validation results.
    /// </summary>
    [JsonPropertyName("dataTypeValidation")]
    public List<DataTypeValidationResult> DataTypeValidation { get; init; } = new();

    /// <summary>
    /// Overall constraint compliance score (0.0 = non-compliant, 1.0 = fully compliant).
    /// </summary>
    [JsonPropertyName("complianceScore")]
    public double ComplianceScore { get; init; }
}

/// <summary>
/// Results of demographic data analysis.
/// </summary>
public record DemographicAnalysisResult
{
    /// <summary>
    /// Demographic field issues found.
    /// </summary>
    [JsonPropertyName("issues")]
    public List<DemographicIssue> Issues { get; init; } = new();

    /// <summary>
    /// Name validation results.
    /// </summary>
    [JsonPropertyName("nameValidation")]
    public NameValidationResult NameValidation { get; init; } = new();

    /// <summary>
    /// Date validation results.
    /// </summary>
    [JsonPropertyName("dateValidation")]
    public DateValidationResult DateValidation { get; init; } = new();

    /// <summary>
    /// Address validation results.
    /// </summary>
    [JsonPropertyName("addressValidation")]
    public AddressValidationResult AddressValidation { get; init; } = new();

    /// <summary>
    /// Overall demographic data quality score (0.0 = poor quality, 1.0 = high quality).
    /// </summary>
    [JsonPropertyName("qualityScore")]
    public double QualityScore { get; init; }
}

/// <summary>
/// Assessment of clinical impact from message differences.
/// </summary>
public record ClinicalImpactAssessment
{
    /// <summary>
    /// Overall clinical impact level.
    /// </summary>
    [JsonPropertyName("impactLevel")]
    public ClinicalImpactLevel ImpactLevel { get; init; }

    /// <summary>
    /// Patient safety impact score (0.0 = no impact, 1.0 = high safety risk).
    /// </summary>
    [JsonPropertyName("patientSafetyScore")]
    public double PatientSafetyScore { get; init; }

    /// <summary>
    /// Care quality impact score (0.0 = no impact, 1.0 = high care impact).
    /// </summary>
    [JsonPropertyName("careQualityScore")]
    public double CareQualityScore { get; init; }

    /// <summary>
    /// Operational impact score (0.0 = no impact, 1.0 = high operational impact).
    /// </summary>
    [JsonPropertyName("operationalScore")]
    public double OperationalScore { get; init; }

    /// <summary>
    /// Specific clinical impact areas affected.
    /// </summary>
    [JsonPropertyName("impactAreas")]
    public List<ClinicalImpactArea> ImpactAreas { get; init; } = new();

    /// <summary>
    /// Clinical recommendations based on the impact assessment.
    /// </summary>
    [JsonPropertyName("clinicalRecommendations")]
    public List<string> ClinicalRecommendations { get; init; } = new();
}

/// <summary>
/// A specific constraint violation found during validation.
/// </summary>
public record ConstraintViolation
{
    /// <summary>
    /// Field path where the violation occurred.
    /// </summary>
    [JsonPropertyName("fieldPath")]
    public string FieldPath { get; init; } = "";

    /// <summary>
    /// Type of constraint violation.
    /// </summary>
    [JsonPropertyName("violationType")]
    public ConstraintViolationType ViolationType { get; init; }

    /// <summary>
    /// Description of the constraint that was violated.
    /// </summary>
    [JsonPropertyName("constraintDescription")]
    public string ConstraintDescription { get; init; } = "";

    /// <summary>
    /// Expected value or format.
    /// </summary>
    [JsonPropertyName("expectedValue")]
    public string? ExpectedValue { get; init; }

    /// <summary>
    /// Actual value that violated the constraint.
    /// </summary>
    [JsonPropertyName("actualValue")]
    public string? ActualValue { get; init; }

    /// <summary>
    /// Severity of this specific violation.
    /// </summary>
    [JsonPropertyName("severity")]
    public ConstraintViolationSeverity Severity { get; init; }

    /// <summary>
    /// Suggested resolution for this violation.
    /// </summary>
    [JsonPropertyName("suggestedResolution")]
    public string? SuggestedResolution { get; init; }
}

/// <summary>
/// Issue with a required field.
/// </summary>
public record RequiredFieldIssue
{
    /// <summary>
    /// Path of the required field.
    /// </summary>
    [JsonPropertyName("fieldPath")]
    public string FieldPath { get; init; } = "";

    /// <summary>
    /// Name of the required field.
    /// </summary>
    [JsonPropertyName("fieldName")]
    public string FieldName { get; init; } = "";

    /// <summary>
    /// Type of issue with the required field.
    /// </summary>
    [JsonPropertyName("issueType")]
    public RequiredFieldIssueType IssueType { get; init; }

    /// <summary>
    /// Description of why this field is required.
    /// </summary>
    [JsonPropertyName("requirementReason")]
    public string RequirementReason { get; init; } = "";

    /// <summary>
    /// Suggested value or action for the required field.
    /// </summary>
    [JsonPropertyName("suggestedAction")]
    public string SuggestedAction { get; init; } = "";
}

/// <summary>
/// Result of validating a field's data type.
/// </summary>
public record DataTypeValidationResult
{
    /// <summary>
    /// Field path being validated.
    /// </summary>
    [JsonPropertyName("fieldPath")]
    public string FieldPath { get; init; } = "";

    /// <summary>
    /// Expected data type.
    /// </summary>
    [JsonPropertyName("expectedType")]
    public string ExpectedType { get; init; } = "";

    /// <summary>
    /// Whether the field value matches the expected type.
    /// </summary>
    [JsonPropertyName("isValid")]
    public bool IsValid { get; init; }

    /// <summary>
    /// Validation error message if invalid.
    /// </summary>
    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Suggested format or correction.
    /// </summary>
    [JsonPropertyName("suggestedFormat")]
    public string? SuggestedFormat { get; init; }
}

/// <summary>
/// Procedural recommendation for resolving analysis issues.
/// </summary>
public record ProceduralRecommendation
{
    /// <summary>
    /// Type of procedural recommendation.
    /// </summary>
    [JsonPropertyName("type")]
    public RecommendationType Type { get; init; }

    /// <summary>
    /// Priority level of this recommendation.
    /// </summary>
    [JsonPropertyName("priority")]
    public RecommendationPriority Priority { get; init; }

    /// <summary>
    /// Title of the recommendation.
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; init; } = "";

    /// <summary>
    /// Detailed description of the recommendation.
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; init; } = "";

    /// <summary>
    /// Specific actions to take.
    /// </summary>
    [JsonPropertyName("actions")]
    public List<string> Actions { get; init; } = new();

    /// <summary>
    /// Fields affected by this recommendation.
    /// </summary>
    [JsonPropertyName("affectedFields")]
    public List<string> AffectedFields { get; init; } = new();

    /// <summary>
    /// Expected impact of following this recommendation.
    /// </summary>
    [JsonPropertyName("expectedImpact")]
    public string ExpectedImpact { get; init; } = "";
}

/// <summary>
/// Metadata about procedural analysis execution.
/// </summary>
public record ProceduralAnalysisMetadata
{
    /// <summary>
    /// Time taken to perform the analysis.
    /// </summary>
    [JsonPropertyName("executionTime")]
    public TimeSpan ExecutionTime { get; init; }

    /// <summary>
    /// Number of fields analyzed.
    /// </summary>
    [JsonPropertyName("fieldsAnalyzed")]
    public int FieldsAnalyzed { get; init; }

    /// <summary>
    /// Number of constraints evaluated.
    /// </summary>
    [JsonPropertyName("constraintsEvaluated")]
    public int ConstraintsEvaluated { get; init; }

    /// <summary>
    /// Number of demographic validations performed.
    /// </summary>
    [JsonPropertyName("demographicValidations")]
    public int DemographicValidations { get; init; }

    /// <summary>
    /// Analysis engine version used.
    /// </summary>
    [JsonPropertyName("engineVersion")]
    public string EngineVersion { get; init; } = "1.0";

    /// <summary>
    /// Whether analysis was performed using cached data.
    /// </summary>
    [JsonPropertyName("usedCache")]
    public bool UsedCache { get; init; }
}

/// <summary>
/// Demographic data issue types.
/// </summary>
public record DemographicIssue
{
    [JsonPropertyName("fieldPath")]
    public string FieldPath { get; init; } = "";

    [JsonPropertyName("issueType")]
    public DemographicIssueType IssueType { get; init; }

    [JsonPropertyName("description")]
    public string Description { get; init; } = "";

    [JsonPropertyName("severity")]
    public DemographicIssueSeverity Severity { get; init; }
}

/// <summary>
/// Name validation result.
/// </summary>
public record NameValidationResult
{
    [JsonPropertyName("isValid")]
    public bool IsValid { get; init; }

    [JsonPropertyName("issues")]
    public List<string> Issues { get; init; } = new();

    [JsonPropertyName("suggestions")]
    public List<string> Suggestions { get; init; } = new();
}

/// <summary>
/// Date validation result.
/// </summary>
public record DateValidationResult
{
    [JsonPropertyName("isValid")]
    public bool IsValid { get; init; }

    [JsonPropertyName("dateFormatIssues")]
    public List<string> DateFormatIssues { get; init; } = new();

    [JsonPropertyName("logicalIssues")]
    public List<string> LogicalIssues { get; init; } = new();
}

/// <summary>
/// Address validation result.
/// </summary>
public record AddressValidationResult
{
    [JsonPropertyName("isValid")]
    public bool IsValid { get; init; }

    [JsonPropertyName("issues")]
    public List<string> Issues { get; init; } = new();

    [JsonPropertyName("standardizedForm")]
    public string? StandardizedForm { get; init; }
}

/// <summary>
/// Clinical impact area affected by differences.
/// </summary>
public record ClinicalImpactArea
{
    [JsonPropertyName("area")]
    public string Area { get; init; } = "";

    [JsonPropertyName("impactDescription")]
    public string ImpactDescription { get; init; } = "";

    [JsonPropertyName("riskLevel")]
    public RiskLevel RiskLevel { get; init; }
}

/// <summary>
/// Clinical impact level enumeration.
/// </summary>
public enum ClinicalImpactLevel
{
    None,
    Low,
    Moderate,
    High,
    Critical
}

/// <summary>
/// Constraint violation type enumeration.
/// </summary>
public enum ConstraintViolationType
{
    RequiredFieldMissing,
    InvalidDataType,
    ValueOutOfRange,
    InvalidFormat,
    ReferenceConstraintViolation,
    CrossFieldConstraintViolation
}

/// <summary>
/// Constraint violation severity enumeration.
/// </summary>
public enum ConstraintViolationSeverity
{
    Info,
    Warning,
    Error,
    Critical
}

/// <summary>
/// Required field issue type enumeration.
/// </summary>
public enum RequiredFieldIssueType
{
    Missing,
    Empty,
    Invalid,
    Conditional
}

/// <summary>
/// Demographic issue type enumeration.
/// </summary>
public enum DemographicIssueType
{
    InvalidName,
    InvalidDate,
    InvalidAddress,
    InconsistentData,
    MissingRequiredDemo
}

/// <summary>
/// Demographic issue severity enumeration.
/// </summary>
public enum DemographicIssueSeverity
{
    Info,
    Warning,
    Error
}

/// <summary>
/// Recommendation type enumeration.
/// </summary>
public enum RecommendationType
{
    FieldCorrection,
    ConstraintCompliance,
    DataQualityImprovement,
    ClinicalSafety,
    OperationalEfficiency
}

/// <summary>
/// Recommendation priority enumeration.
/// </summary>
public enum RecommendationPriority
{
    Low,
    Medium,
    High,
    Critical
}

/// <summary>
/// Risk level enumeration.
/// </summary>
public enum RiskLevel
{
    None,
    Low,
    Medium,
    High,
    Critical
}

/// <summary>
/// Field category enumeration for healthcare message fields.
/// </summary>
public enum FieldCategory
{
    System,           // MSH, EVN - message control fields
    Demographics,     // PID, NK1 - patient demographic information
    Clinical,         // DG1, RXE, OBX - clinical and medical data
    Critical,         // AL1 - high-impact safety-related fields
    Administrative    // PV1, IN1 - workflow and billing information
}