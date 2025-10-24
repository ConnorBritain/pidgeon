# Data Source Plugin Architecture - Intelligent Healthcare Data Access

**Version**: 1.0
**Date**: October 23, 2025
**Status**: 📋 Planning - Comprehensive strategy for scalable data source management
**Related LEDGER**: LEDGER-044

---

## 🎯 **Executive Summary**

Transform Pidgeon's data access from monolithic embedded datasets to a flexible plugin architecture with API integration, downloadable datasets, and tiered access control.

**Key Innovation**: Users download only the datasets they need, with optional API access for live updates and extended coverage.

**Business Alignment**: Perfectly maps to Free/Pro/Enterprise tiers while solving the "multi-gigabyte repo" problem.

---

## 📊 **Current State Analysis**

### **Existing Datasets (in `Pidgeon.Data/datasets/`)**

| Dataset | Compressed | Extracted | Coverage | Status |
|---------|-----------|-----------|----------|--------|
| **NPPES** | 1.0 GB | 2.3 GB | 7M+ providers | ✅ Downloaded |
| **LOINC** | 80 MB | 500 MB | 95K+ lab codes | ✅ Downloaded |
| **NDC** | 10 MB | 68 MB | 200K+ medications | ✅ Downloaded |
| **ICD-10** | 22 MB | 50 MB | 71K+ diagnoses | ✅ Downloaded |
| **US ZIP Codes** | 4 MB | 6 MB | Complete US | ✅ Downloaded |
| **FDA Orange Book** | 1 MB | 8 MB | Drug equivalents | ✅ Downloaded |
| **CVX Codes** | 54 KB | N/A | 288 vaccines | ✅ Downloaded |

**Total Size**: ~1.2 GB compressed, ~3+ GB extracted

### **Current Problems**

1. **Repository Bloat**: 1.2 GB in git repo (NPPES alone is 1 GB)
2. **All-or-Nothing**: Users must download everything or manually manage files
3. **No API Integration**: Missing live data access for UMLS, RxNorm, Census
4. **No Tier Gating**: Free tier gets same datasets as Enterprise
5. **Update Complexity**: Manual download and extract process

---

## 🏗️ **Proposed Architecture: Data Source Plugins**

### **Plugin-Based Data Access**

Similar to our AI model management (`pidgeon ai`), create a dedicated data management system with pluggable data sources.

```csharp
namespace Pidgeon.Data.Abstractions
{
    /// <summary>
    /// Represents a pluggable data source for realistic healthcare data generation.
    /// </summary>
    public interface IDataSourcePlugin
    {
        string SourceId { get; }           // e.g., "rxnorm-api", "embedded-medications"
        DataAccessTier RequiredTier { get; }  // Free, Professional, Enterprise
        bool RequiresConfiguration { get; }    // Needs API keys, etc.
        bool IsAvailable { get; }              // Currently accessible

        Task<Result<bool>> ValidateAccessAsync(CancellationToken ct);
    }

    /// <summary>
    /// Plugin for medication data sources
    /// </summary>
    public interface IMedicationDataSource : IDataSourcePlugin
    {
        Task<Result<Medication>> GetMedicationAsync(string query, CancellationToken ct);
        Task<Result<IEnumerable<Medication>>> SearchMedicationsAsync(string searchTerm, CancellationToken ct);
        Task<Result<IEnumerable<Medication>>> GetCommonMedicationsAsync(int count, CancellationToken ct);
    }

    /// <summary>
    /// Plugin for demographic data sources
    /// </summary>
    public interface IDemographicDataSource : IDataSourcePlugin
    {
        Task<Result<PersonName>> GenerateNameAsync(DemographicConstraints constraints, CancellationToken ct);
        Task<Result<Address>> GenerateAddressAsync(string? state = null, string? zipCode = null, CancellationToken ct);
    }

    /// <summary>
    /// Plugin for diagnosis/procedure code sources
    /// </summary>
    public interface IDiagnosisCodeSource : IDataSourcePlugin
    {
        Task<Result<DiagnosisCode>> GetCodeAsync(string code, CancellationToken ct);
        Task<Result<IEnumerable<DiagnosisCode>>> SearchCodesAsync(string searchTerm, CancellationToken ct);
        Task<Result<IEnumerable<DiagnosisCode>>> GetCommonCodesAsync(int count, string? specialty = null, CancellationToken ct);
    }

    public enum DataAccessTier
    {
        Free,           // Embedded small datasets
        Professional,   // API access + downloadable datasets
        Enterprise      // Complete datasets + custom data
    }
}
```

