# Pidgeon Healthcare Interoperability Platform

**Enterprise-Ready Healthcare Testing Without PHI Compliance Nightmares**

Pidgeon is a complete healthcare interoperability platform for generating, validating, and testing HL7, FHIR, and NCPDP messages with realistic synthetic data. Built for integration engineers, consultants, informaticists, and healthcare IT teams who need production-quality test scenarios without the legal complexity of using real patient data.

## 🎯 Project Status

- **Version**: 0.1.0 (P0 MVP Complete)
- **Health Score**: 100/100 - Clean architecture maintained ✅
- **Build Status**: All features functional ✅
- **Distribution**: Self-contained executables ready for 6 platforms ✅
- **Test Coverage**: Unit (6/6), Integration (8/8), Regression (passing) ✅
- **Current Phase**: Sprint 2 - Scaling the Foundation

## 🚀 Key Features

### Core Platform (Free - CLI)
- **Message Generation**: Realistic HL7 v2.3, FHIR R4, NCPDP SCRIPT messages with smart standard inference
- **De-identification**: HIPAA Safe Harbor compliant PHI removal with cross-message referential integrity
- **Validation Engine**: Multi-mode validation with vendor compatibility checking
- **Vendor Pattern Detection**: Smart inference and configuration management for vendor-specific implementations
- **Semantic Path System**: Cross-standard field access (`patient.mrn` works for HL7 and FHIR)
- **Session Management**: Maintain patient context across commands for workflow automation
- **Lock-Aware Generation**: Preserve locked values while regenerating other fields
- **Standards Lookup**: Complete HL7 v2.3 dictionary with 784 components (segments, fields, tables, datatypes)
- **Path Discovery**: Find and explore semantic paths across healthcare standards

### Professional Tier Features (Pro/Enterprise)
- **Workflow Wizard**: Interactive guided scenarios for multi-step healthcare testing ⚠️ Beta
- **Diff + AI Triage**: Field-aware comparison with AI-powered troubleshooting hints ⚠️ Beta
- **Scenario Generation**: Complete clinical workflow bundles
- **FHIR Search Simulation**: Realistic server response testing

## 📦 Repository Structure

```
hl7generator/
├── pidgeon/              # Core CLI application (.NET 8)
│   ├── src/
│   │   ├── Pidgeon.CLI/      # Command-line interface (14 commands)
│   │   ├── Pidgeon.Core/     # Business logic & domain models
│   │   └── Pidgeon.Data/     # Embedded resources & data access
│   ├── scripts/
│   │   ├── build.sh          # Cross-platform build script (Unix/WSL)
│   │   └── build.ps1         # PowerShell build script (Windows)
│   └── tests/
│       └── Pidgeon.Tests/    # C# unit tests (xUnit)
│
├── pidgeon-tests/        # Comprehensive test suite
│   ├── unit/                 # Fast unit tests (6 passing)
│   ├── integration/          # End-to-end CLI tests (8 passing)
│   ├── regression/           # Shell script validation suites
│   └── quick_validation.sh   # 30-second developer validation
│
├── pidgeon-gui/          # Next.js web interface (planned/WIP)
│   └── src/                  # TypeScript + Tailwind UI components
│
└── private/              # Development docs & architecture (gitignored)
    └── docs/                 # Roadmap, user stories, technical specs
```

## 🎬 Quick Start

### Using Pre-Built Binaries

Download the latest release for your platform from `/pidgeon/packages/`:
- **Windows**: `pidgeon-win-x64.tar.gz` or `pidgeon-win-arm64.tar.gz`
- **Linux**: `pidgeon-linux-x64.tar.gz` or `pidgeon-linux-arm64.tar.gz`
- **macOS**: `pidgeon-osx-arm64.tar.gz` (Apple Silicon) or `pidgeon-osx-x64.tar.gz` (Intel)

Extract and run:
```bash
tar -xzf pidgeon-<platform>.tar.gz
cd pidgeon-<platform>
./Pidgeon.CLI --help  # Linux/macOS
.\Pidgeon.CLI.exe --help  # Windows
```

### Building from Source

**Prerequisites**:
- .NET 8.0 SDK or later
- Bash (Unix/WSL) or PowerShell (Windows)

**Build all platforms**:
```bash
cd pidgeon

# Unix/WSL
bash scripts/build.sh --clean --version "0.1.0"

# Windows PowerShell
.\scripts\build.ps1 -Clean -Version "0.1.0"
```

Binaries output to `pidgeon/packages/` directory.

## 💻 Usage Examples

### Generate Realistic HL7 Messages
```bash
# Single ADT admission message
pidgeon generate ADT^A01

# 10 lab results with deterministic generation
pidgeon generate ORU^R01 --count 10 --output labs.hl7

# FHIR Observation resources
pidgeon generate Observation --standard fhir --count 5
```

