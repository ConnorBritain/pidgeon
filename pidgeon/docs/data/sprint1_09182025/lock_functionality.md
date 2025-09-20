# Pidgeon Lock/Set Functionality - Comprehensive Feature Specification

**Status**: Feature Design
**Priority**: P1 - High Impact CLI Enhancement
**Target Release**: Sprint 2 (Post Data-Enriched CLI)
**Business Impact**: Workflow automation + incremental testing = Pro tier conversion driver

## 🎯 **Executive Summary**

The Lock/Set functionality transforms Pidgeon from a one-shot message generator into a **workflow automation platform** that maintains patient consistency across complex healthcare scenarios. This feature directly addresses the #1 pain point in integration testing: maintaining realistic data relationships while iterating on specific aspects.

**Core Value Proposition**: "Generate a full patient journey once, then iterate on individual touchpoints without breaking continuity"

---

## 🏗️ **CLI Architecture Philosophy Adherence**

### **CLI_REFERENCE.md Alignment**
Following the established CLI patterns from `docs/roadmap/CLI_REFERENCE.md`:

```bash
# Pattern: Action → Target → Modifiers
pidgeon lock create --name patient_workflow_1 --from-message ./samples/admit.hl7
pidgeon set patient.mrn "MR123456" --lock patient_workflow_1
pidgeon generate message --type RDE^O11 --use-lock patient_workflow_1
```

### **Three-Tier Feature Distribution**
- **🆓 Core**: Basic lock/set for single session
- **🔒 Professional**: Persistent locks, templates, batch operations
- **🏢 Enterprise**: Team locks, audit trails, workflow orchestration

---

## 📋 **Core Feature Set**

### **1. Session Lock Management (Core - Free)**

#### **Lock Creation**
```bash
# Create from existing message
pidgeon lock create --name admit_scenario --from-message ./samples/admit.hl7

# Create from generation parameters
pidgeon lock create --name surgery_flow --template adult_female --age 45 --condition "knee_replacement"

# Auto-lock during generation (convenience)
pidgeon generate message --type ADT^A01 --auto-lock surgery_patient
```

#### **Lock Inspection**
```bash
# List active locks
pidgeon lock list
# Output:
# 🔒 Active Locks:
#   admit_scenario     (Patient: Jane Doe, MRN: MR789012, Created: 2 hours ago)
#   surgery_flow       (Patient: Maria Garcia, MRN: MR345678, Created: 30 min ago)

# Show lock details
pidgeon lock show admit_scenario
# Output detailed patient demographics, identifiers, timestamps
```

#### **Lock Cleanup**
```bash
# Remove specific lock
pidgeon lock remove admit_scenario

# Clear all session locks
pidgeon lock clear

# Auto-cleanup after session timeout (24 hours default)
```

### **2. Granular Value Setting (Core - Free)**

#### **Patient Demographics**
```bash
# Core identifiers
pidgeon set patient.mrn "MR987654" --lock surgery_flow
pidgeon set patient.name "Robert Johnson" --lock surgery_flow
pidgeon set patient.dob "1978-03-15" --lock surgery_flow

# Contact information
pidgeon set patient.phone "(555) 123-4567" --lock surgery_flow
pidgeon set patient.address.street "123 Oak Street" --lock surgery_flow
```

#### **Clinical Context**
```bash
# Timestamps with smart relative dating
pidgeon set timestamp.admit "2025-01-15T08:30:00" --lock surgery_flow
pidgeon set timestamp.surgery "+2d" --lock surgery_flow  # 2 days after admit
pidgeon set timestamp.discharge "+5d" --lock surgery_flow

# Clinical values
pidgeon set encounter.location "OR-3" --lock surgery_flow
pidgeon set provider.attending "DR12345^SMITH^JOHN" --lock surgery_flow
```

#### **Message-Specific Fields**
```bash
# Lab results
pidgeon set lab.glucose "95" --lock surgery_flow
pidgeon set lab.hemoglobin "12.8" --lock surgery_flow

# Medications
pidgeon set medication.primary "MORPHINE 2MG IV Q4H PRN" --lock surgery_flow
pidgeon set medication.secondary "ACETAMINOPHEN 650MG PO Q6H" --lock surgery_flow
```

