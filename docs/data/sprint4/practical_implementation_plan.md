# Practical Implementation Plan - Working with Extracted Datasets

## 🎯 **Current Situation Assessment**

### **✅ What We Have (Extracted & Ready)**

1. **Name Distributions** - EXCELLENT NEWS!
   - `FirstName.json` - General first names
   - `FirstNameMale.json` - Male-specific names
   - `FirstNameFemale.json` - Female-specific names
   - `LastName.json` - Surnames
   - **Impact**: NO NEED for Census data! Regional weighting is nice-to-have, not MVP critical.

2. **NPPES Provider Data** - PROBLEM: 11GB CSV extracted
   - Main file: `npidata_pfile_20050523-20250907.csv` (11GB)
   - **ACTION REQUIRED**: Must subset this TODAY

3. **NDC Medications** - READY
   - Located in: `ndctext/product.txt`
   - Ready for processing into top medications

4. **LOINC Lab Tests** - READY
   - Located in: `Loinc_2.81/LoincTable/Loinc.csv`
   - Ready for extraction of common tests

5. **ZIP Codes** - READY
   - Located in: `simplemaps_uszips_basicv1.911/uszips.csv`
   - Perfect for address generation

6. **CVX Vaccines** - READY
   - Already in text format: `cvx.txt`
   - 288 vaccines ready to use

7. **Orange Book** - READY
   - Located in: `EOBZIP_2025_08/products.txt`
   - Brand/generic relationships

### **❌ What's Missing (But Manageable)**

1. **ICD-10 Codes** - We have index but need actual codes
   - **WORKAROUND**: Hardcode top 50 diagnoses for MVP

2. **SNOMED/RxNorm** - Government shutdown blocking UMLS access
   - **WORKAROUND**: Use NDC codes only for now

---

## 🚀 **Immediate Action Plan**

### **PRIORITY 1: Subset the NPPES Data (TODAY)**

The 11GB NPPES file will crash any normal process. Here's a Python script to subset it:

```python
# subset_nppes.py - Run this TODAY in the datasets directory
import csv
import os
from collections import defaultdict

def subset_nppes_providers(input_file, output_file, max_providers=10000):
    """
    Extract a manageable subset of providers from the massive NPPES file
    Target: 10,000 providers across key specialties
    """

    # Priority specialties for MVP (covers most common scenarios)
    priority_taxonomies = {
        # Primary Care
        '208D00000X': 'General Practice',
        '207R00000X': 'Internal Medicine',
        '207Q00000X': 'Family Medicine',
        '208000000X': 'Pediatrics',

        # Specialists
        '207RC0000X': 'Cardiovascular Disease',
        '207L00000X': 'Anesthesiology',
        '207X00000X': 'Orthopedic Surgery',
        '207VX0201X': 'Obstetrics & Gynecology',

        # Emergency/Hospital
        '207P00000X': 'Emergency Medicine',
        '207RH0000X': 'Hospitalist',

        # Mental Health
        '2084P0800X': 'Psychiatry',

        # Diagnostics
        '207RX0202X': 'Diagnostic Radiology',
        '207ZP0102X': 'Pathology'
    }

    providers_by_specialty = defaultdict(list)
    providers_per_specialty = 1000  # Max per specialty

    print(f"Processing {input_file}...")
    row_count = 0

    with open(input_file, 'r', encoding='utf-8', errors='ignore') as infile:
        reader = csv.DictReader(infile)

        for row in reader:
            row_count += 1
            if row_count % 100000 == 0:
                print(f"Processed {row_count:,} rows...")

            # Only individual providers (not organizations)
            if row['Entity Type Code'] != '1':
                continue

            # Check primary taxonomy
            taxonomy = row.get('Healthcare Provider Taxonomy Code_1', '')

            if taxonomy in priority_taxonomies:
                specialty = priority_taxonomies[taxonomy]

                # Skip if we have enough of this specialty
                if len(providers_by_specialty[specialty]) >= providers_per_specialty:
                    continue

                # Extract key fields only
                provider = {
                    'npi': row['NPI'],
                    'last_name': row.get('Provider Last Name (Legal Name)', ''),
                    'first_name': row.get('Provider First Name', ''),
                    'middle_name': row.get('Provider Middle Name', ''),
                    'credential': row.get('Provider Credential Text', ''),
                    'specialty': specialty,
                    'taxonomy_code': taxonomy,
                    'practice_address': row.get('Provider First Line Business Practice Location Address', ''),
                    'practice_city': row.get('Provider Business Practice Location Address City Name', ''),
                    'practice_state': row.get('Provider Business Practice Location Address State Name', ''),
                    'practice_zip': row.get('Provider Business Practice Location Address Postal Code', '')[:5]
                }

                providers_by_specialty[specialty].append(provider)

                # Check if we have enough total
                total = sum(len(v) for v in providers_by_specialty.values())
                if total >= max_providers:
                    print(f"Reached target of {max_providers} providers")
                    break

    # Write subset to new CSV
    print(f"\nWriting subset to {output_file}...")

    with open(output_file, 'w', newline='', encoding='utf-8') as outfile:
        fieldnames = ['npi', 'last_name', 'first_name', 'middle_name', 'credential',
                     'specialty', 'taxonomy_code', 'practice_address', 'practice_city',
                     'practice_state', 'practice_zip']
        writer = csv.DictWriter(outfile, fieldnames=fieldnames)
        writer.writeheader()

        for specialty, providers in providers_by_specialty.items():
            print(f"  {specialty}: {len(providers)} providers")
            for provider in providers:
                writer.writerow(provider)

    total_written = sum(len(v) for v in providers_by_specialty.values())
    print(f"\nComplete! Extracted {total_written} providers to {output_file}")
    print(f"File size: {os.path.getsize(output_file) / 1024 / 1024:.1f} MB")

if __name__ == '__main__':
    input_path = 'NPPES_Data_Dissemination_September_2025/npidata_pfile_20050523-20250907.csv'
    output_path = 'providers_subset.csv'

    subset_nppes_providers(input_path, output_path, max_providers=10000)
```

