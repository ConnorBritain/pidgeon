# HIGH VALUE TODO ITEMS
## Items that MUST be implemented for production-ready HL7 system

---

## 🔴 CRITICAL: Validation Result Type Mismatch (4 errors)

### **Problem:**
Tests expect validation results to have `.IsValid` and `.Errors` properties, but are receiving `List<string>` instead.

### **Affected Files:**
- `tests/Segmint.Tests/HL7/Messages/RDEMessageTests.cs:303,304,318,319`

### **Current Error Pattern:**
```csharp
// Tests expect this to work:
var result = message.Validate();
result.IsValid.Should().BeTrue();
result.Errors.Should().BeEmpty();

// But result is currently List<string>, not ValidationResult
```

### **Implementation Required:**
1. **Create ValidationResult class:**
   ```csharp
   public class ValidationResult
   {
       public bool IsValid => !Errors.Any();
       public List<string> Errors { get; set; } = new();
       
       public static ValidationResult Success() => new();
       public static ValidationResult Failure(params string[] errors) => new() { Errors = errors.ToList() };
   }
   ```

2. **Update validation methods** to return `ValidationResult` instead of `List<string>`

3. **Location:** Likely in `Segmint.Core.Standards.HL7.v23.Messages` or validation infrastructure

### **Business Impact:**
- **CRITICAL** - Validation is core to HL7 message processing
- Without proper validation, invalid messages could be processed
- Essential for healthcare data integrity and compliance

---

## 🔴 CRITICAL: Missing DispenseInfo Type in RDSMessage (2 errors)

### **Problem:** 
`RDSMessage.DispenseInfo` type doesn't exist, breaking pharmacy workflow functionality.

### **Affected Files:**
- `tests/Segmint.Tests/Standards/HL7/v23/Workflows/WorkflowValidationTests.cs:259`

### **Current Error:**
```csharp
// This fails:
message.DispenseInfo.SomeProperty
// CS0426: The type name 'DispenseInfo' does not exist in the type 'RDSMessage'
```

### **Implementation Required:**
1. **Create DispenseInfo class:**
   ```csharp
   public class DispenseInfo
   {
       public string? DispenseId { get; set; }
       public DateTime? DispenseDate { get; set; }
       public string? Medication { get; set; }
       public decimal? Quantity { get; set; }
       public string? Units { get; set; }
       public string? PharmacyId { get; set; }
       // Add other properties based on HL7 RDS specification
   }
   ```

2. **Add to RDSMessage class:**
   ```csharp
   public class RDSMessage : HL7Message
   {
       // Existing properties...
       public DispenseInfo DispenseInfo { get; set; } = new();
   }
   ```

3. **Location:** `src/Segmint.Core/Standards/HL7/v23/Messages/RDSMessage.cs`

### **Business Impact:**
- **CRITICAL** - Required for pharmacy workflows
- RDS (Pharmacy/Treatment Dispense) messages are essential for:
  - Medication dispensing tracking
  - Pharmacy integration
  - Treatment compliance monitoring
- Without this, pharmacy workflows will not function

---

## 🟡 MEDIUM: TimestampField.Timezone Property (1 error)

### **Problem:**
Tests expect `TimestampField.Timezone` property that doesn't exist.

### **Affected Files:**
- `tests/Segmint.Tests/HL7/Types/TimestampFieldTests.cs:244`

### **Current Error:**
```csharp
// This fails:
timestampField.Timezone.Should().Be("+0500");
// CS1061: 'TimestampField' does not contain a definition for 'Timezone'
```

### **Implementation Required:**
1. **Add Timezone property to TimestampField:**
   ```csharp
   public class TimestampField : HL7Field
   {
       // Existing properties...
       
       /// <summary>
       /// Gets the timezone offset from the timestamp (e.g., "+0500", "-0800")
       /// </summary>
       public string? Timezone
       {
           get
           {
               if (string.IsNullOrEmpty(RawValue)) return null;
               
               // Extract timezone from HL7 timestamp format: YYYYMMDDHHMM[SS[.SSSS]][+/-ZZZZ]
               var match = Regex.Match(RawValue, @"[+-]\d{4}$");
               return match.Success ? match.Value : null;
           }
       }
   }
   ```

2. **Location:** `src/Segmint.Core/Standards/HL7/v23/Types/TimestampField.cs`

### **Business Impact:**
- **MEDIUM** - Important for multi-timezone healthcare systems
- Required for accurate time correlation across different facilities
- Critical for systems spanning multiple time zones

---

## 📋 IMPLEMENTATION PRIORITY

### **Phase 1 (IMMEDIATE - Required for core functionality):**
1. ✅ Validation Result Type Mismatch
2. ✅ Missing DispenseInfo Type

### **Phase 2 (NEXT - Required for production):**
1. ✅ TimestampField.Timezone Property

### **Total Effort Estimate:**
- **Phase 1**: 4-6 hours (2-3 hours each)
- **Phase 2**: 1-2 hours
- **Total**: 5-8 hours

---

## 🧪 TESTING REQUIREMENTS

After implementing these items, ensure:

1. **All existing tests pass**
2. **New validation logic is thoroughly tested**  
3. **Pharmacy workflows are validated end-to-end**
4. **Timezone handling works across different locales**

---

## 📝 NOTES

- These items were identified during comprehensive error analysis
- 42 → 7 errors achieved (83% reduction)
- Remaining 7 errors are ALL documented here
- Core API functionality is working
- These are the ONLY remaining blockers for production readiness