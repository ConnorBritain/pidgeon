# Pidgeon Smart Diff Command Design Specification

**Version**: 2.0
**Created**: January 2025
**Sprint**: Sprint 3 - Final Features
**Status**: Practical design using existing data sources

---

## 🎯 **Executive Summary**

The enhanced `pidgeon diff` command will provide **smart field-level analysis** of healthcare message differences using existing HL7 metadata and vendor patterns, without requiring custom healthcare databases.

**Key Value**: Transforms basic diff from "field X changed" into "Patient Name (PID.5) changed from 'John Doe' to 'Jane Doe' - demographic field, valid format" using data already scraped and validated.

---

## 📊 **Data-Driven Analysis Using Existing Assets**

### **Leverage Current Data Sources**
1. **HL7 v2.3 scraped field definitions** - Field names, data types, usage patterns
2. **Existing validation metadata** - Required/optional, format patterns, lengths
3. **Vendor profiles** - Epic/Cerner patterns already detected
4. **Message type definitions** - Segment requirements, field populations

### **Smart Analysis Without New Databases**
- **Field categorization**: Use segment names (PID=demographics, AL1=allergy, RXE=medication)
- **Format validation**: Leverage existing validation rules for "still valid" vs "now broken"
- **Pattern detection**: Use existing vendor pattern matching
- **Timing analysis**: Simple arithmetic on timestamp fields

---

## 🧠 **Smart Diff Analysis Engine**

### **1. Field-Level Intelligence**
**Purpose**: Make field changes meaningful using existing metadata
**Implementation**:
```csharp
public class SmartFieldAnalyzer
{
    private readonly IFieldMetadataService _fieldMetadata; // Uses existing scraped data
    private readonly IValidationService _validator;       // Uses existing validation

    public SmartFieldChange AnalyzeChange(string fieldPath, string oldValue, string newValue)
    {
        var metadata = _fieldMetadata.GetFieldInfo(fieldPath); // From scraped HL7 data
        var category = CategorizeField(fieldPath);             // PID=demo, AL1=critical, etc
        var validation = _validator.ValidateBoth(fieldPath, oldValue, newValue);

        return new SmartFieldChange
        {
            FieldPath = fieldPath,
            FieldName = metadata.Name,        // "Patient Name" not "PID.5"
            Category = category,              // Demographic/Clinical/Administrative
            OldValue = oldValue,
            NewValue = newValue,
            FormatStatus = validation.Status, // "Both valid", "Now invalid", etc
            DataType = metadata.DataType      // From existing scraped data
        };
    }
}
```

### **2. Pattern Detection Using Existing Vendor Profiles**
**Purpose**: Detect vendor differences using existing pattern analysis
**Implementation**:
```csharp
public class VendorPatternDetector
{
    private readonly IVendorProfileService _vendorProfiles; // Uses existing profiles

    public VendorDriftAnalysis DetectDrift(List<SmartFieldChange> changes)
    {
        var patterns = new List<string>();

        // Use existing vendor pattern detection
        foreach (var change in changes)
        {
            var oldVendor = _vendorProfiles.DetectVendor(change.OldValue, change.FieldPath);
            var newVendor = _vendorProfiles.DetectVendor(change.NewValue, change.FieldPath);

            if (oldVendor != newVendor)
            {
                patterns.Add($"{change.FieldName}: {oldVendor} → {newVendor} format");
            }
        }

        return new VendorDriftAnalysis { DetectedPatterns = patterns };
    }
}
```

### **3. Timestamp Pattern Analysis**
**Purpose**: Detect systematic time shifts (common in data migration)
**Implementation**:
```csharp
public class TimestampAnalyzer
{
    public TimestampPattern AnalyzeTimestamps(List<SmartFieldChange> changes)
    {
        var timestampChanges = changes
            .Where(c => IsTimestampField(c.FieldPath)) // MSH.7, PID.7, etc
            .Where(c => DateTime.TryParse(c.OldValue, out _) && DateTime.TryParse(c.NewValue, out _))
            .ToList();

        if (timestampChanges.Count < 2) return TimestampPattern.None;

        // Look for consistent offset
        var offsets = timestampChanges.Select(c =>
            DateTime.Parse(c.NewValue) - DateTime.Parse(c.OldValue)
        ).ToList();

        if (offsets.All(o => o == offsets.First()))
        {
            return new TimestampPattern
            {
                Type = PatternType.ConsistentShift,
                Offset = offsets.First(),
                AffectedFields = timestampChanges.Count
            };
        }

        return TimestampPattern.None;
    }
}
```

---

## 📋 **Enhanced Diff Output Design**

### **Smart Summary Section**
```
🔍 Smart Message Diff Analysis
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Files: baseline.hl7 → candidate.hl7
Total Changes: 8 fields

📊 Change Categories:
  Demographics: 3 fields  (Patient Name, DOB, Address)
  Clinical: 1 field       (Allergy Information)
  Administrative: 4 fields (Timestamps, Visit Number)

🕐 Pattern Detection:
  ✓ All timestamps shifted by +2 hours (4 fields)
  ⚠️ Visit number format: V123456 → VN-123456 (Epic → Cerner style)
```

