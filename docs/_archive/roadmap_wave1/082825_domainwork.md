# Domain Buildout Strategy - Phase Implementation Plan
**Date**: August 28, 2025  
**Status**: 🚀 EXECUTION READY - Following Sacred Principles  
**Priority**: CRITICAL - Foundation for all P0 MVP features  
**Authority**: INIT Sacred Principles + Wave 1 V1 MVP Commitment

---

## 🎯 **Strategic Foundation: Sacred Principles Alignment**

### **INIT Sacred Principles Compliance**
Following `docs/roadmap/INIT.md` immutable architectural principles:

**SACRED PRINCIPLE #1: Four-Domain Architecture Foundation**
- **Clinical**: Healthcare business concepts (Patient, Prescription, Provider)
- **Messaging**: Wire format structures (HL7_ORM_Message, FHIR_Bundle)
- **Configuration**: Vendor patterns (VendorConfiguration, FieldPattern)
- **Transformation**: Mapping rules (MappingRule, TransformationSet)

**SACRED PRINCIPLE #2: Plugin Architecture**
- New standards as plugins, never breaking existing code
- Standards-specific implementations stay in `Standards/` folder
- Domain adapters translate between bounded contexts

**SACRED PRINCIPLE #3: Dependency Injection Throughout**
- No static classes for business logic
- All services injectable and testable
- Result<T> pattern for explicit error handling

### **P0 MVP Feature Requirements**
Domain buildout must directly support validated user stories:

1. **P0 #1 (Message Generation)** - Clinical → HL7/FHIR/NCPDP transformations
2. **P0 #2 (Message Validation)** - Parse and validate with detailed error reporting  
3. **P0 #3 (Vendor Pattern Detection)** - Configuration analysis from message samples
4. **P0 #4 (Format Error Debugging)** - Rich parsing errors with suggestions
5. **P0 #5 (Synthetic Test Data)** - Realistic healthcare data generation

---

## 📋 **Phase-Based Implementation Strategy**

### **Phase 1: File Organization (Sacred Principle Alignment)**
**Priority**: IMMEDIATE  
**Risk**: LOW (file moves, namespace updates)  
**Duration**: 1 hour  
**P0 Support**: Foundation for all features  

#### **Current State Analysis**
```yaml
Current Incorrect Structure:
  Domain/
    DataTypes/Universal/ → Should be Clinical/Entities/
    DataTypes/HL7/       → Should be Messaging/HL7v2/DataTypes/
    Messages/HL7/        → Should be Messaging/HL7v2/Messages/
    Messages/FHIR/       → Should be Messaging/FHIR/Bundles/
    Messages/NCPDP/      → Should be Messaging/NCPDP/Transactions/
  Types/                 → Should be Configuration/Entities/
  Services/Configuration/ → Should be Configuration/Services/
```

#### **Target Sacred Structure**
```yaml
Target Four-Domain Structure:
  Domain/
    Clinical/
      Entities/
        - Patient.cs       (MOVE from DataTypes/Universal/)
        - Provider.cs      (MOVE from DataTypes/Universal/)
        - Medication.cs    (MOVE from DataTypes/Universal/)
      ValueObjects/        (CREATE new)
    Messaging/
      HL7v2/
        Messages/
          - HL7Message.cs  (MOVE from Messages/HL7/)
        DataTypes/
          - CX_*.cs        (MOVE from DataTypes/HL7/)
        Segments/          (REFACTOR from Standards/HL7/v23/)
      FHIR/
        Bundles/
          - FHIRBundle.cs  (MOVE from Messages/FHIR/)
        Resources/         (CREATE new)
      NCPDP/
        Transactions/
          - NCPDPTransaction.cs (MOVE from Messages/NCPDP/)
    Configuration/
      Entities/
        - VendorConfiguration.cs (MOVE from Types/)
        - ConfigurationAddress.cs (MOVE from Types/)
        - FieldPatterns.cs (MOVE from Types/)
      Services/
        - (MOVE all from Services/Configuration/)
    Transformation/
      Entities/            (CREATE new)
      Services/            (CREATE new)
  Adapters/
    Interfaces/            (EXISTS - already correct!)
    Implementation/        (ENHANCE existing)
```