---

## 🎯 **Tiered Data Access Strategy**

### **Free Tier (Embedded)**

**Philosophy**: Covers 70% of test scenarios, drives adoption, zero configuration

**Implementation**: Small JSON files embedded as resources (~1-2 MB total)

| Data Type | Coverage | Storage |
|-----------|----------|---------|
| Medications | Top 25 (Lisinopril, Metformin, etc.) | Embedded JSON |
| Patient Names | 50 first names, 50 last names (diverse) | Embedded JSON |
| Addresses | 10 generic US addresses | Embedded JSON |
| ICD-10 Codes | Top 100 common diagnoses | Embedded JSON |
| CPT Codes | Top 50 procedures | Embedded JSON |
| Lab Tests (LOINC) | Top 50 common tests | Embedded JSON |

**Example**:
```csharp
public class EmbeddedMedicationSource : IMedicationDataSource
{
    public string SourceId => "embedded-medications";
    public DataAccessTier RequiredTier => DataAccessTier.Free;
    public bool RequiresConfiguration => false;

    private static readonly List<Medication> _medications = LoadEmbeddedMedications();

    private static List<Medication> LoadEmbeddedMedications()
    {
        var json = ResourceLoader.LoadEmbeddedResource("Data/medications.json");
        return JsonSerializer.Deserialize<List<Medication>>(json) ?? new();
    }
}
```

### **Professional Tier**

**Philosophy**: API access + downloadable datasets, user manages API keys (BYOK pattern)

**Two Access Modes**:

#### **Mode 1: API Access (BYOK)**

Users provide their own API keys for:
- **RxNorm REST API** (free from NIH/NLM)
- **UMLS Terminology Services** (free with registration)
- **US Census Bureau API** (free with registration)
- **FDA API** (no key required)

**Benefits**:
- Always up-to-date data
- No storage overhead
- Local SQLite caching for offline use

**CLI Configuration**:
```bash
pidgeon data configure --source rxnorm --api-key YOUR_NLM_API_KEY
pidgeon data configure --source umls --api-key YOUR_UMLS_API_KEY
pidgeon data configure --source census --api-key YOUR_CENSUS_KEY
```

#### **Mode 2: Downloadable Datasets**

Users can download curated datasets for offline use:

```bash
pidgeon data download --dataset icd10-full        # 15 MB compressed
pidgeon data download --dataset rxnorm-cache      # 50 MB compressed
pidgeon data download --dataset census-names      # 5 MB compressed
pidgeon data download --dataset loinc-common      # 10 MB compressed
```

**Storage**: `~/.pidgeon/data/datasets/`

**Cache Strategy**: Local SQLite database with LRU eviction

### **Enterprise Tier**

**Philosophy**: Complete datasets + custom data hosting + no API rate limits

**Capabilities**:
- **Private dataset hosting**: Custom formularies, demographics
- **Historical data**: ICD-9, legacy code sets
- **Custom datasets**: Import organization-specific reference data
- **Unlimited API usage**: No rate limiting on cloud services
- **Team sharing**: Centralized data sources for teams

---

## 🔌 **Data Source Implementation Examples**

### **Example 1: Embedded (Free Tier)**

