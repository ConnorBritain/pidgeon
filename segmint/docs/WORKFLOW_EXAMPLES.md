# Pharmacy Workflow Template Usage Examples

This document provides comprehensive examples of how to use the Segmint pharmacy workflow templates. The hybrid approach combines **Code Templates** (80% of use cases) with **Builder Pattern** (20% of complex scenarios).

## Table of Contents

1. [Quick Start](#quick-start)
2. [Code Templates (Static Methods)](#code-templates-static-methods)
3. [Builder Pattern (Fluent Interface)](#builder-pattern-fluent-interface)
4. [End-to-End Workflows](#end-to-end-workflows)
5. [Validation and Error Handling](#validation-and-error-handling)
6. [Advanced Scenarios](#advanced-scenarios)

---

## Quick Start

### Simple Prescription (Template Approach)

```csharp
using Segmint.Core.Standards.HL7.v23.Workflows.Templates;

// Create a basic prescription in one line
var prescription = PharmacyWorkflows.CreateNewPrescription(
    patientId: "PAT123",
    lastName: "Smith", 
    firstName: "John",
    medicationCode: "12345678",
    medicationName: "Lisinopril 10mg",
    quantity: 30m,
    units: "TAB",
    sig: "Take 1 tablet by mouth daily",
    providerId: "PRV456",
    refills: 3,
    daysSupply: 30
);

// Generate HL7 message
string hl7Message = prescription.ToString();
```

### Complex Prescription (Builder Approach)

```csharp
using Segmint.Core.Standards.HL7.v23.Workflows.Builders;

// Create a complex prescription with fluent interface
var prescription = PharmacyWorkflowBuilder.NewPrescription()
    .WithPatientDemographics("PAT123", "Smith", "John", "Robert", 
        new DateTime(1980, 5, 15), "M", "123456789")
    .WithPatientAddress("123 Main St", "Anytown", "CA", "12345")
    .WithMedication("00406055705", "Oxycodone 5mg", 5m, "mg", "TAB")
    .WithInstructions("Take 1 tablet every 6 hours as needed for pain", 20m, "TAB", 0)
    .WithProvider("PRV456", "Johnson", "Robert", "MD")
    .AsControlledSubstance("II", "BJ1234567")
    .WithPriority("S")
    .WithNote("Patient has chronic pain condition", "CLINICAL")
    .Build();
```

---

## Code Templates (Static Methods)

### 1. Basic Prescription

```csharp
// Most common use case - basic prescription
var rdeMessage = PharmacyWorkflows.CreateNewPrescription(
    patientId: "PAT001",
    lastName: "Doe",
    firstName: "Jane",
    medicationCode: "00093310505", // NDC code
    medicationName: "Metformin 500mg",
    quantity: 60m,
    units: "TAB",
    sig: "Take 1 tablet twice daily with meals",
    providerId: "PRV123",
    refills: 5,
    daysSupply: 30
);

Console.WriteLine($"Prescription created: {rdeMessage.MessageHeader.MessageControlId.Value}");
```

### 2. Comprehensive Prescription with Patient Demographics

```csharp
// Include full patient demographics
var rdeMessage = PharmacyWorkflows.CreateComprehensivePrescription(
    patientId: "PAT002",
    lastName: "Williams",
    firstName: "Michael",
    dateOfBirth: new DateTime(1975, 8, 22),
    gender: "M",
    medicationCode: "00781158301",
    medicationName: "Atorvastatin",
    strength: 20m,
    strengthUnits: "mg",
    dosageForm: "TAB",
    quantity: 30m,
    units: "TAB",
    sig: "Take 1 tablet by mouth at bedtime",
    providerId: "PRV456",
    providerLastName: "Johnson",
    providerFirstName: "Sarah",
    refills: 5,
    daysSupply: 30
);
```

### 3. Controlled Substance Prescription

```csharp
// Controlled substance with DEA requirements
var rdeMessage = PharmacyWorkflows.CreateControlledSubstancePrescription(
    patientId: "PAT003",
    lastName: "Brown",
    firstName: "Robert",
    dateOfBirth: new DateTime(1965, 3, 15), // Required for controlled substances
    medicationCode: "00406055705",
    medicationName: "Oxycodone 5mg",
    quantity: 20m,
    units: "TAB",
    sig: "Take 1 tablet every 6 hours as needed for pain",
    providerId: "PRV789",
    deaNumber: "BJ1234567", // DEA number required
    schedule: "II", // Schedule II controlled substance
    refills: 0 // No refills for Schedule II
);
```

### 4. Order Response (Acceptance)

```csharp
// Accept a prescription order
var orrMessage = PharmacyWorkflows.CreateAcceptanceResponse(
    originalControlId: "RDE_20240315_1234", // From original RDE message
    placerOrderNumber: "ORD123456",
    fillerOrderNumber: "RX789012", // Pharmacy-assigned number
    acceptanceMessage: "Order accepted for processing",
    estimatedFillTime: DateTime.Now.AddMinutes(30)
);
```

### 5. Order Response (Rejection)

```csharp
// Reject a prescription with detailed errors
var detailedErrors = new List<(string code, string description, string severity)>
{
    ("DRUG_NOT_COVERED", "Medication not on formulary", "E"),
    ("PRIOR_AUTH_REQUIRED", "Prior authorization required", "E"),
    ("QUANTITY_EXCEEDED", "Quantity exceeds plan limits", "W")
};

var orrMessage = PharmacyWorkflows.CreateRejectionResponse(
    originalControlId: "RDE_20240315_5678",
    placerOrderNumber: "ORD999999",
    rejectionReason: "Multiple issues with prescription",
    detailedErrors: detailedErrors
);
```

### 6. Dispense Record

```csharp
// Record medication dispensing
var rdsMessage = PharmacyWorkflows.CreateDispenseRecord(
    patientId: "PAT001",
    lastName: "Doe",
    firstName: "Jane",
    prescriptionNumber: "RX2024031500123",
    medicationCode: "00093310505",
    medicationName: "Metformin 500mg",
    dispensedAmount: 60m,
    units: "TAB",
    pharmacist: "Jane Doe, RPh",
    lotNumber: "LOT123456",
    expirationDate: new DateTime(2025, 12, 31),
    manufacturer: "Teva Pharmaceuticals",
    refillsRemaining: 4
);
```

---

## Builder Pattern (Fluent Interface)

### 1. Complex Patient Demographics

```csharp
var prescription = PharmacyWorkflowBuilder.NewPrescription()
    .WithPatientDemographics(
        patientId: "PAT004",
        lastName: "Garcia",
        firstName: "Maria",
        middleName: "Elena",
        dateOfBirth: new DateTime(1990, 7, 12),
        gender: "F",
        ssn: "987654321"
    )
    .WithPatientAddress(
        street: "456 Oak Avenue",
        city: "Springfield",
        state: "IL",
        zipCode: "62701",
        country: "USA"
    )
    .WithPatientPhones(
        homePhone: "555-1234",
        workPhone: "555-5678"
    )
    .WithMedication("00172588070", "Levothyroxine 75mcg")
    .WithInstructions("Take 1 tablet by mouth daily on empty stomach")
    .WithProvider("PRV999", "Anderson", "David", "MD")
    .Build();
```

### 2. Medication with Detailed Information

```csharp
var prescription = PharmacyWorkflowBuilder.NewPrescription()
    .WithPatient("PAT005", "Taylor", "James")
    .WithMedication(
        medicationCode: "00093310505",
        medicationName: "Metformin Extended Release",
        strength: 500m,
        strengthUnits: "mg",
        dosageForm: "TAB"
    )
    .WithDispensing(
        quantity: 90m,
        units: "TAB",
        refills: 5
    )
    .WithInstructions("Take 1 tablet by mouth twice daily with meals")
    .WithProvider("PRV111", "Wilson", "Emily")
    .Build();
```

### 3. Order Control and Priority

```csharp
var prescription = PharmacyWorkflowBuilder.NewPrescription()
    .WithPatient("PAT006", "Miller", "Susan")
    .WithMedication("00781158301", "Atorvastatin 20mg")
    .WithInstructions("Take 1 tablet by mouth at bedtime")
    .WithProvider("PRV222", "Davis", "Mark")
    .WithOrderControl("NW", "IP") // New order, In process
    .WithOrderTiming(
        orderDateTime: DateTime.Now,
        effectiveDateTime: DateTime.Now.AddHours(2)
    )
    .WithPlacerOrderNumber("CUSTOM_ORD_123456")
    .WithPriority("S") // Stat priority
    .Build();
```

### 4. Multiple Notes and Special Instructions

```csharp
var prescription = PharmacyWorkflowBuilder.NewPrescription()
    .WithPatient("PAT007", "Anderson", "Robert")
    .WithMedication("00172588070", "Levothyroxine 100mcg")
    .WithInstructions("Take 1 tablet by mouth daily 30 minutes before breakfast")
    .WithProvider("PRV333", "Thompson", "Lisa")
    .WithNote("Patient allergic to shellfish", "ALLERGY")
    .WithNote("Insurance: Aetna PPO", "INSURANCE")
    .WithNote("Copay: $15.00", "COPAY")
    .WithNote("Patient counseled on timing of administration", "COUNSELING")
    .WithPriority("R") // Routine priority
    .Build();
```

---

## End-to-End Workflows

### Complete RDE → ORR → RDS Workflow

```csharp
// Step 1: Create prescription order (RDE)
var rdeMessage = PharmacyWorkflows.CreateNewPrescription(
    "PAT123", "Smith", "John",
    "12345678", "Lisinopril 10mg",
    30m, "TAB", "Take 1 tablet by mouth daily",
    "PRV456", refills: 3
);

Console.WriteLine($"Order created: {rdeMessage.MessageHeader.MessageControlId.Value}");

// Step 2: Pharmacy accepts order (ORR)
var orrMessage = PharmacyWorkflows.CreateAcceptanceResponse(
    originalControlId: rdeMessage.MessageHeader.MessageControlId.Value,
    placerOrderNumber: rdeMessage.CommonOrder.PlacerOrderNumber.Value,
    fillerOrderNumber: "RX789012",
    acceptanceMessage: "Order accepted for processing",
    estimatedFillTime: DateTime.Now.AddMinutes(30)
);

Console.WriteLine($"Order accepted: {orrMessage.MessageAcknowledgment.AcknowledgmentCode.Value}");

// Step 3: Pharmacy dispenses medication (RDS)
var rdsMessage = PharmacyWorkflows.CreateDispenseRecord(
    "PAT123", "Smith", "John",
    "RX2024031500123", "12345678", "Lisinopril 10mg",
    30m, "TAB", "Jane Doe, RPh",
    "LOT123456", new DateTime(2025, 12, 31), "Generic Pharma", 3
);

Console.WriteLine($"Medication dispensed: {rdsMessage.MessageType}");
```

### Controlled Substance Workflow

```csharp
// Step 1: Controlled substance prescription
var rdeMessage = PharmacyWorkflowBuilder.NewPrescription()
    .WithPatientDemographics("PAT456", "Brown", "Alice", 
        dateOfBirth: new DateTime(1985, 4, 10), gender: "F")
    .WithMedication("00406055705", "Oxycodone 5mg", 5m, "mg", "TAB")
    .WithInstructions("Take 1 tablet every 6 hours as needed for pain", 20m, "TAB", 0)
    .WithProvider("PRV789", "Johnson", "Robert", "MD")
    .AsControlledSubstance("II", "BJ1234567")
    .WithPriority("S")
    .WithNote("Patient has documented chronic pain condition", "CLINICAL")
    .Build();

// Step 2: Pharmacy response (additional verification required)
var orrMessage = PharmacyWorkflows.CreateAcceptanceResponse(
    rdeMessage.MessageHeader.MessageControlId.Value,
    rdeMessage.CommonOrder.PlacerOrderNumber.Value,
    "CS789012", // Controlled substance fill number
    "Controlled substance order accepted - patient ID verified",
    DateTime.Now.AddMinutes(45)
);

// Step 3: Controlled substance dispensing
var rdsMessage = PharmacyWorkflows.CreateDispenseRecord(
    "PAT456", "Brown", "Alice",
    "CS2024031500456", "00406055705", "Oxycodone 5mg",
    20m, "TAB", "John Smith, PharmD",
    "LOT654321", new DateTime(2025, 8, 15), "Mallinckrodt", 0
);

// Add controlled substance dispensing note
rdsMessage.WithWorkflowNote(
    "Controlled substance dispensed - patient counseled on safe use and storage", 
    "CONTROLLED_SUBSTANCE"
);
```

---

## Validation and Error Handling

### Message Validation

```csharp
using Segmint.Core.Standards.HL7.v23.Workflows.Common;

// Validate prescription before sending
var prescription = PharmacyWorkflows.CreateNewPrescription(
    "PAT123", "Smith", "John",
    "12345678", "Lisinopril 10mg",
    30m, "TAB", "Take 1 tablet by mouth daily",
    "PRV456"
);

// Validate workflow
var validation = prescription.ValidateWorkflow();
if (validation.IsValid)
{
    Console.WriteLine("Prescription is valid and ready for transmission");
}
else
{
    Console.WriteLine("Validation errors found:");
    foreach (var error in validation.ValidationIssues)
    {
        Console.WriteLine($"- {error}");
    }
}

// Check transmission readiness
if (prescription.IsReadyForTransmission())
{
    string hl7Message = prescription.ToString();
    // Send to pharmacy system
}
```

### Builder Validation

```csharp
// Validate during build process
var builder = PharmacyWorkflowBuilder.NewPrescription()
    .WithPatient("", "Smith", "John") // Empty patient ID - will cause error
    .WithMedication("12345678", "Lisinopril 10mg")
    .WithInstructions("Take 1 tablet by mouth daily");

// Check validation without building
var validationErrors = builder.ValidateOnly();
if (validationErrors.Count > 0)
{
    Console.WriteLine("Validation errors:");
    foreach (var error in validationErrors)
    {
        Console.WriteLine($"- {error}");
    }
    
    // Fix errors and build
    var prescription = builder
        .WithPatient("PAT123", "Smith", "John") // Fix patient ID
        .Build();
}
```

### Error Response Handling

```csharp
// Handle pharmacy rejection
var detailedErrors = new List<(string code, string description, string severity)>
{
    ("DRUG_INTERACTION", "Potential interaction with Warfarin", "W"),
    ("DOSAGE_ALERT", "Dosage exceeds recommended maximum", "E"),
    ("AGE_RESTRICTION", "Not recommended for patients over 65", "W")
};

var rejectionResponse = PharmacyWorkflows.CreateRejectionResponse(
    "RDE_20240315_1234",
    "ORD123456",
    "Prescription requires clinical review",
    detailedErrors
);

// Extract error information
var responseInfo = rejectionResponse.GetOrderResponseInfo();
Console.WriteLine($"Rejection Code: {responseInfo.AcknowledgmentCode}");
Console.WriteLine($"Error Count: {responseInfo.ErrorCount}");

foreach (var error in responseInfo.Errors)
{
    Console.WriteLine($"Error: {error.ErrorCode} - {error.ErrorText} (Severity: {error.Severity})");
}
```

---

## Advanced Scenarios

### Partial Fill Workflow

```csharp
// Original prescription for 90 tablets
var originalPrescription = PharmacyWorkflows.CreateNewPrescription(
    "PAT789", "Wilson", "Sarah",
    "12345678", "Lisinopril 10mg",
    90m, "TAB", "Take 1 tablet by mouth daily",
    "PRV456", refills: 3
);

// Pharmacy accepts but notes partial fill
var partialAcceptance = PharmacyWorkflows.CreateAcceptanceResponse(
    originalPrescription.MessageHeader.MessageControlId.Value,
    originalPrescription.CommonOrder.PlacerOrderNumber.Value,
    "RX999888",
    "Order accepted - Partial fill: 30 tablets available"
);

// Dispense partial amount
var partialDispense = PharmacyWorkflows.CreateDispenseRecord(
    "PAT789", "Wilson", "Sarah",
    "RX2024031500789", "12345678", "Lisinopril 10mg",
    30m, "TAB", "Mike Johnson, RPh", // Only 30 tablets
    "LOT999888", new DateTime(2025, 10, 15), "Sandoz", 3
);

// Add partial fill note
partialDispense.WithWorkflowNote(
    "Partial fill: 30 of 90 tablets dispensed. Remaining 60 tablets to follow when available.",
    "PARTIAL_FILL"
);
```

### High-Volume Processing

```csharp
// Process multiple prescriptions efficiently
var prescriptions = new[]
{
    ("PAT001", "Lisinopril 10mg", "12345678"),
    ("PAT002", "Metformin 500mg", "87654321"),
    ("PAT003", "Atorvastatin 20mg", "11223344")
};

var processedOrders = new List<(RDEMessage rde, ORRMessage orr, RDSMessage rds)>();

foreach (var (patientId, medication, code) in prescriptions)
{
    // Create prescription
    var rde = PharmacyWorkflows.CreateNewPrescription(
        patientId, "TestPatient", "User",
        code, medication,
        30m, "TAB", "Take as directed",
        "PRV123"
    );

    // Auto-accept
    var orr = PharmacyWorkflows.CreateAcceptanceResponse(
        rde.MessageHeader.MessageControlId.Value,
        rde.CommonOrder.PlacerOrderNumber.Value,
        $"RX{DateTime.Now:yyyyMMddHHmmss}"
    );

    // Auto-dispense
    var rds = PharmacyWorkflows.CreateDispenseRecord(
        patientId, "TestPatient", "User",
        $"RX{DateTime.Now:yyyyMMddHHmmss}", code, medication,
        30m, "TAB", "Auto Dispense System"
    );

    processedOrders.Add((rde, orr, rds));
}

Console.WriteLine($"Processed {processedOrders.Count} prescriptions");
```

### Workflow Summary and Tracking

```csharp
// Get workflow summary for tracking
var prescription = PharmacyWorkflows.CreateNewPrescription(
    "PAT123", "Smith", "John",
    "12345678", "Lisinopril 10mg",
    30m, "TAB", "Take 1 tablet by mouth daily",
    "PRV456"
);

var summary = prescription.GetWorkflowSummary();
Console.WriteLine($"Message Type: {summary.MessageType}");
Console.WriteLine($"Workflow Type: {summary.WorkflowType}");
Console.WriteLine($"Patient: {summary.PatientName} (ID: {summary.PatientId})");
Console.WriteLine($"Control ID: {summary.MessageControlId}");
Console.WriteLine($"Date/Time: {summary.MessageDateTime}");
Console.WriteLine($"Validation Status: {summary.ValidationStatus}");

// Extract prescription-specific information
var prescriptionInfo = prescription.GetPrescriptionInfo();
Console.WriteLine($"Medication: {prescriptionInfo.MedicationName}");
Console.WriteLine($"Code: {prescriptionInfo.MedicationCode}");
Console.WriteLine($"Instructions: {prescriptionInfo.Instructions}");
Console.WriteLine($"Provider: {prescriptionInfo.OrderingProvider}");
```

---

## Best Practices

### 1. Choose the Right Approach

- **Use Code Templates** for 80% of standard prescriptions
- **Use Builder Pattern** for complex scenarios with multiple optional fields
- **Use End-to-End Workflows** for complete pharmacy integration

### 2. Validation Strategy

```csharp
// Always validate before transmission
if (message.IsReadyForTransmission())
{
    // Safe to send
    TransmitMessage(message);
}
else
{
    // Handle validation errors
    var errors = message.ValidateWorkflow();
    LogValidationErrors(errors);
}
```

### 3. Error Handling

```csharp
// Graceful error handling
try
{
    var prescription = PharmacyWorkflowBuilder.NewPrescription()
        .WithPatient(patientId, lastName, firstName)
        .WithMedication(medicationCode, medicationName)
        .WithInstructions(sig)
        .Build();
}
catch (InvalidOperationException ex)
{
    Console.WriteLine($"Prescription validation failed: {ex.Message}");
    // Handle validation errors appropriately
}
```

### 4. Performance Considerations

- Use static templates for high-volume scenarios
- Validate once before transmission
- Cache frequently used configurations
- Use parallel processing for batch operations

---

This documentation provides comprehensive examples for all pharmacy workflow scenarios. The hybrid approach ensures both simplicity for common cases and flexibility for complex requirements.