**Date**: September 2, 2025  
**Decision Type**: Architectural Foundation  
**Impact**: Critical - affects all standard adapters and domain boundaries  

### **Context**
During P0.1 domain boundary violation fixes, encountered decision point for DTO strategy:
- **Option A**: Shared DTOs (PatientDto used by HL7, FHIR, NCPDP)
- **Option B**: Standard-specific DTOs (HL7PatientDto, FHIRPatientDto, NCPDPPatientDto)  
- **Option C**: Hybrid approach (core DTOs + standard-specific extensions)

### **Decision: Hybrid DTO Strategy**

#### **Core Shared DTOs** (Universal Healthcare Concepts)
```csharp
// Application/DTOs/PatientDto.cs - Universal patient demographics
PatientDto: Id, Name, DOB, Gender, Address, Phone, Race, Language, etc.

// Application/DTOs/PrescriptionDto.cs - Universal prescription data  
PrescriptionDto: Id, Patient, Medication, Dosage, Prescriber, Instructions, etc.
```

#### **Standard-Specific Extensions** (Future)
```csharp
// Application/DTOs/Extensions/HL7PatientExtensions.cs
static class HL7PatientExtensions {
    static HL7SpecificFields GetHL7Fields(this PatientDto patient) { ... }
}

// Application/DTOs/Extensions/FHIRPatientExtensions.cs  
static class FHIRPatientExtensions {
    static FHIRSpecificFields GetFHIRFields(this PatientDto patient) { ... }
}
```

### **Implementation Pattern**
```csharp
// Clinical → DTO conversion (Application layer)
PatientDto patientDto = clinicalPatient.ToDto();

// Each standard uses what it needs from shared DTO
hl7Adapter.CreatePID(patientDto);     // Uses: Id, Name, DOB, Gender
fhirAdapter.CreatePatient(patientDto); // Uses: All fields + FHIR extensions
ncpdpAdapter.CreatePatient(patientDto); // Uses: Id, Name, DOB only
```

### **Benefits**
- **DRY Compliance**: No duplication of universal healthcare concepts
- **Standards Agnostic**: Core DTOs work with any healthcare standard
- **Extensible**: Standard-specific extensions without core DTO pollution
- **Testable**: Single DTO pattern, multiple standard outputs
- **Universal Platform**: Aligns with "AI-augmented universal healthcare standards platform" mission

### **Trade-offs**
- **Complexity**: Hybrid approach requires more design coordination
- **DTO Size**: Shared DTOs contain fields not used by all standards
- **Extension Management**: Need discipline to use extensions vs core DTO expansion

### **Alternative Approaches Considered**
- **Standard-Specific DTOs**: Rejected - massive duplication, violates DRY
- **Single Monolithic DTO**: Rejected - would become unwieldy with 100+ FHIR fields
- **No DTOs**: Rejected - violates domain boundary separation

### **Implementation Status**
- ✅ Core DTOs created (PatientDto, PrescriptionDto, etc.)
- ✅ Clinical→DTO conversions implemented (DtoConversions.cs)
- 🔄 Infrastructure layer conversion integration (in progress)
- ⏳ Standard-specific extensions (future P1+ work)

**Dependencies**: P0.1 domain boundary violation fixes  
**Rollback**: Remove DTOs, revert to direct Clinical entity usage (not recommended)

---

**LEDGER Principles**:
1. **Every significant decision gets documented**
2. **Rollback procedures are mandatory for architectural changes**
3. **Code examples are required for implementation decisions**
4. **Dependencies must be tracked for impact analysis**
5. **Alternative approaches must be documented with rejection reasons**

*This LEDGER serves as the single source of truth for all architectural and implementation decisions. When in doubt, refer to the LEDGER. When making changes, update the LEDGER.*