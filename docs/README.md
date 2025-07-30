# Segmint HL7

Comprehensive HL7 message toolsuite and interface management platform. AI-driven capabilities for message generation, analysis, validation, and configuration management across EHR, pharmacy, and healthcare workflows.

## Overview

This tool addresses a critical gap in healthcare interface testing by providing:

- **Realistic HL7 Message Generation**: Create valid HL7 v2.3 messages compatible with existing healthcare software interfaces
- **AI-Driven Synthetic Data**: Use LangChain to generate clinically appropriate patient demographics, medications, and clinical data
- **Configuration Inference**: Advanced capability to analyze raw HL7 messages and automatically generate interface configurations
- **Vendor Schema Management**: Track and manage changes across multiple vendor HL7 implementations

## Key Features

### 🏗️ **Robust Architecture**
- Clean, modular design for healthcare integration compatibility
- Comprehensive HL7 v2.3 field type system with validation
- Standard and custom Z-segment support for pharmacy workflows

### 🤖 **AI-Powered Data Generation**
- LangChain integration for realistic synthetic data
- Context-aware generation maintaining consistency across related messages
- Constraint-based generation for controlled substances and clinical requirements

### 📊 **Configuration Management**
- **Automated Message Analysis**: Upload raw HL7 messages to automatically infer interface specifications
- **Version Control**: Track vendor schema changes over time
- **Lockable Configurations**: Prevent accidental changes to production configurations
- **Diff Tracking**: Identify and document vendor schema changes automatically

### 🔧 **Multiple Interfaces**
- **Command-line Interface (CLI)**: Powerful batch processing and automation capabilities
- **Graphical User Interface (GUI)**: User-friendly desktop application with interactive message building
- **RESTful API**: Web integration for development and automation workflows
- **Comprehensive Validation**: Multi-level validation and testing capabilities

## Problem Solved

Healthcare organizations struggle with:
- **Manual Schema Mapping**: Time-intensive process of mapping HL7 fields to specifications
- **Vendor Change Tracking**: Difficulty detecting when vendors modify their HL7 implementations
- **Configuration Maintenance**: Complex process of maintaining interface configurations across multiple vendors
- **Testing Data Generation**: Need for realistic test data that doesn't expose PHI

This tool automates these processes, reducing the operational burden of HL7 interface management.

## Quick Start

### Prerequisites

```bash
# Clone the repository
git clone <repository-url>
cd hl7generator

# Install Python dependencies
pip install -r requirements.txt

# Install in development mode
pip install -e .
```

### GUI Interface (Recommended for Beginners)

```bash
# Launch the graphical interface
python segmint.py

# Alternative launch methods:
python -m app.gui.main_window
python app/gui/main_window.py
```

The GUI provides:
- **Interactive Message Generator**: Build messages with forms and real-time preview
- **Visual Configuration Manager**: Manage and organize interface configurations
- **Integrated Validator**: Validate messages with detailed reporting
- **Message Analysis**: Analyze existing HL7 messages to infer specifications
- **Light/Dark Themes**: Healthcare-themed interface with accessibility options

### CLI Interface (Advanced Users)

```bash
# Generate individual messages
segmint generate --type RDE --facility DEMO_FACILITY

# Generate complete workflows  
segmint workflow --type new_prescription --output ./messages/

# Analyze messages to infer configurations
segmint analyze sample_messages.hl7 --name "Demo Interface"

# Validate messages
segmint validate sample_message.hl7 --levels syntax semantic

# Manage configurations
segmint config list --status locked
segmint config create "vendor_config" config.json --user "john.doe"
```

## Architecture

The system is built with modularity and maintainability in mind:

```
app/
├── core/           # Base HL7 classes (Message, Segment, Field)
├── field_types/    # HL7 data types (ST, ID, CE, XPN, etc.)
├── segments/       # Standard and custom segments
├── messages/       # Message type implementations
├── config/         # Configuration management
├── config_analyzer/# Message analysis and inference
├── config_library/ # Configuration library management
├── synthetic/      # AI data generation
├── validation/     # Multi-level validation
└── cli/           # Command-line interface
```

## Supported Message Types

- **RDE**: Pharmacy/Treatment Encoded Order
- **RDS**: Pharmacy/Treatment Dispense
- **ADT**: Admit/Discharge/Transfer
- **ORM**: Order Message
- **ACK**: Acknowledgment

## Custom Z-Segments

Supports pharmacy-specific custom segments:
- **ZPM**: Pyxis Machine Data (automated dispensing)
- **ZMA**: Medication Administration
- **ZAC**: Account Information (correctional facilities)
- **ZJC**: Juvenile Corrections
- And more...

## Contributing

This project follows strict HL7 v2.3 compliance while extending functionality for modern pharmacy workflows. See [ARCHITECTURE.md](./ARCHITECTURE.md) for detailed implementation guidance.

## License

MIT License - see [LICENSE.md](./LICENSE.md) for details.

## Author

Connor England (connor.r.england@gmail.com)

---

*Designed to address healthcare interface challenges while maintaining compatibility with existing EHR, pharmacy, and healthcare systems.*