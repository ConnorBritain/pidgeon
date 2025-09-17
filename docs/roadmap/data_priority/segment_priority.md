# HL7 v2.3 Segment Development Priority Order

**Purpose**: Priority-ordered development plan for 140+ HL7 v2.3 segments based on clinical workflow criticality and message dependencies.

**Strategy**: Build universal infrastructure first, then patient-centric workflows, followed by specialized clinical operations.

**Library Coverage**:  100% supported by hl7-dictionary - MANDATORY research and validation required for each segment.

**Dependencies**: Requires Tier 1-2 datatypes (ST, ID, NM, CX, XPN, TS, CE, HD) completed first.

---

## <¯ **TIER 1: UNIVERSAL INFRASTRUCTURE** (6 segments) - Week 1
**Rationale**: Every message uses these segments. Block all message development if missing.

### **Message Foundation (3 segments)**
1. **MSH** (Message Header) - Every HL7 message starts with MSH, controls routing and processing
2. **MSA** (Message Acknowledgement) - Universal response mechanism for all transactions
3. **ERR** (Error) - Standard error reporting across all message types

### **Clinical Foundation (3 segments)**
4. **EVN** (Event Type) - Event details for all ADT, order, and result messages
5. **NTE** (Notes and Comments) - Attachable to any segment for additional clinical information
6. **DSC** (Continuation Pointer) - Message fragmentation support for large datasets

---

## <¯ **TIER 2: PATIENT IDENTIFICATION & DEMOGRAPHICS** (8 segments) - Week 2
**Rationale**: Patient identity is prerequisite for all clinical workflows. Core healthcare operations.

### **Patient Core (4 segments)**
7. **PID** (Patient Identification) - Primary patient demographics and identifiers
8. **PD1** (Patient Demographics) - Additional patient information and preferences
9. **NK1** (Next of Kin) - Emergency contacts and family information
10. **MRG** (Merge Patient Information) - Patient record merging and identity management

### **Visit Management (4 segments)**
11. **PV1** (Patient Visit) - Encounter information, admission, location, providers
12. **PV2** (Patient Visit Additional) - Extended visit information and administrative details
13. **ROL** (Role) - Person/provider roles in patient care and encounters
14. **ACC** (Accident) - Accident and injury details for workers compensation and liability

---

## <¯ **TIER 3: CLINICAL ORDERS & RESULTS** (12 segments) - Week 3
**Rationale**: Core clinical workflow - orders drive healthcare processes, results provide outcomes.

### **Order Management (6 segments)**
15. **ORC** (Common Order) - Universal order control for all clinical orders
16. **OBR** (Observation Request) - Lab orders, radiology orders, procedure requests
17. **RQD** (Requisition Detail) - Supply and stock requisition orders
18. **RQ1** (Requisition Detail-1) - Enhanced requisition information
19. **SPR** (Stored Procedure Request) - Database query and stored procedure requests
20. **QRD** (Query Definition) - Query structure for information requests

### **Results & Observations (6 segments)**
21. **OBX** (Observation/Result) - Lab results, vital signs, clinical observations
22. **QRF** (Query Filter) - Query response filtering and parameters
23. **RDF** (Table Row Definition) - Tabular data structure definition
24. **RDT** (Table Row Data) - Tabular data content for structured results
25. **DSP** (Display Data) - Formatted display of query responses
26. **QAK** (Query Acknowledgement) - Query processing status and metadata

---

## <¯ **TIER 4: PROVIDER & ORGANIZATION MANAGEMENT** (10 segments) - Week 4
**Rationale**: Healthcare requires provider tracking, credentials, and organizational context.

### **Provider Information (5 segments)**
27. **STF** (Staff Identification) - Healthcare provider demographics and identifiers
28. **PRA** (Practitioner Detail) - Provider credentials, specialties, and capabilities
29. **PRD** (Provider Data) - Additional provider information and contact details
30. **CTD** (Contact Data) - Contact information for providers and organizations
31. **ORG** (Practitioner Organization Unit) - Provider organizational relationships

### **Location & Facility (5 segments)**
32. **LOC** (Location Identification) - Healthcare facility locations and room information
33. **LDP** (Location Department) - Department and service area definitions
34. **LCH** (Location Characteristic) - Location attributes and capabilities
35. **LCC** (Location Charge Code) - Location-based billing and charge information
36. **LRL** (Location Relationship) - Hierarchical location relationships

---

## <¯ **TIER 5: FINANCIAL & BILLING** (10 segments) - Week 5
**Rationale**: Healthcare operations require financial tracking, insurance, and billing support.

### **Insurance & Guarantor (5 segments)**
37. **IN1** (Insurance) - Primary insurance coverage information
38. **IN2** (Insurance Additional) - Additional insurance details and authorization
39. **IN3** (Insurance Certification) - Insurance certification and pre-authorization
40. **GT1** (Guarantor) - Financial responsibility and guarantor information
41. **AUT** (Authorization Information) - Treatment authorization and approval details

