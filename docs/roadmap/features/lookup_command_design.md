# Pidgeon Lookup Command - Smart Healthcare Standards Reference

**Status**: Ready for Implementation  
**Priority**: P1.3 Core Enhancement  
**Complexity**: Medium  
**Value**: High - Competitive differentiation vs Caristix  

---

## 🎯 **Vision**

Transform healthcare standards lookup from **"hunt through documentation"** to **"instant CLI answers"** with smart pattern recognition and comprehensive coverage.

**User Goal**: `pidgeon lookup PID.3.5` → Instant field definition, usage examples, and generation hints.

---

## 🧠 **Smart Pattern Recognition**

### **Automatic Categorization Logic**
```bash
# PATTERN DETECTION (Zero configuration required)
PID.3.5      → Field (contains dots)
PID.3        → Field (contains dots) 
PID          → Segment (3 letters, all caps)
0001         → Table (4 digits, leading zeros)
AD           → Data Type (2-3 letters, all caps, not a known segment)
A01          → Trigger Event (letter + digits)
"Patient ID" → Search (quoted text)
patient      → Search (lowercase/mixed case/natural language)
```

### **Smart Disambiguation**
```bash
# CONTEXT-AWARE RESOLUTION
PID          → Segment (matches segment list)
GEN          → Data Type (not in segment list, matches datatype list)
0001         → Table (4-digit pattern)
A01          → Trigger (letter+number pattern)
```

---

## 📖 **Command Interface Design**

### **Primary Usage (90% of cases)**
```bash
# SMART INFERENCE - Zero flags needed
pidgeon lookup PID              # Segment definition
pidgeon lookup PID.3.5          # Field definition  
pidgeon lookup 0001             # Table with all values
pidgeon lookup A01              # Trigger event details
pidgeon lookup AD               # Data type definition
```

### **Extended Usage (10% of cases)**
```bash
# CONTENT MODIFIERS
pidgeon lookup PID --fields     # List all fields in segment
pidgeon lookup 0001 --values    # Show all coded values  
pidgeon lookup A01 --structure  # Message structure details
pidgeon lookup PID --examples   # Usage examples
pidgeon lookup PID --verbose    # Full details

# SEARCH CAPABILITIES
pidgeon lookup "Patient ID"     # Free text search
pidgeon lookup emergency        # Keyword search across descriptions
pidgeon lookup --search "address"  # Explicit search mode
```

### **Output Control**
```bash
# FORMAT OPTIONS (inherited from global CLI patterns)
pidgeon lookup PID --format json          # Machine-readable output
pidgeon lookup 0001 --output tables.json  # Write to file
pidgeon lookup PID --quiet                # Minimal output
```

---

## 🗂️ **Data Architecture Integration**

### **JSON Structure Mapping**
```
/data/standards/hl7v23/
├── segments/     → pidgeon lookup PID
├── datatypes/    → pidgeon lookup AD  
├── tables/       → pidgeon lookup 0001
└── triggerevents/ → pidgeon lookup A01
```

### **Field Path Resolution**
```bash
pidgeon lookup PID.3.5
# Resolves to: segments/pid.json → fields.PID.3 → components.PID.3.5
```

### **Search Index Strategy**
```json
{
  "searchable_content": {
    "PID": ["Patient Identification", "demographics", "identifiers"],
    "0001": ["Sex", "administrative sex", "gender", "male", "female"],
    "A01": ["Admit", "admission", "inpatient", "visit notification"]
  }
}
```

---

## 🎨 **Output Design**

