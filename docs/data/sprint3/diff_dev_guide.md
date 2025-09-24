# Pidgeon Smart Diff Command Development Implementation Guide

**Version**: 1.0
**Created**: January 2025
**Status**: Development implementation guide following smart diff design

---

## 🎯 **Development Objectives**

Enhance the existing `DiffCommand.cs` with smart field-level analysis using existing data sources:
- HL7 v2.3 scraped field definitions for field names and descriptions
- Existing validation services for format checking
- Vendor profile services for pattern detection
- Message parsing services for field extraction

**Key Goal**: Transform "PID.5 changed from X to Y" into "Patient Name (PID.5) changed from 'John Doe' to 'Jane Doe' - demographic field, both values valid"

---

## 🏗️ **Implementation Architecture**

### **Core Services to Implement**

#### **1. Smart Field Analyzer**
**File**: `src/Pidgeon.Core/Services/Diff/SmartFieldAnalyzer.cs`
```csharp
public interface ISmartFieldAnalyzer
{
    Task<SmartFieldChange> AnalyzeChangeAsync(string fieldPath, string oldValue, string newValue);
    Task<List<SmartFieldChange>> AnalyzeChangesAsync(List<FieldDifference> differences);
    FieldCategory CategorizeField(string fieldPath);
}

public class SmartFieldAnalyzer : ISmartFieldAnalyzer
{
    private readonly IFieldMetadataService _fieldMetadata;    // Existing - uses scraped HL7 data
    private readonly IValidationService _validationService;  // Existing - validates field formats
    private readonly ILogger<SmartFieldAnalyzer> _logger;

    public SmartFieldAnalyzer(
        IFieldMetadataService fieldMetadata,
        IValidationService validationService,
        ILogger<SmartFieldAnalyzer> logger)
    {
        _fieldMetadata = fieldMetadata;
        _validationService = validationService;
        _logger = logger;
    }

    public async Task<SmartFieldChange> AnalyzeChangeAsync(string fieldPath, string oldValue, string newValue)
    {
        // Get field metadata from existing scraped data
        var metadata = await _fieldMetadata.GetFieldInfoAsync(fieldPath);
        var category = CategorizeField(fieldPath);

        // Validate both values using existing validation service
        var oldValidation = await _validationService.ValidateFieldValueAsync(fieldPath, oldValue);
        var newValidation = await _validationService.ValidateFieldValueAsync(fieldPath, newValue);

        var formatStatus = DetermineFormatStatus(oldValidation, newValidation);

        return new SmartFieldChange
        {
            FieldPath = fieldPath,
            FieldName = metadata?.Name ?? ExtractFieldNameFromPath(fieldPath),
            Category = category,
            OldValue = oldValue,
            NewValue = newValue,
            FormatStatus = formatStatus,
            DataType = metadata?.DataType ?? "Unknown",
            Impact = DetermineImpact(category, oldValidation, newValidation),
            Description = BuildChangeDescription(metadata, oldValue, newValue, formatStatus)
        };
    }

    public FieldCategory CategorizeField(string fieldPath)
    {
        // Use segment-based categorization from existing HL7 knowledge
        var segment = ExtractSegment(fieldPath); // PID.5 -> PID

        return segment switch
        {
            "PID" => FieldCategory.Demographics,        // Patient demographics
            "AL1" => FieldCategory.Critical,            // Allergies - high impact
            "DG1" => FieldCategory.Clinical,            // Diagnosis
            "RXE" or "RXA" or "RXO" => FieldCategory.Clinical, // Medications
            "OBX" or "OBR" => FieldCategory.Clinical,   // Observations/Results
            "IN1" or "IN2" => FieldCategory.Administrative, // Insurance
            "PV1" or "PV2" => FieldCategory.Administrative, // Visit info
            "MSH" or "EVN" => FieldCategory.System,     // Message control
            _ => FieldCategory.Administrative           // Default
        };
    }
}
```

