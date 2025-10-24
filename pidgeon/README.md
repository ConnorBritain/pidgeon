# Pidgeon CLI

**Enterprise-Ready Healthcare Message Generation & Testing Platform**

Pidgeon is a complete command-line platform for healthcare interoperability testing with realistic synthetic data. Generate, validate, de-identify, and test HL7, FHIR, and NCPDP messages without PHI compliance concerns.

## 🎯 Current Status

- **Version**: 0.1.0 (P0 MVP Complete)
- **Health Score**: 100/100 ✅
- **Features**: All 6 P0 features delivered + Sprint 1 enhancements ✅
- **Distribution**: Self-contained executables for 6 platforms ✅
- **Test Coverage**: 14/14 tests passing ✅

## ⚡ Quick Start

### Using Pre-Built Binaries

Download from `packages/` directory and extract:
```bash
tar -xzf pidgeon-<platform>.tar.gz
./pidgeon-<platform>/Pidgeon.CLI --help
```

### Building from Source

**Prerequisites**: .NET 8.0 SDK

```bash
# Build for all platforms
bash scripts/build.sh --clean --version "0.1.0"

# Or run from source
dotnet run --project src/Pidgeon.CLI -- --help
```

## 🚀 Core Features

### 1. Message Generation (Free)
Generate realistic healthcare messages across multiple standards with smart inference:

```bash
# HL7 v2.3 Messages
pidgeon generate ADT^A01                    # Single admission message
pidgeon generate ORU^R01 --count 10        # 10 lab results
pidgeon generate ORM^O01 --output orders.hl7

# FHIR R4 Resources
pidgeon generate Patient --standard fhir --count 5
pidgeon generate Observation --standard fhir

# NCPDP SCRIPT Messages
pidgeon generate NewRx --standard ncpdp
```

**Realistic Data**:
- Official HL7 table values (not "Unknown" placeholders)
- 500+ demographic values (names, addresses, phones)
- Age-appropriate clinical data
- Vendor-specific patterns

### 2. Session Management & Workflow Automation (Free)
Maintain patient context across commands for realistic clinical workflows:

```bash
# Create a session
pidgeon session create patient_journey

# Lock patient identifier
pidgeon set patient.mrn "PAT123456" --session patient_journey

# Generate admission - uses locked MRN
pidgeon generate ADT^A01 --session patient_journey

# Generate labs for same patient
pidgeon generate ORU^R01 --session patient_journey

# All messages share the same patient!
```

**Session Features**:
- TTL-based automatic cleanup
- Conflict resolution
- Audit trails
- Lock-aware generation (preserve values while regenerating others)

### 3. Semantic Path System (Free)
Cross-standard field access with intelligent path resolution:

```bash
# Find patient MRN location across standards
pidgeon path patient.mrn
# Output:
#   HL7v23: PID.3
#   FHIR: Patient.identifier.value

# Set values using semantic paths
pidgeon set patient.mrn "PAT123"
pidgeon set patient.dob "1980-01-15"
pidgeon set encounter.class "inpatient"
```

**Supported Paths**:
- `patient.*` - MRN, name, DOB, gender, address, phone
- `encounter.*` - Class, type, location, attending
- `order.*` - Number, priority, status
- `observation.*` - Code, value, status
- `medication.*` - Code, dosage, route

### 4. De-Identification (Free)
HIPAA Safe Harbor compliant PHI removal with referential integrity:

```bash
# De-identify real production messages
pidgeon deident ./prod_messages/ ./sanitized/ --date-shift 90

# Maintains cross-message consistency:
# - Same patient ID → same synthetic ID
# - Related encounters stay related
# - Date relationships preserved
```

**De-Identification Methods**:
- Deterministic replacement (consistent across messages)
- Date shifting with relationship preservation
- Safe Harbor compliant field removal
- On-premises (no data leaves your machine)

### 5. Message Validation (Free)
Multi-mode validation with detailed error reporting:

```bash
# Standards-based validation
pidgeon validate message.hl7

# Vendor compatibility mode
pidgeon validate message.hl7 --mode compatibility

# Batch validation
pidgeon validate ./messages/*.hl7 --output report.json
```

**Validation Modes**:
- **Standards**: Strict HL7 v2.3/FHIR R4 compliance
- **Compatibility**: Vendor-specific rule enforcement
- **Lenient**: Allow common vendor variations