```csharp
namespace Pidgeon.Data.Sources.Embedded
{
    public class EmbeddedMedicationSource : IMedicationDataSource
    {
        public string SourceId => "embedded-medications";
        public DataAccessTier RequiredTier => DataAccessTier.Free;
        public bool RequiresConfiguration => false;
        public bool IsAvailable => true;

        private static readonly Lazy<List<Medication>> _medications =
            new(() => LoadEmbeddedMedications());

        public async Task<Result<IEnumerable<Medication>>> GetCommonMedicationsAsync(
            int count,
            CancellationToken ct)
        {
            await Task.Yield();
            return Result<IEnumerable<Medication>>.Success(_medications.Value.Take(count));
        }

        private static List<Medication> LoadEmbeddedMedications()
        {
            var json = ResourceLoader.LoadEmbeddedResource("Data/medications.json");
            return JsonSerializer.Deserialize<List<Medication>>(json) ?? new();
        }
    }
}
```

### **Example 2: API Access with Caching (Professional Tier)**

```csharp
namespace Pidgeon.Data.Sources.Api
{
    public class RxNormApiMedicationSource : IMedicationDataSource
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IDataSourceConfiguration _config;
        private readonly ILocalCache _cache;

        public string SourceId => "rxnorm-api";
        public DataAccessTier RequiredTier => DataAccessTier.Professional;
        public bool RequiresConfiguration => true;
        public bool IsAvailable => !string.IsNullOrEmpty(_config.GetApiKey("rxnorm"));

        public async Task<Result<Medication>> GetMedicationAsync(
            string query,
            CancellationToken ct)
        {
            // 1. Check local cache first (SQLite)
            if (_cache.TryGet<Medication>($"med:{query}", out var cached))
                return Result<Medication>.Success(cached);

            // 2. Check API key configuration
            var apiKey = _config.GetApiKey("rxnorm");
            if (string.IsNullOrEmpty(apiKey))
            {
                return Result<Medication>.Failure(
                    "RxNorm API key not configured. Run: pidgeon data configure --source rxnorm"
                );
            }

            // 3. Call RxNorm REST API
            var client = _httpClientFactory.CreateClient("RxNorm");
            var response = await client.GetAsync(
                $"https://rxnav.nlm.nih.gov/REST/rxcui.json?name={Uri.EscapeDataString(query)}",
                ct
            );

            if (!response.IsSuccessStatusCode)
                return Result<Medication>.Failure($"RxNorm API error: {response.StatusCode}");

            var content = await response.Content.ReadAsStringAsync(ct);
            var medication = ParseRxNormResponse(content, query);

            // 4. Cache the result (30-day TTL)
            await _cache.SetAsync($"med:{query}", medication, TimeSpan.FromDays(30), ct);

            return Result<Medication>.Success(medication);
        }
    }
}
```

### **Example 3: Downloadable Dataset (Professional Tier)**

```csharp
namespace Pidgeon.Data.Sources.Downloaded
{
    public class DownloadedICD10Source : IDiagnosisCodeSource
    {
        private readonly IDatasetManager _datasetManager;
        private readonly ILocalDatabase _db;

        public string SourceId => "icd10-full";
        public DataAccessTier RequiredTier => DataAccessTier.Professional;
        public bool RequiresConfiguration => false;
        public bool IsAvailable => _datasetManager.IsDatasetAvailable("icd10-full");

        public async Task<Result<bool>> ValidateAccessAsync(CancellationToken ct)
        {
            if (!_datasetManager.IsDatasetAvailable("icd10-full"))
            {
                return Result<bool>.Failure(
                    "ICD-10 full dataset not downloaded. Run: pidgeon data download --dataset icd10-full"
                );
            }

            return Result<bool>.Success(true);
        }

        public async Task<Result<DiagnosisCode>> GetCodeAsync(string code, CancellationToken ct)
        {
            // Query from local SQLite database
            var sql = "SELECT * FROM icd10_codes WHERE code = @code";
            var result = await _db.QuerySingleOrDefaultAsync<DiagnosisCode>(sql, new { code }, ct);

            if (result == null)
                return Result<DiagnosisCode>.Failure($"ICD-10 code not found: {code}");

            return Result<DiagnosisCode>.Success(result);
        }
    }
}
```

