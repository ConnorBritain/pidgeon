// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using Segmint.Core.HL7.Validation;

namespace Segmint.Core.Configuration.Templates;

/// <summary>
/// Provides healthcare scenario-specific configuration templates for specialized workflows.
/// </summary>
public static class HealthcareScenarioTemplates
{
    /// <summary>
    /// Creates a configuration template optimized for ORM (Order Management) scenarios.
    /// Critical for MVP - supports lab orders, radiology orders, and general order management.
    /// </summary>
    /// <returns>A configuration optimized for order management workflows.</returns>
    public static SegmintConfiguration CreateOrderManagementTemplate()
    {
        return new SegmintConfiguration
        {
            HL7 = new HL7Configuration
            {
                DefaultVersion = "2.3",
                DefaultSendingApplication = "OrderManagementSystem",
                DefaultReceivingApplication = "LIS", // Laboratory Information System
                DefaultProcessingId = "P",
                AutoGenerateControlId = true,
                IncludeTimestamps = true,
                DefaultTimeZone = "America/New_York",
                ControlIdFormat = "ORD{0:yyyyMMddHHmmss}{1:D6}",
                CustomFieldMappings = new Dictionary<string, string>
                {
                    ["ORDER_NUMBER"] = "ORC.2",
                    ["PLACER_ORDER_NUMBER"] = "ORC.2", 
                    ["FILLER_ORDER_NUMBER"] = "ORC.3",
                    ["ORDERING_PHYSICIAN"] = "ORC.12",
                    ["ENTERED_BY"] = "ORC.10",
                    ["VERIFIED_BY"] = "ORC.11",
                    ["ORDER_PRIORITY"] = "ORC.7",
                    ["ORDER_STATUS"] = "ORC.1",
                    ["TEST_CODE"] = "OBR.4",
                    ["SPECIMEN_SOURCE"] = "OBR.15",
                    ["COLLECTION_DATE"] = "OBR.7"
                }
            },
            Validation = new ValidationConfiguration
            {
                EnabledTypes = [ValidationType.Syntax, ValidationType.Semantic, ValidationType.Clinical, ValidationType.Interface],
                StrictMode = true,
                ValidateRequiredFields = true,
                ValidateDataTypes = true,
                ValidateCodeTables = true,
                EnableClinicalValidation = true,
                CustomRules = new Dictionary<string, ValidationRule>
                {
                    ["ValidOrderControl"] = new ValidationRule
                    {
                        Name = "Valid Order Control Code",
                        Description = "Validates ORC.1 order control codes",
                        Expression = "ORC.1 in ['NW', 'OK', 'UA', 'CA', 'DC', 'CR', 'SC', 'SN', 'XO', 'XR', 'DE', 'HD', 'HR']",
                        ErrorMessage = "Order control code must be a valid HL7 value (NW, OK, UA, CA, DC, etc.)",
                        Severity = ValidationSeverity.Error,
                        Type = ValidationType.Semantic
                    },
                    ["ValidOrderPriority"] = new ValidationRule
                    {
                        Name = "Valid Order Priority",
                        Description = "Validates order priority codes",
                        Expression = "ORC.7 in ['S', 'A', 'R', 'P', 'C', 'T'] or empty",
                        ErrorMessage = "Order priority must be S(STAT), A(ASAP), R(Routine), P(Preop), C(Callback), or T(Timing Critical)",
                        Severity = ValidationSeverity.Warning,
                        Type = ValidationType.Semantic
                    },
                    ["ValidTestCode"] = new ValidationRule
                    {
                        Name = "Valid Test Code",
                        Description = "Ensures test codes are present for orders",
                        Expression = "OBR.4 not empty",
                        ErrorMessage = "Test code (OBR.4) is required for all orders",
                        Severity = ValidationSeverity.Error,
                        Type = ValidationType.Clinical
                    },
                    ["ValidOrderingPhysician"] = new ValidationRule
                    {
                        Name = "Valid Ordering Physician",
                        Description = "Validates ordering physician is specified",
                        Expression = "ORC.12 not empty and contains '^'",
                        ErrorMessage = "Ordering physician (ORC.12) must be specified with proper name format",
                        Severity = ValidationSeverity.Error,
                        Type = ValidationType.Clinical
                    }
                }
            },
            Interface = new InterfaceConfiguration
            {
                Name = "Order Management Interface",
                Description = "Interface for lab orders, radiology orders, and general order management",
                SupportedMessageTypes = ["ORM", "ORU", "ACK"],
                SupportedTriggerEvents = new Dictionary<string, List<string>>
                {
                    ["ORM"] = ["O01"], // Order message
                    ["ORU"] = ["R01"], // Observation result
                    ["ACK"] = ["O01", "R01"] // Acknowledgments
                },
                RequiredSegments = new Dictionary<string, List<string>>
                {
                    ["ORM"] = ["MSH", "PID", "ORC", "OBR"],
                    ["ORU"] = ["MSH", "PID", "ORC", "OBR", "OBX"],
                    ["ACK"] = ["MSH", "MSA"]
                },
                OptionalSegments = new Dictionary<string, List<string>>
                {
                    ["ORM"] = ["PV1", "NTE", "DG1", "OBX"],
                    ["ORU"] = ["PV1", "NTE", "CTI"],
                    ["ACK"] = ["NTE", "ERR"]
                }
            },
            Logging = new LoggingConfiguration
            {
                MinimumLevel = LogLevel.Information,
                LogHL7Messages = true,
                LogValidationResults = true,
                MaskSensitiveData = true,
                SensitiveFieldPatterns = ["SSN", "PatientId", "MedicalRecordNumber", "OrderNumber", "SpecimenId"]
            }
        };
    }