#### **Phase 1 Todo List**
- [ ] **Create directory structure** following sacred four-domain pattern
- [ ] **Move Clinical entities** (Patient, Provider, Medication) to Clinical/Entities/
- [ ] **Move HL7 DataTypes** to Messaging/HL7v2/DataTypes/
- [ ] **Move Message classes** to appropriate Messaging subdomains
- [ ] **Move Configuration types** from Types/ to Configuration/Entities/
- [ ] **Move Configuration services** to Configuration/Services/
- [ ] **Update all namespaces** to match sacred principle structure
- [ ] **Fix import references** across entire solution
- [ ] **Verify clean build** after file moves

### **Phase 2: Complete Clinical Domain**
**Priority**: HIGH  
**Risk**: MEDIUM (new entities, enhancements)  
**Duration**: 2 hours  
**P0 Support**: #1 (Message Generation), #5 (Synthetic Test Data)  

#### **Clinical Domain Completion Requirements**
**Sacred Principle Compliance**: Clinical domain must have zero dependencies on other domains

#### **Missing Entities Analysis**
```csharp
Required for P0 #1 & #5:
- Encounter.cs      (MISSING - needed for ADT message generation)
- Complete Prescription.cs (PARTIAL - needs Patient+Provider+Medication linking)

Value Objects (MISSING - needed for realistic data):
- PersonName.cs     (extract from XPN concepts)
- Address.cs        (extract from XAD concepts)
- DosageInstructions.cs (for prescription details)
- PhoneNumber.cs    (for contact information)
```

#### **Phase 2 Todo List**
- [ ] **Create Encounter entity** with admission/discharge/transfer semantics
- [ ] **Enhance Prescription entity** to properly link Patient+Provider+Medication
- [ ] **Create PersonName value object** (Family, Given, Middle components)
- [ ] **Create Address value object** (Street, City, State, Postal, Country)
- [ ] **Create DosageInstructions value object** (Dose, Frequency, Route, Duration)
- [ ] **Create PhoneNumber value object** with validation
- [ ] **Add clinical validation** using Result<T> pattern throughout
- [ ] **Create domain factories** for realistic test data generation
- [ ] **Implement age calculation** and demographic methods on Patient
- [ ] **Add clinical business rules** (prescription validation, encounter states)

### **Phase 3: Complete HL7 Message Structures**
**Priority**: HIGH  
**Risk**: MEDIUM (well-defined specs, existing patterns)  
**Duration**: 3 hours  
**P0 Support**: #1 (Generation), #2 (Validation), #4 (Debugging)  

#### **Message Structure Requirements**
**Sacred Principle Compliance**: Messaging domain depends only on Clinical domain (via adapters)

#### **Missing Message Structures Analysis**
```csharp
Current State:
- GenericHL7Message.cs (EXISTS - basic structure)
- MSH_Segment.cs (EXISTS - in Standards/HL7/v23/)
- PID_Segment.cs (EXISTS - in Standards/HL7/v23/)

Required for P0 Features:
- HL7_ADT_Message.cs (MISSING - needed for patient admission scenarios)
- HL7_RDE_Message.cs (MISSING - needed for prescription scenarios)
- HL7_ORU_Message.cs (MISSING - needed for result scenarios)
- Complete PV1_Segment.cs (PARTIAL - needs all 52 fields)
- Complete ORC_Segment.cs (PARTIAL - needs all order control fields)
- Complete RXE_Segment.cs (MISSING - pharmacy encoding)
```

#### **Phase 3 Todo List**
- [ ] **Create HL7_ADT_Message** with MSH+EVN+PID+PV1 structure
- [ ] **Create HL7_RDE_Message** with MSH+PID+ORC+RXE+RXR structure
- [ ] **Create HL7_ORU_Message** with MSH+PID+OBR+OBX structure
- [ ] **Complete PV1_Segment** with all 52 fields for patient visit data
- [ ] **Complete ORC_Segment** with all order control fields
- [ ] **Create RXE_Segment** for pharmacy encoding (25 fields)
- [ ] **Create RXR_Segment** for pharmacy route (6 fields)
- [ ] **Create OBR_Segment** for observation request (47 fields)
- [ ] **Create OBX_Segment** for observation/result (17 fields)
- [ ] **Add message validation** with detailed error reporting for P0 #4
- [ ] **Create message builders** for fluent message construction
- [ ] **Add serialization/parsing** with proper error handling

### **Phase 4: Adapter Implementations**
**Priority**: HIGH  
**Risk**: MEDIUM (interfaces exist, clear requirements)  
**Duration**: 2 hours  
**P0 Support**: #1 (Generation), #3 (Vendor Patterns), Integration  

