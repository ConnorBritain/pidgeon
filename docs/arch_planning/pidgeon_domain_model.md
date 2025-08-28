# Pidgeon Domain Model Architecture
**Date**: August 27, 2025  
**Status**: 🎯 FOUNDATIONAL DOMAIN ARCHITECTURE - Updated August 28, 2025  
**Purpose**: Define the right-sized domain model architecture for long-term scalability and completeness

**Latest Update**: Added comprehensive Messaging Domain organizational strategy based on P0 MVP requirements and sacred architectural principles.

---

## **Executive Summary**

Pidgeon requires **four distinct bounded contexts** to properly model the complexity of healthcare integration without forcing unnatural unification. This document defines these domains, their boundaries, and the implementation strategy.

---

## **The Four Bounded Contexts**

Looking at what we're actually building, we have four genuinely different models of healthcare data:

### **1. Clinical Domain (Healthcare Business Concepts)**

```csharp
namespace Pidgeon.Core.Domain.Clinical
{
    // What healthcare professionals think about
    public record Patient(
        string MRN,
        PersonName Name,
        DateTime BirthDate,
        BloodType? BloodType,
        List<Allergy> Allergies
    );
    
    public record Prescription(
        Medication Drug,
        Dosing Instructions,
        Provider Prescriber,
        DateTime WrittenDate
    );
    
    public record Encounter(
        string EncounterId,
        Patient Patient,
        Provider AttendingProvider,
        DateTime AdmitTime,
        EncounterType Type
    );
}
```

**Owns**: The "real" healthcare concepts  
**Purpose**: Generate realistic clinical data  
**Language**: "Prescribe amoxicillin 500mg TID for John Smith"  
**Changes When**: Medical practice evolves, new treatments emerge

### **2. Messaging Domain (Wire Format Structures)**

**ARCHITECTURAL DECISION (August 28, 2025)**: Based on P0 MVP requirements and sacred principles analysis, the Messaging Domain uses **explicit versioning**, **hybrid shared components**, and **wire format structures**.

```csharp
namespace Pidgeon.Core.Domain.Messaging
{
    // Standard-specific wire format structures
    namespace HL7v2
    {
        namespace Messages
        {
            // Message types shared across HL7 v2.x versions
            public abstract record HL7Message(
                MSHSegment MSH,
                string MessageControlId,
                HL7EncodingChars Encoding
            ) : HealthcareMessage;
            
            public record ADTMessage : HL7Message; // Admissions/Discharges/Transfers
            public record RDEMessage : HL7Message; // Pharmacy Orders
            public record ORMMessage : HL7Message; // General Orders
            public record ORUMessage : HL7Message; // Observation Results
        }
        
        namespace Segments
        {
            // Segments shared across HL7 v2.x versions (hybrid approach)
            public record MSHSegment(...); // Message Header - same across versions
            public record PIDSegment(...); // Patient ID - mostly same, minor version differences
            public record PV1Segment(...); // Patient Visit - same across versions
            public record ORCSegment(...); // Common Order - same across versions
        }
        
        namespace DataTypes  
        {
            // Data types shared across HL7 v2.x versions
            public record XPN_ExtendedPersonName(...); // Person name structure
            public record CX_ExtendedCompositeId(...); // Patient ID structure  
            public record CE_CodedElement(...);        // Coded values
        }
    }
    
    namespace FHIR
    {
        namespace Resources
        {
            // FHIR R4 resource structures (explicit versioning for future R5 support)
            public abstract record FHIRResource(string Id, Meta Metadata);
            public record PatientResource : FHIRResource;
            public record MedicationRequestResource : FHIRResource;
        }
        
        namespace Bundles
        {
            public record FHIRBundle(
                List<FHIRResource> Entry,
                FHIRBundleType Type,
                Meta Metadata
            ) : HealthcareMessage;
        }
        
        namespace DataTypes
        {
            public record HumanName(...);    // FHIR person name
            public record Identifier(...);   // FHIR identifier  
            public record CodeableConcept(...); // FHIR coded concepts
        }
    }
    
    namespace NCPDP
    {
        namespace Transactions
        {
            // NCPDP SCRIPT transaction structures
            public abstract record NCPDPTransaction(
                string TransactionId,
                NCPDPTransactionType Type
            ) : HealthcareMessage;
            
            public record NewRxTransaction : NCPDPTransaction;
            public record RefillTransaction : NCPDPTransaction;
        }
        
        namespace Segments
        {
            public record UIB_InterchangeHeader(...); // NCPDP header
            public record PVD_ProviderSegment(...);   // Provider information
            public record PTT_PatientSegment(...);    // Patient information
        }
    }
    
    // Base healthcare message abstraction
    public abstract record HealthcareMessage(
        string Standard,           // "HL7v2", "FHIR", "NCPDP"
        string MessageControlId,   // Unique message identifier
        string SendingSystem,      // Source system
        string ReceivingSystem,    // Target system
        DateTime Timestamp
    );
}
```

