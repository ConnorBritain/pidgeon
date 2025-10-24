# Pidgeon Architectural Decision Ledger

This document tracks significant architectural decisions, changes, and their rationale.

## Format

Each entry follows this structure:
- **LEDGER-XXX**: Unique identifier
- **Date**: When the decision was made
- **Problem**: What issue was being addressed
- **Solution**: What was implemented
- **Impact**: How this affects the codebase
- **Rollback**: How to undo if needed (if applicable)

---

## LEDGER-001: Free Tier Dataset Integration

**Date**: 2025-10-23
**Problem**: Message generation was using scattered HL7-specific tables and lacked realistic clinical data (medications, diagnoses, lab tests). Repository had 1.2 GB of unorganized datasets creating bloat.

**Solution**: Integrated curated free tier datasets (84 KB total) with complete data source plugin architecture:

### Dataset Organization
- Created structured directory: `datasets/_dev/`, `datasets/free/`, `datasets/pro/`, `datasets/enterprise/`
- Moved development files (ZIP sources, extracted data, scripts) to gitignored `_dev/` folder
- Extracted and curated free tier data:
  - **Patient Names**: 5,000 combinations (50 male, 50 female, 50 last names)
  - **Addresses**: 10 US addresses across 6 regions
  - **Medications**: 25 top prescribed drugs with NDC codes
  - **ICD-10 Codes**: 99 common diagnoses across all body systems
  - **LOINC Codes**: 49 common lab tests (CBC, CMP, vitals, etc.)
  - **CVX Codes**: 98 active vaccine codes

### Code Architecture
- **Data Models** (DTOs): Standard-agnostic models in `Application/DTOs/Data/`
  - `DemographicData`, `MedicationData`, `DiagnosisData`, `LabTestData`, `VaccineData`

- **Data Source Interfaces**: Plugin-ready interfaces in `Application/Interfaces/Data/`
  - `IDemographicDataSource`, `IMedicationDataSource`, `IDiagnosisDataSource`, `ILabTestDataSource`, `IVaccineDataSource`

- **Embedded Implementations**: Free tier sources in `Infrastructure/Data/`
  - `EmbeddedDemographicDataSource`, `EmbeddedMedicationDataSource`, `EmbeddedDiagnosisDataSource`, `EmbeddedLabTestDataSource`, `EmbeddedVaccineDataSource`
  - Load from embedded resources, cache in memory, provide random selection with filtering

- **Field Value Resolvers**: Enhanced resolver chain in `Services/FieldValueResolvers/`
  - **Updated**: `DemographicFieldResolver` - Now uses data sources instead of HL7 tables
  - **New**: `MedicationFieldResolver` (Priority 75) - Drug names, NDC codes, strength, form, route
  - **New**: `DiagnosisFieldResolver` (Priority 75) - ICD-10 codes and descriptions
  - **New**: `LabTestFieldResolver` (Priority 75) - LOINC codes and test names

- **DI Registration**: Added `AddEmbeddedDataSources()` extension method
  - All data sources registered as singletons
  - New field resolvers added to priority chain at Priority 75

### Extraction Scripts
Created Python scripts in `datasets/_dev/scripts/`:
- `extract_free_tier_demographics.py` - Stride pattern sampling for diversity
- `extract_free_tier_medications.py` - Top 25 medications from NDC database
- `extract_free_tier_icd10.py` - Common diagnoses from CMS frequency data
- `extract_free_tier_loinc.py` - Common lab tests and vitals
- `extract_free_tier_cvx.py` - Active vaccine codes from CDC

### Resource Embedding
- Updated `Pidgeon.Data.csproj` to embed `datasets/free/*.json` as resources
- Resources accessible via `data.datasets.free.{filename}.json` naming convention

**Impact**:
- ✅ **Reduced repository size**: Moved 1.2 GB to gitignored folders
- ✅ **Standard-agnostic data**: Same datasets work across HL7, FHIR, NCPDP
- ✅ **Plugin architecture**: Ready for Pro/Enterprise tier API-based data sources
- ✅ **Realistic generation**: Messages now contain actual drug names, diagnoses, lab tests
- ✅ **No breaking changes**: All existing APIs maintained, new resolvers added to chain

**Files Changed**:
- `src/Pidgeon.Data/Pidgeon.Data.csproj` - Added embedded resource configuration
- `src/Pidgeon.Data/datasets/free/` - 6 JSON files (patient_names, addresses, medications, icd10_common, loinc_common, vaccines_cvx)
- `src/Pidgeon.Core/Application/DTOs/Data/` - 5 new DTO files
- `src/Pidgeon.Core/Application/Interfaces/Data/` - 5 new interface files
- `src/Pidgeon.Core/Infrastructure/Data/` - 5 new implementation files
- `src/Pidgeon.Core/Services/FieldValueResolvers/` - 1 updated, 3 new resolvers
- `src/Pidgeon.Core/Common/Extensions/ServiceCollectionExtensions.cs` - Added `AddEmbeddedDataSources()` method
- `.gitignore` - Added dataset exclusions

**Rollback**: If needed, revert the following:
1. Remove `AddEmbeddedDataSources()` call from `ServiceCollectionExtensions.cs:58`
2. Remove new field resolver registrations (lines 329-337)
3. Revert `DemographicFieldResolver.cs` to use HL7 table loading
4. Delete `Application/DTOs/Data/`, `Application/Interfaces/Data/`, `Infrastructure/Data/` directories
5. Remove embedded resource entries from `Pidgeon.Data.csproj`

**Future Work**:
- Phase 2: API-based data sources for Professional tier (RxNorm, UMLS, LOINC API)
- Phase 3: Downloadable extended datasets with quarterly updates
- Phase 4: Enterprise tier with complete datasets and custom data hosting

---
