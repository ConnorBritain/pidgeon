# Pidgeon Development Plan - Aligned Feature Roadmap
**Version**: 3.0  
**Updated**: September 5, 2025  
**Status**: Definitive roadmap aligned with feature vision  
**North Star**: Realistic scenario testing without the legal/compliance nightmare of using real patient data

---

## 🎯 **Current Status - Foundation Complete**

### **Architectural Health**
**Health Score**: **100/100** - All rehabilitation complete ✅  
**Build Status**: Clean (0 errors, 43 tests passing) ✅  
**P0 Readiness**: **READY** - Foundation issues resolved ✅  

### **Completed Foundation Work** 
✅ All P0-P4 architectural priorities completed (Sept 4, 2025)
✅ Domain boundary violations: 0 (was 21)
✅ Infrastructure dependencies: 0 (was 20)  
✅ Code duplication: <2% (was 15%)
✅ Critical FIXMEs: 0 (was 4)
✅ Professional code standards applied

**Ready for P0 MVP Development** with perfect architectural foundation

---

## 🎯 **P0 — MVP Foundation (Weeks 1-6)**
**Duration**: 6 weeks  
**Goal**: Prove value to design partners; deliver essential daily-use workflows  
**Strategy**: CLI-first approach with clear Free/Pro/Enterprise feature gating

### **P0 Core Features**

#### **1. Healthcare Message Generation Engine** 🆓
**User Story**: "Generate realistic HL7/FHIR test messages for daily integration testing"  
**Scope**:
- HL7 v2.3: ADT, ORU, RDE/RXO/RXR message types
- FHIR R4: Patient, Encounter, Observation, Medication, MedicationRequest
- NCPDP SCRIPT: NewRx, Refill, Cancel (basic)
- Deterministic generation with seed support
- 25 medications, 50 names, age-appropriate conditions

**CLI Commands**:
```bash
pidgeon generate message --type ADT^A01
pidgeon generate message --type ORU^R01 --count 10 --output labs.hl7
pidgeon generate bundle --standard fhir --resource Observation --count 100 -o obs.ndjson
```

#### **2. Message Validation Engine** 🆓
**User Story**: "Understand exactly why vendor messages fail validation for quick fixes"  
**Scope**:
- HL7 v2 parser handling real-world vendor quirks
- Validation modes: Strict (compliance) vs Compatibility (vendor reality)
- Field-level errors with specific paths and fixes
- Segment analysis for missing/unexpected fields

**CLI Commands**:
```bash
pidgeon validate --file labs.hl7 --mode strict
pidgeon validate --file labs.hl7 --mode compatibility --report validation.html
```

#### **3. On-Premises De-identification** 🆓 **NEW**
**User Story**: "Import real messages, replace identifiers while preserving scenario integrity"  
**Scope**:
- Deterministic ID remapping (consistent across messages)
- Date shifting with configurable offsets
- Cross-message consistency preservation
- Fully on-prem, optional lightweight local models

**CLI Commands**:
```bash
pidgeon deident --in ./samples --out ./synthetic --date-shift 30d
pidgeon deident --in msg.hl7 --out msg_safe.hl7 --salt "team-seed" --preview
```

#### **4. Vendor Pattern Detection** 🆓
**User Story**: "Quickly understand vendor-specific patterns for correct interface configuration"  
**Scope**:
- Pattern inference from sample messages
- Auto-generate vendor-specific validation rules
- Pre-built Epic, Cerner, AllScripts, Meditech profiles
- Pattern library for saving configurations

**CLI Commands**:
```bash
pidgeon config analyze --samples ./inbox --save epic_er.json
pidgeon config use --name epic_er.json
pidgeon config list
```

#### **5. Workflow Wizard** 🔒 **[Pro]**
**User Story**: "Guided flow for creating multi-step test scenarios with validation checklists"  
**Scope**:
- Interactive wizard: base patient → scenario selection → vendor config
- Step-by-step prompts or scenario file input
- Output validation checklists
- Common workflows: admit → lab → discharge

**CLI Commands**:
```bash
pidgeon workflow wizard  # Interactive guided setup
pidgeon workflow run --file scenarios/admit_lab_rx.yml
pidgeon workflow list
```

#### **6. Diff + AI Triage** 🔒 **[Pro]**
**User Story**: "Visual diff of message problems for troubleshooting in minutes, not hours"  
**Scope**:
- Side-by-side field-level comparison
- HL7 field-aware, FHIR JSON tree diff
- AI-powered "why it broke" suggestions
- Confidence scores on suggested fixes