#### **Messaging Domain Organizational Principles:**

1. **Explicit Versioning Strategy**: 
   - Standards folder: `HL7v23Plugin/`, `HL7v24Plugin/`, `FHIRv4Plugin/`
   - Domain folder: `HL7v2/` (version-agnostic), `FHIR/` (version-agnostic)
   - **Rationale**: Healthcare systems run different versions simultaneously

2. **Hybrid Shared Components**:
   - Common segments (MSH, PID) live in `Domain/Messaging/HL7v2/Segments/`
   - Version-specific overrides live in `Standards/HL7v23/Segments/` (when needed)
   - **Rationale**: 95% of segments are identical across versions, 5% need overrides

3. **Wire Format Structures**:
   - Domain contains concrete message structures, not abstract concepts  
   - Direct mapping from Clinical domain to specific standard formats
   - **Rationale**: P0 features need direct HL7/FHIR generation, not cross-standard mapping

#### **Standards Plugin Separation:**

```csharp
namespace Pidgeon.Core.Standards
{
    // Version-specific business logic and parsing
    namespace HL7v23
    {
        public class HL7v23Plugin : IStandardPlugin
        {
            // v2.3-specific parsing rules
            // v2.3-specific validation logic
            // v2.3-specific serialization
        }
        
        namespace Segments
        {
            // Only contains segments that differ from common HL7v2 segments
            public record PIDSegment : Domain.Messaging.HL7v2.Segments.PIDSegment
            {
                // v2.3-specific field differences (if any)
            }
        }
    }
}
```

**Owns**: Complete message structures as they exist in standards, organized by standard family  
**Purpose**: Parse, validate, generate, and analyze actual healthcare messages  
**Language**: "MSH.9 contains ORM^O01, PID.5 has 3 components, using HL7 v2.3 encoding"  
**Changes When**: Standards evolve (HL7 v2.3 → v2.4 → v2.5, FHIR R4 → R5), new standards added

### **3. Configuration Domain (Vendor Patterns)**

```csharp
namespace Pidgeon.Core.Domain.Configuration
{
    // What makes Epic different from Cerner
    public record VendorConfiguration(
        ConfigurationAddress Address,
        Dictionary<string, FieldPattern> RequiredFields,
        Dictionary<string, FieldPattern> OptionalFields,
        List<CustomSegmentPattern> ZSegments,
        List<FormatDeviation> Deviations,
        double Confidence
    );
    
    public record FieldPattern(
        string Path,              // "PID.5"
        Cardinality Cardinality,  // 1..1, 0..*, etc.
        string? RegexPattern,
        List<string> CommonValues,
        double PopulationRate    // 0.95 = 95% of messages have this
    );
    
    public record CustomSegmentPattern(
        string SegmentId,         // "ZPD"
        string Description,       // "Epic custom patient data"
        List<FieldPattern> Fields,
        double Frequency         // How often it appears
    );
    
    public record ConfigurationAddress(
        string Vendor,           // "Epic"
        string Standard,         // "HL7v23"
        string MessageType,      // "ADT^A01"
        string? Version         // "2.3.1"
    );
}
```

**Owns**: Patterns, rules, and vendor-specific behaviors  
**Purpose**: Configuration intelligence and compatibility  
**Language**: "Epic always populates PID.18, Cerner uses PID.2"  
**Changes When**: Vendors update their implementations, new patterns discovered

### **4. Transformation Domain (Mapping Rules)**