### **Financial Transactions (5 segments)**
42. **FT1** (Financial Transaction) - Individual charges, payments, and adjustments
43. **BLG** (Billing) - Billing account and grouping information
44. **UB1** (UB82 Data) - UB-82 institutional billing form data
45. **UB2** (UB92 Data) - UB-92 institutional billing form data
46. **DRG** (Diagnosis Related Group) - DRG assignment and reimbursement information

---

## <¯ **TIER 6: PHARMACY & MEDICATION MANAGEMENT** (12 segments) - Week 6
**Rationale**: Medication safety and pharmacy operations are critical clinical workflows.

### **Pharmacy Orders (6 segments)**
47. **RXO** (Pharmacy Prescription Order) - Prescription orders and medication requests
48. **RXE** (Pharmacy Encoded Order) - Detailed pharmacy order with specific instructions
49. **RXC** (Pharmacy Component Order) - Medication components and compound prescriptions
50. **RXG** (Pharmacy Give) - Medication administration instructions
51. **RXR** (Pharmacy Route) - Medication administration routes and methods
52. **ODS** (Dietary Orders) - Dietary orders and nutritional instructions

### **Pharmacy Administration (6 segments)**
53. **RXA** (Pharmacy Administration) - Medication administration records
54. **RXD** (Pharmacy Dispense) - Medication dispensing information
55. **ODT** (Diet Tray Instructions) - Specific dietary delivery instructions
56. **VAR** (Variance) - Medication variance and exception reporting
57. **BTS** (Batch Trailer) - Batch processing trailer for pharmacy transactions
58. **BHS** (Batch Header) - Batch processing header for pharmacy transactions

---

## <¯ **TIER 7: CLINICAL DOCUMENTATION** (12 segments) - Week 7
**Rationale**: Clinical documentation, diagnoses, procedures, and care planning.

### **Diagnoses & Procedures (6 segments)**
59. **DG1** (Diagnosis) - Patient diagnoses and diagnostic information
60. **PR1** (Procedures) - Surgical and medical procedures performed
61. **AL1** (Allergy Information) - Patient allergies and adverse reactions
62. **IAM** (Patient Adverse Reaction) - Detailed adverse reaction information
63. **DB1** (Disability) - Patient disability status and accommodations
64. **PRB** (Problem Detail) - Problem list and clinical issues

### **Care Planning & Goals (6 segments)**
65. **GOL** (Goal Detail) - Patient care goals and objectives
66. **PTH** (Pathway) - Clinical pathways and care plans
67. **PCR** (Possible Causal Relationship) - Clinical relationship analysis
68. **TXA** (Document Notification) - Clinical document notifications
69. **CTI** (Clinical Trial Identification) - Research trial participation
70. **CSP** (Clinical Study Phase) - Clinical study phase information

---

## <¯ **TIER 8: SCHEDULING & APPOINTMENTS** (10 segments) - Week 8
**Rationale**: Healthcare scheduling and resource management for operational efficiency.

### **Appointment Management (5 segments)**
71. **SCH** (Schedule Activity Information) - Appointment and scheduling details
72. **AIG** (Appointment Information - General Resource) - General resource scheduling
73. **AIL** (Appointment Information - Location) - Location-based scheduling
74. **AIP** (Appointment Information - Personnel) - Personnel scheduling
75. **AIS** (Appointment Information - Service) - Service-based scheduling

### **Resource Management (5 segments)**
76. **RGS** (Resource Group) - Resource group definitions and management
77. **APR** (Appointment Preferences) - Scheduling preferences and constraints
78. **ARQ** (Appointment Request) - Appointment requests and booking
79. **NST** (Statistics) - Scheduling statistics and metrics
80. **NCK** (System Clock) - System time synchronization

---

## <¯ **TIER 9: SPECIALIZED CLINICAL WORKFLOWS** (16 segments) - Week 9-10
**Rationale**: Specialized healthcare operations, devices, specimens, and regulatory requirements.

### **Laboratory & Specimens (8 segments)**
81. **OM1** (General Observation Master) - Test definitions and parameters
82. **OM2** (Numeric Observation) - Numeric test result specifications
83. **OM3** (Categorical Observation) - Categorical test result specifications
84. **OM4** (Specimen Observations) - Specimen collection requirements
85. **OM5** (Observation Batteries) - Test panel and battery definitions
86. **OM6** (Calculated Observations) - Calculated and derived test results
87. **SAC** (Specimen and Container) - Specimen container and handling information
88. **SPM** (Specimen) - Detailed specimen information and processing

### **Specialized Operations (8 segments)**
89. **CDM** (Charge Description Master) - Charge master and billing codes
90. **CM0** (Clinical Study Master) - Clinical trial master information
91. **CM1** (Clinical Study Phase Master) - Study phase definitions
92. **CM2** (Clinical Study Schedule Master) - Study scheduling information
93. **CSS** (Clinical Study Data Schedule) - Study data collection schedules
94. **CSR** (Clinical Study Registration) - Study participant registration
95. **ADD** (Addendum) - Document addendum and amendments
96. **FAC** (Facility) - Healthcare facility information

---

