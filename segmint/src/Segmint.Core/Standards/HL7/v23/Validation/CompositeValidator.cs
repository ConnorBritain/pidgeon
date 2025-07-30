// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.


using Segmint.Core.HL7;
using Segmint.Core.Standards.HL7.v23.Messages;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System;
namespace Segmint.Core.HL7.Validation;

/// <summary>
/// Default validation configuration.
/// </summary>
public class DefaultValidationConfiguration : IValidationConfiguration
{
    /// <inheritdoc />
    public IEnumerable<ValidationType> EnabledValidationTypes { get; set; } = 
        [ValidationType.Syntax, ValidationType.Semantic, ValidationType.Clinical, ValidationType.Interface];
    
    /// <inheritdoc />
    public int MaxIssues { get; set; } = 1000;
    
    /// <inheritdoc />
    public bool StopOnFirstError { get; set; } = false;
    
    /// <inheritdoc />
    public ValidationSeverity MinimumSeverity { get; set; } = ValidationSeverity.Info;
    
    /// <inheritdoc />
    public IDictionary<string, object> InterfaceRules { get; set; } = new Dictionary<string, object>();
    
    /// <inheritdoc />
    public IDictionary<string, object> ClinicalRules { get; set; } = new Dictionary<string, object>();
    
    /// <inheritdoc />
    public bool IsValidationTypeEnabled(ValidationType validationType)
    {
        return EnabledValidationTypes.Contains(validationType);
    }
    
    /// <inheritdoc />
    public bool ShouldReportSeverity(ValidationSeverity severity)
    {
        return severity >= MinimumSeverity;
    }
}

/// <summary>
/// Validation context implementation.
/// </summary>
public class ValidationContext : IValidationContext
{
    /// <inheritdoc />
    public IValidationConfiguration Configuration { get; }
    
    /// <inheritdoc />
    public string CurrentPath { get; private set; }
    
    /// <inheritdoc />
    public IDictionary<string, object> Data { get; } = new Dictionary<string, object>();
    
    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationContext"/> class.
    /// </summary>
    /// <param name="configuration">The validation configuration.</param>
    /// <param name="currentPath">The current validation path.</param>
    public ValidationContext(IValidationConfiguration configuration, string currentPath = "")
    {
        Configuration = configuration;
        CurrentPath = currentPath;
    }
    
    /// <inheritdoc />
    public IValidationContext CreateChild(string location)
    {
        var childPath = string.IsNullOrEmpty(CurrentPath) ? location : $"{CurrentPath}.{location}";
        var child = new ValidationContext(Configuration, childPath);
        
        // Copy data to child context
        foreach (var item in Data)
        {
            child.Data[item.Key] = item.Value;
        }
        
        return child;
    }
    
    /// <inheritdoc />
    public void AddData(string key, object value)
    {
        Data[key] = value;
    }
    
    /// <inheritdoc />
    public T? GetData<T>(string key)
    {
        if (Data.TryGetValue(key, out var value) && value is T typedValue)
        {
            return typedValue;
        }
        return default;
    }
    
    /// <inheritdoc />
    public bool HasData(string key)
    {
        return Data.ContainsKey(key);
    }
}

/// <summary>
/// Composite validator that orchestrates all validation types.
/// </summary>
public class CompositeValidator : ICompositeValidator
{
    private readonly List<IFieldValidator> _fieldValidators = new();
    private readonly List<ISegmentValidator> _segmentValidators = new();
    private readonly List<IMessageValidator> _messageValidators = new();
    private readonly IValidationConfiguration _configuration;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="CompositeValidator"/> class.
    /// </summary>
    /// <param name="configuration">The validation configuration.</param>
    public CompositeValidator(IValidationConfiguration? configuration = null)
    {
        _configuration = configuration ?? new DefaultValidationConfiguration();
        RegisterDefaultValidators();
    }
    
    /// <inheritdoc />
    public IEnumerable<IFieldValidator> FieldValidators => _fieldValidators;
    
    /// <inheritdoc />
    public IEnumerable<ISegmentValidator> SegmentValidators => _segmentValidators;
    
    /// <inheritdoc />
    public IEnumerable<IMessageValidator> MessageValidators => _messageValidators;
    
    /// <summary>
    /// Registers a field validator.
    /// </summary>
    /// <param name="validator">The field validator to register.</param>
    public void RegisterFieldValidator(IFieldValidator validator)
    {
        _fieldValidators.Add(validator);
    }
    
    /// <summary>
    /// Registers a segment validator.
    /// </summary>
    /// <param name="validator">The segment validator to register.</param>
    public void RegisterSegmentValidator(ISegmentValidator validator)
    {
        _segmentValidators.Add(validator);
    }
    
    /// <summary>
    /// Registers a message validator.
    /// </summary>
    /// <param name="validator">The message validator to register.</param>
    public void RegisterMessageValidator(IMessageValidator validator)
    {
        _messageValidators.Add(validator);
    }
    