### 6. Vendor Pattern Detection (Free)
Smart inference and configuration management:

```bash
# Analyze vendor implementation patterns
pidgeon config analyze ./vendor_samples/ --save epic_config.json

# Use vendor configuration
pidgeon generate ADT^A01 --config epic_config.json

# List stored configurations
pidgeon config list
```

**Pattern Intelligence**:
- Field population frequencies
- Vendor-specific conventions
- Custom segment usage
- Non-standard field mappings

### 7. Workflow Wizard ⚠️ Beta [Pro]
Interactive guided scenarios for complex testing:

```bash
pidgeon workflow

# Interactive prompts:
# 1. Select scenario type (patient admission, lab workflow, etc.)
# 2. Configure scenario parameters
# 3. Generate complete multi-message workflows
```

**Scenarios**:
- Patient admission with ancillary orders
- Lab result workflows (order → result → amended result)
- Medication workflows (prescription → dispense → administration)
- Transfer scenarios with ADT^A02/A03/A06 sequences

### 8. Diff + AI Triage ⚠️ Beta [Pro]
Field-aware comparison with AI-powered troubleshooting:

```bash
# Compare two HL7 messages
pidgeon diff message1.hl7 message2.hl7

# Compare directories (folder-to-folder)
pidgeon diff ./envA/ ./envB/ --output diff_report.html

# With AI analysis
pidgeon diff message1.hl7 message2.hl7 --ai-triage
```

**Comparison Features**:
- Field-level diff for HL7
- JSON tree diff for FHIR
- AI-powered hints for common issues
- HTML report generation

### 9. Standards Lookup (Free)
Complete HL7 v2.3 dictionary with 784 components:

```bash
# Segment definitions
pidgeon lookup PID
pidgeon lookup MSH

# Field definitions
pidgeon lookup PID.3
pidgeon lookup PID.5

# HL7 tables
pidgeon lookup table:0001  # Administrative Sex
pidgeon lookup table:0003  # Event Type

# Data types
pidgeon lookup datatype:XPN  # Extended Person Name
```

### 10. FHIR Search Simulation (Free)
Test FHIR search queries with realistic server responses:

```bash
# Simulate FHIR Patient search
pidgeon fhir-search Patient --params "family=Smith&given=John"

# Simulate Observation search
pidgeon search Observation --params "patient=123&code=718-7"
```

### 11. Message Discovery (Free)
Search engine for finding fields and components:

```bash
# Find where patient name appears
pidgeon find "patient name"

# Find all fields containing MRN
pidgeon find "mrn"
```

## 📦 Installation & Distribution

### Build Parameters

```bash
# Bash (Unix/WSL)
bash scripts/build.sh [OPTIONS]
  --clean                  Clean previous builds
  --version <version>      Set version (default: 0.1.0)
  --configuration <config> Build config (default: Release)
  --enable-ai              Include AI features (LLamaSharp)

# PowerShell (Windows)
.\scripts\build.ps1 [OPTIONS]
  -Clean                   Clean previous builds
  -Version <version>       Set version
  -Configuration <config>  Build configuration
  -EnableAI                Include AI features
```

### Build Output

All platforms output to `packages/` directory:
- `pidgeon-win-x64.tar.gz` (43MB binary, 38MB archive)
- `pidgeon-win-arm64.tar.gz` (43MB binary, 37MB archive)
- `pidgeon-linux-x64.tar.gz` (44MB binary, 37MB archive)
- `pidgeon-linux-arm64.tar.gz` (44MB binary, 37MB archive)
- `pidgeon-osx-x64.tar.gz` (44MB binary, 37MB archive)
- `pidgeon-osx-arm64.tar.gz` (44MB binary, 37MB archive)
- `checksums.txt` (SHA256 verification)

**Binary Features**:
- Self-contained (no .NET runtime required)
- Single-file executables
- Embedded resources (no separate data files)
- Cross-platform compatible

## 🏗️ Architecture

### Project Structure

