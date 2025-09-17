# HL7 v2.3 Datatype Development Priority Order

**Purpose**: Priority-ordered development plan for 86 HL7 v2.3 datatypes based on foundational dependencies and clinical criticality.

**Strategy**: Build primitive foundation first, then composite types that depend on primitives, prioritizing healthcare workflow criticality.

**Library Coverage**:  100% supported by hl7-dictionary - MANDATORY research and validation required for each type.

---

## <¯ **TIER 1: FOUNDATIONAL PRIMITIVES** (8 types) - Week 1
**Rationale**: Everything else depends on these core primitives. Block all other development if missing.

### **Critical Foundation (4 types)**
1. **ST** (String Data) - Universal primitive used in 90% of composite types
2. **ID** (Coded values for HL7 tables) - Table lookups, codes throughout HL7
3. **NM** (Numeric) - All measurements, counts, sequences
4. **SI** (Sequence ID) - Segment sequencing, ordering logic

### **Essential Foundation (4 types)**
5. **DT** (Date) - All date fields, critical for healthcare records
6. **TM** (Time) - All time fields, medication timing, scheduling
7. **TX** (Text Data) - Clinical notes, comments, narrative text
8. **FT** (Formatted Text Data) - Rich text content with formatting

---

## <¯ **TIER 2: COMPOSITION ENABLERS** (12 types) - Week 2
**Rationale**: Core composite types that enable complex healthcare data structures.

### **Core Identity & Addressing (6 types)**
9. **CX** (Extended Composite ID With Check Digit) - Patient IDs, medical record numbers
10. **XPN** (Extended Person Name) - Patient names, provider names
11. **XAD** (Extended Address) - Patient addresses, facility locations
12. **XTN** (Extended Telecommunication Number) - Phone, email, contact info
13. **XCN** (Extended Composite ID Number And Name) - Provider identification
14. **XON** (Extended Composite Name And ID For Organizations) - Facility, department IDs

### **Healthcare Core Composites (6 types)**
15. **TS** (Time Stamp) - Date/time combinations, critical for sequencing
16. **CE** (Coded Element) - Coded medical terminology (diagnoses, procedures)
17. **HD** (Hierarchic Designator) - System identifiers, application routing
18. **EI** (Entity Identifier) - Universal entity identification
19. **PL** (Person Location) - Patient location tracking, bed management
20. **TQ** (Timing Quantity) - Medication dosing, procedure scheduling

---

## <¯ **TIER 3: CLINICAL WORKFLOW TYPES** (16 types) - Week 3
**Rationale**: Direct support for core clinical operations and documentation.

### **Clinical Documentation (8 types)**
21. **ED** (Encapsulated Data) - Embedded documents, images, reports
22. **SN** (Structured Numeric) - Lab values with units and ranges
23. **CQ** (Composite Quantity With Units) - Medication doses, lab results
24. **MO** (Money) - Financial amounts, billing information
25. **DR** (Date Time Range) - Date ranges for treatments, coverage
26. **RI** (Repeat Interval) - Recurring schedules, medication frequencies
27. **CF** (Coded Element With Formatted Values) - Complex coded values
28. **CNE** (Coded with No Exceptions) - Strictly controlled vocabularies

### **System Integration (8 types)**
29. **CN** (Composite ID Number And Name) - Basic provider identification
30. **FC** (Financial Class) - Insurance classifications, payment types
31. **IS** (Coded value for user-defined tables) - Site-specific codes
32. **CK** (Composite ID With Check Digit) - Legacy ID format support
33. **TN** (Telephone Number) - Basic phone number format
34. **CP** (Composite Price) - Complex pricing structures
35. **PT** (Processing Type) - Message processing priorities
36. **VH** (Visiting Hours) - Facility operational schedules

---

## <¯ **TIER 4: SPECIALIZED CLINICAL TYPES** (20 types) - Week 4
**Rationale**: Advanced clinical documentation and specialized healthcare operations.

### **Advanced Clinical (10 types)**
37. **NA** (Numeric Array) - Multi-value numeric data (waveforms, arrays)
38. **MA** (Multiplexed Array) - Complex multi-dimensional clinical data
39. **AD** (Address) - Basic address format (legacy support)
40. **PPN** (Performing Person Time Stamp) - Procedure performer tracking
41. **CD** (Channel Definition) - Medical device channel configuration
42. **RP** (Reference Pointer) - Cross-references between messages
43. **RCD** (Row Column Definition) - Tabular data structure definition
44. **QIP** (Query Input Parameter List) - Query parameter specifications
45. **QSC** (Query Selection Criteria) - Advanced query filtering
46. **SCV** (Scheduling Class Value Pair) - Complex scheduling attributes

