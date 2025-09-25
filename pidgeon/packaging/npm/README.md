# Pidgeon CLI

Healthcare message testing and validation CLI

[![npm version](https://badge.fury.io/js/%40pidgeonhealth%2Fcli.svg)](https://badge.fury.io/js/%40pidgeonhealth%2Fcli)
[![License: MPL 2.0](https://img.shields.io/badge/License-MPL%202.0-brightgreen.svg)](https://opensource.org/licenses/MPL-2.0)

## Installation

```bash
npm install -g @pidgeonhealth/cli
```

## Quick Start

```bash
# Check version
pidgeon --version

# Generate HL7 messages
pidgeon generate message --type ADT^A01 --count 5

# Validate messages
pidgeon validate --file sample.hl7

# De-identify PHI
pidgeon deident --in ./samples --out ./cleaned

# Analyze vendor patterns
pidgeon config analyze --samples ./messages
```

## Features

- **Multi-standard Support**: HL7 v2.3/v2.4, FHIR R4, NCPDP SCRIPT
- **Message Generation**: Create realistic test data for any healthcare standard
- **Advanced Validation**: Vendor-specific pattern detection and compliance checking
- **PHI De-identification**: On-premises PHI removal with referential integrity
- **Workflow Wizard**: Interactive multi-step scenario creation
- **AI-Powered Analysis**: Intelligent message comparison and debugging

## Healthcare Standards Supported

### HL7 v2.3/v2.4
- **ADT** (Admit/Discharge/Transfer)
- **ORU** (Observation Result)
- **ORM** (Order)
- **RDE** (Pharmacy/Treatment Encoded Order)

### FHIR R4
- **Patient** resources
- **Encounter** resources
- **Observation** resources
- **Medication** resources

### NCPDP SCRIPT
- **NewRx** transactions
- **Refill** requests
- **Cancel** transactions

## Usage Examples

### Generate Test Messages

```bash
# Generate ADT messages
pidgeon generate message --type ADT^A01 --count 10 --output ./adt-messages.hl7

# Generate FHIR Patient resources
pidgeon generate fhir --resource Patient --count 5 --format json

# Generate pharmacy orders
pidgeon generate message --type RDE^O11 --count 3
```

### Validate Messages

```bash
# Validate single file
pidgeon validate --file message.hl7

# Validate directory
pidgeon validate --directory ./messages --format detailed

# Validate with vendor patterns
pidgeon validate --file message.hl7 --vendor epic
```

### De-identify PHI

```bash
# Basic de-identification
pidgeon deident --in ./raw-messages --out ./cleaned-messages

# Advanced options
pidgeon deident \
  --in ./samples \
  --out ./synthetic \
  --date-shift 30d \
  --preserve-demographics \
  --seed 12345
```

## Programmatic Usage

You can also use Pidgeon CLI programmatically in Node.js:

```javascript
const pidgeon = require('@pidgeonhealth/cli');

// Run commands programmatically
await pidgeon.run(['generate', 'message', '--type', 'ADT^A01']);

// Get binary path for custom usage
console.log(pidgeon.binaryPath);
```

## Platform Support

This package automatically downloads the appropriate binary for your platform:

- **Windows**: x64, ARM64
- **macOS**: x64 (Intel), ARM64 (Apple Silicon)
- **Linux**: x64, ARM64

## Documentation

- **Full Documentation**: https://docs.pidgeon.health
- **API Reference**: https://docs.pidgeon.health/api
- **Examples**: https://docs.pidgeon.health/examples

## Support

- **Issues**: https://github.com/PidgeonHealth/pidgeon/issues
- **Discussions**: https://github.com/PidgeonHealth/pidgeon/discussions
- **Email**: support@pidgeon.health

## License

Mozilla Public License 2.0 - see [LICENSE](https://github.com/PidgeonHealth/pidgeon/blob/main/LICENSE) for details.