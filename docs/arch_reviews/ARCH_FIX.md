# Architectural Rehabilitation Master Plan
**Status**: 🚧 ACTIVE REMEDIATION  
**Health Score**: 87/100 → Target: 98/100  
**Timeline**: 3.5-4.5 weeks (120-165 hours)  
**Last Updated**: September 2, 2025  

## 📚 **Source Documents**
*These architectural review documents contain the detailed findings that inform this rehabilitation plan:*

1. **[`PIDGEON_AR_FINAL.md`](./PIDGEON_AR_FINAL.md)** - Comprehensive 5-phase review summary with critical findings
2. **[`FUNDAMENTAL_ANALYSIS.md`](./ar_082925/FUNDAMENTAL_ANALYSIS.md)** - Sacred principles violations and SRP analysis (Phase 3)
3. **[`QUALITY_ANALYSIS.md`](./ar_082925/QUALITY_ANALYSIS.md)** - DRY violations and technical debt inventory (Phase 4)
4. **[`COHERENCE_ASSESSMENT.md`](./ar_082925/COHERENCE_ASSESSMENT.md)** - Integration assessment and pattern consistency (Phase 5)
5. **[`CLEANUP_INVENTORY.md`](./ar_082925/CLEANUP_INVENTORY.md)** - Dead code and cleanup requirements (Phase 2)
6. **[`HISTORICAL_EVOLUTION_ANALYSIS.md`](./ar_082925/HISTORICAL_EVOLUTION_ANALYSIS.md)** - Architectural evolution context (Phase 1)

---

## 🎯 **Mission Statement**
Transform Pidgeon's strong but flawed foundation (87/100) into a production-ready architecture (98/100) through systematic remediation of domain violations, code duplication, and technical debt - enabling rapid P0 MVP development without future refactoring.

## 📊 **Executive Summary**

### **Current State**
- **Build Status**: ✅ Clean (0 errors, 0 warnings)
- **Architecture Score**: 87/100 (Strong with critical violations)
- **Technical Debt**: 636 items (32 TODOs, 4 FIXMEs, 600+ duplicate lines)
- **P0 Readiness**: ❌ BLOCKED by architectural violations

### **Target State**
- **Architecture Score**: 98/100 (Production-ready)
- **Technical Debt**: <50 items (managed, non-blocking)
- **Code Duplication**: <2% (from current ~15%)
- **P0 Readiness**: ✅ UNBLOCKED with clean foundation

### **Critical Path**
1. **Week 1**: Domain boundary violations (BLOCKS EVERYTHING)
2. **Week 2**: Critical duplication & patterns
3. **Week 3**: P0-blocking debt & validation
4. **Week 4**: Polish & optimization (if time permits)

---

## 🚨 **PRIORITY 0: CRITICAL BLOCKERS** 
*Must fix before ANY new feature development*

### **P0.1: Domain Boundary Violations** 🔴
**Impact**: Violates sacred four-domain architecture, blocks clean testing  
**Effort**: 8-12 hours  
**Dependencies**: None - must be fixed first  

#### **Messaging→Clinical Violations (9 files)**
```
[ ] PIDSegment.cs:5 → imports Clinical.Entities
[ ] ORCSegment.cs:5 → imports Clinical.Entities  
[ ] RXESegment.cs:5 → imports Clinical.Entities
[ ] PV1Segment.cs:5 → imports Clinical.Entities
[ ] XPN_ExtendedPersonName.cs:5 → imports Clinical.Entities
[ ] XAD_ExtendedAddress.cs:5 → imports Clinical.Entities
[ ] ADTMessage.cs:5 → imports Clinical.Entities
[ ] RDEMessage.cs:5 → imports Clinical.Entities
[ ] PIDSegment.cs:9 → imports Clinical.Entities.MaritalStatus
```

**Fix Strategy**:
1. Create DTOs in Application layer for Clinical→Messaging transformations
2. Update segments to use DTOs instead of domain entities
3. Implement proper adapter pattern for domain translations

