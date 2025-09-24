# Pidgeon Find Command Development Implementation Guide

**Version**: 1.0
**Created**: January 2025
**Status**: Development implementation guide following design specification

---

## 🎯 **Development Objectives**

Replace the current mock implementations in `FindCommand.cs` with real functionality using existing data sources:
- HL7 v2.3 scraped field definitions
- Existing vendor profiles and patterns
- Message type definitions and validation rules
- Standard reference services already in codebase

---

## 🏗️ **Implementation Architecture**

### **Core Services to Implement**

#### **1. Field Discovery Service**
**File**: `src/Pidgeon.Core/Services/Search/FieldDiscoveryService.cs`
```csharp
public interface IFieldDiscoveryService
{
    Task<FieldSearchResult> FindBySemanticPathAsync(string semanticPath);
    Task<FieldSearchResult> FindByPatternAsync(string pattern, PatternType type);
    Task<CrossStandardMapping> MapAcrossStandardsAsync(string fieldPath);
    Task<FieldSearchResult> FindBySemanticAsync(string query, string? context);
}

public class FieldDiscoveryService : IFieldDiscoveryService
{
    private readonly IStandardReferenceService _referenceService;
    private readonly IFieldMetadataService _fieldMetadata;
    private readonly IVendorProfileService _vendorProfiles;

    // Implementation uses existing services - NO new databases
}
```

#### **2. Message Search Service**
**File**: `src/Pidgeon.Core/Services/Search/MessageSearchService.cs`
```csharp
public interface IMessageSearchService
{
    Task<List<ValueLocation>> FindValueInFilesAsync(string value, string searchPath);
    Task<List<ValueLocation>> FindValueInDirectoryAsync(string value, string directory);
}

public class MessageSearchService : IMessageSearchService
{
    private readonly IHL7ParserService _hl7Parser;
    private readonly IFHIRParserService _fhirParser;

    // Scans files and parses with existing parsers
}
```

#### **3. Field Index Service**
**File**: `src/Pidgeon.Core/Services/Search/FieldIndexService.cs`
```csharp
public interface IFieldIndexService
{
    Task<List<FieldMetadata>> GetFieldsByStandardAsync(string standard);
    Task<List<FieldMetadata>> SearchFieldsAsync(string query);
    Task<List<CrossReference>> GetCrossReferencesAsync(string fieldPath);
}

public class FieldIndexService : IFieldIndexService
{
    // Pre-builds search indices from existing scraped data
    // Cached in memory for performance
}
```

---

## 📊 **Data Models Implementation**

### **Search Result Models**
**File**: `src/Pidgeon.Core/Domain/Search/FieldSearchModels.cs`
```csharp
public class FieldSearchResult
{
    public List<FieldLocation> Locations { get; init; } = new();
    public List<CrossReference> RelatedFields { get; init; } = new();
    public List<string> Suggestions { get; init; } = new();
    public SearchMetrics Metrics { get; init; } = new();
}

public class FieldLocation
{
    public string Standard { get; init; } = "";        // HL7v23, FHIR-R4, NCPDP
    public string Path { get; init; } = "";            // PID.5, Patient.name, etc
    public string Description { get; init; } = "";     // From scraped metadata
    public string MessageType { get; init; } = "";     // ADT^A01, Patient, etc
    public string DataType { get; init; } = "";        // From existing field defs
    public string Usage { get; init; } = "";           // Required/Optional
    public List<string> Examples { get; init; } = new();
}

public class CrossStandardMapping
{
    public FieldLocation SourceField { get; init; } = new();
    public List<MappedField> TargetFields { get; init; } = new();
    public MappingQuality Quality { get; init; }
}

public class MappedField : FieldLocation
{
    public MappingType Type { get; init; }    // Direct, Filtered, Conceptual, None
    public double Confidence { get; init; }
    public string Notes { get; init; } = "";
}

public enum MappingType { Direct, Filtered, Conceptual, None }
public enum MappingQuality { Excellent, Good, Fair, Poor }
```

---

## 🔧 **Implementation Steps**

### **Phase 1: Core Infrastructure (Week 1)**

#### **Day 1-2: Service Registration & Interfaces**
```csharp
// In Program.cs or service registration
services.AddScoped<IFieldDiscoveryService, FieldDiscoveryService>();
services.AddScoped<IMessageSearchService, MessageSearchService>();
services.AddScoped<IFieldIndexService, FieldIndexService>();
```

