# HL7 v2.3 Table Development Priority Order

**Purpose**: Priority-ordered development plan for 396 HL7 v2.3 code tables based on cross-reference frequency and clinical workflow criticality.

**Strategy**: Build universal code tables first, then patient-specific codes, followed by specialized clinical and administrative codes.

**Library Coverage**:  100% supported by hl7-dictionary - MANDATORY research and validation required for each table.

**Dependencies**: Requires core datatypes (ST, ID, IS, CE) completed first for coded element support.

---

## <¯ **TIER 1: UNIVERSAL FOUNDATION TABLES** (8 tables) - Week 1
**Rationale**: Referenced across all healthcare workflows and message types. Block everything if missing.

### **Core Demographics (4 tables)**
1. **Table 0001** (Administrative Sex) - M/F/O/U - Universal patient attribute
2. **Table 0002** (Marital Status) - M/S/D/W - Patient demographics and insurance
3. **Table 0005** (Race) - Patient demographics and clinical research
4. **Table 0006** (Religion) - Patient preferences and care considerations

### **Message Infrastructure (4 tables)**
5. **Table 0076** (Message Type) - ADT^A01, ORM^O01 - Every HL7 message header
6. **Table 0103** (Processing ID) - P/T/D - Production/Test/Debug message routing
7. **Table 0104** (Version ID) - 2.3/2.4/2.5 - HL7 version compatibility
8. **Table 0008** (Acknowledgment Code) - AA/AE/AR - Universal message responses

---

## <¯ **TIER 2: PATIENT WORKFLOW TABLES** (12 tables) - Week 2
**Rationale**: Core patient management and healthcare operations. Essential for clinical workflows.

### **Patient Status & Movement (6 tables)**
9. **Table 0004** (Patient Class) - I/O/E/N - Inpatient/Outpatient/Emergency/Newborn
10. **Table 0007** (Admission Type) - E/R/L/N - Emergency/Routine/Labor/Newborn
11. **Table 0023** (Admit Source) - Physician referral, emergency room, transfer
12. **Table 0112** (Discharge Disposition) - Home, transfer, expired, AMA
13. **Table 0116** (Bed Status) - O/C/U/K - Occupied/Closed/Unoccupied/Contaminated
14. **Table 0069** (Hospital Service) - MED/SUR/OBS/ICU - Medical service departments

### **Clinical Status & Priority (6 tables)**
15. **Table 0027** (Priority) - S/A/R/P - Stat/ASAP/Routine/Preoperative
16. **Table 0052** (Diagnosis Type) - A/W/F - Admitting/Working/Final diagnosis
17. **Table 0078** (Abnormal Flags) - H/L/A/AA - High/Low/Abnormal/Critical
18. **Table 0085** (Observation Result Status) - F/P/R/C - Final/Preliminary/Results/Corrected
19. **Table 0038** (Order Status) - A/CA/CM/DC/ER/HD/IP/RP/SC - Active/Canceled/Complete
20. **Table 0119** (Order Control Codes) - NW/CA/OC/CR/SC - New/Cancel/Change/Replace

---

## <¯ **TIER 3: CLINICAL DOCUMENTATION TABLES** (16 tables) - Week 3
**Rationale**: Essential for clinical documentation, diagnoses, procedures, and results.

### **Clinical Codes & Classifications (8 tables)**
21. **Table 0074** (Diagnostic Service Section) - LAB/RAD/PT/OTH - Lab/Radiology/Pathology
22. **Table 0080** (Nature of Abnormal Testing) - A/N/S - Age/Normal/Sex related
23. **Table 0123** (Result Status) - O/I/S/A/P/F/R/C/X - Order/In Lab/Schedule/Aborted
24. **Table 0174** (Nature of Service/Test) - A/D/L/M/O/P/R/S/T - Atomic/Diagnostic/Lab
25. **Table 0228** (Diagnosis Classification) - C/D/I/M/O/R/T - Consultation/Diagnosis
26. **Table 0229** (DRG Payor) - Medicare/Medicaid/Commercial insurance categories
27. **Table 0001** (Administrative Sex) - Referenced in clinical contexts for normal ranges
28. **Table 0230** (Procedure Functional Type) - A/D/I/T - Anesthesia/Diagnostic/Invasive