### **PRIORITY 2: Create Top 50 ICD-10 Diagnoses (Hardcoded)**

Since we can't get the full ICD-10 file yet, here's a hardcoded solution:

```python
# create_common_diagnoses.py
import json

# Top 50 most common ICD-10 codes in US healthcare
common_diagnoses = [
    # Chronic conditions
    {"code": "I10", "description": "Essential (primary) hypertension", "category": "chronic"},
    {"code": "E11.9", "description": "Type 2 diabetes mellitus without complications", "category": "chronic"},
    {"code": "E78.5", "description": "Hyperlipidemia, unspecified", "category": "chronic"},
    {"code": "K21.9", "description": "Gastro-esophageal reflux disease without esophagitis", "category": "chronic"},
    {"code": "M79.3", "description": "Myalgia", "category": "chronic"},
    {"code": "F32.9", "description": "Major depressive disorder, single episode, unspecified", "category": "mental_health"},
    {"code": "F41.9", "description": "Anxiety disorder, unspecified", "category": "mental_health"},

    # Acute conditions
    {"code": "J06.9", "description": "Acute upper respiratory infection, unspecified", "category": "acute"},
    {"code": "U07.1", "description": "COVID-19", "category": "acute"},
    {"code": "J20.9", "description": "Acute bronchitis, unspecified", "category": "acute"},
    {"code": "N39.0", "description": "Urinary tract infection, site not specified", "category": "acute"},
    {"code": "R50.9", "description": "Fever, unspecified", "category": "acute"},
    {"code": "R05", "description": "Cough", "category": "symptom"},
    {"code": "R51.9", "description": "Headache, unspecified", "category": "symptom"},

    # Preventive/Screening
    {"code": "Z00.00", "description": "General adult medical exam without abnormal findings", "category": "preventive"},
    {"code": "Z12.31", "description": "Screening mammogram for malignant neoplasm of breast", "category": "screening"},
    {"code": "Z13.1", "description": "Screening for diabetes mellitus", "category": "screening"},
    {"code": "Z23", "description": "Encounter for immunization", "category": "preventive"},

    # Common symptoms
    {"code": "M54.5", "description": "Low back pain", "category": "symptom"},
    {"code": "R06.02", "description": "Shortness of breath", "category": "symptom"},
    {"code": "R07.9", "description": "Chest pain, unspecified", "category": "symptom"},
    {"code": "M25.511", "description": "Pain in right shoulder", "category": "symptom"},
    {"code": "R42", "description": "Dizziness and giddiness", "category": "symptom"},

    # More chronic conditions
    {"code": "J44.1", "description": "COPD with acute exacerbation", "category": "chronic"},
    {"code": "J45.909", "description": "Unspecified asthma, uncomplicated", "category": "chronic"},
    {"code": "N18.3", "description": "Chronic kidney disease, stage 3", "category": "chronic"},
    {"code": "E03.9", "description": "Hypothyroidism, unspecified", "category": "chronic"},
    {"code": "G47.33", "description": "Obstructive sleep apnea", "category": "chronic"},

    # Cardiovascular
    {"code": "I48.91", "description": "Unspecified atrial fibrillation", "category": "cardiovascular"},
    {"code": "I25.10", "description": "Atherosclerotic heart disease of native coronary artery", "category": "cardiovascular"},
    {"code": "I50.9", "description": "Heart failure, unspecified", "category": "cardiovascular"},

    # Musculoskeletal
    {"code": "M17.11", "description": "Unilateral primary osteoarthritis, right knee", "category": "musculoskeletal"},
    {"code": "M81.0", "description": "Age-related osteoporosis without current pathological fracture", "category": "musculoskeletal"},

    # Infections
    {"code": "B97.29", "description": "Other coronavirus as the cause of diseases", "category": "infection"},
    {"code": "A41.9", "description": "Sepsis, unspecified organism", "category": "infection"},
    {"code": "J18.9", "description": "Pneumonia, unspecified organism", "category": "infection"},

    # Pregnancy-related
    {"code": "Z33.1", "description": "Pregnant state, incidental", "category": "pregnancy"},
    {"code": "O80", "description": "Single liveborn", "category": "pregnancy"},

    # Behavioral health
    {"code": "F17.210", "description": "Nicotine dependence, cigarettes, uncomplicated", "category": "behavioral"},
    {"code": "F10.10", "description": "Alcohol use disorder, mild", "category": "behavioral"},
    {"code": "G47.00", "description": "Insomnia, unspecified", "category": "behavioral"},

    # Pediatric-specific
    {"code": "J00", "description": "Acute nasopharyngitis [common cold]", "category": "pediatric"},
    {"code": "H66.90", "description": "Otitis media, unspecified, unspecified ear", "category": "pediatric"},

    # Administrative
    {"code": "Z51.11", "description": "Encounter for antineoplastic chemotherapy", "category": "administrative"},
    {"code": "Z79.899", "description": "Other long term (current) drug therapy", "category": "administrative"},

    # Additional common
    {"code": "R73.03", "description": "Prediabetes", "category": "metabolic"},
    {"code": "D64.9", "description": "Anemia, unspecified", "category": "hematologic"},
    {"code": "N40.0", "description": "Benign prostatic hyperplasia without lower urinary tract symptoms", "category": "genitourinary"},
    {"code": "K58.0", "description": "Irritable bowel syndrome with diarrhea", "category": "gastrointestinal"},
    {"code": "L30.9", "description": "Dermatitis, unspecified", "category": "dermatologic"}
]

# Save to JSON file
with open('common_diagnoses.json', 'w') as f:
    json.dump({
        'diagnoses': common_diagnoses,
        'metadata': {
            'count': len(common_diagnoses),
            'source': 'Hardcoded based on US healthcare statistics',
            'note': 'Replace with full ICD-10 when available'
        }
    }, f, indent=2)

print(f"Created {len(common_diagnoses)} common diagnoses")
```

