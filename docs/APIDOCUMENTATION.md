# Segmint HL7 Generator - Complete API Documentation

## Overview

This document provides comprehensive API documentation for the Segmint HL7 Generator platform. It consolidates all API references and provides detailed documentation for every component, service, and interface in the system.

**Purpose**: To provide developers, testers, and AI agents with complete API specifications to enable effective development, testing, and troubleshooting of HL7 message processing workflows.

**Last Updated**: 2025-07-18

---

## Table of Contents

1. [Architecture Overview](#architecture-overview)
2. [Core Message API](#core-message-api)
3. [Data Generation API](#data-generation-api)
4. [Service Interfaces](#service-interfaces)
5. [Validation Framework API](#validation-framework-api)
6. [Configuration Management API](#configuration-management-api)
7. [Workflow Templates API](#workflow-templates-api)
8. [CLI Command API](#cli-command-api)
9. [Error Handling & Codes](#error-handling--codes)
10. [Common Usage Patterns](#common-usage-patterns)
11. [Testing Guidelines](#testing-guidelines)

---

## Architecture Overview

### Component Structure

```
Segmint HL7 Generator
├── CLI (Command Line Interface)
│   ├── Commands (Generate, Validate, Analyze, Config)
│   ├── Services (Async service implementations)
│   └── Output Handlers
├── Core Library
│   ├── Standards (HL7 v2.3, FHIR R4, NCPDP)
│   ├── DataGeneration (Synthetic data services)
│   ├── Validation (Multi-level validation engine)
│   ├── Configuration (Inference and management)
│   └── Performance (Caching and optimization)
└── GUI (WPF Application - In Development)
```

### Key Design Patterns

- **Hybrid Template-Builder Pattern**: Combines template workflows with programmatic builders
- **Async-First Architecture**: All service interfaces are async for scalability
- **Plugin Architecture**: Extensible validation and generation components
- **Multi-Standard Support**: Designed for HL7 v2.x, FHIR, and NCPDP

---

## Core Message API

### Base Classes

#### HL7Message

Base class for all HL7 message types in the system.

```csharp
public abstract class HL7Message
{
    // Properties
    public MSHSegment Header { get; set; }
    public List<ISegment> Segments { get; }
    public string MessageType { get; }
    public string TriggerEvent { get; }
    
    // Core Methods
    public void AddSegment(ISegment segment);
    public T GetSegment<T>() where T : ISegment;
    public List<T> GetSegments<T>() where T : ISegment;
    public string Encode();
    public static HL7Message Parse(string hl7Text);
    
    // Validation
    public ValidationResult Validate(ValidationLevel level = ValidationLevel.Standard);
    public bool IsValid();
}
```

### Message Types

#### RDEMessage (Pharmacy Order)

```csharp
public class RDEMessage : HL7Message
{
    // Structure: MSH EVN PID [PV1] ORC RXE [RXR] [RXC] [NTE]
    
    // Key Properties
    public MSHSegment MessageHeader { get; set; }
    public EVNSegment EventType { get; set; }
    public PIDSegment PatientIdentification { get; set; }
    public PV1Segment PatientVisit { get; set; }  // Optional
    public ORCSegment CommonOrder { get; set; }
    public RXESegment PharmacyOrder { get; set; }
    public List<RXRSegment> RouteSegments { get; set; }
    public List<RXCSegment> ComponentSegments { get; set; }
    public List<NTESegment> Notes { get; set; }
    
    // Factory Methods
    public static RDEMessage CreateNewOrder(
        string facilityId,
        PatientDemographics patient,
        PrescriptionOrder prescription);
    
    public static RDEMessage CreateFromTemplate(
        string templateName,
        Dictionary<string, object> parameters);
}
```

#### ORRMessage (Order Response)

```csharp
public class ORRMessage : HL7Message
{
    // Structure: MSH MSA [ERR] [PID] [PV1] {ORC [RXO] [DG1] [NTE]}
    
    // Key Properties
    public MSHSegment Header { get; set; }
    public MSASegment MessageAcknowledgment { get; set; }
    public List<ERRSegment> Errors { get; set; }
    public List<OrderResponseGroup> OrderResponses { get; set; }
    
    // Response Methods
    public bool IsAccepted() => MessageAcknowledgment.AcknowledgmentCode.Value == "AA";
    public bool IsRejected() => MessageAcknowledgment.AcknowledgmentCode.Value == "AE";
    public bool HasErrors() => Errors.Any();
    
    // Factory Methods
    public static ORRMessage CreateAcceptance(RDEMessage originalOrder);
    public static ORRMessage CreateRejection(RDEMessage originalOrder, List<ValidationError> errors);
}
```

#### RDSMessage (Pharmacy Dispense)

```csharp
public class RDSMessage : HL7Message
{
    // Structure: MSH EVN PID [PV1] {RXD [RXR] [RXC]}
    
    // Key Properties
    public MSHSegment Header { get; set; }
    public EVNSegment EventType { get; set; }
    public PIDSegment PatientIdentification { get; set; }
    public PV1Segment PatientVisit { get; set; }  // Optional
    public List<DispenseGroup> Dispenses { get; set; }
    
    // Dispense Methods
    public void AddDispense(RXDSegment dispense, List<RXRSegment> routes = null);
    public RXDSegment GetPrimaryDispense() => Dispenses.FirstOrDefault()?.Dispense;
    
    // Factory Methods
    public static RDSMessage CreateFromOrder(RDEMessage order, DispenseInfo dispenseInfo);
}
```

### Segment Access Patterns

#### Proper Field Access

```csharp
// CORRECT: Access fields through typed properties
string patientId = message.PatientIdentification.PatientId.IdNumber;
string firstName = message.PatientIdentification.PatientName.GivenName;
DateTime? birthDate = message.PatientIdentification.DateTimeOfBirth.Value;

// INCORRECT: Do not use string indexers or field positions
// string patientId = message["PID"][3]; // DON'T DO THIS
```

#### Null-Safe Access

```csharp
// Always check for null segments and fields
string roomNumber = message.PatientVisit?.AssignedPatientLocation?.Room ?? "Unknown";
string middleName = message.PatientIdentification?.PatientName?.MiddleInitialOrName ?? string.Empty;
```

---

## Data Generation API

### SyntheticDataService

Main service for coordinating synthetic data generation.

```csharp
public class SyntheticDataService
{
    // Constructor
    public SyntheticDataService(
        IDataGenerator<PatientDemographics> demographicsGenerator,
        IDataGenerator<PrescriptionOrder> medicationGenerator,
        IDataGenerator<PatientJourney> journeyGenerator,
        ILogger<SyntheticDataService> logger = null);
    
    // Message Generation Methods
    public async Task<List<RDEMessage>> GenerateRDEMessages(
        int count,
        DataGenerationConstraints constraints = null);
    
    public async Task<List<ADTMessage>> GenerateADTMessages(
        int count,
        DataGenerationConstraints constraints = null);
    
    // Scenario Generation
    public async Task<ScenarioData> GenerateScenario(
        ScenarioType scenario,
        DataGenerationConstraints constraints = null);
    
    // Patient Journey Generation
    public async Task<PatientJourneySequence> GeneratePatientJourneySequence(
        JourneyType journeyType,
        TimeSpan duration,
        DataGenerationConstraints constraints = null);
    
    // Test Suite Generation
    public async Task<TestSuite> GenerateTestSuite(
        TestSuiteConfiguration config);
}

// Scenario Types
public enum ScenarioType
{
    PharmacyOrder,           // Simple prescription order
    EmergencyAdmission,      // ER admission with medications
    ElectiveSurgery,         // Scheduled surgery with pre/post meds
    OutpatientVisit,         // Clinic visit with prescriptions
    CompletePatientStay      // Full admission to discharge
}
```

### Data Generator Interface

```csharp
public interface IDataGenerator<T>
{
    // Single item generation
    T Generate(DataGenerationConstraints constraints = null);
    Task<T> GenerateAsync(DataGenerationConstraints constraints = null);
    
    // Batch generation
    List<T> GenerateBatch(int count, DataGenerationConstraints constraints = null);
    Task<List<T>> GenerateBatchAsync(int count, DataGenerationConstraints constraints = null);
    
    // Reproducible generation
    T GenerateWithSeed(int seed, DataGenerationConstraints constraints = null);
    
    // Constraint validation
    bool CanSatisfyConstraints(DataGenerationConstraints constraints);
}
```

### Demographics Generator

```csharp
public class DemographicsGenerator : IDataGenerator<PatientDemographics>
{
    // Configuration
    public DemographicsGeneratorOptions Options { get; set; }
    
    // Generation Methods
    public PatientDemographics Generate(DataGenerationConstraints constraints = null);
    
    // Specific Component Generation
    public PersonName GenerateName(Gender? gender = null);
    public AddressInfo GenerateAddress(string state = null);
    public string GenerateSSN(bool includeDashes = true);
    public ContactInfo GenerateContactInfo();
    public InsuranceInfo GenerateInsurance(InsuranceType? type = null);
    public EmergencyContact GenerateEmergencyContact();
    
    // Demographic Constraints
    public class DemographicConstraints : DataGenerationConstraints
    {
        public int? MinAge { get; set; }
        public int? MaxAge { get; set; }
        public Gender? Gender { get; set; }
        public string State { get; set; }
        public bool IncludeSensitiveData { get; set; }
    }
}

// Generated Data Model
public class PatientDemographics
{
    public string PatientId { get; set; }
    public PersonName Name { get; set; }
    public DateTime DateOfBirth { get; set; }
    public Gender Gender { get; set; }
    public string SSN { get; set; }
    public AddressInfo Address { get; set; }
    public ContactInfo Contact { get; set; }
    public InsuranceInfo Insurance { get; set; }
    public EmergencyContact EmergencyContact { get; set; }
}
```

### Medication Generator

```csharp
public class MedicationGenerator : IDataGenerator<PrescriptionOrder>
{
    // Configuration
    public MedicationGeneratorOptions Options { get; set; }
    
    // Generation Methods
    public PrescriptionOrder Generate(DataGenerationConstraints constraints = null);
    
    // Specific Generation
    public MedicationData GenerateMedication(
        MedicationClass? medicationClass = null,
        bool controlledSubstance = false);
    
    public PrescribingInfo GeneratePrescribingInfo(
        MedicationData medication,
        int? daysSupply = null);
    
    public PrescriberInfo GeneratePrescriberInfo(
        string specialty = null);
    
    public PharmacyInfo GeneratePharmacyInfo();
    
    // Medication Constraints
    public class MedicationConstraints : DataGenerationConstraints
    {
        public MedicationClass? MedicationClass { get; set; }
        public bool? ControlledSubstance { get; set; }
        public int? MinDaysSupply { get; set; }
        public int? MaxDaysSupply { get; set; }
        public string RouteOfAdministration { get; set; }
    }
}

// Generated Data Models
public class PrescriptionOrder
{
    public string OrderNumber { get; set; }
    public DateTime OrderDateTime { get; set; }
    public MedicationData Medication { get; set; }
    public PrescribingInfo Prescribing { get; set; }
    public PrescriberInfo Prescriber { get; set; }
    public PharmacyInfo Pharmacy { get; set; }
    public string Status { get; set; }
}

public class MedicationData
{
    public string NDCCode { get; set; }
    public string GenericName { get; set; }
    public string BrandName { get; set; }
    public string Strength { get; set; }
    public string DosageForm { get; set; }
    public MedicationClass Class { get; set; }
    public bool IsControlledSubstance { get; set; }
    public string DEASchedule { get; set; }
}
```

### Patient Journey Generator

```csharp
public class PatientJourneyGenerator : IDataGenerator<PatientJourney>
{
    // Configuration
    public JourneyGeneratorOptions Options { get; set; }
    
    // Journey Generation
    public PatientJourney Generate(DataGenerationConstraints constraints = null);
    
    // Specific Journey Types
    public PatientJourney GenerateEmergencyVisit(TimeSpan? duration = null);
    public PatientJourney GenerateElectiveSurgery(DateTime? scheduledDate = null);
    public PatientJourney GenerateOutpatientVisit(string clinicType = null);
    public PatientJourney GenerateInpatientStay(int? lengthOfStayDays = null);
    
    // Event Generation
    public ClinicalEvent GenerateAdmission(AdmissionType type);
    public ClinicalEvent GenerateTransfer(string fromUnit, string toUnit);
    public ClinicalEvent GenerateDischarge(DischargeDisposition disposition);
    public ClinicalEvent GenerateProcedure(string procedureType);
    
    // Journey Constraints
    public class JourneyConstraints : DataGenerationConstraints
    {
        public JourneyType? JourneyType { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? MinEvents { get; set; }
        public int? MaxEvents { get; set; }
        public List<string> RequiredEventTypes { get; set; }
    }
}

// Journey Types
public enum JourneyType
{
    EmergencyVisit,
    ElectiveSurgery,
    OutpatientVisit,
    InpatientMedical,
    InpatientSurgical,
    ObservationStay,
    RecurringOutpatient,
    LongTermCare,
    Rehabilitation
}

// Generated Data Model
public class PatientJourney
{
    public string JourneyId { get; set; }
    public JourneyType Type { get; set; }
    public DateTime StartDateTime { get; set; }
    public DateTime? EndDateTime { get; set; }
    public List<ClinicalEvent> Events { get; set; }
    public FacilityInfo Facility { get; set; }
    public List<PhysicianInfo> Physicians { get; set; }
    public string PrimaryDiagnosis { get; set; }
    public List<string> SecondaryDiagnoses { get; set; }
}
```

---

## Service Interfaces

### IMessageGeneratorService

Async service interface for HL7 message generation.

```csharp
public interface IMessageGeneratorService
{
    // Message Generation
    Task<GenerationResult> GenerateMessagesAsync(GenerationRequest request);
    
    // Message Type Information
    Task<List<MessageTypeInfo>> GetAvailableMessageTypesAsync();
    Task<MessageTypeInfo> GetMessageTypeInfoAsync(string messageType);
    
    // Template Management
    Task<List<TemplateInfo>> GetAvailableTemplatesAsync(string messageType = null);
    Task<TemplateInfo> GetTemplateInfoAsync(string templateName);
    
    // Scenario Support
    Task<List<ScenarioInfo>> GetAvailableScenariosAsync();
    Task<ScenarioResult> GenerateScenarioAsync(string scenarioName, ScenarioParameters parameters);
}

// Request/Response Models
public class GenerationRequest
{
    public string MessageType { get; set; }
    public int Count { get; set; }
    public string TemplateName { get; set; }
    public Dictionary<string, object> Parameters { get; set; }
    public DataGenerationConstraints Constraints { get; set; }
    public OutputFormat OutputFormat { get; set; }
}

public class GenerationResult
{
    public bool Success { get; set; }
    public List<string> Messages { get; set; }
    public List<GenerationError> Errors { get; set; }
    public GenerationStatistics Statistics { get; set; }
}
```

### IValidationService

Async service interface for HL7 message validation.

```csharp
public interface IValidationService
{
    // Message Validation
    Task<ValidationResult> ValidateAsync(ValidationRequest request);
    Task<MessageValidationResult> ValidateMessageAsync(string message, ValidationOptions options);
    
    // Batch Validation
    Task<BatchValidationResult> ValidateBatchAsync(List<string> messages, ValidationOptions options);
    
    // Validation Configuration
    Task<List<ValidationLevel>> GetAvailableValidationLevelsAsync();
    Task<ValidationRules> GetValidationRulesAsync(string messageType);
    
    // Custom Validators
    Task RegisterValidatorAsync(IMessageValidator validator);
    Task<List<ValidatorInfo>> GetRegisteredValidatorsAsync();
}

// Validation Models
public class ValidationRequest
{
    public string Message { get; set; }
    public string FilePath { get; set; }
    public ValidationLevel Level { get; set; }
    public List<string> SpecificValidators { get; set; }
    public bool IncludeWarnings { get; set; }
}

public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<ValidationError> Errors { get; set; }
    public List<ValidationWarning> Warnings { get; set; }
    public ValidationSummary Summary { get; set; }
}

public class ValidationSummary
{
    public int TotalMessages { get; set; }
    public int ValidMessages { get; set; }
    public int InvalidMessages { get; set; }
    public Dictionary<string, int> ErrorsByType { get; set; }
    public TimeSpan Duration { get; set; }
}

// Validation Levels
public enum ValidationLevel
{
    Syntax,      // Basic HL7 syntax validation
    Schema,      // Message structure validation
    Content,     // Field content validation
    Business,    // Business rule validation
    Complete     // All validation levels
}
```

### IConfigurationService

Service interface for configuration management.

```csharp
public interface IConfigurationService
{
    // Configuration Loading
    Task<Configuration> LoadConfigurationAsync(string path);
    Task<Configuration> GetDefaultConfigurationAsync();
    
    // Configuration Inference
    Task<InferredConfiguration> InferConfigurationAsync(List<string> sampleMessages);
    Task<ConfigurationDiff> CompareConfigurationsAsync(Configuration config1, Configuration config2);
    
    // Template Management
    Task<List<ConfigurationTemplate>> GetTemplatesAsync();
    Task<Configuration> ApplyTemplateAsync(string templateName, Dictionary<string, object> parameters);
    
    // Validation
    Task<ConfigurationValidationResult> ValidateConfigurationAsync(Configuration config);
}
```

### IOutputService

Service interface for output handling.

```csharp
public interface IOutputService
{
    // Output Methods
    Task WriteMessageAsync(string message, OutputFormat format, string destination);
    Task WriteBatchAsync(List<string> messages, OutputFormat format, string destination);
    
    // Format Conversion
    Task<string> ConvertFormatAsync(string message, OutputFormat from, OutputFormat to);
    
    // Supported Formats
    Task<List<OutputFormat>> GetSupportedFormatsAsync();
}

public enum OutputFormat
{
    HL7,         // Standard HL7 pipe-delimited
    JSON,        // JSON representation
    XML,         // XML representation
    CSV,         // CSV for batch processing
    FHIR         // FHIR Bundle (future)
}
```

---

## Validation Framework API

### ValidationEngine

Core validation engine that orchestrates all validation activities.

```csharp
public class ValidationEngine
{
    // Constructor
    public ValidationEngine(
        IEnumerable<IMessageValidator> validators,
        ValidationConfiguration configuration,
        ILogger<ValidationEngine> logger = null);
    
    // Validation Methods
    public ValidationResult Validate(
        HL7Message message,
        ValidationLevel level = ValidationLevel.Complete);
    
    public async Task<ValidationResult> ValidateAsync(
        HL7Message message,
        ValidationLevel level = ValidationLevel.Complete);
    
    // Batch Validation
    public BatchValidationResult ValidateBatch(
        List<HL7Message> messages,
        ValidationLevel level = ValidationLevel.Complete);
    
    // Custom Validation
    public void RegisterValidator(IMessageValidator validator);
    public void UnregisterValidator(Type validatorType);
    
    // Configuration
    public void ConfigureLevel(ValidationLevel level, Action<LevelConfiguration> configure);
}
```

### Message Validators

#### Base Validator Interface

```csharp
public interface IMessageValidator
{
    // Properties
    string Name { get; }
    ValidationLevel Level { get; }
    int Priority { get; }
    
    // Validation
    Task<ValidationResult> ValidateAsync(HL7Message message, ValidationContext context);
    bool CanValidate(HL7Message message);
}
```

#### Built-in Validators

```csharp
// Syntax Validator
public class SyntaxValidator : IMessageValidator
{
    public ValidationLevel Level => ValidationLevel.Syntax;
    
    // Validates:
    // - Segment delimiters
    // - Field separators
    // - Encoding characters
    // - Message structure
}

// Schema Validator
public class SchemaValidator : IMessageValidator
{
    public ValidationLevel Level => ValidationLevel.Schema;
    
    // Validates:
    // - Required segments present
    // - Segment order
    // - Field cardinality
    // - Data types
}

// Content Validator
public class ContentValidator : IMessageValidator
{
    public ValidationLevel Level => ValidationLevel.Content;
    
    // Validates:
    // - Field formats (dates, times, etc.)
    // - Code set membership
    // - Value ranges
    // - Field dependencies
}

// Business Rule Validator
public class BusinessRuleValidator : IMessageValidator
{
    public ValidationLevel Level => ValidationLevel.Business;
    
    // Validates:
    // - Pharmacy workflow rules
    // - Clinical guidelines
    // - Regulatory requirements
    // - Custom business logic
}
```

### Validation Error Reference

```csharp
public class ValidationError
{
    public string ErrorCode { get; set; }
    public ErrorSeverity Severity { get; set; }
    public string Location { get; set; }  // e.g., "PID-3.1"
    public string Message { get; set; }
    public string Details { get; set; }
    public string RecommendedAction { get; set; }
}

// Error Codes
public static class ValidationErrorCodes
{
    // Syntax Errors (1000-1999)
    public const string SYNTAX_INVALID_DELIMITER = "1001";
    public const string SYNTAX_MISSING_HEADER = "1002";
    public const string SYNTAX_MALFORMED_SEGMENT = "1003";
    
    // Schema Errors (2000-2999)
    public const string SCHEMA_MISSING_REQUIRED_SEGMENT = "2001";
    public const string SCHEMA_INVALID_SEGMENT_ORDER = "2002";
    public const string SCHEMA_MISSING_REQUIRED_FIELD = "2003";
    public const string SCHEMA_INVALID_CARDINALITY = "2004";
    
    // Content Errors (3000-3999)
    public const string CONTENT_INVALID_DATE_FORMAT = "3001";
    public const string CONTENT_INVALID_CODE_VALUE = "3002";
    public const string CONTENT_VALUE_OUT_OF_RANGE = "3003";
    public const string CONTENT_INVALID_DATA_TYPE = "3004";
    
    // Business Rule Errors (4000-4999)
    public const string BUSINESS_INVALID_MEDICATION_DOSE = "4001";
    public const string BUSINESS_MISSING_PRESCRIBER_DEA = "4002";
    public const string BUSINESS_INVALID_REFILL_COUNT = "4003";
    public const string BUSINESS_DUPLICATE_ORDER = "4004";
}

// Error Severity
public enum ErrorSeverity
{
    Fatal,       // Message cannot be processed
    Error,       // Message is invalid but may be processable
    Warning,     // Message is valid but has potential issues
    Information  // Informational only
}
```

### Custom Validator Implementation

```csharp
// Example: Custom Pharmacy Validator
public class PharmacyBusinessRuleValidator : IMessageValidator
{
    public string Name => "Pharmacy Business Rules";
    public ValidationLevel Level => ValidationLevel.Business;
    public int Priority => 100;
    
    public async Task<ValidationResult> ValidateAsync(
        HL7Message message, 
        ValidationContext context)
    {
        var errors = new List<ValidationError>();
        
        if (message is RDEMessage rde)
        {
            // Validate controlled substance requirements
            if (IsControlledSubstance(rde))
            {
                if (string.IsNullOrEmpty(rde.CommonOrder.OrderingProvider?.DEANumber))
                {
                    errors.Add(new ValidationError
                    {
                        ErrorCode = "4002",
                        Severity = ErrorSeverity.Error,
                        Location = "ORC-12",
                        Message = "DEA number required for controlled substances",
                        RecommendedAction = "Add prescriber DEA number to ORC-12"
                    });
                }
            }
            
            // Validate dosage limits
            var dose = ParseDose(rde.PharmacyOrder.GiveAmountMinimum);
            if (dose > GetMaxDose(rde.PharmacyOrder.GiveCode))
            {
                errors.Add(new ValidationError
                {
                    ErrorCode = "4001",
                    Severity = ErrorSeverity.Warning,
                    Location = "RXE-3",
                    Message = "Dose exceeds typical maximum",
                    Details = $"Dose of {dose} exceeds typical max",
                    RecommendedAction = "Verify dose with prescriber"
                });
            }
        }
        
        return new ValidationResult { Errors = errors };
    }
    
    public bool CanValidate(HL7Message message) => message is RDEMessage;
}
```

---

## Configuration Management API

### ConfigurationManager

Main class for managing HL7 configuration.

```csharp
public class ConfigurationManager
{
    // Configuration Loading
    public Configuration LoadConfiguration(string path);
    public Configuration LoadFromJson(string json);
    public Configuration LoadFromYaml(string yaml);
    
    // Configuration Saving
    public void SaveConfiguration(Configuration config, string path);
    public string ExportToJson(Configuration config);
    public string ExportToYaml(Configuration config);
    
    // Configuration Inference
    public InferredConfiguration InferConfiguration(
        List<HL7Message> sampleMessages,
        InferenceOptions options = null);
    
    // Configuration Validation
    public ConfigurationValidationResult Validate(Configuration config);
    
    // Template Management
    public Configuration ApplyTemplate(
        string templateName,
        Dictionary<string, object> parameters);
}
```

### Configuration Inference Engine

```csharp
public class ConfigurationInferenceEngine
{
    // Inference Methods
    public InferredConfiguration InferFromMessages(
        List<HL7Message> messages,
        InferenceOptions options);
    
    public FieldConfiguration InferFieldConfiguration(
        List<string> fieldValues,
        FieldMetadata metadata);
    
    public SegmentConfiguration InferSegmentConfiguration(
        List<ISegment> segments);
    
    // Analysis Methods
    public MessageStatistics AnalyzeMessages(List<HL7Message> messages);
    public FieldUsageReport AnalyzeFieldUsage(List<HL7Message> messages);
    public ValidationRuleSet SuggestValidationRules(List<HL7Message> messages);
}

// Inference Options
public class InferenceOptions
{
    public double ConfidenceThreshold { get; set; } = 0.95;
    public int MinimumSampleSize { get; set; } = 10;
    public bool InferValidationRules { get; set; } = true;
    public bool InferDefaultValues { get; set; } = true;
    public bool InferCodeSets { get; set; } = true;
}
```

### Configuration Models

```csharp
public class Configuration
{
    public string Name { get; set; }
    public string Version { get; set; }
    public Dictionary<string, MessageConfiguration> Messages { get; set; }
    public Dictionary<string, SegmentConfiguration> Segments { get; set; }
    public Dictionary<string, FieldConfiguration> Fields { get; set; }
    public ValidationConfiguration Validation { get; set; }
    public GenerationConfiguration Generation { get; set; }
}

public class FieldConfiguration
{
    public string Name { get; set; }
    public string DataType { get; set; }
    public bool Required { get; set; }
    public int? MinLength { get; set; }
    public int? MaxLength { get; set; }
    public string Pattern { get; set; }
    public List<string> AllowedValues { get; set; }
    public string DefaultValue { get; set; }
}
```

---

## Workflow Templates API

### PharmacyWorkflows

Comprehensive pharmacy workflow template system.

```csharp
public static class PharmacyWorkflows
{
    // Standard Prescription Workflow
    public static WorkflowTemplate StandardPrescription()
    {
        return new WorkflowTemplate
        {
            Name = "Standard Prescription",
            Description = "Complete prescription order to dispense workflow",
            Steps = new List<WorkflowStep>
            {
                new WorkflowStep
                {
                    Name = "Prescription Order",
                    MessageType = "RDE^O11",
                    Generator = (context) => GeneratePrescriptionOrder(context),
                    Validators = new[] { "PrescriptionValidator" }
                },
                new WorkflowStep
                {
                    Name = "Order Acceptance",
                    MessageType = "ORR^O02",
                    Generator = (context) => GenerateOrderAcceptance(context),
                    DependsOn = "Prescription Order"
                },
                new WorkflowStep
                {
                    Name = "Dispense Notification",
                    MessageType = "RDS^O13",
                    Generator = (context) => GenerateDispenseNotification(context),
                    DependsOn = "Order Acceptance"
                }
            }
        };
    }
    
    // Controlled Substance Workflow
    public static WorkflowTemplate ControlledSubstancePrescription()
    {
        return new WorkflowTemplate
        {
            Name = "Controlled Substance Prescription",
            Description = "DEA-compliant controlled substance workflow",
            Steps = new List<WorkflowStep>
            {
                // Additional validation steps for controlled substances
                new WorkflowStep
                {
                    Name = "DEA Validation",
                    MessageType = "QBP^Q13",
                    Generator = (context) => GenerateDEAQuery(context)
                },
                // ... rest of workflow
            }
        };
    }
    
    // Refill Request Workflow
    public static WorkflowTemplate RefillRequest()
    {
        return new WorkflowTemplate
        {
            Name = "Prescription Refill",
            Description = "Patient-initiated refill request workflow",
            // ... workflow steps
        };
    }
}
```

### Workflow Execution Engine

```csharp
public class WorkflowEngine
{
    // Workflow Execution
    public async Task<WorkflowResult> ExecuteWorkflowAsync(
        WorkflowTemplate template,
        WorkflowContext context);
    
    public async Task<WorkflowResult> ExecuteStepAsync(
        WorkflowStep step,
        WorkflowContext context);
    
    // Workflow Validation
    public WorkflowValidationResult ValidateWorkflow(
        WorkflowTemplate template,
        WorkflowContext context);
    
    // Event Handling
    public event EventHandler<WorkflowStepCompletedEventArgs> StepCompleted;
    public event EventHandler<WorkflowErrorEventArgs> WorkflowError;
}

// Workflow Context
public class WorkflowContext
{
    public Dictionary<string, object> Parameters { get; set; }
    public Dictionary<string, HL7Message> GeneratedMessages { get; set; }
    public PatientDemographics Patient { get; set; }
    public FacilityInfo Facility { get; set; }
    public DateTime StartTime { get; set; }
}
```

---

## CLI Command API

### Generate Command

```bash
# Basic Usage
segmint generate --type RDE --count 10 --output messages.hl7

# With Template
segmint generate --type RDE --template standard-prescription --output rx.hl7

# With Constraints
segmint generate --type ADT --count 5 --constraint "age:65-80" --constraint "gender:F"

# Scenario Generation
segmint generate --scenario emergency-admission --duration 24h
```

#### Command Options

```csharp
public class GenerateOptions
{
    [Option('t', "type", Required = true, HelpText = "Message type (RDE, ADT, ORM, etc.)")]
    public string MessageType { get; set; }
    
    [Option('c', "count", Default = 1, HelpText = "Number of messages to generate")]
    public int Count { get; set; }
    
    [Option("template", HelpText = "Template name to use")]
    public string Template { get; set; }
    
    [Option("scenario", HelpText = "Generate complete scenario")]
    public string Scenario { get; set; }
    
    [Option("constraint", HelpText = "Generation constraints")]
    public IEnumerable<string> Constraints { get; set; }
    
    [Option('o', "output", HelpText = "Output file path")]
    public string OutputPath { get; set; }
    
    [Option('f', "format", Default = OutputFormat.HL7, HelpText = "Output format")]
    public OutputFormat Format { get; set; }
}
```

### Validate Command

```bash
# Basic Validation
segmint validate --input message.hl7

# Specific Validation Level
segmint validate --input messages.hl7 --level schema

# Batch Validation
segmint validate --input ./messages/ --level complete --output report.json
```

#### Command Options

```csharp
public class ValidateOptions
{
    [Option('i', "input", Required = true, HelpText = "Input file or directory")]
    public string Input { get; set; }
    
    [Option('l', "level", Default = ValidationLevel.Complete, HelpText = "Validation level")]
    public ValidationLevel Level { get; set; }
    
    [Option("validators", HelpText = "Specific validators to run")]
    public IEnumerable<string> Validators { get; set; }
    
    [Option('o', "output", HelpText = "Output report path")]
    public string OutputPath { get; set; }
    
    [Option("format", Default = "text", HelpText = "Report format (text, json, xml)")]
    public string ReportFormat { get; set; }
}
```

### Analyze Command

```bash
# Configuration Inference
segmint analyze --input ./sample-messages/ --infer config --output inferred-config.json

# Message Statistics
segmint analyze --input messages.hl7 --stats --output stats-report.json

# Field Usage Analysis
segmint analyze --input ./messages/ --field-usage --output field-usage.csv
```

### Config Command

```bash
# Load Configuration
segmint config load --file custom-config.json

# Compare Configurations
segmint config compare --config1 old.json --config2 new.json --output diff.json

# Apply Template
segmint config apply-template --template pharmacy-standard --output my-config.json
```

---

## Error Handling & Codes

### Exception Hierarchy

```csharp
// Base Exception
public class SegmintException : Exception
{
    public string ErrorCode { get; set; }
    public ErrorCategory Category { get; set; }
    public Dictionary<string, object> Context { get; set; }
}

// Specific Exceptions
public class MessageParsingException : SegmintException { }
public class ValidationException : SegmintException { }
public class GenerationException : SegmintException { }
public class ConfigurationException : SegmintException { }
public class WorkflowException : SegmintException { }
```

### Error Categories and Codes

```csharp
public enum ErrorCategory
{
    Parsing,      // 1xxx codes
    Validation,   // 2xxx codes
    Generation,   // 3xxx codes
    Configuration,// 4xxx codes
    Workflow,     // 5xxx codes
    System        // 9xxx codes
}

// Common Error Codes
public static class ErrorCodes
{
    // Parsing Errors
    public const string PARSE_INVALID_MESSAGE_STRUCTURE = "1001";
    public const string PARSE_UNKNOWN_SEGMENT_TYPE = "1002";
    public const string PARSE_INVALID_FIELD_SEPARATOR = "1003";
    
    // Generation Errors
    public const string GEN_CONSTRAINT_CONFLICT = "3001";
    public const string GEN_TEMPLATE_NOT_FOUND = "3002";
    public const string GEN_INSUFFICIENT_DATA = "3003";
    
    // Workflow Errors
    public const string WF_STEP_DEPENDENCY_FAILED = "5001";
    public const string WF_TIMEOUT = "5002";
    public const string WF_INVALID_CONTEXT = "5003";
}
```

### Error Handling Patterns

```csharp
// Recommended Error Handling
try
{
    var result = await service.GenerateMessagesAsync(request);
    if (!result.Success)
    {
        // Handle generation errors
        foreach (var error in result.Errors)
        {
            logger.LogError("Generation error: {Code} - {Message}", 
                error.ErrorCode, error.Message);
        }
    }
}
catch (GenerationException ex)
{
    logger.LogError(ex, "Generation failed: {ErrorCode}", ex.ErrorCode);
    // Handle specific generation exception
}
catch (SegmintException ex)
{
    logger.LogError(ex, "Segmint error: {Category} - {ErrorCode}", 
        ex.Category, ex.ErrorCode);
    // Handle general Segmint exceptions
}
```

---

## Common Usage Patterns

### Generate and Validate Workflow

```csharp
// 1. Setup services
var generator = new SyntheticDataService(
    demographicsGenerator, 
    medicationGenerator, 
    journeyGenerator);

var validator = new ValidationEngine(
    validators, 
    configuration);

// 2. Generate messages with constraints
var constraints = new DataGenerationConstraints
{
    ["MinAge"] = 65,
    ["MaxAge"] = 80,
    ["State"] = "CA"
};

var messages = await generator.GenerateRDEMessages(10, constraints);

// 3. Validate generated messages
foreach (var message in messages)
{
    var result = await validator.ValidateAsync(message, ValidationLevel.Complete);
    if (!result.IsValid)
    {
        // Handle validation errors
        foreach (var error in result.Errors)
        {
            Console.WriteLine($"Error at {error.Location}: {error.Message}");
        }
    }
}
```

### Custom Workflow Implementation

```csharp
// Define custom workflow
var customWorkflow = new WorkflowTemplate
{
    Name = "Prior Authorization",
    Steps = new List<WorkflowStep>
    {
        new WorkflowStep
        {
            Name = "Submit PA Request",
            MessageType = "RDE^O11",
            Generator = async (context) =>
            {
                var patient = context.Patient;
                var medication = context.Parameters["Medication"] as MedicationData;
                
                return RDEMessage.CreateNewOrder(
                    context.Facility.Id,
                    patient,
                    new PrescriptionOrder
                    {
                        Medication = medication,
                        Status = "PA_REQUIRED"
                    });
            }
        },
        // Additional steps...
    }
};

// Execute workflow
var engine = new WorkflowEngine();
var context = new WorkflowContext
{
    Patient = await demographicsGenerator.GenerateAsync(),
    Facility = new FacilityInfo { Id = "HOSP001", Name = "Main Hospital" },
    Parameters = new Dictionary<string, object>
    {
        ["Medication"] = await medicationGenerator.GenerateAsync(
            new MedicationConstraints { ControlledSubstance = false })
    }
};

var result = await engine.ExecuteWorkflowAsync(customWorkflow, context);
```

### Configuration Inference from Samples

```csharp
// Load sample messages
var messages = Directory.GetFiles("./samples/", "*.hl7")
    .Select(file => HL7Message.Parse(File.ReadAllText(file)))
    .ToList();

// Infer configuration
var inferenceEngine = new ConfigurationInferenceEngine();
var inferred = inferenceEngine.InferFromMessages(
    messages,
    new InferenceOptions
    {
        ConfidenceThreshold = 0.90,
        MinimumSampleSize = 5,
        InferValidationRules = true
    });

// Review and save configuration
Console.WriteLine($"Inferred {inferred.Fields.Count} field configurations");
Console.WriteLine($"Inferred {inferred.ValidationRules.Count} validation rules");

var configManager = new ConfigurationManager();
configManager.SaveConfiguration(inferred.ToConfiguration(), "inferred-config.json");
```

---

## Testing Guidelines

### Unit Testing Message Generation

```csharp
[TestClass]
public class MessageGenerationTests
{
    private SyntheticDataService _service;
    
    [TestInitialize]
    public void Setup()
    {
        _service = new SyntheticDataService(
            new DemographicsGenerator(),
            new MedicationGenerator(),
            new PatientJourneyGenerator());
    }
    
    [TestMethod]
    public async Task GenerateRDEMessage_WithConstraints_ShouldRespectAge()
    {
        // Arrange
        var constraints = new DataGenerationConstraints
        {
            ["MinAge"] = 65,
            ["MaxAge"] = 80
        };
        
        // Act
        var messages = await _service.GenerateRDEMessages(10, constraints);
        
        // Assert
        Assert.AreEqual(10, messages.Count);
        foreach (var message in messages)
        {
            var birthDate = message.PatientIdentification.DateTimeOfBirth.Value;
            var age = DateTime.Now.Year - birthDate.Year;
            Assert.IsTrue(age >= 65 && age <= 80);
        }
    }
}
```

### Integration Testing Workflows

```csharp
[TestClass]
public class WorkflowIntegrationTests
{
    [TestMethod]
    public async Task StandardPrescriptionWorkflow_ShouldCompleteSuccessfully()
    {
        // Arrange
        var workflow = PharmacyWorkflows.StandardPrescription();
        var engine = new WorkflowEngine();
        var context = CreateTestContext();
        
        // Act
        var result = await engine.ExecuteWorkflowAsync(workflow, context);
        
        // Assert
        Assert.IsTrue(result.Success);
        Assert.AreEqual(3, result.CompletedSteps.Count);
        Assert.IsNotNull(result.GeneratedMessages["Prescription Order"]);
        Assert.IsNotNull(result.GeneratedMessages["Order Acceptance"]);
        Assert.IsNotNull(result.GeneratedMessages["Dispense Notification"]);
    }
}
```

### Validation Testing

```csharp
[TestClass]
public class ValidationTests
{
    private ValidationEngine _engine;
    
    [TestMethod]
    public void ValidateRDEMessage_MissingRequiredField_ShouldReturnError()
    {
        // Arrange
        var message = new RDEMessage();
        // Intentionally leave out required fields
        
        // Act
        var result = _engine.Validate(message, ValidationLevel.Schema);
        
        // Assert
        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e => 
            e.ErrorCode == ValidationErrorCodes.SCHEMA_MISSING_REQUIRED_FIELD));
    }
}
```

### Performance Testing

```csharp
[TestClass]
public class PerformanceTests
{
    [TestMethod]
    [TestCategory("Performance")]
    public async Task GenerateLargeBatch_ShouldCompleteWithinTimeout()
    {
        // Arrange
        var service = new SyntheticDataService(/* dependencies */);
        var stopwatch = Stopwatch.StartNew();
        
        // Act
        var messages = await service.GenerateRDEMessages(1000);
        stopwatch.Stop();
        
        // Assert
        Assert.IsTrue(stopwatch.ElapsedMilliseconds < 5000, 
            $"Generation took {stopwatch.ElapsedMilliseconds}ms");
        Assert.AreEqual(1000, messages.Count);
    }
}
```

---

## Appendix: Quick Reference

### Message Type Mapping

| Message Type | Description | Structure |
|--------------|-------------|-----------|
| RDE^O11 | Pharmacy Order | MSH EVN PID PV1 ORC RXE RXR |
| ORR^O02 | Order Response | MSH MSA ERR ORC |
| RDS^O13 | Dispense | MSH EVN PID RXD RXR |
| ADT^A01 | Admission | MSH EVN PID PV1 |
| ADT^A03 | Discharge | MSH EVN PID PV1 |

### Common Field Paths

| Data Element | Field Path | Example |
|--------------|------------|---------|
| Patient ID | PID-3.1 | PAT123456 |
| Patient Name | PID-5 | Smith^John^A |
| Birth Date | PID-7 | 19800315 |
| Drug Code | RXE-2.1 | 00185003060 |
| Quantity | RXE-3 | 30 |
| Order Status | ORC-1 | NW |

### Validation Level Comparison

| Level | Checks | Performance | Use Case |
|-------|--------|-------------|----------|
| Syntax | Structure only | Fast | Quick validation |
| Schema | + Required fields | Medium | Development |
| Content | + Field formats | Slower | Testing |
| Business | + Business rules | Slowest | Production |

---

## Version History

- **v1.0.0** (2025-07-18): Initial comprehensive API documentation
- Consolidated Python and C# API references
- Added complete Data Generation API documentation
- Added Service Interface specifications
- Added Validation Framework details with error codes
- Added Testing Guidelines section

---

## Next Steps

This documentation should now provide complete API specifications for testing and development. Key areas covered:

1. ✅ Complete Data Generation API with all generators
2. ✅ All Service Interfaces documented
3. ✅ Validation Framework with error codes
4. ✅ Workflow Templates API
5. ✅ Configuration Management API
6. ✅ Common usage patterns and examples
7. ✅ Testing guidelines

With this documentation, developers and testers should be able to:
- Understand all available APIs and their parameters
- Write accurate tests without guessing at implementations
- Properly handle errors and validation results
- Implement custom validators and workflows
- Generate appropriate test data with constraints