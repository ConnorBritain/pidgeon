# Interoperability Data Integration Strategy

**Date**: September 21, 2025
**Status**: Planning Complete - Ready for Implementation
**Context**: HL7 v2-to-FHIR mapping integration with SQLite migration

---

## 🔍 **License Analysis & Inclusion Strategy**

### **License Compliance**
✅ **Apache License 2.0** - Highly permissive for commercial use
- ✅ **Use**: Can use for any purpose, including commercial
- ✅ **Modify**: Can modify the mapping files as needed
- ✅ **Redistribute**: Can include in our repository
- ✅ **Private Use**: Can use in proprietary software
- ⚠️ **Requirements**: Must include license notice and attribution

### **What to Include (Minimal Essential Set)**

#### **INCLUDE** (~2MB total):
```
pidgeon/data/interop/
├── LICENSE                                    # Required attribution
├── ATTRIBUTION.md                             # Our attribution file
└── hl7-fhir-mappings/                        # Cleaned, curated subset
    ├── segments/                              # 424K - Core segment mappings
    │   ├── PID[Patient].csv                   # Essential patient mappings
    │   ├── PV1[Encounter].csv                 # Essential encounter mappings
    │   ├── MSH[MessageHeader].csv             # Message control
    │   ├── OBX[Observation].csv               # Lab results
    │   ├── ORC[ServiceRequest].csv            # Orders
    │   ├── DG1[Condition].csv                 # Diagnoses
    │   ├── AL1[AllergyIntolerance].csv        # Allergies
    │   └── IN1[Coverage].csv                  # Insurance
    ├── datatypes/                             # 392K - Data type conversions
    │   ├── CX[Identifier].csv                 # Identifier mappings
    │   ├── XPN[HumanName].csv                 # Name mappings
    │   ├── XAD[Address].csv                   # Address mappings
    │   └── XTN[ContactPoint].csv              # Phone/contact mappings
    ├── codesystems/                           # 708K - Vocabulary mappings
    │   ├── AdministrativeSex.csv              # Gender codes
    │   ├── MaritalStatus.csv                  # Marital status codes
    │   └── PatientClass.csv                   # Encounter classes
    └── inventories/                           # 60K - Summary files
        ├── segments-inventory.csv
        ├── datatypes-inventory.csv
        └── codesystems-inventory.csv
```

#### **EXCLUDE** (~9MB+ of build infrastructure):
- ❌ `/input/` - FHIR IG source files (9.3MB)
- ❌ `/src/` - Java build tools (136K)
- ❌ `/template/` - FHIR IG templates
- ❌ `_build.*`, `_gen*.*` - Build scripts
- ❌ `pom.xml`, `sushi-config.yaml` - Build configuration
- ❌ `/samples/` - Keep a few key examples, not all 80K

### **Ongoing Updates Strategy**

#### **Option 1: Selective Git Subtree (Recommended)**
```bash
# Add official repo as remote
git remote add hl7-v2-fhir https://github.com/HL7/v2-to-fhir.git

# Pull only mappings directory
git subtree pull --prefix=pidgeon/data/interop/hl7-fhir-mappings hl7-v2-fhir master --squash

# Update when needed (quarterly?)
git subtree pull --prefix=pidgeon/data/interop/hl7-fhir-mappings hl7-v2-fhir master --squash
```

#### **Option 2: Automated Sync Script**
```bash
#!/bin/bash
# scripts/update-hl7-mappings.sh
# Downloads latest mappings and updates our curated subset
```

---

## 🗄️ **SQLite Integration Strategy**

### **Migration from CSV → SQLite**

Instead of parsing CSVs at runtime, we **import once during build/migration** into SQLite tables optimized for our semantic path system.

#### **Database Schema Design**

```sql
-- Core semantic path definitions (generated from CSV mappings)
CREATE TABLE semantic_paths (
    id INTEGER PRIMARY KEY,
    semantic_path TEXT NOT NULL UNIQUE,           -- 'patient.mrn'
    tier TEXT NOT NULL CHECK(tier IN ('essential', 'advanced')),
    category TEXT NOT NULL,                       -- 'patient', 'encounter', etc.
    description TEXT NOT NULL,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP
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
    source_file TEXT,                            -- Original CSV file reference
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP
);

-- Path aliases (smart resolution)
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

-- Performance indexes
CREATE INDEX idx_semantic_paths_tier ON semantic_paths(tier);
CREATE INDEX idx_semantic_paths_category ON semantic_paths(category);
CREATE INDEX idx_standard_mappings_standard ON standard_mappings(standard);
CREATE INDEX idx_standard_mappings_path ON standard_mappings(semantic_path_id, standard);
CREATE INDEX idx_path_aliases_alias ON path_aliases(alias);
```

#### **CSV Import Process**

```csharp
public class MappingImportService
{
    public async Task ImportSegmentMappingsAsync(string csvPath)
    {
        var rows = await CsvParser.ParseAsync(csvPath);

        foreach (var row in rows)
        {
            // Generate semantic path: PID-5 → patient.name
            var semanticPath = GenerateSemanticPath(row.HL7Field, row.FHIRPath);

            // Classify tier: essential vs advanced
            var tier = ClassifyTier(row.HL7Field, row.FHIRPath);

            // Insert into semantic_paths table
            var pathId = await InsertSemanticPathAsync(semanticPath, tier, row.Description);

            // Insert HL7 mapping
            await InsertStandardMappingAsync(pathId, "HL7v23", row.HL7Field, row);

            // Insert FHIR mapping
            await InsertStandardMappingAsync(pathId, "FHIRv4", row.FHIRPath, row);

            // Handle conditional logic
            if (!string.IsNullOrEmpty(row.Condition))
            {
                await InsertConditionalMappingAsync(mappingId, row.Condition);
            }
        }
    }
}
```