#### **2. Vendor Pattern Detector**
**File**: `src/Pidgeon.Core/Services/Diff/VendorPatternDetector.cs`
```csharp
public interface IVendorPatternDetector
{
    Task<VendorDriftAnalysis> DetectDriftAsync(List<SmartFieldChange> changes);
    Task<VendorInfo> DetectVendorAsync(string value, string fieldPath);
}

public class VendorPatternDetector : IVendorPatternDetector
{
    private readonly IVendorProfileService _vendorProfiles; // Existing vendor pattern service

    public async Task<VendorDriftAnalysis> DetectDriftAsync(List<SmartFieldChange> changes)
    {
        var patterns = new List<string>();
        var vendorChanges = new Dictionary<string, int>();

        foreach (var change in changes)
        {
            var oldVendor = await DetectVendorAsync(change.OldValue, change.FieldPath);
            var newVendor = await DetectVendorAsync(change.NewValue, change.FieldPath);

            if (oldVendor.Name != newVendor.Name && oldVendor.Name != "Unknown" && newVendor.Name != "Unknown")
            {
                var pattern = $"{change.FieldName}: {oldVendor.Name} → {newVendor.Name} format";
                patterns.Add(pattern);

                var changeKey = $"{oldVendor.Name}→{newVendor.Name}";
                vendorChanges[changeKey] = vendorChanges.GetValueOrDefault(changeKey, 0) + 1;
            }
        }

        return new VendorDriftAnalysis
        {
            DetectedPatterns = patterns,
            VendorChanges = vendorChanges,
            SystemicDrift = DetectSystemicDrift(vendorChanges)
        };
    }

    public async Task<VendorInfo> DetectVendorAsync(string value, string fieldPath)
    {
        // Use existing vendor profile detection
        var profile = await _vendorProfiles.DetectVendorProfileAsync(value, fieldPath);
        return new VendorInfo
        {
            Name = profile?.VendorName ?? "Unknown",
            Confidence = profile?.Confidence ?? 0.0,
            Patterns = profile?.DetectedPatterns ?? new List<string>()
        };
    }
}
```

#### **3. Timestamp Pattern Analyzer**
**File**: `src/Pidgeon.Core/Services/Diff/TimestampAnalyzer.cs`
```csharp
public interface ITimestampAnalyzer
{
    TimestampPattern AnalyzeTimestamps(List<SmartFieldChange> changes);
    bool IsTimestampField(string fieldPath);
}

public class TimestampAnalyzer : ITimestampAnalyzer
{
    private static readonly string[] TimestampFields =
    {
        "MSH.7",   // Date/Time of Message
        "PID.7",   // Date/Time of Birth
        "PV1.44",  // Admit Date/Time
        "PV1.45",  // Discharge Date/Time
        "OBR.7",   // Observation Date/Time
        "EVN.2"    // Recorded Date/Time
    };

    public TimestampPattern AnalyzeTimestamps(List<SmartFieldChange> changes)
    {
        var timestampChanges = changes
            .Where(c => IsTimestampField(c.FieldPath))
            .Where(c => TryParseTimestamp(c.OldValue, out var oldTime) &&
                       TryParseTimestamp(c.NewValue, out var newTime))
            .Select(c => new TimestampChange
            {
                FieldPath = c.FieldPath,
                FieldName = c.FieldName,
                OldTime = ParseTimestamp(c.OldValue),
                NewTime = ParseTimestamp(c.NewValue),
                Offset = ParseTimestamp(c.NewValue) - ParseTimestamp(c.OldValue)
            })
            .ToList();

        if (timestampChanges.Count < 2)
            return TimestampPattern.None;

        // Check for consistent offset
        var firstOffset = timestampChanges.First().Offset;
        if (timestampChanges.All(t => Math.Abs((t.Offset - firstOffset).TotalMinutes) < 1))
        {
            return new TimestampPattern
            {
                Type = PatternType.ConsistentShift,
                Offset = firstOffset,
                AffectedFields = timestampChanges.Count,
                Description = FormatOffsetDescription(firstOffset),
                Changes = timestampChanges
            };
        }

        return TimestampPattern.None;
    }

    public bool IsTimestampField(string fieldPath)
    {
        return TimestampFields.Contains(fieldPath, StringComparer.OrdinalIgnoreCase) ||
               fieldPath.ToLower().Contains("date") ||
               fieldPath.ToLower().Contains("time");
    }
}
```