### **Segment Lookup Output**
```bash
$ pidgeon lookup PID
┌─ PID - Patient Identification ────────────────────────────────┐
│ Standard: HL7 v2.3 | Usage: Required | Chapter: Patient Admin │
├─────────────────────────────────────────────────────────────────┤
│ Description: Primary means of communicating patient            │
│ identification information. Contains permanent patient         │
│ identifying and demographic information.                       │
├─────────────────────────────────────────────────────────────────┤
│ Key Fields:                                                    │
│   PID.3  Patient Identifier List        [Required]            │
│   PID.5  Patient Name                    [Optional]           │
│   PID.7  Date/Time of Birth              [Optional]           │
│   PID.8  Administrative Sex              [Optional]           │
│   PID.11 Patient Address                 [Optional]           │
├─────────────────────────────────────────────────────────────────┤
│ Usage: Required in ADT^A01, ORU^R01, ORM^O01, RDE^O11         │
│ More: pidgeon lookup PID --fields                              │
└─────────────────────────────────────────────────────────────────┘
```

### **Field Lookup Output**
```bash
$ pidgeon lookup PID.3.5
┌─ PID.3.5 - Identifier Type Code ──────────────────────────────┐
│ Segment: PID | Component: 5 | Data Type: ID | Length: 5       │
├─────────────────────────────────────────────────────────────────┤
│ Description: Code corresponding to the type of identifier.     │
│ May be used as qualifier to Assigning Authority component.     │
├─────────────────────────────────────────────────────────────────┤
│ Table: 0203 - Identifier Type                                 │
│   MR - Medical record number                                   │
│   SS - Social Security number                                  │
│   DL - Driver's license number                                 │
│   AN - Account number                                          │
├─────────────────────────────────────────────────────────────────┤
│ Examples: MR, SS, DL, FN                                       │
│ Generation: Most common: MR for medical record number         │
└─────────────────────────────────────────────────────────────────┘
```

### **Table Lookup Output**
```bash
$ pidgeon lookup 0001
┌─ Table 0001 - Sex ─────────────────────────────────────────────┐
│ Type: User Defined | Chapter: Patient Administration          │
├─────────────────────────────────────────────────────────────────┤
│ Values:                                                        │
│   F - Female                                                   │
│   M - Male                                                     │
│   O - Other                                                    │
│   U - Unknown                                                  │
├─────────────────────────────────────────────────────────────────┤
│ Used in: PID.8, NK1.15                                        │
│ Generation: F(51%), M(49%), O(0.2%), U(0.1%)                  │
└─────────────────────────────────────────────────────────────────┘
```

### **Search Results Output**
```bash
$ pidgeon lookup "patient id"
Found 3 matches for "patient id":

┌─ Segments ─────────────────────────────────────────────────────┐
│ PID - Patient Identification                                   │
│   Primary means of communicating patient identification...     │
└─────────────────────────────────────────────────────────────────┘

┌─ Fields ───────────────────────────────────────────────────────┐
│ PID.3 - Patient Identifier List                               │
│   List of identifiers used by healthcare facility...          │
└─────────────────────────────────────────────────────────────────┘

┌─ Tables ───────────────────────────────────────────────────────┐
│ 0203 - Identifier Type                                        │
│   Codes for different types of patient identifiers...         │
└─────────────────────────────────────────────────────────────────┘

Use: pidgeon lookup PID.3 for details
```

---

## 🏗️ **Implementation Architecture**

### **Command Structure**
```csharp
[Command("lookup", Description = "Look up HL7 fields, segments, tables, and data types")]
public class LookupCommand : AsyncCommand<LookupCommandSettings>
{
    public async Task<int> ExecuteAsync(CommandContext context, LookupCommandSettings settings)
    {
        // Smart pattern recognition
        var lookupType = PatternDetector.DetectType(settings.Query);
        
        // Route to appropriate handler
        return lookupType switch
        {
            LookupType.Field => await HandleFieldLookup(settings),
            LookupType.Segment => await HandleSegmentLookup(settings),
            LookupType.Table => await HandleTableLookup(settings),
            LookupType.DataType => await HandleDataTypeLookup(settings),
            LookupType.TriggerEvent => await HandleTriggerLookup(settings),
            LookupType.Search => await HandleSearchLookup(settings),
            _ => await HandleSearchLookup(settings) // Default to search
        };
    }
}
```