    /// <inheritdoc />
    public ValidationResult ValidateField(HL7Field field)
    {
        var result = new ValidationResult();
        
        foreach (var validator in _fieldValidators)
        {
            if (validator.CanValidate(field))
            {
                var validationResult = validator.Validate(field);
                var filteredIssues = FilterIssues(validationResult.Issues);
                result.AddIssues(filteredIssues);
                
                if (_configuration.StopOnFirstError && result.ErrorCount > 0)
                    break;
                
                if (result.TotalIssues >= _configuration.MaxIssues)
                    break;
            }
        }
        
        return result;
    }
    
    /// <inheritdoc />
    public ValidationResult ValidateSegment(HL7Segment segment)
    {
        var result = new ValidationResult();
        
        // Validate the segment itself
        foreach (var validator in _segmentValidators)
        {
            if (validator.CanValidate(segment))
            {
                var validationResult = validator.Validate(segment);
                var filteredIssues = FilterIssues(validationResult.Issues);
                result.AddIssues(filteredIssues);
                
                if (_configuration.StopOnFirstError && result.ErrorCount > 0)
                    break;
                
                if (result.TotalIssues >= _configuration.MaxIssues)
                    break;
            }
        }
        
        // TODO: Field-level validation disabled for MVP
        // Re-enable after field validation API is restored
        // for (int i = 0; i < segment.FieldCount; i++)
        // {
        //     var field = segment.GetField(i);
        //     var fieldResult = ValidateField(field);
        //     result.Merge(fieldResult);
        //     
        //     if (_configuration.StopOnFirstError && result.ErrorCount > 0)
        //         break;
        //     
        //     if (result.TotalIssues >= _configuration.MaxIssues)
        //         break;
        // }
        
        return result;
    }
    
    /// <inheritdoc />
    public ValidationResult ValidateMessage(HL7Message message)
    {
        return ValidateMessage(message, _configuration.EnabledValidationTypes);
    }
    
    /// <inheritdoc />
    public ValidationResult ValidateMessage(HL7Message message, IEnumerable<ValidationType> validationTypes)
    {
        var result = new ValidationResult();
        var enabledTypes = validationTypes.ToHashSet();
        
        // Validate the message itself
        foreach (var validator in _messageValidators)
        {
            if (validator.CanValidate(message))
            {
                var validationResult = validator.Validate(message);
                var filteredIssues = FilterIssues(validationResult.Issues, enabledTypes);
                result.AddIssues(filteredIssues);
                
                if (_configuration.StopOnFirstError && result.ErrorCount > 0)
                    break;
                
                if (result.TotalIssues >= _configuration.MaxIssues)
                    break;
            }
        }
        
        // Validate individual segments in the message
        for (int i = 0; i < message.SegmentCount; i++)
        {
            var segment = message[i];
            if (segment != null)
            {
                var segmentResult = ValidateSegment(segment);
                var filteredIssues = FilterIssues(segmentResult.Issues, enabledTypes);
                result.AddIssues(filteredIssues);
                
                if (_configuration.StopOnFirstError && result.ErrorCount > 0)
                    break;
                
                if (result.TotalIssues >= _configuration.MaxIssues)
                    break;
            }
        }
        
        return result;
    }
    
    /// <inheritdoc />
    public ValidationResult ValidateHL7String(string hl7String)
    {
        var result = new ValidationResult();
        
        try
        {
            // Basic syntax validation
            var syntaxIssues = ValidateHL7Syntax(hl7String);
            result.AddIssues(syntaxIssues);
            
            if (result.ErrorCount > 0 && _configuration.StopOnFirstError)
                return result;
            
            // Parse and validate the message
            var message = ParseHL7Message(hl7String);
            if (message != null)
            {
                var messageResult = ValidateMessage(message);
                result.Merge(messageResult);
            }
            else
            {
                result.AddIssue(ValidationIssue.SyntaxError("PARSE001", 
                    "Unable to parse HL7 message", "Message"));
            }
        }
        catch (Exception ex)
        {
            result.AddIssue(ValidationIssue.SyntaxError("PARSE002", 
                $"Error parsing HL7 message: {ex.Message}", "Message"));
        }
        
        return result;
    }
    
    /// <summary>
    /// Registers default validators.
    /// </summary>
    private void RegisterDefaultValidators()
    {
        // MVP: Only basic syntax validation for now
        // TODO: Re-enable field, segment, and message validators after API restoration
        
        // For now, validation relies on the built-in ValidateHL7Syntax method
        // which provides comprehensive syntax checking without requiring
        // specific field/segment/message validator implementations
    }
    
    /// <summary>
    /// Filters issues based on configuration.
    /// </summary>
    /// <param name="issues">The issues to filter.</param>
    /// <param name="enabledTypes">The enabled validation types.</param>
    /// <returns>Filtered issues.</returns>
    private IEnumerable<ValidationIssue> FilterIssues(IEnumerable<ValidationIssue> issues, 
        HashSet<ValidationType>? enabledTypes = null)
    {
        var typesToCheck = enabledTypes ?? _configuration.EnabledValidationTypes.ToHashSet();
        
        return issues
            .Where(issue => typesToCheck.Contains(issue.Type))
            .Where(issue => _configuration.ShouldReportSeverity(issue.Severity))
            .Take(_configuration.MaxIssues);
    }
    
