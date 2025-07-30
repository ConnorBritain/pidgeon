// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using Segmint.Core.HL7.Validation;

namespace Segmint.Core.Configuration.Templates;

/// <summary>
/// Provides pre-configured templates for common Segmint scenarios.
/// </summary>
public static class ConfigurationTemplates
{
    /// <summary>
    /// Creates a configuration template optimized for RDE (pharmacy order) scenarios.
    /// </summary>
    /// <returns>A configuration optimized for pharmacy orders.</returns>
    public static SegmintConfiguration CreatePharmacyTemplate()
    {
        return new SegmintConfiguration
        {
            HL7 = new HL7Configuration
            {
                DefaultVersion = "2.3",
                DefaultSendingApplication = "PharmacySystem",
                DefaultReceivingApplication = "HIS",
                DefaultProcessingId = "P",
                AutoGenerateControlId = true,
                IncludeTimestamps = true,
                DefaultTimeZone = "UTC",
                ControlIdFormat = "RX{0:yyyyMMddHHmmss}{1:D6}",
                CustomFieldMappings = new Dictionary<string, string>
                {
                    ["DEA_NUMBER"] = "RXE.22",
                    ["PRESCRIBER_ID"] = "RXE.14",
                    ["PHARMACY_ID"] = "RXE.15",
                    ["NDC_CODE"] = "RXE.2"
                }
            },
            Validation = new ValidationConfiguration
            {
                EnabledTypes = [ValidationType.Syntax, ValidationType.Semantic, ValidationType.Clinical],
                StrictMode = true,
                ValidateRequiredFields = true,
                ValidateDataTypes = true,
                ValidateCodeTables = true,
                EnableClinicalValidation = true,
                CustomRules = new Dictionary<string, ValidationRule>
                {
                    ["ValidDEA"] = new ValidationRule
                    {
                        Name = "Valid DEA Number",
                        Description = "Validates DEA number format and checksum",
                        Expression = "RXE.22 matches '^[A-Z]{2}\\d{7}$'",
                        ErrorMessage = "DEA number must be 2 letters followed by 7 digits",
                        Severity = ValidationSeverity.Error,
                        Type = ValidationType.Clinical
                    },
                    ["ValidNDC"] = new ValidationRule
                    {
                        Name = "Valid NDC Code",
                        Description = "Validates NDC code format",
                        Expression = "RXE.2 matches '^\\d{4,5}-\\d{3,4}-\\d{1,2}$'",
                        ErrorMessage = "NDC code must follow standard format",
                        Severity = ValidationSeverity.Warning,
                        Type = ValidationType.Semantic
                    }
                }
            },
            Interface = new InterfaceConfiguration
            {
                Name = "Pharmacy Interface",
                Description = "Interface for pharmacy order processing",
                SupportedMessageTypes = ["RDE", "ACK"],
                SupportedTriggerEvents = new Dictionary<string, List<string>>
                {
                    ["RDE"] = ["O01"],
                    ["ACK"] = ["O01"]
                },
                RequiredSegments = new Dictionary<string, List<string>>
                {
                    ["RDE"] = ["MSH", "EVN", "PID", "ORC", "RXE"],
                    ["ACK"] = ["MSH", "MSA"]
                },
                OptionalSegments = new Dictionary<string, List<string>>
                {
                    ["RDE"] = ["PV1", "NTE", "RXR", "RXC"],
                    ["ACK"] = ["NTE", "ERR"]
                }
            },
            Logging = new LoggingConfiguration
            {
                MinimumLevel = LogLevel.Information,
                LogHL7Messages = true,
                LogValidationResults = true,
                MaskSensitiveData = true,
                SensitiveFieldPatterns = ["DEA", "SSN", "PatientId", "MedicalRecordNumber"]
            }
        };
    }