```csharp
namespace Pidgeon.Core.Domain.Transformation
{
    // The rules for how concepts map between domains
    public record MappingRule(
        SourcePath Source,           // "Clinical.Patient.Name"
        TargetPath Target,           // "HL7.PID.5"
        TransformFunction Transform, // How to convert
        ValidationRule? Validation,  // Constraints
        string? Context             // "Epic" or "Cerner" specific
    );
    
    public record TransformationSet(
        string Name,                // "PrescriptionToRDE"
        string SourceDomain,        // "Clinical"
        string TargetDomain,        // "Messaging.HL7"
        List<MappingRule> Rules,
        Dictionary<string, object> Metadata
    );
    
    public record TransformFunction(
        string Type,               // "Direct", "Lookup", "Calculate", "Custom"
        string? Expression,        // For calculated transforms
        Dictionary<string, string>? LookupTable  // For value mappings
    );
    
    public record FieldMapping(
        string Name,
        string Description,
        bool IsRequired,
        object? DefaultValue
    );
}
```

**Owns**: The transformation rules themselves (which can be data-driven!)  
**Purpose**: Define how to map between domains  
**Language**: "Patient.Name.Family maps to PID.5.1"  
**Changes When**: New mappings needed, vendor-specific rules discovered

---

## **Why All Four Are True Domains**

Each bounded context has:

1. **Its own entities** that are persisted and have identity
2. **Its own lifecycle** and reasons to change
3. **Its own language** and concepts
4. **Its own experts** who understand it

The key insight: **Transformation rules are entities too!** They're not just code - they're configurable mappings that can be:
- Stored in a database
- Modified by users
- Versioned over time
- Different per vendor/client

---

## **Domain Model Best Practices Applied**

### **When Multiple Domains Make Sense**

Multiple domains are appropriate when:

1. ✅ **Different perspectives need different representations**
   - Developers think "generate a prescription for John"
   - Analysts think "parse MSH.3 for vendor patterns"

2. ✅ **Forcing unification creates more complexity than it solves**
   - Conversion utilities are the symptom
   - Plugin pollution is another symptom

3. ✅ **The domains evolve independently**
   - Business rules change differently than message specs
   - HL7 v2.8 shouldn't break prescription logic

### **The Domain-Driven Design Perspective**

Eric Evans (DDD creator) explicitly advocates for **multiple bounded contexts** when you have different **Ubiquitous Languages**:

```
Our Four Languages:
1. Clinical Language: "Patient", "Prescription", "Provider"
2. Message Language: "PID Segment", "Field 5", "Component separator"
3. Configuration Language: "Vendor pattern", "Field population rate"
4. Transformation Language: "Source path", "Target field", "Mapping rule"

These are fundamentally different models of healthcare data.
```

---

## **The Right-Sized Architecture**

```yaml
Domain Model Structure:
  Clinical/
    ├── Entities/
    │   ├── Patient.cs
    │   ├── Provider.cs
    │   ├── Medication.cs
    │   └── Prescription.cs
    ├── ValueObjects/
    │   ├── PersonName.cs
    │   ├── Dosing.cs
    │   └── Address.cs
    └── "What clinicians think about"
    
  Messaging/
    ├── HL7v2/
    │   ├── Messages/
    │   │   ├── HL7Message.cs
    │   │   ├── HL7_ORM_Message.cs
    │   │   └── HL7_ADT_Message.cs
    │   ├── Segments/
    │   │   ├── MSH_Segment.cs
    │   │   ├── PID_Segment.cs
    │   │   └── OBR_Segment.cs
    │   └── DataTypes/
    │       ├── CX_ExtendedCompositeId.cs
    │       └── XPN_ExtendedPersonName.cs
    ├── FHIR/
    │   ├── Bundles/
    │   ├── Resources/
    │   └── DataTypes/
    └── NCPDP/
        ├── Transactions/
        └── Segments/
        
  Configuration/
    ├── VendorConfiguration.cs
    ├── ConfigurationAddress.cs
    ├── FieldPattern.cs
    ├── CustomSegmentPattern.cs
    └── "What makes vendors different"
    
  Transformation/
    ├── MappingRule.cs
    ├── TransformationSet.cs
    ├── TransformFunction.cs
    ├── FieldMapping.cs
    └── "How domains relate to each other"

Service Layer:
  Generation/
    └── Uses Clinical domain to create test data
    
  Analysis/
    └── Uses Messaging domain to analyze structures
    
  Validation/
    └── Uses Configuration domain to check compatibility
    
  Translation/
    └── Uses Transformation domain to map between others
```

