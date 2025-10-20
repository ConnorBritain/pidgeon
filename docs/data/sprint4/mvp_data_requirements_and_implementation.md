# MVP Data Requirements & Technical Implementation Guide

## 🎯 **Executive Summary**

This document defines the **minimum viable dataset** required for Pidgeon's MVP to generate realistic healthcare messages that healthcare professionals will recognize as authentic. It includes specific technical implementation patterns for data extraction, processing, and integration.

**Key Insight**: We don't need perfect coverage - we need the **right coverage** of the most common scenarios that represent 80% of real-world healthcare messaging.

---

## 📊 **MVP Data Requirements - The 80/20 Rule**

### **What We Actually Need for MVP Success**

The goal isn't to have every possible medication or diagnosis - it's to have the **most common ones** that healthcare professionals encounter daily. Here's what actually matters:

### **🏆 Tier 1: Absolutely Critical (MVP Blockers)**

#### **1. Top 200 Medications (Covers 70% of prescriptions)**
- **Source**: Extract from NDC `product.txt` (already have)
- **Filter Criteria**: Most prescribed medications by volume
- **Key Fields**: NDC code, generic name, brand name, strength, route, dosage form
- **Storage**: In-memory array for <5ms access
- **Example Coverage**: Lisinopril, Metformin, Atorvastatin, Levothyroxine, Amlodipine

#### **2. Top 100 Lab Tests (Covers 85% of orders)**
- **Source**: Extract from LOINC CSV (already have)
- **Filter**: CBC, BMP, CMP, Lipid Panel, TSH, HbA1c, UA, PT/INR
- **Key Fields**: LOINC code, test name, units, typical result format
- **Storage**: In-memory dictionary
- **Critical**: Need synthetic reference ranges (can hardcode initially)

#### **3. Top 50 Common Diagnoses (Covers 60% of encounters)**
- **Source**: ICD-10 codes (**MISSING - HIGH PRIORITY**)
- **Examples**: Hypertension (I10), Type 2 Diabetes (E11.9), COVID-19 (U07.1)
- **Storage**: In-memory lookup table
- **Workaround**: Can hardcode top 50 until we get full ICD-10 file

#### **4. Realistic Patient Names (1000 combinations)**
- **Source**: Census data (**MISSING - BUT WORKABLE**)
- **Workaround**: Use popular names lists from public sources
- **Implementation**: 100 first names × 100 last names = 10,000 combinations
- **Storage**: In-memory arrays with weighted selection

#### **5. Valid Geographic Data (Already have)**
- **Source**: `uszips.csv` - complete US ZIP codes
- **Usage**: City/state/ZIP correlation for addresses
- **Storage**: SQLite with indexed lookups

### **🥈 Tier 2: Important but Not Blocking**

#### **6. Provider Names & Specialties (Subset of 1000)**
- **Source**: NPPES (have but need streaming extraction)
- **Strategy**: Extract top specialties only
- **Storage**: SQLite table with specialty index

#### **7. Insurance Information**
- **Workaround**: Hardcode top 10 payers (BCBS, UHC, Aetna, etc.)
- **Storage**: Static configuration file

#### **8. Vaccine Codes (Already have)**
- **Source**: `cvx.txt` - 288 vaccines
- **Storage**: In-memory for immunization messages

### **🥉 Tier 3: Nice to Have (Post-MVP)**

- Drug interaction data (RxNorm)
- Complete SNOMED terminology
- Reference range distributions (NHANES)
- Specialty-specific correlations

---

## 🏗️ **Technical Implementation Patterns**

### **1. NPPES Stream Processing (Critical Pattern)**

The NPPES file is 11GB uncompressed. We CANNOT load this into memory or even extract it fully. Here's the solution:

```python
# stream_process_nppes.py
import csv
import zipfile
from collections import defaultdict

def extract_provider_subset(zip_path, output_path, max_providers=10000):
    """
    Stream process NPPES ZIP without full extraction
    Memory usage: ~50MB regardless of file size
    """

    # Priority specialties for MVP
    target_specialties = {
        '208D00000X',  # General Practice
        '207R00000X',  # Internal Medicine
        '207Q00000X',  # Family Medicine
        '261QP2300X',  # Primary Care Clinic
        '207L00000X',  # Emergency Medicine
        '208000000X',  # Pediatrics
    }

    providers_by_specialty = defaultdict(list)

    with zipfile.ZipFile(zip_path, 'r') as zf:
        # Open the CSV directly from ZIP (no extraction)
        with zf.open('npidata_pfile_20050523-20250907.csv', 'r') as csvfile:
            # Use text mode wrapper for CSV reader
            text_wrapper = io.TextIOWrapper(csvfile, encoding='utf-8')
            reader = csv.DictReader(text_wrapper)

            for row in reader:
                # Check if active provider
                if row['Entity Type Code'] == '1':  # Individual
                    taxonomy = row['Healthcare Provider Taxonomy Code_1']

                    if taxonomy in target_specialties:
                        provider = {
                            'npi': row['NPI'],
                            'last_name': row['Provider Last Name (Legal Name)'],
                            'first_name': row['Provider First Name'],
                            'specialty': taxonomy,
                            'city': row['Provider Business Practice Location Address City Name'],
                            'state': row['Provider Business Practice Location Address State Name'],
                        }

                        providers_by_specialty[taxonomy].append(provider)

                        # Stop when we have enough
                        total = sum(len(v) for v in providers_by_specialty.values())
                        if total >= max_providers:
                            break

    # Save subset to CSV
    with open(output_path, 'w', newline='') as f:
        fieldnames = ['npi', 'last_name', 'first_name', 'specialty', 'city', 'state']
        writer = csv.DictWriter(f, fieldnames=fieldnames)
        writer.writeheader()

        for providers in providers_by_specialty.values():
            for provider in providers[:2000]:  # Max 2000 per specialty
                writer.writerow(provider)

    print(f"Extracted {total} providers to {output_path}")
```

### **2. Three-Tier Caching Architecture**

```csharp
// ThreeTierCache.cs - Smart caching with fallback
public class ThreeTierCache<T>
{
    // Tier 1: In-Memory (Ultra-fast, limited size)
    private readonly MemoryCache _memoryCache;
    private readonly int _memoryCacheSize = 1000;

    // Tier 2: SQLite (Fast, complete dataset)
    private readonly ISqliteRepository _database;

    // Tier 3: File/API (Slow, comprehensive)
    private readonly IExternalDataSource _external;

    public async Task<T> GetAsync(string key)
    {
        // L1 Cache: Memory (< 1ms)
        if (_memoryCache.TryGetValue(key, out T cached))
        {
            UpdateAccessFrequency(key); // LRU tracking
            return cached;
        }

        // L2 Cache: Database (< 20ms)
        var dbResult = await _database.GetAsync<T>(key);
        if (dbResult != null)
        {
            // Promote to memory if frequently accessed
            if (IsFrequentlyAccessed(key))
            {
                _memoryCache.Set(key, dbResult, TimeSpan.FromHours(1));
            }
            return dbResult;
        }

        // L3: External source (< 200ms)
        var external = await _external.FetchAsync(key);
        if (external != null)
        {
            // Backfill caches
            await _database.SetAsync(key, external);
            _memoryCache.Set(key, external, TimeSpan.FromMinutes(10));
        }

        return external;
    }
}
```

### **3. In-Memory Dataset Loading Pattern**

```csharp
// InMemoryDatasets.cs - Preload common data at startup
public class InMemoryDatasets
{
    // Static readonly for thread safety and performance
    private static readonly Lazy<InMemoryDatasets> _instance =
        new(() => new InMemoryDatasets());

    public static InMemoryDatasets Instance => _instance.Value;

    // Preloaded datasets (loaded once at startup)
    public readonly string[] CommonMedications;
    public readonly Dictionary<string, LabTest> CommonLabTests;
    public readonly (string First, string Last, double Weight)[] WeightedNames;
    public readonly Dictionary<string, DiagnosisInfo> CommonDiagnoses;

    private InMemoryDatasets()
    {
        // Load at construction (happens once)
        CommonMedications = LoadTopMedications();
        CommonLabTests = LoadCommonLabTests();
        WeightedNames = LoadWeightedNames();
        CommonDiagnoses = LoadCommonDiagnoses();
    }

    private string[] LoadTopMedications()
    {
        // Hardcoded top 200 medications for MVP
        // Later: Load from database
        return new[]
        {
            "Lisinopril 10mg tablet",
            "Metformin 500mg tablet",
            "Atorvastatin 20mg tablet",
            "Levothyroxine 50mcg tablet",
            "Amlodipine 5mg tablet",
            "Metoprolol 25mg tablet",
            "Omeprazole 20mg capsule",
            "Simvastatin 40mg tablet",
            "Losartan 50mg tablet",
            "Albuterol HFA 90mcg inhaler",
            // ... top 200
        };
    }

    private Dictionary<string, LabTest> LoadCommonLabTests()
    {
        // Most common lab tests with synthetic ranges
        return new Dictionary<string, LabTest>
        {
            ["2345-7"] = new LabTest
            {
                LoincCode = "2345-7",
                Name = "Glucose",
                Units = "mg/dL",
                NormalRange = "70-100",
                GenerateValue = () => Random.Shared.Next(70, 100)
            },
            ["2093-3"] = new LabTest
            {
                LoincCode = "2093-3",
                Name = "Cholesterol",
                Units = "mg/dL",
                NormalRange = "<200",
                GenerateValue = () => Random.Shared.Next(150, 200)
            },
            // ... top 100 tests
        };
    }
}
```