    /// <summary>
    /// Creates a configuration template optimized for ADT (admission/discharge/transfer) scenarios.
    /// </summary>
    /// <returns>A configuration optimized for patient management.</returns>
    public static SegmintConfiguration CreatePatientManagementTemplate()
    {
        return new SegmintConfiguration
        {
            HL7 = new HL7Configuration
            {
                DefaultVersion = "2.3",
                DefaultSendingApplication = "HIS",
                DefaultReceivingApplication = "EMR",
                DefaultProcessingId = "P",
                AutoGenerateControlId = true,
                IncludeTimestamps = true,
                DefaultTimeZone = "America/New_York",
                ControlIdFormat = "ADT{0:yyyyMMddHHmmss}{1:D6}",
                CustomFieldMappings = new Dictionary<string, string>
                {
                    ["MRN"] = "PID.3",
                    ["ACCOUNT_NUMBER"] = "PID.18",
                    ["VISIT_NUMBER"] = "PV1.19",
                    ["ATTENDING_PHYSICIAN"] = "PV1.7"
                }
            },
            Validation = new ValidationConfiguration
            {
                EnabledTypes = [ValidationType.Syntax, ValidationType.Semantic, ValidationType.Interface],
                StrictMode = false,
                ValidateRequiredFields = true,
                ValidateDataTypes = true,
                ValidateCodeTables = false,
                EnableClinicalValidation = false,
                CustomRules = new Dictionary<string, ValidationRule>
                {
                    ["ValidMRN"] = new ValidationRule
                    {
                        Name = "Valid Medical Record Number",
                        Description = "Validates MRN format",
                        Expression = "PID.3 not empty and length >= 6",
                        ErrorMessage = "Medical Record Number must be at least 6 characters",
                        Severity = ValidationSeverity.Error,
                        Type = ValidationType.Semantic
                    },
                    ["ValidPatientClass"] = new ValidationRule
                    {
                        Name = "Valid Patient Class",
                        Description = "Validates patient class values",
                        Expression = "PV1.2 in ['E', 'I', 'O', 'P', 'R', 'B', 'N']",
                        ErrorMessage = "Patient class must be E, I, O, P, R, B, or N",
                        Severity = ValidationSeverity.Warning,
                        Type = ValidationType.Semantic
                    }
                }
            },
            Interface = new InterfaceConfiguration
            {
                Name = "Patient Management Interface",
                Description = "Interface for patient admission, discharge, and transfer events",
                SupportedMessageTypes = ["ADT", "ACK"],
                SupportedTriggerEvents = new Dictionary<string, List<string>>
                {
                    ["ADT"] = ["A01", "A02", "A03", "A04", "A05", "A06", "A07", "A08"],
                    ["ACK"] = ["A01", "A02", "A03", "A04", "A05", "A06", "A07", "A08"]
                },
                RequiredSegments = new Dictionary<string, List<string>>
                {
                    ["ADT"] = ["MSH", "EVN", "PID", "PV1"],
                    ["ACK"] = ["MSH", "MSA"]
                },
                OptionalSegments = new Dictionary<string, List<string>>
                {
                    ["ADT"] = ["NTE", "PV2", "OBX", "AL1", "DG1"],
                    ["ACK"] = ["NTE", "ERR"]
                }
            },
            Logging = new LoggingConfiguration
            {
                MinimumLevel = LogLevel.Information,
                LogHL7Messages = true,
                LogValidationResults = true,
                MaskSensitiveData = true,
                SensitiveFieldPatterns = ["SSN", "PatientId", "MedicalRecordNumber", "CreditCard", "DateOfBirth"]
            }
        };
    }

    /// <summary>
    /// Creates a configuration template for development and testing scenarios.
    /// </summary>
    /// <returns>A configuration optimized for development.</returns>
    public static SegmintConfiguration CreateDevelopmentTemplate()
    {
        return new SegmintConfiguration
        {
            HL7 = new HL7Configuration
            {
                DefaultVersion = "2.3",
                DefaultSendingApplication = "Segmint-Dev",
                DefaultReceivingApplication = "TestReceiver",
                DefaultProcessingId = "T", // Test mode
                AutoGenerateControlId = true,
                IncludeTimestamps = true,
                DefaultTimeZone = "UTC",
                ControlIdFormat = "DEV{0:yyyyMMddHHmmss}{1:D6}"
            },
            Validation = new ValidationConfiguration
            {
                EnabledTypes = [ValidationType.Syntax, ValidationType.Semantic],
                StrictMode = false,
                ValidateRequiredFields = false,
                ValidateDataTypes = true,
                ValidateCodeTables = false,
                EnableClinicalValidation = false
            },
            Interface = new InterfaceConfiguration
            {
                Name = "Development Interface",
                Description = "Flexible interface for development and testing",
                SupportedMessageTypes = ["RDE", "ADT", "ACK"],
                SupportedTriggerEvents = new Dictionary<string, List<string>>
                {
                    ["RDE"] = ["O01"],
                    ["ADT"] = ["A01", "A02", "A03", "A04", "A08"],
                    ["ACK"] = ["O01", "A01", "A02", "A03", "A04", "A08"]
                },
                MaxMessageSize = 2097152, // 2MB for large test messages
                MaxSegmentsPerMessage = 2000
            },
            Logging = new LoggingConfiguration
            {
                MinimumLevel = LogLevel.Debug,
                LogHL7Messages = true,
                LogValidationResults = true,
                LogPerformanceMetrics = true,
                MaskSensitiveData = false, // For development testing
                LogFilePath = "logs/segmint-dev.log"
            },
            Performance = new PerformanceConfiguration
            {
                EnablePerformanceMonitoring = true,
                MaxMemoryUsageMB = 1024,
                ProcessingTimeoutSeconds = 120,
                EnableCaching = false // Disable for consistent testing
            }
        };
    }