### **Device & System Integration (10 types)**
47. **DLN** (Driver's License Number) - Patient identification alternative
48. **JCC** (Job Code Class) - Occupational health classifications
49. **VARIES** (Variable Datatype) - Dynamic typing support
50. **CM_MSG** (Message Type) - Message routing and identification
51. **CM_PI** (Person Identifier) - Alternative person identification
52. **CM_PTA** (Policy Type) - Insurance policy specifications
53. **CM_UVC** (Value Code And Amount) - Billing value codes
54. **CM_PCF** (Pre-certification Required) - Authorization requirements
55. **CM_PIP** (Privileges) - Provider privilege specifications
56. **CM_PLN** (Practitioner ID Numbers) - Provider licensing numbers

---

## <¯ **TIER 5: LEGACY & SPECIALIZED SUPPORT** (30 types) - Week 5-6
**Rationale**: Backward compatibility, specialized workflows, and edge case support.

### **Legacy Support Types (15 types)**
57. **CM_ABS_RANGE** (Absolute Range) - Measurement range specifications
58. **CM_AUI** (Authorization Information) - Legacy authorization format
59. **CM_CCD** (Charge Time) - Billing time specifications
60. **CM_CCP** (Channel Calibration Parameters) - Device calibration
61. **CM_CD_ELECTRODE** (Electrode Parameter) - Medical device electrodes
62. **CM_CSU** (Channel Sensitivity/units) - Device sensitivity settings
63. **CM_DDI** (Daily Deductible) - Insurance deductible tracking
64. **CM_DIN** (Activation Date) - Service activation tracking
65. **CM_DLD** (Discharge Location) - Legacy discharge tracking
66. **CM_DLT** (Delta Check) - Lab result change validation
67. **CM_DTN** (Day Type And Number) - Calendar date specifications
68. **CM_EIP** (Parent Order) - Order hierarchy relationships
69. **CM_ELD** (Error) - Legacy error reporting format
70. **CM_LA1** (Location With Address Information) - Enhanced location data
71. **CM_MDV** (Minimum/maximum Data Values) - Value range constraints

### **Specialized Workflow Types (15 types)**
72. **CM_MOC** (Charge To Practise) - Provider billing attribution
73. **CM_NDL** (Observing Practitioner) - Observation performer tracking
74. **CM_OCD** (Occurence) - Event occurrence specifications
75. **CM_OSD** (Order Sequence) - Order sequencing logic
76. **CM_OSP** (Occurence Span) - Time span tracking
77. **CM_PEN** (Penalty) - Financial penalty specifications
78. **CM_PRL** (Parent Result Link) - Result hierarchy relationships
79. **CM_RANGE** (Wertebereich) - International range format
80. **CM_RFR** (Reference Range) - Normal value range specifications
81. **CM_RI** (Interval) - Time interval specifications
82. **CM_RMC** (Room Coverage) - Room service coverage
83. **CM_SPD** (Specialty) - Medical specialty classifications
84. **CM_SPS** (Specimen Source) - Specimen collection sources
85. **CM_VR** (Value Qualifier) - Value interpretation qualifiers
86. **CM_WVI** (Channel Identifier) - Device channel identification

---

## =¨ **CRITICAL DEPENDENCIES**

### **Foundation Dependencies**
- **ALL COMPOSITE TYPES** depend on primitives (ST, ID, NM, SI, DT, TM)
- **PATIENT IDENTIFICATION** requires: ST, ID, CX, XPN, XAD
- **CLINICAL RESULTS** requires: ST, NM, CE, TS, SN, CQ
- **PROVIDER IDENTIFICATION** requires: ST, ID, XCN, HD

### **Blocking Relationships**
- **NO segments** can be completed without Tier 1 primitives
- **NO messages** can be completed without core identity types (CX, XPN)
- **NO clinical workflows** can function without TS, CE, HD
- **NO financial operations** can work without MO, FC types

### **MVP Critical Path**
**Week 1**: Tier 1 primitives enable basic string/numeric/date operations
**Week 2**: Tier 2 composites enable patient identification and core workflows
**Week 3**: Tier 3 types enable full clinical documentation
**Week 4**: Tier 4 types enable advanced healthcare operations
**Week 5-6**: Tier 5 types provide complete standard coverage

---

## =Ë **DEVELOPMENT PROCESS**

### **For Each Datatype**
1. **RESEARCH FIRST**: `node dev-tools/research-hl7-dictionary.js datatype <NAME>`
2. **CREATE TEMPLATE**: Follow _TEMPLATES/datatype_template.json structure
3. **VALIDATE ALWAYS**: `node scripts/validate-against-hl7-dictionary.js datatype <NAME>`
4. **FIX ALL ERRORS**: Zero tolerance for validation failures
5. **DELETE BACKUP**: Only after successful validation

### **Quality Gates**
-  **100% library validation pass** required before backup deletion
-  **Template compliance** verified against _TEMPLATES/datatype_template.json
-  **Component structure** matches official HL7 v2.3 specification
-  **Cross-references** properly documented for composite types

---

## <¯ **SUCCESS METRICS**

### **Completion Tracking**
- **Week 1**: 8 primitives complete ’ Enables basic operations
- **Week 2**: 20 types complete ’ Enables patient identification
- **Week 3**: 36 types complete ’ Enables clinical workflows
- **Week 4**: 56 types complete ’ Enables advanced operations
- **Week 5-6**: 86 types complete ’ Full HL7 v2.3 datatype coverage

### **Quality Verification**
- **Zero validation failures** across all implemented types
- **100% template compliance** for consistency
- **Complete dependency chain** enabling segment development
- **Foundation ready** for Tier 1 segment development

**Remember**: Datatypes are the foundation of the entire HL7 standard. Perfect accuracy here enables everything else.