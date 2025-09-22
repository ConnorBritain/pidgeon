# HL7 Database Strategy - From Web Scraping to Production Database

## 🚨 **CRITICAL UPDATE** (September 22, 2025)
**DISCOVERY**: Current CLI works perfectly via JSON files - SQLite database is **NOT REQUIRED FOR MVP**
- **Reality**: All features (lookup, generation, validation, sessions) use JSON-based implementations
- **Database Status**: Complete SQLite implementation exists but is **NOT INTEGRATED with CLI**
- **Ship Decision**: Database migration is **POST-SHIP PERFORMANCE OPTIMIZATION**, not ship requirement
- **Business Value**: SQLite becomes **Professional tier performance enhancement** (1ms vs 100ms lookups)

## Executive Summary
Transform scraped HL7 v2.3 data from Caristix into a normalized, efficient SQLite database that powers Pidgeon's generation, validation, and lookup capabilities while enabling value-add data sets for tiered pricing.

---

## Phase 1: Complete Data Acquisition (Scraping)

### 1.1 Trigger Events (In Progress)
**Status**: Enhanced scraper with hierarchy detection working
**Target**: 276 total trigger events for HL7 v2.3
**Key Features Captured**:
- Event code, title, chapter, description
- Hierarchical segment structure with groups
- Optionality (R/O/C) and repeatability (-//*)
- Parent-child relationships via padding detection
- Group nesting levels (0, 1, 2+)

**Quality Metrics Required**:
- 100% events with chapters
- 95%+ segments with descriptions
- Proper hierarchy preservation
- Group expansion success

### 1.2 Segments (To Do)
**URL Pattern**: `https://hl7-definition.caristix.com/v2/HL7v2.3/Segments/{SEGMENT_CODE}`
**Target**: ~120 segment definitions
**Data to Capture**:
- Segment code, name, description
- Field definitions (position, name, data type, optionality, repeatability)
- Field lengths and constraints
- Table references for coded fields
- Usage notes and examples

### 1.3 Data Types (To Do)
**URL Pattern**: `https://hl7-definition.caristix.com/v2/HL7v2.3/DataTypes/{TYPE_CODE}`
**Target**: ~80 data type definitions
**Data to Capture**:
- Data type code, name, description
- Component structure (for composite types)
- Format specifications
- Length constraints
- Validation rules

### 1.4 Code Tables (To Do)
**URL Pattern**: `https://hl7-definition.caristix.com/v2/HL7v2.3/Tables/{TABLE_NUMBER}`
**Target**: ~500 tables
**Data to Capture**:
- Table number and name
- Code values and descriptions
- User-definable vs HL7-defined
- Suggested values
- No-suggested values tables

---

## Phase 1.5: CLI Performance Aspirations

### **Current CLI Status Analysis**
**Excellent Foundation**: Our CLI already demonstrates 95% effectiveness with rich data access:
- ✅ **Segments**: 96 segments with complete field definitions
- ✅ **Data Types**: 90+ data types with component structures
- ✅ **Tables**: 306 tables with enumerated values
- ✅ **Demographics**: 500+ realistic demographic values (FirstName, LastName, ZipCode, etc.)
- ✅ **Trigger Events**: 276 complete message structure definitions

### **CLI Enhancement Targets**
**Performance Goals**:
- **Lookup Operations**: <50ms response time (vs current JSON parsing ~100-200ms)
- **Complex Queries**: Advanced filtering, cross-reference navigation
- **Search Operations**: Full-text search across all descriptions and field names
- **Relationship Queries**: Fast parent-child navigation (PID.3 → Table 0001 → values)

**CLI-Optimized Enhancements**:
```sql
-- CLI-specific indexes for common lookup patterns
CREATE INDEX idx_cli_field_lookup ON fields(segment_id, position);     -- PID.3 lookups
CREATE INDEX idx_cli_table_values ON code_values(table_id, code);      -- Table value lookups
CREATE INDEX idx_cli_trigger_events ON trigger_events(code);           -- ADT_A01 lookups
CREATE INDEX idx_cli_demographics ON data_values(data_set_id, value);  -- FirstName lookups

-- CLI path recognition view
CREATE VIEW cli_element_lookup AS
SELECT
    s.code || '.' || f.position as path,
    f.name,
    f.description,
    dt.code as data_type,
    f.optionality,
    f.length,
    ct.table_number,
    f.repeatability
FROM fields f
JOIN segments s ON f.segment_id = s.id
JOIN data_types dt ON f.data_type_id = dt.id
LEFT JOIN code_tables ct ON f.table_id = ct.id;
```

### **Integration with Current Demographics**
Our rich demographic datasets are already integrated and working perfectly:
- **Generation Service**: Uses `IDemographicsDataService` with realistic names, addresses, phone numbers
- **Constraint-Aware**: Integrates with `IConstraintResolver` for HL7 table validation
- **Performance**: JSON-based caching provides good performance, database will enhance further

**Database Migration Priority**:
1. **Week 1**: Migrate current JSON data → SQLite schema
2. **Week 2**: Implement hybrid service (database + JSON fallback)
3. **Week 3**: Update CLI commands for database queries
4. **Week 4**: Performance optimization and GUI foundation

---

## Phase 2: Database Design Principles

### 2.1 Core Design Goals
1. **Normalization**: Eliminate redundancy, maintain referential integrity
2. **Performance**: Optimize for common queries (generation, validation, lookup)
3. **Extensibility**: Support multiple HL7 versions, FHIR, NCPDP
4. **Hierarchy**: Preserve complex nested relationships
5. **Versioning**: Track changes and support multiple standards versions

### 2.2 Key Relationships
```
Standards (1) --> (*) Trigger Events
Trigger Events (1) --> (*) Event Segments (hierarchical)
Segments (1) --> (*) Fields
Fields (*) --> (1) Data Types
Fields (*) --> (0..1) Code Tables
Code Tables (1) --> (*) Code Values
Data Types (1) --> (*) Components (for composite types)
Event Segments (*) --> (0..1) Event Segments (parent-child)
```

### 2.3 Performance Considerations
- **Indexes**: On frequently queried fields (event codes, segment codes)
- **Denormalized Views**: For complex hierarchies (materialized views)
- **JSON Fields**: For flexible metadata and vendor patterns
- **FTS**: Full-text search on descriptions and documentation

---

## Phase 3: Proposed Database Schema

### 3.1 Core Structure Tables

```sql
-- Foundation
CREATE TABLE standards (
    id INTEGER PRIMARY KEY,
    version TEXT NOT NULL UNIQUE,  -- '2.3', '2.5', '2.7'
    release_date DATE,
    description TEXT,
    is_active BOOLEAN DEFAULT TRUE
);

-- Trigger Events with hierarchy support
CREATE TABLE trigger_events (
    id INTEGER PRIMARY KEY,
    standard_id INTEGER NOT NULL,
    code TEXT NOT NULL,  -- 'ADT_A01'
    title TEXT,
    chapter TEXT,
    description TEXT,
    message_structure_id INTEGER,  -- Link to message structure
    FOREIGN KEY (standard_id) REFERENCES standards(id),
    UNIQUE(standard_id, code)
);

CREATE INDEX idx_trigger_events_code ON trigger_events(code);
CREATE INDEX idx_trigger_events_chapter ON trigger_events(chapter);

-- Segments definitions
CREATE TABLE segments (
    id INTEGER PRIMARY KEY,
    standard_id INTEGER NOT NULL,
    code TEXT NOT NULL,  -- 'MSH', 'PID'
    name TEXT,
    description TEXT,
    purpose TEXT,
    FOREIGN KEY (standard_id) REFERENCES standards(id),
    UNIQUE(standard_id, code)
);

CREATE INDEX idx_segments_code ON segments(code);

-- Hierarchical event-segment relationships
CREATE TABLE event_segments (
    id INTEGER PRIMARY KEY,
    event_id INTEGER NOT NULL,
    segment_id INTEGER,  -- NULL for groups
    parent_id INTEGER,  -- Self-referencing for hierarchy
    position INTEGER NOT NULL,
    level INTEGER NOT NULL DEFAULT 0,
    optionality TEXT CHECK(optionality IN ('R', 'O', 'C')),
    repeatability TEXT,  -- '1', '*', 'inf', specific number
    is_group BOOLEAN DEFAULT FALSE,
    group_name TEXT,  -- For groups like 'PATIENT', 'PROCEDURE'
    group_path JSON,  -- ["QUERY_RESPONSE", "PROCEDURE"]
    FOREIGN KEY (event_id) REFERENCES trigger_events(id),
    FOREIGN KEY (segment_id) REFERENCES segments(id),
    FOREIGN KEY (parent_id) REFERENCES event_segments(id)
);

CREATE INDEX idx_event_segments_hierarchy ON event_segments(event_id, parent_id, position);
```

### 3.2 Field and Data Type Tables

```sql
-- Data types (primitives and composites)
CREATE TABLE data_types (
    id INTEGER PRIMARY KEY,
    standard_id INTEGER NOT NULL,
    code TEXT NOT NULL,  -- 'ST', 'NM', 'TS', 'XPN'
    name TEXT,
    description TEXT,
    category TEXT,  -- 'primitive', 'composite'
    max_length INTEGER,
    FOREIGN KEY (standard_id) REFERENCES standards(id),
    UNIQUE(standard_id, code)
);

-- Components for composite data types
CREATE TABLE data_type_components (
    id INTEGER PRIMARY KEY,
    data_type_id INTEGER NOT NULL,
    position INTEGER NOT NULL,
    name TEXT,
    data_type_code TEXT,  -- Can reference another data type
    optionality TEXT,
    description TEXT,
    FOREIGN KEY (data_type_id) REFERENCES data_types(id)
);

-- Fields within segments
CREATE TABLE fields (
    id INTEGER PRIMARY KEY,
    segment_id INTEGER NOT NULL,
    position INTEGER NOT NULL,
    name TEXT,
    data_type_id INTEGER,
    optionality TEXT,
    repeatability TEXT,
    length INTEGER,
    table_id INTEGER,  -- Reference to code table
    description TEXT,
    usage_notes TEXT,
    FOREIGN KEY (segment_id) REFERENCES segments(id),
    FOREIGN KEY (data_type_id) REFERENCES data_types(id),
    UNIQUE(segment_id, position)
);

CREATE INDEX idx_fields_segment ON fields(segment_id);
```

### 3.3 Code Tables and Values

```sql
-- Code tables (lookup tables)
CREATE TABLE code_tables (
    id INTEGER PRIMARY KEY,
    standard_id INTEGER NOT NULL,
    table_number TEXT NOT NULL,  -- '0001', '0002'
    name TEXT,
    type TEXT,  -- 'HL7', 'User', 'External'
    description TEXT,
    FOREIGN KEY (standard_id) REFERENCES standards(id),
    UNIQUE(standard_id, table_number)
);

CREATE INDEX idx_code_tables_number ON code_tables(table_number);

-- Code values within tables
CREATE TABLE code_values (
    id INTEGER PRIMARY KEY,
    table_id INTEGER NOT NULL,
    code TEXT NOT NULL,
    description TEXT,
    is_deprecated BOOLEAN DEFAULT FALSE,
    sort_order INTEGER,
    FOREIGN KEY (table_id) REFERENCES code_tables(id),
    UNIQUE(table_id, code)
);

CREATE INDEX idx_code_values_lookup ON code_values(table_id, code);
```

### 3.4 Value-Add Data Sets

```sql
-- Data set categories for realistic generation
CREATE TABLE data_sets (
    id INTEGER PRIMARY KEY,
    name TEXT NOT NULL,
    category TEXT NOT NULL,  -- 'names', 'medications', 'diagnoses', 'procedures'
    subcategory TEXT,  -- 'first_names', 'last_names', 'brand_drugs', 'generic_drugs'
    tier TEXT NOT NULL DEFAULT 'free',  -- 'free', 'pro', 'enterprise'
    source TEXT,  -- 'CMS', 'FDA', 'CDC', 'synthetic'
    version TEXT,
    description TEXT,
    record_count INTEGER,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Actual data values with metadata
CREATE TABLE data_values (
    id INTEGER PRIMARY KEY,
    data_set_id INTEGER NOT NULL,
    value TEXT NOT NULL,
    display_value TEXT,  -- For different formatting
    code TEXT,  -- ICD-10, CPT, NDC codes
    frequency REAL DEFAULT 1.0,  -- For realistic distribution
    metadata JSON,  -- Additional properties
    gender TEXT,  -- For gender-specific names
    ethnicity TEXT,  -- For culturally appropriate names
    year_start INTEGER,  -- For temporal relevance
    year_end INTEGER,
    FOREIGN KEY (data_set_id) REFERENCES data_sets(id)
);

CREATE INDEX idx_data_values_lookup ON data_values(data_set_id, value);
CREATE INDEX idx_data_values_frequency ON data_values(data_set_id, frequency DESC);
```

### 3.5 Runtime and Analytics Tables

```sql
-- Generated/imported messages storage
CREATE TABLE messages (
    id INTEGER PRIMARY KEY,
    message_id TEXT UNIQUE,  -- UUID
    event_type TEXT,
    standard_version TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    source TEXT,  -- 'generated', 'imported', 'validated'
    content TEXT,  -- Full HL7 message
    content_json JSON,  -- Parsed structure
    validation_status TEXT,
    validation_errors JSON,
    metadata JSON  -- Tags, notes, etc.
);

CREATE INDEX idx_messages_type ON messages(event_type);
CREATE INDEX idx_messages_created ON messages(created_at);

-- Vendor patterns learned from analysis
CREATE TABLE vendor_patterns (
    id INTEGER PRIMARY KEY,
    vendor TEXT NOT NULL,
    system_name TEXT,
    event_type TEXT,
    segment_code TEXT,
    field_path TEXT,  -- 'PID.5.1'
    pattern_type TEXT,  -- 'always_populated', 'never_used', 'format'
    pattern_value JSON,
    confidence REAL,
    sample_count INTEGER,
    discovered_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_vendor_patterns ON vendor_patterns(vendor, event_type);
```

---

## Phase 4: Data Migration Strategy

### 4.1 ETL Pipeline
```python
# Pseudo-code for migration
class HL7DatabaseBuilder:
    def __init__(self, db_path='hl7_v23.db'):
        self.conn = sqlite3.connect(db_path)
        self.create_schema()

    def import_trigger_events(self, json_path):
        """Import from scraped trigger events JSON"""
        # Parse JSON with hierarchy
        # Insert events with proper parent-child relationships
        # Maintain referential integrity

    def import_segments(self, json_path):
        """Import segment definitions"""
        # Parse segment structure
        # Link fields to data types
        # Connect to code tables

    def import_data_types(self, json_path):
        """Import data type definitions"""
        # Handle primitive vs composite
        # Build component relationships

    def import_code_tables(self, json_path):
        """Import code tables and values"""
        # Maintain table references
        # Handle user-definable markers
```

### 4.2 Data Quality Validation
- Referential integrity checks
- Completeness validation
- Hierarchy consistency
- Cross-reference verification

---

## Phase 5: Query Patterns and Access Layer

### 5.1 Common Query Patterns
```sql
-- Get complete event structure with hierarchy
WITH RECURSIVE event_tree AS (
    SELECT * FROM event_segments WHERE event_id = ? AND parent_id IS NULL
    UNION ALL
    SELECT es.* FROM event_segments es
    JOIN event_tree et ON es.parent_id = et.id
)
SELECT * FROM event_tree ORDER BY level, position;

-- Find all events using a specific segment
SELECT DISTINCT te.* FROM trigger_events te
JOIN event_segments es ON te.id = es.event_id
JOIN segments s ON es.segment_id = s.id
WHERE s.code = ?;

-- Get field structure for segment
SELECT f.*, dt.code as data_type, ct.table_number
FROM fields f
JOIN data_types dt ON f.data_type_id = dt.id
LEFT JOIN code_tables ct ON f.table_id = ct.id
WHERE f.segment_id = ?
ORDER BY f.position;
```

### 5.2 Performance Optimization
- Strategic indexes on lookup fields
- Materialized views for complex hierarchies
- Query plan analysis and optimization
- Connection pooling for concurrent access

---

## Phase 6: Distribution and Deployment

### 6.1 Database Files
```
pidgeon/
   data/
      core/
         hl7_v23.db      # Complete HL7 v2.3 structure
         hl7_v25.db      # HL7 v2.5 (future)
         hl7_v27.db      # HL7 v2.7 (future)
      datasets/
         free.db         # Basic data sets
         pro.db          # Enhanced data sets (encrypted)
         enterprise.db   # Full medical codes (licensed)
      user/
          workspace.db    # User's messages and patterns
```

### 6.2 Update Strategy
- Differential updates for data sets
- Version migration scripts
- Backwards compatibility
- Data integrity verification

---

## Phase 7: Semantic Path Integration (NEW)

### 7.1 Official HL7 v2-to-FHIR Mapping Integration
Building on our comprehensive HL7 structure database, we're adding **semantic path mapping** capabilities derived from official HL7 v2-to-FHIR Implementation Guide CSV files.

**Data Source**: Apache 2.0 licensed CSV files from HL7 International
- **Location**: `data/interop/hl7-fhir-mappings/` (~1.6MB curated dataset)
- **Content**: 279 mapping files (75 segments, 105 datatypes, 99 codesystems)
- **Update Strategy**: Git subtree from official HL7 repository

### 7.2 Two-Tier Semantic Path Architecture
**Design Goal**: Progressive disclosure system balancing simplicity (80% of users) with completeness (advanced interoperability scenarios)

```sql
-- Semantic path definitions (generated from official mappings)
CREATE TABLE semantic_paths (
    id INTEGER PRIMARY KEY,
    semantic_path TEXT NOT NULL UNIQUE,           -- 'patient.mrn'
    tier TEXT NOT NULL CHECK(tier IN ('essential', 'advanced')),
    category TEXT NOT NULL,                       -- 'patient', 'encounter', etc.
    description TEXT NOT NULL,
    hl7_field TEXT,                              -- 'PID-3'
    fhir_path TEXT,                              -- 'Patient.identifier[2]'
    source_file TEXT,                            -- Original CSV file reference
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (category) REFERENCES semantic_categories(name)
);

-- Standard-specific mappings (from CSV data)
CREATE TABLE standard_mappings (
    id INTEGER PRIMARY KEY,
    semantic_path_id INTEGER REFERENCES semantic_paths(id),
    standard TEXT NOT NULL,                       -- 'HL7v23', 'FHIRv4'
    field_path TEXT NOT NULL,                     -- 'PID.3.1', 'Patient.identifier[0].value'
    data_type TEXT,                               -- 'CX', 'Identifier'
    cardinality_min INTEGER DEFAULT 0,
    cardinality_max INTEGER DEFAULT 1,            -- -1 for unlimited
    conditions TEXT,                              -- JSON array of IF/THEN conditions
    comments TEXT,
    vocabulary_mapping TEXT,                      -- Code system translations
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP
);

-- Path aliases (smart resolution: patient.mrn → patient.identifiers.mrn)
CREATE TABLE path_aliases (
    id INTEGER PRIMARY KEY,
    alias TEXT NOT NULL,                         -- 'patient.identifiers.mrn'
    semantic_path_id INTEGER REFERENCES semantic_paths(id),
    priority INTEGER DEFAULT 0                   -- Higher = preferred
);

-- Conditional mapping logic (from CSV IF/THEN statements)
CREATE TABLE conditional_mappings (
    id INTEGER PRIMARY KEY,
    standard_mapping_id INTEGER REFERENCES standard_mappings(id),
    condition_type TEXT NOT NULL,                -- 'IF', 'ELSE_IF', 'ELSE'
    condition_expression TEXT,                   -- 'PV1-2.1 NOT EQUALS "P"'
    result_path TEXT,                           -- Alternative field path
    result_value TEXT                           -- Static value to set
);

-- Vocabulary mappings (from codesystems CSVs)
CREATE TABLE vocabulary_mappings (
    id INTEGER PRIMARY KEY,
    hl7_table TEXT,                             -- 'Table 0001'
    hl7_code TEXT,                              -- 'M'
    hl7_display TEXT,                           -- 'Male'
    fhir_system TEXT,                           -- 'http://hl7.org/fhir/administrative-gender'
    fhir_code TEXT,                             -- 'male'
    fhir_display TEXT                           -- 'Male'
);

-- Semantic categories for organization
CREATE TABLE semantic_categories (
    name TEXT PRIMARY KEY,                       -- 'patient', 'encounter', 'observation'
    display_name TEXT NOT NULL,                 -- 'Patient Demographics'
    description TEXT,
    sort_order INTEGER DEFAULT 0
);

-- Performance indexes
CREATE INDEX idx_semantic_paths_tier ON semantic_paths(tier);
CREATE INDEX idx_semantic_paths_category ON semantic_paths(category);
CREATE INDEX idx_standard_mappings_standard ON standard_mappings(standard);
CREATE INDEX idx_standard_mappings_path ON standard_mappings(semantic_path_id, standard);
CREATE INDEX idx_path_aliases_alias ON path_aliases(alias);
CREATE INDEX idx_vocabulary_mappings_hl7 ON vocabulary_mappings(hl7_table, hl7_code);
```

### 7.3 Integration with Existing Schema
**Key Integration Points**:
1. **Link to existing fields table**: `semantic_paths.hl7_field` → `segments.code + '.' + fields.position`
2. **Vocabulary mapping integration**: `vocabulary_mappings.hl7_table` → `code_tables.table_number`
3. **Data type correlation**: `standard_mappings.data_type` → `data_types.code`

### 7.4 CSV Import Pipeline
```csharp
public class SemanticPathImportPipeline
{
    // Phase 1: Parse official CSV mappings
    public async Task<ImportedMappingData> ImportOfficialMappingsAsync()
    {
        var parser = new V2ToFhirMappingParser();
        var mappingData = new ImportedMappingData();

        // Import segment mappings (75 files)
        var segmentFiles = Directory.GetFiles("data/interop/hl7-fhir-mappings/segments");
        foreach (var file in segmentFiles)
        {
            var rows = await parser.ParseSegmentMappingAsync(file);
            mappingData.SegmentMappings[file] = rows;
        }

        // Import datatype and codesystem mappings...
        return mappingData;
    }

    // Phase 2: Generate semantic paths with tier classification
    public async Task<List<GeneratedSemanticPath>> GenerateSemanticPathsAsync(ImportedMappingData data)
    {
        var generator = new SemanticPathGenerator();
        return await generator.GenerateSemanticPathsAsync(data);
    }

    // Phase 3: Persist to SQLite database
    public async Task PersistToDatabase(List<GeneratedSemanticPath> paths, IDbConnection db)
    {
        // Insert semantic paths with tier classification
        // Create standard mappings for HL7 and FHIR
        // Build vocabulary mappings from codesystem files
        // Generate intelligent aliases
    }
}
```

### 7.5 Performance Integration
**Target Performance** (aligns with existing goals):
- **Essential Paths (Tier 1)**: <1ms lookup (in-memory indexes)
- **Advanced Paths (Tier 2)**: <5ms complex queries with joins
- **Cross-Standard Resolution**: <10ms HL7 ↔ FHIR path translation
- **Fuzzy Search**: <100ms full-text search across semantic paths

**Query Optimization Examples**:
```sql
-- Essential path lookup (optimized for CLI speed)
SELECT sp.semantic_path, sp.description, sm_hl7.field_path as hl7_path, sm_fhir.field_path as fhir_path
FROM semantic_paths sp
JOIN standard_mappings sm_hl7 ON sp.id = sm_hl7.semantic_path_id AND sm_hl7.standard = 'HL7v23'
JOIN standard_mappings sm_fhir ON sp.id = sm_fhir.semantic_path_id AND sm_fhir.standard = 'FHIRv4'
WHERE sp.tier = 'essential' AND sp.semantic_path = ?;

-- Cross-standard path resolution
SELECT hl7_mapping.field_path as hl7_field, fhir_mapping.field_path as fhir_field
FROM semantic_paths sp
JOIN standard_mappings hl7_mapping ON sp.id = hl7_mapping.semantic_path_id AND hl7_mapping.standard = 'HL7v23'
JOIN standard_mappings fhir_mapping ON sp.id = fhir_mapping.semantic_path_id AND fhir_mapping.standard = 'FHIRv4'
WHERE sp.semantic_path = ?;

-- Fuzzy search with alias resolution
SELECT DISTINCT sp.semantic_path, sp.description, sp.tier
FROM semantic_paths sp
LEFT JOIN path_aliases pa ON sp.id = pa.semantic_path_id
WHERE sp.semantic_path LIKE ? OR sp.description LIKE ? OR pa.alias LIKE ?
ORDER BY sp.tier, sp.semantic_path;
```

### 7.6 Integration with CLI Commands
**Enhanced PathCommand functionality**:
```bash
# Tier 1 (Essential) - Default behavior
pidgeon path list                    # Shows 25-30 essential paths
pidgeon path resolve patient.mrn     # <1ms lookup from database

# Tier 2 (Advanced) - Complete functionality
pidgeon path list --complete         # Shows 200+ paths from database
pidgeon path search "identifier"     # Fuzzy search across all paths
pidgeon path translate patient.mrn --from hl7 --to fhir  # Cross-standard resolution
```

---

## Next Steps

1. **Complete Trigger Events Scraping** (Current)
   - Run full scrape of 276 events
   - Validate hierarchy preservation
   - Quality check all data points

2. **Abstract Scraping Framework**
   - Create base scraper class
   - Implement segment scraper
   - Implement data type scraper
   - Implement table scraper

3. **Database Implementation**
   - Create schema from this design
   - Build migration tools
   - Implement access layer
   - Add query optimization

4. **Integration with Pidgeon**
   - Replace JSON lookups with DB queries
   - Implement caching layer
   - Add data set management
   - Enable vendor pattern learning

---

## Success Metrics

- **Data Completeness**: 95%+ coverage of all HL7 elements
- **Query Performance**: <10ms for common lookups
- **Database Size**: <50MB for core structure
- **Hierarchy Accuracy**: 100% preservation of relationships
- **Cross-Reference Integrity**: Zero orphaned references