```
src/
├── Pidgeon.CLI/          # Command-line interface
│   ├── Commands/         # 14 command implementations
│   ├── Services/         # CLI-specific services
│   └── Program.cs        # Entry point
├── Pidgeon.Core/         # Business logic & domain models
│   ├── Domain/           # Four-domain architecture
│   │   ├── Clinical/     # Healthcare concepts
│   │   ├── Messaging/    # Wire formats
│   │   ├── Configuration/# Vendor patterns
│   │   └── Transformation/ # Semantic paths
│   ├── Services/         # Core business services
│   │   ├── FieldValueResolvers/ # Priority chain
│   │   ├── Generation/   # Message generation
│   │   ├── Validation/   # Multi-mode validation
│   │   └── DeIdentification/ # PHI removal
│   └── Standards/        # Plugin implementations
│       ├── HL7v23/       # HL7 v2.3 plugin
│       ├── FHIR/         # FHIR R4 plugin
│       └── NCPDP/        # NCPDP plugin
└── Pidgeon.Data/         # Embedded resources
    ├── datasets/         # Demographics, medications, etc.
    └── standards/        # HL7 tables, segments, fields
```

### Design Principles

1. **Plugin Architecture**: Standards implemented as plugins, never modify core
2. **Dependency Injection**: All services injectable, no static classes
3. **Result<T> Pattern**: Explicit error handling, no exceptions for control flow
4. **Four-Domain Model**: Clinical, Messaging, Configuration, Transformation
5. **Standards-Agnostic Core**: Domain models independent of wire formats

### Field Value Resolution Chain

Priority 100 → 10:
1. **Session Locks** (100): Explicit user-set values
2. **HL7 Specific** (90): MSH segment special handling
3. **HL7 Tables** (85): Official coded values from HL7 tables
4. **Demographics** (80): Realistic names, addresses, phones
5. **Clinical** (75): Medications, procedures, diagnoses
6. **Identifiers** (75): MRNs, account numbers, provider IDs
7. **Contact** (75): Phone numbers, email addresses
8. **HL7 Coded Values** (70): General coded value generation
9. **Fallback** (10): Random/default generation

## 🧪 Testing

See `../pidgeon-tests/` for comprehensive test suite:
- Unit tests (C# xUnit): 6/6 passing
- Integration tests (end-to-end CLI): 8/8 passing
- Regression tests (shell scripts): passing
- Quick validation: `./pidgeon-tests/quick_validation.sh` (30 seconds)

## 📚 Complete Command Reference

```bash
pidgeon --help                          # Show all commands
pidgeon <command> --help                # Command-specific help

# Core Commands (Free)
pidgeon generate <type>                 # Generate messages/resources
pidgeon validate <files>                # Validate against standards
pidgeon deident <input> <output>        # De-identify real messages
pidgeon config                          # Vendor pattern management
pidgeon session                         # Session lifecycle management
pidgeon set <field> <value>             # Set field with session awareness
pidgeon path                            # Semantic path discovery
pidgeon lookup <query>                  # Standards dictionary lookup
pidgeon find <query>                    # Search for fields/components
pidgeon search <resource>               # FHIR search simulation
pidgeon fhir-search                     # FHIR search testing

# Professional Commands (Pro/Beta)
pidgeon workflow                        # Interactive workflow wizard
pidgeon diff <paths>                    # Field-aware comparison + AI hints
pidgeon scenario <type>                 # Clinical scenario bundles

# Utility Commands
pidgeon completions <shell>             # Generate shell completions
pidgeon --version                       # Show version
pidgeon --verbose                       # Enable verbose output
```

## 🎯 Use Cases

**Integration Engineers**:
- Generate test data for interface development
- Validate vendor message compliance
- Reproduce production issues with synthetic data

**Healthcare IT Consultants**:
- Demonstrate vendor capabilities
- Compare implementation differences
- Create training scenarios

**QA/Testing Teams**:
- Build comprehensive test suites
- Automate regression testing
- Create edge case scenarios

**Clinical Informaticists**:
- Understand vendor implementations
- Validate workflow sequences
- Test system integrations

## 📄 License

Mozilla Public License 2.0 - See LICENSE file for details.

## 🤝 Contributing

Contributions welcome! Please:
1. Follow four-domain architecture
2. Use dependency injection (no static classes)
3. Implement new standards as plugins
4. Write behavior-driven tests
5. Follow STOP-THINK-ACT error methodology

---

**Built for the healthcare integration community** | **Version 0.1.0** | **P0 MVP Complete**