### **Field-Level Detail**
```
📝 FIELD-LEVEL CHANGES
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

🏥 Demographics (Medium Impact)
  Patient Name (PID.5): "SMITH^JOHN^A" → "SMITH^JANE^A"
    Data Type: XPN (Extended Person Name)
    Status: ✅ Both values valid format

  Date of Birth (PID.7): "19850315" → "19840315"
    Data Type: TS (Timestamp)
    Status: ✅ Both values valid, patient age changed by 1 year

🚨 Clinical (High Impact)
  Allergy Type (AL1.3): "DA" → "FA"
    Data Type: IS (Drug vs Food allergy)
    Status: ✅ Both valid codes, but meaning changed significantly
    Impact: May affect medication safety alerts

⏰ Timestamp Pattern (Systematic)
  Message Time (MSH.7): "202501151400" → "202501151600" (+2h)
  Admit Time (PV1.44): "202501151000" → "202501151200" (+2h)
  All timestamps consistently shifted by +2 hours
```

### **Validation Status Integration**
```
✅ VALIDATION STATUS
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Using existing validation rules:

✅ 7 of 8 changes maintain valid formats
❌ 1 field now invalid: PV1.19 (Visit Number)
   - Old: "V123456" (valid Epic format)
   - New: "VN-123456999" (exceeds 12 character limit)

💡 Recommendation: Verify visit number truncation rules
```

---

## 🛠️ **Implementation Architecture**

### **Smart Diff Service Interface**
```csharp
public interface ISmartDiffService : IMessageDiffService
{
    Task<SmartDiffResult> AnalyzeWithContextAsync(
        string leftMessage,
        string rightMessage,
        DiffOptions options);
}

public class SmartDiffResult
{
    public List<SmartFieldChange> FieldChanges { get; init; } = new();
    public VendorDriftAnalysis VendorAnalysis { get; init; } = new();
    public TimestampPattern TimestampPattern { get; init; } = TimestampPattern.None;
    public ValidationSummary ValidationSummary { get; init; } = new();
    public DiffStatistics Statistics { get; init; } = new();
}

public class SmartFieldChange
{
    public string FieldPath { get; init; } = "";      // PID.5
    public string FieldName { get; init; } = "";      // Patient Name
    public FieldCategory Category { get; init; }      // Demographics/Clinical/Administrative
    public string OldValue { get; init; } = "";
    public string NewValue { get; init; } = "";
    public ValidationStatus FormatStatus { get; init; } // Valid/Invalid/Warning
    public string DataType { get; init; } = "";       // From existing metadata
    public string Impact { get; init; } = "";         // Based on field category
}
```

### **Service Dependencies (All Existing)**
```csharp
public class SmartDiffService : ISmartDiffService
{
    private readonly IFieldMetadataService _fieldMetadata;    // Existing - scraped HL7 data
    private readonly IValidationService _validationService;  // Existing - validation rules
    private readonly IVendorProfileService _vendorProfiles; // Existing - vendor patterns
    private readonly IMessageDiffService _baseDiffService;   // Existing - basic diff

    // Implementation uses existing services, no new databases needed
}
```

---

## 🚀 **Implementation Plan**

### **Phase 1: Smart Field Analysis (Week 1)**
- [ ] Create `SmartFieldAnalyzer` using existing `IFieldMetadataService`
- [ ] Implement field categorization based on segment names (PID/AL1/RXE/etc)
- [ ] Add validation status integration using existing `IValidationService`
- [ ] Create enhanced output formatting with field names and categories

### **Phase 2: Pattern Detection (Week 2)**
- [ ] Implement `VendorPatternDetector` using existing `IVendorProfileService`
- [ ] Add `TimestampAnalyzer` for systematic time shift detection
- [ ] Create vendor drift analysis and reporting
- [ ] Add configuration change detection (format changes, new fields)

### **Phase 3: Integration & Output (Week 3)**
- [ ] Integrate smart analysis with existing `DiffCommand`
- [ ] Add enhanced console output with categories and impact levels
- [ ] Implement HTML/JSON report generation
- [ ] Add summary statistics and recommendations

### **Phase 4: Refinement (Week 4)**
- [ ] Add more field categorization rules based on HL7 segments
- [ ] Enhance vendor pattern detection with existing profiles
- [ ] Performance optimization for large message comparisons
- [ ] User testing and output refinement

---

## ✅ **Success Metrics**

### **Practical Goals**
- [ ] **Field context provided** for 90%+ of changes (using existing metadata)
- [ ] **Vendor pattern detection** using existing vendor profiles
- [ ] **Validation integration** shows which changes break existing rules
- [ ] **Sub-second performance** for typical message pairs
- [ ] **Zero new databases** - uses only existing scraped and profile data

### **User Value**
- [ ] **Meaningful change descriptions** instead of raw field paths
- [ ] **Impact categorization** helps prioritize which changes matter
- [ ] **Pattern detection** identifies systematic changes vs individual edits
- [ ] **Professional output** suitable for technical and business stakeholders

---

## 💡 **Future Enhancements**

### **Using More Existing Data**
- **Message frequency analysis** - Use existing generation patterns to identify "unusual" changes
- **Field correlation analysis** - Use existing field relationship data
- **Historical pattern learning** - Build on existing vendor profile detection
- **Cross-standard mapping** - Use existing FHIR/HL7 equivalence data

This practical design leverages existing data assets to provide intelligent diff analysis without requiring new healthcare knowledge bases or external dependencies.