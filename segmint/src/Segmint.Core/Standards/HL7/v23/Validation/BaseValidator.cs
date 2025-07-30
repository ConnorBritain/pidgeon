// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.


using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;
namespace Segmint.Core.HL7.Validation;

/// <summary>
/// Base class for all HL7 validators.
/// </summary>
/// <typeparam name="T">The type of object to validate.</typeparam>
public abstract class BaseValidator<T> : IValidator<T>
{
    /// <inheritdoc />
    public abstract IEnumerable<ValidationType> SupportedTypes { get; }
    
    /// <inheritdoc />
    public abstract ValidationResult Validate(T item);
    
    /// <inheritdoc />
    public virtual async Task<ValidationResult> ValidateAsync(T item, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() => Validate(item), cancellationToken);
    }
    
    /// <summary>
    /// Creates a validation issue for the specified parameters.
    /// </summary>
    /// <param name="severity">The severity level.</param>
    /// <param name="type">The validation type.</param>
    /// <param name="code">The error code.</param>
    /// <param name="description">The description.</param>
    /// <param name="location">The location.</param>
    /// <returns>A new ValidationIssue instance.</returns>
    protected static ValidationIssue CreateIssue(ValidationSeverity severity, ValidationType type, 
        string code, string description, string location)
    {
        return ValidationIssue.Create(severity, type, code, description, location);
    }
    
    /// <summary>
    /// Creates a syntax error issue.
    /// </summary>
    /// <param name="code">The error code.</param>
    /// <param name="description">The description.</param>
    /// <param name="location">The location.</param>
    /// <returns>A new ValidationIssue instance.</returns>
    protected static ValidationIssue SyntaxError(string code, string description, string location)
    {
        return ValidationIssue.SyntaxError(code, description, location);
    }
    
    /// <summary>
    /// Creates a semantic error issue.
    /// </summary>
    /// <param name="code">The error code.</param>
    /// <param name="description">The description.</param>
    /// <param name="location">The location.</param>
    /// <returns>A new ValidationIssue instance.</returns>
    protected static ValidationIssue SemanticError(string code, string description, string location)
    {
        return ValidationIssue.SemanticError(code, description, location);
    }
    
    /// <summary>
    /// Creates a clinical warning issue.
    /// </summary>
    /// <param name="code">The warning code.</param>
    /// <param name="description">The description.</param>
    /// <param name="location">The location.</param>
    /// <returns>A new ValidationIssue instance.</returns>
    protected static ValidationIssue ClinicalWarning(string code, string description, string location)
    {
        return ValidationIssue.ClinicalWarning(code, description, location);
    }
    
    /// <summary>
    /// Creates an interface error issue.
    /// </summary>
    /// <param name="code">The error code.</param>
    /// <param name="description">The description.</param>
    /// <param name="location">The location.</param>
    /// <returns>A new ValidationIssue instance.</returns>
    protected static ValidationIssue InterfaceError(string code, string description, string location)
    {
        return ValidationIssue.InterfaceError(code, description, location);
    }
    
    /// <summary>
    /// Validates that a required field is not empty.
    /// </summary>
    /// <param name="field">The field to validate.</param>
    /// <param name="location">The field location.</param>
    /// <returns>A validation issue if the field is empty, null otherwise.</returns>
    protected static ValidationIssue? ValidateRequired(HL7Field field, string location)
    {
        if (field.IsRequired && field.IsEmpty)
        {
            return SemanticError("REQ001", $"Required field is empty", location);
        }
        return null;
    }
    
    /// <summary>
    /// Validates that a field's length does not exceed the maximum.
    /// </summary>
    /// <param name="field">The field to validate.</param>
    /// <param name="location">The field location.</param>
    /// <returns>A validation issue if the field exceeds max length, null otherwise.</returns>
    protected static ValidationIssue? ValidateMaxLength(HL7Field field, string location)
    {
        if (field.MaxLength.HasValue && field.RawValue.Length > field.MaxLength.Value)
        {
            return SemanticError("LEN001", 
                $"Field length {field.RawValue.Length} exceeds maximum {field.MaxLength.Value}", location)
                .WithValues(field.RawValue, $"Max length: {field.MaxLength.Value}");
        }
        return null;
    }
    
    /// <summary>
    /// Validates that a field does not contain HL7 control characters.
    /// </summary>
    /// <param name="field">The field to validate.</param>
    /// <param name="location">The field location.</param>
    /// <returns>A validation issue if control characters are found, null otherwise.</returns>
    protected static ValidationIssue? ValidateNoControlCharacters(HL7Field field, string location)
    {
        if (field.RawValue.Any(c => c == '|' || c == '^' || c == '&' || c == '~' || c == '\\'))
        {
            return SyntaxError("CTRL001", 
                "Field contains HL7 control characters", location)
                .WithValues(field.RawValue, "Control characters must be escaped");
        }
        return null;
    }
    
    /// <summary>
    /// Validates that a string value matches a regular expression pattern.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="pattern">The regex pattern.</param>
    /// <param name="location">The field location.</param>
    /// <param name="errorCode">The error code.</param>
    /// <param name="description">The error description.</param>
    /// <returns>A validation issue if the pattern doesn't match, null otherwise.</returns>
    protected static ValidationIssue? ValidatePattern(string value, string pattern, string location, 
        string errorCode, string description)
    {
        if (!string.IsNullOrEmpty(value) && !System.Text.RegularExpressions.Regex.IsMatch(value, pattern))
        {
            return SyntaxError(errorCode, description, location)
                .WithValues(value, $"Pattern: {pattern}");
        }
        return null;
    }
    
    /// <summary>
    /// Validates that a value is in a list of allowed values.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="allowedValues">The allowed values.</param>
    /// <param name="location">The field location.</param>
    /// <param name="errorCode">The error code.</param>
    /// <param name="description">The error description.</param>
    /// <returns>A validation issue if the value is not allowed, null otherwise.</returns>
    protected static ValidationIssue? ValidateAllowedValues(string value, IEnumerable<string> allowedValues, 
        string location, string errorCode, string description)
    {
        if (!string.IsNullOrEmpty(value) && !allowedValues.Contains(value))
        {
            return SemanticError(errorCode, description, location)
                .WithValues(value, $"Allowed values: {string.Join(", ", allowedValues)}");
        }
        return null;
    }
    
    /// <summary>
    /// Validates that a numeric value is within a specified range.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="min">The minimum value.</param>
    /// <param name="max">The maximum value.</param>
    /// <param name="location">The field location.</param>
    /// <param name="errorCode">The error code.</param>
    /// <param name="description">The error description.</param>
    /// <returns>A validation issue if the value is out of range, null otherwise.</returns>
    protected static ValidationIssue? ValidateRange(decimal value, decimal min, decimal max, 
        string location, string errorCode, string description)
    {
        if (value < min || value > max)
        {
            return SemanticError(errorCode, description, location)
                .WithValues(value.ToString(), $"Range: {min} - {max}");
        }
        return null;
    }
    
    /// <summary>
    /// Validates that a date value is within a reasonable range.
    /// </summary>
    /// <param name="date">The date to validate.</param>
    /// <param name="location">The field location.</param>
    /// <returns>A validation issue if the date is unreasonable, null otherwise.</returns>
    protected static ValidationIssue? ValidateReasonableDate(DateTime date, string location)
    {
        var minDate = new DateTime(1900, 1, 1);
        var maxDate = DateTime.Now.AddYears(100);
        
        if (date < minDate || date > maxDate)
        {
            return SemanticError("DATE001", 
                $"Date {date:yyyy-MM-dd} is outside reasonable range", location)
                .WithValues(date.ToString("yyyy-MM-dd"), $"Range: {minDate:yyyy-MM-dd} - {maxDate:yyyy-MM-dd}");
        }
        return null;
    }
    
    /// <summary>
    /// Validates that a birth date is reasonable for a living person.
    /// </summary>
    /// <param name="birthDate">The birth date to validate.</param>
    /// <param name="location">The field location.</param>
    /// <returns>A validation issue if the birth date is unreasonable, null otherwise.</returns>
    protected static ValidationIssue? ValidateBirthDate(DateTime birthDate, string location)
    {
        var minDate = new DateTime(1900, 1, 1);
        var maxDate = DateTime.Now;
        
        if (birthDate < minDate || birthDate > maxDate)
        {
            return SemanticError("BIRTH001", 
                $"Birth date {birthDate:yyyy-MM-dd} is outside reasonable range", location)
                .WithValues(birthDate.ToString("yyyy-MM-dd"), $"Range: {minDate:yyyy-MM-dd} - {maxDate:yyyy-MM-dd}");
        }
        
        var age = DateTime.Now.Year - birthDate.Year;
        if (age > 150)
        {
            return ClinicalWarning("BIRTH002", 
                $"Patient age {age} is unusually high", location)
                .WithValues(age.ToString(), "Age > 150 years");
        }
        
        return null;
    }
    
    /// <summary>
    /// Collects all non-null validation issues from the provided issues.
    /// </summary>
    /// <param name="issues">The validation issues to collect.</param>
    /// <returns>A list of non-null validation issues.</returns>
    protected static List<ValidationIssue> CollectIssues(params ValidationIssue?[] issues)
    {
        return issues.Where(i => i != null).Cast<ValidationIssue>().ToList();
    }
    
    /// <summary>
    /// Creates a validation result with the specified issues.
    /// </summary>
    /// <param name="issues">The validation issues.</param>
    /// <returns>A new ValidationResult instance.</returns>
    protected static ValidationResult CreateResult(params ValidationIssue?[] issues)
    {
        var nonNullIssues = CollectIssues(issues);
        return nonNullIssues.Any() ? ValidationResult.WithIssues(nonNullIssues) : ValidationResult.Success();
    }
    
    /// <summary>
    /// Creates a validation result with the specified issues.
    /// </summary>
    /// <param name="issues">The validation issues.</param>
    /// <returns>A new ValidationResult instance.</returns>
    protected static ValidationResult CreateResult(IEnumerable<ValidationIssue> issues)
    {
        var issueList = issues.ToList();
        return issueList.Any() ? ValidationResult.WithIssues(issueList) : ValidationResult.Success();
    }
}
