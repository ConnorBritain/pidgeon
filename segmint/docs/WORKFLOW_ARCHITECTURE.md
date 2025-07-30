# Workflow Architecture - Directory Structure

*Created: July 17, 2025*

## 🏗️ **Scalable Workflow Organization**

We have designed a scalable directory structure that organizes workflow templates and builders across multiple healthcare standards, ensuring consistency and future growth.

## 📁 **Complete Directory Structure**

```
/src/Segmint.Core/Standards/
├── Common/                          # Cross-standard interfaces
│   ├── IStandardConfig.cs
│   ├── IStandardField.cs
│   ├── IStandardMessage.cs
│   └── IStandardValidator.cs
├── HL7/v23/                         # HL7 v2.3 Implementation
│   ├── Messages/                    # Message types (RDE, ORR, RDS, ORM, ADT)
│   ├── Segments/                    # Segment types (PID, MSH, ORC, RXO, etc.)
│   ├── Types/                       # Field types (PersonName, Timestamp, etc.)
│   ├── Validation/                  # Validation framework
│   └── Workflows/                   # 🆕 Workflow orchestration layer
│       ├── Templates/               # Static factory methods
│       │   ├── PharmacyWorkflows.cs     # Pharmacy workflow templates
│       │   ├── LabWorkflows.cs          # Lab workflow templates
│       │   └── RadiologyWorkflows.cs    # Radiology workflow templates
│       ├── Builders/                # Fluent interface builders
│       │   ├── PrescriptionBuilder.cs   # Prescription builder
│       │   ├── OrderResponseBuilder.cs  # Response builder
│       │   └── DispenseBuilder.cs       # Dispense builder
│       └── Common/                  # Shared workflow utilities
│           ├── WorkflowValidation.cs    # Validation utilities
│           └── WorkflowExtensions.cs    # Extension methods
├── FHIR/R4/                         # 🔮 Future FHIR R4 Support
│   ├── README.md
│   └── Workflows/                   # Future FHIR workflows
│       ├── Templates/               # FHIR workflow templates
│       ├── Builders/                # FHIR resource builders
│       └── Common/                  # FHIR workflow utilities
└── NCPDP/XML/                       # 🔮 Future NCPDP Support
    ├── README.md
    └── Workflows/                   # Future NCPDP workflows
        ├── Templates/               # NCPDP workflow templates
        ├── Builders/                # NCPDP transaction builders
        └── Common/                  # NCPDP workflow utilities
```

## 🎯 **Design Principles**

### **1. Standard-Specific Organization**
- Each healthcare standard (HL7, FHIR, NCPDP) has its own directory
- Version-specific subdirectories (v23, R4, XML)
- Consistent structure across all standards

### **2. Hybrid Approach Separation**
- **Templates/**: Static factory methods for common workflows (80% of use cases)
- **Builders/**: Fluent interface builders for complex scenarios (20% of use cases)
- **Common/**: Shared utilities and extensions

### **3. Scalable Growth**
- Future standards can be added with consistent structure
- Standard-specific workflows without cross-contamination
- Shared patterns across standards where applicable

### **4. Architecture Consistency**
- Follows existing codebase patterns
- Maintains separation of concerns
- Integrates with existing validation and message frameworks

## 🔄 **Workflow Type Organization**

### **HL7 v2.3 Workflows (Current)**
- **PharmacyWorkflows**: Prescription, dispensing, order response workflows
- **LabWorkflows**: Laboratory order and result workflows
- **RadiologyWorkflows**: Radiology order and report workflows

### **FHIR R4 Workflows (Future)**
- **MedicationWorkflows**: MedicationRequest, MedicationDispense workflows
- **PatientWorkflows**: Patient resource management workflows
- **EncounterWorkflows**: Encounter and episode workflows

### **NCPDP XML Workflows (Future)**
- **ClaimWorkflows**: Pharmacy claim processing workflows
- **EligibilityWorkflows**: Real-time benefits verification workflows
- **PriorAuthWorkflows**: Prior authorization workflows

## 🔧 **Implementation Strategy**

### **Phase 1: HL7 Templates (Current)**
```csharp
// Static factory methods for common workflows
public static class PharmacyWorkflows
{
    public static RDEMessage CreateNewPrescription(...)
    public static ORRMessage CreateAcceptanceResponse(...)
    public static RDSMessage CreateDispenseRecord(...)
}
```

### **Phase 2: HL7 Builders (Current)**
```csharp
// Fluent interface for complex scenarios
public class PrescriptionBuilder
{
    public PrescriptionBuilder WithPatient(...)
    public PrescriptionBuilder WithMedication(...)
    public RDEMessage Build()
}
```

### **Phase 3: Future Standards (Planned)**
- Apply same hybrid pattern to FHIR R4
- Apply same hybrid pattern to NCPDP XML
- Maintain consistent API patterns across standards

## 🎨 **Integration Points**

### **CLI Integration**
- Templates provide efficient scripting interface
- Builders enable interactive scenarios
- Both integrate with existing CLI commands

### **GUI Integration**
- Templates for quick workflow execution
- Builders for form-based workflow construction
- Visual workflow designer potential

### **Validation Integration**
- Workflows leverage existing validation framework
- Standard-specific validation rules
- Workflow-level validation for complex scenarios

## 📊 **Benefits of This Structure**

1. **Consistency**: Same patterns across all standards
2. **Scalability**: Easy to add new standards and workflows
3. **Maintainability**: Clear separation of concerns
4. **Performance**: Templates provide zero-overhead workflows
5. **Flexibility**: Builders enable complex customization
6. **Future-Proof**: Structure supports long-term growth

## 🚀 **Current Status**

- ✅ **Directory Structure**: Complete and documented
- ✅ **Documentation**: READMEs for all standards
- ✅ **Architecture**: Hybrid approach approved
- 🔄 **Implementation**: Ready to begin PharmacyWorkflows development

This architecture provides a solid foundation for implementing workflow templates and builders across all healthcare standards, ensuring consistency while enabling future growth and customization.