### **Provider & Location (8 tables)**
29. **Table 0182** (Staff Type) - Doctor/Nurse/Technician/Clerk - Provider classifications
30. **Table 0286** (Provider Role) - Attending/Consulting/Referring physician roles
31. **Table 0200** (Name Type) - A/B/C/L/M/P/S - Alias/Birth/Chosen/Legal/Maiden
32. **Table 0201** (Telecommunication Use Code) - H/W/M - Home/Work/Mobile contact
33. **Table 0202** (Telecommunication Equipment Type) - PH/FX/Internet/Pager equipment
34. **Table 0203** (Identifier Type) - SS/DL/MR/AN - SSN/License/Medical Record/Account
35. **Table 0204** (Organizational Name Type) - A/D/L/SL - Alias/Display/Legal/Stock
36. **Table 0261** (Location Equipment) - OXY/SUC/VIL/VEN - Oxygen/Suction/Ventilator

---

## <¯ **TIER 4: FINANCIAL & INSURANCE TABLES** (12 tables) - Week 4
**Rationale**: Healthcare billing, insurance, and financial operations.

### **Insurance & Coverage (6 tables)**
37. **Table 0135** (Assignment of Benefits) - Y/N/M - Yes/No/Modified assignment
38. **Table 0137** (Mail Claim Party) - E/I/P - Employer/Insurance/Patient mailing
39. **Table 0173** (Coordination of Benefits) - CO/IN/MS - Coordination of benefits
40. **Table 0309** (Coverage Type) - B/C/H/P - Basic/Catastrophic/Health/Pharmacy
41. **Table 0344** (Patient Relationship to Insured) - 01/02/03/18 - Self/Spouse/Child
42. **Table 0401** (Government Reimbursement Program) - Medicare/Medicaid/CHAMPUS

### **Financial Operations (6 tables)**
43. **Table 0122** (Charge Type) - CH/CO/CR/DP/GR/NC/PC/RS - Charge/Credit/Debit
44. **Table 0146** (Amount Type) - DF/LM/PC/PY/UL - Differential/Limit/Percentage
45. **Table 0148** (Money or Percentage Indicator) - AT/PC - Absolute amount/Percentage
46. **Table 0193** (Amount Class) - AT/LM/PC/UL - Absolute/Limit/Percentage/Unlimited
47. **Table 0205** (Price Type) - AP/DC/IC/PF/TP/UL - Administrative/Discount/Total
48. **Table 0557** (Payee Type) - ORG/PERS/PPER - Organization/Person/Pay to Person

---

## <¯ **TIER 5: PHARMACY & MEDICATION TABLES** (16 tables) - Week 5
**Rationale**: Medication safety, pharmacy operations, and prescription management.

### **Medication Administration (8 tables)**
49. **Table 0162** (Route of Administration) - PO/IV/IM/SQ/TOP - Oral/IV/Injection/Topical
50. **Table 0163** (Body Site) - BE/BU/LA/RA/LE/RE - Buttock/Arm/Leg administration sites
51. **Table 0164** (Administration Device) - AP/BP/BT/BU/CP/CU/SF/VN - Pump/Tube/Needle
52. **Table 0165** (Administration Method) - CHEW/DISC/DUST/SHAK - Methods of medication delivery
53. **Table 0166** (RX Component Type) - A/B - Additive/Base medication components
54. **Table 0167** (Substitution Status) - 0/1/2/3/4/5/7/8/G/N/T - Substitution allowed/not
55. **Table 0321** (Dispense Method) - A/F/T/U - Automatic/Floor/Traditional/Unit dose
56. **Table 0322** (Completion Status) - CP/PA/RE - Complete/Partially administered/Refused