---

## 🗄️ **Database Schema & Loading Strategy**

### **Step 1: Extend Existing SQLite Database**

```sql
-- extend_pidgeon_db.sql
-- Run this against the existing pidgeon.db

-- Names (using existing JSON files)
CREATE TABLE IF NOT EXISTS names_first (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT NOT NULL,
    gender TEXT, -- 'M', 'F', or NULL for unisex
    frequency_rank INTEGER,
    UNIQUE(name, gender)
);

CREATE TABLE IF NOT EXISTS names_last (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT NOT NULL UNIQUE,
    frequency_rank INTEGER
);

-- Medications (from NDC)
CREATE TABLE IF NOT EXISTS medications (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    ndc_code TEXT UNIQUE,
    proprietary_name TEXT,
    nonproprietary_name TEXT,
    dosage_form TEXT,
    strength TEXT,
    route TEXT,
    labeler_name TEXT,
    active_ingredient TEXT,
    frequency_rank INTEGER -- We'll calculate this
);

-- Lab Tests (from LOINC)
CREATE TABLE IF NOT EXISTS lab_tests (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    loinc_code TEXT UNIQUE NOT NULL,
    long_common_name TEXT,
    component TEXT,
    property TEXT,
    system TEXT,
    scale_type TEXT,
    units TEXT,
    normal_low REAL,  -- We'll add synthetic ranges
    normal_high REAL,
    frequency_rank INTEGER
);

-- Providers (from NPPES subset)
CREATE TABLE IF NOT EXISTS providers (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    npi TEXT UNIQUE NOT NULL,
    last_name TEXT,
    first_name TEXT,
    middle_name TEXT,
    credential TEXT,
    specialty TEXT,
    taxonomy_code TEXT,
    practice_city TEXT,
    practice_state TEXT,
    practice_zip TEXT
);

-- Diagnoses (hardcoded for now)
CREATE TABLE IF NOT EXISTS diagnoses (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    icd10_code TEXT UNIQUE NOT NULL,
    description TEXT,
    category TEXT,
    frequency_rank INTEGER
);

-- ZIP codes (from simplemaps)
CREATE TABLE IF NOT EXISTS zip_codes (
    zip TEXT PRIMARY KEY,
    city TEXT,
    state_id TEXT,
    state_name TEXT,
    county_name TEXT,
    latitude REAL,
    longitude REAL,
    population INTEGER,
    timezone TEXT
);

-- Create indexes for performance
CREATE INDEX idx_names_first_gender ON names_first(gender);
CREATE INDEX idx_names_first_rank ON names_first(frequency_rank);
CREATE INDEX idx_names_last_rank ON names_last(frequency_rank);
CREATE INDEX idx_medications_rank ON medications(frequency_rank);
CREATE INDEX idx_lab_tests_rank ON lab_tests(frequency_rank);
CREATE INDEX idx_providers_specialty ON providers(specialty);
CREATE INDEX idx_diagnoses_category ON diagnoses(category);
CREATE INDEX idx_zip_codes_state ON zip_codes(state_id);
```

