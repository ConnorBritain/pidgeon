// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.


using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System;
namespace Segmint.Core.HL7.Validation;

/// <summary>
/// Represents the severity level of a validation issue.
/// </summary>
public enum ValidationSeverity
{
    /// <summary>
    /// Informational message or suggestion.
    /// </summary>
    Info,
    
    /// <summary>
    /// Warning that should be addressed but doesn't prevent processing.
    /// </summary>
    Warning,
    
    /// <summary>
    /// Error that prevents successful processing.
    /// </summary>
    Error,
    
    /// <summary>
    /// Critical error that indicates severe compliance issues.
    /// </summary>
    Critical
}

/// <summary>
/// Represents the type of validation that detected an issue.
/// </summary>
public enum ValidationType
{
    /// <summary>
    /// Syntax validation (HL7 format compliance).
    /// </summary>
    Syntax,
    
    /// <summary>
    /// Semantic validation (field requirements, data types).
    /// </summary>
    Semantic,
    
    /// <summary>
    /// Interface validation (vendor-specific rules).
    /// </summary>
    Interface,
    
    /// <summary>
    /// Clinical validation (medical appropriateness).
    /// </summary>
    Clinical,
    
    /// <summary>
    /// Transport validation (network framing).
    /// </summary>
    Transport
}

/// <summary>
/// Represents a single validation issue found during validation.
/// </summary>
public class ValidationIssue
{
    /// <summary>
    /// Gets or sets the severity of this issue.
    /// </summary>
    public ValidationSeverity Severity { get; set; }
    
    /// <summary>
    /// Gets or sets the type of validation that detected this issue.
    /// </summary>
    public ValidationType Type { get; set; }
    