#### **Domain→Infrastructure Violations (12 files)**
```
[ ] PIDSegment.cs:6 → imports Infrastructure.Standards.Common.HL7
[ ] MSHSegment.cs:5 → imports Infrastructure.Standards.Common.HL7
[ ] RXESegment.cs:6 → imports Infrastructure.Standards.Common.HL7
[ ] RXRSegment.cs:5 → imports Infrastructure.Standards.Common.HL7
[ ] ORCSegment.cs:6 → imports Infrastructure.Standards.Common.HL7
[ ] PV1Segment.cs:6 → imports Infrastructure.Standards.Common.HL7
[ ] XPN_ExtendedPersonName.cs:6 → imports Infrastructure.Standards.Common.HL7
[ ] XAD_ExtendedAddress.cs:6 → imports Infrastructure.Standards.Common.HL7
[ ] XTN_ExtendedTelecommunication.cs:5 → imports Infrastructure.Standards.Common.HL7
[ ] HL7Message.cs:7-9 → imports Infrastructure.Standards.*
[ ] CE_CodedElement.cs:5 → imports Pidgeon.Core (wrong namespace)
[ ] Multiple Infrastructure files → import Pidgeon.Core instead of proper namespace
```

**Fix Strategy**:
1. Move HL7Field<T> base classes to Domain.Messaging.HL7v2.Common
2. Create domain-appropriate field abstractions
3. Update all imports to use domain types
4. Fix namespace imports (Pidgeon.Core → proper namespaces)

### **P0.2: Critical FIXME Violations** 🔴
**Impact**: Blocks P0 Feature #3 (Vendor Pattern Detection)  
**Effort**: 6-8 hours  
**Dependencies**: P0.1 completion recommended  

```
[ ] FieldPatternAnalyzer.cs:91 - Plugin delegation broken
[ ] FieldPatternAnalyzer.cs:141 - Adapter integration failure
[ ] FieldPatternAnalyzer.cs:189 - Configuration adapter missing
[ ] ConfidenceCalculator.cs:249 - Coverage calculation dependency
```

**Fix Strategy**:
1. Implement proper adapter pattern for plugin→service communication
2. Fix service dependency injection chain
3. Validate adapter integration with tests

### **P0.3: Service→Infrastructure Dependencies** 🔴
**Impact**: Violates Clean Architecture, prevents proper testing  
**Effort**: 4-6 hours  
**Dependencies**: P0.1 must be complete  

```
[ ] ConfidenceCalculator.cs:6 → imports Infrastructure.Standards.Abstractions
[ ] ConfigurationValidationService.cs:5 → imports Infrastructure.Standards.Abstractions
[ ] FieldPatternAnalyzer.cs:6 → imports Infrastructure.Standards.Abstractions
[ ] FormatDeviationDetector.cs:6 → imports Infrastructure.Standards.Abstractions
[ ] MessagePatternAnalyzer.cs:6 → imports Infrastructure.Standards.Abstractions
[ ] VendorDetectionService.cs:7 → imports Infrastructure.Standards.Abstractions
[ ] ValidationService.cs:7 → imports Standards.Common
[ ] ServiceCollectionExtensions.cs:6 → imports Infrastructure (from Common layer)
[ ] AlgorithmicGenerationService.cs:5 → imports Domain.Clinical.Entities
[ ] IGenerationService.cs:5 → imports Domain.Clinical.Entities  
[ ] IStandardMessageFactory.cs:6 → imports Domain.Clinical.Entities
[ ] IStandardVendorDetectionPlugin.cs:5 → imports Domain.Configuration.Services
[ ] HL7FieldAnalysisPlugin.cs:9-10 → imports Domain.Messaging
[ ] HL7Parser.cs:6-7 → imports Domain.Messaging.HL7v2
[ ] HL7v23MessageFactory.cs:5-7 → imports Domain entities
```

**Fix Strategy**:
1. Create Application-layer interfaces for plugin contracts
2. Move plugin interfaces to Application layer
3. Update service imports to use Application interfaces
4. Create DTOs for Infrastructure→Domain communication

