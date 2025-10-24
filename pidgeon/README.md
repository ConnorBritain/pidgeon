# Pidgeon

**Realistic HL7 Message Generation & Validation for Healthcare Integration Testing**

Pidgeon is a cross-platform CLI tool for generating, validating, and testing HL7 v2.x messages with realistic healthcare data. Built for integration engineers, testers, and healthcare IT professionals who need production-quality test data without using real PHI.

## Features

- **Realistic Message Generation**: Creates HL7 messages with semantically accurate data using official HL7 tables and realistic demographics
- **Multiple Message Types**: Supports ADT, ORU, ORM, SIU, and other common HL7 message types
- **Standards Compliant**: Built on official HL7 v2.3 specifications with proper field mappings
- **Vendor Pattern Support**: Configure and replicate vendor-specific HL7 implementations
- **Cross-Platform**: Native binaries for Windows, Linux, and macOS (x64 and ARM64)
- **Self-Contained**: Single-file executables with no runtime dependencies

## Quick Start

### Using Pre-Built Binaries

Download the latest release for your platform:
- **Windows**: `pidgeon-win-x64.tar.gz`
- **Linux**: `pidgeon-linux-x64.tar.gz`
- **macOS**: `pidgeon-osx-arm64.tar.gz` (Apple Silicon) or `pidgeon-osx-x64.tar.gz` (Intel)

Extract and run:
```bash
tar -xzf pidgeon-<platform>.tar.gz
cd pidgeon-<platform>
./pidgeon --help  # Linux/macOS
.\pidgeon.exe --help  # Windows
```

### Building from Source

**Prerequisites**:
- .NET 8.0 SDK or later
- Bash (Unix/WSL) or PowerShell (Windows)

**Build for all platforms**:
```bash
# Unix/WSL
bash scripts/build.sh --clean --version "0.1.0"

# Windows PowerShell
.\scripts\build.ps1 -Clean -Version "0.1.0"
```

Binaries will be in `packages/` directory.

## Usage

### Generate HL7 Messages

```bash
# Generate a single ADT^A01 message
pidgeon generate --type "ADT^A01"

# Generate 10 ORU^R01 lab results
pidgeon generate --type "ORU^R01" --count 10

# Generate with specific configuration
pidgeon generate --type "ORM^O01" --output orders.hl7
```

### Message Output Example

```
MSH|^~\&|PIDGEON^^L|PIDGEON_FACILITY|TARGET^^L|TARGET_FACILITY|20251024003000||ADT^A01|MSG20251024003000|P|2.3
EVN|A01|20251024003000
PID|1||PAT123456||Doe^John^A||19800115|M||2106-3|123 Main Street^^Springfield^IL^62701||555-1234||en|M|
PV1|1|I|ICU^201^A||||ATT123^Smith^Jane|||MED||||ADM|||||VIP123|||||||||||||||||||||||20251024003000
```

### Available Message Types

- **ADT**: Admit, Discharge, Transfer (A01, A04, A08, etc.)
- **ORU**: Observation Results (R01 - Lab Results)
- **ORM**: Order Messages (O01 - General Orders)
- **SIU**: Scheduling Information (S12 - Appointments)
- **FHIR**: Patient, Observation resources
- **NCPDP**: Pharmacy messages

## Architecture

Pidgeon uses a plugin-based architecture with:

- **Core Domain**: Standards-agnostic healthcare concepts (Patient, Prescription, etc.)
- **Field Value Resolvers**: Priority-based chain for realistic data generation
  - HL7 Table-driven values (official coded values)
  - Demographics (realistic names, addresses, phone numbers)
  - Clinical data (medications, procedures, diagnoses)
  - Identifiers (MRNs, account numbers, provider IDs)
- **Standard Adapters**: HL7 v2.x, FHIR, NCPDP serialization
- **Vendor Patterns**: Configurable vendor-specific implementations

## Development

### Project Structure

```
pidgeon/
├── src/
│   ├── Pidgeon.CLI/          # Command-line interface
│   ├── Pidgeon.Core/         # Core business logic and domain models
│   └── Pidgeon.Data/         # Data access and embedded resources
├── tests/
│   └── Pidgeon.Tests/        # Unit and integration tests
├── scripts/
│   ├── build.sh              # Unix/WSL build script
│   └── build.ps1             # PowerShell build script
└── packages/                 # Build output (tar.gz archives)
```

### Running Tests

```bash
dotnet test
```

### Running from Source

```bash
dotnet run --project src/Pidgeon.CLI -- generate --type "ADT^A01"
```

## Build Details

The build scripts create self-contained, single-file executables using:
- **PublishSingleFile**: All dependencies bundled
- **EnableCompressionInSingleFile**: Reduced binary size
- **PublishReadyToRun**: Improved startup performance
- **Self-Contained**: No .NET runtime installation required

### Build Parameters

```bash
# Bash script options
--clean                  # Clean previous builds
--version <version>      # Set version number (default: 0.1.0)
--configuration <config> # Build configuration (default: Release)
--enable-ai              # Enable AI features (requires additional dependencies)

# PowerShell script options
-Clean                   # Clean previous builds
-Version <version>       # Set version number (default: 0.1.0)
-Configuration <config>  # Build configuration (default: Release)
-EnableAI                # Enable AI features
```

## Roadmap

- [x] Core HL7 v2.3 message generation
- [x] Official HL7 table integration
- [x] Realistic demographic data
- [x] Cross-platform standalone builds
- [ ] Message validation engine
- [ ] Vendor pattern detection
- [ ] On-premises de-identification
- [ ] FHIR R4 support
- [ ] NCPDP SCRIPT support

## Contributing

Contributions welcome! Please open an issue or pull request for bugs, features, or improvements.

## License

This project is licensed under the Mozilla Public License 2.0 - see the LICENSE file for details.

## Support

For issues, questions, or feature requests, please open a GitHub issue.