### **Pharmacy Operations (8 tables)**
57. **Table 0168** (Processing Priority) - S/A/R/P/C/T - Stat/ASAP/Routine/Preop/Callback
58. **Table 0169** (Reporting Priority) - C/L/R/S - Call/Letter/Report/Stat reporting
59. **Table 0480** (Pharmacy Order Types) - I/O/S - Inpatient/Outpatient/Specialty orders
60. **Table 0482** (Order Type) - I/O - Inpatient/Outpatient pharmacy orders
61. **Table 0483** (Authorization Mode) - EL/EM/IP/PA/PH/RE/TE - Electronic/Phone/Paper
62. **Table 0484** (Dispense Type) - B/F/P/S/T/U - Bulk/Floor/Partial/Supply/Trial
63. **Table 0485** (Extended Priority Codes) - S/A/R/P/C/T/Z - Enhanced priority codes
64. **Table 0477** (Controlled Substance Schedule) - I/II/III/IV/V - DEA schedules

---

## <¯ **TIER 6: LABORATORY & DIAGNOSTICS TABLES** (20 tables) - Week 6
**Rationale**: Laboratory operations, specimen handling, and diagnostic workflows.

### **Specimen Management (10 tables)**
65. **Table 0065** (Specimen Action Code) - A/G/L/O/P/R/S - Add/Generated/Lab/Patient/Received
66. **Table 0070** (Specimen Source Codes) - BLD/CSF/FLU/GAS/HAR/LIQ/PRT/SER/UR - Blood/CSF/Fluid
67. **Table 0371** (Additive/Preservative) - ACDA/ACDB/ACET/AMIES - Specimen additives
68. **Table 0487** (Specimen Type) - ABS/AMN/ASP/BBL/BDY/BIFL/BLD - Specimen types
69. **Table 0488** (Specimen Collection Method) - ANP/BAP/BCAE/BCAN/BCPD - Collection methods
70. **Table 0490** (Specimen Reject Reason) - EX/CL/CO/DA/HI/HO/I/IP/NO/QS/RA/RC - Expired/Clotted
71. **Table 0491** (Specimen Quality) - E/F/G/I/P/Q - Excellent/Fair/Good/Inadequate/Poor
72. **Table 0492** (Specimen Appropriateness) - A/I/P/R - Appropriate/Inappropriate/Preferred
73. **Table 0493** (Specimen Condition) - AUT/CFU/CLO/CON/COOL/FROZ/HEM/LIVE/ROOM - Condition
74. **Table 0170** (Derived Specimen) - P/C/A - Parent/Child/Aliquot specimen relationships

### **Laboratory Results (10 tables)**
75. **Table 0125** (Value Type) - AD/CE/CF/CK/CN/CP/CQ/CX/DT/ED/FT/MO/NM/PN/RP/SN/ST/TM/TN/TS/TX - Data types
76. **Table 0254** (Kind of Quantity) - ABS/ACNC/ACT/APER/ARBA/AREA/ASPECT - Quantity types
77. **Table 0255** (Duration Categories) - **/30M/1H/2H/2.5H/3H/4H/5H/6H/12H/24H - Time periods
78. **Table 0256** (Time Delay Post Challenge) - 10D/180D/1H/1L/1M/1W/2.5H/7D/BS - Delay periods
79. **Table 0389** (Analyte Repeat Status) - D/F/O/R - Diluted/Reflex/Original/Repeated
80. **Table 0392** (Match Reason) - DB/MC/MI/NA/NM/SS/UN - Database/Master/MPI match
81. **Table 0440** (Data Types) - AD/CE/CF/CK/CM/CN/CP/CQ/CX/DT/ED/FT/ID/IS/MO/NM - Valid datatypes
82. **Table 0441** (Immunization Registry Status) - A/I/L/P/U - Active/Inactive/Lost/Purged
83. **Table 0507** (Observation Result Handling) - A/BCC/CC/F/N/NFR - Alert/Blind copy/Copy
84. **Table 0510** (Blood Product Dispense Status) - CR/DS/PT/PU/RA/RL/RQ/RS/WA - Created/Dispensed