---

## 🛠️ **CLI Command Structure**

### **Command Decision: Top-Level `data` Command**

**Rationale**: Parallel to `ai` command, manages data resources (not models)

```bash
pidgeon data <subcommand> [options]
```

### **Subcommands**

#### **1. `pidgeon data sources` - List Available Data Sources**

```bash
pidgeon data sources

# Output:
# FREE TIER (Available):
#   ✓ embedded-medications (25 medications)
#   ✓ embedded-demographics (50 names, 10 addresses)
#   ✓ embedded-icd10 (100 common codes)
#
# PROFESSIONAL TIER:
#   API Sources:
#     ✓ rxnorm-api (Configured, 1,234 cached)
#     ✗ umls-api (Not configured - run: pidgeon data configure --source umls)
#     ✗ census-api (Not configured - run: pidgeon data configure --source census)
#
#   Downloaded Datasets:
#     ✓ icd10-full (71,486 codes, 15 MB)
#     ✗ loinc-full (Not downloaded - run: pidgeon data download --dataset loinc-full)
#
# ENTERPRISE TIER:
#   [Requires Enterprise license]
```

#### **2. `pidgeon data configure` - Configure API Access**

```bash
# Configure RxNorm API access
pidgeon data configure --source rxnorm --api-key YOUR_NLM_API_KEY

# Configure UMLS Terminology Services
pidgeon data configure --source umls --api-key YOUR_UMLS_API_KEY

# Configure US Census Bureau API
pidgeon data configure --source census --api-key YOUR_CENSUS_KEY

# List configured sources
pidgeon data configure --list

# Remove API key
pidgeon data configure --source rxnorm --remove
```

#### **3. `pidgeon data download` - Download Datasets**

```bash
# Download specific dataset
pidgeon data download --dataset icd10-full

# Download with progress
pidgeon data download --dataset rxnorm-cache --verbose

# List available downloads
pidgeon data download --list

# Download multiple datasets
pidgeon data download --dataset icd10-full,loinc-common,census-names

# Output:
# Downloading ICD-10 Full Dataset...
# [========================================] 100% (15 MB)
# ✓ Downloaded to: ~/.pidgeon/data/datasets/icd10-full
# ✓ Loaded 71,486 diagnosis codes
#
# Ready to use! Try:
#   pidgeon generate ADT^A01 --count 10
```

#### **4. `pidgeon data cache` - Manage Local Cache**

```bash
# Show cache statistics
pidgeon data cache stats

# Output:
# Cache Statistics:
#   Total Size: 245 MB
#   Items Cached: 12,456
#   Hit Rate: 87.3%
#
# By Source:
#   rxnorm-api: 1,234 items (120 MB) - Last used: 2 hours ago
#   umls-api: 856 items (85 MB) - Last used: 1 day ago
#   census-api: 445 items (40 MB) - Last used: 3 hours ago

# Clear cache for specific source
pidgeon data cache clear --source rxnorm

# Clear entire cache
pidgeon data cache clear --all

# Set cache size limit
pidgeon data cache config --max-size 500MB
```

#### **5. `pidgeon data info` - Dataset Information**

```bash
# Get info about a dataset
pidgeon data info --dataset icd10-full

# Output:
# Dataset: ICD-10-CM Full Codes (2026)
#   ID: icd10-full
#   Version: 2026
#   Size: 15 MB compressed, 50 MB extracted
#   Records: 71,486 diagnosis codes
#   Last Updated: October 1, 2025
#
# Tier: Professional
# Status: Downloaded ✓
# Location: ~/.pidgeon/data/datasets/icd10-full
#
# Description:
#   Complete ICD-10-CM diagnosis code set for realistic
#   diagnosis generation in HL7 DG1 segments.
#
# Source: https://www.cms.gov/medicare/coding-billing/icd-10-codes
# License: Public Domain (CMS)
```

---

## 📦 **Dataset Manager Service**

