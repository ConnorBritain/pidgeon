# Pidgeon Find Command Design Specification

**Version**: 1.0
**Created**: January 2025
**Sprint**: Sprint 3 - Final Features
**Status**: Design specification for implementation

---

## 🎯 **Executive Summary**

The `pidgeon find` command will be Pidgeon's **bidirectional search engine** for healthcare message discovery and field location, enabling users to search for fields by semantic meaning, locate values across multiple files, and discover cross-standard field mappings.

**Key Value**: Transforms healthcare standards navigation from "where is this field?" into "search for what you need" with intelligent discovery and cross-standard mapping.

---

## 🔍 **Search Modes Deep Dive**

### **1. Field Discovery Mode**
**Purpose**: Find where specific clinical concepts live across standards
**Input**: Semantic field names or paths
**Output**: Technical field locations with context

```bash
# Find patient medical record number locations
pidgeon find patient.mrn
# Output:
#   HL7v2.3: PID.3 (Patient Identifier List)
#   FHIR R4: Patient.identifier[?type.coding.code='MR']
#   NCPDP: PatientId (Prescription transactions)

# Find all medication-related fields
pidgeon find --field medication
# Output:
#   HL7v2.3: RXE.2 (Give Code), RXA.5 (Administered Code)
#   FHIR R4: MedicationRequest.medicationCodeableConcept
#   NCPDP: DrugCoveredId, GenericProductIdentifier
```

### **2. Value Search Mode**
**Purpose**: Locate specific values within message files
**Input**: Actual data values
**Output**: File locations where values appear

```bash
# Find where patient ID "M123456" appears
pidgeon find "M123456" --in ./samples/
# Output:
#   sample_adt.hl7:PID.3.1 (Patient ID)
#   patient_bundle.json:Patient.identifier[0].value
#   prescription.xml:PatientId

# Find all files containing a specific date
pidgeon find "2024-01-15" --value
# Output:
#   msg1.hl7:MSH.7 (Date/Time of Message)
#   msg2.hl7:PID.7 (Date/Time of Birth)
#   encounter.json:Encounter.period.start
```

### **3. Pattern Matching Mode**
**Purpose**: Wildcard and regex-based field discovery
**Input**: Patterns with wildcards or regex
**Output**: All matching field paths

```bash
# Find all PID segment fields
pidgeon find --pattern "PID.*"
# Output:
#   PID.1 (Set ID - PID)
#   PID.3 (Patient Identifier List)
#   PID.5 (Patient Name)
#   ... (all PID fields)

# Find all date/time fields across standards
pidgeon find --pattern "*date*|*time*" --regex
```

### **4. Cross-Standard Mapping Mode**
**Purpose**: Show equivalent fields across different standards
**Input**: Field from any standard
**Output**: Equivalent fields in other standards

```bash
# Map encounter location across standards
pidgeon find --map PV1.3
# Output:
#   HL7v2.3: PV1.3 (Assigned Patient Location) [SOURCE]
#   FHIR R4: Encounter.location.location (reference)
#   NCPDP: PharmacyServiceType (conceptual equivalent)
```

### **5. Semantic Search Mode**
**Purpose**: Find fields by clinical meaning or context
**Input**: Natural language terms
**Output**: Clinically relevant fields

```bash
# Find all allergy-related fields
pidgeon find allergy --semantic
# Output:
#   HL7v2.3: AL1 segment (Patient Allergy Information)
#   FHIR R4: AllergyIntolerance resource
#   Clinical Context: Drug allergies, food allergies, environmental

# Find prescription-related fields
pidgeon find prescription --semantic --type medication
```

---

## 🏗️ **Technical Architecture**

### **Core Services Required**

```csharp
public interface IFieldDiscoveryService
{
    Task<FieldSearchResult> FindBySemanticPathAsync(string semanticPath);
    Task<FieldSearchResult> FindByValueAsync(string value, SearchScope scope);
    Task<FieldSearchResult> FindByPatternAsync(string pattern, PatternType type);
    Task<CrossStandardMapping> MapAcrossStandardsAsync(string fieldPath);
    Task<FieldSearchResult> FindBySemanticAsync(string query, string? context);
}

public interface IFieldIndexService
{
    Task<List<FieldMetadata>> GetFieldsByStandardAsync(string standard);
    Task<List<FieldMetadata>> SearchFieldsAsync(string query);
    Task<List<CrossReference>> GetCrossReferencesAsync(string fieldPath);
}

public interface IMessageSearchService
{
    Task<List<ValueLocation>> FindValueInFilesAsync(string value, string searchPath);
    Task<List<ValueLocation>> FindValueInDirectoryAsync(string value, string directory);
}
```

