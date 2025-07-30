# HL7 v2.3 Workflow Templates and Builders

This directory contains the workflow orchestration layer for HL7 v2.3 message generation, following our hybrid approach of combining static templates with fluent builders.

## Directory Structure

```
Workflows/
├── Templates/          # Static factory methods for common workflows
│   ├── PharmacyWorkflows.cs      # Pharmacy-specific workflow templates
│   ├── LabWorkflows.cs           # Laboratory workflow templates
│   └── RadiologyWorkflows.cs     # Radiology workflow templates
├── Builders/           # Fluent interface builders for complex scenarios
│   ├── PrescriptionBuilder.cs    # Prescription workflow builder
│   ├── OrderResponseBuilder.cs   # Order response workflow builder
│   └── DispenseBuilder.cs        # Dispensing workflow builder
└── Common/             # Shared workflow utilities
    ├── WorkflowValidation.cs     # Workflow validation utilities
    └── WorkflowExtensions.cs     # Extension methods for workflow operations
```

## Usage Philosophy

### **Templates (Primary)** - 80% of use cases
Static factory methods providing pre-configured workflows with compile-time safety:
```csharp
var prescription = PharmacyWorkflows.CreateNewPrescription(
    patientId: "12345",
    lastName: "Smith", 
    firstName: "John",
    medicationCode: "0069-2587-68",
    medicationName: "Lisinopril 10mg Tablet",
    // ... other parameters
);
```

### **Builders (Secondary)** - 20% of complex scenarios
Fluent interface for customization and advanced workflows:
```csharp
var prescription = PharmacyWorkflowBuilder.NewPrescription()
    .WithPatient("12345", "Smith", "John")
    .WithMedication("0069-2587-68", "Lisinopril 10mg Tablet", 10, "mg")
    .WithProvider("DEA123456", "Johnson", "Robert")
    .WithInstructions("Take 1 tablet by mouth daily", 3, 30)
    .Build();
```

## Architecture Benefits

- **Consistency**: Aligns with existing `ORMMessage.CreateLabOrder()` pattern
- **Performance**: No runtime parsing overhead for templates
- **Flexibility**: Builder pattern enables complex customization
- **CLI/GUI Compatible**: Templates for CLI, builders for GUI
- **Type Safety**: Full compile-time checking and IntelliSense support

## Integration Points

- **Messages**: Uses existing message types (RDE, ORR, RDS, ORM, ADT)
- **Segments**: Leverages all HL7 v2.3 segments
- **Validation**: Integrates with existing validation framework
- **Configuration**: Works with configuration inference system