    /// <summary>
    /// Creates a configuration template optimized for Laboratory Information System (LIS) integration.
    /// </summary>
    /// <returns>A configuration optimized for lab workflows.</returns>
    public static SegmintConfiguration CreateLaboratoryTemplate()
    {
        return new SegmintConfiguration
        {
            HL7 = new HL7Configuration
            {
                DefaultVersion = "2.3",
                DefaultSendingApplication = "LIS",
                DefaultReceivingApplication = "HIS",
                DefaultProcessingId = "P",
                AutoGenerateControlId = true,
                IncludeTimestamps = true,
                DefaultTimeZone = "UTC",
                ControlIdFormat = "LAB{0:yyyyMMddHHmmss}{1:D6}",
                CustomFieldMappings = new Dictionary<string, string>
                {
                    ["SPECIMEN_ID"] = "SPM.2",
                    ["ACCESSION_NUMBER"] = "OBR.3", 
                    ["TEST_CODE"] = "OBR.4",
                    ["RESULT_VALUE"] = "OBX.5",
                    ["REFERENCE_RANGE"] = "OBX.7",
                    ["ABNORMAL_FLAGS"] = "OBX.8",
                    ["RESULT_STATUS"] = "OBX.11",
                    ["OBSERVATION_DATE"] = "OBX.14",
                    ["PERFORMING_LAB"] = "OBX.15"
                }
            },
            Validation = new ValidationConfiguration
            {
                EnabledTypes = [ValidationType.Syntax, ValidationType.Semantic, ValidationType.Clinical, ValidationType.Interface],
                StrictMode = true,
                ValidateRequiredFields = true,
                ValidateDataTypes = true,
                ValidateCodeTables = true,
                EnableClinicalValidation = true,
                CustomRules = new Dictionary<string, ValidationRule>
                {
                    ["ValidResultStatus"] = new ValidationRule
                    {
                        Name = "Valid Result Status",
                        Description = "Validates observation result status",
                        Expression = "OBX.11 in ['C', 'D', 'F', 'I', 'N', 'O', 'P', 'R', 'S', 'U', 'W', 'X']",
                        ErrorMessage = "Result status must be valid (C=Corrected, D=Deleted, F=Final, etc.)",
                        Severity = ValidationSeverity.Error,
                        Type = ValidationType.Clinical
                    },
                    ["ValidValueType"] = new ValidationRule
                    {
                        Name = "Valid Value Type",
                        Description = "Validates observation value type",
                        Expression = "OBX.2 in ['CE', 'CWE', 'ST', 'TX', 'FT', 'NM', 'SN', 'DT', 'TM', 'TS']",
                        ErrorMessage = "Value type must be valid HL7 data type",
                        Severity = ValidationSeverity.Error,
                        Type = ValidationType.Semantic
                    },
                    ["ValidNumericResult"] = new ValidationRule
                    {
                        Name = "Valid Numeric Result",
                        Description = "Validates numeric results have proper format",
                        Expression = "OBX.2 = 'NM' implies OBX.5 matches '^[0-9]*\\.?[0-9]+$'",
                        ErrorMessage = "Numeric results must contain valid numbers",
                        Severity = ValidationSeverity.Warning,
                        Type = ValidationType.Clinical
                    }
                }
            },
            Interface = new InterfaceConfiguration
            {
                Name = "Laboratory Interface",
                Description = "Interface for laboratory result reporting and order management",
                SupportedMessageTypes = ["ORU", "ORM", "ACK"],
                SupportedTriggerEvents = new Dictionary<string, List<string>>
                {
                    ["ORU"] = ["R01", "R03"], // Unsolicited transmission of results, Unsolicited transmission of requested information
                    ["ORM"] = ["O01"], // Order message
                    ["ACK"] = ["R01", "R03", "O01"]
                },
                RequiredSegments = new Dictionary<string, List<string>>
                {
                    ["ORU"] = ["MSH", "PID", "ORC", "OBR", "OBX"],
                    ["ORM"] = ["MSH", "PID", "ORC", "OBR"],
                    ["ACK"] = ["MSH", "MSA"]
                },
                OptionalSegments = new Dictionary<string, List<string>>
                {
                    ["ORU"] = ["PV1", "NTE", "CTI", "SPM"],
                    ["ORM"] = ["PV1", "NTE", "DG1"],
                    ["ACK"] = ["NTE", "ERR"]
                }
            },
            Logging = new LoggingConfiguration
            {
                MinimumLevel = LogLevel.Information,
                LogHL7Messages = true,
                LogValidationResults = true,
                MaskSensitiveData = true,
                SensitiveFieldPatterns = ["SSN", "PatientId", "MedicalRecordNumber", "SpecimenId", "AccessionNumber"]
            }
        };
    }

