# Pidgeon Healthcare Platform - Architecture Overview
**Version**: 2.0  
**Updated**: September 1, 2025  
**Status**: Authoritative architectural specification  
**Scope**: Complete system architecture with current implementation status

---

## 🎯 **Architectural Vision**

**Mission**: Build a healthcare interoperability platform that scales from individual developers to enterprise teams without architectural refactoring.

**Core Principle**: Domain-driven design with plugin architecture enables adding new healthcare standards as plugins, never breaking existing functionality.

---

## 🏗️ **Four-Domain Architecture**

### **Domain Boundaries & Responsibilities**

```csharp
// ✅ SACRED: Four bounded contexts for healthcare integration
namespace Pidgeon.Core.Domain {
    // 1. Clinical Domain - Healthcare business concepts
    public record Patient(string MRN, PersonName Name, DateTime BirthDate);
    public record Prescription(Patient Patient, Medication Drug, Provider Prescriber);
    
    // 2. Messaging Domain - Wire format structures  
    public class HL7_ORM_Message { /* HL7 structure */ }
    public class FHIR_Bundle { /* FHIR structure */ }
    
    // 3. Configuration Domain - Vendor patterns
    public record VendorConfiguration(string Name, FieldPattern[] Patterns);
    
    // 4. Transformation Domain - Cross-standard mapping
    public record MappingRule(string Source, string Target, TransformationType Type);
}
```

#### **1. Clinical Domain (`Pidgeon.Core.Domain.Clinical`)**
**Purpose**: Pure healthcare business logic - what doctors, patients, and care teams understand  
**Entities**: Patient, Provider, Medication, Encounter, Diagnosis  
**Responsibilities**:
- Healthcare data generation with clinical accuracy
- Business rule validation (age restrictions, drug interactions)  
- Demographic and clinical data relationships

**Key Design Patterns**:
- Rich domain models with business logic
- Value objects for medical concepts (Age, Weight, BloodPressure)
- Domain services for complex clinical rules

#### **2. Messaging Domain (`Pidgeon.Core.Domain.Messaging`)**
**Purpose**: Wire format representations - how healthcare systems exchange data  
**Standards**: HL7 v2.x, FHIR R4, NCPDP SCRIPT  
**Responsibilities**:
- Message structure definitions and validation
- Standard-specific parsing and serialization
- Field-level error reporting and debugging

**Key Design Patterns**:
- Standard-specific namespaces (HL7v2, FHIR, NCPDP)
- Immutable message structures
- Comprehensive validation with detailed error reporting

#### **3. Configuration Domain (`Pidgeon.Core.Domain.Configuration`)**
**Purpose**: Vendor intelligence - how real-world systems actually behave  
**Entities**: VendorConfiguration, FieldPattern, ValidationProfile  
**Responsibilities**:
- Vendor-specific pattern detection and storage
- Configuration drift monitoring
- Validation rule customization per vendor

**Key Design Patterns**:
- Pattern recognition algorithms
- Configuration versioning and change detection
- Vendor fingerprinting for automatic detection

#### **4. Transformation Domain (`Pidgeon.Core.Domain.Transformation`)**
**Purpose**: Cross-standard intelligence - how to convert between formats  
**Entities**: MappingRule, TransformationSet, SemanticPreservation  
**Responsibilities**:
- HL7 ↔ FHIR ↔ NCPDP transformations
- Data loss detection and reporting
- Semantic accuracy validation

**Key Design Patterns**:
- Bidirectional transformation rules
- Semantic preservation tracking
- Terminology integration (LOINC, RxNorm, SNOMED)

---

## 🔌 **Plugin Architecture**

### **Core Orchestration Pattern**

```csharp
// ✅ Standards are plugins, not core components
public interface IStandardPlugin {
    string StandardName { get; }
    bool CanHandle(string messageType);
    Result<Message> Parse(string rawMessage);
    Result<string> Generate(DomainObject data);
}

// ✅ Service orchestration delegates to plugins
public class GenerationService {
    public Result<string> GenerateMessage(GenerationRequest request) {
        var plugin = _pluginRegistry.GetPlugin(request.Standard);
        return plugin.Generate(request.DomainData);
    }
}
```

### **Plugin Categories**

#### **Standard Plugins**
- **HL7v23Plugin**: Complete HL7 v2.3 implementation
- **HL7v24Plugin**: HL7 v2.4 with backward compatibility
- **FHIR_R4Plugin**: FHIR R4 resource handling
- **NCPDP_Plugin**: Pharmacy messaging support

#### **Vendor Intelligence Plugins**
- **EpicPlugin**: Epic-specific patterns and quirks
- **CernerPlugin**: Cerner implementation variations
- **AllScriptsPlugin**: AllScripts formatting rules
- **MedTechPlugin**: Meditech vendor patterns

