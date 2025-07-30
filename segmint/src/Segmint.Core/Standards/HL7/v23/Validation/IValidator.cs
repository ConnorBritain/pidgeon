// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.


using Segmint.Core.HL7;
using Segmint.Core.Standards.HL7.v23.Messages;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System;
namespace Segmint.Core.HL7.Validation;

/// <summary>
/// Defines the contract for HL7 validators.
/// </summary>
/// <typeparam name="T">The type of object to validate.</typeparam>
public interface IValidator<in T>
{
    /// <summary>
    /// Gets the validation types supported by this validator.
    /// </summary>
    IEnumerable<ValidationType> SupportedTypes { get; }
    
    /// <summary>
    /// Validates the specified object.
    /// </summary>
    /// <param name="item">The object to validate.</param>
    /// <returns>The validation result.</returns>
    ValidationResult Validate(T item);
    
    /// <summary>
    /// Validates the specified object asynchronously.
    /// </summary>
    /// <param name="item">The object to validate.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The validation result.</returns>
    Task<ValidationResult> ValidateAsync(T item, CancellationToken cancellationToken = default);
}

/// <summary>
/// Defines the contract for field validators.
/// </summary>
public interface IFieldValidator : IValidator<HL7Field>
{
    /// <summary>
    /// Gets the field types supported by this validator.
    /// </summary>
    IEnumerable<string> SupportedFieldTypes { get; }
    
    /// <summary>
    /// Determines whether this validator can validate the specified field.
    /// </summary>
    /// <param name="field">The field to check.</param>
    /// <returns>True if this validator can validate the field, false otherwise.</returns>
    bool CanValidate(HL7Field field);
}

/// <summary>
/// Defines the contract for segment validators.
/// </summary>
public interface ISegmentValidator : IValidator<HL7Segment>
{
    /// <summary>
    /// Gets the segment IDs supported by this validator.
    /// </summary>
    IEnumerable<string> SupportedSegmentIds { get; }
    
    /// <summary>
    /// Determines whether this validator can validate the specified segment.
    /// </summary>
    /// <param name="segment">The segment to check.</param>
    /// <returns>True if this validator can validate the segment, false otherwise.</returns>
    bool CanValidate(HL7Segment segment);
}

/// <summary>
/// Defines the contract for message validators.
/// </summary>
public interface IMessageValidator : IValidator<HL7Message>
{
    /// <summary>
    /// Gets the message types supported by this validator.
    /// </summary>
    IEnumerable<string> SupportedMessageTypes { get; }
    
    /// <summary>
    /// Determines whether this validator can validate the specified message.
    /// </summary>
    /// <param name="message">The message to check.</param>
    /// <returns>True if this validator can validate the message, false otherwise.</returns>
    bool CanValidate(HL7Message message);
}

/// <summary>
/// Defines the contract for composite validators that can validate multiple types.
/// </summary>
public interface ICompositeValidator
{
    /// <summary>
    /// Gets the field validators.
    /// </summary>
    IEnumerable<IFieldValidator> FieldValidators { get; }
    
    /// <summary>
    /// Gets the segment validators.
    /// </summary>
    IEnumerable<ISegmentValidator> SegmentValidators { get; }
    
    /// <summary>
    /// Gets the message validators.
    /// </summary>
    IEnumerable<IMessageValidator> MessageValidators { get; }
    
    /// <summary>
    /// Validates an HL7 field.
    /// </summary>
    /// <param name="field">The field to validate.</param>
    /// <returns>The validation result.</returns>
    ValidationResult ValidateField(HL7Field field);
    
    /// <summary>
    /// Validates an HL7 segment.
    /// </summary>
    /// <param name="segment">The segment to validate.</param>
    /// <returns>The validation result.</returns>
    ValidationResult ValidateSegment(HL7Segment segment);
    
    /// <summary>
    /// Validates an HL7 message.
    /// </summary>
    /// <param name="message">The message to validate.</param>
    /// <returns>The validation result.</returns>
    ValidationResult ValidateMessage(HL7Message message);
    
    /// <summary>
    /// Validates an HL7 message with specified validation types.
    /// </summary>
    /// <param name="message">The message to validate.</param>
    /// <param name="validationTypes">The types of validation to perform.</param>
    /// <returns>The validation result.</returns>
    ValidationResult ValidateMessage(HL7Message message, IEnumerable<ValidationType> validationTypes);
    
    /// <summary>
    /// Validates an HL7 message string.
    /// </summary>
    /// <param name="hl7String">The HL7 message string to validate.</param>
    /// <returns>The validation result.</returns>
    ValidationResult ValidateHL7String(string hl7String);
}

/// <summary>
/// Defines the contract for validation configuration.
/// </summary>
public interface IValidationConfiguration
{
    /// <summary>
    /// Gets the enabled validation types.
    /// </summary>
    IEnumerable<ValidationType> EnabledValidationTypes { get; }
    
    /// <summary>
    /// Gets the maximum number of issues to report.
    /// </summary>
    int MaxIssues { get; }
    
    /// <summary>
    /// Gets a value indicating whether to stop validation on first error.
    /// </summary>
    bool StopOnFirstError { get; }
    
    /// <summary>
    /// Gets the minimum severity level to report.
    /// </summary>
    ValidationSeverity MinimumSeverity { get; }
    
    /// <summary>
    /// Gets the interface-specific validation rules.
    /// </summary>
    IDictionary<string, object> InterfaceRules { get; }
    
    /// <summary>
    /// Gets the clinical validation rules.
    /// </summary>
    IDictionary<string, object> ClinicalRules { get; }
    
    /// <summary>
    /// Determines whether a specific validation type is enabled.
    /// </summary>
    /// <param name="validationType">The validation type to check.</param>
    /// <returns>True if the validation type is enabled, false otherwise.</returns>
    bool IsValidationTypeEnabled(ValidationType validationType);
    
    /// <summary>
    /// Determines whether an issue should be reported based on its severity.
    /// </summary>
    /// <param name="severity">The severity level to check.</param>
    /// <returns>True if the issue should be reported, false otherwise.</returns>
    bool ShouldReportSeverity(ValidationSeverity severity);
}

/// <summary>
/// Defines the contract for validation context.
/// </summary>
public interface IValidationContext
{
    /// <summary>
    /// Gets the validation configuration.
    /// </summary>
    IValidationConfiguration Configuration { get; }
    
    /// <summary>
    /// Gets the current validation path.
    /// </summary>
    string CurrentPath { get; }
    
    /// <summary>
    /// Gets the context data.
    /// </summary>
    IDictionary<string, object> Data { get; }
    
    /// <summary>
    /// Creates a child context for a specific location.
    /// </summary>
    /// <param name="location">The location identifier.</param>
    /// <returns>A new validation context for the specified location.</returns>
    IValidationContext CreateChild(string location);
    
    /// <summary>
    /// Adds data to the context.
    /// </summary>
    /// <param name="key">The data key.</param>
    /// <param name="value">The data value.</param>
    void AddData(string key, object value);
    
    /// <summary>
    /// Gets data from the context.
    /// </summary>
    /// <typeparam name="T">The type of data to retrieve.</typeparam>
    /// <param name="key">The data key.</param>
    /// <returns>The data value, or default if not found.</returns>
    T? GetData<T>(string key);
    
    /// <summary>
    /// Determines whether the context has data for the specified key.
    /// </summary>
    /// <param name="key">The data key.</param>
    /// <returns>True if the context has data for the key, false otherwise.</returns>
    bool HasData(string key);
}