    /// <summary>
    /// Creates a configuration template optimized for Radiology Information System (RIS) integration.
    /// </summary>
    /// <returns>A configuration optimized for radiology workflows.</returns>
    public static SegmintConfiguration CreateRadiologyTemplate()
    {
        return new SegmintConfiguration
        {
            HL7 = new HL7Configuration
            {
                DefaultVersion = "2.3",
                DefaultSendingApplication = "RIS",
                DefaultReceivingApplication = "PACS",
                DefaultProcessingId = "P",
                AutoGenerateControlId = true,
                IncludeTimestamps = true,
                DefaultTimeZone = "America/New_York",
                ControlIdFormat = "RAD{0:yyyyMMddHHmmss}{1:D6}",
                CustomFieldMappings = new Dictionary<string, string>
                {
                    ["STUDY_INSTANCE_UID"] = "OBR.3",
                    ["MODALITY"] = "OBR.24",
                    ["BODY_PART"] = "OBR.15",
                    ["RADIOLOGIST"] = "OBR.32",
                    ["TECHNOLOGIST"] = "OBR.34",
                    ["STUDY_DATE"] = "OBR.7",
                    ["ACCESSION_NUMBER"] = "OBR.18",
                    ["PROCEDURE_CODE"] = "OBR.4"
                }
            },
            Validation = new ValidationConfiguration
            {
                EnabledTypes = [ValidationType.Syntax, ValidationType.Semantic, ValidationType.Clinical, ValidationType.Interface],
                StrictMode = true,
                ValidateRequiredFields = true,
                ValidateDataTypes = true,
                ValidateCodeTables = true,
                EnableClinicalValidation = true,
                CustomRules = new Dictionary<string, ValidationRule>
                {
                    ["ValidModality"] = new ValidationRule
                    {
                        Name = "Valid Radiology Modality",
                        Description = "Validates DICOM modality codes",
                        Expression = "OBR.24 in ['CT', 'MR', 'XR', 'US', 'NM', 'RF', 'DX', 'CR', 'DR', 'MG', 'PT', 'OT']",
                        ErrorMessage = "Modality must be valid DICOM code (CT, MR, XR, US, etc.)",
                        Severity = ValidationSeverity.Error,
                        Type = ValidationType.Clinical
                    },
                    ["ValidStudyInstanceUID"] = new ValidationRule
                    {
                        Name = "Valid Study Instance UID",
                        Description = "Validates DICOM Study Instance UID format",
                        Expression = "OBR.3 matches '^[0-9\\.]+$' and length <= 64",
                        ErrorMessage = "Study Instance UID must be valid DICOM UID format",
                        Severity = ValidationSeverity.Warning,
                        Type = ValidationType.Semantic
                    },
                    ["ValidRadiologist"] = new ValidationRule
                    {
                        Name = "Valid Radiologist",
                        Description = "Ensures radiologist is specified for finalized studies",
                        Expression = "OBR.25 = 'F' implies OBR.32 not empty",
                        ErrorMessage = "Radiologist must be specified for final results",
                        Severity = ValidationSeverity.Error,
                        Type = ValidationType.Clinical
                    }
                }
            },
            Interface = new InterfaceConfiguration
            {
                Name = "Radiology Interface",
                Description = "Interface for radiology orders and result reporting",
                SupportedMessageTypes = ["ORM", "ORU", "ACK"],
                SupportedTriggerEvents = new Dictionary<string, List<string>>
                {
                    ["ORM"] = ["O01"], // Order message
                    ["ORU"] = ["R01"], // Result message
                    ["ACK"] = ["O01", "R01"]
                },
                RequiredSegments = new Dictionary<string, List<string>>
                {
                    ["ORM"] = ["MSH", "PID", "ORC", "OBR"],
                    ["ORU"] = ["MSH", "PID", "ORC", "OBR", "OBX"],
                    ["ACK"] = ["MSH", "MSA"]
                },
                OptionalSegments = new Dictionary<string, List<string>>
                {
                    ["ORM"] = ["PV1", "NTE", "DG1", "ZDS"], // ZDS = custom DICOM segment
                    ["ORU"] = ["PV1", "NTE", "CTI"],
                    ["ACK"] = ["NTE", "ERR"]
                }
            },
            Logging = new LoggingConfiguration
            {
                MinimumLevel = LogLevel.Information,
                LogHL7Messages = true,
                LogValidationResults = true,
                MaskSensitiveData = true,
                SensitiveFieldPatterns = ["SSN", "PatientId", "MedicalRecordNumber", "StudyInstanceUID", "AccessionNumber"]
            }
        };
    }