---

## **Domain Adapter Strategy**

Each domain boundary uses adapters to translate between bounded contexts without leaking domain concepts:

```csharp
namespace Pidgeon.Core.Adapters.Interfaces
{
    // Clinical → Messaging Adapter
    public interface IClinicalToMessagingAdapter
    {
        Task<HL7Message> CreateAdmissionMessageAsync(Patient patient, Encounter encounter, string eventType = "A01");
        Task<HL7Message> CreatePrescriptionOrderAsync(Prescription prescription);
        Task<FHIRBundle> CreateFHIRBundleAsync(Patient patient, IEnumerable<Prescription>? prescriptions = null);
        Task<NCPDPTransaction> CreateNCPDPTransactionAsync(Prescription prescription, string transactionType = "NewRx");
    }
    
    // Messaging → Clinical Adapter  
    public interface IMessagingToClinicalAdapter
    {
        Task<Patient?> ExtractPatientFromHL7Async(HL7Message message);
        Task<Patient?> ExtractPatientFromFHIRAsync(FHIRBundle bundle);
        Task<Prescription?> ExtractPrescriptionFromHL7Async(HL7Message message);
        Task<IEnumerable<Prescription>> ExtractPrescriptionsFromFHIRAsync(FHIRBundle bundle);
        Task<Prescription?> ExtractPrescriptionFromNCPDPAsync(NCPDPTransaction transaction);
        Task<Encounter?> ExtractEncounterFromHL7Async(HL7Message message);
    }
    
    // Messaging → Configuration Adapter
    public interface IMessagingToConfigurationAdapter
    {
        Task<FieldPatterns> AnalyzePatternsAsync(IEnumerable<HealthcareMessage> messages);
        Task<VendorConfiguration> InferConfigurationAsync(IEnumerable<HealthcareMessage> messages);
        Task<List<FormatDeviation>> DetectDeviationsAsync(HealthcareMessage message, VendorConfiguration configuration);
        Task<Dictionary<string, FieldFrequency>> CalculateFieldStatisticsAsync(IEnumerable<HealthcareMessage> messages);
        Task<SegmentPattern> AnalyzeSegmentPatternsAsync(IEnumerable<HealthcareMessage> messages, string segmentType);
        Task<ComponentPattern> AnalyzeComponentPatternsAsync(IEnumerable<HealthcareMessage> messages, string fieldType);
    }
    
    // Configuration → Transformation Adapter
    public interface IConfigurationToTransformationAdapter
    {
        Task<TransformationSet> GenerateRulesAsync(VendorConfiguration config);
        Task<MappingRule> AdaptRuleAsync(MappingRule baseRule, VendorConfiguration config);
    }
}
```

---

## **Why This is the RIGHT Long-Term Architecture**

### **1. Each Domain Can Evolve Independently**
- **Clinical**: Add new medical concepts without touching messages
- **Messaging**: Support new HL7 versions without breaking clinical logic
- **Configuration**: Detect new vendor patterns without changing domains
- **Transformation**: Define new mapping strategies without coupling

### **2. Clear Ownership and Responsibilities**
- No confusion about where code belongs
- Each team could own a domain
- Changes are localized to one domain
- Interfaces are explicit at boundaries

### **3. Supports the Business Model**
- **Free Tier**: Clinical domain + basic Messaging
- **Professional**: + Configuration intelligence
- **Enterprise**: + Custom Transformations
- **SaaS**: All domains with multi-tenant isolation

### **4. Scales with Complexity**
- **New standard?** Add to Messaging domain
- **New vendor?** Add to Configuration domain
- **New clinical concept?** Add to Clinical domain
- **New mapping need?** Add to Transformation domain

### **5. Enables Advanced Features**
- **AI Integration**: Each domain provides focused context
- **Version Management**: Each domain versions independently
- **Analytics**: Each domain has clear metrics
- **Customization**: Transformation rules are data, not code

---

## **The Implementation Path**