#### **4. Smart Diff Service**
**File**: `src/Pidgeon.Core/Services/Diff/SmartDiffService.cs`
```csharp
public interface ISmartDiffService : IMessageDiffService
{
    Task<SmartDiffResult> AnalyzeWithContextAsync(string leftMessage, string rightMessage, DiffOptions options);
}

public class SmartDiffService : ISmartDiffService
{
    private readonly IMessageDiffService _baseDiffService;      // Existing basic diff
    private readonly ISmartFieldAnalyzer _fieldAnalyzer;
    private readonly IVendorPatternDetector _vendorDetector;
    private readonly ITimestampAnalyzer _timestampAnalyzer;

    public async Task<SmartDiffResult> AnalyzeWithContextAsync(
        string leftMessage,
        string rightMessage,
        DiffOptions options)
    {
        // Use existing diff service for basic comparison
        var basicDiff = await _baseDiffService.CompareMessagesAsync(leftMessage, rightMessage);

        // Add smart analysis
        var smartFieldChanges = await _fieldAnalyzer.AnalyzeChangesAsync(basicDiff.Differences);
        var vendorAnalysis = await _vendorDetector.DetectDriftAsync(smartFieldChanges);
        var timestampPattern = _timestampAnalyzer.AnalyzeTimestamps(smartFieldChanges);

        // Build validation summary using existing validation
        var validationSummary = await BuildValidationSummaryAsync(smartFieldChanges);

        return new SmartDiffResult
        {
            // Include basic diff properties
            LeftPath = basicDiff.LeftPath,
            RightPath = basicDiff.RightPath,
            IsIdentical = basicDiff.IsIdentical,
            Differences = basicDiff.Differences,

            // Add smart analysis
            FieldChanges = smartFieldChanges,
            VendorAnalysis = vendorAnalysis,
            TimestampPattern = timestampPattern,
            ValidationSummary = validationSummary,
            Statistics = BuildStatistics(smartFieldChanges)
        };
    }
}
```

---

## 📊 **Data Models Implementation**

### **Smart Analysis Models**
**File**: `src/Pidgeon.Core/Domain/Diff/SmartDiffModels.cs`
```csharp
public class SmartDiffResult : MessageDiff
{
    public List<SmartFieldChange> FieldChanges { get; init; } = new();
    public VendorDriftAnalysis VendorAnalysis { get; init; } = new();
    public TimestampPattern TimestampPattern { get; init; } = TimestampPattern.None;
    public ValidationSummary ValidationSummary { get; init; } = new();
    public DiffStatistics Statistics { get; init; } = new();
}

public class SmartFieldChange
{
    public string FieldPath { get; init; } = "";           // PID.5
    public string FieldName { get; init; } = "";           // Patient Name
    public FieldCategory Category { get; init; }           // Demographics/Clinical/etc
    public string OldValue { get; init; } = "";
    public string NewValue { get; init; } = "";
    public ValidationStatus FormatStatus { get; init; }    // BothValid/NowInvalid/etc
    public string DataType { get; init; } = "";            // XPN, TS, etc
    public string Impact { get; init; } = "";              // Based on category + validation
    public string Description { get; init; } = "";        // Human-readable summary
}

public class VendorDriftAnalysis
{
    public List<string> DetectedPatterns { get; init; } = new();
    public Dictionary<string, int> VendorChanges { get; init; } = new();
    public bool SystemicDrift { get; init; }
    public string Summary { get; init; } = "";
}

public class TimestampPattern
{
    public PatternType Type { get; init; }
    public TimeSpan Offset { get; init; }
    public int AffectedFields { get; init; }
    public string Description { get; init; } = "";
    public List<TimestampChange> Changes { get; init; } = new();

    public static TimestampPattern None => new() { Type = PatternType.None };
}

public enum FieldCategory
{
    System,           // MSH, message control
    Demographics,     // PID patient info
    Clinical,         // DG1, RXE, OBX clinical data
    Critical,         // AL1 allergies, safety info
    Administrative    // PV1, IN1 workflow/billing
}

public enum ValidationStatus
{
    BothValid,
    BothInvalid,
    NowInvalid,
    NowValid,
    Unknown
}

public enum PatternType
{
    None,
    ConsistentShift,
    FormatChange,
    VendorMigration
}
```

---

## 🔧 **Implementation Steps**

### **Phase 1: Smart Field Analysis (Week 1)**