#### **Adapter Implementation Requirements**
**Sacred Principle Compliance**: Adapters translate between domains without leaking concepts

#### **Current Adapter State Analysis**
```csharp
Interfaces (EXISTS):
- IClinicalToMessagingAdapter     ✅ Complete interface
- IMessagingToClinicalAdapter     ✅ Complete interface  
- IMessagingToConfigurationAdapter ✅ Complete interface

Implementations:
- ClinicalToMessagingAdapter      ❌ MISSING (critical for P0 #1)
- MessagingToClinicalAdapter      ❌ MISSING (needed for extraction)
- HL7ToConfigurationAdapter       ✅ EXISTS but needs enhancement
```

#### **Phase 4 Todo List**
- [ ] **Implement ClinicalToMessagingAdapter** for HL7 message generation
  - [ ] CreateAdmissionMessageAsync (Patient+Encounter → ADT)
  - [ ] CreatePrescriptionOrderAsync (Prescription → RDE)
  - [ ] CreateFHIRBundleAsync (Patient+Prescriptions → Bundle)
  - [ ] CreateNCPDPTransactionAsync (Prescription → NewRx)
- [ ] **Implement MessagingToClinicalAdapter** for clinical extraction
  - [ ] ExtractPatientFromHL7Async (ADT → Patient)
  - [ ] ExtractPrescriptionFromHL7Async (RDE → Prescription)
  - [ ] ExtractEncounterFromHL7Async (ADT → Encounter)
- [ ] **Enhance HL7ToConfigurationAdapter** for P0 #3
  - [ ] Complete AnalyzePatternsAsync implementation
  - [ ] Add vendor signature detection logic
  - [ ] Implement deviation detection algorithms
- [ ] **Add comprehensive error handling** with Result<T> pattern
- [ ] **Create adapter unit tests** covering P0 user scenarios
- [ ] **Add adapter integration tests** with real healthcare data patterns

---

## 🎯 **Success Criteria & Validation**

### **Phase Completion Gates**

#### **Phase 1 Success Criteria:**
- [ ] Clean build with zero compilation errors
- [ ] All files in correct four-domain structure
- [ ] All namespaces follow sacred principle patterns
- [ ] No broken references across solution

#### **Phase 2 Success Criteria:**
- [ ] Complete Clinical domain entities with validation
- [ ] Rich value objects for realistic data generation
- [ ] Clinical domain has zero external dependencies
- [ ] Can generate varied patient/prescription scenarios

#### **Phase 3 Success Criteria:**
- [ ] Generate valid HL7 messages for ADT, RDE, ORU scenarios
- [ ] Parse existing HL7 messages with detailed error reporting
- [ ] Message structures support all P0 user scenarios
- [ ] Performance targets: <50ms for message processing

#### **Phase 4 Success Criteria:**
- [ ] Complete Clinical → HL7 message generation pipeline
- [ ] End-to-end vendor pattern detection working
- [ ] All P0 features have working domain model support
- [ ] Integration tests pass with real healthcare scenarios

### **P0 Feature Validation Tests**

#### **P0 #1 - Message Generation Test:**
```csharp
// Must pass after Phase 4:
var patient = PatientFactory.CreateRandomPatient();
var prescription = PrescriptionFactory.Create(patient, medication, provider);
var adapter = serviceProvider.GetService<IClinicalToMessagingAdapter>();
var hl7Message = await adapter.CreatePrescriptionOrderAsync(prescription);
// Result: Valid RDE message with all required segments
```

#### **P0 #3 - Vendor Pattern Detection Test:**
```csharp
// Must pass after Phase 4:
var epicMessages = LoadEpicSampleMessages();
var adapter = serviceProvider.GetService<IMessagingToConfigurationAdapter>();
var config = await adapter.InferConfigurationAsync(epicMessages);
// Result: Detected "Epic" vendor with >85% confidence
```

---

## 🚨 **Risk Mitigation & Rollback Plans**

### **Phase 1 Risks:**
- **Risk**: Namespace changes break references
- **Mitigation**: Use IDE refactoring tools, validate build frequently
- **Rollback**: Git revert, all changes are file moves only

### **Phase 2 Risks:**
- **Risk**: Clinical domain becomes too complex
- **Mitigation**: Focus only on P0 requirements, defer nice-to-have features
- **Rollback**: Remove new entities, keep enhanced existing ones