---

## <¯ **TIER 7: SCHEDULING & APPOINTMENTS TABLES** (12 tables) - Week 7
**Rationale**: Healthcare scheduling, resource management, and appointment workflows.

### **Appointment Management (6 tables)**
85. **Table 0276** (Appointment Reason Codes) - CHECKUP/EMERGENCY/FOLLOWUP/ROUTINE/WALKIN
86. **Table 0277** (Appointment Type Codes) - Complete/Incomplete/Normal/Tentative/Emergency
87. **Table 0278** (Filler Status Codes) - Blocked/Booked/Canceled/Complete/Deleted/Discontinued
88. **Table 0279** (Allow Substitution Codes) - Confirm/Notify/No notification required
89. **Table 0324** (Location Characteristic ID) - GEN/IMP/INF/LIC/OVR/PH/SHA/STF/TEA - Characteristics
90. **Table 0325** (Location Relationship ID) - ALI/DTY/MGR/OWN/PAR/REL/SUP/TMP - Relationships

### **Resource Management (6 tables)**
91. **Table 0267** (Days of the Week) - MON/TUE/WED/THU/FRI/SAT/SUN - Scheduling days
92. **Table 0268** (Override) - A/R/X - Allowed/Restricted/Not allowed overrides
93. **Table 0269** (Charge On Indicator) - 0/1 - Charge/No charge for no-shows
94. **Table 0326** (Visit Indicator) - A/V - Account/Visit level scheduling
95. **Table 0329** (Quantity Method) - A/C/E/R/T - Actual/Count/Estimated/Remaining
96. **Table 0331** (Facility Type) - H/O/C/N - Hospital/Outpatient/Clinic/Nursing home

---

## <¯ **TIER 8: ADMINISTRATIVE & WORKFLOW TABLES** (16 tables) - Week 8
**Rationale**: Administrative operations, workflow management, and system operations.

### **System Operations (8 tables)**
97. **Table 0155** (Accept/Application Acknowledgment) - AL/NE/ER/SU - Always/Never/Error/Success
98. **Table 0207** (Processing Mode) - A/I/Not present/R/T - Archive/Initialize/Real-time/Training
99. **Table 0208** (Query Response Status) - AE/AR/NF/OK - Application error/Reject/Not found/OK
100. **Table 0356** (Alternate Character Set Handling) - ISO-8859-1/ISO-8859-2/ISO-8859-5/JIS - Character sets
101. **Table 0357** (Message Error Condition Codes) - 0/100/101/102/103/200/201/202/203/204/205/206/207 - Error codes
102. **Table 0396** (Coding System) - 99zzz/ACR/ASTM-E1238/C4/C5/CAS/CD2/CDCA/CLIP/CLP - Coding systems
103. **Table 0211** (Alternate Character Sets) - ASCII/8859/1/2/3/4/5/6/7/8/9/10/15/ISO-IR-14/ISO-IR-87/ISO-IR-159
104. **Table 0354** (Message Structure) - ACK/ADR_A19/ADT_A01/ADT_A02/ADT_A03/ADT_A04 - Message structures

