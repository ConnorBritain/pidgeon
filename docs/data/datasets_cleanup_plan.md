# Datasets Folder Cleanup and Restructuring Plan

**Date**: October 23, 2025
**Status**: Ready for Execution
**Goal**: Organize datasets into tiered structure, create free tier curated data, validate quality

---

## 🔍 **Current State Analysis**

### **What We Have**
```
datasets/
├── EOBZIP_2025_08/              # Extracted FDA Orange Book
├── EOBZIP_2025_08.zip           # Source archive (1.1 MB)
├── Loinc_2.81/                  # Extracted LOINC (95K+ lab codes)
├── Loinc_2.81.zip               # Source archive (78 MB)
├── NPPES_Data_Dissemination_September_2025/  # Extracted (2.3 GB!)
├── NPPES_Data_Dissemination_September_2025.zip  # Source archive (1.0 GB)
├── icd10cm-Code Descriptions-2026/  # Extracted ICD-10 codes
├── table-and-index/             # ICD-10 index files
├── table-and-index.zip          # Source archive (22 MB)
├── ndctext/                     # Extracted NDC drug database
├── ndctext.zip                  # Source archive (11 MB)
├── simplemaps_uszips_basicv1.911/  # Extracted US ZIP codes
├── simplemaps_uszips_basicv1.911.zip  # Source archive (4 MB)
├── cvx.txt                      # Vaccine codes (54 KB)
├── section111validicd10-jan2025_0.xlsx  # ICD-10 valid codes list
├── section111validicd9-jan2025_0.xlsx   # ICD-9 valid codes list
├── load_data_to_sqlite.py       # Processing script
├── load_icd10_text.py           # Processing script
├── process_icd_codes.py         # Processing script
├── subset_nppes.py              # Processing script
└── README.md                    # Current documentation
```

### **Problems**
1. **Mixed Content**: Raw data, extracted data, scripts, and ZIPs all at same level
2. **Massive Size**: 1.2 GB of ZIPs in repo, 3+ GB extracted
3. **No Organization**: Can't tell what's development vs production data
4. **No Free Tier**: No curated small dataset for free tier
5. **Scripts Mixed In**: Python processing scripts in same folder as data

---

## 🎯 **Proposed New Structure**

```
datasets/
├── _dev/                        # Development-only (gitignored, except metadata)
│   ├── sources/                 # Original ZIP files (keep for re-extraction)
│   │   ├── EOBZIP_2025_08.zip
│   │   ├── Loinc_2.81.zip
│   │   ├── NPPES_Data_Dissemination_September_2025.zip  # Git LFS or external
│   │   ├── ndctext.zip
│   │   ├── simplemaps_uszips_basicv1.911.zip
│   │   └── table-and-index.zip
│   ├── extracted/               # Extracted raw data (gitignored)
│   │   ├── EOBZIP_2025_08/
│   │   ├── Loinc_2.81/
│   │   ├── NPPES/
│   │   ├── ndctext/
│   │   ├── icd10cm-2026/
│   │   └── uszips/
│   ├── scripts/                 # Processing scripts (version controlled)
│   │   ├── extract_free_tier.py
│   │   ├── extract_pro_tier.py
│   │   ├── load_data_to_sqlite.py
│   │   ├── subset_nppes.py
│   │   └── README.md
│   └── metadata.json            # Dataset versions, sources, update dates
│
├── free/                        # Free tier curated data (version controlled)
│   ├── medications.json         # Top 25 medications (~10 KB)
│   ├── patient_names.json       # 50 first + 50 last names (~5 KB)
│   ├── addresses.json           # 10 generic US addresses (~2 KB)
│   ├── icd10_common.json        # Top 100 diagnosis codes (~15 KB)
│   ├── loinc_common.json        # Top 50 lab tests (~10 KB)
│   ├── cpt_common.json          # Top 50 procedures (~8 KB)
│   ├── vaccines_cvx.json        # CVX vaccine codes (~50 KB)
│   └── README.md                # Free tier data documentation
│
├── pro/                         # Professional tier datasets (gitignored)
│   ├── icd10_full.db            # SQLite database (71K codes, ~15 MB)
│   ├── loinc_common.db          # SQLite database (10K tests, ~10 MB)
│   ├── medications_rxnorm.db    # SQLite database (60K meds, ~50 MB)
│   └── README.md                # Pro tier data documentation
│
├── enterprise/                  # Enterprise tier datasets (gitignored)
│   ├── nppes_full.db            # SQLite database (7M providers, ~500 MB)
│   ├── snomed_ct.db             # SQLite database (350K concepts, ~200 MB)
│   └── README.md                # Enterprise tier data documentation
│
└── README.md                    # Main documentation (updated)
```