#### **Day 3-4: Field Index Building**
```csharp
public class FieldIndexService : IFieldIndexService
{
    private readonly Dictionary<string, List<FieldMetadata>> _semanticIndex;
    private readonly Dictionary<string, List<FieldMetadata>> _keywordIndex;
    private readonly Dictionary<string, CrossStandardMapping> _mappingIndex;

    public FieldIndexService(IStandardReferenceService referenceService)
    {
        // Build indices from existing scraped data
        _semanticIndex = BuildSemanticIndex(referenceService);
        _keywordIndex = BuildKeywordIndex(referenceService);
        _mappingIndex = BuildMappingIndex(referenceService);
    }

    private Dictionary<string, List<FieldMetadata>> BuildSemanticIndex(IStandardReferenceService service)
    {
        var index = new Dictionary<string, List<FieldMetadata>>();

        // Use existing HL7 scraped data
        var hl7Fields = service.GetAllHL7Fields(); // Uses existing data

        foreach (var field in hl7Fields)
        {
            // Map semantic paths: patient.mrn -> PID.3
            var semanticPaths = ExtractSemanticPaths(field);
            foreach (var path in semanticPaths)
            {
                if (!index.ContainsKey(path))
                    index[path] = new List<FieldMetadata>();

                index[path].Add(new FieldMetadata
                {
                    Standard = "HL7v23",
                    Path = field.Path,
                    Description = field.Description,
                    DataType = field.DataType,
                    Usage = field.Usage
                });
            }
        }

        return index;
    }
}
```

#### **Day 5: Auto-Mode Detection Logic**
```csharp
private SearchMode DetermineSearchMode(string query, bool isField, bool isValue, string? pattern, bool isMap, bool isSemantic)
{
    // Existing logic in FindCommand.cs is already good
    // Just needs to wire to real services instead of mocks
}
```

### **Phase 2: Search Implementation (Week 2)**

#### **Day 1-2: Field Discovery Mode**
```csharp
private async Task<List<FindResult>> FindFieldLocationsAsync(string fieldPath, string? standard, string? messageType, CancellationToken cancellationToken)
{
    var results = new List<FindResult>();

    // Use real field discovery service instead of mocks
    var searchResult = await _fieldDiscoveryService.FindBySemanticPathAsync(fieldPath);

    foreach (var location in searchResult.Locations)
    {
        // Filter by standard if specified
        if (!string.IsNullOrEmpty(standard) &&
            !location.Standard.Equals(standard, StringComparison.OrdinalIgnoreCase))
            continue;

        results.Add(new FindResult
        {
            Standard = location.Standard,
            Location = location.Path,
            Description = location.Description,
            MessageType = location.MessageType,
            FieldName = ExtractFieldName(location.Description),
            DataType = location.DataType,
            Usage = location.Usage,
            Example = location.Examples.FirstOrDefault() ?? ""
        });
    }

    return results;
}
```

#### **Day 3-4: Value Search Mode**
```csharp
private async Task<List<FindResult>> FindValueLocationsAsync(string value, string? searchPath, string? standard, CancellationToken cancellationToken)
{
    var results = new List<FindResult>();

    // Use real message search service
    List<ValueLocation> locations;

    if (!string.IsNullOrEmpty(searchPath))
    {
        if (Directory.Exists(searchPath))
            locations = await _messageSearchService.FindValueInDirectoryAsync(value, searchPath);
        else
            locations = await _messageSearchService.FindValueInFilesAsync(value, searchPath);
    }
    else
    {
        // Search in current directory
        locations = await _messageSearchService.FindValueInDirectoryAsync(value, Environment.CurrentDirectory);
    }

    foreach (var location in locations)
    {
        results.Add(new FindResult
        {
            Standard = location.Standard,
            Location = $"{location.FileName}:{location.FieldPath}",
            Description = $"Found value '{value}' in {location.FieldDescription}",
            MessageType = location.MessageType,
            FieldName = location.FieldDescription,
            DataType = location.DataType,
            Usage = "Found",
            Example = value
        });
    }

    return results;
}
```

#### **Day 5: Pattern Matching Mode**
```csharp
private async Task<List<FindResult>> FindByPatternAsync(string pattern, string? standard, CancellationToken cancellationToken)
{
    var results = new List<FindResult>();

    // Use field index service for pattern matching
    var searchResult = await _fieldDiscoveryService.FindByPatternAsync(pattern, PatternType.Wildcard);

    foreach (var location in searchResult.Locations.Where(l => MatchesStandardFilter(l, standard)))
    {
        results.Add(new FindResult
        {
            Standard = location.Standard,
            Location = location.Path,
            Description = location.Description,
            MessageType = location.MessageType,
            FieldName = ExtractFieldName(location.Description),
            DataType = location.DataType,
            Usage = location.Usage
        });
    }

    return results;
}
```

### **Phase 3: Advanced Features (Week 3)**