### **Step 2: Data Loading Script**

```python
# load_data_to_sqlite.py
import sqlite3
import csv
import json
from pathlib import Path

def load_all_data(db_path='pidgeon.db', data_dir='datasets'):
    """Load all extracted data into SQLite database"""

    conn = sqlite3.connect(db_path)
    cur = conn.cursor()

    # 1. Load Names from JSON files
    print("Loading names...")
    load_names(cur, data_dir)

    # 2. Load Provider subset
    print("Loading providers...")
    if Path(f'{data_dir}/providers_subset.csv').exists():
        load_providers(cur, f'{data_dir}/providers_subset.csv')
    else:
        print("  WARNING: Run subset_nppes.py first!")

    # 3. Load Medications (top 1000)
    print("Loading medications...")
    load_medications(cur, f'{data_dir}/ndctext/product.txt')

    # 4. Load Lab Tests (top 500)
    print("Loading lab tests...")
    load_lab_tests(cur, f'{data_dir}/Loinc_2.81/LoincTable/Loinc.csv')

    # 5. Load ZIP codes
    print("Loading ZIP codes...")
    load_zip_codes(cur, f'{data_dir}/simplemaps_uszips_basicv1.911/uszips.csv')

    # 6. Load Diagnoses (hardcoded)
    print("Loading diagnoses...")
    load_diagnoses(cur, f'{data_dir}/common_diagnoses.json')

    conn.commit()
    conn.close()
    print("\nAll data loaded successfully!")

def load_names(cur, data_dir):
    """Load first and last names from JSON files"""

    # Load male names
    with open(f'../standards/hl7v23/tables/FirstNameMale.json') as f:
        male_names = json.load(f)
        for i, item in enumerate(male_names['values'][:500]):  # Top 500
            cur.execute('''
                INSERT OR IGNORE INTO names_first (name, gender, frequency_rank)
                VALUES (?, 'M', ?)
            ''', (item['value'], i + 1))

    # Load female names
    with open(f'../standards/hl7v23/tables/FirstNameFemale.json') as f:
        female_names = json.load(f)
        for i, item in enumerate(female_names['values'][:500]):  # Top 500
            cur.execute('''
                INSERT OR IGNORE INTO names_first (name, gender, frequency_rank)
                VALUES (?, 'F', ?)
            ''', (item['value'], i + 1))

    # Load last names
    with open(f'../standards/hl7v23/tables/LastName.json') as f:
        last_names = json.load(f)
        for i, item in enumerate(last_names['values'][:1000]):  # Top 1000
            cur.execute('''
                INSERT OR IGNORE INTO names_last (name, frequency_rank)
                VALUES (?, ?)
            ''', (item['value'], i + 1))

    print(f"  Loaded {cur.rowcount} names")

def load_medications(cur, file_path, limit=1000):
    """Load top medications from NDC product file"""

    # Common medications list for prioritization
    common_generics = [
        'lisinopril', 'metformin', 'atorvastatin', 'amlodipine', 'omeprazole',
        'simvastatin', 'levothyroxine', 'metoprolol', 'losartan', 'gabapentin'
    ]

    with open(file_path, 'r', encoding='utf-8') as f:
        reader = csv.DictReader(f, delimiter='\t')

        count = 0
        for row in reader:
            if count >= limit:
                break

            # Skip if not active
            if row.get('ENDMARKETINGDATE'):
                continue

            # Prioritize common medications
            generic = (row.get('NONPROPRIETARYNAME') or '').lower()
            rank = 999
            for i, common in enumerate(common_generics):
                if common in generic:
                    rank = i + 1
                    break

            cur.execute('''
                INSERT OR IGNORE INTO medications (
                    ndc_code, proprietary_name, nonproprietary_name,
                    dosage_form, strength, route, labeler_name,
                    active_ingredient, frequency_rank
                ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)
            ''', (
                row.get('PRODUCTNDC'),
                row.get('PROPRIETARYNAME'),
                row.get('NONPROPRIETARYNAME'),
                row.get('DOSAGEFORMNAME'),
                row.get('ACTIVE_NUMERATOR_STRENGTH'),
                row.get('ROUTENAME'),
                row.get('LABELERNAME'),
                row.get('SUBSTANCENAME'),
                rank + count
            ))
            count += 1

    print(f"  Loaded {count} medications")

# Similar functions for lab_tests, providers, zip_codes, diagnoses...
```