    /// <summary>
    /// Creates a configuration template optimized for Emergency Department workflows.
    /// </summary>
    /// <returns>A configuration optimized for emergency department operations.</returns>
    public static SegmintConfiguration CreateEmergencyDepartmentTemplate()
    {
        return new SegmintConfiguration
        {
            HL7 = new HL7Configuration
            {
                DefaultVersion = "2.3",
                DefaultSendingApplication = "ED_System",
                DefaultReceivingApplication = "HIS",
                DefaultProcessingId = "P",
                AutoGenerateControlId = true,
                IncludeTimestamps = true,
                DefaultTimeZone = "America/New_York",
                ControlIdFormat = "ED{0:yyyyMMddHHmmss}{1:D6}",
                CustomFieldMappings = new Dictionary<string, string>
                {
                    ["TRIAGE_LEVEL"] = "PV1.10",
                    ["CHIEF_COMPLAINT"] = "DG1.3",
                    ["ACUITY_LEVEL"] = "PV2.25",
                    ["AMBULANCE_ID"] = "PV2.26",
                    ["INJURY_CODE"] = "DG1.4",
                    ["ED_DISPOSITION"] = "PV1.36"
                }
            },
            Validation = new ValidationConfiguration
            {
                EnabledTypes = [ValidationType.Syntax, ValidationType.Semantic, ValidationType.Clinical, ValidationType.Interface],
                StrictMode = false, // More flexible for emergency situations
                ValidateRequiredFields = true,
                ValidateDataTypes = true,
                ValidateCodeTables = false, // Flexible for emergency coding
                EnableClinicalValidation = true,
                CustomRules = new Dictionary<string, ValidationRule>
                {
                    ["ValidTriageLevel"] = new ValidationRule
                    {
                        Name = "Valid Triage Level",
                        Description = "Validates emergency triage levels",
                        Expression = "PV1.10 in ['1', '2', '3', '4', '5'] or empty",
                        ErrorMessage = "Triage level must be 1(Critical) to 5(Non-urgent)",
                        Severity = ValidationSeverity.Warning,
                        Type = ValidationType.Clinical
                    },
                    ["ValidPatientClass"] = new ValidationRule
                    {
                        Name = "Valid ED Patient Class",
                        Description = "Validates patient class for ED",
                        Expression = "PV1.2 in ['E', 'O', 'I'] or empty",
                        ErrorMessage = "ED patient class should be E(Emergency), O(Outpatient), or I(Inpatient)",
                        Severity = ValidationSeverity.Warning,
                        Type = ValidationType.Semantic
                    }
                }
            },
            Interface = new InterfaceConfiguration
            {
                Name = "Emergency Department Interface",
                Description = "High-priority interface for emergency department operations",
                SupportedMessageTypes = ["ADT", "ORM", "ORU", "ACK"],
                SupportedTriggerEvents = new Dictionary<string, List<string>>
                {
                    ["ADT"] = ["A01", "A02", "A03", "A04", "A08"], // Admission, transfer, discharge, registration, update
                    ["ORM"] = ["O01"], // Orders (labs, radiology, etc.)
                    ["ORU"] = ["R01"], // Results
                    ["ACK"] = ["A01", "A02", "A03", "A04", "A08", "O01", "R01"]
                },
                RequiredSegments = new Dictionary<string, List<string>>
                {
                    ["ADT"] = ["MSH", "EVN", "PID", "PV1"],
                    ["ORM"] = ["MSH", "PID", "ORC", "OBR"],
                    ["ORU"] = ["MSH", "PID", "ORC", "OBR", "OBX"],
                    ["ACK"] = ["MSH", "MSA"]
                },
                OptionalSegments = new Dictionary<string, List<string>>
                {
                    ["ADT"] = ["NTE", "PV2", "OBX", "AL1", "DG1", "GT1", "IN1"],
                    ["ORM"] = ["PV1", "NTE", "DG1"],
                    ["ORU"] = ["PV1", "NTE", "CTI"],
                    ["ACK"] = ["NTE", "ERR"]
                },
                ConnectionTimeoutSeconds = 10, // Fast timeouts for ED urgency
                Retry = new RetryConfiguration
                {
                    MaxAttempts = 3,
                    InitialDelayMs = 500,
                    MaxDelayMs = 5000,
                    UseExponentialBackoff = true
                }
            },
            Logging = new LoggingConfiguration
            {
                MinimumLevel = LogLevel.Information,
                LogHL7Messages = true,
                LogValidationResults = true,
                MaskSensitiveData = true,
                SensitiveFieldPatterns = ["SSN", "PatientId", "MedicalRecordNumber", "CreditCard"]
            },
            Performance = new PerformanceConfiguration
            {
                EnablePerformanceMonitoring = true,
                ProcessingTimeoutSeconds = 15, // Fast processing for ED
                MaxConcurrentOperations = Environment.ProcessorCount * 3,
                EnableCaching = true,
                CacheExpirationMinutes = 10 // Shorter cache for dynamic ED data
            }
        };
    }