### **4. Smart Field Resolution with Realistic Patterns**

```csharp
// RealisticFieldResolver.cs - Context-aware value generation
public class RealisticFieldResolver : IFieldValueResolver
{
    private readonly InMemoryDatasets _datasets = InMemoryDatasets.Instance;

    public async Task<string?> ResolveAsync(FieldResolutionContext context)
    {
        return context.SemanticPath switch
        {
            // Medications - weighted by frequency
            "medication.name" => GetWeightedMedication(context),

            // Lab values - normal distribution
            "lab.result" => GenerateRealisticLabValue(context),

            // Names - demographic appropriate
            "patient.name" => GenerateDemographicName(context),

            // Diagnoses - age/gender appropriate
            "diagnosis.code" => GetContextualDiagnosis(context),

            _ => null
        };
    }

    private string GetWeightedMedication(FieldResolutionContext context)
    {
        // 70% from top 20, 25% from next 80, 5% from rest
        var roll = Random.Shared.NextDouble();

        if (roll < 0.70)
            return _datasets.CommonMedications[Random.Shared.Next(0, 20)];
        else if (roll < 0.95)
            return _datasets.CommonMedications[Random.Shared.Next(20, 100)];
        else
            return _datasets.CommonMedications[Random.Shared.Next(100, 200)];
    }

    private string GenerateRealisticLabValue(FieldResolutionContext context)
    {
        var testCode = context.ResolvedFields.GetValueOrDefault("lab.code");
        if (_datasets.CommonLabTests.TryGetValue(testCode, out var test))
        {
            // 70% normal, 20% slightly abnormal, 10% critical
            var roll = Random.Shared.NextDouble();

            if (roll < 0.70)
                return test.GenerateNormalValue();
            else if (roll < 0.90)
                return test.GenerateAbnormalValue(mild: true);
            else
                return test.GenerateCriticalValue();
        }

        return "100"; // Default
    }
}
```

---

## 🚀 **MVP Implementation Roadmap**

### **Day 1-2: Data Extraction & Processing**

```bash
# 1. Create extraction directory structure
mkdir -p extracted/{core,cache,staging}

# 2. Extract core files (< 1 hour)
python3 extract_ndc_top_medications.py  # Top 200 from product.txt
python3 extract_loinc_common_tests.py   # Top 100 from Loinc.csv
python3 extract_zip_codes.py            # All from uszips.csv

# 3. Stream process NPPES (< 30 minutes)
python3 stream_process_nppes.py \
    --input NPPES_Data_Dissemination_September_2025.zip \
    --output extracted/core/providers_subset.csv \
    --max-providers 10000

# 4. Create hardcoded supplements
python3 generate_common_diagnoses.py    # Top 50 ICD-10 codes
python3 generate_common_names.py        # Weighted name lists
```

### **Day 3-4: Database Setup & Loading**

```sql
-- Minimal MVP schema
CREATE TABLE IF NOT EXISTS medications (
    id INTEGER PRIMARY KEY,
    ndc TEXT,
    name TEXT,
    strength TEXT,
    route TEXT,
    frequency_rank INTEGER
);

CREATE TABLE IF NOT EXISTS lab_tests (
    loinc_code TEXT PRIMARY KEY,
    name TEXT,
    units TEXT,
    normal_low REAL,
    normal_high REAL,
    frequency_rank INTEGER
);

CREATE TABLE IF NOT EXISTS providers (
    npi TEXT PRIMARY KEY,
    last_name TEXT,
    first_name TEXT,
    specialty TEXT
);

-- Indexes for performance
CREATE INDEX idx_med_frequency ON medications(frequency_rank);
CREATE INDEX idx_lab_frequency ON lab_tests(frequency_rank);
CREATE INDEX idx_provider_specialty ON providers(specialty);
```

