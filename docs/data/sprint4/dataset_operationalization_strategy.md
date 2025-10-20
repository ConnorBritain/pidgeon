# Dataset Operationalization Strategy

## 📊 **Current Dataset Inventory Analysis**

Based on the downloaded files in `pidgeon/src/Pidgeon.Data/datasets/`, we have:

### ✅ **Available Datasets**
1. **NDC Drug Products** (`ndctext.zip`)
   - Files: `product.txt`, `package.txt`
   - Size: ~68MB uncompressed
   - Format: Tab-delimited with headers
   - Contains: NDC codes, drug names, strengths, dosage forms, routes, labelers

2. **LOINC Laboratory Codes** (`Loinc_2.81.zip`)
   - Files: `LoincTable/Loinc.csv` + 80+ accessory files
   - Size: ~80MB compressed
   - Format: CSV with extensive metadata
   - Contains: 95,000+ lab test codes, descriptions, units, methods

3. **ICD-10 Diagnosis Codes** (`table-and-index.zip`)
   - Files: Multiple XML/PDF index files
   - Size: ~22MB compressed
   - Format: XML structured data
   - Contains: Complete ICD-10-CM code listings

4. **NPPES Provider Registry** (`NPPES_Data_Dissemination_September_2025.zip`)
   - Files: Main CSV (10.9GB), plus endpoint/othername files
   - Size: 1GB compressed, 11GB+ uncompressed
   - Format: CSV with complex structure
   - Contains: 7M+ healthcare providers with NPI, specialties, addresses

5. **US ZIP Codes** (`simplemaps_uszips_basicv1.911.zip`)
   - Files: `uszips.csv`
   - Size: ~6MB uncompressed
   - Format: CSV with geographic data
   - Contains: ZIP codes, cities, states, lat/lng, demographics

6. **CVX Vaccine Codes** (`cvx.txt`)
   - File: Already extracted, pipe-delimited
   - Size: 288 vaccine codes
   - Format: Plain text with pipes
   - Contains: Vaccine codes, descriptions, status

7. **FDA Orange Book** (`EOBZIP_2025_08.zip`)
   - Files: `products.txt`, `patent.txt`, `exclusivity.txt`
   - Size: ~8MB uncompressed
   - Format: Tab-delimited
   - Contains: Generic/brand equivalents, patent info

### ❌ **Missing Critical Datasets**
1. **Census Names Data** - Need to download separately
2. **UMLS/RxNorm** - Requires registration
3. **SNOMED CT** - Requires UMLS license

---

## 🏗️ **Extraction and Processing Strategy**

### **Phase 1: Immediate Extraction (Week 1)**

#### **1.1 Data Extraction Scripts**
Create extraction utilities in `Pidgeon.Data/Scripts/`:

```csharp
// DatasetExtractor.cs - Extract key files from archives
public class DatasetExtractor
{
    public async Task ExtractEssentialFiles(string datasetsPath)
    {
        // NDC Products
        await ExtractFromZip("ndctext.zip", ["product.txt"], "ndc/");

        // LOINC Main Table
        await ExtractFromZip("Loinc_2.81.zip", ["LoincTable/Loinc.csv"], "loinc/");

        // US ZIP Codes
        await ExtractFromZip("simplemaps_uszips_basicv1.911.zip", ["uszips.csv"], "geographic/");

        // Orange Book
        await ExtractFromZip("EOBZIP_2025_08.zip", ["products.txt"], "fda/");

        // ICD-10 (selective extraction)
        await ExtractFromZip("table-and-index.zip", ["Table and Index/icd10cm_index_2026.xml"], "icd10/");

        // CVX already extracted
        File.Copy("cvx.txt", "extracted/vaccines/cvx.txt");
    }
}
```

#### **1.2 Selective NPPES Processing**
**Critical**: The NPPES file is 11GB uncompressed. We need selective processing:

```csharp
// NPPESProcessor.cs - Stream processing for large files
public class NPPESProcessor
{
    public async Task ProcessProviderSubset(string nppesCsv, string outputPath)
    {
        var targetSpecialties = new[] {
            "Internal Medicine", "Emergency Medicine", "Family Medicine",
            "Pediatrics", "Cardiology", "Radiology", "Pathology"
        };

        // Stream process: Keep only top specialties, active providers
        // Target: 10,000 most common providers (~50MB)
        using var reader = new StreamReader(nppesCsv);
        using var writer = new StreamWriter(outputPath);

        await ProcessLargeCSVStream(reader, writer,
            filter: row => IsTargetSpecialty(row, targetSpecialties) && IsActive(row),
            maxRecords: 10000);
    }
}
```

### **Phase 2: Database Schema Design (Week 1-2)**

#### **2.1 Extend Existing SQLite Schema**
Build on current `pidgeon.db` structure:

```sql
-- Core medication catalog
CREATE TABLE medications (
    ndc_code TEXT PRIMARY KEY,
    proprietary_name TEXT,
    nonproprietary_name TEXT,
    dosage_form TEXT,
    route TEXT,
    strength TEXT,
    strength_unit TEXT,
    labeler_name TEXT,
    dea_schedule TEXT,
    active BOOLEAN DEFAULT TRUE,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP
);

-- Laboratory test catalog
CREATE TABLE lab_tests (
    loinc_code TEXT PRIMARY KEY,
    component TEXT,
    property TEXT,
    time_aspect TEXT,
    system TEXT,
    scale_type TEXT,
    method_type TEXT,
    class TEXT,
    long_common_name TEXT,
    status TEXT,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP
);

-- Provider registry (subset)
CREATE TABLE providers (
    npi TEXT PRIMARY KEY,
    organization_name TEXT,
    last_name TEXT,
    first_name TEXT,
    credential TEXT,
    primary_taxonomy TEXT,
    primary_specialty TEXT,
    practice_address TEXT,
    practice_city TEXT,
    practice_state TEXT,
    practice_zip TEXT,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP
);

-- Geographic data
CREATE TABLE zip_codes (
    zip TEXT PRIMARY KEY,
    city TEXT,
    state_id TEXT,
    state_name TEXT,
    latitude REAL,
    longitude REAL,
    population INTEGER,
    county_name TEXT,
    timezone TEXT
);

-- Vaccine codes
CREATE TABLE vaccines (
    cvx_code TEXT PRIMARY KEY,
    short_description TEXT,
    full_name TEXT,
    vaccine_status TEXT,
    notes TEXT
);

-- Create indexes for performance
CREATE INDEX idx_medications_name ON medications(proprietary_name);
CREATE INDEX idx_medications_generic ON medications(nonproprietary_name);
CREATE INDEX idx_lab_tests_component ON lab_tests(component);
CREATE INDEX idx_providers_specialty ON providers(primary_specialty);
CREATE INDEX idx_zip_codes_state ON zip_codes(state_id);
```

#### **2.2 Data Loading Pipeline**
```csharp
// DatabasePopulator.cs
public class DatabasePopulator
{
    public async Task PopulateFromExtractedFiles(string extractedPath)
    {
        // Parallel loading for performance
        var tasks = new[]
        {
            LoadMedications(Path.Combine(extractedPath, "ndc/product.txt")),
            LoadLabTests(Path.Combine(extractedPath, "loinc/Loinc.csv")),
            LoadProviders(Path.Combine(extractedPath, "nppes/providers_subset.csv")),
            LoadZipCodes(Path.Combine(extractedPath, "geographic/uszips.csv")),
            LoadVaccines(Path.Combine(extractedPath, "vaccines/cvx.txt"))
        };

        await Task.WhenAll(tasks);

        // Create derived tables for performance
        await CreateFrequencyTables();
        await CreateLookupTables();
    }

    private async Task CreateFrequencyTables()
    {
        // Create "common" subsets for fast in-memory loading
        await ExecuteSql(@"
            CREATE TABLE common_medications AS
            SELECT * FROM medications
            WHERE labeler_name IN (SELECT labeler_name FROM medications GROUP BY labeler_name ORDER BY COUNT(*) DESC LIMIT 100)
            LIMIT 1000;

            CREATE TABLE common_lab_tests AS
            SELECT * FROM lab_tests
            WHERE class IN ('CHEM', 'HEMA', 'MICRO', 'SERO')
            LIMIT 500;
        ");
    }
}
```

---