```csharp
namespace Pidgeon.Data.Services
{
    public interface IDatasetManager
    {
        /// <summary>
        /// Downloads a dataset and stores it locally
        /// </summary>
        Task<Result<DatasetDownloadResult>> DownloadDatasetAsync(
            string datasetId,
            IProgress<DownloadProgress>? progress = null,
            CancellationToken ct = default
        );

        /// <summary>
        /// Checks if a dataset is available locally
        /// </summary>
        bool IsDatasetAvailable(string datasetId);

        /// <summary>
        /// Gets metadata about an available dataset
        /// </summary>
        Result<DatasetMetadata> GetDatasetMetadata(string datasetId);

        /// <summary>
        /// Removes a downloaded dataset to free space
        /// </summary>
        Task<Result<bool>> RemoveDatasetAsync(string datasetId, CancellationToken ct = default);

        /// <summary>
        /// Lists all downloadable datasets
        /// </summary>
        Task<Result<IEnumerable<DatasetMetadata>>> ListAvailableDatasetsAsync(CancellationToken ct = default);

        /// <summary>
        /// Lists all locally downloaded datasets
        /// </summary>
        Task<Result<IEnumerable<DatasetInfo>>> ListDownloadedDatasetsAsync(CancellationToken ct = default);
    }

    public record DatasetMetadata(
        string Id,
        string Name,
        string Description,
        long CompressedSizeBytes,
        long ExtractedSizeBytes,
        DataAccessTier RequiredTier,
        string Version,
        DateTime LastUpdated,
        string SourceUrl,
        string License
    );

    public record DatasetInfo(
        string Id,
        string Name,
        long SizeBytes,
        DateTime DownloadedAt,
        DateTime LastAccessed,
        string LocalPath,
        int RecordCount
    );
}
```

---

## 💾 **Local Storage Strategy**

### **Directory Structure**

```
~/.pidgeon/
├── data/
│   ├── cache/                      # API response cache (SQLite)
│   │   ├── rxnorm.db               # RxNorm API cache
│   │   ├── umls.db                 # UMLS API cache
│   │   └── census.db               # Census API cache
│   │
│   ├── datasets/                   # Downloaded datasets
│   │   ├── icd10-full/
│   │   │   ├── metadata.json
│   │   │   └── codes.db            # SQLite database
│   │   ├── rxnorm-cache/
│   │   │   ├── metadata.json
│   │   │   └── medications.db
│   │   └── census-names/
│   │       ├── metadata.json
│   │       └── names.db
│   │
│   └── config/
│       └── data-sources.json       # API keys, configuration
```

### **Cache Strategy**

**Three-Tier Caching**:
1. **Memory Cache**: Hot data (last 1000 items)
2. **SQLite Cache**: Warm data (API responses with TTL)
3. **Downloaded Datasets**: Cold data (complete local datasets)

**Cache Policies**:
- **TTL-based expiration**: 30 days for medications, 90 days for codes
- **LRU eviction**: When cache exceeds configured size limit
- **Offline-first**: Use cache even if API is unavailable

---

## 🌐 **API Integration Strategy**

### **RxNorm REST API**

**Endpoint**: https://rxnav.nlm.nih.gov/REST/
**Authentication**: None required (free public API)
**Rate Limits**: 20 requests/second (generous)

**Key Operations**:
- Search by name: `/rxcui.json?name={drug}`
- Get drug info: `/rxcui/{rxcui}/properties.json`
- Get related drugs: `/rxcui/{rxcui}/related.json`

**Caching**: 30-day TTL (drug data changes infrequently)

### **UMLS Terminology Services API**

**Endpoint**: https://uts-ws.nlm.nih.gov/rest/
**Authentication**: API key (free with NLM registration)
**Rate Limits**: Reasonable for individual use

**Key Operations**:
- Search concepts: `/search/current?string={term}`
- Get concept details: `/content/current/CUI/{cui}`
- LOINC/SNOMED lookups

**Caching**: 90-day TTL (terminology stable)