## <¯ **TIER 10: MASTER FILE MANAGEMENT** (8 segments) - Week 11
**Rationale**: Master data management and reference data maintenance.

### **Master File Operations (4 segments)**
97. **MFI** (Master File Identification) - Master file identification and control
98. **MFE** (Master File Entry) - Master file entry operations
99. **MFA** (Master File Acknowledgement) - Master file operation acknowledgements
100. **IIM** (Inventory Item Master) - Inventory and supply master data

### **Configuration Management (4 segments)**
101. **EQL** (Embedded Query Language) - Query language specifications
102. **ERQ** (Event Replay Query) - Event replay and audit queries
103. **VTQ** (Virtual Table Query Request) - Virtual table query operations
104. **URD** (Results/Update Definition) - Update definition specifications

---

## <¯ **TIER 11: REMAINING SPECIALIZED SEGMENTS** (40+ segments) - Week 12-13
**Rationale**: Edge cases, legacy support, and specialized industry requirements.

### **Document Management (6 segments)**
105. **FHS** (File Header) - File-level message headers
106. **FTS** (File Trailer) - File-level message trailers
107. **URS** (Unsolicited Selection) - Unsolicited data selection criteria
108. **RF1** (Referral Information) - Patient referral information
109. **PEO** (Product Experience Observation) - Product experience reporting
110. **PES** (Product Experience Sender) - Product experience sender information

### **Legacy & Specialized (34+ segments)**
111-140+. **All remaining segments** including:
- Device management (EQU, EQP, INV)
- Transplant/donation (various)
- Immunization tracking (various)
- Blood banking (various)
- Quality assurance (various)
- Regulatory reporting (various)

---

## =¨ **CRITICAL DEPENDENCIES**

### **Datatype Dependencies (BLOCKING)**
- **CANNOT START** segment development without Tier 1-2 datatypes completed
- **MSH segment** requires: ST, ID, HD, TS (message infrastructure)
- **PID segment** requires: CX, XPN, XAD, XTN (patient identification)
- **Clinical segments** require: CE, TS, NM, SN (coded values and measurements)

### **Cross-Segment Dependencies**
- **ALL MESSAGES** require MSH (message header)
- **PATIENT WORKFLOWS** require PID + PV1 (patient + visit context)
- **CLINICAL ORDERS** require ORC + OBR (order control + request)
- **CLINICAL RESULTS** require OBR + OBX (request + observation)
- **FINANCIAL WORKFLOWS** require PID + IN1 + GT1 (patient + insurance)

### **Message Type Blocking**
- **ADT Messages**: MSH, EVN, PID, PV1, NK1
- **ORM Messages**: MSH, PID, PV1, ORC, OBR
- **ORU Messages**: MSH, PID, OBR, OBX
- **RDE Messages**: MSH, PID, ORC, RXO, RXE

---

## =Ë **DEVELOPMENT PROCESS**

### **For Each Segment**
1. **VERIFY DATATYPE DEPS**: Ensure required datatypes completed first
2. **RESEARCH FIRST**: `node dev-tools/research-hl7-dictionary.js segment <NAME>`
3. **CREATE TEMPLATE**: Follow _TEMPLATES/segment_template.json structure
4. **VALIDATE ALWAYS**: `node scripts/validate-against-hl7-dictionary.js segment <NAME>`
5. **FIX ALL ERRORS**: Zero tolerance for validation failures
6. **DELETE BACKUP**: Only after successful validation

### **Quality Gates**
-  **Datatype dependencies verified** before starting segment
-  **100% library validation pass** required before backup deletion
-  **Template compliance** verified against _TEMPLATES/segment_template.json
-  **Field structure** matches official HL7 v2.3 specification
-  **Required vs optional** fields properly documented

---

## <¯ **SUCCESS METRICS**

### **Completion Tracking**
- **Week 1**: 6 infrastructure segments ’ Enables basic messaging
- **Week 2**: 14 segments ’ Enables patient workflows
- **Week 3**: 26 segments ’ Enables clinical orders/results
- **Week 4**: 36 segments ’ Enables provider management
- **Week 5**: 46 segments ’ Enables financial workflows
- **Week 6**: 58 segments ’ Enables pharmacy operations
- **Week 7**: 70 segments ’ Enables clinical documentation
- **Week 8**: 80 segments ’ Enables scheduling
- **Week 9-10**: 96 segments ’ Enables specialized workflows
- **Week 11**: 104 segments ’ Enables master data management
- **Week 12-13**: 140+ segments ’ Complete HL7 v2.3 segment coverage

### **Clinical Workflow Enablement**
- **Patient Registration**: Week 2 (PID, PV1, NK1)
- **Lab Orders**: Week 3 (ORC, OBR, OBX)
- **Pharmacy Orders**: Week 6 (RXO, RXE, RXA)
- **Financial Billing**: Week 5 (IN1, GT1, FT1)
- **Clinical Documentation**: Week 7 (DG1, PR1, AL1)

**Remember**: Segments define the structure of healthcare data. Perfect accuracy here enables meaningful message construction.