#### **Day 1-2: Service Registration**
```csharp
// In Program.cs or service registration
services.AddScoped<ISmartFieldAnalyzer, SmartFieldAnalyzer>();
services.AddScoped<IVendorPatternDetector, VendorPatternDetector>();
services.AddScoped<ITimestampAnalyzer, TimestampAnalyzer>();
services.AddScoped<ISmartDiffService, SmartDiffService>();
```

#### **Day 3-4: Field Categorization**
```csharp
public FieldCategory CategorizeField(string fieldPath)
{
    var segment = ExtractSegment(fieldPath); // PID.5 -> PID

    // Use existing HL7 knowledge - no new databases needed
    var categoryMap = new Dictionary<string, FieldCategory>
    {
        ["MSH"] = FieldCategory.System,
        ["EVN"] = FieldCategory.System,
        ["PID"] = FieldCategory.Demographics,
        ["AL1"] = FieldCategory.Critical,         // Allergy - high impact
        ["DG1"] = FieldCategory.Clinical,         // Diagnosis
        ["PR1"] = FieldCategory.Clinical,         // Procedure
        ["RXE"] = FieldCategory.Clinical,         // Pharmacy/Medication Encoded Order
        ["RXA"] = FieldCategory.Clinical,         // Pharmacy/Treatment Administration
        ["RXO"] = FieldCategory.Clinical,         // Pharmacy/Treatment Order
        ["OBX"] = FieldCategory.Clinical,         // Observation/Result
        ["OBR"] = FieldCategory.Clinical,         // Observation Request
        ["PV1"] = FieldCategory.Administrative,   // Patient Visit
        ["PV2"] = FieldCategory.Administrative,   // Patient Visit Additional
        ["IN1"] = FieldCategory.Administrative,   // Insurance
        ["IN2"] = FieldCategory.Administrative,   // Insurance Additional
    };

    return categoryMap.GetValueOrDefault(segment, FieldCategory.Administrative);
}
```

#### **Day 5: Enhanced Output Integration**
```csharp
// Modify existing DiffCommand.cs to use SmartDiffService
private async Task<int> HandleDiffAnalysis(/* parameters */)
{
    // Use SmartDiffService instead of basic diff
    var smartResult = await _smartDiffService.AnalyzeWithContextAsync(leftMessage, rightMessage, options);

    // Enhanced output
    DisplaySmartSummary(smartResult);
    DisplayFieldChanges(smartResult.FieldChanges);
    DisplayPatternAnalysis(smartResult.VendorAnalysis, smartResult.TimestampPattern);
    DisplayValidationStatus(smartResult.ValidationSummary);

    return 0;
}
```

### **Phase 2: Pattern Detection (Week 2)**

#### **Day 1-3: Vendor Pattern Integration**
```csharp
public async Task<VendorDriftAnalysis> DetectDriftAsync(List<SmartFieldChange> changes)
{
    var patterns = new List<string>();

    foreach (var change in changes)
    {
        // Use existing vendor profile service - no new detection needed
        var oldProfile = await _vendorProfiles.DetectVendorProfileAsync(change.OldValue, change.FieldPath);
        var newProfile = await _vendorProfiles.DetectVendorProfileAsync(change.NewValue, change.FieldPath);

        if (oldProfile?.VendorName != newProfile?.VendorName &&
            oldProfile != null && newProfile != null)
        {
            patterns.Add($"{change.FieldName}: {oldProfile.VendorName} → {newProfile.VendorName} format detected");
        }
    }

    return new VendorDriftAnalysis
    {
        DetectedPatterns = patterns,
        SystemicDrift = patterns.Count >= 3 // Threshold for systemic change
    };
}
```

#### **Day 4-5: Timestamp Analysis**
```csharp
private string FormatOffsetDescription(TimeSpan offset)
{
    if (offset.TotalHours >= 1)
        return $"{offset.TotalHours:+#;-#;0}h shift";
    else if (offset.TotalMinutes >= 1)
        return $"{offset.TotalMinutes:+#;-#;0}m shift";
    else
        return $"{offset.TotalSeconds:+#;-#;0}s shift";
}
```

### **Phase 3: Enhanced Output (Week 3)**

