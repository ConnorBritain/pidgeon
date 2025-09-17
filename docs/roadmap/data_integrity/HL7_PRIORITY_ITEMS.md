# HL7 v2.3 Priority Development Items - MVP Critical Path

**Purpose**: Rank-ordered critical items for systematic data development, prioritized by MVP necessity and interdependency analysis.

**Strategy**: Build foundation � Enable core workflows � Complete ecosystem

---

## <� **SEGMENTS** (Priority: CRITICAL - Foundation for all lookup functionality)

### **Tier 1: Core Foundation** (MVP Blockers - Build First)
1. **MSH** - Message Header (ALL messages depend on this)
2. **PID** - Patient Identification (Core patient workflows)
3. **PV1** - Patient Visit (Encounter context for everything)
4. **OBR** - Observation Request (Lab/radiology orders - 60% of healthcare workflows)
5. **OBX** - Observation/Result (Test results - completes order cycle)

### **Tier 2: Essential Workflows** (High Impact - Build Second)
6. **EVN** - Event Type (Required for ADT messages)
7. **NK1** - Next of Kin (Patient safety and contact information)
8. **AL1** - Patient Allergy Information (Critical safety data)
9. **DG1** - Diagnosis (Clinical reasoning and billing)
10. **IN1** - Insurance (Financial workflows)

### **Tier 3: Common Extensions** (Medium Impact - Build Third)
11. **PV2** - Patient Visit Additional Info (Extended encounter data)
12. **IN2** - Insurance Additional Information (Extended financial data)
13. **GT1** - Guarantor (Financial responsibility)
14. **NTE** - Notes and Comments (Universal annotation)
15. **ORC** - Common Order (Order control and tracking)

### **Tier 4: Specialized Workflows** (Lower Impact - Build Fourth)
16. **RXE** - Pharmacy/Treatment Encoded Order (Medication orders)
17. **RXA** - Pharmacy/Treatment Administration (Medication administration)
18. **SCH** - Scheduling Activity Information (Appointment scheduling)
19. **PR1** - Procedures (Surgical and procedure codes)
20. **ACC** - Accident (Emergency department workflows)

---

## =� **TABLES** (Priority: HIGH - Enable coded value validation)

### **Tier 1: Universal References** (Used across multiple segments)
1. **Table 0001** - Administrative Sex (PID.8 - universal patient data)
2. **Table 0002** - Marital Status (PID.16 - demographics)
3. **Table 0005** - Race (PID.10 - demographics and reporting)
4. **Table 0006** - Religion (PID.17 - patient preferences)
5. **Table 0004** - Patient Class (PV1.2 - encounter classification)

### **Tier 2: Clinical Operations** (Core healthcare workflows)
6. **Table 0007** - Admission Type (PV1.4 - encounter context)
7. **Table 0023** - Admit Source (PV1.14 - patient flow)
8. **Table 0112** - Discharge Disposition (PV1.36 - patient outcomes)
9. **Table 0063** - Relationship (NK1.3 - emergency contacts)
10. **Table 0127** - Allergen Type (AL1.3 - safety information)

### **Tier 3: Diagnostic and Results** (Lab and clinical data)
11. **Table 0074** - Diagnostic Service Section ID (OBR.24 - lab departments)
12. **Table 0125** - Value Type (OBX.2 - result data types)
13. **Table 0078** - Abnormal Flags (OBX.8 - result interpretation)
14. **Table 0080** - Nature of Abnormal Testing (OBX.14 - result context)
15. **Table 0085** - Observation Result Status (OBX.11 - result workflow)

### **Tier 4: Specialized Clinical** (Department-specific workflows)
16. **Table 0052** - Diagnosis Type (DG1.6 - diagnosis classification)
17. **Table 0061** - Check Digit Scheme (Various identifier validation)
18. **Table 0203** - Identifier Type (Various ID classification)
19. **Table 0131** - Contact Role (NK1.7 - contact relationships)
20. **Table 0064** - Financial Class (PV1.20 - billing categories)

---

## =' **DATA TYPES** (Priority: MEDIUM - Enable complex field structures)

### **Tier 1: Universal Primitives** (Used everywhere)
1. **ST** - String (Most common text field)
2. **ID** - Coded values (Table references)
3. **NM** - Numeric (Measurements and counts)
4. **DT** - Date (Healthcare dates)
5. **TM** - Time (Healthcare times)

### **Tier 2: Complex Identifiers** (Critical for patient matching)
6. **CX** - Extended Composite ID with Check Digit (Patient IDs)
7. **XPN** - Extended Person Name (Patient/provider names)
8. **XAD** - Extended Address (Patient addresses)
9. **XTN** - Extended Telecommunication Number (Phone numbers)
10. **TS** - Timestamp (Date/time combinations)

### **Tier 3: Clinical Data Types** (Healthcare-specific structures)
11. **CE** - Coded Element (Clinical codes with text)
12. **CF** - Coded Element with Formatted Values (Extended clinical codes)
13. **HD** - Hierarchic Designator (System/facility identifiers)
14. **EI** - Entity Identifier (Unique system identifiers)
15. **CM** - Composite (Various composite structures)

### **Tier 4: Specialized Types** (Department-specific needs)
16. **SN** - Structured Numeric (Lab values with comparators)
17. **DR** - Date/Time Range (Appointment scheduling)
18. **FC** - Financial Class (Billing information)
19. **DLN** - Driver's License Number (Patient identification)
20. **CQ** - Composite Quantity with Units (Medical measurements)

---

## =� **MESSAGES** (Priority: LOW - Build after segments/tables complete)