### **Runtime Query Performance**

With SQLite indexes, we get blazing fast lookups:

```csharp
// Tier 1 (Essential) paths - <1ms
public async Task<List<SemanticPathDefinition>> GetEssentialPathsAsync()
{
    return await _db.QueryAsync<SemanticPathDefinition>(
        "SELECT * FROM semantic_paths WHERE tier = 'essential' ORDER BY category, semantic_path"
    );
}

// Resolve semantic path to standard field - <1ms
public async Task<StandardMapping> ResolvePathAsync(string semanticPath, string standard)
{
    return await _db.QueryFirstAsync<StandardMapping>(@"
        SELECT sm.* FROM standard_mappings sm
        JOIN semantic_paths sp ON sm.semantic_path_id = sp.id
        WHERE sp.semantic_path = @semanticPath AND sm.standard = @standard
    ", new { semanticPath, standard });
}

// Fuzzy search with aliases - <5ms
public async Task<List<SemanticPathDefinition>> SearchPathsAsync(string query)
{
    return await _db.QueryAsync<SemanticPathDefinition>(@"
        SELECT DISTINCT sp.* FROM semantic_paths sp
        LEFT JOIN path_aliases pa ON sp.id = pa.semantic_path_id
        WHERE sp.semantic_path LIKE @pattern
           OR sp.description LIKE @pattern
           OR pa.alias LIKE @pattern
        ORDER BY sp.tier, sp.semantic_path
    ", new { pattern = $"%{query}%" });
}
```

---

## 🔄 **Development Workflow Integration**

### **Phase 1: CSV Import Migration (Week 1)**

```bash
# 1. Clean up interop directory
pidgeon/data/interop/
├── LICENSE
├── ATTRIBUTION.md
└── hl7-fhir-mappings/     # Curated 2MB subset

# 2. Create migration script
dotnet run --project tools/MappingImporter
# Parses CSVs → Populates SQLite tables

# 3. Update SemanticPathService
# Query SQLite instead of parsing CSVs at runtime
```

### **Phase 2: Enhanced Querying (Week 2)**

```csharp
public class SqliteSemanticPathService : ISemanticPathService
{
    private readonly IDbConnection _db;

    public async Task<Result<IReadOnlyList<SemanticPathDefinition>>> GetAvailablePathsAsync(
        SemanticPathTier? tier = null)
    {
        var sql = "SELECT * FROM semantic_paths";
        if (tier.HasValue)
            sql += " WHERE tier = @tier";
        sql += " ORDER BY category, semantic_path";

        var paths = await _db.QueryAsync<SemanticPathDefinition>(sql, new { tier = tier?.ToString() });
        return Result<IReadOnlyList<SemanticPathDefinition>>.Success(paths.ToList());
    }

    // ... other methods using SQLite queries
}
```

### **Phase 3: Advanced Features (Week 3)**

- **Conditional Logic Engine**: Evaluate IF/THEN statements from CSV
- **Cross-Standard Resolution**: Query both HL7 and FHIR mappings
- **Performance Optimization**: Connection pooling, prepared statements
- **Migration Tools**: Update SQLite when CSV mappings change

---

## 🎯 **Benefits of SQLite Integration**

### **Performance Benefits**
- **Essential Paths**: <1ms lookup (in-memory indexes)
- **Advanced Paths**: <5ms complex queries with joins
- **Fuzzy Search**: Full-text search capabilities
- **Caching**: SQLite page cache for repeated queries

### **Maintenance Benefits**
- **Single Source of Truth**: CSV mappings → SQLite → Runtime queries
- **Structured Data**: Normalized tables vs. flat CSV parsing
- **Query Flexibility**: Complex filters, joins, aggregations
- **Backup/Recovery**: Standard SQLite database practices

### **Development Benefits**
- **Schema Evolution**: ALTER TABLE for new features
- **Testing**: Seed test database with known mappings
- **Debugging**: SQL queries easier to debug than CSV parsing
- **Extensibility**: Add custom semantic paths alongside official ones

---

## 🚀 **Implementation Plan**

### **Week 1: Setup & Import**
1. **Clean interop directory**: Keep only essential CSV files
2. **Create import service**: Parse CSVs → SQLite tables
3. **Design database schema**: Optimized for semantic path queries
4. **Initial data import**: Essential segments (PID, PV1, MSH)

### **Week 2: Service Integration**
1. **Update SemanticPathService**: Query SQLite instead of CSV
2. **Add tier filtering**: Essential vs. advanced path queries
3. **Implement fuzzy search**: Full-text search across paths
4. **Performance testing**: Ensure <1ms essential path lookups

### **Week 3: Advanced Features**
1. **Conditional logic engine**: Evaluate IF/THEN from mappings
2. **Cross-standard resolution**: HL7 ↔ FHIR path translation
3. **Alias resolution**: Smart fallback (patient.mrn → patient.identifiers.mrn)
4. **Update tooling**: Scripts to refresh from latest HL7 mappings

---

**This approach gives us the best of both worlds: authoritative HL7 mappings with high-performance SQLite queries optimized for our two-tier semantic path system.**