### **3. Incremental Generation (Core - Free)**

#### **Lock-Aware Generation**
```bash
# Use locked patient for new message types
pidgeon generate message --type ADT^A02 --use-lock surgery_flow  # Transfer
pidgeon generate message --type ORM^O01 --use-lock surgery_flow  # Lab Orders
pidgeon generate message --type RDE^O11 --use-lock surgery_flow  # Pharmacy
pidgeon generate message --type ADT^A03 --use-lock surgery_flow  # Discharge

# Override specific fields while maintaining lock
pidgeon generate message --type ADT^A02 --use-lock surgery_flow --override "PV1.3=ICU-2"
```

#### **Batch Workflow Generation**
```bash
# Generate complete patient flow
pidgeon generate workflow --lock surgery_flow --sequence "admit,surgery,recovery,discharge"

# Custom workflow sequences
pidgeon generate workflow --lock surgery_flow --steps "./workflows/cardiac_bypass.yml"
```

---

## 🔒 **Professional Tier Features**

### **4. Persistent Lock Templates (Professional)**

#### **Template Management**
```bash
# Save lock as reusable template
pidgeon template save surgery_flow --name "adult_orthopedic_surgery" --description "Standard knee replacement workflow"

# List available templates
pidgeon template list
# Show library of saved templates with usage statistics

# Apply template to new lock
pidgeon lock create --from-template adult_orthopedic_surgery --name new_patient_flow
```

#### **Template Marketplace**
```bash
# Download community templates (Professional feature)
pidgeon template download "epic_emergency_dept" --author "healthcare_dev_community"
pidgeon template download "cerner_surgical_workflow" --verified

# Share templates (anonymized)
pidgeon template publish surgery_flow --name "orthopedic_workflow_v2"
```

### **5. Advanced Workflow Automation (Professional)**

#### **Smart Date Progression**
```bash
# Define workflow with intelligent date spacing
pidgeon workflow create cardiac_surgery
pidgeon workflow add-step pre_op --template "pre_surgical_assessment" --timing "-1d"
pidgeon workflow add-step surgery --template "cardiac_procedure" --timing "baseline"
pidgeon workflow add-step post_op --template "icu_monitoring" --timing "+0d"
pidgeon workflow add-step discharge --template "home_recovery" --timing "+3d"

# Execute workflow with automatic date progression
pidgeon workflow run cardiac_surgery --start-date "2025-02-01"
```

#### **Conditional Logic**
```bash
# Workflow branches based on patient characteristics
pidgeon workflow create er_triage
pidgeon workflow add-condition "age > 65" --branch "geriatric_protocol"
pidgeon workflow add-condition "chief_complaint contains chest" --branch "cardiac_workup"
```

### **6. Cross-Message Validation (Professional)**

#### **Continuity Checking**
```bash
# Validate workflow consistency
pidgeon validate workflow --lock surgery_flow --check-continuity
# Validates: MRN consistency, date progression, provider assignments, location logic

# Export validation report
pidgeon validate workflow --lock surgery_flow --report "./reports/workflow_validation.html"
```

---

## 🏢 **Enterprise Tier Features**

### **7. Team Collaboration (Enterprise)**

#### **Shared Lock Workspace**
```bash
# Create team-accessible locks
pidgeon lock create epic_integration_test --team --shared

# Lock checkout system
pidgeon lock checkout epic_integration_test --message "Testing epic ADT feed"
pidgeon lock checkin epic_integration_test --message "Lab integration complete"

# Team lock visibility
pidgeon lock list --team
# Shows who's using what locks, current checkout status
```

### **8. Audit Trail & Governance (Enterprise)**

#### **Change Tracking**
```bash
# Full audit trail for compliance
pidgeon audit workflow --lock surgery_flow --from "2025-01-15" --to "2025-01-20"

# Export audit trail for compliance reporting
pidgeon audit export --format compliance --lock surgery_flow --output "./compliance/audit_trail.json"
```

#### **Access Control**
```bash
# Role-based permissions
pidgeon lock create sensitive_patient --access-level "lead_developer"
pidgeon template create --restricted --approval-required
```

---

## 🛠️ **Technical Implementation Strategy**

