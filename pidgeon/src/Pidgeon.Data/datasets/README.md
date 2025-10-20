# Dataset Setup Guide

## Overview

This directory contains reference datasets for healthcare data generation. Due to their large size (several GB), **extracted dataset directories are excluded from version control**.

⚠️ **IMPORTANT**: Some ZIP files exceed GitHub's 100 MB limit:
- `NPPES_Data_Dissemination_September_2025.zip` (1.03 GB) - **TOO LARGE FOR GITHUB**
- Other ZIPs are under 100 MB and can be committed

**Options for NPPES file:**
1. **Exclude from git** - Add to `.gitignore` and document download link
2. **Use Git LFS** - Track with Git Large File Storage
3. **External storage** - Host on cloud storage (Azure Blob, S3, etc.)

## Directory Structure

```
datasets/
├── *.zip                    # Source archives (version controlled)
├── *.py                     # Processing scripts (version controlled)
├── NPPES_*/                 # Extracted (gitignored)
├── Loinc_*/                 # Extracted (gitignored)
├── EOBZIP_*/                # Extracted (gitignored)
├── ndctext/                 # Extracted (gitignored)
├── icd10cm-*/               # Extracted (gitignored)
├── simplemaps_*/            # Extracted (gitignored)
└── table-and-index/         # Extracted (gitignored)
```

## Initial Setup

### 1. Extract Required Datasets

```bash
# Navigate to datasets directory
cd pidgeon/src/Pidgeon.Data/datasets

# Extract NPPES (National Provider Registry)
unzip -q NPPES_Data_Dissemination_September_2025.zip -d NPPES_Data_Dissemination_September_2025/

# Extract LOINC (Laboratory Codes)
unzip -q Loinc_2.81.zip -d Loinc_2.81/

# Extract Orange Book (FDA Drug Database)
unzip -q EOBZIP_2025_08.zip -d EOBZIP_2025_08/

# Extract NDC Drug Products
unzip -q ndctext.zip -d ndctext/

# Extract US ZIP Codes
unzip -q simplemaps_uszips_basicv1.911.zip -d simplemaps_uszips_basicv1.911/

# Extract ICD-10 Codes
unzip -q "icd10cm-Code Descriptions-2026.zip" -d "icd10cm-Code Descriptions-2026/"
unzip -q table-and-index.zip -d table-and-index/
```

### 2. Process Large Files (Optional)

For the massive NPPES file (2.2 GB), use the subset script:

```bash
python subset_nppes.py
```

This creates a smaller subset of providers for development/testing.

## Datasets Included

| Dataset | Size (Compressed) | Size (Extracted) | Purpose |
|---------|------------------|------------------|---------|
| **NPPES** | 1.0 GB | 2.3 GB | Healthcare provider registry (NPI, names, specialties) |
| **LOINC** | 80 MB | 500 MB | Laboratory test codes and descriptions |
| **NDC** | 10 MB | 68 MB | Drug products and NDC codes |
| **ICD-10** | 22 MB | 50 MB | Diagnosis codes |
| **US ZIP Codes** | 4 MB | 6 MB | Geographic data |
| **FDA Orange Book** | 1 MB | 8 MB | Generic/brand drug equivalents |
| **CVX Codes** | 54 KB | N/A | Vaccine codes |

## Data Sources

- **NPPES**: https://download.cms.gov/nppes/NPI_Files.html
- **LOINC**: https://loinc.org/downloads/ (requires free registration)
- **NDC**: https://www.fda.gov/drugs/drug-approvals-and-databases/national-drug-code-directory
- **ICD-10**: https://www.cms.gov/medicare/coding-billing/icd-10-codes
- **ZIP Codes**: https://simplemaps.com/data/us-zips
- **Orange Book**: https://www.fda.gov/drugs/drug-approvals-and-databases/orange-book-data-files
- **CVX**: https://www2a.cdc.gov/vaccines/iis/iisstandards/vaccines.asp

## Usage in Application

These datasets are used by field value resolvers to generate realistic:
- Patient names and addresses
- Provider NPIs and specialties
- Medication names and NDC codes
- Laboratory test codes (LOINC)
- Diagnosis codes (ICD-10)
- Vaccine codes (CVX)

See `docs/data/sprint4/dataset_operationalization_strategy.md` for implementation details.

## Maintenance

### Updating Datasets

Datasets are updated on different schedules:
- **NPPES**: Monthly
- **LOINC**: Biannually (February, August)
- **NDC**: Weekly
- **ICD-10**: Annually (October)

### Adding New Datasets

1. Download and add ZIP file to this directory
2. Add extraction path to `.gitignore`
3. Update this README
4. Create processing script if needed

## Troubleshooting

### "Out of disk space" during extraction
- NPPES alone requires 2.3 GB extracted
- Ensure 5+ GB free space before extracting all datasets

### "ZIP file corrupt"
- Re-download from official source
- Verify file size matches expected size

### "Python script fails"
- Ensure Python 3.8+ installed
- Install dependencies: `pip install pandas`

## Notes

⚠️ **Do NOT commit extracted directories to git**
- They're multi-gigabyte and will be rejected by GitHub
- ZIP files are sufficient for version control
- Each developer extracts locally as needed

✅ **ZIP files are tracked in git** for convenience
- Total size: ~1.1 GB compressed
- Enables quick setup without hunting for downloads
