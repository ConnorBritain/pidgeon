# HL7 v2.3 Message Development Priority Order

**Purpose**: Priority-ordered development plan for 240+ HL7 v2.3 message types based on healthcare workflow criticality and clinical frequency.

**Strategy**: Build universal infrastructure first, then core clinical workflows, followed by specialized healthcare operations.

**Library Coverage**:  100% supported by hl7-dictionary - MANDATORY research and validation required for each message.

**Dependencies**: Requires foundation segments (MSH, PID, PV1, ORC, OBR, OBX) and core datatypes completed first.

---

## <¯ **TIER 1: UNIVERSAL MESSAGE INFRASTRUCTURE** (2 messages) - Week 1
**Rationale**: Every HL7 system needs acknowledgment and error handling. Foundation for all other messages.

### **Universal Response Messages (2 messages)**
1. **ACK** (General Acknowledgment) - Universal acknowledgment for all message types
2. **ACK** (Application Accept/Reject) - Error handling and application-level responses

---

## <¯ **TIER 2: PATIENT ADMINISTRATION FOUNDATION** (8 messages) - Week 2
**Rationale**: Patient management is prerequisite for all clinical workflows. Core healthcare operations.

### **Core Patient Workflow (4 messages)**
3. **ADT_A01** (Admit/Visit Notification) - Patient admission and registration
4. **ADT_A03** (Discharge/End Visit) - Patient discharge and visit closure
5. **ADT_A04** (Register Patient) - Patient registration and demographic updates
6. **ADT_A08** (Update Patient Information) - Patient demographic and insurance updates

### **Essential Patient Operations (4 messages)**
7. **ADT_A02** (Transfer Patient) - Patient location and service transfers
8. **ADT_A11** (Cancel Admit/Visit) - Admission cancellation and error correction
9. **ADT_A12** (Cancel Transfer) - Transfer cancellation and correction
10. **ADT_A13** (Cancel Discharge) - Discharge cancellation and correction

---

## <¯ **TIER 3: CLINICAL ORDERS & RESULTS** (6 messages) - Week 3
**Rationale**: Core clinical workflow - orders drive healthcare processes, results provide outcomes.

### **Order Management (3 messages)**
11. **ORM_O01** (Order Message) - Lab orders, radiology orders, procedure requests
12. **ORR_O02** (Order Response) - Order acknowledgment and status updates
13. **ORD_O02** (Dietary Order Acknowledgment) - Food service and nutrition orders

### **Results Reporting (3 messages)**
14. **ORU_R01** (Unsolicited Transmission of Observation) - Lab results, vital signs, clinical observations
15. **ORF_R02** (Response to Query for Observation) - Query-based result retrieval
16. **ORF_R04** (Response to Query; Transmission of Requested Observation) - Specific observation queries

---

## <¯ **TIER 4: PHARMACY & MEDICATION MANAGEMENT** (8 messages) - Week 4
**Rationale**: Medication safety and pharmacy operations are critical clinical workflows.

### **Pharmacy Orders (4 messages)**
17. **RDE_O01** (Pharmacy/Treatment Encoded Order) - Prescription orders with detailed encoding
18. **RDO_O01** (Pharmacy/Treatment Order) - Basic pharmacy order messages
19. **RGV_O01** (Pharmacy/Treatment Give) - Medication administration orders
20. **OMS_O01** (Stock Requisition Order) - Pharmacy inventory and supply orders

### **Pharmacy Administration (4 messages)**
21. **RAS_O01** (Pharmacy/Treatment Administration Message) - Medication administration records
22. **RAS_O02** (Pharmacy/Treatment Administration Message) - Enhanced administration tracking
23. **RRA_O02** (Pharmacy/Treatment Administration Acknowledgment) - Administration confirmations
24. **RRD_O02** (Pharmacy/Treatment Dispense Acknowledgment) - Dispensing confirmations

---

## <¯ **TIER 5: QUERY & INFORMATION MANAGEMENT** (8 messages) - Week 5
**Rationale**: Healthcare systems need robust query capabilities for clinical decision support.

### **Query Infrastructure (4 messages)**
25. **QRY_Q01** (Query Sent for Immediate Response) - General query mechanism
26. **QRY_A19** (Patient Query) - Patient demographic and location queries
27. **DSR_Q01** (Display Response Message) - Formatted display responses
28. **DSR_Q03** (Deferred Response to Query) - Asynchronous query processing