### **P0.4: Result<T> Pattern Violations** 🔴
**Impact**: Inconsistent error handling, breaks business logic patterns  
**Effort**: 3-4 hours  
**Dependencies**: None  

```
[ ] HL7Message.cs:147 → throws ArgumentNullException instead of Result<T>
[ ] HL7Message.cs:347-348 → throws ArgumentException for business logic
[ ] ConfigurationAddress.cs:37,41-43 → throws ArgumentException
[ ] MessagePattern.cs:101 → throws ArgumentException for business logic
[ ] VendorConfiguration.cs:95 → throws ArgumentException
```

**Fix Strategy**:
1. Replace ArgumentException with Result<T>.Failure() for business logic
2. Keep ArgumentNullException only for constructor validation
3. Update method signatures to return Result<T>
4. Update calling code to handle Result<T> returns

### **P0.5: CLI Dependency Injection Violations** 🔴
**Impact**: Breaks plugin architecture, prevents proper testing  
**Effort**: 2-3 hours  
**Dependencies**: None  

```
[ ] ConfigCommand.cs:56 → Direct GenerationService instantiation
[ ] GenerateCommand.cs:33 → Direct GenerationService instantiation  
[ ] InfoCommand.cs:30 → Direct service instantiation pattern
```

**Fix Strategy**:
1. Inject IGenerationService through constructor DI
2. Update command constructors to use proper dependency injection
3. Remove direct service instantiation patterns

---

## 🔥 **PRIORITY 1: HIGH-IMPACT FIXES**
*Fix immediately after P0 to prevent exponential debt growth*

### **P1.1: CLI Command Pattern Duplication** 🟠
**Impact**: 300+ duplicate lines, maintenance nightmare  
**Effort**: 12-16 hours  
**Dependencies**: None  

```
[ ] ConfigCommand.cs - 4 identical command methods (lines 39-337)
[ ] GenerateCommand.cs - Duplicate option patterns (lines 31-58)
[ ] ValidateCommand.cs - Same command structure (lines 32-51)
[ ] Program.cs - Repetitive command registration (lines 97-101)
```

**Fix Strategy**:
1. Extract CommandBuilder base class with option creation patterns
2. Create CommandExecutor for common execution logic
3. Implement template method pattern for command-specific logic
4. Use convention-based command registration

### **P1.2: HL7Field Constructor Trinity Pattern** 🟠
**Impact**: 270+ duplicate lines across 15+ field types  
**Effort**: 8-12 hours  
**Dependencies**: P0.1 (domain boundaries fixed)  

```
[ ] StringField.cs - Trinity pattern (lines 24-48)
[ ] NumericField.cs - Trinity pattern (lines 25-47)
[ ] DateField.cs - Trinity pattern (lines 24-59)
[ ] TimestampField.cs - Trinity pattern (lines 25-59)
[ ] PersonNameField.cs - Same pattern (lines 19-37)
[ ] AddressField.cs - Same pattern (lines 19-37)
[ ] TelephoneField.cs - Same pattern (lines 19-27)
[ ] + 8 more field types with identical pattern
```

**Fix Strategy**:
1. Create HL7FieldBase<T> with protected constructor patterns
2. Implement factory method in base class
3. Override only type-specific parsing logic
4. Consolidate validation patterns

### **P1.3: Service Registration Explosion** 🟠
**Impact**: 22+ identical AddScoped calls, brittle DI setup  
**Effort**: 6-8 hours  
**Dependencies**: None  

```
[ ] ServiceCollectionExtensions.cs:28-49 - 22 identical registrations
[ ] Program.cs:66-73 - 5 identical AddScoped patterns
[ ] Multiple plugin registrations with same pattern
```

**Fix Strategy**:
1. Implement convention-based service registration
2. Create ServiceRegistrar with assembly scanning
3. Use attribute-based service marking
4. Consolidate plugin registration patterns

### **P1.4: Configuration Entity Property Explosion** 🟠
**Impact**: 500+ duplicate property lines, 50+ JsonPropertyName attributes  
**Effort**: 20-30 hours  
**Dependencies**: None (can parallelize)  

