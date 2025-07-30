// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Segmint.Core.HL7.Validation;

namespace Segmint.Core.Configuration;

/// <summary>
/// Represents the main configuration for Segmint.
/// </summary>
public class SegmintConfiguration
{
    /// <summary>
    /// Gets or sets the HL7 configuration.
    /// </summary>
    public HL7Configuration HL7 { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the validation configuration.
    /// </summary>
    public ValidationConfiguration Validation { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the interface configuration.
    /// </summary>
    public InterfaceConfiguration Interface { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the logging configuration.
    /// </summary>
    public LoggingConfiguration Logging { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the performance configuration.
    /// </summary>
    public PerformanceConfiguration Performance { get; set; } = new();
}

/// <summary>
/// HL7 specific configuration settings.
/// </summary>
public class HL7Configuration
{
    /// <summary>
    /// Gets or sets the default HL7 version.
    /// </summary>
    public string DefaultVersion { get; set; } = "2.3";
    
    /// <summary>
    /// Gets or sets the field separator character.
    /// </summary>
    public char FieldSeparator { get; set; } = '|';
    
    /// <summary>
    /// Gets or sets the component separator character.
    /// </summary>
    public char ComponentSeparator { get; set; } = '^';
    
    /// <summary>
    /// Gets or sets the repetition separator character.
    /// </summary>
    public char RepetitionSeparator { get; set; } = '~';
    
    /// <summary>
    /// Gets or sets the escape character.
    /// </summary>
    public char EscapeCharacter { get; set; } = '\\';
    
    /// <summary>
    /// Gets or sets the subcomponent separator character.
    /// </summary>
    public char SubcomponentSeparator { get; set; } = '&';
    
    /// <summary>
    /// Gets or sets the segment terminator.
    /// </summary>
    public string SegmentTerminator { get; set; } = "\r";
    
    /// <summary>
    /// Gets or sets the encoding characters string.
    /// </summary>
    [JsonIgnore]
    public string EncodingCharacters => $"{ComponentSeparator}{RepetitionSeparator}{EscapeCharacter}{SubcomponentSeparator}";
    
    /// <summary>
    /// Gets or sets the default sending application.
    /// </summary>
    public string DefaultSendingApplication { get; set; } = "Segmint";
    
    /// <summary>
    /// Gets or sets the default sending facility.
    /// </summary>
    public string DefaultSendingFacility { get; set; } = "";
    
    /// <summary>
    /// Gets or sets the default receiving application.
    /// </summary>
    public string DefaultReceivingApplication { get; set; } = "";
    
    /// <summary>
    /// Gets or sets the default receiving facility.
    /// </summary>
    public string DefaultReceivingFacility { get; set; } = "";
    
    /// <summary>
    /// Gets or sets the default processing ID.
    /// </summary>
    public string DefaultProcessingId { get; set; } = "P";
    
    /// <summary>
    /// Gets or sets whether to generate message control IDs automatically.
    /// </summary>
    public bool AutoGenerateControlId { get; set; } = true;
    
    /// <summary>
    /// Gets or sets the control ID format.
    /// </summary>
    public string ControlIdFormat { get; set; } = "SEG{0:yyyyMMddHHmmss}{1:D6}";
    
    /// <summary>
    /// Gets or sets whether to include timestamps in messages.
    /// </summary>
    public bool IncludeTimestamps { get; set; } = true;
    
    /// <summary>
    /// Gets or sets the timezone for timestamps.
    /// </summary>
    public string DefaultTimeZone { get; set; } = "UTC";
    
    /// <summary>
    /// Gets or sets custom field mappings.
    /// </summary>
    public Dictionary<string, string> CustomFieldMappings { get; set; } = new();
    
    /// <summary>
    /// Gets or sets custom segment definitions.
    /// </summary>
    public Dictionary<string, SegmentDefinition> CustomSegments { get; set; } = new();
}

/// <summary>
/// Validation configuration settings.
/// </summary>
public class ValidationConfiguration : IValidationConfiguration
{
    /// <summary>
    /// Gets or sets the enabled validation types.
    /// </summary>
    public List<ValidationType> EnabledTypes { get; set; } = 
        [ValidationType.Syntax, ValidationType.Semantic, ValidationType.Clinical, ValidationType.Interface];
    
    /// <inheritdoc />
    [JsonIgnore]
    public IEnumerable<ValidationType> EnabledValidationTypes => EnabledTypes;
    
    /// <inheritdoc />
    public int MaxIssues { get; set; } = 1000;
    
    /// <inheritdoc />
    public bool StopOnFirstError { get; set; } = false;
    
    /// <inheritdoc />
    public ValidationSeverity MinimumSeverity { get; set; } = ValidationSeverity.Info;
    
    /// <summary>
    /// Gets or sets whether to perform strict validation.
    /// </summary>
    public bool StrictMode { get; set; } = false;
    
    /// <summary>
    /// Gets or sets whether to validate field lengths.
    /// </summary>
    public bool ValidateFieldLengths { get; set; } = true;
    
    /// <summary>
    /// Gets or sets whether to validate required fields.
    /// </summary>
    public bool ValidateRequiredFields { get; set; } = true;
    
    /// <summary>
    /// Gets or sets whether to validate data types.
    /// </summary>
    public bool ValidateDataTypes { get; set; } = true;
    
    /// <summary>
    /// Gets or sets whether to validate against code tables.
    /// </summary>
    public bool ValidateCodeTables { get; set; } = true;
    
    /// <summary>
    /// Gets or sets whether to perform clinical validation.
    /// </summary>
    public bool EnableClinicalValidation { get; set; } = true;
    
    /// <summary>
    /// Gets or sets the interface-specific validation rules.
    /// </summary>
    public Dictionary<string, object> InterfaceRules { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the clinical validation rules.
    /// </summary>
    public Dictionary<string, object> ClinicalRules { get; set; } = new();
    
    /// <summary>
    /// Gets or sets custom validation rules.
    /// </summary>
    public Dictionary<string, ValidationRule> CustomRules { get; set; } = new();
    
    /// <inheritdoc />
    IDictionary<string, object> IValidationConfiguration.InterfaceRules => 
        InterfaceRules.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    
    /// <inheritdoc />
    IDictionary<string, object> IValidationConfiguration.ClinicalRules => 
        ClinicalRules.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    
    /// <inheritdoc />
    public bool IsValidationTypeEnabled(ValidationType validationType)
    {
        return EnabledTypes.Contains(validationType);
    }
    
    /// <inheritdoc />
    public bool ShouldReportSeverity(ValidationSeverity severity)
    {
        return severity >= MinimumSeverity;
    }
}

/// <summary>
/// Interface configuration settings.
/// </summary>
public class InterfaceConfiguration
{
    /// <summary>
    /// Gets or sets the interface name.
    /// </summary>
    public string Name { get; set; } = "Default";
    
    /// <summary>
    /// Gets or sets the interface description.
    /// </summary>
    public string Description { get; set; } = "";
    
    /// <summary>
    /// Gets or sets the interface version.
    /// </summary>
    public string Version { get; set; } = "1.0";
    
    /// <summary>
    /// Gets or sets the supported message types.
    /// </summary>
    public List<string> SupportedMessageTypes { get; set; } = ["RDE", "ADT", "ACK"];
    
    /// <summary>
    /// Gets or sets the supported trigger events.
    /// </summary>
    public Dictionary<string, List<string>> SupportedTriggerEvents { get; set; } = new()
    {
        ["RDE"] = ["O01"],
        ["ADT"] = ["A01", "A02", "A03", "A04", "A05", "A06", "A07", "A08"],
        ["ACK"] = ["O01", "A01", "A02", "A03", "A04", "A05", "A06", "A07", "A08"]
    };
    
    /// <summary>
    /// Gets or sets the required segments by message type.
    /// </summary>
    public Dictionary<string, List<string>> RequiredSegments { get; set; } = new()
    {
        ["RDE"] = ["MSH", "PID", "ORC", "RXE"],
        ["ADT"] = ["MSH", "PID", "PV1"],
        ["ACK"] = ["MSH", "MSA"]
    };
    
    /// <summary>
    /// Gets or sets the optional segments by message type.
    /// </summary>
    public Dictionary<string, List<string>> OptionalSegments { get; set; } = new()
    {
        ["RDE"] = ["PV1", "NTE", "RXR", "RXC"],
        ["ADT"] = ["NTE", "PV2"],
        ["ACK"] = ["NTE", "ERR"]
    };
    
    /// <summary>
    /// Gets or sets the maximum message size in bytes.
    /// </summary>
    public int MaxMessageSize { get; set; } = 1048576; // 1MB
    
    /// <summary>
    /// Gets or sets the maximum number of segments per message.
    /// </summary>
    public int MaxSegmentsPerMessage { get; set; } = 1000;
    
    /// <summary>
    /// Gets or sets the connection timeout in seconds.
    /// </summary>
    public int ConnectionTimeoutSeconds { get; set; } = 30;
    
    /// <summary>
    /// Gets or sets the retry configuration.
    /// </summary>
    public RetryConfiguration Retry { get; set; } = new();
    
    /// <summary>
    /// Gets or sets custom interface rules.
    /// </summary>
    public Dictionary<string, object> CustomRules { get; set; } = new();
}

/// <summary>
/// Logging configuration settings.
/// </summary>
public class LoggingConfiguration
{
    /// <summary>
    /// Gets or sets the minimum log level.
    /// </summary>
    public LogLevel MinimumLevel { get; set; } = LogLevel.Information;
    
    /// <summary>
    /// Gets or sets whether to log HL7 messages.
    /// </summary>
    public bool LogHL7Messages { get; set; } = true;
    
    /// <summary>
    /// Gets or sets whether to log validation results.
    /// </summary>
    public bool LogValidationResults { get; set; } = true;
    
    /// <summary>
    /// Gets or sets whether to log performance metrics.
    /// </summary>
    public bool LogPerformanceMetrics { get; set; } = false;
    
    /// <summary>
    /// Gets or sets whether to mask sensitive data in logs.
    /// </summary>
    public bool MaskSensitiveData { get; set; } = true;
    
    /// <summary>
    /// Gets or sets the sensitive field patterns to mask.
    /// </summary>
    public List<string> SensitiveFieldPatterns { get; set; } = 
        ["SSN", "PatientId", "MedicalRecordNumber", "CreditCard"];
    
    /// <summary>
    /// Gets or sets the log file path.
    /// </summary>
    public string LogFilePath { get; set; } = "logs/segmint.log";
    
    /// <summary>
    /// Gets or sets the maximum log file size in MB.
    /// </summary>
    public int MaxLogFileSizeMB { get; set; } = 100;
    
    /// <summary>
    /// Gets or sets the number of log files to retain.
    /// </summary>
    public int RetainedLogFileCount { get; set; } = 10;
    
    /// <summary>
    /// Gets or sets structured logging settings.
    /// </summary>
    public StructuredLoggingConfiguration StructuredLogging { get; set; } = new();
}

/// <summary>
/// Performance configuration settings.
/// </summary>
public class PerformanceConfiguration
{
    /// <summary>
    /// Gets or sets whether to enable performance monitoring.
    /// </summary>
    public bool EnablePerformanceMonitoring { get; set; } = false;
    
    /// <summary>
    /// Gets or sets the maximum memory usage in MB.
    /// </summary>
    public int MaxMemoryUsageMB { get; set; } = 512;
    
    /// <summary>
    /// Gets or sets the message processing timeout in seconds.
    /// </summary>
    public int ProcessingTimeoutSeconds { get; set; } = 60;
    
    /// <summary>
    /// Gets or sets the maximum concurrent operations.
    /// </summary>
    public int MaxConcurrentOperations { get; set; } = Environment.ProcessorCount * 2;
    
    /// <summary>
    /// Gets or sets whether to enable caching.
    /// </summary>
    public bool EnableCaching { get; set; } = true;
    
    /// <summary>
    /// Gets or sets the cache expiration time in minutes.
    /// </summary>
    public int CacheExpirationMinutes { get; set; } = 30;
    
    /// <summary>
    /// Gets or sets the maximum cache size in MB.
    /// </summary>
    public int MaxCacheSizeMB { get; set; } = 50;
    
    /// <summary>
    /// Gets or sets batch processing settings.
    /// </summary>
    public BatchProcessingConfiguration BatchProcessing { get; set; } = new();
}

/// <summary>
/// Retry configuration settings.
/// </summary>
public class RetryConfiguration
{
    /// <summary>
    /// Gets or sets the maximum number of retry attempts.
    /// </summary>
    public int MaxAttempts { get; set; } = 3;
    
    /// <summary>
    /// Gets or sets the initial delay between retries in milliseconds.
    /// </summary>
    public int InitialDelayMs { get; set; } = 1000;
    
    /// <summary>
    /// Gets or sets the maximum delay between retries in milliseconds.
    /// </summary>
    public int MaxDelayMs { get; set; } = 30000;
    
    /// <summary>
    /// Gets or sets the backoff multiplier.
    /// </summary>
    public double BackoffMultiplier { get; set; } = 2.0;
    
    /// <summary>
    /// Gets or sets whether to use exponential backoff.
    /// </summary>
    public bool UseExponentialBackoff { get; set; } = true;
    
    /// <summary>
    /// Gets or sets the jitter factor for randomizing delays.
    /// </summary>
    public double JitterFactor { get; set; } = 0.1;
}

/// <summary>
/// Structured logging configuration.
/// </summary>
public class StructuredLoggingConfiguration
{
    /// <summary>
    /// Gets or sets whether structured logging is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;
    
    /// <summary>
    /// Gets or sets the output format (JSON, XML, etc.).
    /// </summary>
    public string OutputFormat { get; set; } = "JSON";
    
    /// <summary>
    /// Gets or sets whether to include request context.
    /// </summary>
    public bool IncludeRequestContext { get; set; } = true;
    
    /// <summary>
    /// Gets or sets whether to include performance metrics.
    /// </summary>
    public bool IncludePerformanceMetrics { get; set; } = false;
    
    /// <summary>
    /// Gets or sets custom properties to include.
    /// </summary>
    public Dictionary<string, string> CustomProperties { get; set; } = new();
}

/// <summary>
/// Batch processing configuration.
/// </summary>
public class BatchProcessingConfiguration
{
    /// <summary>
    /// Gets or sets whether batch processing is enabled.
    /// </summary>
    public bool Enabled { get; set; } = false;
    
    /// <summary>
    /// Gets or sets the batch size.
    /// </summary>
    public int BatchSize { get; set; } = 100;
    
    /// <summary>
    /// Gets or sets the batch timeout in seconds.
    /// </summary>
    public int BatchTimeoutSeconds { get; set; } = 30;
    
    /// <summary>
    /// Gets or sets the maximum queue size.
    /// </summary>
    public int MaxQueueSize { get; set; } = 1000;
    
    /// <summary>
    /// Gets or sets whether to process batches in parallel.
    /// </summary>
    public bool ParallelProcessing { get; set; } = true;
    
    /// <summary>
    /// Gets or sets the degree of parallelism.
    /// </summary>
    public int DegreeOfParallelism { get; set; } = Environment.ProcessorCount;
}

/// <summary>
/// Custom segment definition.
/// </summary>
public class SegmentDefinition
{
    /// <summary>
    /// Gets or sets the segment ID.
    /// </summary>
    public string SegmentId { get; set; } = "";
    
    /// <summary>
    /// Gets or sets the segment name.
    /// </summary>
    public string Name { get; set; } = "";
    
    /// <summary>
    /// Gets or sets the segment description.
    /// </summary>
    public string Description { get; set; } = "";
    
    /// <summary>
    /// Gets or sets the field definitions.
    /// </summary>
    public List<FieldDefinition> Fields { get; set; } = new();
}

/// <summary>
/// Field definition for custom segments.
/// </summary>
public class FieldDefinition
{
    /// <summary>
    /// Gets or sets the field number.
    /// </summary>
    public int FieldNumber { get; set; }
    
    /// <summary>
    /// Gets or sets the field name.
    /// </summary>
    public string Name { get; set; } = "";
    
    /// <summary>
    /// Gets or sets the data type.
    /// </summary>
    public string DataType { get; set; } = "ST";
    
    /// <summary>
    /// Gets or sets whether the field is required.
    /// </summary>
    public bool Required { get; set; } = false;
    
    /// <summary>
    /// Gets or sets the maximum length.
    /// </summary>
    public int? MaxLength { get; set; }
    
    /// <summary>
    /// Gets or sets the allowed values.
    /// </summary>
    public List<string> AllowedValues { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the default value.
    /// </summary>
    public string? DefaultValue { get; set; }
}

/// <summary>
/// Custom validation rule definition.
/// </summary>
public class ValidationRule
{
    /// <summary>
    /// Gets or sets the rule name.
    /// </summary>
    public string Name { get; set; } = "";
    
    /// <summary>
    /// Gets or sets the rule description.
    /// </summary>
    public string Description { get; set; } = "";
    
    /// <summary>
    /// Gets or sets the validation expression.
    /// </summary>
    public string Expression { get; set; } = "";
    
    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    public string ErrorMessage { get; set; } = "";
    
    /// <summary>
    /// Gets or sets the validation severity.
    /// </summary>
    public ValidationSeverity Severity { get; set; } = ValidationSeverity.Error;
    
    /// <summary>
    /// Gets or sets the validation type.
    /// </summary>
    public ValidationType Type { get; set; } = ValidationType.Semantic;
    
    /// <summary>
    /// Gets or sets whether the rule is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;
}

/// <summary>
/// Log level enumeration.
/// </summary>
public enum LogLevel
{
    Trace,
    Debug,
    Information,
    Warning,
    Error,
    Critical,
    None
}