### **Specialized Queries (4 messages)**
29. **QRY_R02** (Query for Results of Observation) - Clinical results queries
30. **OSQ_Q06** (Query for Order Status) - Order tracking and status queries
31. **OSR_Q06** (Query Response for Order Status) - Order status responses
32. **EQQ_Q01** (Embedded Query Language Query) - Complex structured queries

---

## <¯ **TIER 6: EXTENDED PATIENT ADMINISTRATION** (16 messages) - Week 6
**Rationale**: Complete patient administration workflows including specialized scenarios.

### **Patient Movement & Tracking (8 messages)**
33. **ADT_A05** (Pre-admit Patient) - Pre-registration and scheduling
34. **ADT_A06** (Change Outpatient to Inpatient) - Patient class changes
35. **ADT_A07** (Change Inpatient to Outpatient) - Status transitions
36. **ADT_A09** (Patient Departing - Tracking) - Leave of absence tracking
37. **ADT_A10** (Patient Arriving - Tracking) - Return from leave tracking
38. **ADT_A21** (Patient Goes on Leave of Absence) - Temporary departures
39. **ADT_A22** (Patient Returns from Leave of Absence) - Leave returns
40. **ADT_A20** (Bed Status Update) - Bed management and availability

### **Patient Record Management (8 messages)**
41. **ADT_A18** (Merge Patient Information) - Patient record consolidation
42. **ADT_A24** (Link Patient Information) - Patient record linking
43. **ADT_A37** (Unlink Patient Information) - Record unlinking operations
44. **ADT_A23** (Delete Patient Record) - Record deletion and cleanup
45. **ADT_A28** (Add Person Information) - Person registration
46. **ADT_A29** (Delete Person Information) - Person record removal
47. **ADT_A30** (Merge Person Information) - Person record merging
48. **ADT_A31** (Update Person Information) - Person demographic updates

---

## <¯ **TIER 7: FINANCIAL & BILLING MESSAGES** (8 messages) - Week 7
**Rationale**: Healthcare operations require financial tracking, insurance, and billing support.

### **Account Management (4 messages)**
49. **BAR_P01** (Add Patient Accounts) - Patient account creation
50. **BAR_P02** (Purge Patient Accounts) - Account cleanup and archival
51. **BAR_P05** (Update Account) - Account information updates
52. **BAR_P06** (End Account) - Account closure and finalization

### **Financial Transactions (4 messages)**
53. **DFT_P03** (Post Detail Financial Transaction) - Individual charges and payments
54. **PIN_I07** (Unsolicited Insurance Information) - Insurance coverage updates
55. **RPA_I08** (Return Patient Authorization) - Authorization responses
56. **RQA_I08** (Request for Treatment Authorization) - Authorization requests

---

## <¯ **TIER 8: SCHEDULING & APPOINTMENTS** (12 messages) - Week 8
**Rationale**: Healthcare scheduling and resource management for operational efficiency.

### **Appointment Notifications (6 messages)**
57. **SIU_S12** (Notification of New Appointment Booking) - New appointment scheduling
58. **SIU_S13** (Notification of Appointment Rescheduling) - Schedule changes
59. **SIU_S14** (Notification of Appointment Modification) - Appointment updates
60. **SIU_S15** (Notification of Appointment Cancellation) - Cancellations
61. **SIU_S16** (Notification of Appointment Discontinuation) - Service discontinuation
62. **SIU_S17** (Notification of Appointment Deletion) - Appointment removal

### **Schedule Management (6 messages)**
63. **SRM_S01** (Request New Appointment Booking) - Appointment requests
64. **SRM_S02** (Request Appointment Rescheduling) - Reschedule requests
65. **SRM_S03** (Request Appointment Modification) - Modification requests
66. **SRM_S04** (Request Appointment Cancellation) - Cancellation requests
67. **SRR_S01** (Scheduled Request Response) - Appointment request responses
68. **SQM_S25** (Schedule Query Message and Response) - Schedule information queries

---

## <¯ **TIER 9: CLINICAL DOCUMENTATION** (12 messages) - Week 9
**Rationale**: Clinical documentation, care planning, and quality management.