```
[ ] FieldFrequency.cs:14-219 - 4 nearly identical property sets 
[ ] FormatDeviation.cs:14-270 - 4 property set duplications
[ ] MessagePattern.cs:12-91 - Complex property duplication
[ ] VendorConfiguration.cs:14-51 - Property pattern duplication
[ ] ConfigurationMetadata.cs:12-54 - Similar property patterns
[ ] ValidationResult.cs - 3 records with identical property patterns
[ ] ServiceInfo.cs:12-162 - 4 records with similar structures
[ ] All with repetitive JsonPropertyName attributes (50+ occurrences)
```

**Fix Strategy**:
1. Create base configuration records with shared properties
2. Implement property composition pattern
3. Create JsonPropertyName convention handler
4. Consolidate dictionary initialization patterns

### **P1.5: MSH Field Extraction Duplication** 🟠
**Impact**: Core HL7 processing logic massively duplicated  
**Effort**: 4-6 hours  
**Dependencies**: P0.1  

```
[ ] HL7FieldAnalysisPlugin.cs:119-154 - 4 identical MSH extraction methods
[ ] All use same mshSegment.Split('|') + null checks pattern
[ ] ExtractMessageControlId, ExtractSendingSystem, ExtractReceivingSystem, ExtractVersionId
```

**Fix Strategy**:
1. Create MshHeaderParser utility class
2. Consolidate field extraction logic
3. Use generic field accessor pattern

### **P1.6: Vendor Detection Pattern Duplication** 🟠
**Impact**: Core vendor detection severely duplicated  
**Effort**: 6-10 hours  
**Dependencies**: P0.2  

```
[ ] HL7VendorDetectionPlugin.cs:57-81 - Header extraction pattern
[ ] Multiple pattern evaluation loops (lines 135-146, 176-185, 190-199, 204-213)
[ ] 5 identical regex patterns with same structure
[ ] Rule evaluation logic repetition (lines 227-258)
```

**Fix Strategy**:
1. Create PatternEvaluationFramework
2. Consolidate regex pattern processing  
3. Extract rule evaluation engine

### **P1.7: Algorithmic Generation Service Duplication** 🟠
**Impact**: Generation logic heavily duplicated  
**Effort**: 6-8 hours  
**Dependencies**: P0.1  

```
[ ] AlgorithmicGenerationService.cs:28-180 - 4 identical generate method patterns
[ ] Random seeding pattern repetition (lines 33, 76, 123, 156)
[ ] Try-catch error handling structure duplication across all methods
[ ] Same Result<T> return structure
```

**Fix Strategy**:
1. Extract GenerateMethodBase template
2. Consolidate random seeding logic
3. Create unified error handling pattern

### **P1.8: Plugin Registry Retrieval Duplication** 🟠
**Impact**: Plugin access patterns duplicated everywhere  
**Effort**: 4-6 hours  
**Dependencies**: P0.3  

```
[ ] StandardPluginRegistry.cs:96-109 - Identical plugin retrieval patterns
[ ] FieldPatternAnalyzer.cs:45-50,84-89,134-139,182-187 - 4 identical blocks
[ ] MessagePatternAnalyzer.cs:86-92 - Same plugin retrieval
[ ] GenerationService.cs:67-74,99-106,121-128 - 3 identical plugin blocks
```

**Fix Strategy**:
1. Create PluginAccessor base class
2. Implement generic plugin retrieval pattern
3. Use composition over duplication

---

## 💼 **PRIORITY 2: STRUCTURAL IMPROVEMENTS**
*Important architectural quality issues*

### **P2.1: Single Responsibility Principle Violations** 🟡
**Impact**: Complex testing, maintenance burden  
**Effort**: 6-8 hours  
**Dependencies**: P0 completion recommended  

```
[ ] Program.cs:107-176 → Console helpers embedded in entry point
[ ] MessagePatternAnalyzer.cs → Mixes orchestration with analysis implementation
[ ] IConfigurationCatalog.cs → Interface too large (129 lines, multiple responsibilities)
[ ] MessagePattern.cs:98-144 → Complex merge logic mixed with entity definition
```