### **Lock Storage Architecture**
```
Core (Free):     ~/.pidgeon/session_locks/     (24-hour TTL)
Professional:    ~/.pidgeon/templates/         (Persistent, sync-capable)
Enterprise:      Central lock server           (Team coordination)
```

### **Data Structure**
```csharp
public record PatientLock
{
    public string Name { get; init; }
    public LockScope Scope { get; init; }  // Session, Persistent, Team
    public PatientDemographics Demographics { get; init; }
    public Dictionary<string, string> CustomFields { get; init; }
    public TimestampContext Timestamps { get; init; }
    public ProviderContext Providers { get; init; }
    public List<GeneratedMessage> MessageHistory { get; init; }
    public DateTime Created { get; init; }
    public DateTime LastUsed { get; init; }
}
```

### **Integration Points**
- **Generation Engine**: Lock values override defaults during message creation
- **Validation Engine**: Cross-message continuity checking
- **Demographics Service**: Enhanced with lock-aware realistic data
- **Configuration System**: Template storage and retrieval

---

## 🎯 **User Experience Workflows**

### **Scenario 1: Emergency Department to Discharge**
```bash
# 1. Create patient from real-world admit message
pidgeon lock create er_patient --from-message ./samples/er_admit.hl7

# 2. Generate lab orders (maintains patient context)
pidgeon generate message --type ORM^O01 --use-lock er_patient

# 3. Modify patient condition, generate updated labs
pidgeon set patient.condition "chest_pain_resolved" --lock er_patient
pidgeon generate message --type ORU^R01 --use-lock er_patient

# 4. Generate discharge (automatic date progression)
pidgeon generate message --type ADT^A03 --use-lock er_patient
```

### **Scenario 2: Medication Workflow Testing**
```bash
# 1. Create surgical patient with specific allergies
pidgeon lock create surgery_patient --template adult_male --age 55
pidgeon set patient.allergies "PENICILLIN,LATEX" --lock surgery_patient

# 2. Test prescription workflow with allergy checking
pidgeon generate message --type RDE^O11 --use-lock surgery_patient --medication "AMOXICILLIN"
# Should trigger allergy validation warnings

# 3. Swap to safe medication
pidgeon set medication.antibiotic "AZITHROMYCIN" --lock surgery_patient
pidgeon generate message --type RDE^O11 --use-lock surgery_patient
```

### **Scenario 3: Multi-System Integration Testing**
```bash
# 1. Create patient template for epic-to-cerner testing
pidgeon template create epic_patient --vendor epic --demographics adult_female
pidgeon lock create integration_test --from-template epic_patient

# 2. Generate epic-style ADT
pidgeon generate message --type ADT^A01 --use-lock integration_test --vendor epic

# 3. Transform to cerner format while maintaining patient identity
pidgeon generate message --type ADT^A01 --use-lock integration_test --vendor cerner

# 4. Validate cross-system consistency
pidgeon validate cross-system --source epic --target cerner --lock integration_test
```

---

## 🚀 **Competitive Differentiation**

### **vs. Traditional Tools**
- **Mirth Connect**: Static transformations → Dynamic patient-centric workflows
- **HL7 Soup**: One-shot generation → Persistent patient journey management
- **Custom Scripts**: Manual maintenance → Automated workflow orchestration

### **vs. Modern Platforms**
- **FHIR Testing Tools**: FHIR-only → Multi-standard workflow support
- **Postman/Insomnia**: API-centric → Healthcare scenario-centric
- **Integration Platforms**: Enterprise-only → Free-to-Pro progression

### **Unique Value Propositions**
1. **Patient Journey Continuity**: Only tool that maintains realistic patient context across message types
2. **Incremental Testing**: Change one field, maintain everything else automatically
3. **Workflow Automation**: Generate complete clinical scenarios with one command
4. **Cross-Standard Support**: Same patient, multiple standards (HL7, FHIR, NCPDP)

---

## 📈 **Business Model Integration**

### **Conversion Funnel Strategy**
1. **Core Hook**: Free basic lock/set gets users addicted to workflow continuity
2. **Professional Upgrade**: Template library + advanced workflows drive subscription
3. **Enterprise Expansion**: Team collaboration + audit trails unlock organizational sales