---

## 📋 **Migration Plan**

### **Step 1: Create New Directory Structure**

```bash
cd pidgeon/src/Pidgeon.Data/datasets

# Create new directories
mkdir -p _dev/sources
mkdir -p _dev/extracted
mkdir -p _dev/scripts
mkdir -p free
mkdir -p pro
mkdir -p enterprise
```

### **Step 2: Move Existing Files**

```bash
# Move ZIP files to _dev/sources
mv *.zip _dev/sources/

# Move extracted directories to _dev/extracted
mv EOBZIP_2025_08/ _dev/extracted/
mv Loinc_2.81/ _dev/extracted/
mv NPPES_Data_Dissemination_September_2025/ _dev/extracted/
mv "icd10cm-Code Descriptions-2026/" _dev/extracted/icd10cm-2026/
mv table-and-index/ _dev/extracted/
mv ndctext/ _dev/extracted/
mv simplemaps_uszips_basicv1.911/ _dev/extracted/uszips/

# Move processing scripts to _dev/scripts
mv *.py _dev/scripts/
mv cvx.txt _dev/extracted/  # Source data file

# Move Excel files to _dev/extracted
mv *.xlsx _dev/extracted/

# Keep README.md at root (will update)
```

### **Step 3: Update .gitignore**

Add to `pidgeon/src/Pidgeon.Data/.gitignore`:

```gitignore
# Datasets - development and large files
datasets/_dev/sources/*.zip
datasets/_dev/extracted/
datasets/pro/
datasets/enterprise/

# Exception: Keep source metadata and scripts
!datasets/_dev/scripts/
!datasets/_dev/metadata.json

# Free tier is committed (small files)
# No ignore needed for datasets/free/
```

---

## 🎯 **Free Tier Data Extraction Strategy**

### **Objective**
Create high-quality, curated datasets that cover **70% of test scenarios** with **<100 KB total size**.

### **Data Sources and Extraction**

#### **1. Top 25 Medications** (`medications.json`)

**Source**: `_dev/extracted/ndctext/product.txt`

**Selection Criteria** (in priority order):
1. Most commonly prescribed (via frequency data if available)
2. Coverage across major categories:
   - Cardiovascular (Lisinopril, Atorvastatin, Metoprolol)
   - Diabetes (Metformin, Insulin Glargine)
   - Pain/Inflammation (Ibuprofen, Acetaminophen, Aspirin)
   - Antibiotics (Amoxicillin, Azithromycin)
   - Mental Health (Sertraline, Escitalopram)
   - Respiratory (Albuterol, Montelukast)
   - GI (Omeprazole, Pantoprazole)

**Target List (25 medications)**:
```json
[
  {
    "name": "Lisinopril",
    "genericName": "Lisinopril",
    "brandName": "Prinivil, Zestril",
    "rxcui": "29046",
    "ndcExample": "00093-7370-01",
    "strength": "10 mg",
    "dosageForm": "Tablet, Oral",
    "therapeuticClass": "ACE Inhibitor",
    "commonUses": ["Hypertension", "Heart Failure"]
  }
  // ... 24 more
]
```

**Extraction Script**: `_dev/scripts/extract_free_tier_medications.py`

#### **2. Patient Names** (`patient_names.json`)

**Source**: US Census Bureau most common names (if available) or curated diverse list

**Selection Criteria**:
- 50 first names (25 male, 25 female, diverse cultural backgrounds)
- 50 last names (diverse cultural backgrounds)
- Include common American, Hispanic, Asian, African American names

**Structure**:
```json
{
  "firstNames": {
    "male": ["James", "Michael", "Robert", "John", "David", ...],
    "female": ["Mary", "Patricia", "Jennifer", "Linda", "Elizabeth", ...]
  },
  "lastNames": ["Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Martinez", "Rodriguez", ...]
}
```

**Manual Curation**: Based on US Census data (public domain)