### **Document Management (6 messages)**
69. **MDM_T01** (Original Document Notification) - New clinical documents
70. **MDM_T02** (Original Document Notification and Content) - Documents with content
71. **MDM_T03** (Document Status Change Notification) - Document status updates
72. **MDM_T05** (Document Addendum Notification) - Document amendments
73. **MDM_T07** (Document Edit Notification) - Document revisions
74. **QRY_T12** (Document Query) - Document retrieval queries

### **Care Planning (6 messages)**
75. **PPR_PC1** (Problem Add) - Problem list management
76. **PPR_PC2** (Problem Update) - Problem updates and modifications
77. **PGL_PC6** (Goal Add) - Care goal establishment
78. **PGL_PC7** (Goal Update) - Goal progress and modifications
79. **PPP_PCB** (Pathway Problem-Oriented Add) - Clinical pathway management
80. **PTR_PCF** (Pathway Problem-Oriented Query Response) - Pathway queries

---

## <¯ **TIER 10: MASTER FILE MANAGEMENT** (8 messages) - Week 10
**Rationale**: Master data management and reference data maintenance.

### **Master File Operations (4 messages)**
81. **MFN_M01** (Master File Not Otherwise Specified) - General master file updates
82. **MFN_M02** (Master File - Staff Practitioner) - Provider master file maintenance
83. **MFN_M04** (Master Files Charge Description) - Charge master updates
84. **MFN_M05** (Patient Location Master File) - Location master maintenance

### **Master File Responses (4 messages)**
85. **MFK_M01** (Master File Application Acknowledgment) - General file acknowledgments
86. **MFK_M02** (Master File Application Acknowledgment) - Staff file acknowledgments
87. **MFK_M04** (Master File Application Acknowledgment) - Charge master acknowledgments
88. **MFK_M05** (Master File Application Acknowledgment) - Location master acknowledgments

---

## <¯ **TIER 11: SPECIALIZED CLINICAL WORKFLOWS** (20 messages) - Week 11-12
**Rationale**: Specialized healthcare operations, clinical research, and regulatory requirements.

### **Clinical Research (8 messages)**
89. **CRM_C01** (Register Patient on Clinical Trial) - Trial enrollment
90. **CRM_C02** (Cancel Patient Registration on Clinical Trial) - Enrollment cancellation
91. **CRM_C03** (Correct/Update Registration Information) - Registration updates
92. **CRM_C04** (Patient Has Gone Off Clinical Trial) - Trial discontinuation
93. **CSU_C09** (Automated Time Intervals for Reporting) - Scheduled research reports
94. **CSU_C10** (Patient Completes Clinical Trial) - Trial completion
95. **CSU_C11** (Patient Completes Phase of Clinical Trial) - Phase completion
96. **CSU_C12** (Update/Correction of Patient Order/Result Information) - Research data corrections

### **Product Experience (8 messages)**
97. **PEX_P07** (Unsolicited Initial Individual Product Experience Report) - Adverse event reporting
98. **PEX_P08** (Unsolicited Update Individual Product Experience Report) - Event updates
99. **SUR_P09** (Summary Product Experience Report) - Aggregate event reports
100. **REF_I12** (Patient Referral) - Inter-provider referrals
101. **REF_I13** (Modify Patient Referral) - Referral modifications
102. **REF_I14** (Cancel Patient Referral) - Referral cancellations
103. **REF_I15** (Request Patient Referral Status) - Referral status queries
104. **RRI_I12** (Return Referral Information) - Referral responses

### **Information Exchange (4 messages)**
105. **RPI_I01** (Return Patient Information) - Patient data responses
106. **RQI_I01** (Request for Insurance Information) - Insurance data requests
107. **RQC_I05** (Request for Patient Clinical Information) - Clinical data requests
108. **RCI_I05** (Return Clinical Information) - Clinical data responses

---

## <¯ **TIER 12: REMAINING SPECIALIZED MESSAGES** (130+ messages) - Week 13-16
**Rationale**: Complete HL7 v2.3 message coverage for specialized workflows and edge cases.

### **Remaining Categories Include:**
- **Vaccination Management** (VXQ, VXR, VXU, VXX) - Immunization tracking
- **Blood Banking** (various) - Transfusion and blood product management
- **Laboratory Information** (OM1-OM6 related) - Test master data management
- **Dietary Management** (OMD, OMN) - Nutrition and food service
- **Equipment Management** (various) - Medical device integration
- **Security & Access** (various) - Authentication and authorization
- **International Standards** (various) - Global healthcare interoperability
- **Legacy Compatibility** (various) - Backward compatibility support