**CLI Commands**:
```bash
pidgeon diff --left ./envA --right ./envB --report diff.html
pidgeon diff --left old.hl7 --right new.hl7 --ignore MSH-7,PV1.44
```

### **P0 Packaging & Business Model**
- **CLI Core**: Always free, unlimited procedural generation
- **Community GUI**: Free with capped messages (100/day)
- **Pro ($29/mo)**: Workflow Wizard, Diff+Triage, local AI models, unlimited generation
- **Enterprise ($199/seat)**: SSO, audit logs, private models, advanced profiles

### **P0 Success Metrics**
- **Week 2**: 25+ active users providing feedback
- **Week 4**: 50+ users with 60% weekly engagement
- **Week 6**: 100+ users, 20% trying Pro features
- **Technical**: <50ms generation, <20ms validation, 99% uptime

---

## 📨 **P1 — Market Expansion (Months 2-4)**
**Duration**: 12 weeks  
**Goal**: Lock in early logos, expand FHIR reach, build stickiness  
**Strategy**: Add configuration intelligence and broader standards support

### **P1 Expansion Features**

#### **1. Configuration Manager v1** 🔒 **[Pro]**
**User Story**: "Track vendor configuration changes over time to prevent interface breakages"  
**Scope**:
- Infer specifications from message traffic
- Version control with diff visualization
- Library of validated patterns
- Change detection and alerts

**CLI Commands**:
```bash
pidgeon config analyze --samples ./traffic --save v2.json
pidgeon config diff --left v1.json --right v2.json
```

#### **2. Vendor Spec Guide + Trust Hub** 🔒 **[Pro/Ent]**
**User Story**: "Centralized vendor documentation with compliance verification"  
**Scope**:
- Upload vendor specifications
- Annotate validation rule checks
- Link errors to spec clauses
- Shareable attested profiles

#### **3. FHIR R4 Expansion** 🆓
**User Story**: "Comprehensive FHIR test data for modern API integrations"  
**Scope**:
- Extended resources: Practitioner, Organization, Location
- Clinical resources: AllergyIntolerance, Condition, Procedure
- DiagnosticReport, ServiceRequest resources
- FHIR search test harness

#### **4. Message Studio v1** 🔒 **[Pro]**
**User Story**: "Visual interface to create test messages without learning syntax"  
**Scope**:
- Natural language → message generation
- Structured editor with field hints
- GUI complement to CLI
- Template library

#### **5. Standards-Tuned Chatbot** 🔒 **[Pro]**
**User Story**: "AI assistance for understanding complex messages"  
**Scope**:
- "Explain this message" functionality
- HL7 ↔ FHIR field mapping
- Read-only analysis mode
- Healthcare context awareness

#### **6. Targeted Packs**
- **Mirth Migration Pack** ($199 add-on): Regression testing for Mirth upgrades
- **Redox Pre-flight Pack** (Enterprise): Network API testing scenarios

### **P1 Success Metrics**
- **Users**: 500+ active (5x growth from P0)
- **Revenue**: $15K+ MRR, 25% from Professional tier
- **Partners**: 5+ case studies from design partners
- **Technical**: 1,000+ concurrent users, 99% uptime

## 🚀 **P2 — Defensible Moats & Scale (Months 4-9)**
**Duration**: 24 weeks  
**Goal**: Become indispensable enterprise quality layer  
**Strategy**: Build competitive advantages that can't be easily replicated

### **P2 Platform Differentiation**

#### **1. Cross-Standard Transforms (GA)** 🆕 **[Enterprise]**
**User Story**: "Seamless data transformation between HL7, FHIR, and NCPDP"  
**Scope**:
- Bidirectional HL7 ↔ FHIR ↔ NCPDP conversions
- Terminology integration: LOINC, RxNorm, SNOMED
- Semantic preservation with data loss detection
- Custom mapping rules

#### **2. Team & Governance** 🆕 **[Enterprise]**
**User Story**: "Team governance tools for large healthcare IT organizations"  
**Scope**:
- Project workspaces with isolated environments
- Role-based access: Admin, Lead, Developer, Viewer
- SSO integration: SAML, OAuth, LDAP
- Approval workflows and audit trails

#### **3. Profiles at Scale** 🆕 **[Enterprise Premium]**
**User Story**: "Comprehensive Implementation Guide validation matrix"  
**Scope**:
- Full IG/profile validation matrix
- Cached terminology services (VSAC, THO)
- Sub-100ms validation for complex profiles
- Automated compliance reporting

#### **4. Trust Hub v2 & Marketplace**
**User Story**: "Monetize vendor expertise through content marketplace"  
**Scope**:
- Shareable vendor packs and consultancy blueprints
- Revenue-sharing ecosystem (70/30 split)
- Community-driven quality ratings
- Trust scoring and certifications