## 🔧 **Integration with Field Value Resolvers**

### **Phase 3: Enhanced Resolver Implementation (Week 2)**

#### **3.1 Database-Backed Resolvers**
Extend existing resolvers to use database lookups:

```csharp
// Enhanced DemographicFieldResolver.cs
public class DemographicFieldResolver : IFieldValueResolver
{
    private readonly MedicationService _medicationService;
    private readonly ProviderService _providerService;
    private readonly GeographicService _geographicService;

    public async Task<string?> ResolveAsync(FieldResolutionContext context)
    {
        return context.SemanticPath switch
        {
            // Medication fields (RXE, RXO segments)
            "medication.ndc" => await _medicationService.GetRandomNDCAsync(),
            "medication.name" => await _medicationService.GetRandomMedicationNameAsync(),
            "medication.strength" => await _medicationService.GetRandomStrengthAsync(),
            "medication.route" => await _medicationService.GetRandomRouteAsync(),

            // Provider fields (MSH, PV1, OBR segments)
            "provider.npi" => await _providerService.GetRandomNPIAsync(),
            "provider.name" => await _providerService.GetRandomProviderNameAsync(context.Options.Specialty),
            "provider.specialty" => await _providerService.GetRandomSpecialtyAsync(),

            // Lab test fields (OBR, OBX segments)
            "lab.loinc_code" => await _labService.GetRandomLOINCAsync(),
            "lab.test_name" => await _labService.GetRandomTestNameAsync(),
            "lab.result_value" => await _labService.GetRandomResultValueAsync(context.TestCode),

            // Geographic fields (PID segment)
            "patient.zip" => await _geographicService.GetRandomZipAsync(context.Options.State),
            "patient.city" => await _geographicService.GetCityForZipAsync(context.ResolvedFields["patient.zip"]),
            "patient.state" => await _geographicService.GetStateForZipAsync(context.ResolvedFields["patient.zip"]),

            _ => await base.ResolveAsync(context)
        };
    }
}
```

#### **3.2 Smart Clinical Correlations**
Create intelligent associations between data elements:

```csharp
// ClinicalCorrelationService.cs
public class ClinicalCorrelationService
{
    public async Task<string> GetContextualMedication(string diagnosisCode, string patientAge)
    {
        // Age-appropriate medication selection
        if (int.TryParse(patientAge, out int age) && age < 18)
        {
            return await GetPediatricMedication(diagnosisCode);
        }

        // Diagnosis-appropriate medication selection
        var correlations = await GetMedicationDiagnosisCorrelations(diagnosisCode);
        return correlations.GetRandomWeighted();
    }

    public async Task<string> GetRealisticLabValue(string loincCode, string patientAge, string gender)
    {
        var referenceRange = await GetReferenceRange(loincCode, patientAge, gender);

        // 70% normal, 20% slightly abnormal, 10% significantly abnormal
        var random = Random.Shared.NextDouble();
        return random switch
        {
            < 0.7 => referenceRange.GetNormalValue(),
            < 0.9 => referenceRange.GetSlightlyAbnormalValue(),
            _ => referenceRange.GetSignificantlyAbnormalValue()
        };
    }
}
```

---

## 📈 **Performance Optimization Strategy**

### **Phase 4: Multi-Tier Caching (Week 2-3)**

#### **4.1 Three-Tier Architecture**
```csharp
// CachedDataService.cs
public class CachedDataService
{
    // Tier 1: In-Memory (fastest, most common)
    private static readonly Dictionary<string, string[]> _commonCache = new()
    {
        ["common_medications"] = LoadTop1000Medications(),
        ["common_lab_tests"] = LoadTop500LabTests(),
        ["common_specialties"] = LoadCommonSpecialties()
    };

    // Tier 2: SQLite with indexes (fast, complete)
    private readonly ISqliteRepository _database;

    // Tier 3: Source files (comprehensive, slower)
    private readonly IFileDataRepository _files;

    public async Task<string> GetValueAsync(string category, string? context = null)
    {
        // Try in-memory first
        if (_commonCache.TryGetValue($"common_{category}", out var cached))
        {
            return cached[Random.Shared.Next(cached.Length)];
        }

        // Try database second
        var dbResult = await _database.GetRandomValueAsync(category, context);
        if (dbResult != null) return dbResult;

        // Fall back to files
        return await _files.GetRandomValueAsync(category, context);
    }
}
```