### **Pattern Detection Service**
```csharp
public static class PatternDetector
{
    public static LookupType DetectType(string query)
    {
        // Field pattern: PID.3.5, MSH.9, OBR.4
        if (Regex.IsMatch(query, @"^[A-Z]{2,3}\.\d+(\.\d+)?$"))
            return LookupType.Field;
            
        // Table pattern: 0001, 0203, 0076
        if (Regex.IsMatch(query, @"^\d{4}$"))
            return LookupType.Table;
            
        // Trigger pattern: A01, O01, R01
        if (Regex.IsMatch(query, @"^[A-Z]\d{2}$"))
            return LookupType.TriggerEvent;
            
        // Segment pattern: PID, MSH, OBR (3 letters, known segment)
        if (Regex.IsMatch(query, @"^[A-Z]{2,3}$") && IsKnownSegment(query))
            return LookupType.Segment;
            
        // Data type pattern: AD, CE, CX (2-3 letters, not a segment)  
        if (Regex.IsMatch(query, @"^[A-Z]{2,3}$") && IsKnownDataType(query))
            return LookupType.DataType;
            
        // Default to search for everything else
        return LookupType.Search;
    }
}
```

### **Service Integration**
```csharp
public interface IStandardLookupService
{
    Task<LookupResult> LookupSegmentAsync(string segmentName);
    Task<LookupResult> LookupFieldAsync(string fieldPath);
    Task<LookupResult> LookupTableAsync(string tableNumber);
    Task<LookupResult> LookupDataTypeAsync(string dataTypeName);
    Task<LookupResult> LookupTriggerEventAsync(string triggerCode);
    Task<IEnumerable<LookupResult>> SearchAsync(string searchTerms);
}
```

---

## 🚀 **Implementation Plan**

### **Phase 1: Core Infrastructure**
1. ✅ Create `LookupCommand` with pattern detection
2. ✅ Implement `PatternDetector` service  
3. ✅ Extend `JsonHL7ReferencePlugin` for lookup operations
4. ✅ Create output formatters for human-readable display

### **Phase 2: Smart Features**
1. ✅ Implement field path resolution (PID.3.5 → JSON structure)
2. ✅ Add search functionality across all categories
3. ✅ Create content modifiers (--fields, --values, --structure)
4. ✅ Integration testing with existing JSON foundation

### **Phase 3: Polish & Enhancement**
1. ⏳ Rich output formatting with tables/boxes
2. ⏳ Search indexing for performance  
3. ⏳ Auto-complete support for shell completion
4. ⏳ JSON output format for machine consumption

---

## 💡 **Competitive Advantages**

### **vs. Caristix (Web-based)**
- ✅ **CLI-first**: Instant access without browser context switching
- ✅ **Offline**: Works without internet connectivity
- ✅ **Automation-friendly**: JSON output for scripting
- ✅ **Generation integration**: Lookup → Generate workflow

### **vs. Manual Documentation**
- ✅ **Smart search**: Find by description, not just codes
- ✅ **Cross-references**: Automatic field relationships
- ✅ **Usage context**: Where fields are used, examples
- ✅ **Generation hints**: Realistic data creation guidance

### **vs. Existing CLI Tools**
- ✅ **Pattern recognition**: Zero-configuration usage
- ✅ **Comprehensive coverage**: 1,025+ definitions ready
- ✅ **Rich content**: Examples, cross-references, vendor intelligence
- ✅ **Future-ready**: Extensible to FHIR R4, NCPDP standards

---

## 📊 **Success Metrics**

### **Adoption Metrics**
- `pidgeon lookup` usage in CLI analytics
- Search query patterns and success rates
- Time-to-answer for common lookup scenarios

### **Quality Metrics**
- Search result relevance and accuracy
- Field resolution success rate (PID.3.5 → correct definition)
- User feedback on output formatting and usefulness

### **Business Impact**
- Differentiation vs Caristix in user onboarding
- Conversion from lookup usage to generation/validation workflows
- Professional tier upgrade triggers from advanced lookup features

---

**Implementation Ready**: This design provides complete specification for building the industry-leading healthcare standards lookup CLI tool that leverages our comprehensive HL7 v2.3 foundation.