### **Data Management (8 tables)**
105. **Table 0136** (Yes/No Indicator) - Y/N - Universal yes/no responses
106. **Table 0183** (Active/Inactive) - A/I - Status indicators
107. **Table 0206** (Segment Action Code) - A/D/I/U - Add/Delete/Inactive/Update segment actions
108. **Table 0209** (Relational Operator) - CT/EQ/GE/GT/LE/LT/NE - Contains/Equal/Greater/Less than
109. **Table 0210** (Relational Conjunction) - AND/OR - Query logic operators
110. **Table 0301** (Universal ID Type) - CLIA/CLIP/DNS/GUID/HCD/HL7 - Identifier type codes
111. **Table 0355** (Primary Key Value Type) - CE/PL - Coded element/Person location key types
112. **Table 0532** (Expanded Yes/No Indicator) - Y/N/UNK/NASK/NAV - Yes/No/Unknown/Not asked/Not available

---

## <¯ **TIER 9: SPECIALTY CLINICAL TABLES** (24 tables) - Week 9-10
**Rationale**: Specialized clinical workflows, regulatory requirements, and advanced operations.

### **Clinical Specialties (12 tables)**
113. **Table 0127** (Allergen Type) - DA/EA/FA/LA/MA/MC/PA/VA - Drug/Environmental/Food/Animal allergens
114. **Table 0128** (Allergy Severity) - MI/MO/SV/U - Mild/Moderate/Severe/Unknown severity
115. **Table 0189** (Ethnic Group) - H/N/U - Hispanic/Non-Hispanic/Unknown ethnicity
116. **Table 0220** (Living Arrangement) - A/F/I/R/S/U - Alone/Family/Institution/Relative/Spouse
117. **Table 0223** (Living Dependency) - C/M/O/S/U/WO - Caregiver/Medical/Other/Spouse/Unknown
118. **Table 0292** (Vaccines Administered) - CVX codes - CDC vaccine administered codes
119. **Table 0315** (Living Will Code) - F/I/N/U/Y - Furnished/Indicated/No/Unknown/Yes
120. **Table 0316** (Organ Donor Code) - F/I/N/P/R/U/Y - Furnished/Indicated/No/Part/Refused/Unknown/Yes
121. **Table 0437** (Alert Device Code) - B/C/L/O/V/W - Bracelet/Card/Laser/Other/Voice/Wallet
122. **Table 0438** (Allergy Clinical Status) - A/C/I/P/U - Active/Confirmed/Inactive/Pending/Unconfirmed
123. **Table 0443** (Provider Role) - AD/AP/AT/CLP/CP/CR/PP/RP/RT - Admitting/Attending/Consulting physician
124. **Table 0444** (Name Assembly Order) - F/G - Family first/Given first name assembly

### **Quality & Safety (12 tables)**
125. **Table 0514** (Transfusion Adverse Reaction) - ABOINC/ACCUT/ALLERGIC/ANAPHYLAC/BACTINF - Reactions
126. **Table 0516** (Error Severity) - E/F/I/W - Error/Fatal/Information/Warning severity levels
127. **Table 0517** (Inform Person Code) - HD/IG/PAT/USR - Help desk/Information/Patient/User notification
128. **Table 0518** (Override Type) - EXTN/INLV/OERR/PRL/REAS/RPLC/UNAN - Extension/Interval/Error overrides
129. **Table 0520** (Message Waiting Priority) - HIGH/LOW/MEDIUM - Message queue priorities
130. **Table 0536** (Certificate Status) - E/I/P/R/V - Expired/Inactive/Provisional/Revoked/Valid
131. **Table 0540** (Inactive Reason Code) - L/R/S/T - License expired/Retired/Suspended/Terminated
132. **Table 0569** (Adjustment Action) - WA/OA/PR - Write off/Adjustment/Partial payment
133. **Table 0725** (Mood Codes) - APT/ARQ/DEF/EVN/GOL/INT/PRMS/PRP/RQO - Appointment/Request/Definition moods
134. **Table 0776** (Item Status) - A/I/P - Active/Inactive/Pending item status
135. **Table 0778** (Item Type) - EQP/IMP/MED/SUP - Equipment/Implant/Medication/Supply types
136. **Table 0881** (Role Executing Physician) - B/S - Both/Secondary role execution