#### **3. Addresses** (`addresses.json`)

**Source**: `_dev/extracted/uszips/uszips.csv`

**Selection Criteria**:
- 10 realistic addresses across different US regions
- Mix of urban, suburban, rural
- Various states (CA, TX, NY, FL, IL, PA, OH, GA, NC, MI)

**Structure**:
```json
[
  {
    "street": "123 Main Street",
    "city": "Springfield",
    "state": "IL",
    "zipCode": "62701",
    "county": "Sangamon County"
  }
  // ... 9 more
]
```

#### **4. Top 100 ICD-10 Codes** (`icd10_common.json`)

**Source**: `_dev/extracted/icd10cm-2026/icd10cm-codes-2026.txt`

**Selection Criteria**:
- Most frequently used in ambulatory care
- Coverage across major body systems
- Common preventive care codes (Z-codes)

**Sample Categories**:
- Hypertension (I10)
- Diabetes (E11.-)
- Hyperlipidemia (E78.5)
- Annual physical (Z00.00)
- Depression (F32.9)
- Back pain (M54.5)
- COPD (J44.9)
- CKD (N18.3)

**Structure**:
```json
[
  {
    "code": "I10",
    "description": "Essential (primary) hypertension",
    "category": "Cardiovascular",
    "isBillable": true
  }
  // ... 99 more
]
```

**Extraction Source**: `section111validicd10-jan2025_0.xlsx` + frequency data

#### **5. Top 50 LOINC Codes** (`loinc_common.json`)

**Source**: `_dev/extracted/Loinc_2.81/LoincTableCore/LoincTableCore.csv`

**Selection Criteria**:
- Most common lab tests (CBC, CMP, BMP, lipid panel, HbA1c, TSH, etc.)
- Common vitals (BP, HR, Temp, SpO2, Weight, Height)

**Sample Tests**:
- 718-7: Hemoglobin
- 6690-2: WBC count
- 2345-7: Glucose
- 2160-0: Creatinine
- 4548-4: HbA1c
- 3094-0: BUN
- 2951-2: Sodium
- 2823-3: Potassium

**Structure**:
```json
[
  {
    "loincCode": "718-7",
    "component": "Hemoglobin",
    "property": "MCnc",
    "timeAspect": "Pt",
    "system": "Bld",
    "scale": "Qn",
    "method": "",
    "commonName": "Hemoglobin [Mass/volume] in Blood",
    "units": "g/dL"
  }
  // ... 49 more
]
```

#### **6. Top 50 CPT Codes** (`cpt_common.json`)

**Source**: Curated list (CPT codes are AMA proprietary, use common knowledge)

**Selection Criteria**:
- Most common procedures
- Office visits (99213, 99214)
- Common procedures (venipuncture, EKG, injections)

**Structure**:
```json
[
  {
    "code": "99213",
    "description": "Office/outpatient visit, established patient, level 3",
    "category": "Evaluation and Management"
  }
  // ... 49 more
]
```

**Note**: CPT codes are proprietary. Use only common knowledge codes, cite as "example educational use only"

#### **7. CVX Vaccine Codes** (`vaccines_cvx.json`)

**Source**: `_dev/extracted/cvx.txt` (already have this)

**Selection Criteria**: All active vaccine codes

**Structure**:
```json
[
  {
    "cvxCode": "08",
    "shortDescription": "Hep B, adolescent or pediatric",
    "fullName": "Hepatitis B vaccine, pediatric or pediatric/adolescent dosage",
    "vaccineStatus": "Active",
    "lastUpdated": "2010/05/28"
  }
  // ... all active vaccines
]
```

---

## 🛠️ **Extraction Scripts**

### **Script 1: Extract Free Tier Medications**

Create `_dev/scripts/extract_free_tier_medications.py`:

```python
import json
import csv

# Top 25 most prescribed medications (2024 data)
TOP_MEDICATIONS = [
    "Lisinopril", "Atorvastatin", "Metformin", "Amlodipine", "Metoprolol",
    "Omeprazole", "Albuterol", "Losartan", "Gabapentin", "Levothyroxine",
    "Hydrochlorothiazide", "Simvastatin", "Furosemide", "Sertraline", "Escitalopram",
    "Ibuprofen", "Acetaminophen", "Aspirin", "Amoxicillin", "Azithromycin",
    "Montelukast", "Pantoprazole", "Prednisone", "Tramadol", "Insulin Glargine"
]

def extract_ndc_data(product_file):
    """Extract medication data from NDC product.txt file."""
    medications = []

    with open(product_file, 'r', encoding='utf-8') as f:
        reader = csv.DictReader(f, delimiter='\t')

        for row in reader:
            nonproprietary_name = row.get('NONPROPRIETARYNAME', '').strip()

            # Check if this is one of our top medications
            for target_med in TOP_MEDICATIONS:
                if target_med.lower() in nonproprietary_name.lower():
                    med_data = {
                        "name": nonproprietary_name,
                        "genericName": nonproprietary_name,
                        "brandName": row.get('PROPRIETARYNAME', '').strip(),
                        "labelerName": row.get('LABELERNAME', '').strip(),
                        "ndc": row.get('PRODUCTNDC', '').strip(),
                        "strength": row.get('ACTIVE_NUMERATOR_STRENGTH', '').strip(),
                        "unit": row.get('ACTIVE_INGRED_UNIT', '').strip(),
                        "dosageForm": row.get('DOSAGEFORMNAME', '').strip(),
                        "routeName": row.get('ROUTENAME', '').strip()
                    }
                    medications.append(med_data)
                    break  # Found a match, move to next row

            if len(medications) >= 25:
                break

    return medications

if __name__ == "__main__":
    product_file = "../extracted/ndctext/product.txt"
    output_file = "../../free/medications.json"

    medications = extract_ndc_data(product_file)

    # Write to JSON
    with open(output_file, 'w', encoding='utf-8') as f:
        json.dump(medications, f, indent=2, ensure_ascii=False)

    print(f"Extracted {len(medications)} medications to {output_file}")
```

### **Script 2: Extract Free Tier ICD-10 Codes**

Create `_dev/scripts/extract_free_tier_icd10.py`:

```python
import json
import csv

# Top 100 most common ICD-10 codes in ambulatory care
TOP_ICD10_CODES = [
    # Cardiovascular
    "I10", "I25.10", "I50.9", "I48.91",
    # Diabetes
    "E11.9", "E11.65", "E11.22",
    # Lipid disorders
    "E78.5", "E78.0",
    # Respiratory
    "J44.9", "J45.909", "J06.9",
    # Mental Health
    "F32.9", "F41.1", "F33.9",
    # Musculoskeletal
    "M54.5", "M19.90", "M25.50",
    # Kidney
    "N18.3", "N18.9",
    # Preventive
    "Z00.00", "Z00.01", "Z23",
    # ... (add 75 more common codes)
]

def extract_icd10_codes(codes_file):
    """Extract ICD-10 code data."""
    codes = []

    with open(codes_file, 'r', encoding='utf-8') as f:
        for line in f:
            parts = line.strip().split(maxsplit=1)
            if len(parts) == 2:
                code, description = parts

                if code in TOP_ICD10_CODES:
                    codes.append({
                        "code": code,
                        "description": description,
                        "isBillable": not code.endswith('-')  # Simplified check
                    })

    return codes

if __name__ == "__main__":
    codes_file = "../extracted/icd10cm-2026/icd10cm-codes-2026.txt"
    output_file = "../../free/icd10_common.json"

    codes = extract_icd10_codes(codes_file)

    with open(output_file, 'w', encoding='utf-8') as f:
        json.dump(codes, f, indent=2, ensure_ascii=False)

    print(f"Extracted {len(codes)} ICD-10 codes to {output_file}")
```

### **Script 3: Extract Free Tier LOINC Codes**

Similar pattern for LOINC codes from `LoincTableCore.csv`.

---

## 🔌 **Integration with Application**

### **Step 1: Create Free Tier Data Source**

Location: `Pidgeon.Data/Sources/Embedded/EmbeddedDataSource.cs`

```csharp
namespace Pidgeon.Data.Sources.Embedded
{
    public class EmbeddedMedicationSource : IMedicationDataSource
    {
        private static readonly Lazy<List<Medication>> _medications =
            new(() => LoadEmbeddedMedications());

        public string SourceId => "embedded-medications";
        public DataAccessTier RequiredTier => DataAccessTier.Free;

        private static List<Medication> LoadEmbeddedMedications()
        {
            var json = File.ReadAllText("datasets/free/medications.json");
            return JsonSerializer.Deserialize<List<Medication>>(json) ?? new();
        }

        public async Task<Result<Medication>> GetRandomMedicationAsync(CancellationToken ct)
        {
            await Task.Yield();
            var random = new Random();
            var medication = _medications.Value[random.Next(_medications.Value.Count)];
            return Result<Medication>.Success(medication);
        }
    }
}
```