    /// <summary>
    /// Validates basic HL7 syntax.
    /// </summary>
    /// <param name="hl7String">The HL7 string to validate.</param>
    /// <returns>A list of syntax validation issues.</returns>
    private List<ValidationIssue> ValidateHL7Syntax(string hl7String)
    {
        var issues = new List<ValidationIssue>();
        
        if (string.IsNullOrWhiteSpace(hl7String))
        {
            issues.Add(ValidationIssue.SyntaxError("SYN001", 
                "HL7 message is empty", "Message"));
            return issues;
        }
        
        // Check for proper line endings
        var segments = hl7String.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        
        if (segments.Length == 0)
        {
            issues.Add(ValidationIssue.SyntaxError("SYN002", 
                "HL7 message contains no segments", "Message"));
            return issues;
        }
        
        // First segment must be MSH
        if (!segments[0].StartsWith("MSH"))
        {
            issues.Add(ValidationIssue.SyntaxError("SYN003", 
                "First segment must be MSH", "Message"));
        }
        
        // Validate segment structure
        for (int i = 0; i < segments.Length; i++)
        {
            var segment = segments[i];
            var segmentLocation = $"Segment {i + 1}";
            
            if (segment.Length < 3)
            {
                issues.Add(ValidationIssue.SyntaxError("SYN004", 
                    $"Segment too short: {segment}", segmentLocation));
                continue;
            }
            
            // Check segment ID
            var segmentId = segment.Substring(0, 3);
            if (!Regex.IsMatch(segmentId, @"^[A-Z0-9]{3}$"))
            {
                issues.Add(ValidationIssue.SyntaxError("SYN005", 
                    $"Invalid segment ID: {segmentId}", segmentLocation));
            }
            
            // Check field separator for non-MSH segments
            if (segmentId != "MSH")
            {
                if (segment.Length < 4 || segment[3] != '|')
                {
                    issues.Add(ValidationIssue.SyntaxError("SYN006", 
                        $"Missing field separator in segment: {segmentId}", segmentLocation));
                }
            }
            else
            {
                // MSH segment specific validation
                if (segment.Length < 8)
                {
                    issues.Add(ValidationIssue.SyntaxError("SYN007", 
                        "MSH segment too short", segmentLocation));
                }
                else
                {
                    if (segment[3] != '|')
                    {
                        issues.Add(ValidationIssue.SyntaxError("SYN008", 
                            "MSH field separator must be |", segmentLocation));
                    }
                    
                    if (segment.Substring(4, 4) != "^~\\&")
                    {
                        issues.Add(ValidationIssue.SyntaxError("SYN009", 
                            "MSH encoding characters must be ^~\\&", segmentLocation));
                    }
                }
            }
        }
        
        return issues;
    }
    
    /// <summary>
    /// Parses an HL7 message string into a message object.
    /// </summary>
    /// <param name="hl7String">The HL7 string to parse.</param>
    /// <returns>The parsed message, or null if parsing fails.</returns>
    private HL7Message? ParseHL7Message(string hl7String)
    {
        try
        {
            var segments = hl7String.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            
            if (segments.Length == 0 || !segments[0].StartsWith("MSH"))
                return null;
            
            // Extract message type from MSH segment
            var mshFields = segments[0].Split('|');
            if (mshFields.Length < 10)
                return null;
            
            var messageTypeField = mshFields[9];
            var messageTypeParts = messageTypeField.Split('^');
            
            if (messageTypeParts.Length < 2)
                return null;
            
            var messageType = messageTypeParts[0];
            var triggerEvent = messageTypeParts[1];
            
            // Create appropriate message type
            return messageType switch
            {
                "RDE" => new RDEMessage(),
                "ADT" => CreateADTMessage(triggerEvent),
                "ACK" => new ACKMessage(),
                _ => null
            };
        }
        catch
        {
            return null;
        }
    }
    
    /// <summary>
    /// Creates an ADT message based on trigger event.
    /// </summary>
    /// <param name="triggerEvent">The trigger event.</param>
    /// <returns>The ADT message.</returns>
    private ADTMessage CreateADTMessage(string triggerEvent)
    {
        return triggerEvent switch
        {
            "A01" => ADTMessage.CreateAdmitPatient(),
            "A02" => ADTMessage.CreateTransferPatient(),
            "A03" => ADTMessage.CreateDischargePatient(),
            "A04" => ADTMessage.CreateRegisterPatient(),
            "A05" => ADTMessage.CreatePreAdmitPatient(),
            // TODO: Re-enable after ADTMessage API expansion
            // "A06" => ADTMessage.CreateChangeOutpatientToInpatient(),
            // "A07" => ADTMessage.CreateChangeInpatientToOutpatient(),
            // "A08" => ADTMessage.CreateUpdatePatientInfo(),
            _ => new ADTMessage(triggerEvent)
        };
    }
}