### **US Census Bureau API**

**Endpoint**: https://api.census.gov/data/
**Authentication**: API key (free registration)
**Rate Limits**: 500 requests/day free tier

**Key Operations**:
- Name frequency: `/surnames?name={name}`
- Geographic data: `/acs/acs5?get=NAME&for=state:{fips}`

**Caching**: 365-day TTL (census data changes annually)

### **FDA Drug API**

**Endpoint**: https://api.fda.gov/drug/
**Authentication**: Optional (higher rate limits with key)
**Rate Limits**: 240 requests/hour without key, 1000/hour with key

**Key Operations**:
- Drug events: `/event.json?search={query}`
- Drug labels: `/label.json?search={query}`
- NDC directory: `/ndc.json?search={query}`

**Caching**: 30-day TTL

---

## 🔐 **Configuration Management**

### **API Key Storage**

**File**: `~/.pidgeon/data/config/data-sources.json`

```json
{
  "version": "1.0",
  "sources": {
    "rxnorm-api": {
      "type": "api",
      "enabled": true,
      "requiresKey": false,
      "baseUrl": "https://rxnav.nlm.nih.gov/REST/"
    },
    "umls-api": {
      "type": "api",
      "enabled": true,
      "requiresKey": true,
      "apiKey": "ENCRYPTED:xxxxxxxxxxxxx",
      "baseUrl": "https://uts-ws.nlm.nih.gov/rest/"
    },
    "census-api": {
      "type": "api",
      "enabled": false,
      "requiresKey": true,
      "apiKey": null,
      "baseUrl": "https://api.census.gov/data/"
    }
  },
  "cache": {
    "maxSizeMB": 500,
    "defaultTtlDays": 30
  }
}
```

**Security**: API keys encrypted at rest using DPAPI (Windows) or Keychain (macOS)

---

## 📈 **Business Model Alignment**

### **Free Tier**

**Value Proposition**: "Generate realistic test data instantly, zero setup"

**Data Sources**:
- Embedded datasets only (25 meds, 50 names, 100 codes)
- No API access
- No downloadable datasets
- ~2 MB total storage

**Conversion Trigger**:
```bash
$ pidgeon generate RDE^O11 --medication "Eliquis 5mg"

⚠️  The medication "Eliquis 5mg" is not in the free embedded dataset.

Upgrade to Professional for:
  ✓ RxNorm API access (60,000+ medications)
  ✓ Downloadable ICD-10 full dataset (71,000+ codes)
  ✓ US Census demographics API
  ✓ Local caching for offline use

Try Professional free for 14 days: pidgeon upgrade --trial
```

### **Professional Tier ($29/month)**

**Value Proposition**: "API access + curated datasets, bring your own keys"

**Data Sources**:
- All Free tier sources
- API access with BYOK (user provides NLM, Census keys)
- Downloadable curated datasets (ICD-10, LOINC, RxNorm cache)
- Local SQLite caching
- ~500 MB typical storage

**Upgrade Path**: "Need unlimited API usage? Enterprise tier removes rate limits"

### **Enterprise Tier ($199/seat)**

**Value Proposition**: "Complete datasets + custom data + unlimited usage"

**Data Sources**:
- All Professional sources
- Complete NPPES (7M providers)
- Complete SNOMED (350K concepts)
- Custom dataset uploads
- No API rate limits (Pidgeon-hosted endpoints)
- Team-shared data sources
- ~5 GB+ storage

---

## 🚀 **Implementation Roadmap**

### **Phase 1: Foundation (Week 1)**

**Goal**: Core plugin architecture and CLI commands

- [ ] Create `IDataSourcePlugin` abstraction
- [ ] Create `IMedicationDataSource`, `IDemographicDataSource`, `IDiagnosisCodeSource` interfaces
- [ ] Implement `DatasetManager` service
- [ ] Create `pidgeon data sources` command
- [ ] Create embedded data sources (Free tier)

### **Phase 2: API Integration (Week 2)**

**Goal**: Professional tier API access with caching

