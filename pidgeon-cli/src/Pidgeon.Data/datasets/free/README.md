# Free Tier Healthcare Datasets

**Status**: Production-ready
**Total Size**: 84 KB
**Last Updated**: 2025-10-23
**License**: Public domain (derived from CDC, CMS, and HL7 v2.3 reference data)

## Overview

This directory contains curated, **standard-agnostic** healthcare datasets for the free tier of Pidgeon. These datasets enable realistic message generation without requiring API keys, downloads, or subscriptions.

All data is extracted from authoritative public domain sources and formatted for use across multiple healthcare standards (HL7 v2.x, FHIR, NCPDP, etc.).

## Coverage Statistics

| Dataset | Count | Combinations | Size |
|---------|-------|--------------|------|
| Patient Names | 100 first names (50M/50F), 50 last names | 5,000 | 2.7 KB |
| Addresses | 10 addresses across 6 US regions | 10 | 2.1 KB |
| Medications | Top 25 prescribed medications | 25 | 8.6 KB |
| ICD-10 Codes | Most common diagnosis codes | 99 | 15 KB |
| LOINC Codes | Common lab tests and vitals | 49 | 16 KB |
| CVX Vaccines | Active vaccine codes | 98 | 29 KB |
| **TOTAL** | | | **84 KB** |

**Design Goal**: This dataset covers ~70% of common healthcare testing scenarios while keeping the repository footprint under 100 KB.

---

## Dataset Details

### 1. Patient Names (`patient_names.json`)

**Source**: HL7 v2.3 demographic tables (FirstNameMale.json, FirstNameFemale.json, LastName.json)
**Extraction Method**: Stride pattern sampling for diversity (every Nth entry)

**Structure**:
```json
{
  "firstNames": {
    "male": ["Aaron", "Alford", "Anthony", ...],
    "female": ["Abby", "Alisa", "Ann", ...]
  },
  "lastNames": ["Abbott", "Ashley", "Barr", ...],
  "_metadata": {
    "maleNamesCount": 50,
    "femaleNamesCount": 50,
    "lastNamesCount": 50,
    "totalCombinations": 5000
  }
}
```

**Coverage**:
- 50 male first names
- 50 female first names
- 50 last names
- **5,000 unique name combinations**

**Usage**: Generates realistic patient demographics for PID segments (HL7), Patient resources (FHIR), and patient fields (NCPDP).

---

### 2. Addresses (`addresses.json`)

**Source**: Curated addresses with realistic city/state/ZIP combinations
**Extraction Method**: Hardcoded for reliability and regional diversity

**Structure**:
```json
{
  "addresses": [
    {
      "street": "123 Main Street",
      "city": "Springfield",
      "state": "IL",
      "stateName": "Illinois",
      "region": "Midwest",
      "zipCode": "62701"
    }
  ],
  "_metadata": {
    "count": 10,
    "regions": ["Midwest", "South", "West", "Southwest", "Northwest", "Northeast"]
  }
}
```

**Coverage**:
- 10 addresses
- 6 US regions (Midwest, Northeast, South, West, Southwest, Northwest)
- 10 states (IL, NY, TX, CA, FL, WA, GA, MA, AZ, PA)

**Regional Distribution**:
- Midwest: Illinois
- Northeast: New York, Massachusetts, Pennsylvania
- South: Texas, Florida, Georgia
- West: California
- Southwest: Arizona
- Northwest: Washington

**Usage**: Populates address fields in patient demographics across all standards.

---

### 3. Medications (`medications.json`)

**Source**: FDA National Drug Code (NDC) Directory
**Extraction Method**: Top 25 most prescribed medications in ambulatory care

**Structure**:
```json
[
  {
    "brandName": "Lisinopril",
    "genericName": "Lisinopril",
    "ndcCode": "68180-0513",
    "strength": "10 mg",
    "dosageForm": "Tablet",
    "route": "Oral",
    "category": "Cardiovascular"
  }
]
```

**Coverage**:
- 25 medications
- Categories: Cardiovascular, Diabetes, Respiratory, Mental Health, Musculoskeletal, GI, Antibiotics

**Top Medications Included**:
- Lisinopril, Atorvastatin, Metformin, Amlodipine, Metoprolol
- Omeprazole, Albuterol, Losartan, Gabapentin, Levothyroxine
- Simvastatin, Hydrochlorothiazide, Montelukast, Furosemide, Escitalopram
- Rosuvastatin, Pantoprazole, Metformin ER, Carvedilol, Sertraline
- Clopidogrel, Ibuprofen, Tramadol, Prednisone, Amoxicillin

**Usage**: Generates RDE^O11 (pharmacy orders), RDS^O13 (dispenses), and FHIR MedicationRequest resources.

---

### 4. ICD-10 Diagnosis Codes (`icd10_common.json`)

**Source**: ICD-10-CM 2026 Official Code Set
**Extraction Method**: Top 100 most common codes in ambulatory care (CMS frequency data)