### **Day 5: Integration & Testing**

```csharp
// Startup.cs - Wire everything together
public void ConfigureServices(IServiceCollection services)
{
    // Register in-memory datasets (singleton)
    services.AddSingleton<InMemoryDatasets>();

    // Register caching service
    services.AddSingleton<IMemoryCache, MemoryCache>();
    services.AddScoped<IThreeTierCache, ThreeTierCache>();

    // Register enhanced resolver
    services.AddScoped<IFieldValueResolver, RealisticFieldResolver>(sp =>
        new RealisticFieldResolver(
            sp.GetService<InMemoryDatasets>(),
            sp.GetService<IThreeTierCache>(),
            priority: 85  // Higher than basic resolver
        ));
}
```

---

## 📊 **What "Realistic" Actually Means for MVP**

### **Realistic ≠ Perfect**

For MVP, "realistic" means messages that:
1. **Use real codes** (NDC, LOINC, ICD-10) that systems recognize
2. **Have believable patterns** (common meds, normal lab values)
3. **Follow demographic norms** (age-appropriate, geographic correlation)
4. **Avoid obvious fakes** (no "Test Patient" or "Lorem Ipsum")

### **MVP Coverage Targets**

| Component | MVP Target | Ideal Target | Current Status |
|-----------|------------|--------------|----------------|
| Medications | Top 200 (70% coverage) | Top 1000 (95%) | ✅ Have NDC data |
| Lab Tests | Top 100 (85% coverage) | Top 500 (95%) | ✅ Have LOINC data |
| Diagnoses | Top 50 (60% coverage) | Top 500 (90%) | ❌ Need ICD-10 codes |
| Provider Names | 1000 (variety) | 10,000 | ⚠️ Need streaming |
| Patient Names | 1000 combos | 10,000 | ❌ Need Census or lists |
| Addresses | All US ZIPs | International | ✅ Have ZIP data |

### **Acceptable Shortcuts for MVP**

1. **Hardcode top 50 diagnoses** until ICD-10 downloaded
2. **Use popular name lists** instead of Census data
3. **Generate synthetic lab ranges** based on common knowledge
4. **Limit providers to 10 specialties** instead of all
5. **Skip drug interactions** until RxNorm available

---

## 🎯 **Success Criteria**

### **Technical Metrics**
- ✅ Generate 100 messages in < 5 seconds
- ✅ Field resolution < 50ms per field
- ✅ Memory usage < 100MB for caches
- ✅ Zero external API dependencies

### **Quality Metrics**
- ✅ Real NDC codes for medications
- ✅ Valid LOINC codes for lab tests
- ✅ Proper city/state/ZIP correlation
- ✅ Age-appropriate clinical data
- ✅ No "Test Patient" or placeholder data

### **Business Metrics**
- ✅ Healthcare professional says "this looks real"
- ✅ Messages validate in target systems
- ✅ Supports top 6 message types (ADT, ORU, ORM, RDE, SIU, MDM)
- ✅ Free tier compelling enough to drive adoption

---

## 🚧 **Known Limitations & Workarounds**

### **Current Gaps**

| Gap | Impact | Workaround | Long-term Solution |
|-----|--------|------------|-------------------|
| Census names | Generic names | Use popular name lists | Download Census data |
| ICD-10 codes | No diagnosis codes | Hardcode top 50 | Download CMS file |
| Lab ranges | Unrealistic values | Synthetic ranges | NHANES data |
| RxNorm | No drug relationships | NDC only | UMLS registration |
| SNOMED | Limited terminology | Use simple terms | UMLS license |

### **Acceptable MVP Limitations**
1. **English only** - International later
2. **US addresses only** - Global expansion post-MVP
3. **Common scenarios only** - Edge cases in v2
4. **No temporal patterns** - Seasonal/time patterns later
5. **Basic correlations** - Advanced AI patterns in Pro tier

---

## 🎬 **Next Immediate Actions**

1. **TODAY**: Run NPPES streaming extraction script
2. **TODAY**: Create hardcoded diagnosis list (top 50)
3. **TOMORROW**: Load extracted data into SQLite
4. **TOMORROW**: Implement in-memory caching
5. **THIS WEEK**: Integration test with real message generation

**The MVP doesn't need to be perfect - it needs to be "good enough" that healthcare professionals recognize it as realistic. We can achieve that with the data we have plus smart patterns and shortcuts.**