### **Revenue Impact Projections**
- **Professional Conversion**: +40% (workflow automation is clear value-add)
- **Enterprise Expansion**: +25% (compliance + collaboration requirements)
- **User Retention**: +60% (workflow state creates switching costs)

### **Feature Gating Strategy**
```
Free:         Single session, basic set/get, 5 locks max
Professional: Persistent templates, workflows, unlimited locks, marketplace
Enterprise:   Team locks, audit trails, approval workflows, compliance reporting
```

---

## 🗓️ **Implementation Roadmap**

### **Phase 1: Core Lock/Set (4 weeks)**
- Week 1: Basic lock create/remove/list functionality
- Week 2: Granular field setting with validation
- Week 3: Lock-aware generation integration
- Week 4: Session management and cleanup

### **Phase 2: Professional Features (6 weeks)**
- Week 1-2: Persistent template system
- Week 3-4: Advanced workflow automation
- Week 5-6: Cross-message validation and reporting

### **Phase 3: Enterprise Features (4 weeks)**
- Week 1-2: Team collaboration infrastructure
- Week 3-4: Audit trails and compliance reporting

### **Phase 4: Polish & Scale (2 weeks)**
- Week 1: Performance optimization, edge case handling
- Week 2: Documentation, training materials, launch preparation

**Total Timeline**: 16 weeks (4 months)

---

## 📊 **Success Metrics**

### **Technical KPIs**
- **Lock Creation Rate**: >500 locks/day across user base
- **Workflow Completion**: >80% of multi-step workflows completed successfully
- **Cross-Message Consistency**: >95% validation pass rate for locked workflows
- **Performance**: <100ms lock creation, <50ms field modification

### **Business KPIs**
- **Professional Conversion**: 25% of users using locks upgrade within 90 days
- **Feature Adoption**: 60% of active users create at least one lock per week
- **Workflow Complexity**: Average 4.5 steps per workflow (indicates serious usage)
- **Enterprise Pipeline**: 40% of teams using shared locks convert to Enterprise

### **User Experience KPIs**
- **Learning Curve**: 90% success rate on first workflow creation
- **Time Savings**: 75% reduction in test data setup time (user survey)
- **Satisfaction**: NPS >50 specifically for lock/set functionality

---

## 🎯 **Launch Strategy**

### **Beta Program**
- Target 50 existing users with complex integration scenarios
- Focus on healthcare consultants and senior developers
- Gather workflow patterns for template marketplace seeding

### **Go-to-Market**
1. **Technical Content**: "Building Realistic Healthcare Test Scenarios" blog series
2. **Community**: Share example workflows on GitHub, healthcare dev forums
3. **Partnerships**: Integration with healthcare testing frameworks
4. **Conferences**: Demo at HIMSS, HL7 Working Group meetings

### **Feature Flag Rollout**
```bash
# Progressive feature enablement
pidgeon config enable-preview lock_functionality  # Beta users
pidgeon config enable lock_functionality           # General availability
```

---

## ✅ **Implementation Acceptance Criteria**

### **Core Features**
- [ ] Can create lock from existing HL7 message
- [ ] Can set patient demographics, preserve across generations
- [ ] Can modify individual fields while maintaining consistency
- [ ] Can generate multiple message types using same patient context
- [ ] Session locks automatically clean up after 24 hours

### **Professional Features**
- [ ] Can save/load persistent templates
- [ ] Can define multi-step workflows with date progression
- [ ] Can validate workflow continuity across messages
- [ ] Template marketplace integration functional

### **Enterprise Features**
- [ ] Team lock sharing with checkout/checkin
- [ ] Complete audit trail for compliance reporting
- [ ] Role-based access controls
- [ ] Workflow approval processes

### **Quality Gates**
- [ ] All features covered by integration tests
- [ ] Performance benchmarks met (<100ms operations)
- [ ] Security review passed (no PHI persistence)
- [ ] Documentation complete with examples
- [ ] Feature flags and rollback procedures tested

---

**Summary**: The Lock/Set functionality transforms Pidgeon from a message generator into a workflow automation platform, directly addressing the most painful aspect of healthcare integration testing while creating clear upgrade incentives for Professional and Enterprise tiers.