---

## 🚀 **Three-Tier Caching Implementation**

### **C# Implementation for Pidgeon**

```csharp
// Services/Caching/ThreeTierCacheService.cs
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;

public interface IThreeTierCacheService
{
    Task<T?> GetAsync<T>(string key, string category);
    Task SetAsync<T>(string key, T value, string category);
}

public class ThreeTierCacheService : IThreeTierCacheService
{
    // Tier 1: In-Memory Cache (Ultra-fast)
    private readonly IMemoryCache _memoryCache;
    private readonly ConcurrentDictionary<string, DateTime> _accessTracker;

    // Tier 2: SQLite Database
    private readonly ISqliteRepository _database;

    // Tier 3: External files (fallback)
    private readonly IDataFileRepository _files;

    // Configuration
    private readonly int _memoryItemLimit = 1000;
    private readonly TimeSpan _memoryDuration = TimeSpan.FromHours(1);

    public ThreeTierCacheService(
        IMemoryCache memoryCache,
        ISqliteRepository database,
        IDataFileRepository files)
    {
        _memoryCache = memoryCache;
        _database = database;
        _files = files;
        _accessTracker = new ConcurrentDictionary<string, DateTime>();

        // Preload common data at startup
        Task.Run(PreloadCommonDataAsync);
    }

    public async Task<T?> GetAsync<T>(string key, string category)
    {
        var cacheKey = $"{category}:{key}";

        // L1: Memory Cache (<1ms)
        if (_memoryCache.TryGetValue<T>(cacheKey, out var cached))
        {
            TrackAccess(cacheKey);
            return cached;
        }

        // L2: Database (<20ms)
        var dbResult = await GetFromDatabaseAsync<T>(category, key);
        if (dbResult != null)
        {
            // Promote to memory if frequently accessed
            if (IsFrequentlyAccessed(cacheKey))
            {
                _memoryCache.Set(cacheKey, dbResult, _memoryDuration);
            }
            return dbResult;
        }

        // L3: Files/External (<200ms)
        var fileResult = await _files.GetAsync<T>(category, key);
        if (fileResult != null)
        {
            // Backfill caches
            await SetInDatabaseAsync(category, key, fileResult);
            _memoryCache.Set(cacheKey, fileResult, TimeSpan.FromMinutes(10));
            return fileResult;
        }

        return default(T);
    }

    private async Task PreloadCommonDataAsync()
    {
        // Preload top 100 medications
        var medications = await _database.QueryAsync<Medication>(
            "SELECT * FROM medications WHERE frequency_rank <= 100"
        );
        foreach (var med in medications)
        {
            _memoryCache.Set($"medication:{med.NdcCode}", med, TimeSpan.FromHours(24));
        }

        // Preload top 50 lab tests
        var labs = await _database.QueryAsync<LabTest>(
            "SELECT * FROM lab_tests WHERE frequency_rank <= 50"
        );
        foreach (var lab in labs)
        {
            _memoryCache.Set($"lab:{lab.LoincCode}", lab, TimeSpan.FromHours(24));
        }

        // Preload common names
        var firstNames = await _database.QueryAsync<string>(
            "SELECT name FROM names_first WHERE frequency_rank <= 100"
        );
        _memoryCache.Set("names:first:common", firstNames.ToArray(), TimeSpan.FromHours(24));

        var lastNames = await _database.QueryAsync<string>(
            "SELECT name FROM names_last WHERE frequency_rank <= 100"
        );
        _memoryCache.Set("names:last:common", lastNames.ToArray(), TimeSpan.FromHours(24));
    }

    private void TrackAccess(string key)
    {
        _accessTracker[key] = DateTime.UtcNow;

        // Cleanup old entries periodically
        if (_accessTracker.Count > _memoryItemLimit * 2)
        {
            var cutoff = DateTime.UtcNow.AddHours(-1);
            var toRemove = _accessTracker
                .Where(kvp => kvp.Value < cutoff)
                .Select(kvp => kvp.Key);

            foreach (var oldKey in toRemove)
            {
                _accessTracker.TryRemove(oldKey, out _);
            }
        }
    }

    private bool IsFrequentlyAccessed(string key)
    {
        if (!_accessTracker.TryGetValue(key, out var lastAccess))
            return false;

        // Consider frequently accessed if accessed within last 5 minutes
        return DateTime.UtcNow - lastAccess < TimeSpan.FromMinutes(5);
    }
}
```