### **Phase 3 Risks:**
- **Risk**: HL7 message complexity exceeds time estimates  
- **Mitigation**: Implement core segments first, add fields incrementally
- **Rollback**: Use existing GenericHL7Message for complex scenarios

### **Phase 4 Risks:**
- **Risk**: Adapter implementations are more complex than expected
- **Mitigation**: Start with simplest transformations, add complexity gradually
- **Rollback**: Return NotImplementedException with TODO markers

---

## 📊 **Progress Tracking**

### **Phase 1 Progress:**
- [ ] Directory structure creation
- [ ] File moves completed  
- [ ] Namespace updates applied
- [ ] Build verification passed

### **Phase 2 Progress:**
- [ ] Encounter entity created
- [ ] Prescription entity enhanced
- [ ] Value objects implemented
- [ ] Clinical validation added

### **Phase 3 Progress:**
- [ ] ADT message structure complete
- [ ] RDE message structure complete
- [ ] Core segments implemented
- [ ] Message validation working

### **Phase 4 Progress:**
- [ ] Clinical→Messaging adapter implemented
- [ ] Messaging→Configuration adapter enhanced
- [ ] Integration tests passing
- [ ] P0 features fully supported

---

## 🎯 **Post-Implementation Validation**

### **Sacred Principles Compliance Check:**
- [ ] Four-domain architecture properly implemented
- [ ] Plugin architecture boundaries maintained  
- [ ] Dependency injection used throughout
- [ ] No static classes in business logic
- [ ] Result<T> pattern used for error handling

### **P0 Feature Readiness Check:**
- [ ] P0 #1: Can generate realistic HL7/FHIR/NCPDP messages
- [ ] P0 #2: Can validate messages with detailed error reporting
- [ ] P0 #3: Can detect vendor patterns from message samples
- [ ] P0 #4: Can provide debugging information for format errors
- [ ] P0 #5: Can generate synthetic test datasets

### **Business Model Alignment Check:**
- [ ] Core features work without paid dependencies
- [ ] Professional features clearly separated
- [ ] Enterprise features properly tiered
- [ ] Architecture supports planned Core+ strategy

---

## ✅ **COMPLETION STATUS: Domain Architecture Strategy Established**

### **Architectural Foundation Complete (August 28, 2025)**

#### **🏗️ Messaging Domain Organizational Strategy - APPROVED & DOCUMENTED**

**Three Critical Questions Resolved**:
1. **Q1: Versioning Strategy** → **Explicit Versioning** (HL7v23Plugin, HL7v24Plugin, FHIRv4Plugin)
2. **Q2: Shared Components** → **Hybrid Approach** (Common segments in Domain, version overrides in Standards)  
3. **Q3: Domain Purpose** → **Wire Format Structures** (Direct Clinical → HL7/FHIR mapping for P0 simplicity)

**Documentation Updated**:
- ✅ **Domain Model Architecture**: `docs/arch_planning/pidgeon_domain_model.md` enhanced with comprehensive messaging domain strategy
- ✅ **LEDGER.md**: ARCH-023 entry documenting architectural decision and rationale
- ✅ **Implementation Plan**: Clear directory structure and organizational principles established

**Target Architecture Approved**:
```
Domain/Messaging/
├── HL7v2/ (shared segments, version-agnostic)
├── FHIR/ (R4 resources, future R5 support)  
└── NCPDP/ (transaction structures)

Standards/ (version-specific plugins)
├── HL7v23Plugin/ (parsing, validation, serialization)
├── FHIRv4Plugin/ (business logic)
└── NCPDPPlugin/ (version-specific logic)
```

**Sacred Principles Compliance**: ✅ All four principles maintained with enhanced plugin architecture support

#### **Ready for Implementation Phase**

**Phase 3 Objectives Updated**:
- ✅ **Foundation Complete**: Organizational strategy established and documented
- 🚀 **Next Action**: Begin messaging domain restructuring following approved architecture
- 🎯 **Implementation Path**: Restructure existing HL7 files → Complete message structures → Adapter implementations

**Success Criteria Established**:
- Consistent naming and versioning across all standards
- Shared components isolated without duplication  
- Clear domain/plugin separation maintained
- Architecture scales to future standards without refactoring

---

**Next Action**: Begin Phase 3 implementation - Messaging domain restructuring and HL7 message structure completion following approved architectural strategy.