---

## <¯ **TIER 10: REMAINING SPECIALIZED TABLES** (280+ tables) - Week 11-16
**Rationale**: Complete HL7 v2.3 table coverage for specialized workflows and edge cases.

### **Remaining Categories Include:**
- **Device & Equipment Management** (40+ tables)
- **Blood Banking & Transfusion** (30+ tables)
- **Clinical Research & Trials** (25+ tables)
- **Public Health & Reporting** (35+ tables)
- **International & Regulatory** (20+ tables)
- **Legacy & Compatibility** (50+ tables)
- **Vendor Extensions** (80+ tables)

**Note**: These tables provide complete HL7 v2.3 coverage but are lower priority for MVP functionality.

---

## =¨ **CRITICAL DEPENDENCIES**

### **Datatype Dependencies (BLOCKING)**
- **CANNOT START** table development without core datatypes (ST, ID, IS, CE)
- **Coded elements** require table references for value validation
- **User-defined tables** use IS datatype, HL7 tables use ID datatype

### **Cross-Table Dependencies**
- **Message routing** requires Tables 0076, 0103, 0104 (message type, processing, version)
- **Patient workflows** require Tables 0001, 0002, 0004, 0007 (sex, marital, class, admission)
- **Clinical orders** require Tables 0027, 0038, 0119 (priority, status, control)
- **Results reporting** require Tables 0078, 0085, 0125 (abnormal flags, status, value type)

### **Segment Integration**
- **Tables populate CE fields** in segments (coded element values)
- **Table validation** occurs during segment field validation
- **Cross-reference integrity** between segments and their table references

---

## =Ë **DEVELOPMENT PROCESS**

### **For Each Table**
1. **VERIFY DATATYPE DEPS**: Ensure required datatypes completed first
2. **RESEARCH FIRST**: `node dev-tools/research-hl7-dictionary.js table <NUM>`
3. **CREATE TEMPLATE**: Follow _TEMPLATES/table_template.json structure
4. **VALIDATE ALWAYS**: `node scripts/validate-against-hl7-dictionary.js table <NUM>`
5. **FIX ALL ERRORS**: Zero tolerance for validation failures
6. **DELETE BACKUP**: Only after successful validation

### **Quality Gates**
-  **Code completeness** verified against library (all valid codes included)
-  **Description accuracy** matches official HL7 v2.3 specification
-  **Template compliance** verified against _TEMPLATES/table_template.json
-  **Cross-references** documented for tables used by segments

---

## <¯ **SUCCESS METRICS**

### **Completion Tracking**
- **Week 1**: 8 foundation tables ’ Enables basic messaging and demographics
- **Week 2**: 20 tables ’ Enables patient workflows and status tracking
- **Week 3**: 36 tables ’ Enables clinical documentation and provider management
- **Week 4**: 48 tables ’ Enables financial and insurance operations
- **Week 5**: 64 tables ’ Enables pharmacy and medication workflows
- **Week 6**: 84 tables ’ Enables laboratory and diagnostic operations
- **Week 7**: 96 tables ’ Enables scheduling and appointment management
- **Week 8**: 112 tables ’ Enables administrative and workflow operations
- **Week 9-10**: 136 tables ’ Enables specialty clinical workflows
- **Week 11-16**: 396 tables ’ Complete HL7 v2.3 table coverage

### **Clinical Workflow Enablement**
- **Patient Registration**: Week 1-2 (core demographics and status)
- **Clinical Orders**: Week 2-3 (order control and priority)
- **Lab Results**: Week 3-6 (results, specimens, diagnostics)
- **Pharmacy**: Week 5 (medication administration and dispensing)
- **Scheduling**: Week 7 (appointments and resource management)

**Remember**: Tables provide the controlled vocabularies that give meaning to coded fields throughout HL7. Accuracy here ensures clinical data integrity.