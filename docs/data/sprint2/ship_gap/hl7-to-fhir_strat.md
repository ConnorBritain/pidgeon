# HL7 v2-to-FHIR Official Mapping Integration Strategy

**Version**: 1.0
**Date**: September 21, 2025
**Status**: Discovery Complete - Implementation Ready
**Repository**: `/v2-to-fhir-master/` (Official HL7 v2-to-FHIR mappings)

---

## 🎯 **Executive Summary**

We have discovered and analyzed the official HL7 v2-to-FHIR mapping repository, which provides **comprehensive, authoritative CSV and JSON mappings** between HL7 v2.x segments/fields and FHIR R4 resources. This goldmine of interoperability data perfectly supports our two-tier semantic path architecture, providing both the simple essential paths for 80% of users AND the complete hierarchical paths for advanced interoperability scenarios.

**Key Insight**: Instead of manually creating semantic paths, we can **auto-generate them directly from official HL7 mappings**, ensuring standards compliance and eliminating technical debt.

---

## 🔍 **Repository Discovery & Structure**

### **Location & Contents**
```
/v2-to-fhir-master/v2-to-fhir-master/
├── mappings/
│   ├── segments/           # 100+ segment-to-resource mappings
│   ├── datatypes/          # Data type conversions
│   ├── codesystems/        # Vocabulary mappings
│   ├── messages/           # Complete message structure mappings
│   └── *.csv              # Inventory files listing all mappings
├── input/
│   └── pagecontent/        # Documentation and guides
└── samples/                # Example messages and transformations
```

### **Key Mapping Files Discovered**

#### **Segment Mappings** (Primary source for semantic paths):
- **PID[Patient]**: 30+ field mappings including identifiers, demographics, contact info
- **PV1[Encounter]**: 50+ field mappings for visits, locations, providers
- **OBX[Observation]**: Lab results and clinical observations
- **ORC/OBR[ServiceRequest]**: Orders and requests
- **DG1[Condition]**: Diagnoses and problems
- **AL1/IAM[AllergyIntolerance]**: Allergy information
- **IN1[Coverage]**: Insurance and billing

#### **Mapping Format** (CSV Structure):
```csv
Sort Order,Identifier,Name,Data Type,Cardinality,Condition,FHIR Attribute,Data Type,Mapping,Comments
3,PID-3,Patient Identifier List,CX,1,-1,,,identifier[2],Identifier,CX[Identifier]
5,PID-5,Patient Name,XPN,1,-1,,,name[1],HumanName,XPN[HumanName]
7,PID-7,Date/Time of Birth,DTM,0,1,,,birthDate,date
```

### **Rich Metadata Available**:
1. **Conditional Logic**: IF/THEN statements for context-aware mappings
2. **Cardinality**: Min/max occurrences for both HL7 and FHIR
3. **Data Type Mappings**: Explicit conversion rules (e.g., CX→Identifier)
4. **Vocabulary Mappings**: Code system translations
5. **Implementation Comments**: Guidance on edge cases and variations

---

## 🏗️ **Two-Tier Semantic Path Extraction**

### **Tier 1: Essential Paths (Direct Extraction)**

From analyzing the PID→Patient mapping, we can extract the following essential semantic paths that cover 80% of testing scenarios:

```yaml
# PATIENT CORE (from PID segment)
patient.mrn:                 PID-3 → Patient.identifier[2]
patient.name:                PID-5 → Patient.name[1]
patient.firstName:           PID-5.2 → Patient.name[1].given[0]
patient.lastName:            PID-5.1 → Patient.name[1].family
patient.dateOfBirth:         PID-7 → Patient.birthDate
patient.sex:                 PID-8 → Patient.gender
patient.address:             PID-11 → Patient.address[1]
patient.phone:               PID-13 → Patient.telecom[1] (use="home")
patient.insurance.member:    IN1-36 → Coverage.identifier

# ENCOUNTER CORE (from PV1 segment)
encounter.location:          PV1-3 → Encounter.location[1].location
encounter.type:              PV1-4 → Encounter.type
encounter.class:             PV1-2 → Encounter.class
encounter.date:              PV1-44 → Encounter.period.start
encounter.account:           PID-18 → Account.identifier

# PROVIDER CORE (from PV1/ORC segments)
provider.name:               PV1-7 → Practitioner.name
provider.npi:                PV1-7.1 → Practitioner.identifier

# MESSAGE CONTROL (from MSH segment)
message.timestamp:           MSH-7 → MessageHeader.timestamp
message.facility:            MSH-4 → MessageHeader.sender
message.application:         MSH-3 → MessageHeader.source.name
```

### **Tier 2: Advanced Paths (Hierarchical Extraction)**

The repository reveals complex hierarchical paths perfect for advanced interoperability:

```yaml
# ADVANCED PATIENT IDENTIFIERS (from PID-3 components)
patient.identifiers.mrn:                          PID-3 → Patient.identifier
patient.identifiers.mrn.value:                    PID-3.1 → Patient.identifier.value
patient.identifiers.mrn.assigning_authority:      PID-3.4 → Patient.identifier.assigner
patient.identifiers.mrn.assigning_authority.name: PID-3.4.1 → Patient.identifier.assigner.display
patient.identifiers.mrn.type:                     PID-3.5 → Patient.identifier.type

# ADVANCED DEMOGRAPHICS (from extended PID fields)
patient.demographics.race:                        PID-10 → US Core Race Extension
patient.demographics.race.coding.code:            PID-10.1 → extension.valueCoding.code
patient.demographics.race.coding.system:          PID-10.3 → extension.valueCoding.system
patient.demographics.ethnicity:                   PID-22 → US Core Ethnicity Extension
patient.demographics.mothers_maiden_name:         PID-6 → mothersMaidenName extension
patient.demographics.birth_sex:                   PID-8 → US Core Birth Sex

# ADVANCED CONTACT INFO (from PID components)
patient.contact.phone.home:                       PID-13 → telecom[use="home"]
patient.contact.phone.work:                       PID-14 → telecom[use="work"]
patient.contact.phone.mobile:                     PID-13/14 → telecom[use="mobile"]
patient.contact.address.home.line1:               PID-11.1 → address.line[0]
patient.contact.address.home.city:                PID-11.3 → address.city
patient.contact.address.home.state:               PID-11.4 → address.state
patient.contact.address.home.postal_code:         PID-11.5 → address.postalCode

# CONDITIONAL MAPPINGS (from IF/THEN logic)
encounter.status:                                 IF PV1-2="P" THEN "planned" ELSE "in-progress"
patient.birthTime:                                IF LENGTH(PID-7)>8 THEN birthTime extension
encounter.location.prior:                         PV1-6 → location[status="completed"]
```

---

## 💡 **Implementation Strategy**

### **Phase 1: CSV Parser & Path Generator (Week 1)**

#### **1. Create Mapping Parser Service**
```csharp
public class V2ToFhirMappingParser
{
    public async Task<IReadOnlyList<SemanticPathDefinition>> ParseSegmentMappingAsync(string csvPath)
    {
        // Parse CSV structure
        // Extract HL7 field → FHIR attribute mappings
        // Generate semantic paths with tier classification
        // Handle conditional logic and cardinality
    }
}
```

#### **2. Tier Classification Algorithm**
```csharp
public SemanticPathTier ClassifyPath(string hl7Field, string fhirPath)
{
    // Essential: Common demographics, core identifiers
    var essentialFields = new[] { "PID-3", "PID-5", "PID-7", "PID-8", "PV1-3", "PV1-2" };

    // Essential: First level paths (patient.mrn, encounter.location)
    if (essentialFields.Contains(hl7Field) && !fhirPath.Contains("extension"))
        return SemanticPathTier.Essential;

    // Advanced: Extensions, complex hierarchies, conditional logic
    return SemanticPathTier.Advanced;
}
```

#### **3. Auto-Generate Plugins from Mappings**
```csharp
public class OfficialMappingSemanticPathPlugin : ISemanticPathPlugin
{
    private readonly Dictionary<string, SemanticPathDefinition> _paths;

    public OfficialMappingSemanticPathPlugin()
    {
        // Load from parsed CSV files
        _paths = LoadOfficialMappings();
    }

    public async Task<Result<SemanticPathResolution>> ResolvePathAsync(
        string semanticPath,
        string messageType,
        SemanticPathContext context)
    {
        // Use official mappings to resolve paths
        // Support both HL7→FHIR and FHIR→HL7 directions
    }
}
```

### **Phase 2: Advanced Hierarchy & Conditional Logic (Week 2)**

#### **1. Conditional Mapping Handler**
```csharp
public class ConditionalMappingEngine
{
    public string EvaluateCondition(string condition, Message context)
    {
        // Parse IF/THEN logic from CSV
        // "IF PV1-2.1 NOT EQUALS 'P'" → check actual value
        // Return appropriate FHIR mapping based on condition
    }
}
```

#### **2. Hierarchy Builder**
```csharp
public class PathHierarchyBuilder
{
    public SemanticPathDefinition BuildHierarchy(string basePath, List<ComponentMapping> components)
    {
        // Build patient.identifiers.mrn.assigning_authority from PID-3 components
        // Create nested path structure from HL7 component mappings
        // Support drilling down into complex types
    }
}
```

### **Phase 3: Cross-Standard Support (Week 3)**

#### **1. Bidirectional Mapping Engine**
```csharp
public interface IBidirectionalMapper
{
    // HL7 → FHIR
    Task<string> MapHL7ToFHIRAsync(string hl7Path, string value);

    // FHIR → HL7
    Task<string> MapFHIRToHL7Async(string fhirPath, string value);

    // Semantic → Both
    Task<CrossStandardMapping> ResolveSemanticPathAsync(string semanticPath);
}
```

---

