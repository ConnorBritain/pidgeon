# HL7 v2.3 Data Sprint Process

**Purpose**: Systematic, high-quality recreation of HL7 v2.3 JSON definitions using pristine templates while maintaining official standards compliance.

**Status**: ACTIVE - Template-based clean slate approach initiated
**Priority**: CRITICAL - Enables CLI lookup functionality excellence

---

## 🎯 **Sprint Objectives**

### **Primary Goal**
Transform 1,025 bloated, schema-driven JSON files into lean, template-compliant definitions that enhance `pidgeon lookup` command functionality.

### **Quality Standards**
- **100% template compliance** - Every file follows `_TEMPLATES/` structure exactly
- **Official HL7 v2.3 accuracy** - All content verified against https://www.hl7.eu/HL7v2x/v23/std23/hl7.htm
- **Lookup optimization** - Clean field paths, searchable descriptions, proper cross-references
- **Zero YAGNI violations** - No generation artifacts, vendor bloat, or over-engineering

---

## 📋 **MANDATORY Sprint Process**

### **Step 1: Research Phase (Do NOT Skip)**
For each file to create:

1. **Review backup file** in `_backup/{category}/filename.json`
   - Understand existing field structure
   - Extract essential information
   - Note current YAGNI violations to avoid

2. **Check priority order** in `docs/roadmap/data_integrity/HL7_PRIORITY_ITEMS.md`
   - Verify item is in current development tier
   - Understand interdependencies and prerequisites
   - Follow MVP critical path for maximum impact

3. **Consult official HL7 v2.3 standard** at https://www.hl7.eu/HL7v2x/v23/std23/hl7.htm
   - Verify field definitions and usage codes
   - Confirm cardinality and data types
   - Check official examples and constraints

4. **Reference template** in `_TEMPLATES/{category}_template.json`
   - Follow structure exactly
   - Apply quality guidelines from `_TEMPLATES/README.md`
   - Use validation checklist from `_TEMPLATES/CLEANUP.md`

### **Step 2: Creation Phase (Template-First)**
1. **Copy appropriate template** as starting point
2. **Fill core identity** (segment/table/dataType + name + description)
3. **Add essential fields** following template patterns exactly
4. **Include 2-3 representative examples** maximum
5. **Add minimal critical notes** only

### **Step 3: Validation Phase (Zero Tolerance)**
Apply complete checklist from `_TEMPLATES/CLEANUP.md`:

#### **Essential Structure Check**
- [ ] Identity complete (segment/name/description)
- [ ] Standard metadata (`"standard": "hl7v23", "version": "2.3"`)
- [ ] Usage patterns (R/O/C codes proper)
- [ ] Cardinality (min/max values set)
- [ ] Data types (valid HL7 v2.3 types only)

#### **Field-Level Validation**
- [ ] Field naming (XXX.1, XXX.2 format)
- [ ] Table references (valid table numbers)
- [ ] Valid values (array of codes for coded fields)
- [ ] Examples (real-world representative)
- [ ] Components (proper nested structure)

#### **YAGNI Compliance**
- [ ] No generation hints
- [ ] No vendor variations
- [ ] No scenario objects
- [ ] Lean examples (max 3)
- [ ] Minimal notes (critical only)

#### **Lookup Optimization**
- [ ] Searchable descriptions (business-focused)
- [ ] Cross-references (usedIn populated)
- [ ] Component paths (XXX.Y.Z addressing)
- [ ] Pattern detection ready

### **Step 4: Completion Phase**
1. **Save new file** to appropriate directory (`segments/`, `tables/`, etc.)
2. **Delete corresponding backup** from `_backup/{category}/filename.json`
3. **Track progress** - shrinking backup folders show completion

---

## 🚨 **CRITICAL: Never Skip Steps**

### **Forbidden Shortcuts**
- ❌ **Don't copy-paste between files** - each requires individual research
- ❌ **Don't assume field meanings** - verify against official HL7 standard
- ❌ **Don't skip template validation** - every field must follow patterns
- ❌ **Don't include YAGNI** - if unsure, exclude rather than bloat

### **Required References**
Every file creation MUST reference:
1. **Backup file** - understand existing structure
2. **Priority order** - follow MVP critical path from HL7_PRIORITY_ITEMS.md
3. **Official HL7 v2.3 standard** - verify accuracy
4. **Template** - follow structure exactly
5. **Quality guidelines** - apply standards consistently

---

## 📊 **Sprint Priority Order**

### **Phase 1: Core Segments** (Highest Impact)
1. **MSH** (Message Header) - most critical for all messages
2. **PID** (Patient Identification) - core patient data
3. **PV1** (Patient Visit) - encounter information
4. **OBR** (Observation Request) - lab/radiology orders
5. **OBX** (Observation/Result) - test results

### **Phase 2: Essential Tables** (High Impact)
1. **Table 0001** (Administrative Sex) - widely referenced
2. **Table 0002** (Marital Status) - patient demographics
3. **Table 0076** (Message Type) - message structure
4. **Table 0080** (Nature of Abnormal Testing) - clinical results

### **Phase 3: Foundation Data Types** (Medium Impact)
1. **ST** (String) - most common primitive
2. **TS** (Timestamp) - critical for sequencing
3. **CE** (Coded Element) - coded values
4. **XPN** (Extended Person Name) - patient names
5. **CX** (Extended Composite ID) - patient identifiers

---

## 🎯 **Multi-Agent Coordination**

### **Agent Assignment Strategy**
- **One agent per file** - prevents conflicts
- **Sequential completion** - finish validation before starting next
- **Backup deletion tracking** - immediate progress visibility
- **Quality gates** - no agent bypasses validation checklist

### **Agent Instructions**
1. **State your target file** clearly at start
2. **Reference all required sources** (backup + HL7 standard + template)
3. **Show validation checklist completion** before finishing
4. **Delete backup file** only after successful validation
5. **Report completion** with file location

---

## 🏆 **Success Metrics**

### **Progress Tracking**
- **Backup files remaining**: 1,025 → 0 (shrinking = progress)
- **Empty backup folders**: Indicates category completion
- **Template compliance**: 100% validation checklist adherence
- **Lookup functionality**: Enhanced search and display quality

### **Quality Verification**
- **File size reduction**: 40-60% smaller than originals
- **Clean descriptions**: Business-focused, under 200 characters
- **Proper cross-references**: usedIn arrays populated
- **Official accuracy**: All fields verified against HL7 v2.3 standard

---

## 🚀 **Sprint Completion**

### **Final Validation**
1. **All backup folders empty** - 1,025 files migrated
2. **Lookup command testing** - verify functionality preserved
3. **Template compliance audit** - spot-check random files
4. **Performance verification** - confirm parsing improvements

### **Documentation Updates**
1. **Update BACKUP_MANIFEST.md** - mark sprint complete
2. **Archive _backup directory** - preserve for historical reference
3. **Update CLI documentation** - reflect enhanced lookup capabilities

---

**Remember**: This sprint establishes the foundation for all future CLI functionality. Quality over speed - every file must be pristine, accurate, and template-compliant.