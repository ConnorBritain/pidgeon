# Free and Open Healthcare Data Sources

This document contains research findings for free and open healthcare data sources that can be integrated into Pidgeon's realistic message generation system.

## <¯ Confirmed Free Sources (Public Domain)

### 1. FDA National Drug Code (NDC) Directory
**Source**: https://www.fda.gov/drugs/drug-approvals-and-databases/national-drug-code-directory
- **API Access**: https://open.fda.gov/apis/drug/ndc/
- **Direct Downloads**: https://www.fda.gov/industry/structured-product-labeling-resources/drug-registration-and-listing-system
- **Format**: JSON, CSV, XML
- **Content**: 200,000+ drug products with NDC codes, product names, dosage forms, routes
- **Update Frequency**: Daily updates via API
- **License**: Public Domain (U.S. Government)
- **Integration**: Perfect for medication generation in RDE/RXE segments

### 2. LOINC Laboratory Codes
**Source**: https://loinc.org/downloads/
- **API Access**: https://fhir.loinc.org/ (FHIR Terminology Server)
- **Direct Downloads**: https://loinc.org/downloads/loinc-table/
- **Format**: CSV, XML, FHIR
- **Content**: 95,000+ lab test codes with descriptions, units, reference ranges
- **Update Frequency**: Bi-annual releases
- **License**: Free with registration (LOINC License)
- **Integration**: Critical for ORU^R01 OBX segments and lab result generation

### 3. ICD-10-CM Diagnosis Codes
**Source**: https://www.cms.gov/medicare/icd-10/icd-10-cm
- **Direct Downloads**: https://www.cms.gov/medicare/icd-10/icd-10-cm/icd-10-cm-diagnosis-codes
- **FTP Access**: https://www.cms.gov/files/zip/2024-icd-10-cm-codes.zip
- **Format**: XML, TXT, PDF
- **Content**: Complete ICD-10-CM diagnosis codes with descriptions
- **Update Frequency**: Annual (October)
- **License**: Public Domain
- **Integration**: Essential for DG1 segments and diagnosis-related message generation

### 4. NPPES NPI Registry (Provider Information)
**Source**: https://download.cms.gov/nppes/NPI_Files.html
- **Full Database**: https://download.cms.gov/nppes/NPPES_Data_Dissemination_January_2024.zip
- **Weekly Updates**: https://download.cms.gov/nppes/NPI_Deactivated_Weekly_*.zip
- **Format**: CSV (6GB+ full file)
- **Content**: 7M+ healthcare providers with NPI, names, specialties, addresses
- **Update Frequency**: Weekly incremental, monthly full refresh
- **License**: Public Domain
- **Integration**: Perfect for provider information in MSH, PV1, OBR segments

### 5. RxNorm Drug Normalization
**Source**: https://www.nlm.nih.gov/research/umls/rxnorm/
- **API Access**: https://rxnav.nlm.nih.gov/RxNormAPIs.html
- **Direct Downloads**: https://www.nlm.nih.gov/research/umls/rxnorm/docs/rxnormfiles.html
- **Format**: RRF files, REST API responses
- **Content**: Normalized drug names, clinical drugs, ingredients, strengths
- **Update Frequency**: Monthly
- **License**: UMLS License (free registration required)
- **Integration**: Enhanced medication normalization and drug interaction data

### 6. U.S. Census Demographics
**Source**: https://www.census.gov/data/developers/data-sets.html
- **API Access**: https://api.census.gov/data.html
- **Names Database**: https://www.census.gov/topics/population/genealogy/data/2010_surnames.html
- **Format**: JSON, CSV via API
- **Content**: Name frequencies, demographic distributions, geographic data
- **Update Frequency**: Decennial census, annual community survey
- **License**: Public Domain
- **Integration**: Realistic patient demographics and name generation

## =, Specialized Clinical Sources

### 7. CDC NHANES Laboratory Data
**Source**: https://wwwn.cdc.gov/nchs/nhanes/
- **Data Access**: https://wwwn.cdc.gov/nchs/nhanes/search/datapage.aspx
- **Format**: XPT (SAS), CSV conversion available
- **Content**: Population-based lab results with reference ranges and distributions
- **Use Case**: Realistic lab value distributions for abnormal/normal result weighting
- **License**: Public Domain
- **Integration**: Statistical models for lab result generation

### 8. CDC Vaccine Information (CVX Codes)
**Source**: https://www2a.cdc.gov/vaccines/iis/iisstandards/vaccines.asp?rpt=cvx
- **Direct Download**: https://www2a.cdc.gov/vaccines/iis/iisstandards/XML.asp
- **Format**: XML, CSV
- **Content**: Vaccine product codes (CVX), manufacturers, administration codes
- **Update Frequency**: Monthly
- **License**: Public Domain
- **Integration**: Immunization messages and pediatric workflows

### 9. FDA Orange Book (Generic/Brand Equivalents)
**Source**: https://www.fda.gov/drugs/drug-approvals-and-databases/orange-book-data-files
- **Direct Downloads**: https://www.fda.gov/media/76860/download
- **Format**: ZIP containing multiple TXT files
- **Content**: Therapeutic equivalence ratings, patent information, exclusivity data
- **Update Frequency**: Monthly
- **License**: Public Domain
- **Integration**: Brand/generic medication relationships