## 🎯 **Benefits of Official Mapping Integration**

### **Immediate Benefits**
1. **Zero Manual Path Creation**: All paths auto-generated from authoritative source
2. **Standards Compliance**: Using official HL7 Working Group mappings
3. **Rich Metadata**: Comments, conditions, cardinality included automatically
4. **Complete Coverage**: 100+ segments already mapped by HL7 experts

### **Long-Term Benefits**
1. **Future-Proof**: Updates automatically when HL7 releases new mappings
2. **Community Trust**: Using the same mappings as major EHR vendors
3. **Reduced Maintenance**: No need to manually maintain path definitions
4. **Cross-Standard Consistency**: Official mappings ensure correct translations

---

## 📊 **Mapping Coverage Analysis**

### **Currently Available in Repository**

| Category | Segments | Fields | Tier 1 Paths | Tier 2 Paths |
|----------|----------|--------|--------------|--------------|
| **Patient Demographics** | PID, NK1, PD1 | 50+ | 10 | 40+ |
| **Encounters** | PV1, PV2 | 60+ | 5 | 55+ |
| **Orders/Results** | ORC, OBR, OBX | 80+ | 8 | 70+ |
| **Diagnoses** | DG1, PR1 | 30+ | 3 | 25+ |
| **Insurance** | IN1, IN2, IN3 | 40+ | 2 | 35+ |
| **Allergies** | AL1, IAM | 20+ | 2 | 15+ |
| **Message Control** | MSH, MSA, EVN | 30+ | 3 | 25+ |

**Total**: ~300+ field mappings ready for semantic path generation

### **Path Generation Estimates**

From the official mappings, we can generate:
- **25-30 Essential Paths** (Tier 1): Core fields used in 80% of scenarios
- **250+ Advanced Paths** (Tier 2): Complete hierarchical coverage
- **500+ Conditional Paths**: Context-aware mappings with IF/THEN logic

---

## 🚀 **Implementation Roadmap**

### **Week 1: Foundation & Parser**
- [ ] Create CSV parsing service for official mappings
- [ ] Build tier classification algorithm
- [ ] Generate initial 25-30 essential paths
- [ ] Create mapping data structures

### **Week 2: Advanced Features**
- [ ] Parse conditional logic (IF/THEN statements)
- [ ] Build hierarchical path structures
- [ ] Support component-level mappings (PID-3.4.1)
- [ ] Add US Core extension support

### **Week 3: Integration & Testing**
- [ ] Integrate with PathCommand
- [ ] Add fuzzy search across generated paths
- [ ] Performance optimization (<1ms lookups)
- [ ] Comprehensive testing with real messages

### **Week 4: Polish & Documentation**
- [ ] Complete two-tier CLI experience
- [ ] Generate documentation from mappings
- [ ] Add examples from actual conversions
- [ ] Performance validation

---

## 📝 **Key Files to Parse (Priority Order)**

### **Essential (Tier 1) - Parse First**
1. `HL7 Segment - FHIR R4_ PID[Patient] - PID.csv` - Patient demographics
2. `HL7 Segment - FHIR R4_ PV1[Encounter] - PV1.csv` - Encounter information
3. `HL7 Segment - FHIR R4_ MSH[MessageHeader] - R4.csv` - Message control
4. `HL7 Segment - FHIR R4_ IN1[Coverage] - Sheet1.csv` - Insurance basics
5. `HL7 Segment - FHIR R4_ ORC[ServiceRequest] - ORC.csv` - Order information

### **Advanced (Tier 2) - Parse Second**
1. `HL7 Segment - FHIR R4_ OBX[Observation] - OBX.csv` - Lab results
2. `HL7 Segment - FHIR R4_ DG1[Condition] - Sheet1.csv` - Diagnoses
3. `HL7 Segment - FHIR R4_ AL1[AllergyIntolerance] - AL1.csv` - Allergies
4. `HL7 Segment - FHIR R4_ NK1[RelatedPerson] - Sheet1.csv` - Next of kin
5. Complete data type mappings from `/datatypes/` directory

---

## 🎯 **Success Criteria**

1. **Tier 1 Success**: 25-30 essential paths auto-generated and working
2. **Tier 2 Success**: 250+ advanced paths available with --complete flag
3. **Standards Compliance**: All paths match official HL7 mappings exactly
4. **Performance**: <1ms essential path lookup, <5ms advanced path lookup
5. **Migration Testing**: Successfully test HL7→FHIR conversion scenarios

---

## 💡 **Next Steps**

1. **Update Design Document**: Add official mapping integration architecture
2. **Update Dev Document**: Revise implementation phases for CSV parsing
3. **Begin Parser Implementation**: Start with PID and PV1 segments
4. **Create Path Generator**: Auto-generate semantic paths from mappings
5. **Test with Real Data**: Validate against actual HL7/FHIR messages

---

**This strategy leverages official HL7 standards work to create a robust, compliant, and maintainable semantic path system that serves both simple and advanced users effectively.**