#### **Day 1-2: Smart Summary Display**
```csharp
private void DisplaySmartSummary(SmartDiffResult result)
{
    Console.WriteLine("🔍 Smart Message Diff Analysis");
    Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
    Console.WriteLine($"Files: {Path.GetFileName(result.LeftPath)} → {Path.GetFileName(result.RightPath)}");
    Console.WriteLine($"Total Changes: {result.FieldChanges.Count} fields");
    Console.WriteLine();

    // Category breakdown
    var categories = result.FieldChanges.GroupBy(c => c.Category);
    Console.WriteLine("📊 Change Categories:");
    foreach (var category in categories)
    {
        var icon = GetCategoryIcon(category.Key);
        var examples = string.Join(", ", category.Take(3).Select(c => c.FieldName));
        Console.WriteLine($"  {icon} {category.Key}: {category.Count()} fields  ({examples})");
    }
    Console.WriteLine();

    // Pattern detection summary
    if (result.TimestampPattern.Type != PatternType.None)
    {
        Console.WriteLine("🕐 Pattern Detection:");
        Console.WriteLine($"  ✓ {result.TimestampPattern.Description} ({result.TimestampPattern.AffectedFields} fields)");
    }

    if (result.VendorAnalysis.DetectedPatterns.Any())
    {
        foreach (var pattern in result.VendorAnalysis.DetectedPatterns.Take(2))
        {
            Console.WriteLine($"  ⚠️ {pattern}");
        }
    }
    Console.WriteLine();
}
```

#### **Day 3-4: Field-Level Details**
```csharp
private void DisplayFieldChanges(List<SmartFieldChange> changes)
{
    Console.WriteLine("📝 FIELD-LEVEL CHANGES");
    Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
    Console.WriteLine();

    var groupedByCategory = changes.GroupBy(c => c.Category).OrderByDescending(g => GetCategoryPriority(g.Key));

    foreach (var categoryGroup in groupedByCategory)
    {
        var icon = GetCategoryIcon(categoryGroup.Key);
        var impact = GetCategoryImpact(categoryGroup.Key);

        Console.WriteLine($"{icon} {categoryGroup.Key} ({impact} Impact)");

        foreach (var change in categoryGroup.Take(5)) // Limit per category
        {
            Console.WriteLine($"  {change.FieldName} ({change.FieldPath}): \"{change.OldValue}\" → \"{change.NewValue}\"");
            Console.WriteLine($"    Data Type: {change.DataType}");
            Console.WriteLine($"    Status: {GetStatusIcon(change.FormatStatus)} {change.Description}");

            if (!string.IsNullOrEmpty(change.Impact))
            {
                Console.WriteLine($"    Impact: {change.Impact}");
            }
            Console.WriteLine();
        }
    }
}
```

#### **Day 5: Validation Integration Display**
```csharp
private void DisplayValidationStatus(ValidationSummary summary)
{
    Console.WriteLine("✅ VALIDATION STATUS");
    Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
    Console.WriteLine("Using existing validation rules:");
    Console.WriteLine();

    Console.WriteLine($"✅ {summary.ValidChanges} of {summary.TotalChanges} changes maintain valid formats");

    if (summary.InvalidChanges > 0)
    {
        Console.WriteLine($"❌ {summary.InvalidChanges} field(s) now invalid:");

        foreach (var invalidChange in summary.InvalidFields.Take(3))
        {
            Console.WriteLine($"   - {invalidChange.FieldName} ({invalidChange.FieldPath})");
            Console.WriteLine($"     Old: \"{invalidChange.OldValue}\" (valid)");
            Console.WriteLine($"     New: \"{invalidChange.NewValue}\" (invalid: {invalidChange.ValidationError})");
        }
    }

    if (summary.Recommendations.Any())
    {
        Console.WriteLine();
        Console.WriteLine("💡 Recommendations:");
        foreach (var recommendation in summary.Recommendations.Take(3))
        {
            Console.WriteLine($"  • {recommendation}");
        }
    }
    Console.WriteLine();
}
```

### **Phase 4: Integration & Testing (Week 4)**

#### **Integration with Existing DiffCommand**
```csharp
// Update constructor to inject SmartDiffService
public DiffCommand(
    ILogger<DiffCommand> logger,
    IMessageDiffService messageDiffService,
    ISmartDiffService smartDiffService,  // Add this
    FirstTimeUserService firstTimeUserService)
    : base(logger, firstTimeUserService)
{
    _messageDiffService = messageDiffService;
    _smartDiffService = smartDiffService;     // Add this
    _firstTimeUserService = firstTimeUserService;
}
```