#### **4.2 Lazy Loading with Preloading**
```csharp
// DataPreloader.cs - Background service
public class DataPreloader : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Preload common data at startup
        await PreloadInMemoryCache();

        // Warm database connections
        await WarmDatabaseConnections();

        // Background refresh every 4 hours
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromHours(4), stoppingToken);
            await RefreshCache();
        }
    }
}
```

---

## 🚨 **Critical Gaps and Missing Data**

### **Immediate Gaps to Address**

#### **1. Census Names Data (High Priority)**
- **Missing**: Patient first/last name frequency distributions
- **Impact**: Generic name generation instead of realistic frequency-weighted names
- **Solution**: Download from https://www.census.gov/topics/population/genealogy/data/2010_surnames.html
- **Files Needed**:
  - `Names_2010Census.xlsx` (surnames)
  - `yob[year].txt` files (first names by year/gender)

#### **2. Missing ICD-10 Data Structure (Medium Priority)**
- **Current**: Only index files, not actual code-to-description mappings
- **Impact**: Cannot generate realistic diagnosis codes for DG1 segments
- **Solution**: Download from https://www.cms.gov/files/zip/2024-icd-10-cm-codes.zip
- **Files Needed**: `icd10cm_codes_2024.txt` (code-description pairs)

#### **3. Lab Reference Ranges (Medium Priority)**
- **Current**: LOINC codes but no reference ranges or typical values
- **Impact**: Cannot generate realistic lab results in OBX segments
- **Solution**: Use NHANES data or create synthetic ranges based on LOINC metadata
- **Files Needed**: Reference range tables by age/gender

#### **4. Specialty-Condition Correlations (Low Priority)**
- **Current**: Providers and diagnoses exist separately
- **Impact**: Cardiologist might generate dermatology diagnosis
- **Solution**: Create correlation tables from Medicare utilization data
- **Files Needed**: Provider specialty → common diagnosis mappings

### **Data Quality Issues**

#### **1. NPPES File Size Management**
- **Issue**: 11GB file too large for standard processing
- **Strategy**: Stream processing with selective extraction
- **Target**: Extract 10,000 most active providers across key specialties
- **Performance**: Reduce to ~50MB for fast loading

#### **2. CVX Data Format**
- **Issue**: Pipe-delimited with inconsistent quoting
- **Strategy**: Custom parser with robust field splitting
- **Cleanup**: Remove inactive vaccines, normalize descriptions

#### **3. NDC Data Complexity**
- **Issue**: Multiple strength formats, inactive products
- **Strategy**: Filter to active products only, normalize strengths
- **Cleanup**: Remove discontinued drugs, standardize units

---

## 🎯 **Implementation Priorities**

### **Week 1: Core Foundation**
1. ✅ **Extract essential files** (NDC, LOINC, ZIP codes, CVX)
2. ✅ **Create database schema** with performance indexes
3. ✅ **Build data loading pipeline** with validation
4. ✅ **Implement basic field resolvers** for medications, labs, geography

### **Week 2: Enhanced Intelligence**
1. 🔄 **Download missing Census names data**
2. 🔄 **Process NPPES provider subset** (10K most common)
3. 🔄 **Create clinical correlation tables** (age-appropriate medications)
4. 🔄 **Add smart randomization** (70% normal labs, 20% mild abnormal, 10% severe)

### **Week 3: Performance Optimization**
1. 🔄 **Implement three-tier caching** (memory → SQLite → files)
2. 🔄 **Add background preloading** service
3. 🔄 **Create frequency-weighted selection** algorithms
4. 🔄 **Optimize database queries** with proper indexing

### **Week 4: Integration and Testing**
1. 🔄 **Integration testing** across all message types
2. 🔄 **Performance benchmarking** (<50ms field resolution)
3. 🔄 **Clinical realism validation** (healthcare professional review)
4. 🔄 **Documentation and examples**

---

## 🏗️ **Technical Implementation Guide**

### **Immediate Action Items**

