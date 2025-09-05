# HL7 v2.3 Standards Requirements - Validated Implementation Guide

**Created**: September 5, 2025  
**Purpose**: Authoritative requirements for HL7 v2.3 standards-compliant message generation  
**Sources**: HL7.org official specification, validated web searches, reference implementations

---

## 🎯 HL7 v2.3 Message Structure Requirements

### **Core Structure Rules**
1. **Segments**: Separated by carriage return `\r` (0x0D hex)
2. **Fields**: Separated by pipe `|` character
3. **Components**: Separated by caret `^` character  
4. **Subcomponents**: Separated by ampersand `&` character
5. **Repetitions**: Separated by tilde `~` character
6. **Escape**: Backslash `\` character

### **Message Line Endings**
- Segments MUST be separated by `\r\n` (CRLF)
- Message should NOT end with trailing CRLF

---

## 📋 ADT^A01 (Admit/Visit Notification) Requirements

### **Required Segments (MANDATORY)**
1. **MSH** - Message Header
2. **EVN** - Event Type  
3. **PID** - Patient Identification
4. **PV1** - Patient Visit

### **Optional Segments**
- `[NK1]` - Next of Kin (may repeat)
- `[PV2]` - Patient Visit Additional Info
- `[{OBX}]` - Observation/Result (may repeat)
- `[{AL1}]` - Allergy Information (may repeat)
- `[{DG1}]` - Diagnosis Information (may repeat)
- Additional: PR1, GT1, IN1, IN2, IN3, ACC, UB1, UB2

---

## 🔧 MSH Segment Requirements (HL7 v2.3)

### **Required Fields (6 mandatory)**

| Position | Field Name | Format | Example | Validation Rules |
|----------|------------|---------|---------|------------------|
| MSH-1 | Field Separator | Single char | `\|` | Always pipe character (ASCII 124) |
| MSH-2 | Encoding Characters | 4 chars | `^~\\&` | Component, repeat, escape, subcomponent |
| MSH-9 | Message Type | ID^ID^ID | `ADT^A01^ADT_A01` | Message^Trigger^Structure (3rd optional) |
| MSH-10 | Message Control ID | String | `MSG00001` | Unique identifier per message |
| MSH-11 | Processing ID | Single char | `P` | P=Production, T=Training, D=Debug |
| MSH-12 | Version ID | Version | `2.3` | Must be "2.3" for v2.3 messages |

### **Commonly Used Optional Fields**

| Position | Field Name | Format | Example |
|----------|------------|---------|---------|
| MSH-3 | Sending Application | String | `PIDGEON` |
| MSH-4 | Sending Facility | String | `FACILITY` |
| MSH-5 | Receiving Application | String | `RECEIVER` |
| MSH-6 | Receiving Facility | String | `FACILITY` |
| MSH-7 | Date/Time of Message | Timestamp | `20250905141523` |

### **MSH Timestamp Format**
- Format: `YYYYMMDDHHMMSS` (14 digits)
- Variants allowed: `YYYYMMDD` (8), `YYYYMMDDHHMM` (12)
- Must be valid date/time values

---

## 📋 EVN Segment Requirements

### **Required Fields**

| Position | Field Name | Format | Example | Validation |
|----------|------------|---------|---------|------------|
| EVN-1 | Event Type Code | ID | `A01` | Must match MSH-9 trigger |
| EVN-2 | Recorded Date/Time | Timestamp | `20250905141523` | Same format as MSH-7 |

### **Optional Fields**
- EVN-3: Date/Time Planned Event
- EVN-4: Event Reason Code
- EVN-5: Operator ID

---

## 👤 PID Segment Requirements

### **Required Fields**

| Position | Field Name | Format | Example | Validation |
|----------|------------|---------|---------|------------|
| PID-1 | Set ID | SI | `1` | Positive integer, usually 1 |
| PID-3 | Patient Identifier List | CX | `12345^^^FACILITY^MR` | ID^^^AssigningAuth^Type |
| PID-5 | Patient Name | XPN | `Doe^John^A^III` | Family^Given^Middle^Suffix^Prefix |

### **Commonly Used Optional Fields**

| Position | Field Name | Format | Example | Validation |
|----------|------------|---------|---------|------------|
| PID-7 | Date of Birth | DT | `19800515` | YYYYMMDD format |
| PID-8 | Administrative Sex | IS | `M` | M/F/O/U/A/N |
| PID-11 | Patient Address | XAD | `123 Main St^Apt 4^City^ST^12345^USA` | Street^Other^City^State^Zip^Country |

### **Name Component Rules**
- Minimum: `Family^Given`
- Full format: `Family^Given^Middle^Suffix^Prefix^Degree`
- Multiple names separated by `~`

---

## 🏥 PV1 Segment Requirements

### **Required Fields**

| Position | Field Name | Format | Example | Validation |
|----------|------------|---------|---------|------------|
| PV1-1 | Set ID | SI | `1` | Positive integer |
| PV1-2 | Patient Class | IS | `I` | E=Emergency, I=Inpatient, O=Outpatient, P=Preadmit, R=Recurring, B=Obstetrics |

### **Commonly Used Optional Fields**

| Position | Field Name | Format | Example | Validation |
|----------|------------|---------|---------|------------|
| PV1-3 | Assigned Patient Location | PL | `WARD^101^A` | Building^Room^Bed^Facility^Status^Type |
| PV1-7 | Attending Doctor | XCN | `004777^SMITH^JOHN^A` | ID^Family^Given^Middle |
| PV1-19 | Visit Number | CX | `V123456` | Visit identifier |
| PV1-44 | Admit Date/Time | TS | `20250905141523` | Timestamp format |

---

## 🧪 ORU^R01 (Lab Results) Additional Requirements

### **Required Segments**
1. MSH - Message Header
2. PID - Patient Identification  
3. OBR - Observation Request (at least one)
4. OBX - Observation Result (at least one per OBR)

### **OBR Segment Key Fields**
- OBR-1: Set ID (positive integer)
- OBR-4: Universal Service Identifier (required)

### **OBX Segment Key Fields**
- OBX-1: Set ID (positive integer)
- OBX-2: Value Type (NM=Numeric, TX=Text, CE=Coded Element, etc.)
- OBX-3: Observation Identifier (required)
- OBX-5: Observation Value (required)

---

## 💊 RDE^O11 (Pharmacy Order) Additional Requirements

### **Required Segments**
1. MSH - Message Header
2. PID - Patient Identification
3. RXE - Pharmacy/Treatment Encoded Order

### **RXE Segment Key Fields**
- RXE-1: Quantity/Timing
- RXE-2: Give Code (medication identifier, required)
- RXE-3: Give Amount Min
- RXE-5: Give Units

---

## ✅ Validation Checklist

### **Message Level**
- [ ] Segments separated by `\r\n`
- [ ] No trailing CRLF at end of message
- [ ] Required segments present in correct order
- [ ] MSH is first segment

### **MSH Segment**
- [ ] Field separator is `|`
- [ ] Encoding characters are `^~\&`
- [ ] Message type matches pattern (e.g., `ADT^A01`)
- [ ] Control ID is unique
- [ ] Processing ID is valid (P/T/D)
- [ ] Version is "2.3"

### **Field Level**
- [ ] Timestamps in valid format (YYYYMMDDHHMMSS or variants)
- [ ] Required fields non-empty
- [ ] Gender codes valid (M/F/O/U/A/N)
- [ ] Patient class valid (E/I/O/P/R/B)
- [ ] Set IDs are positive integers
- [ ] Name components properly delimited with `^`

---

## 🎯 Implementation Priority

### **Phase 1: Core ADT^A01**
1. MSH segment builder with all required fields
2. EVN segment with event type and timestamp
3. PID segment with patient demographics
4. PV1 segment with visit information

### **Phase 2: Extended Message Types**
1. ADT^A08 (Update patient)
2. ADT^A03 (Discharge)
3. ORU^R01 (Lab results with OBR/OBX)
4. RDE^O11 (Pharmacy orders with RXE)

### **Phase 3: Validation & Polish**
1. Field-level validation
2. Segment order validation
3. Required field enforcement
4. Data type validation

---

*This document represents the authoritative requirements for HL7 v2.3 implementation based on official standards and validated sources.*