    /// <summary>
    /// Creates a configuration template for high-volume production scenarios.
    /// </summary>
    /// <returns>A configuration optimized for production performance.</returns>
    public static SegmintConfiguration CreateProductionTemplate()
    {
        return new SegmintConfiguration
        {
            HL7 = new HL7Configuration
            {
                DefaultVersion = "2.3",
                DefaultSendingApplication = "Segmint-Prod",
                DefaultProcessingId = "P",
                AutoGenerateControlId = true,
                IncludeTimestamps = true,
                DefaultTimeZone = "UTC"
            },
            Validation = new ValidationConfiguration
            {
                EnabledTypes = [ValidationType.Syntax, ValidationType.Semantic, ValidationType.Clinical, ValidationType.Interface],
                StrictMode = true,
                ValidateRequiredFields = true,
                ValidateDataTypes = true,
                ValidateCodeTables = true,
                EnableClinicalValidation = true,
                MaxIssues = 100, // Limit for performance
                StopOnFirstError = false
            },
            Interface = new InterfaceConfiguration
            {
                Name = "Production Interface",
                Description = "High-performance production interface",
                ConnectionTimeoutSeconds = 60,
                Retry = new RetryConfiguration
                {
                    MaxAttempts = 5,
                    InitialDelayMs = 2000,
                    MaxDelayMs = 60000,
                    UseExponentialBackoff = true,
                    BackoffMultiplier = 2.0
                }
            },
            Logging = new LoggingConfiguration
            {
                MinimumLevel = LogLevel.Warning,
                LogHL7Messages = false, // Disable for performance
                LogValidationResults = true,
                LogPerformanceMetrics = true,
                MaskSensitiveData = true,
                MaxLogFileSizeMB = 500,
                RetainedLogFileCount = 30
            },
            Performance = new PerformanceConfiguration
            {
                EnablePerformanceMonitoring = true,
                MaxMemoryUsageMB = 2048,
                ProcessingTimeoutSeconds = 30,
                MaxConcurrentOperations = Environment.ProcessorCount * 4,
                EnableCaching = true,
                CacheExpirationMinutes = 60,
                MaxCacheSizeMB = 256,
                BatchProcessing = new BatchProcessingConfiguration
                {
                    Enabled = true,
                    BatchSize = 500,
                    BatchTimeoutSeconds = 60,
                    MaxQueueSize = 5000,
                    ParallelProcessing = true,
                    DegreeOfParallelism = Environment.ProcessorCount
                }
            }
        };
    }

    /// <summary>
    /// Creates a configuration template for validation-focused scenarios.
    /// </summary>
    /// <returns>A configuration optimized for comprehensive validation.</returns>
    public static SegmintConfiguration CreateValidationTemplate()
    {
        return new SegmintConfiguration
        {
            HL7 = new HL7Configuration
            {
                DefaultVersion = "2.3",
                DefaultSendingApplication = "ValidationTool",
                DefaultProcessingId = "T",
                AutoGenerateControlId = true,
                IncludeTimestamps = true
            },
            Validation = new ValidationConfiguration
            {
                EnabledTypes = [ValidationType.Syntax, ValidationType.Semantic, ValidationType.Clinical, ValidationType.Interface],
                StrictMode = true,
                ValidateRequiredFields = true,
                ValidateDataTypes = true,
                ValidateCodeTables = true,
                EnableClinicalValidation = true,
                MaxIssues = 10000, // Allow many issues for comprehensive reporting
                StopOnFirstError = false,
                MinimumSeverity = ValidationSeverity.Info
            },
            Interface = new InterfaceConfiguration
            {
                Name = "Validation Interface",
                Description = "Comprehensive validation interface for quality assurance"
            },
            Logging = new LoggingConfiguration
            {
                MinimumLevel = LogLevel.Debug,
                LogHL7Messages = true,
                LogValidationResults = true,
                LogPerformanceMetrics = false,
                MaskSensitiveData = true
            },
            Performance = new PerformanceConfiguration
            {
                EnablePerformanceMonitoring = false,
                ProcessingTimeoutSeconds = 300, // Extended timeout for thorough validation
                EnableCaching = false // Disable for consistent validation results
            }
        };
    }

    /// <summary>
    /// Gets all available template names.
    /// </summary>
    /// <returns>List of template names.</returns>
    public static List<string> GetAvailableTemplates()
    {
        return
        [
            "pharmacy",
            "patient-management", 
            "development",
            "production",
            "validation"
        ];
    }

    /// <summary>
    /// Creates a configuration from the specified template name.
    /// </summary>
    /// <param name="templateName">The template name.</param>
    /// <returns>The configuration for the specified template.</returns>
    /// <exception cref="ArgumentException">Thrown when template name is not recognized.</exception>
    public static SegmintConfiguration CreateFromTemplate(string templateName)
    {
        return templateName.ToLowerInvariant() switch
        {
            "pharmacy" => CreatePharmacyTemplate(),
            "patient-management" => CreatePatientManagementTemplate(),
            "development" => CreateDevelopmentTemplate(),
            "production" => CreateProductionTemplate(),
            "validation" => CreateValidationTemplate(),
            _ => throw new ArgumentException($"Unknown template: {templateName}. Available templates: {string.Join(", ", GetAvailableTemplates())}")
        };
    }
}