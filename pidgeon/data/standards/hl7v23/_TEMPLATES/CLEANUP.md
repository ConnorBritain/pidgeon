# HL7 v2.3 JSON Cleanup Guide

**Purpose**: Systematic approach to migrate existing JSON files to pristine template standards while maintaining lookup functionality.

## 🎯 **Migration Strategy**

### **Phase 1: Template-First New Development**
**Priority**: All NEW files must follow templates exactly
- Use templates as starting point for any new segment/table/datatype
- Copy template → Fill in specific values → Validate against checklist
- **No exceptions** - templates are the new standard

### **Phase 2: Strategic Remediation of Existing Files**
**Priority**: High-impact files first, based on lookup usage patterns

#### **Remediation Order**:
1. **Core segments** (MSH, PID, PV1, OBR, OBX) - most frequently looked up
2. **Common tables** (0001, 0002, 0003, 0076, 0080) - referenced across segments
3. **Essential data types** (ST, ID, TS, CE, XPN) - foundational types
4. **Message structures** - once lookup supports them
5. **Remaining segments** - systematic completion

## 📋 **File-by-File Remediation Process**

### **Step 1: Backup and Analyze**
```bash
# Create backup of current file
cp segments/PID.json segments/PID.json.backup

# Analyze current structure vs template
diff segments/PID.json _TEMPLATES/segment_template.json
```

### **Step 2: Template-Based Rewrite**
**CRITICAL**: Don't try to "fix" existing files - rewrite from template

1. **Copy appropriate template**
2. **Fill core identity** (segment/table/dataType + name)
3. **Add essential fields** following template structure exactly
4. **Include only required examples** (2-3 maximum)
5. **Add minimal notes** (critical constraints only)

### **Step 3: Quality Validation Checklist**

#### **✅ Essential Structure Check**
- [ ] **Identity complete**: segment/name/description filled
- [ ] **Standard metadata**: `"standard": "hl7v23", "version": "2.3"`
- [ ] **Usage patterns**: R/O/C usage codes properly set
- [ ] **Cardinality**: min/max values for all fields and components
- [ ] **Data types**: Valid HL7 v2.3 types only

#### **✅ Field-Level Validation**
- [ ] **Field naming**: XXX.1, XXX.2 format (not XXX-1, XXX_1)
- [ ] **Table references**: Valid table numbers where applicable
- [ ] **Valid values**: Array of codes for coded fields
- [ ] **Examples**: Real-world representative values
- [ ] **Components**: Proper nested structure for composite types

#### **✅ YAGNI Compliance**
- [ ] **No generation hints**: Remove `generationRules`, `fieldGuidance`
- [ ] **No vendor variations**: Remove complex vendor-specific objects
- [ ] **No scenario objects**: Remove nested workflow scenarios
- [ ] **Lean examples**: Maximum 3 examples per field
- [ ] **Minimal notes**: Only critical implementation gotchas

#### **✅ Lookup Optimization**
- [ ] **Searchable descriptions**: Clear, concise, business-focused
- [ ] **Cross-references**: usedIn arrays populated correctly
- [ ] **Component paths**: Proper XXX.Y.Z nested addressing
- [ ] **Pattern detection ready**: Clean field paths for lookup

## 🔧 **Template-Specific Guidelines**

### **Segments** (`segment_template.json`)
```json
{
  "segment": "PID",
  "name": "Patient Identification",
  "description": "Contains patient demographic and identification information",
  "fields": {
    "PID.3": {
      "name": "Patient Identifier List",
      "description": "Primary patient identifier with assigning authority",
      "dataType": "CX",
      "usage": "R",
      "cardinality": {"min": 1, "max": "unbounded"},
      "table": "0061",
      "validValues": ["MR", "PI", "SS"],
      "examples": ["123456789^^^EPIC^MR", "987654321^^^CERNER^PI"]
    }
  }
}
```

**Key Points**:
- Field keys as `PID.1`, `PID.2` (not `PID-1` or `field1`)
- Include `validValues` for coded fields (extracted by lookup)
- Component structure under `components` object when needed
- Examples show format, not repetition

### **Tables** (`table_template.json`)
```json
{
  "table": "0061",
  "name": "Check Digit Scheme",
  "description": "Identifies the check digit scheme employed",
  "type": "hl7_defined",
  "values": [
    {
      "code": "M10",
      "description": "Mod 10 algorithm",
      "definition": "Check digit algorithm using modulus 10"
    }
  ],
  "usedIn": ["PID.3.4", "PV1.19.4"]
}
```

**Key Points**:
- Both `description` (brief) and `definition` (detailed) for values
- `usedIn` tracks segment.field relationships
- Type classification: `hl7_defined` vs `user_defined`

### **Data Types** (`datatype_template.json`)
```json
{
  "dataType": "CX",
  "name": "Extended Composite ID With Check Digit",
  "description": "Composite identifier with check digit and assigning authority",
  "category": "Composite",
  "components": {
    "CX.1": {
      "name": "ID Number",
      "dataType": "ST",
      "usage": "O",
      "length": 15
    },
    "CX.4": {
      "name": "Assigning Authority",
      "dataType": "HD",
      "usage": "O"
    }
  }
}
```

**Key Points**:
- Component keys as `CX.1`, `CX.2` etc.
- Length constraints where specified in standard
- Clear usage hierarchy for nested components

## 🚨 **Common Pitfalls to Avoid**

### **❌ Don't Do - Legacy Patterns**
- **Verbose descriptions**: "This field contains the patient's medical record number..."
- **Generation artifacts**: References to "auto-generated" or "procedural"
- **YAGNI bloat**: Complex nested objects for vendor variations
- **Inconsistent structure**: Different field naming across files
- **Over-detailed examples**: 10+ examples showing same pattern

### **✅ Do - Template Patterns**
- **Concise descriptions**: "Primary patient identifier with assigning authority"
- **Clean structure**: Consistent field/component naming
- **Essential examples**: 2-3 showing format variation
- **Standard metadata**: Always include standard/version
- **Business focus**: What field means, not how it's generated

## 📊 **Progress Tracking**

### **High-Priority Remediation Targets**
- [ ] **MSH** (Message Header) - most critical segment
- [ ] **PID** (Patient Identification) - core patient data
- [ ] **PV1** (Patient Visit) - encounter information
- [ ] **OBR** (Observation Request) - lab/radiology orders
- [ ] **OBX** (Observation/Result) - test results
- [ ] **Table 0001** (Administrative Sex) - widely referenced
- [ ] **Table 0002** (Marital Status) - patient demographics
- [ ] **Data Type ST** (String) - most common primitive
- [ ] **Data Type TS** (Timestamp) - critical for sequencing

### **Success Metrics**
- **Lookup output cleanliness**: No YAGNI fields in search results
- **Search accuracy**: Descriptions match business intent
- **Cross-reference integrity**: usedIn relationships complete
- **Performance**: JSON parsing speed improved
- **Maintainability**: Template adherence measurable

## 🎉 **End Goal**

**Before**: Inconsistent, bloated JSON files with generation artifacts
**After**: Pristine, lean, lookup-optimized definitions following template standards

**Result**: Professional-grade standards database that enhances Pidgeon's lookup functionality while establishing foundation for cross-standard platform features.

---

**Remember**: Templates aren't suggestions - they're the new standard. Every file should follow them exactly for consistency, quality, and platform evolution.