### **Tier 1: Core ADT** (Patient administration - highest volume)
1. **ADT^A01** - Admit/Visit Notification (Hospital admissions)
2. **ADT^A02** - Transfer a Patient (Room/unit transfers)
3. **ADT^A03** - Discharge/End Visit (Hospital discharges)
4. **ADT^A04** - Register a Patient (Outpatient registration)
5. **ADT^A08** - Update Patient Information (Demographics changes)

### **Tier 2: Core Orders** (Clinical orders - critical workflows)
6. **ORU^R01** - Observation Result (Lab results - most common)
7. **ORM^O01** - Order Message (New orders)
8. **RDE^O11** - Pharmacy/Treatment Encoded Order (Medication orders)
9. **ORR^O02** - Order Response (Order acknowledgments)
10. **ACK** - General Acknowledgment (Universal response)

### **Tier 3: Specialized Workflows** (Department-specific)
11. **SIU^S12** - Schedule Information (Appointment scheduling)
12. **MDM^T02** - Medical Document Management (Clinical documents)
13. **BAR^P01** - Add Patient Accounts (Billing initiation)
14. **DFT^P03** - Post Detail Financial Transaction (Charges)
15. **QRY^Q01** - Query for Information (Data requests)

---

## � **TRIGGER EVENTS** (Priority: LOWEST - Simple mappings to messages)

### **Tier 1: Patient Administration** (ADT workflows)
1. **A01** - Admit/Visit Notification
2. **A02** - Transfer a Patient
3. **A03** - Discharge/End Visit
4. **A04** - Register a Patient
5. **A08** - Update Patient Information

### **Tier 2: Clinical Orders** (Order workflows)
6. **R01** - Observation Result
7. **O01** - Order Message
8. **O11** - Pharmacy/Treatment Encoded Order
9. **O02** - Order Response
10. **S12** - Schedule Information

### **Tier 3: Financial and Administrative** (Business workflows)
11. **P01** - Add Patient Accounts
12. **P03** - Post Detail Financial Transaction
13. **T02** - Medical Document Management
14. **Q01** - Query for Information
15. **Z99** - Custom/Site-defined Events

---

## <� **Development Strategy by Interdependency**

### **Phase 1: Foundation** (Build in this exact order)
1. **Core Data Types** (ST, ID, NM, CX, XPN) - Everything depends on these
2. **Universal Tables** (0001, 0002, 0004, 0005, 0006) - Referenced by segments
3. **Core Segments** (MSH, PID, PV1) - Foundation for all messages
4. **First Message** (ADT^A01) - Proves end-to-end functionality

### **Phase 2: Clinical Workflows** (Expand systematically)
1. **Clinical Data Types** (CE, CF, TS, HD) - Support complex clinical data
2. **Clinical Tables** (0007, 0023, 0074, 0125) - Lab and encounter codes
3. **Clinical Segments** (OBR, OBX, EVN) - Lab and result workflows
4. **Core Clinical Messages** (ORU^R01, ORM^O01) - Complete order cycle

### **Phase 3: Complete Ecosystem** (Fill remaining gaps)
1. **Specialized Data Types** (SN, DR, FC) - Department-specific needs
2. **Specialized Tables** (0052, 0061, 0203) - Edge case support
3. **Extended Segments** (NK1, AL1, DG1, IN1) - Complete patient context
4. **Complete Message Set** - All remaining ADT and order messages

---

## =� **MVP Success Criteria**

### **Phase 1 Complete** (Foundation MVP)
-  Basic patient identification and demographics (PID + core tables)
-  Simple encounter context (PV1 + encounter tables)
-  Message structure (MSH + ADT^A01)
-  `pidgeon lookup` works for core patient/encounter fields

### **Phase 2 Complete** (Clinical MVP)
-  Lab order and result workflows (OBR/OBX + lab tables)
-  Core clinical data types support complex medical data
-  `pidgeon generate` creates realistic lab scenarios
-  `pidgeon validate` catches clinical data errors

### **Phase 3 Complete** (Production MVP)
-  Complete patient context (contacts, allergies, insurance)
-  All major message types for hospital integration testing
-  Comprehensive lookup coverage for healthcare developers
-  Ready for real-world healthcare integration projects

**Target**: Phase 1 complete enables 60% of healthcare integration testing scenarios. Phase 2 reaches 85%. Phase 3 achieves 95%+ coverage.

---

## 📋 **COMPREHENSIVE PRIORITY REFERENCES**

### **Complete Development Roadmaps Available**

After completing the main priority items listed above, developers can find **COMPREHENSIVE** priority orderings with full coverage in:

📁 **`docs/roadmap/data_priority/`**
- **`datatype_priority.md`** - Complete 86 datatypes with foundational dependency ordering
- **`segment_priority.md`** - Complete 140+ segments with clinical workflow criticality
- **`table_priority.md`** - Complete 396 tables with cross-reference frequency analysis
- **`message_priority.md`** - Complete 240+ messages with healthcare workflow ordering
- **`triggerevent_priority.md`** - Complete 130+ trigger events with clinical event frequency

### **Enhanced Development Process**

Each comprehensive priority file includes:
- ✅ **Ground truth validation** using `hl7-dictionary` library (80% automated coverage)
- ✅ **Research → Create → Validate workflow** with zero tolerance for validation failures
- ✅ **Critical dependency mapping** showing blocking relationships between categories
- ✅ **MVP milestone tracking** with weekly completion targets and clinical workflow enablement
- ✅ **Template compliance requirements** linking to `_TEMPLATES/` patterns

### **Usage Guidance**

**For MVP Development**: Follow this document's tiered approach for fastest time-to-value.

**For Complete Coverage**: Reference the `data_priority/` files for systematic development of the full HL7 v2.3 standard.

**For Quality Assurance**: Use the ground truth validation tools documented in `docs/HL7_LIBRARY_PROCESS.md` to ensure zero hallucinations and 100% standards compliance.