**Fix Strategy**:
1. Extract Console helpers (TableFormatter, ProgressBar) to separate classes
2. Split MessagePatternAnalyzer into orchestrator + analysis service
3. Segregate IConfigurationCatalog into focused interfaces
4. Move complex merge logic to domain service

## 💼 **PRIORITY 2B: P0-BLOCKING TODOS**
*Must complete for MVP features to work*

### **P2B.1: Message Generation TODOs** 🟡
**Impact**: Blocks P0 Feature #1 (Generate Messages)  
**Effort**: 8-16 hours  
**Dependencies**: P0.1, P0.2  

```
[ ] GenerationService.cs:70 - Implement plugin delegation
[ ] GenerationService.cs:102 - Complete RDE generation
[ ] GenerationService.cs:124 - Implement custom message support
[ ] ADTMessage.cs:70,77,78 - Complete segment initialization
```

### **P2B.2: Message Validation TODOs** 🟡
**Impact**: Blocks P0 Feature #2 (Validate Messages)  
**Effort**: 16-24 hours  
**Dependencies**: P0.1  

```
[ ] HL7Message.cs:267 - Implement parsing logic
[ ] HL7Message.cs:512 - Complete validation framework
[ ] GenericHL7Message.cs:27,36 - Generic parsing/serialization
```

### **P2B.3: Message Factory TODOs** 🟡
**Impact**: Blocks multi-version support  
**Effort**: 12-20 hours  
**Dependencies**: P2.1, P2.2  

```
[ ] HL7v23MessageFactory.cs:121,137,147 - Complete factory methods
[ ] HL7v24MessageFactory.cs:29,38,47,56,65 - Implement v2.4 support
[ ] HL7v24Plugin.cs:46,53,60,67 - Complete plugin implementation
```

---

## 🎨 **PRIORITY 3: QUALITY IMPROVEMENTS**
*Important but not blocking*

### **P3.1: Segment Pattern Duplication** 🟢
**Impact**: Maintenance burden, error propagation  
**Effort**: 8-12 hours  
**Dependencies**: P1.2  

```
[ ] PIDSegment.cs - InitializeFields pattern (lines 161-185)
[ ] MSHSegment.cs - Same InitializeFields pattern (lines 37-86)
[ ] PV1Segment.cs - Same InitializeFields pattern (lines 45-118)
[ ] ORCSegment.cs - Same pattern (lines 20-57)
[ ] RXESegment.cs - Same pattern (lines 29-75)
[ ] RXRSegment.cs - Same pattern (lines 25-38)
```

### **P3.2: Clinical Entity Validation Duplication** 🟢
**Impact**: Business logic duplication  
**Effort**: 8-12 hours  
**Dependencies**: None  

```
[ ] Patient.cs - Validation pattern (lines 124-134)
[ ] Medication.cs - Same validation structure (lines 93-105)
[ ] Provider.cs - Same validation structure (lines 113-129)
[ ] Encounter.cs - Same validation structure (lines 119-137)
[ ] Extract base validation pattern
```

### **P3.3: Plugin Structure Duplication** 🟢
**Impact**: Maintenance burden for multi-version support  
**Effort**: 16-24 hours  
**Dependencies**: P2.3  

```
[ ] HL7v23Plugin.cs vs HL7v24Plugin.cs - Entire structure duplicated
[ ] HL7v23MessageFactory.cs vs HL7v24MessageFactory.cs - Complete duplication
[ ] Extract version-agnostic base implementations
```

### **P3.4: Test Pattern Duplication** 🟢
**Impact**: Test maintenance burden  
**Effort**: 4-6 hours  
**Dependencies**: None  

```
[ ] HL7ParserTests.cs - 6 identical test method structures
[ ] Extract test fixture base class
[ ] Implement data-driven tests
```

---

## 🧹 **PRIORITY 4: CLEANUP & POLISH**
*Nice to have, do if time permits*

### **P4.1: Meta-Commentary Removal** 🔵
**Impact**: Professional code quality  
**Effort**: 2-3 hours  
**Dependencies**: None  