### **Step 2: Register Data Sources**

In `Pidgeon.Data/ServiceRegistration.cs`:

```csharp
public static IServiceCollection AddDataSources(this IServiceCollection services)
{
    // Free tier embedded sources
    services.AddSingleton<IMedicationDataSource, EmbeddedMedicationSource>();
    services.AddSingleton<IDiagnosisCodeSource, EmbeddedICD10Source>();
    services.AddSingleton<ILaboratoryTestSource, EmbeddedLoincSource>();

    return services;
}
```

### **Step 3: Update Field Value Resolvers**

Wire the data sources into existing field value resolvers:

```csharp
public class MedicationFieldResolver : IFieldValueResolver
{
    private readonly IMedicationDataSource _medicationSource;

    public async Task<string> ResolveValueAsync(FieldContext context, CancellationToken ct)
    {
        var result = await _medicationSource.GetRandomMedicationAsync(ct);

        if (result.IsSuccess)
        {
            return FormatForHL7(result.Value);
        }

        // Fallback to simple placeholder
        return "Lisinopril 10 mg Tablet";
    }
}
```

---

## ✅ **Validation Plan**

### **Step 1: Generate Test Messages**

```bash
# Generate 10 ADT messages using free tier data
pidgeon generate ADT^A01 --count 10 --output test_adt.hl7

# Generate 10 RDE messages using free tier data
pidgeon generate RDE^O11 --count 10 --output test_rde.hl7

# Generate 10 ORU messages using free tier data
pidgeon generate ORU^R01 --count 10 --output test_oru.hl7
```

### **Step 2: Validate Quality**

**Manual Review Checklist**:
- [ ] Patient names look realistic and diverse
- [ ] Addresses are valid and diverse (different states)
- [ ] Medications are recognizable common drugs
- [ ] ICD-10 codes are common diagnoses
- [ ] LOINC codes are common lab tests
- [ ] Messages validate against HL7 spec

### **Step 3: Automated Tests**

```csharp
[Test]
public async Task FreeT Tier_Medications_AreRealistic()
{
    var source = new EmbeddedMedicationSource();
    var medication = await source.GetRandomMedicationAsync(CancellationToken.None);

    Assert.That(medication.IsSuccess);
    Assert.That(medication.Value.Name, Is.Not.Empty);
    Assert.That(medication.Value.DosageForm, Is.Not.Empty);
}
```

---

## 📊 **Success Criteria**

### **Technical**
- [ ] Free tier datasets < 100 KB total
- [ ] Free tier covers 25 meds, 50 names, 100 ICD-10 codes, 50 LOINC codes
- [ ] All free tier data is JSON (easy to parse, version control friendly)
- [ ] _dev folder is gitignored except scripts and metadata
- [ ] Existing generation still works after migration

### **Quality**
- [ ] Generated messages look realistic to healthcare professionals
- [ ] Data diversity (cultural names, geographic addresses, etc.)
- [ ] No placeholder or dummy data (e.g., "Test Patient", "123 Main St")

### **Business**
- [ ] Clear upgrade path visible (when free tier doesn't have data)
- [ ] Free tier drives adoption (good enough for basic testing)
- [ ] Pro tier offers clear value (60K meds vs 25 meds)

---

## 🚀 **Execution Checklist**

- [ ] Create new directory structure
- [ ] Move existing files to _dev
- [ ] Update .gitignore
- [ ] Create extraction scripts
- [ ] Run extraction scripts to create free tier data
- [ ] Validate free tier JSON files
- [ ] Create EmbeddedDataSource classes
- [ ] Wire into field value resolvers
- [ ] Generate test messages
- [ ] Manual quality review
- [ ] Write automated tests
- [ ] Update READMEs
- [ ] Commit free tier data to git
- [ ] Document for team

---

**Next Step**: Execute Step 1 (create directory structure) and begin free tier data extraction.
