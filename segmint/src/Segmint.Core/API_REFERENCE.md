# Segmint HL7 Generator API Reference

## Overview

This document provides comprehensive API documentation for the Segmint HL7 Generator's workflow templates and message classes. This reference is essential for developers and AI agents working with the pharmacy workflow system.

**Target Audience**: Developers, AI agents, and system integrators working with HL7 pharmacy workflows.

---

## Table of Contents

1. [Core Message Types](#core-message-types)
2. [Workflow Templates](#workflow-templates)
3. [Segment Types](#segment-types)
4. [Field Types](#field-types)
5. [Common Access Patterns](#common-access-patterns)
6. [Error Handling](#error-handling)
7. [Validation Rules](#validation-rules)
8. [Best Practices](#best-practices)

---

## Core Message Types

### RDEMessage (Pharmacy Order)

**Purpose**: Represents an HL7 RDE (Pharmacy Order) message for communicating pharmacy orders.

**Structure**: `MSH EVN PID [PV1] ORC RXE [RXR] [RXC] [NTE]`

**Key Properties**:
- `MessageHeader` (`MSHSegment`): Message header
- `EventType` (`EVNSegment`): Event type
- `PatientIdentification` (`PIDSegment`): Patient information
- `CommonOrder` (`ORCSegment`): Order control
- `PharmacyOrder` (`RXESegment`): Medication details

**Patient Information Access**:
```csharp
// Access patient details
string patientId = message.PatientIdentification.PatientId.IdNumber;
string firstName = message.PatientIdentification.PatientName.GivenName;
string lastName = message.PatientIdentification.PatientName.FamilyName;
DateTime? birthDate = message.PatientIdentification.DateTimeOfBirth.Value;
string gender = message.PatientIdentification.AdministrativeSex.Value;
```

**Medication Access**:
```csharp
// Access medication details
string drugCode = message.PharmacyOrder.GiveCode.Identifier;
string drugName = message.PharmacyOrder.GiveCode.Text;
string quantity = message.PharmacyOrder.GiveAmountMinimum.Quantity;
string units = message.PharmacyOrder.GiveAmountMinimum.Units;
string instructions = message.PharmacyOrder.ProvidersAdministrationInstructions.Value;
```

**Order Information Access**:
```csharp
// Access order details
string orderControl = message.CommonOrder.OrderControl.Value;
string placerOrderNumber = message.CommonOrder.PlacerOrderNumber.Value;
string orderingProvider = message.CommonOrder.OrderingProvider.Value;
DateTime? orderDateTime = message.CommonOrder.DateTimeOfTransaction.Value;
```

### ORRMessage (Order Response)

**Purpose**: Represents an HL7 ORR (Order Response) message for pharmacy order responses.

**Structure**: `MSH MSA [ERR] [PID] [PV1] [ORC] [RXO] [DG1] [NTE]`

**Key Properties**:
- `Header` (`MSHSegment`): Message header
- `MessageAcknowledgment` (`MSASegment`): Acknowledgment status
- `Errors` (`List<ERRSegment>`): Error details
- `OrderResponses` (`List<OrderResponseGroup>`): Order response groups

**Response Status Access**:
```csharp
// Check response status
string ackCode = orrMessage.MessageAcknowledgment.AcknowledgmentCode.Value;
bool isAccepted = ackCode == "AA";
bool isRejected = ackCode == "AE";
bool isApplicationError = ackCode == "AR";
string originalMessageControlId = orrMessage.MessageAcknowledgment.MessageControlId.Value;
```

**Error Information Access**:
```csharp
// Access error details
bool hasErrors = orrMessage.Errors.Any();
foreach (var error in orrMessage.Errors)
{
    string errorCode = error.ApplicationErrorCode.Value;
    string errorDescription = error.DiagnosticInformation.Value;
    string severity = error.Severity.Value;
    bool isFatal = error.IsFatal();
}
```

### RDSMessage (Pharmacy Dispense)

**Purpose**: Represents an HL7 RDS (Pharmacy Dispense) message for medication dispensing records.

**Structure**: `MSH [PID] [PV1] ORC RXO RXE RXD [RXC] [OBX] [NTE]`

**Key Properties**:
- `Header` (`MSHSegment`): Message header
- `PatientIdentification` (`PIDSegment`): Patient information
- `Dispenses` (`List<PharmacyDispenseGroup>`): Dispense records

**Dispense Information Access**:
```csharp
// Access dispense details
var firstDispense = rdsMessage.Dispenses[0];
var dispensedMedication = firstDispense.DispenseRecord?.DispenseGiveCode.Text;
var dispensedAmount = firstDispense.DispenseRecord?.ActualDispenseAmount.RawValue;
var dispensedUnits = firstDispense.DispenseRecord?.ActualDispenseUnits.Identifier;
var prescriptionNumber = firstDispense.DispenseRecord?.PrescriptionNumber.RawValue;
var dispensingDate = firstDispense.DispenseRecord?.DateTimeDispensed.ToDateTime();
```

**Medication Tracking Access**:
```csharp
// Access lot tracking information
var lotNumber = firstDispense.DispenseRecord?.SubstanceLotNumber.RawValue;
var expirationDate = firstDispense.DispenseRecord?.SubstanceExpirationDate.ToDateTime();
var manufacturer = firstDispense.DispenseRecord?.SubstanceManufacturerName.Identifier;
var pharmacist = firstDispense.DispenseRecord?.DispensingProvider.IdNumber;
```

---

## Workflow Templates

### PharmacyWorkflows Class

**Purpose**: Static factory class providing pre-configured workflow templates for common pharmacy operations.

**Location**: `Segmint.Core.Standards.HL7.v23.Workflows.Templates.PharmacyWorkflows`

#### New Prescription Methods

**CreateNewPrescription**:
```csharp
public static RDEMessage CreateNewPrescription(
    string patientId,
    string lastName,
    string firstName,
    string medicationCode,
    string medicationName,
    decimal quantity,
    string units,
    string sig,
    string providerId,
    int refills = 0,
    int daysSupply = 30)
```

**CreateComprehensivePrescription**:
```csharp
public static RDEMessage CreateComprehensivePrescription(
    string patientId,
    string lastName,
    string firstName,
    DateTime dateOfBirth,
    string gender,
    string medicationCode,
    string medicationName,
    decimal strength,
    string strengthUnits,
    string dosageForm,
    decimal quantity,
    string units,
    string sig,
    string providerId,
    string providerLastName,
    string providerFirstName,
    int refills = 0,
    int daysSupply = 30)
```

**CreateControlledSubstancePrescription**:
```csharp
public static RDEMessage CreateControlledSubstancePrescription(
    string patientId,
    string lastName,
    string firstName,
    DateTime dateOfBirth,
    string medicationCode,
    string medicationName,
    decimal quantity,
    string units,
    string sig,
    string providerId,
    string providerDeaNumber,
    string controlledSubstanceSchedule,
    int refills = 0)
```

#### Order Response Methods

**CreateAcceptanceResponse**:
```csharp
public static ORRMessage CreateAcceptanceResponse(
    string originalMessageControlId,
    string placerOrderNumber,
    string? fillerOrderNumber = null,
    string? acceptanceMessage = null,
    DateTime? estimatedFillTime = null)
```

**CreateRejectionResponse**:
```csharp
public static ORRMessage CreateRejectionResponse(
    string originalMessageControlId,
    string placerOrderNumber,
    string rejectionReason,
    List<(string code, string description, string severity)>? detailedErrors = null)
```

**CreateDrugInteractionResponse**:
```csharp
public static ORRMessage CreateDrugInteractionResponse(
    string originalMessageControlId,
    string placerOrderNumber,
    List<(string severity, string drug1, string drug2, string effect)> interactions,
    bool allowOverride = true)
```

#### Dispensing Methods

**CreateDispenseRecord**:
```csharp
public static RDSMessage CreateDispenseRecord(
    string patientId,
    string lastName,
    string firstName,
    string prescriptionNumber,
    string medicationCode,
    string medicationName,
    decimal dispensedAmount,
    string units,
    string pharmacist,
    string? lotNumber = null,
    DateTime? expirationDate = null,
    string? manufacturer = null,
    int? refillsRemaining = null)
```

**CreateControlledSubstanceDispense**:
```csharp
public static RDSMessage CreateControlledSubstanceDispense(
    string patientId,
    string lastName,
    string firstName,
    string prescriptionNumber,
    string medicationCode,
    string medicationName,
    decimal dispensedAmount,
    string units,
    string pharmacist,
    string lotNumber,
    DateTime expirationDate,
    string manufacturer,
    string deaNumber,
    string controlledSubstanceSchedule)
```

---

## Common Access Patterns

### Field Access Patterns

**Important**: Field access varies by field type. Common patterns:

**String Fields**:
- `field.Value` - Get/set string value
- `field.RawValue` - Get raw string representation

**Coded Element Fields**:
- `field.Identifier` - Code identifier
- `field.Text` - Display text
- `field.CodingSystem` - Coding system

**Numeric Fields**:
- `field.RawValue` - String representation
- `field.ToDecimal()` - Decimal value

**Date/Time Fields**:
- `field.Value` - DateTime value
- `field.ToDateTime()` - Nullable DateTime

**Identifier Fields**:
- `field.Value` - Identifier value
- `field.IdNumber` - ID number component

### Property Access Patterns

**CORRECT Property Names**:
- `message.PatientIdentification` (NOT `message.Patient`)
- `message.PharmacyOrder` (NOT `message.Medication`)
- `message.CommonOrder` (NOT `message.Order`)

**CORRECT Field Access**:
- `patient.PatientId.IdNumber` (NOT `patient.PatientId` directly)
- `patient.PatientName.GivenName` (NOT `patient.PatientName.Value`)
- `medication.GiveCode.Identifier` (NOT `medication.GiveCode.Value`)

### Safe Access Patterns

**Null-Safe Access**:
```csharp
// Safe access with null checks
string? patientId = message.PatientIdentification?.PatientId?.IdNumber;
string medicationName = message.PharmacyOrder?.GiveCode?.Text ?? "Unknown";
```

**Validation Before Access**:
```csharp
// Validate before accessing
if (message.PatientIdentification != null)
{
    var patientName = message.PatientIdentification.PatientName.ToDisplayString();
}
```

---

## Error Handling

### Common Error Types

1. **Null Reference Errors**: Properties may be null
2. **Field Access Errors**: Wrong property names or access patterns
3. **Type Conversion Errors**: Incorrect field value access
4. **Validation Errors**: Message structure violations

### Error Prevention

**Use Proper Null Checks**:
```csharp
if (message.PatientIdentification?.PatientId?.IdNumber != null)
{
    // Safe to access
}
```

**Use Safe Access Patterns**:
```csharp
string patientId = message.PatientIdentification?.PatientId?.IdNumber ?? "Unknown";
```

**Validate Messages**:
```csharp
var validationErrors = message.Validate();
if (validationErrors.Any())
{
    // Handle validation errors
}
```

---

## Validation Rules

### Message Structure Validation

**RDEMessage**:
- Must contain MSH, EVN, PID, ORC, RXE segments
- Segments must be in correct order
- Required fields must be populated

**ORRMessage**:
- Must contain MSH, MSA segments
- Acknowledgment code must be valid (AA, AE, AR)
- Error segments required for rejection responses

**RDSMessage**:
- Must contain MSH segment
- At least one dispense record required
- Dispense records must have valid medication codes

### Field Validation

**Required Fields**:
- Patient ID in PID segment
- Medication code in RXE segment
- Order control in ORC segment

**Format Validation**:
- Dates must be valid DateTime values
- Numeric fields must be valid decimals
- Coded fields must have valid identifiers

---

## Best Practices

### 1. Use Workflow Templates

**Recommended**: Use `PharmacyWorkflows` static methods for common scenarios
```csharp
// Good
var prescription = PharmacyWorkflows.CreateNewPrescription(
    patientId, lastName, firstName, medicationCode, medicationName,
    quantity, units, sig, providerId, refills, daysSupply);

// Avoid manual message construction when templates exist
```

### 2. Proper Error Handling

**Always validate messages**:
```csharp
var validationErrors = message.Validate();
if (validationErrors.Any())
{
    foreach (var error in validationErrors)
    {
        Console.WriteLine($"Validation Error: {error}");
    }
}
```

### 3. Consistent Access Patterns

**Use consistent field access**:
```csharp
// Consistent pattern
var patientId = message.PatientIdentification.PatientId.IdNumber;
var medicationCode = message.PharmacyOrder.GiveCode.Identifier;
var orderControl = message.CommonOrder.OrderControl.Value;
```

### 4. Null Safety

**Always check for null**:
```csharp
// Safe access
if (message.PatientIdentification?.PatientId?.IdNumber != null)
{
    var id = message.PatientIdentification.PatientId.IdNumber;
}
```

### 5. Use Proper Field Types

**Access fields by their correct type**:
```csharp
// Correct field access
string medicationCode = codedField.Identifier;  // Not codedField.Value
string medicationName = codedField.Text;        // Not codedField.Value
decimal quantity = numericField.ToDecimal();    // Not numericField.Value
```

---

## API Quick Reference

### Message Types
- `RDEMessage` - Pharmacy orders
- `ORRMessage` - Order responses
- `RDSMessage` - Dispensing records

### Factory Methods
- `PharmacyWorkflows.CreateNewPrescription()` - Basic prescriptions
- `PharmacyWorkflows.CreateAcceptanceResponse()` - Order acceptance
- `PharmacyWorkflows.CreateDispenseRecord()` - Dispensing records

### Key Properties
- `PatientIdentification` - Patient information (PID)
- `PharmacyOrder` - Medication details (RXE)
- `CommonOrder` - Order control (ORC)
- `MessageAcknowledgment` - Response status (MSA)

### Field Access
- `.Identifier` - Code values
- `.Text` - Display text
- `.RawValue` - String representation
- `.ToDateTime()` - Date conversion
- `.ToDecimal()` - Numeric conversion

---

This API reference provides the essential information needed to work with the Segmint HL7 Generator's pharmacy workflow system. For specific implementation details, refer to the source code and unit tests.

**Document Version**: 1.0  
**Last Updated**: 2025-01-18  
**Target Systems**: HL7 v2.3 Pharmacy Workflows