```
[ ] HL7ToConfigurationAdapter.cs:16-26 - Remove architectural commentary
[ ] IClinicalToMessagingAdapter.cs:17-22 - Remove meta-commentary
[ ] IMessagingToClinicalAdapter.cs:17-22 - Remove meta-commentary
[ ] IMessagingToConfigurationAdapter.cs - Remove meta-commentary
```

### **P4.2: Namespace & Import Violations** 🔵
**Impact**: Inconsistent namespace usage  
**Effort**: 2-3 hours  
**Dependencies**: None  

```
[ ] IConfigurationPlugin.cs:7 → Application interface in Domain namespace
[ ] test_parser.cs:2 → Incorrect namespace import
[ ] Multiple Infrastructure files → import Pidgeon.Core instead of specific namespaces
[ ] IStandardMessage.cs:5-6 → imports Pidgeon.Core + Standards.Common
[ ] IStandardValidator.cs:5-6 → imports Pidgeon.Core + Standards.Common
```

**Fix Strategy**:
1. Move Application interfaces to proper Application namespace
2. Fix test imports to use correct namespaces
3. Update Infrastructure imports to use specific namespaces
4. Standardize Common namespace usage

### **P4.3: Placeholder Implementation** 🔵
**Impact**: Feature completeness  
**Effort**: Variable (40+ hours total)  
**Dependencies**: P0-P2 complete  

```
[ ] 12 placeholder message types (ACK, BAR, DFT, MDM, etc.)
[ ] 8 placeholder segment types (AL1, DG1, EVN, GT1, etc.)
[ ] 5 placeholder data types (CX, EI, HD, TS)
```

### **P4.4: Documentation Hardcoding** 🔵
**Impact**: Standard-agnostic documentation  
**Effort**: 2-4 hours  
**Dependencies**: None  

```
[ ] Remove hardcoded "ADT"/"RDE" examples from interfaces
[ ] Update documentation to use generic placeholders
[ ] Ensure standard-agnostic descriptions
```

### **P4.5: Enum/Constant Duplication** 🔵
**Impact**: Code organization  
**Effort**: 3-5 hours  
**Dependencies**: None  

```
[ ] Consolidate similar enum patterns
[ ] Extract shared constants
[ ] Create centralized healthcare constants
```

---

## 📈 **Progress Tracking**

### **Week 1 Targets (45 hours)**
- [ ] P0.1: Domain Boundary Violations (12 hours)
- [ ] P0.2: Critical FIXME Violations (8 hours)
- [ ] P0.3: Service→Infrastructure Dependencies (6 hours)
- [ ] P0.4: Result<T> Pattern Violations (4 hours)
- [ ] P0.5: CLI Dependency Injection Violations (3 hours)
- [ ] P1.1: CLI Command Pattern (start - 12 hours)

### **Week 2 Targets (45-50 hours)**
- [ ] P1.1: CLI Command Pattern (complete)
- [ ] P1.2: HL7Field Constructor Trinity (12 hours)
- [ ] P1.3: Service Registration Explosion (8 hours)
- [ ] P1.5: MSH Field Extraction Duplication (6 hours)
- [ ] P1.6: Vendor Detection Pattern Duplication (10 hours)
- [ ] P1.4: Configuration Entity Properties (start - 15 hours)

### **Week 3 Targets (40-45 hours)**
- [ ] P1.4: Configuration Entity Properties (complete)
- [ ] P1.7: Algorithmic Generation Service Duplication (8 hours)
- [ ] P1.8: Plugin Registry Retrieval Duplication (6 hours)
- [ ] P2.1: Single Responsibility Violations (8 hours)
- [ ] P2B.1: Message Generation TODOs (16 hours)

### **Week 4 Targets (if needed)**
- [ ] P2B.2: Message Validation TODOs (complete)
- [ ] P2B.3: Message Factory TODOs
- [ ] P3.1-P3.4: Quality improvements as time permits

---

## 🎯 **Success Metrics**