    /// <summary>
    /// Creates a configuration template optimized for Surgical/Operating Room workflows.
    /// </summary>
    /// <returns>A configuration optimized for OR operations.</returns>
    public static SegmintConfiguration CreateSurgicalTemplate()
    {
        return new SegmintConfiguration
        {
            HL7 = new HL7Configuration
            {
                DefaultVersion = "2.3",
                DefaultSendingApplication = "OR_System",
                DefaultReceivingApplication = "HIS",
                DefaultProcessingId = "P",
                AutoGenerateControlId = true,
                IncludeTimestamps = true,
                DefaultTimeZone = "America/New_York",
                ControlIdFormat = "OR{0:yyyyMMddHHmmss}{1:D6}",
                CustomFieldMappings = new Dictionary<string, string>
                {
                    ["SURGERY_DATE"] = "SCH.11",
                    ["SURGEON"] = "AIP.3",
                    ["PROCEDURE_CODE"] = "SCH.7",
                    ["OR_ROOM"] = "SCH.8",
                    ["ANESTHESIOLOGIST"] = "AIP.3",
                    ["SCHEDULED_DURATION"] = "SCH.9",
                    ["ACTUAL_START_TIME"] = "SCH.11",
                    ["ACTUAL_END_TIME"] = "SCH.12"
                }
            },
            Validation = new ValidationConfiguration
            {
                EnabledTypes = [ValidationType.Syntax, ValidationType.Semantic, ValidationType.Clinical, ValidationType.Interface],
                StrictMode = true,
                ValidateRequiredFields = true,
                ValidateDataTypes = true,
                ValidateCodeTables = true,
                EnableClinicalValidation = true,
                CustomRules = new Dictionary<string, ValidationRule>
                {
                    ["ValidSurgeon"] = new ValidationRule
                    {
                        Name = "Valid Surgeon",
                        Description = "Ensures primary surgeon is specified",
                        Expression = "AIP.3 not empty and AIP.4 = 'PRF'",
                        ErrorMessage = "Primary surgeon must be specified for surgical procedures",
                        Severity = ValidationSeverity.Error,
                        Type = ValidationType.Clinical
                    },
                    ["ValidProcedureCode"] = new ValidationRule
                    {
                        Name = "Valid Procedure Code",
                        Description = "Validates surgical procedure codes",
                        Expression = "SCH.7 not empty",
                        ErrorMessage = "Procedure code is required for surgical scheduling",
                        Severity = ValidationSeverity.Error,
                        Type = ValidationType.Clinical
                    }
                }
            },
            Interface = new InterfaceConfiguration
            {
                Name = "Surgical Interface",
                Description = "Interface for surgical scheduling and OR management",
                SupportedMessageTypes = ["SIU", "ACK"],
                SupportedTriggerEvents = new Dictionary<string, List<string>>
                {
                    ["SIU"] = ["S12", "S13", "S14", "S15", "S17"], // Schedule events
                    ["ACK"] = ["S12", "S13", "S14", "S15", "S17"]
                },
                RequiredSegments = new Dictionary<string, List<string>>
                {
                    ["SIU"] = ["MSH", "SCH", "PID", "RGS", "AIG", "AIP"],
                    ["ACK"] = ["MSH", "MSA"]
                },
                OptionalSegments = new Dictionary<string, List<string>>
                {
                    ["SIU"] = ["NTE", "PV1", "DG1", "OBX"],
                    ["ACK"] = ["NTE", "ERR"]
                }
            },
            Logging = new LoggingConfiguration
            {
                MinimumLevel = LogLevel.Information,
                LogHL7Messages = true,
                LogValidationResults = true,
                MaskSensitiveData = true,
                SensitiveFieldPatterns = ["SSN", "PatientId", "MedicalRecordNumber", "SurgeonId"]
            }
        };
    }