#### **Service Plugins**
- **AI_Enhancement**: Optional AI-powered generation
- **Terminology_Services**: LOINC/RxNorm integration
- **Compliance_Validation**: Regulatory rule checking

---

## 🧱 **Dependency Flow & Clean Architecture**

### **Sacred Dependency Rules**

```
Infrastructure.Standards ←── Application.Services ←── Domain
        ↑                           ↑                    ↑
    Plugins &              Service Layer        Pure Business Logic
    External APIs          Orchestration           (No Dependencies)
```

#### **Domain Layer (Zero Dependencies)**
- **Clinical**: Pure healthcare business logic
- **Messaging**: Standard message structures  
- **Configuration**: Vendor pattern definitions
- **Transformation**: Mapping rule definitions

#### **Application Layer (Domain Dependencies Only)**
- **Services**: Business workflow orchestration
- **Use Cases**: Feature implementation
- **Interfaces**: Plugin contracts and abstractions

#### **Infrastructure Layer (All Dependencies Allowed)**
- **Plugins**: Standard implementations
- **Repositories**: Data persistence
- **External Services**: API integrations, AI services

### **Cross-Domain Communication**

```csharp
// ✅ Domains communicate through adapters
public interface IClinicalToMessagingAdapter {
    Result<HL7Message> ConvertToHL7(Patient patient, MessageType type);
    Result<FHIRBundle> ConvertToFHIR(Patient patient, BundleType type);
}

// ✅ Single-domain services with adapter injection
public class GenerationService {
    private readonly IClinicalToMessagingAdapter _adapter;
    
    public Result<string> GenerateHL7(Patient patient) {
        return _clinicalGenerator.GeneratePatient()
            .Bind(patient => _adapter.ConvertToHL7(patient, MessageType.ADT));
    }
}
```

---

## 📊 **Current Implementation Status**

### **✅ Completed (Foundation Ready)**
- **Four-domain structure**: Proper namespace organization
- **Plugin architecture**: Full service delegation pattern
- **Dependency injection**: All services properly injected
- **Error handling**: Comprehensive Result<T> pattern (179+ usages)
- **Core generation**: Clinical entity creation with realistic data

### **🚧 Critical Issues (Must Fix Before P0)**
- **Domain boundary violations**: 21 files importing across domain boundaries
- **Infrastructure dependencies**: 12 domain files importing infrastructure
- **Code duplication**: 600+ duplicate lines requiring consolidation  
- **Critical TODOs**: 4 P0-blocking implementations needed

### **📈 Health Score Progression**
| Aspect | Current | Post-Foundation | Target |
|--------|---------|-----------------|--------|
| **Domain Purity** | 58/100 | 95/100 | 95/100 |
| **Plugin Architecture** | 95/100 | 95/100 | 95/100 |
| **Error Handling** | 100/100 | 100/100 | 100/100 |
| **Code Quality** | 40/100 | 90/100 | 95/100 |
| **Overall** | **87/100** | **95/100** | **95/100** |

---

## 🎯 **Design Principles & Patterns**

### **Sacred Principles (Immutable)**

#### **1. Domain-First Architecture**
- Healthcare concepts drive design, not technical constraints
- Business logic lives in domain, not in infrastructure
- Domain models are rich with healthcare intelligence

#### **2. Plugin Extensibility**  
- New standards added as plugins, never core changes
- Vendor patterns isolated in configuration plugins
- No breaking changes to existing functionality

#### **3. Dependency Injection Throughout**
- Zero static classes or methods (kills testability)
- All services injectable and mockable for testing
- Clear service boundaries and contracts

#### **4. Result<T> Error Handling**
- No exceptions for business logic control flow  
- Explicit success/failure handling throughout
- Detailed error context for debugging

#### **5. Configuration-Driven Behavior**
- Strict vs compatibility validation modes
- Vendor-specific pattern application
- Runtime behavior modification without code changes

#### **6. Core+ Business Model Alignment**
- Clear separation between free and paid features
- Plugin architecture enables premium vendor intelligence
- API boundaries support subscription tier enforcement

### **Key Patterns**

#### **Domain Events**
```csharp
public record PatientCreated(Patient Patient, DateTime Timestamp) : IDomainEvent;
```

#### **Specification Pattern**
```csharp
public class ValidPatientSpecification : ISpecification<Patient> {
    public bool IsSatisfiedBy(Patient patient) => /* validation logic */;
}
```

#### **Repository Pattern**
```csharp
public interface IVendorConfigurationRepository {
    Result<VendorConfiguration> FindByName(string vendorName);
    Result<Unit> Save(VendorConfiguration configuration);
}
```

---

## 🔧 **Technical Architecture**

### **Runtime Architecture**