**Note**: These messages provide complete HL7 v2.3 coverage but are lower priority for MVP functionality.

---

## =¨ **CRITICAL DEPENDENCIES**

### **Segment Dependencies (BLOCKING)**
- **CANNOT START** message development without core segments completed
- **ALL MESSAGES** require MSH (message header) segment
- **PATIENT MESSAGES** require PID (patient identification) segment
- **CLINICAL MESSAGES** require ORC, OBR, OBX (order control, request, observation)
- **VISIT MESSAGES** require PV1 (patient visit) segment

### **Message Type Dependencies**
- **ADT Messages** (Patient Admin): MSH, EVN, PID, PV1, NK1
- **ORM Messages** (Orders): MSH, PID, PV1, ORC, OBR
- **ORU Messages** (Results): MSH, PID, OBR, OBX
- **RDE Messages** (Pharmacy): MSH, PID, ORC, RXO, RXE
- **Query Messages**: MSH, QRD, QRF, QAK
- **Financial Messages**: MSH, PID, IN1, GT1, FT1

### **Cross-Message Dependencies**
- **Order-Result Pairs**: ORM must exist before ORU for same order
- **Query-Response Pairs**: Query messages require corresponding response messages
- **Patient Context**: Most clinical messages require prior ADT patient registration
- **Acknowledgments**: Every message type needs corresponding ACK capability

---

## =Ë **DEVELOPMENT PROCESS**

### **For Each Message**
1. **VERIFY SEGMENT DEPS**: Ensure required segments completed first
2. **RESEARCH FIRST**: `node dev-tools/research-hl7-dictionary.js message <TYPE>`
3. **CREATE TEMPLATE**: Follow _TEMPLATES/message_template.json structure
4. **VALIDATE ALWAYS**: `node scripts/validate-against-hl7-dictionary.js message <TYPE>`
5. **FIX ALL ERRORS**: Zero tolerance for validation failures
6. **DELETE BACKUP**: Only after successful validation

### **Quality Gates**
-  **Segment dependencies verified** before starting message
-  **100% library validation pass** required before backup deletion
-  **Template compliance** verified against _TEMPLATES/message_template.json
-  **Message structure** matches official HL7 v2.3 specification
-  **Trigger event alignment** with corresponding trigger event definitions

---

## <¯ **SUCCESS METRICS**

### **Completion Tracking**
- **Week 1**: 2 infrastructure messages ’ Enables basic messaging capability
- **Week 2**: 10 messages ’ Enables patient administration workflows
- **Week 3**: 16 messages ’ Enables clinical orders and results
- **Week 4**: 24 messages ’ Enables pharmacy operations
- **Week 5**: 32 messages ’ Enables query and information management
- **Week 6**: 48 messages ’ Enables complete patient administration
- **Week 7**: 56 messages ’ Enables financial and billing operations
- **Week 8**: 68 messages ’ Enables scheduling and appointments
- **Week 9**: 80 messages ’ Enables clinical documentation
- **Week 10**: 88 messages ’ Enables master file management
- **Week 11-12**: 108 messages ’ Enables specialized clinical workflows
- **Week 13-16**: 240+ messages ’ Complete HL7 v2.3 message coverage

### **Healthcare Workflow Enablement**
- **Patient Registration**: Week 2 (ADT_A01, ADT_A04, ADT_A08)
- **Clinical Orders**: Week 3 (ORM_O01, ORR_O02)
- **Lab Results**: Week 3 (ORU_R01, ORF_R02)
- **Pharmacy Orders**: Week 4 (RDE_O01, RAS_O01)
- **Patient Queries**: Week 5 (QRY_A19, DSR_Q01)
- **Appointment Scheduling**: Week 8 (SIU_S12, SRM_S01)

### **Integration Readiness**
- **EHR Integration**: Week 6 (complete patient administration)
- **Laboratory Integration**: Week 6 (orders + results + queries)
- **Pharmacy Integration**: Week 7 (orders + administration + billing)
- **Clinical Decision Support**: Week 9 (documentation + care planning)
- **Population Health**: Week 12 (research + reporting + registries)

**Remember**: Messages define the clinical workflows that drive healthcare operations. Perfect accuracy here enables meaningful healthcare interoperability.