    /// <summary>
    /// Gets or sets the error code for this issue.
    /// </summary>
    public string Code { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the human-readable description of this issue.
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the location where this issue was found.
    /// </summary>
    public string Location { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the segment ID where this issue was found.
    /// </summary>
    public string? SegmentId { get; set; }
    
    /// <summary>
    /// Gets or sets the field number where this issue was found.
    /// </summary>
    public int? FieldNumber { get; set; }
    
    /// <summary>
    /// Gets or sets the component number where this issue was found.
    /// </summary>
    public int? ComponentNumber { get; set; }
    
    /// <summary>
    /// Gets or sets the actual value that caused the issue.
    /// </summary>
    public string? ActualValue { get; set; }
    
    /// <summary>
    /// Gets or sets the expected value or format.
    /// </summary>
    public string? ExpectedValue { get; set; }
    
    /// <summary>
    /// Gets or sets suggested fixes for this issue.
    /// </summary>
    public List<string> Suggestions { get; set; } = new();
    
    /// <summary>
    /// Gets or sets additional context information.
    /// </summary>
    public Dictionary<string, object> Context { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the timestamp when this issue was detected.
    /// </summary>
    public DateTime DetectedAt { get; set; } = DateTime.Now;
    
    /// <summary>
    /// Creates a new validation issue.
    /// </summary>
    /// <param name="severity">The severity level.</param>
    /// <param name="type">The validation type.</param>
    /// <param name="code">The error code.</param>
    /// <param name="description">The description.</param>
    /// <param name="location">The location.</param>
    /// <returns>A new ValidationIssue instance.</returns>
    public static ValidationIssue Create(ValidationSeverity severity, ValidationType type, 
        string code, string description, string location)
    {
        return new ValidationIssue
        {
            Severity = severity,
            Type = type,
            Code = code,
            Description = description,
            Location = location
        };
    }
    
    /// <summary>
    /// Creates a syntax error.
    /// </summary>
    /// <param name="code">The error code.</param>
    /// <param name="description">The description.</param>
    /// <param name="location">The location.</param>
    /// <returns>A new ValidationIssue instance.</returns>
    public static ValidationIssue SyntaxError(string code, string description, string location)
    {
        return Create(ValidationSeverity.Error, ValidationType.Syntax, code, description, location);
    }
    
    /// <summary>
    /// Creates a semantic error.
    /// </summary>
    /// <param name="code">The error code.</param>
    /// <param name="description">The description.</param>
    /// <param name="location">The location.</param>
    /// <returns>A new ValidationIssue instance.</returns>
    public static ValidationIssue SemanticError(string code, string description, string location)
    {
        return Create(ValidationSeverity.Error, ValidationType.Semantic, code, description, location);
    }
    
    /// <summary>
    /// Creates a clinical warning.
    /// </summary>
    /// <param name="code">The warning code.</param>
    /// <param name="description">The description.</param>
    /// <param name="location">The location.</param>
    /// <returns>A new ValidationIssue instance.</returns>
    public static ValidationIssue ClinicalWarning(string code, string description, string location)
    {
        return Create(ValidationSeverity.Warning, ValidationType.Clinical, code, description, location);
    }
    
    /// <summary>
    /// Creates an interface error.
    /// </summary>
    /// <param name="code">The error code.</param>
    /// <param name="description">The description.</param>
    /// <param name="location">The location.</param>
    /// <returns>A new ValidationIssue instance.</returns>
    public static ValidationIssue InterfaceError(string code, string description, string location)
    {
        return Create(ValidationSeverity.Error, ValidationType.Interface, code, description, location);
    }
    
    /// <summary>
    /// Sets the segment and field location for this issue.
    /// </summary>
    /// <param name="segmentId">The segment ID.</param>
    /// <param name="fieldNumber">The field number.</param>
    /// <param name="componentNumber">The component number (optional).</param>
    /// <returns>This ValidationIssue instance for method chaining.</returns>
    public ValidationIssue WithLocation(string segmentId, int fieldNumber, int? componentNumber = null)
    {
        SegmentId = segmentId;
        FieldNumber = fieldNumber;
        ComponentNumber = componentNumber;
        Location = componentNumber.HasValue 
            ? $"{segmentId}.{fieldNumber}.{componentNumber}"
            : $"{segmentId}.{fieldNumber}";
        return this;
    }
    
    /// <summary>
    /// Sets the actual and expected values for this issue.
    /// </summary>
    /// <param name="actualValue">The actual value.</param>
    /// <param name="expectedValue">The expected value.</param>
    /// <returns>This ValidationIssue instance for method chaining.</returns>
    public ValidationIssue WithValues(string? actualValue, string? expectedValue)
    {
        ActualValue = actualValue;
        ExpectedValue = expectedValue;
        return this;
    }
    
    /// <summary>
    /// Adds a suggestion for fixing this issue.
    /// </summary>
    /// <param name="suggestion">The suggestion text.</param>
    /// <returns>This ValidationIssue instance for method chaining.</returns>
    public ValidationIssue WithSuggestion(string suggestion)
    {
        Suggestions.Add(suggestion);
        return this;
    }
    
    /// <summary>
    /// Adds context information for this issue.
    /// </summary>
    /// <param name="key">The context key.</param>
    /// <param name="value">The context value.</param>
    /// <returns>This ValidationIssue instance for method chaining.</returns>
    public ValidationIssue WithContext(string key, object value)
    {
        Context[key] = value;
        return this;
    }
    
    /// <summary>
    /// Gets a formatted string representation of this issue.
    /// </summary>
    /// <returns>A formatted string representation.</returns>
    public override string ToString()
    {
        return $"[{Severity}] {Type} {Code}: {Description} at {Location}";
    }
}

/// <summary>
/// Represents the result of a validation operation.
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// Gets the list of validation issues found.
    /// </summary>
    public List<ValidationIssue> Issues { get; } = new();
    
    /// <summary>
    /// Gets a value indicating whether the validation was successful (no errors or critical issues).
    /// </summary>
    [JsonIgnore]
    public bool IsValid => !Issues.Any(i => i.Severity == ValidationSeverity.Error || i.Severity == ValidationSeverity.Critical);
    
    /// <summary>
    /// Gets the total number of issues found.
    /// </summary>
    [JsonIgnore]
    public int TotalIssues => Issues.Count;
    
    /// <summary>
    /// Gets the number of critical issues.
    /// </summary>
    [JsonIgnore]
    public int CriticalCount => Issues.Count(i => i.Severity == ValidationSeverity.Critical);
    
    /// <summary>
    /// Gets the number of error issues.
    /// </summary>
    [JsonIgnore]
    public int ErrorCount => Issues.Count(i => i.Severity == ValidationSeverity.Error);
    
    /// <summary>
    /// Gets the number of warning issues.
    /// </summary>
    [JsonIgnore]
    public int WarningCount => Issues.Count(i => i.Severity == ValidationSeverity.Warning);
    
    /// <summary>
    /// Gets the number of info issues.
    /// </summary>
    [JsonIgnore]
    public int InfoCount => Issues.Count(i => i.Severity == ValidationSeverity.Info);
    
    /// <summary>
    /// Gets all errors (critical and error severity).
    /// </summary>
    [JsonIgnore]
    public IEnumerable<ValidationIssue> Errors => Issues.Where(i => i.Severity == ValidationSeverity.Error || i.Severity == ValidationSeverity.Critical);
    
    /// <summary>
    /// Gets all warnings.
    /// </summary>
    [JsonIgnore]
    public IEnumerable<ValidationIssue> Warnings => Issues.Where(i => i.Severity == ValidationSeverity.Warning);
    
    /// <summary>
    /// Gets all info messages.
    /// </summary>
    [JsonIgnore]
    public IEnumerable<ValidationIssue> Infos => Issues.Where(i => i.Severity == ValidationSeverity.Info);
    
    /// <summary>
    /// Gets validation statistics.
    /// </summary>
    [JsonIgnore]
    public ValidationStats Stats => new()
    {
        TotalIssues = TotalIssues,
        CriticalCount = CriticalCount,
        ErrorCount = ErrorCount,
        WarningCount = WarningCount,
        InfoCount = InfoCount,
        IsValid = IsValid
    };
    
    /// <summary>
    /// Adds a validation issue to the result.
    /// </summary>
    /// <param name="issue">The validation issue to add.</param>
    public void AddIssue(ValidationIssue issue)
    {
        Issues.Add(issue);
    }
    
    /// <summary>
    /// Adds multiple validation issues to the result.
    /// </summary>
    /// <param name="issues">The validation issues to add.</param>
    public void AddIssues(IEnumerable<ValidationIssue> issues)
    {
        Issues.AddRange(issues);
    }
    
    /// <summary>
    /// Merges another validation result into this one.
    /// </summary>
    /// <param name="other">The other validation result to merge.</param>
    public void Merge(ValidationResult other)
    {
        Issues.AddRange(other.Issues);
    }
    
    /// <summary>
    /// Filters issues by severity.
    /// </summary>
    /// <param name="severity">The severity to filter by.</param>
    /// <returns>Issues with the specified severity.</returns>
    public IEnumerable<ValidationIssue> GetIssuesBySeverity(ValidationSeverity severity)
    {
        return Issues.Where(i => i.Severity == severity);
    }
    
    /// <summary>
    /// Filters issues by validation type.
    /// </summary>
    /// <param name="type">The validation type to filter by.</param>
    /// <returns>Issues with the specified validation type.</returns>
    public IEnumerable<ValidationIssue> GetIssuesByType(ValidationType type)
    {
        return Issues.Where(i => i.Type == type);
    }
    
    /// <summary>
    /// Filters issues by location.
    /// </summary>
    /// <param name="segmentId">The segment ID to filter by.</param>
    /// <param name="fieldNumber">The field number to filter by (optional).</param>
    /// <returns>Issues at the specified location.</returns>
    public IEnumerable<ValidationIssue> GetIssuesByLocation(string segmentId, int? fieldNumber = null)
    {
        return Issues.Where(i => i.SegmentId == segmentId && 
            (fieldNumber == null || i.FieldNumber == fieldNumber));
    }
    
    /// <summary>
    /// Gets a summary of the validation results.
    /// </summary>
    /// <returns>A formatted summary string.</returns>
    public string GetSummary()
    {
        if (IsValid)
        {
            return $"Validation successful. {TotalIssues} issues found (Info: {InfoCount}, Warning: {WarningCount})";
        }
        else
        {
            return $"Validation failed. {TotalIssues} issues found (Critical: {CriticalCount}, Error: {ErrorCount}, Warning: {WarningCount}, Info: {InfoCount})";
        }
    }
    
    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    /// <returns>A new ValidationResult instance with no issues.</returns>
    public static ValidationResult Success()
    {
        return new ValidationResult();
    }
    
    /// <summary>
    /// Creates a validation result with a single error.
    /// </summary>
    /// <param name="issue">The validation issue.</param>
    /// <returns>A new ValidationResult instance with the specified issue.</returns>
    public static ValidationResult WithIssue(ValidationIssue issue)
    {
        var result = new ValidationResult();
        result.AddIssue(issue);
        return result;
    }
    
    /// <summary>
    /// Creates a validation result with multiple issues.
    /// </summary>
    /// <param name="issues">The validation issues.</param>
    /// <returns>A new ValidationResult instance with the specified issues.</returns>
    public static ValidationResult WithIssues(IEnumerable<ValidationIssue> issues)
    {
        var result = new ValidationResult();
        result.AddIssues(issues);
        return result;
    }
}

/// <summary>
/// Represents validation statistics.
/// </summary>
public class ValidationStats
{
    /// <summary>
    /// Gets or sets the total number of issues.
    /// </summary>
    public int TotalIssues { get; set; }
    
    /// <summary>
    /// Gets or sets the number of critical issues.
    /// </summary>
    public int CriticalCount { get; set; }
    
    /// <summary>
    /// Gets or sets the number of error issues.
    /// </summary>
    public int ErrorCount { get; set; }
    
    /// <summary>
    /// Gets or sets the number of warning issues.
    /// </summary>
    public int WarningCount { get; set; }
    
    /// <summary>
    /// Gets or sets the number of info issues.
    /// </summary>
    public int InfoCount { get; set; }
    
    /// <summary>
    /// Gets or sets a value indicating whether the validation was successful.
    /// </summary>
    public bool IsValid { get; set; }
}