```
┌─────────────────────────────────────────────────────────────────┐
│                           CLI/API Layer                          │
├─────────────────────────────────────────────────────────────────┤
│                       Application Services                       │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐  │
│  │ GenerationService│  │ValidationService │  │ ConfigService   │  │
│  └─────────────────┘  └─────────────────┘  └─────────────────┘  │
├─────────────────────────────────────────────────────────────────┤
│                        Domain Adapters                          │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐  │
│  │Clinical→Messaging│  │Config→Transform │  │Messaging→Config │  │
│  └─────────────────┘  └─────────────────┘  └─────────────────┘  │
├─────────────────────────────────────────────────────────────────┤
│                         Domain Layer                            │
│  ┌─────────────┐ ┌─────────────┐ ┌─────────────┐ ┌─────────────┐│
│  │  Clinical   │ │  Messaging  │ │Configuration│ │Transformation││
│  │   Domain    │ │   Domain    │ │   Domain    │ │   Domain     ││
│  └─────────────┘ └─────────────┘ └─────────────┘ └─────────────┘│
├─────────────────────────────────────────────────────────────────┤
│                      Plugin Infrastructure                       │
│  ┌─────────────┐ ┌─────────────┐ ┌─────────────┐ ┌─────────────┐│
│  │HL7 Plugins  │ │FHIR Plugins │ │Vendor Plugins│ │ AI Plugins  ││
│  └─────────────┘ └─────────────┘ └─────────────┘ └─────────────┘│
└─────────────────────────────────────────────────────────────────┘
```

### **Data Flow Architecture**

```
Healthcare Request → Clinical Domain → Domain Adapter → Messaging Domain → Standard Plugin → Wire Format
                                                                                            ↓
Healthcare Response ← Clinical Domain ← Domain Adapter ← Messaging Domain ← Standard Plugin ← Parse/Validate
```

### **Performance Targets**
- **Message Generation**: <50ms per message
- **Message Validation**: <20ms per message  
- **Vendor Pattern Detection**: <5 messages to 80% confidence
- **Cross-Standard Transform**: <100ms HL7↔FHIR
- **Concurrent Users**: 10,000+ users supported
- **Uptime**: 99.9% availability target

---

## 🔒 **Security & Compliance**

### **Data Handling Principles**
- **Zero PHI Storage**: All generated data is synthetic
- **Encryption in Transit**: TLS 1.3 for all API communication
- **Plugin Isolation**: Vendor plugins cannot access other vendor data
- **Audit Logging**: Complete trail of all configuration changes

### **Compliance Framework**
- **HIPAA Compliance**: No PHI handling, synthetic data only
- **SOC2 Type II**: Security controls and monitoring
- **Healthcare Standards**: HL7, FHIR, NCPDP specification compliance
- **Enterprise Security**: SSO, RBAC, audit trails for Enterprise tier

---

## 📋 **Architectural Decision Records**

### **Major Decisions (See LEDGER.md for full history)**

#### **ARCH-019: Four-Domain Architecture (Final)**
- **Decision**: Clinical, Messaging, Configuration, Transformation domains
- **Rationale**: Natural healthcare data boundaries, clear separation
- **Impact**: Enables independent evolution of each concern

#### **ARCH-024: Clean Architecture Reorganization** 
- **Decision**: Domain → Application → Infrastructure dependency flow
- **Rationale**: Plugin architecture requires clean boundaries
- **Impact**: Broke some services, requires adapter pattern completion

#### **ARCH-025: Plugin-First Standards Support**
- **Decision**: All healthcare standards implemented as plugins
- **Rationale**: Enables adding standards without breaking existing code
- **Impact**: Excellent extensibility, some complexity in service delegation

---

## 🚀 **Future Architectural Considerations**

### **Scalability Planning**
- **Microservices Readiness**: Domain boundaries align with service boundaries
- **Event-Driven Architecture**: Domain events support eventual consistency
- **Multi-Tenancy**: Plugin architecture enables tenant-specific customization

### **Technology Evolution**
- **.NET Upgrade Path**: Clean architecture supports framework updates
- **Cloud Native**: Designed for containerization and orchestration
- **AI Integration**: Plugin architecture enables AI feature addition

### **Standards Evolution**  
- **HL7 FHIR R5**: Plugin architecture enables new version support
- **Emerging Standards**: CDA, C-CDA, X12 can be added as plugins
- **Vendor Changes**: Configuration domain tracks and adapts to vendor updates

---

**Architecture Status**: Foundation strong with critical violations requiring immediate attention. Post-foundation cleanup will result in production-ready, scalable healthcare interoperability platform.

**Next Steps**: Complete domain boundary repair, eliminate code duplication, implement P0-blocking features. Architecture will then support rapid feature development and scaling.