---

## 📋 **Next Steps - Priority Order**

### **TODAY (Priority 1)**
1. **Run NPPES subset script** - Get providers down to manageable 10K
2. **Run diagnoses script** - Create hardcoded top 50 ICD-10 codes
3. **Execute SQL schema** - Extend pidgeon.db with new tables

### **TOMORROW (Priority 2)**
4. **Run data loading script** - Populate all tables from extracted files
5. **Test data queries** - Verify fast lookups work
6. **Integrate with field resolvers** - Wire up to existing resolver chain

### **THIS WEEK (Priority 3)**
7. **Implement caching service** - Three-tier architecture
8. **Add realistic patterns** - 70/20/10 distributions
9. **Integration testing** - Full message generation with real data

---

## 🎯 **Key Insights**

1. **Names are SOLVED** - Your existing JSON files eliminate need for Census data. Regional weighting is nice-to-have, not critical for MVP.

2. **NPPES is the BLOCKER** - Must subset TODAY or it will crash everything. The script above will handle it.

3. **ICD-10 workaround is FINE** - Top 50 hardcoded diagnoses cover majority of cases for MVP.

4. **Government shutdown is OK** - SNOMED/RxNorm are nice-to-have. NDC codes are sufficient for MVP.

5. **Three-tier caching is ESSENTIAL** - In-memory for speed, SQLite for completeness, files for fallback.

The path forward is clear and achievable with what you have!