    /// <summary>
    /// Gets all available healthcare scenario template names.
    /// </summary>
    /// <returns>List of healthcare scenario template names.</returns>
    public static List<string> GetAvailableHealthcareTemplates()
    {
        return
        [
            "order-management",    // MVP Priority - ORM workflows
            "laboratory",          // Lab orders and results
            "radiology",          // Radiology orders and results
            "emergency-department", // ED workflows
            "surgical"            // OR scheduling and management
        ];
    }

    /// <summary>
    /// Creates a configuration from the specified healthcare scenario template name.
    /// </summary>
    /// <param name="templateName">The healthcare scenario template name.</param>
    /// <returns>The configuration for the specified template.</returns>
    /// <exception cref="ArgumentException">Thrown when template name is not recognized.</exception>
    public static SegmintConfiguration CreateFromHealthcareTemplate(string templateName)
    {
        return templateName.ToLowerInvariant() switch
        {
            "order-management" => CreateOrderManagementTemplate(),
            "laboratory" => CreateLaboratoryTemplate(),
            "radiology" => CreateRadiologyTemplate(),
            "emergency-department" => CreateEmergencyDepartmentTemplate(),
            "surgical" => CreateSurgicalTemplate(),
            _ => throw new ArgumentException($"Unknown healthcare template: {templateName}. Available templates: {string.Join(", ", GetAvailableHealthcareTemplates())}")
        };
    }
}