#### **1. File Extraction Setup**
```bash
# Create organized extraction directories
mkdir -p pidgeon/src/Pidgeon.Data/extracted/{ndc,loinc,nppes,geographic,vaccines,fda,icd10}

# Priority extraction order:
1. ndctext.zip → extracted/ndc/product.txt
2. simplemaps_uszips_basicv1.911.zip → extracted/geographic/uszips.csv
3. cvx.txt → extracted/vaccines/cvx.txt (already done)
4. Loinc_2.81.zip → extracted/loinc/Loinc.csv
5. EOBZIP_2025_08.zip → extracted/fda/products.txt
```

#### **2. Database Setup Commands**
```bash
# Extend existing pidgeon.db with new tables
dotnet run --project Pidgeon.CLI -- database migrate --add-tables medications,lab_tests,providers,zip_codes,vaccines

# Load data from extracted files
dotnet run --project Pidgeon.CLI -- database populate --source extracted/ --parallel
```

#### **3. Field Resolver Integration**
```csharp
// In Startup.cs / Program.cs
services.AddScoped<IMedicationService, DatabaseMedicationService>();
services.AddScoped<IProviderService, DatabaseProviderService>();
services.AddScoped<IGeographicService, DatabaseGeographicService>();
services.AddScoped<ILabService, DatabaseLabService>();

// Enhanced resolver with database backing
services.AddScoped<IFieldValueResolver, EnhancedDemographicFieldResolver>(provider =>
    new EnhancedDemographicFieldResolver(
        provider.GetService<IMedicationService>(),
        provider.GetService<IProviderService>(),
        provider.GetService<ILabService>(),
        priority: 80 // Same as original DemographicFieldResolver
    ));
```

---

## 📊 **Success Metrics and Validation**

### **Quantitative Targets**
- **Field Resolution Speed**: <50ms average lookup time
- **Data Coverage**: 95% medication coverage for common drugs (top 1000)
- **Geographic Coverage**: 100% US ZIP code coverage
- **Provider Coverage**: Representative sample across all major specialties
- **Memory Usage**: <100MB for in-memory caches
- **Database Size**: <2GB for complete dataset

### **Qualitative Validation**
- **Clinical Realism**: Generated messages should be recognizable to healthcare professionals
- **Demographic Accuracy**: Name/address patterns should reflect actual US demographics
- **Medical Appropriateness**: Age-appropriate medications, specialty-appropriate diagnoses
- **Data Freshness**: Most recent datasets (2024-2025 versions)

### **Performance Benchmarks**
```csharp
// Target performance profile
var benchmarks = new[]
{
    ("medication.ndc", "<10ms", "In-memory cache hit"),
    ("provider.npi", "<20ms", "Database query with index"),
    ("lab.reference_range", "<30ms", "Calculated range based on LOINC"),
    ("patient.realistic_name", "<5ms", "Frequency-weighted selection"),
    ("geography.zip_to_city", "<15ms", "Indexed database lookup")
};
```

---

## 🚀 **Beyond P0: Future Enhancements**

### **Professional Tier Features**
- **SNOMED CT Integration**: Complete clinical terminology (requires UMLS license)
- **Drug Interaction Checking**: First Databank or clinical decision support
- **Advanced Demographics**: Income, education, insurance patterns
- **Seasonal Patterns**: Flu vaccines in fall, allergy medications in spring

### **Enterprise Tier Features**
- **Custom Dataset Import**: Client-specific drug formularies, provider networks
- **AI-Enhanced Generation**: ML models trained on real message patterns
- **Regulatory Compliance**: HIPAA audit trails, data lineage tracking
- **Multi-Region Support**: International postal codes, drug databases

---

**Ready for Implementation**: All critical dependencies identified, technical approach validated, and implementation path clearly defined. The foundation is solid for realistic healthcare message generation using authentic public domain datasets.

<function_calls>
<invoke name="TodoWrite">
<parameter name="todos">[{"content": "Identify gaps in current dataset coverage for realistic generation", "status": "completed", "activeForm": "Identifying gaps in current dataset coverage for realistic generation"}, {"content": "Create operationalization plan for integrating datasets into field resolvers", "status": "in_progress", "activeForm": "Creating operationalization plan for integrating datasets into field resolvers"}]