```yaml
Phase 1 - Foundation (Week 1):
  Clinical Domain:
    - Core entities (Patient, Prescription, Provider)
    - Value objects (PersonName, Address, Dosing)
    - Basic generation logic
    
  Messaging Domain:
    - HL7_ORM_Message complete structure
    - PID, MSH, OBR segments with all fields
    - Basic parsing capability
    
  Transformation Domain:
    - Basic mapping rule structure
    - Simple direct mappings
    - Prescription → ORM transformation
    
  Proof Point:
    ✓ Can generate ORM from Prescription
    ✓ No conversion utilities needed
    ✓ Clean domain boundaries

Phase 2 - Configuration (Week 2):
  Configuration Domain:
    - VendorConfiguration structure
    - FieldPattern analysis
    - Pattern detection from messages
    
  Anti-Corruption Layer:
    - IClinicalToMessaging implementation
    - IMessagingToConfiguration implementation
    
  Proof Point:
    ✓ Can detect Epic vs Cerner patterns
    ✓ Can store vendor configurations
    ✓ Pattern detection works on real messages

Phase 3 - Completeness (Week 3):
  Messaging Domain:
    - Add ADT, RDE message types
    - Add FHIR Patient, MedicationRequest
    - Add NCPDP NewRx
    
  Transformation Domain:
    - Complex transformation rules
    - Conditional mappings
    - Vendor-specific variations
    
  Proof Point:
    ✓ Same prescription → HL7/FHIR/NCPDP
    ✓ Round-trip transformations work
    ✓ No domain leakage

Phase 4 - Production (Week 4):
  All Domains:
    - Performance optimization
    - Comprehensive testing
    - Documentation
    
  Service Layer:
    - Generation service (uses Clinical)
    - Analysis service (uses Messaging)
    - Validation service (uses Configuration)
    - Translation service (uses Transformation)
    
  Proof Point:
    ✓ Performance <50ms for transformations
    ✓ 95% test coverage
    ✓ Ready for production use
```

---

## **Migration Strategy from Current State**

### **What We Have Today**
```
Current Structure:
  Domain/
    ├── Patient.cs (business-focused)
    ├── Medication.cs (business-focused)
    ├── Prescription.cs (business-focused)
    └── Provider.cs (business-focused)
    
  Standards/HL7/
    ├── Segments/ (message-focused)
    ├── Fields/ (message-focused)
    └── Messages/ (message-focused)
    
  Configuration/
    ├── VendorConfiguration.cs (pattern-focused)
    └── FieldPatterns.cs (pattern-focused)
    
  Services/
    └── Various services with mixed concerns
```

### **Migration Steps**

1. **Create new domain structure** alongside existing
2. **Move entities** to appropriate domains
3. **Create anti-corruption layer** interfaces
4. **Refactor services** to use single domain each
5. **Remove conversion utilities** as transformations replace them
6. **Delete old structure** once migration complete

---

## **Success Metrics**

### **Architectural Health**
- ✅ Zero conversion utilities between domains
- ✅ Each service depends on exactly one domain
- ✅ No circular dependencies between domains
- ✅ All transformations happen at defined boundaries

### **Business Value**
- ✅ Configuration Intelligence works without hacks
- ✅ New standards can be added without breaking existing
- ✅ Vendor patterns can be detected and stored
- ✅ Performance meets <50ms targets

### **Development Velocity**
- ✅ New features clearly belong to one domain
- ✅ Changes don't cascade across domains
- ✅ Testing is focused and fast
- ✅ Onboarding new developers is straightforward

---

## **The Decision**

**Implement four domains**: Clinical, Messaging, Configuration, and Transformation.

This isn't over-engineering because:
- Each represents genuinely different concepts
- Each has different reasons to change  
- Each could be owned by different teams
- Each has clear boundaries and responsibilities

The Transformation domain is particularly important because it makes mappings **data-driven** rather than hard-coded, which is crucial for a platform that needs to support multiple vendors and standards.

---

## **Key Insights**

1. **Domain Depth Drives Success**: Shallow domains lead to constant patches. Rich, focused domains lead to clean architecture.

2. **Transformation as a Domain**: Treating mapping rules as data rather than code enables vendor-specific customization without code changes.

3. **Anti-Corruption is Essential**: Without explicit boundaries, concepts leak between domains and create coupling.

4. **Four is Right-Sized**: Not too many (confusing), not too few (coupled). Each domain has a clear, distinct purpose.

5. **Evolution Path Clear**: Each domain can evolve independently as standards change, vendors update, or business needs grow.

---

**Next Step**: Begin Phase 1 implementation with Clinical domain entities and HL7_ORM_Message structure, proving the architecture with a single transformation.