---

## 🧪 **Testing Strategy**

### **Unit Tests**
**File**: `tests/Pidgeon.Core.Tests/Services/Diff/SmartFieldAnalyzerTests.cs`
```csharp
public class SmartFieldAnalyzerTests
{
    [Theory]
    [InlineData("PID.5", FieldCategory.Demographics)]
    [InlineData("AL1.3", FieldCategory.Critical)]
    [InlineData("DG1.4", FieldCategory.Clinical)]
    [InlineData("MSH.7", FieldCategory.System)]
    public void CategorizeField_ValidSegment_ReturnsExpectedCategory(string fieldPath, FieldCategory expected)
    {
        // Test field categorization using existing HL7 knowledge
        var analyzer = CreateSmartFieldAnalyzer();
        var result = analyzer.CategorizeField(fieldPath);
        result.Should().Be(expected);
    }

    [Fact]
    public async Task AnalyzeChangeAsync_PatientNameChange_ReturnsSmartFieldChange()
    {
        // Test with real metadata service returning HL7 field info
        var analyzer = CreateSmartFieldAnalyzer();

        var result = await analyzer.AnalyzeChangeAsync("PID.5", "SMITH^JOHN", "SMITH^JANE");

        result.FieldPath.Should().Be("PID.5");
        result.FieldName.Should().Be("Patient Name");
        result.Category.Should().Be(FieldCategory.Demographics);
        result.FormatStatus.Should().Be(ValidationStatus.BothValid);
    }
}
```

### **Integration Tests**
**File**: `tests/Pidgeon.CLI.Tests/Commands/DiffCommandSmartTests.cs`
```csharp
public class DiffCommandSmartTests
{
    [Fact]
    public async Task DiffCommand_WithSmartAnalysis_ShowsFieldNames()
    {
        // Create sample HL7 messages with known differences
        var leftMessage = CreateSampleADTMessage("SMITH^JOHN", "19850315");
        var rightMessage = CreateSampleADTMessage("SMITH^JANE", "19840315");

        var result = await ExecuteCommand("diff", "--left", leftMessage, "--right", rightMessage);

        // Verify smart analysis output
        result.ExitCode.Should().Be(0);
        result.Output.Should().Contain("Patient Name (PID.5)");
        result.Output.Should().Contain("Demographics");
        result.Output.Should().Contain("Both values valid");
    }

    [Fact]
    public async Task DiffCommand_WithTimestampShift_DetectsPattern()
    {
        // Test systematic timestamp shifting detection
        var leftMessage = CreateMessageWithTimestamps("202501151000", "202501151200");
        var rightMessage = CreateMessageWithTimestamps("202501151200", "202501151400"); // +2h shift

        var result = await ExecuteCommand("diff", "--left", leftMessage, "--right", rightMessage);

        result.Output.Should().Contain("timestamps shifted by +2h");
        result.Output.Should().Contain("Pattern Detection");
    }
}
```

---

## ✅ **Acceptance Criteria**

### **Smart Analysis Requirements**
- [ ] **Field names displayed** instead of just paths (e.g., "Patient Name" not "PID.5")
- [ ] **Category classification** working for common HL7 segments
- [ ] **Format validation** integrated with existing validation service
- [ ] **Vendor pattern detection** using existing vendor profiles
- [ ] **Timestamp pattern analysis** detects systematic shifts
- [ ] **Performance** under 2 seconds for typical message pairs

### **Output Quality Requirements**
- [ ] **Clear categorization** by impact level (System/Demographics/Clinical/Critical/Administrative)
- [ ] **Validation status** shows which changes break existing rules
- [ ] **Pattern summaries** identify systematic vs individual changes
- [ ] **Professional formatting** suitable for technical and business review
- [ ] **Backward compatibility** with existing diff command usage

---

## 🚀 **Deployment Checklist**

- [ ] All existing diff functionality preserved (no breaking changes)
- [ ] Smart services registered in DI container
- [ ] Existing validation and vendor services integrated
- [ ] Performance benchmarks meet targets (<2s for typical diffs)
- [ ] Command help updated with smart analysis features
- [ ] Memory usage optimized (no data leaks in analysis)

This implementation enhances the existing diff command with smart field-level analysis using only existing data sources and services, providing meaningful context without requiring new healthcare databases or external dependencies.