- [ ] Implement RxNorm API plugin
- [ ] Implement UMLS API plugin (LOINC/SNOMED)
- [ ] Implement Census API plugin
- [ ] Create `pidgeon data configure` command
- [ ] Create local SQLite cache infrastructure
- [ ] Create `pidgeon data cache` command

### **Phase 3: Downloadable Datasets (Week 3)**

**Goal**: Professional tier dataset downloads

- [ ] Create dataset download infrastructure
- [ ] Package curated datasets (ICD-10, LOINC, etc.)
- [ ] Implement `pidgeon data download` command
- [ ] Implement `pidgeon data info` command
- [ ] Create dataset SQLite loaders

### **Phase 4: Enterprise Features (Week 4)**

**Goal**: Enterprise tier complete datasets + custom uploads

- [ ] Implement complete NPPES dataset support
- [ ] Implement complete SNOMED dataset support
- [ ] Create custom dataset upload infrastructure
- [ ] Implement team sharing (if applicable)

---

## ✅ **Success Criteria**

### **Technical**

- [ ] Zero embedded large datasets in git repo
- [ ] Users can download only what they need
- [ ] API integration with local caching works offline
- [ ] Free tier requires zero configuration
- [ ] Professional tier API setup takes <5 minutes
- [ ] Dataset downloads have progress indicators
- [ ] Cache hit rate >80% for common operations

### **Business**

- [ ] Clear Free → Pro conversion path
- [ ] BYOK pattern reduces Pidgeon's API costs
- [ ] Enterprise tier offers clear value over Pro
- [ ] Documentation shows how to get API keys

### **User Experience**

- [ ] `pidgeon data sources` shows clear status
- [ ] Error messages guide users to solutions
- [ ] Offline mode works with cache
- [ ] Dataset sizes communicated before download

---

## 🤔 **Open Questions**

### **LOINC / SNOMED / RxNorm Licensing**

**LOINC**:
- ✅ Free download with registration
- ✅ Free redistribution allowed (with attribution)
- ✅ API access via UMLS (free with NLM key)
- **Recommendation**: Download + cache approach (Professional tier)

**SNOMED CT**:
- ⚠️ Free in US only (UMLS license)
- ⚠️ Redistribution restricted
- ✅ API access via UMLS (free with NLM key)
- **Recommendation**: API-only (no bundled datasets), Professional tier

**RxNorm**:
- ✅ Public domain
- ✅ Free redistribution
- ✅ Free REST API (no authentication)
- **Recommendation**: API + downloadable cache (Professional tier)

### **Census Data**

**US Census Bureau**:
- ✅ Public domain
- ✅ Free API with registration
- ✅ Free redistribution
- **Recommendation**: API + downloadable cache (Professional tier)

### **Medication Data Strategy**

**Options**:
1. **RxNorm API only** (Pro) - Always current, requires internet
2. **Curated RxNorm snapshot** (Pro download) - Offline, periodic updates
3. **Hybrid** (Pro) - API with local cache fallback

**Recommendation**: **Hybrid approach**
- Default to RxNorm API (always current)
- Cache responses locally (30-day TTL)
- Offer downloadable snapshot for offline use
- Free tier gets embedded 25 medications

---

## 📝 **Next Steps**

1. **Review this plan** - Align with team on approach
2. **Update LEDGER** - Document architectural decision
3. **Create Free tier datasets** - Curate top 25 meds, 50 names, 100 codes
4. **Implement Phase 1** - Core plugin architecture
5. **Document API key acquisition** - Guide users through NLM, Census registration

---

**Document Owner**: Technical Founder
**Reviewers**: Healthcare Consultant, Product Lead
**Related Documents**:
- `docs/LEDGER.md` (LEDGER-044)
- `docs/roadmap/CLI_REFERENCE.md` (Command structure)
- `docs/founding_plan/business_model.md` (Tier strategy)
- `pidgeon/src/Pidgeon.Data/datasets/README.md` (Current datasets)