### **P2 Success Metrics**
- **Scale**: 1,000+ Enterprise users, 10,000+ total
- **Revenue**: $100K+ MRR, 60% from Enterprise
- **Market**: Top 3 healthcare interop platforms
- **Partnerships**: 5+ official EHR vendor relationships
- **Network**: 500+ marketplace contributions

---

## 🔄 **Development Process & Standards**

### **Sprint Structure (2-week cycles)**
- **Sprint Planning**: User story prioritization, technical dependency mapping
- **Daily Standups**: Blocker resolution, architectural decision alignment
- **Sprint Review**: User feedback integration, metric analysis
- **Sprint Retrospective**: Process improvement, architectural lessons learned

### **Quality Gates**
- **Code Review**: All changes require architectural compliance check
- **Automated Testing**: Unit, integration, and behavior-driven tests
- **Performance Monitoring**: Response time and throughput validation
- **Security Scanning**: Dependency vulnerabilities, code security analysis

### **Documentation Standards**
- **Architecture Decisions**: All major decisions recorded in LEDGER.md
- **API Documentation**: OpenAPI specs for all endpoints
- **User Documentation**: Clear guides for each P0 feature
- **Code Documentation**: Professional XML documentation, no meta-commentary

### **Deployment Process**
- **Staging Environment**: Full production replica for testing
- **Blue-Green Deployment**: Zero-downtime deployments
- **Rollback Capability**: Immediate rollback for any production issues
- **Monitoring**: Application performance, error rates, user behavior

---

## 🎯 **Risk Management**

### **Technical Risks & Mitigation**

#### **Foundation Work Underestimation**
- **Risk**: Domain boundary fixes take longer than 3 weeks
- **Mitigation**: Daily progress tracking, scope reduction if needed
- **Escalation**: Consider pair programming for complex adapter implementations

#### **Performance Degradation**  
- **Risk**: Message processing slower than 50ms target
- **Mitigation**: Performance testing throughout development, profiling tools
- **Escalation**: Architecture review for optimization opportunities

#### **User Adoption Slower Than Expected**
- **Risk**: <100 users by end of P0 development
- **Mitigation**: Design partner engagement, early beta program
- **Escalation**: Pivot to focused user segment, adjust feature priorities

### **Business Risks & Mitigation**

#### **Free Tier Too Generous**
- **Risk**: No conversion pressure to Professional tier
- **Mitigation**: Monitor usage patterns, adjust limits based on data
- **Escalation**: Implement soft limits with upgrade prompts

#### **Market Education Required**
- **Risk**: Healthcare IT teams don't understand need for testing tools
- **Mitigation**: Content marketing, case studies from design partners
- **Escalation**: Partner with healthcare IT consultancies for validation

---

## 📊 **Success Metrics & Monitoring**

### **Development Velocity**
- **Story Points**: Target 40 points per 2-week sprint
- **Code Quality**: <10% bug rate in production
- **Technical Debt**: Maintain architectural health >90/100
- **Feature Delivery**: 95% on-time delivery of committed features

### **User Adoption**
- **Weekly Active Users**: Target 60%+ of total registered users
- **Feature Usage**: All 5 P0 features used by >80% of active users
- **Time to Value**: Users generate first useful message within <5 minutes
- **User Satisfaction**: >4.0/5.0 rating on core workflows

### **Business Metrics**
- **Conversion Rate**: >20% of users try Professional features
- **Retention**: >80% of users still active after 30 days
- **Engagement**: Average 3+ sessions per week per active user
- **Referrals**: >30% of new users from existing user referrals

---

## 🚀 **Post-P0 Planning**

### **P1 Preparation (Month 3)**
- Advanced configuration intelligence
- FHIR R4 expansion with additional resources
- Message Studio GUI for non-technical users
- Healthcare AI assistant for message explanation

### **Technical Infrastructure Scaling**
- Multi-region deployment capability
- Advanced monitoring and alerting
- Enterprise security features (SSO, RBAC)
- API rate limiting and usage analytics

### **Market Expansion**
- Enterprise sales process development  
- Partner program with healthcare consultancies
- Industry conference presence and speaking
- Thought leadership content and case studies

---

**Development Status**: Foundation complete (100/100 health). Ready for P0 MVP development with clear feature roadmap and CLI-GUI harmonization strategy.

**Next Actions**: Begin P0 feature development with CLI-first approach. Prioritize core engines and de-identification for immediate user value.

**Success Definition**: P0 MVP with 6 core features delivered in 6 weeks, 100+ engaged users, 20% Pro conversion, and validated roadmap for P1-P2 expansion.