#### **Day 1-3: Cross-Standard Mapping**
```csharp
private async Task<List<FindResult>> FindCrossStandardMappingAsync(string fieldPath, CancellationToken cancellationToken)
{
    var results = new List<FindResult>();

    // Use real cross-standard mapping
    var mapping = await _fieldDiscoveryService.MapAcrossStandardsAsync(fieldPath);

    // Add source field
    results.Add(CreateFindResultFromFieldLocation(mapping.SourceField, "Source"));

    // Add mapped fields
    foreach (var mappedField in mapping.TargetFields)
    {
        var result = CreateFindResultFromFieldLocation(mappedField, mappedField.Type.ToString());
        result.MappingType = $"{mappedField.Type} ({mappedField.Confidence:P0} confidence)";
        results.Add(result);
    }

    return results;
}
```

#### **Day 4-5: Semantic Search**
```csharp
private async Task<List<FindResult>> FindSemanticMatchesAsync(string query, string? standard, string? messageType, CancellationToken cancellationToken)
{
    var results = new List<FindResult>();

    // Use real semantic search
    var searchResult = await _fieldDiscoveryService.FindBySemanticAsync(query, messageType);

    foreach (var location in searchResult.Locations)
    {
        if (!string.IsNullOrEmpty(standard) &&
            !location.Standard.Equals(standard, StringComparison.OrdinalIgnoreCase))
            continue;

        results.Add(new FindResult
        {
            Standard = location.Standard,
            Location = location.Path,
            Description = location.Description,
            MessageType = location.MessageType,
            FieldName = ExtractFieldName(location.Description),
            DataType = location.DataType,
            Usage = location.Usage,
            SemanticMatch = $"Matches '{query}' concept"
        });
    }

    return results;
}
```

### **Phase 4: Performance & Polish (Week 4)**

#### **Caching Implementation**
```csharp
public class CachedFieldDiscoveryService : IFieldDiscoveryService
{
    private readonly IFieldDiscoveryService _inner;
    private readonly IMemoryCache _cache;

    public async Task<FieldSearchResult> FindBySemanticPathAsync(string semanticPath)
    {
        return await _cache.GetOrCreateAsync($"semantic:{semanticPath}", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15);
            return await _inner.FindBySemanticPathAsync(semanticPath);
        });
    }
}
```

---

## 🧪 **Testing Strategy**

### **Unit Tests**
**File**: `tests/Pidgeon.Core.Tests/Services/Search/FieldDiscoveryServiceTests.cs`
```csharp
public class FieldDiscoveryServiceTests
{
    [Fact]
    public async Task FindBySemanticPathAsync_PatientMrn_ReturnsHL7AndFHIRLocations()
    {
        // Arrange
        var service = CreateFieldDiscoveryService();

        // Act
        var result = await service.FindBySemanticPathAsync("patient.mrn");

        // Assert
        result.Locations.Should().NotBeEmpty();
        result.Locations.Should().Contain(l => l.Path == "PID.3" && l.Standard == "HL7v23");
        result.Locations.Should().Contain(l => l.Path.Contains("Patient.identifier") && l.Standard == "FHIR-R4");
    }

    [Fact]
    public async Task FindValueInFilesAsync_ExistingValue_ReturnsCorrectLocation()
    {
        // Test with sample HL7 files containing known values
    }
}
```

### **Integration Tests**
**File**: `tests/Pidgeon.CLI.Tests/Commands/FindCommandIntegrationTests.cs`
```csharp
public class FindCommandIntegrationTests
{
    [Fact]
    public async Task FindCommand_PatientMrn_ReturnsExpectedOutput()
    {
        // Test full command execution with real services
        var result = await ExecuteCommand("find", "patient.mrn");
        result.ExitCode.Should().Be(0);
        result.Output.Should().Contain("PID.3");
        result.Output.Should().Contain("Patient Identifier");
    }
}
```

---

## ✅ **Acceptance Criteria**

### **Functional Requirements**
- [ ] **Field discovery** returns real HL7 field locations using existing scraped data
- [ ] **Value search** scans files and returns accurate locations
- [ ] **Pattern matching** supports wildcards and basic regex
- [ ] **Cross-standard mapping** shows HL7-to-FHIR equivalences
- [ ] **Semantic search** finds related fields by healthcare concept
- [ ] **Auto-mode detection** works without explicit flags
- [ ] **Performance** under 500ms for cached queries

### **Output Requirements**
- [ ] **Clear field names** instead of just paths (e.g., "Patient Name" not "PID.5")
- [ ] **Standard identification** shows which healthcare standard
- [ ] **Context information** includes message types and usage
- [ ] **Smart suggestions** when no results found
- [ ] **Export formats** support JSON and CSV output

---

## 🚀 **Deployment Checklist**

- [ ] All existing tests pass (no regressions)
- [ ] New services registered in DI container
- [ ] Command help text updated with real examples
- [ ] Performance benchmarks meet targets
- [ ] Integration with existing StandardReferenceService verified
- [ ] Memory usage optimized (indices cached efficiently)

This implementation guide transforms the Find command from mock implementations to real functionality using existing data sources, following the established architectural patterns in the codebase.