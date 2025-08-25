# Product Overview

## Segmint Healthcare Interoperability Platform

An AI-augmented, universal healthcare standards platform that provides free basic generation for HL7, FHIR, and NCPDP SCRIPT, with premium features for professional productivity and enterprise governance.

### Core Functionality

**Universal Healthcare Message Platform (Core+ Strategy):**
- Generates test messages across THREE standards: HL7 v2.x, FHIR R4, NCPDP SCRIPT
- **Real-world compatible validation** with strict mode vs compatibility mode for handling spec violations
- **Vendor-specific configuration inference** from sample messages (Epic, Cerner, CorEMR patterns)
- **Baseline configuration library** with pre-built templates for common EHR implementations
- Unified domain model allows same data to generate different standard formats
- AI-powered features (with BYOK) for mapping and documentation

### Key Business Value

- **Universal Coverage**: Only free tool supporting all three major standards
- **Real-World Ready**: Liberal validation accepts messy real-world HL7, strict mode ensures compliance
- **Instant Vendor Onboarding**: Infer interface specifications from sample messages in minutes
- **Time Savings**: Generate hundreds of test messages matching actual vendor patterns
- **Quality Assurance**: Multi-level validation against both specs and real-world implementations
- **AI Augmentation**: Optional AI accelerates mapping and documentation by 80%
- **Zero Lock-in**: Open source core with MPL 2.0 license

### Target Users

- **Healthcare IT Interface Engineers**: Need daily testing/validation across standards
- **Integration Consultants**: Require portable tools for client projects
- **Hospital IT Departments**: Managing interfaces between dozens of systems
- **EHR/Lab/Pharmacy Vendors**: Testing their interface implementations
- **Mirth Refugees**: Organizations seeking alternatives after v4.6 closure

### System Boundaries

- **Input**: Healthcare data models, raw messages, configuration files
- **Processing**: Generation, parsing, validation, transformation, mapping
- **Output**: Valid messages in HL7/FHIR/NCPDP formats, documentation, error reports
- **External Dependencies**: Optional AI services (OpenAI), terminology services

### Product Tiers (Core+ Model)

**🆓 CORE (Open Source - MPL 2.0)**
```
HL7 v2.x:
- Full v2.3 engine with all message types
- Complete parsing, generation, validation
- Compatibility mode for real-world HL7
- Basic configuration inference
- Synthetic data generation

FHIR R4 (Basic):
- Core resources (Patient, Observation, MedicationRequest, etc.)
- JSON/XML serialization
- Basic validation

NCPDP SCRIPT (Basic):
- NewRx, Refill, Cancel messages
- XML generation
- Basic validation

Universal:
- CLI interface
- Unified domain model
- Configuration management
- Basic vendor pattern recognition
```

**💼 PROFESSIONAL ($299 one-time)**
```
Advanced Standards:
- All 150+ FHIR resources
- Complete NCPDP message suite
- HL7 v2.5.1, v2.7 support

Productivity:
- Desktop GUI application
- Visual message designer
- Drag-drop mapping
- Batch processing

Configuration Intelligence:
- Advanced configuration inference engine
- Vendor-specific template library (Epic, Cerner, AllScripts)
- Configuration validation and diff tools
- Pattern recognition and anomaly detection

AI Features (BYOK):
- Mapping suggestions
- Auto-documentation
- Natural language queries
- AI-powered configuration analysis
```

**🏢 ENTERPRISE ($99-199/month per seat)**
```
Cross-Standard:
- HL7 ↔ FHIR ↔ NCPDP transformation
- Terminology integration (LOINC, RxNorm)

Configuration Enterprise:
- Real-time interface conformance monitoring
- Automated configuration drift detection
- Configuration versioning and rollback
- Enterprise vendor configuration library
- Multi-environment configuration management

Collaboration:
- Team workspaces
- Version control integration
- RBAC and SSO
- Audit logging

Cloud Services:
- Hosted validation API
- Real-time collaboration
- Unlimited AI (no BYOK)
- Priority support with SLA
```

### Competitive Positioning

**"The WordPress of Healthcare Interoperability"**
- Free core that everyone uses
- Premium features professionals need
- Enterprise governance organizations require

Unlike Mirth (now closed), Rhapsody ($50K+), or cloud-only solutions (Redox), Segmint offers a modern, open platform with AI augmentation at a fraction of the cost.