## =Ê Additional Reference Sources

### 10. USPS Address Data
**Source**: https://postalpro.usps.com/address-quality
- **ZIP+4 Database**: https://postalpro.usps.com/address-quality/zip-code-lookup
- **Alternative Free Source**: https://simplemaps.com/data/us-zips (limited free version)
- **Format**: CSV, database exports
- **Content**: ZIP codes, cities, states, counties, geographic coordinates
- **License**: Mixed (USPS public, alternatives vary)
- **Integration**: Realistic address generation for patient demographics

### 11. HL7 FHIR Test Data
**Source**: https://github.com/microsoft/fhir-server-samples
- **Synthea Generated**: https://synthea.mitre.org/downloads
- **Format**: FHIR R4 JSON bundles
- **Content**: Synthetic patient records with realistic clinical patterns
- **License**: Apache 2.0 / MIT
- **Integration**: Pattern analysis for realistic clinical scenario generation

### 12. Medicare Provider Utilization Data
**Source**: https://data.cms.gov/provider-summary-by-type-of-service
- **Direct Access**: https://data.cms.gov/provider-summary-by-type-of-service/medicare-physician-other-practitioners
- **Format**: CSV via data.cms.gov
- **Content**: Provider prescribing patterns, procedure volumes, geographic distribution
- **Use Case**: Realistic provider-medication and provider-procedure associations
- **License**: Public Domain
- **Integration**: Enhanced clinical realism based on actual practice patterns

## = Premium Sources (Requiring Registration)

### 13. SNOMED CT (via UMLS)
**Source**: https://www.nlm.nih.gov/healthit/snomedct/us_edition.html
- **Access**: Requires UMLS Terminology Services (UTS) account (free)
- **Format**: RF2 files, FHIR terminology server
- **Content**: 350,000+ clinical concepts with relationships
- **License**: UMLS License Agreement (free but registration required)
- **Integration**: Comprehensive clinical terminology for advanced scenarios

### 14. CPT Codes (Limited Free Access)
**Source**: https://www.ama-assn.org/practice-management/cpt
- **Free Subset**: Some codes available through CMS HCPCS
- **Full Access**: Requires AMA license ($500+ annually)
- **Alternative**: Use HCPCS Level II codes (free) as substitute
- **Integration**: Procedure coding for professional billing scenarios

## =€ Implementation Priority

### Phase 1: Essential Free Sources (Week 1-2)
1. **FDA NDC Directory** - Medication catalog foundation
2. **LOINC** - Laboratory test catalog
3. **ICD-10-CM** - Diagnosis codes
4. **Census Names** - Patient demographics
5. **NPPES NPI** - Provider information (subset extraction)

### Phase 2: Enhanced Sources (Week 3-4)
1. **RxNorm** - Drug normalization and relationships
2. **NHANES** - Lab value statistical distributions
3. **CVX Codes** - Immunization scenarios
4. **Orange Book** - Brand/generic relationships
5. **ZIP Codes** - Geographic accuracy

### Phase 3: Advanced Integration (Week 5-6)
1. **Medicare Utilization** - Practice pattern intelligence
2. **SNOMED CT** - Advanced terminology (requires registration)
3. **Synthea Data** - Pattern analysis for clinical realism
4. **Provider Specialties** - Enhanced provider-procedure associations

## =Ë Technical Integration Notes

### API Rate Limits
- **FDA openFDA**: 240 requests/minute, 120,000/day (no key required)
- **LOINC FHIR**: Unlimited for terminology lookups
- **Census API**: No limits for aggregate data
- **RxNav (RxNorm)**: 20 requests/second

### Data Volume Estimates
- **NDC Database**: ~500MB (JSON format)
- **LOINC Table**: ~200MB (CSV format)
- **ICD-10-CM**: ~100MB (XML format)
- **NPPES Full**: ~6GB (monthly snapshot)
- **NPPES Subset** (top 10K providers): ~50MB

### Storage Strategy
- **In-Memory Cache**: Top 1000 of each category for <50ms lookups
- **SQLite Database**: Full datasets with indexed searches
- **Update Pipeline**: Automated weekly/monthly refresh from sources
- **Version Control**: Track dataset versions for reproducible generation

## <¯ Success Metrics

### Coverage Goals
- **Medications**: 95% of common prescriptions (top 1000 drugs)
- **Lab Tests**: 90% of routine orders (top 500 LOINC codes)
- **Diagnoses**: 85% of frequent conditions (top 500 ICD-10 codes)
- **Providers**: Representative sample across all specialties

### Performance Targets
- **Lookup Speed**: <50ms for cached data, <200ms for database queries
- **Memory Usage**: <100MB for in-memory caches
- **Update Time**: <30 minutes for weekly data refresh
- **Storage Footprint**: <2GB for complete dataset collection

---

*These sources provide the foundation for realistic healthcare message generation while maintaining legal compliance and zero PHI risk. The combination of government open data and standardized healthcare terminologies ensures clinical authenticity without compromising patient privacy.*