### Session-Based Workflows (Maintain Patient Context)
```bash
# Create a session and set patient MRN
pidgeon session create workflow_demo
pidgeon set patient.mrn "PAT123456" --session workflow_demo

# Generate ADT admission - uses locked patient MRN
pidgeon generate ADT^A01 --session workflow_demo

# Generate lab results for same patient
pidgeon generate ORU^R01 --session workflow_demo

# Patient "PAT123456" is consistent across both messages!
```

### De-Identify Real Messages
```bash
# De-identify real HL7 files (HIPAA Safe Harbor compliant)
pidgeon deident ./prod_messages/ ./sanitized_messages/ --date-shift 90

# Preserves referential integrity across related messages
```

### Validate Messages
```bash
# Validate against HL7 v2.3 standards
pidgeon validate message.hl7

# Validate with vendor compatibility mode
pidgeon validate message.hl7 --mode compatibility
```

### Discover Semantic Paths
```bash
# Find how to access patient MRN across standards
pidgeon path patient.mrn

# Output:
# HL7v23: PID.3
# FHIR: Patient.identifier.value
```

### Standards Lookup
```bash
# Look up segment definition
pidgeon lookup PID

# Look up specific field
pidgeon lookup PID.3

# Look up HL7 table
pidgeon lookup table:0001
```

## 🧪 Testing

### Quick Validation (30 seconds)
```bash
cd pidgeon-tests
./quick_validation.sh
```

Validates core MVP functionality before commits.

### Run All Tests
```bash
# Unit tests (fast)
dotnet test pidgeon-tests/unit/

# Integration tests (end-to-end CLI validation)
dotnet test pidgeon-tests/integration/

# Full regression suite
./pidgeon-tests/regression/mvp_regression_suite.sh
```

All tests passing: **14/14** ✅

## 🏗️ Architecture

**Four-Domain Model** with Plugin Architecture:
- **Clinical Domain**: Healthcare business concepts (Patient, Prescription, Encounter)
- **Messaging Domain**: Wire format structures (HL7_Message, FHIR_Bundle, NCPDP_Message)
- **Configuration Domain**: Vendor patterns and field mappings
- **Transformation Domain**: Cross-standard semantic paths and value locking

**Standards as Plugins**: HL7v23, FHIR, NCPDP support via plugin pattern - core services remain standards-agnostic.

**Field Value Resolution Chain** (Priority 100 → 10):
1. Session locks (explicit user values)
2. HL7-specific field mappings
3. Official HL7 table values
4. Realistic demographics
5. Clinical data (medications, procedures, diagnoses)
6. Identifiers (MRNs, account numbers)
7. Fallback generation

## 📚 Documentation

- **[Pidgeon CLI README](pidgeon/README.md)**: Detailed CLI usage and build instructions
- **[Test Suite README](pidgeon-tests/README.md)**: Testing infrastructure and validation
- **Private Docs**: Architecture, roadmap, user stories (in `/private/docs/` - gitignored)

## 🎯 Development Roadmap

### ✅ P0 MVP Complete (Sprint 0 - Weeks 1-8)
All 6 embryonic features delivered:
1. Message Generation Engine
2. De-identification (HIPAA compliant)
3. Validation Engine
4. Vendor Pattern Detection
5. Workflow Wizard [Pro]
6. Diff + AI Triage [Pro]

### ✅ Sprint 1 Complete (Foundation)
- Cross-standard semantic path system
- Lock-aware generation
- Session management with TTL cleanup
- Rich demographic datasets (500+ realistic values)
- Complete HL7 v2.3 lookup system (784 components)

### 🚧 Sprint 2 In Progress (Scale the Foundation)
Focus on business value delivery:
1. Session import/export + template marketplace
2. Developer experience improvements
3. Database migration for performance
4. Professional tier packaging
5. Cross-standard workflow mastery
6. Enterprise foundation (team collaboration)

### 📅 P1 Expansion (Months 3-5)
- Configuration Manager v1 [Pro]
- Vendor Specification Hub [Pro/Ent]
- FHIR R4 expansion
- Message Studio v1 [Pro] - Natural language to message
- Standards Chatbot [Pro] - AI-powered explanation

## 🤝 Contributing

Contributions welcome! Please:
1. Follow the four-domain architecture pattern
2. Use dependency injection (no static classes)
3. Implement new standards as plugins (don't modify core services)
4. Follow STOP-THINK-ACT error resolution methodology
5. Write behavior-driven tests
6. Update relevant documentation

## 📄 License

This project is licensed under the Mozilla Public License 2.0 - see the LICENSE file for details.

## 🌟 Success Metrics

- **Technical Foundation**: 100/100 health score with clean architecture
- **Test Coverage**: 14/14 tests passing across unit, integration, regression
- **Feature Completeness**: P0 MVP fully delivered (6/6 features)
- **Cross-Platform**: Self-contained executables for Windows, Linux, macOS (x64 + ARM64)
- **Enterprise Ready**: Session management, vendor patterns, professional tier features operational

---

**North Star**: *"Realistic scenario testing without the legal/compliance nightmare of using real patient data"*

Built with ❤️ for the healthcare integration community