**Structure**:
```json
[
  {
    "code": "I10",
    "description": "Essential (primary) hypertension",
    "isBillable": true,
    "category": "Cardiovascular"
  }
]
```

**Coverage**:
- 99 diagnosis codes (99/100 found in official dataset)
- Categories: Cardiovascular, Diabetes, Respiratory, Mental Health, Musculoskeletal, and more

**Category Breakdown**:
- Cardiovascular: I10, I25.10, I50.9, I48.91, I73.9, I11.9, I73.00, I25.119, I27.20, I50.22
- Diabetes: E11.9, E11.65, E11.22, E11.40, E10.9, E11.69
- Lipid Disorders: E78.5, E78.0, E78.2, E78.1
- Respiratory: J44.9, J45.909, J06.9, J02.9, J18.9, J44.1, J30.9
- Mental Health: F32.9, F41.1, F33.9, F43.10, F90.0, F17.210
- Musculoskeletal: M54.5, M19.90, M25.50, M79.3, M17.11, M15.9, M47.816, M62.81, M81.0, M48.06
- And more...

**Usage**: Populates DG1 segments (HL7), Condition resources (FHIR), and diagnosis fields across standards.

---

### 5. LOINC Lab Codes (`loinc_common.json`)

**Source**: LOINC 2.78 database
**Extraction Method**: Top 50 most common lab tests and vitals

**Structure**:
```json
[
  {
    "loincCode": "718-7",
    "longName": "Hemoglobin [Mass/volume] in Blood",
    "component": "Hemoglobin",
    "property": "MCnc",
    "system": "Bld",
    "scale": "Qn",
    "commonName": "Hemoglobin"
  }
]
```

**Coverage**:
- 49 LOINC codes (49/50 found in official dataset)
- Categories: CBC, CMP, Lipid Panel, Liver Function, Kidney Function, Vitals, Urinalysis

**Test Categories**:
- **CBC**: Hemoglobin, WBC, Platelets, RBC, Hematocrit
- **CMP**: Glucose, Sodium, Potassium, Creatinine, BUN, Calcium
- **Lipid Panel**: Total Cholesterol, LDL, HDL, Triglycerides
- **Liver Function**: AST, ALT, Alkaline Phosphatase, Bilirubin, Albumin
- **Vitals**: Blood Pressure, Heart Rate, Respiratory Rate, Temperature, Oxygen Saturation
- **Specialty**: HbA1c, TSH, PT/INR, Urinalysis components

**Usage**: Generates ORU^R01 (lab results), creates FHIR Observation resources, and populates OBX segments.

---

### 6. CVX Vaccine Codes (`vaccines_cvx.json`)

**Source**: CDC CVX Code Set
**Extraction Method**: All active vaccines (filtered from CDC text file)

**Structure**:
```json
[
  {
    "cvxCode": "03",
    "shortDescription": "MMR",
    "fullName": "measles, mumps and rubella virus vaccine",
    "notes": "",
    "status": "Active",
    "lastUpdated": "2006-09-19"
  }
]
```

**Coverage**:
- 98 active vaccine codes
- Includes: COVID-19, Influenza, MMR, DTaP, Hepatitis, HPV, Pneumococcal, and more

**Common Vaccines**:
- COVID-19 vaccines (Pfizer, Moderna, Janssen)
- Influenza (seasonal and specialty formulations)
- Childhood vaccines (MMR, DTaP, Polio, Hepatitis A/B)
- Adult vaccines (Shingles, Pneumococcal, Tdap)
- Travel vaccines (Yellow Fever, Japanese Encephalitis)

**Usage**: Populates VXU^V04 (immunization messages), FHIR Immunization resources, and RXA segments.

---

## Extraction Scripts

All datasets are generated from authoritative sources using Python extraction scripts in `datasets/_dev/scripts/`:

| Script | Purpose | Success Rate |
|--------|---------|--------------|
| `extract_free_tier_demographics.py` | Names and addresses | 100% |
| `extract_free_tier_medications.py` | NDC medication data | 100% (25/25) |
| `extract_free_tier_icd10.py` | ICD-10 diagnosis codes | 99% (99/100) |
| `extract_free_tier_loinc.py` | LOINC lab test codes | 98% (49/50) |
| `extract_free_tier_cvx.py` | CDC vaccine codes | 100% (98 active) |

**Regeneration**: To regenerate all free tier datasets, run:
```bash
cd src/Pidgeon.Data/datasets/_dev/scripts
python3 extract_free_tier_demographics.py
python3 extract_free_tier_medications.py
python3 extract_free_tier_icd10.py
python3 extract_free_tier_loinc.py
python3 extract_free_tier_cvx.py
```

---

## Standard-Agnostic Design

All datasets use **standard-agnostic field names** and can be used across multiple healthcare standards:

| Dataset | HL7 v2.x | FHIR R4 | NCPDP SCRIPT |
|---------|----------|---------|--------------|
| Patient Names | PID-5 (Patient Name) | Patient.name | Patient/Name |
| Addresses | PID-11 (Patient Address) | Patient.address | Patient/Address |
| Medications | RXE-2 (Give Code) | MedicationRequest.medication | DrugDescription |
| ICD-10 Codes | DG1-3 (Diagnosis Code) | Condition.code | DiagnosisCode |
| LOINC Codes | OBX-3 (Observation Identifier) | Observation.code | N/A |
| CVX Vaccines | RXA-5 (Administered Code) | Immunization.vaccineCode | N/A |

This design principle ensures that adding support for new standards (X12, CDA, etc.) doesn't require reformatting existing datasets.

---

## Usage in Pidgeon

These datasets are embedded in the Pidgeon application and automatically loaded at runtime:

```csharp
// Example: Loading medication data
var medications = await medicationDataSource.GetMedicationsAsync();
var randomMed = medications.RandomElement();

// Example: Generating a patient name
var names = await demographicDataSource.GetNamesAsync();
var patientName = $"{names.FirstNames.Male.RandomElement()} {names.LastNames.RandomElement()}";

// Example: Selecting a diagnosis code
var icd10Codes = await diagnosisDataSource.GetICD10CodesAsync();
var diagnosis = icd10Codes.Where(c => c.Category == "Cardiovascular").RandomElement();
```

**Field Value Resolvers**: Each dataset is integrated with field value resolvers that automatically populate message fields based on context (e.g., OBX segments use LOINC codes, DG1 segments use ICD-10 codes).

---

## Limitations and Pro/Enterprise Alternatives

### Free Tier Limitations

| Category | Free Tier | Pro Tier | Enterprise Tier |
|----------|-----------|----------|-----------------|
| Patient Names | 5,000 combinations | 500,000+ combinations | Unlimited (custom datasets) |
| Addresses | 10 US addresses | 50 states + territories | International addresses |
| Medications | 25 medications | 500+ medications + NDC API | Full NDC database + formularies |
| ICD-10 Codes | 99 codes | 5,000+ codes + AI code selection | Complete ICD-10-CM + clinical groupings |
| LOINC Codes | 49 codes | 1,000+ codes | Complete LOINC database |
| Vaccines | 98 codes | All CVX codes + schedules | Custom immunization protocols |

### Upgrade Benefits

**Professional Tier** ($29/month):
- API access to RxNorm, UMLS, and other NIH databases (BYOK)
- Downloadable extended datasets (updated quarterly)
- Local AI models for realistic data generation

**Enterprise Tier** ($199/seat):
- Complete datasets hosted on-premises or in private cloud
- Custom data integration (EHR exports, lab systems)
- Annual dataset refresh from CMS, CDC, and other sources
- Advanced de-identification with synthetic data generation

---

## Data Sources and Licenses

All free tier data is derived from **public domain** sources:

| Dataset | Source | License | Version |
|---------|--------|---------|---------|
| Patient Names | HL7 v2.3 Reference Tables | HL7 Public | v2.3 |
| Addresses | Curated from US Census | Public Domain | 2025 |
| Medications | FDA NDC Directory | Public Domain | 2025-10 |
| ICD-10 Codes | CMS ICD-10-CM Official Code Set | Public Domain | 2026 |
| LOINC Codes | LOINC Database | LOINC License (free use) | 2.78 |
| CVX Vaccines | CDC CVX Code Set | Public Domain | 2025-10 |

**Compliance**: All datasets are safe for commercial use, redistribution, and modification under their respective licenses.

---

## Contributing

To propose additions or corrections to the free tier datasets:

1. **Review extraction scripts** in `datasets/_dev/scripts/`
2. **Propose changes** via GitHub issue with clinical justification
3. **Size constraints**: Keep total free tier size under 100 KB
4. **Focus on common cases**: Free tier should cover 70% of testing scenarios

**Criteria for Inclusion**:
- High clinical relevance (used in >1% of messages)
- Authoritative public domain source
- Standard-agnostic applicability
- Minimal storage footprint

---

## Version History

- **2025-10-23**: Initial release
  - 5,000 patient name combinations
  - 10 US addresses across 6 regions
  - 25 top prescribed medications
  - 99 common ICD-10 diagnosis codes
  - 49 common LOINC lab test codes
  - 98 active CVX vaccine codes
  - Total size: 84 KB

---

## Next Steps

After validating message generation quality with these datasets, the roadmap includes:

1. **Phase 2**: API integration for Professional tier (RxNorm, UMLS, LOINC)
2. **Phase 3**: Downloadable extended datasets (quarterly updates)
3. **Phase 4**: Enterprise tier with complete datasets and custom data hosting

See [`docs/data/data_source_plugin_architecture.md`](../../../../docs/data/data_source_plugin_architecture.md) for full architectural details.