### **Data Models**

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
    public string Standard { get; init; } = "";
    public string Path { get; init; } = "";
    public string Description { get; init; } = "";
    public string MessageType { get; init; } = "";
    public string DataType { get; init; } = "";
    public string Usage { get; init; } = "";
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
    public MappingType Type { get; init; } // Direct, Filtered, Conceptual, None
    public double Confidence { get; init; }
    public string Notes { get; init; } = "";
}
```

---

## 📊 **Data Sources & Indexing Strategy**

### **Standards Metadata Sources**
1. **HL7v2.3**: Existing segment/field definitions from scraped data
2. **FHIR R4**: Resource schemas and element definitions
3. **NCPDP**: Transaction field definitions
4. **Cross-references**: Manual mappings for semantic equivalence

### **Indexing Requirements**
```csharp
// Pre-computed search indices
public class FieldIndex
{
    Dictionary<string, List<FieldMetadata>> SemanticIndex;     // patient.mrn -> [PID.3, Patient.identifier]
    Dictionary<string, List<FieldMetadata>> KeywordIndex;     // "medication" -> [RXE.2, MedicationRequest]
    Dictionary<string, CrossStandardMapping> MappingIndex;    // PID.3 -> {FHIR: Patient.identifier, NCPDP: PatientId}
    Dictionary<string, List<string>> PatternIndex;            // "PID.*" -> [PID.1, PID.3, PID.5, ...]
}
```

---

## 🎨 **User Experience Design**

### **Auto-Mode Detection**
The command should intelligently determine search mode based on query characteristics:

```bash
pidgeon find patient.mrn        # Auto: Field mode (contains dot)
pidgeon find "M123456"         # Auto: Value mode (quoted literal)
pidgeon find PID.*             # Auto: Pattern mode (contains wildcard)
pidgeon find medication        # Auto: Semantic mode (natural language)
```

### **Progressive Disclosure**
Start with summary, allow drilling down:

```bash
# Initial summary
pidgeon find patient.mrn
# Output: Found 3 standards, 5 total locations

# Detailed view
pidgeon find patient.mrn --detailed
# Output: Full field definitions, examples, usage notes

# Export for scripting
pidgeon find patient.mrn --format json > patient_mrn_mapping.json
```

### **Smart Suggestions**
When no results found, provide helpful suggestions:

```bash
pidgeon find patinet.mrn
# Output:
#   No results found for 'patinet.mrn'
#   Did you mean: 'patient.mrn'?
#
#   Related searches:
#   • patient.identifier
#   • patient.id
#   • medical.record.number
```

---

## ⚡ **Performance Requirements**

### **Response Time Targets**
- **Field discovery**: <100ms for cached queries, <500ms for new queries
- **Value search**: <2s for single file, <10s for directory
- **Pattern matching**: <200ms for simple patterns, <1s for regex
- **Cross-standard mapping**: <50ms (pre-computed)

### **Caching Strategy**
- **In-memory cache** for frequently accessed field metadata
- **File system cache** for search index (~10MB total)
- **Incremental updates** when new standards data available

---

## 🚀 **Implementation Phases**

### **Phase 1: Core Infrastructure (Week 1)**
- [ ] Design and implement `IFieldDiscoveryService`
- [ ] Create field metadata loading from existing standards data
- [ ] Build basic in-memory search index
- [ ] Implement auto-mode detection logic

### **Phase 2: Search Modes (Week 2)**
- [ ] Field discovery mode with semantic path resolution
- [ ] Value search mode with file scanning
- [ ] Pattern matching with wildcard support
- [ ] Basic cross-standard mapping

### **Phase 3: Advanced Features (Week 3)**
- [ ] Semantic search with clinical context
- [ ] Enhanced cross-standard mapping with confidence scores
- [ ] Smart suggestions and fuzzy matching
- [ ] Export formats (JSON, CSV, table)

### **Phase 4: Polish & Performance (Week 4)**
- [ ] Performance optimization and caching
- [ ] Comprehensive help and examples
- [ ] Integration testing with real healthcare datasets
- [ ] Documentation and usage guides

---

## ✅ **Success Metrics**

### **Functional Requirements**
- [ ] **99% accuracy** in field location discovery
- [ ] **Sub-second response** for cached queries
- [ ] **Cross-standard coverage** for all major clinical concepts
- [ ] **Zero-learning curve** with intelligent auto-mode detection

### **User Adoption Indicators**
- Users can find fields without reading documentation
- Reduces "where is this field?" questions in community
- Enables cross-standard development workflows
- Demonstrates platform intelligence and value

---

## 💡 **Future Enhancements**

### **Advanced Search Features**
- **Fuzzy matching** for typo tolerance
- **Contextual search** based on message type
- **Bookmark/favorites** for frequently searched fields
- **Search history** with smart suggestions

### **Integration Opportunities**
- **IDE plugins** for real-time field discovery
- **Web interface** for visual field exploration
- **API endpoints** for programmatic field discovery
- **Export to mapping tools** (e.g., Mirth, Integration Engine)

This design provides the foundation for implementing a truly powerful field discovery system that makes healthcare standards navigation intuitive and efficient.