### **Architectural Health**
| Metric | Current | Week 1 | Week 2 | Week 3 | Target |
|--------|---------|--------|--------|--------|--------|
| Health Score | 87/100 | 91/100 | 94/100 | 97/100 | 98/100 |
| Domain Violations | 21 | 0 | 0 | 0 | 0 |
| Infrastructure Violations | 20 | 0 | 0 | 0 | 0 |
| Critical FIXMEs | 4 | 0 | 0 | 0 | 0 |
| Result<T> Violations | 5 | 0 | 0 | 0 | 0 |
| SRP Violations | 4 | 4 | 2 | 0 | 0 |
| P0-Blocking TODOs | 32 | 32 | 20 | 0 | 0 |

### **Code Quality**
| Metric | Current | Week 1 | Week 2 | Week 3 | Target |
|--------|---------|--------|--------|--------|--------|
| Duplicate Lines | 600+ | 550+ | 250 | 75 | <50 |
| Duplication Rate | ~15% | ~13% | 6% | 2% | <2% |
| Technical Debt Items | 636 | 580 | 350 | 80 | <50 |
| Critical Violations | 26 | 0 | 0 | 0 | 0 |

### **P0 Feature Readiness**
| Feature | Current | Week 1 | Week 2 | Week 3 |
|---------|---------|--------|--------|--------|
| 1. Generate Messages | ❌ Blocked | ❌ Blocked | 🟡 Partial | ✅ Ready |
| 2. Validate Messages | ❌ Blocked | ❌ Blocked | 🟡 Partial | ✅ Ready |
| 3. Vendor Detection | ❌ Blocked | ✅ Ready | ✅ Ready | ✅ Ready |
| 4. Format Debugging | ❌ Blocked | 🟡 Partial | ✅ Ready | ✅ Ready |
| 5. Test Data | ❌ Blocked | 🟡 Partial | ✅ Ready | ✅ Ready |

---

## 🚀 **Implementation Guidelines**

### **Fix Principles**
1. **No Band-Aids**: Fix root causes, not symptoms
2. **Test First**: Write tests before implementing fixes
3. **Document Decisions**: Update LEDGER.md for architectural changes
4. **Preserve Behavior**: Ensure no regressions during refactoring
5. **Incremental Progress**: Commit working code frequently

### **Code Standards During Fixes**
- Use Result<T> pattern for all new error handling
- Follow existing naming conventions (PascalCase, _camelCase)
- Remove meta-commentary and development artifacts
- Add proper XML documentation for public APIs
- Update affected unit tests

### **Review Checkpoints**
- [ ] Daily: Update completed items in this document
- [ ] Weekly: Validate health score improvements
- [ ] Per Fix: Run full test suite
- [ ] Per Priority: Architecture validation sweep

---

## 📝 **Notes & Decisions Log**

### **Decisions Made**
- Prioritizing domain violations over duplication (unblocks everything)
- Batching similar fixes together for efficiency
- Deferring placeholder implementations until after P0

### **Risks & Mitigations**
- **Risk**: Fix cascade causing new issues → **Mitigation**: Comprehensive testing
- **Risk**: Timeline overrun → **Mitigation**: Focus on P0 blockers only
- **Risk**: Scope creep → **Mitigation**: Strict priority adherence

### **Dependencies Discovered**
- Configuration entity refactoring may impact serialization
- Domain boundary fixes require adapter pattern implementation
- Service registration changes affect entire DI container

---

## ✅ **Completion Criteria**

### **Foundation Ready for P0 When:**
- [ ] Zero domain boundary violations
- [ ] Zero infrastructure dependency violations
- [ ] All critical FIXMEs resolved
- [ ] All P0-blocking TODOs complete
- [ ] Code duplication <5%
- [ ] Health score ≥95/100
- [ ] All P0 features unblocked
- [ ] Full test suite passing

### **Sign-Off Checklist**
- [ ] Architectural review passed
- [ ] Performance benchmarks met
- [ ] Documentation updated
- [ ] LEDGER.md current
- [ ] Team consensus on completion

---

**Document Status**: Living document - update as fixes are completed  
**Owner**: Architecture Team  
**Review Frequency**: Daily during rehabilitation sprint  
**Next Review**: End of Week 1 milestone check