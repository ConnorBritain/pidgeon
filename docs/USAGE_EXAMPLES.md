# Segmint HL7 Usage Examples

Complete guide with practical examples for all Segmint HL7 platform components.

## Table of Contents

- [Quick Start](#quick-start)
- [Graphical User Interface (GUI)](#graphical-user-interface-gui)
- [Command Line Interface (CLI)](#command-line-interface-cli)
- [Python API Examples](#python-api-examples)
- [Real-World Scenarios](#real-world-scenarios)
- [Configuration Management](#configuration-management)
- [Validation Workflows](#validation-workflows)
- [Troubleshooting](#troubleshooting)

## Quick Start

### Installation
```bash
git clone https://github.com/ConnorBritain/segmint.git
cd segmint
pip install -e .
```

### First Message Generation
```bash
# Generate a basic RDE prescription message
segmint generate --type RDE --facility "DEMO_HOSPITAL"

# Generate a complete patient workflow
segmint workflow --type new_prescription --output ./messages/
```

### First Message Analysis
```bash
# Analyze vendor messages to create configuration
segmint analyze vendor_samples.hl7 --name "Epic Interface"
```

### GUI Quick Start
```bash
# Launch the graphical interface
python segmint.py

# Alternative launch methods
python -m app.gui.main_window
python app/gui/main_window.py
```

## Graphical User Interface (GUI)

The Segmint HL7 GUI provides an intuitive, healthcare-themed interface for interactive message generation, validation, and configuration management.

### Launching the GUI

#### Method 1: Direct Launch (Recommended)
```bash
cd hl7generator
python segmint.py
```

#### Method 2: Module Launch
```bash
python -m app.gui.main_window
```

#### Method 3: Direct Script
```bash
python app/gui/main_window.py
```

### GUI Interface Overview

The GUI is organized into five main panels accessible via the top navigation:

1. **🏠 Dashboard**: Overview and quick actions
2. **🎲 Message Generator**: Interactive message creation
3. **⚙️ Config Manager**: Interface configuration management
4. **✅ Validator**: Message validation and analysis
5. **📊 Analyzer**: Message analysis and schema inference

### Message Generator Panel

#### Basic Message Generation

1. **Select Message Type**
   - Choose from RDE, ADT, ORM, ACK message types
   - Each type shows relevant configuration options

2. **Configure Patient Data**
   - Select patient type: Adult, Pediatric, Geriatric, Infant
   - Click "🎲 Generate Random Patient" for synthetic patient data
   - View generated patient demographics in popup

3. **Configure Medications** (for RDE messages)
   - Select medication type: Oral, Injection, Topical, Inhalation
   - Click "💊 Generate Random Medication" for synthetic medication data
   - View generated medication details in popup

4. **Set Facility Information**
   - Enter your facility ID (e.g., "MAIN_HOSPITAL")
   - This appears in all message headers

5. **Batch Generation**
   - Set number of messages to generate (1-100)
   - Useful for creating test datasets

#### Quick Generation Buttons

- **⚡ Generate Messages**: Uses current form settings
- **🏥 Quick RDE**: Instantly generates prescription message
- **🚪 Quick ADT**: Instantly generates admission message

#### Generated Messages Management

- **Message List**: Shows all generated messages with timestamps
- **Preview Panel**: Displays selected message content in HL7 format
- **Action Buttons**:
  - **💾 Save All**: Export all messages to .hl7 file
  - **📋 Copy**: Copy selected message to clipboard
  - **✅ Validate**: Open selected message in validator
  - **🗑️ Clear**: Remove all generated messages

#### Example: Creating a Prescription Message

1. Launch GUI: `python segmint.py`
2. Navigate to "🎲 Message Generator" tab
3. Select "RDE" message type
4. Set facility ID: "DEMO_HOSPITAL"
5. Click "🎲 Generate Random Patient"
6. Review patient info in popup dialog
7. Select "ORAL" medication type
8. Click "💊 Generate Random Medication"
9. Review medication info in popup dialog
10. Click "⚡ Generate Messages"
11. View generated message in preview panel
12. Click "💾 Save All" to export

### Configuration Manager Panel

#### Managing Interface Configurations

1. **Create New Configuration**
   - Click "➕ New Config" button
   - Enter configuration name and description
   - Upload or paste HL7 sample messages
   - Click "Create" to analyze and save

2. **View Existing Configurations**
   - Browse list of saved configurations
   - View metadata: version, status, created date
   - Filter by status (Active, Locked, Draft)

3. **Configuration Actions**
   - **📋 View**: Display configuration details
   - **✏️ Edit**: Modify configuration (if unlocked)
   - **🔒 Lock/Unlock**: Prevent/allow modifications
   - **📊 Compare**: Diff against other configurations
   - **🗑️ Delete**: Remove configuration

#### Example: Creating Interface Configuration

1. Navigate to "⚙️ Config Manager" tab
2. Click "➕ New Config"
3. Enter name: "Epic Interface v2.1"
4. Enter description: "Epic EHR pharmacy interface"
5. Click "📁 Load Sample Messages"
6. Select .hl7 file with Epic messages
7. Review auto-detected message types
8. Click "🔍 Analyze Messages"
9. Review inferred configuration
10. Click "💾 Save Configuration"

### Validator Panel

#### Message Validation Workflow

1. **Load Message**
   - **📁 Load File**: Import .hl7 file
   - **📋 Paste Message**: Paste HL7 content directly
   - **🔗 From Generator**: Use message from generator panel

2. **Configure Validation**
   - **Validation Levels**: Select syntax, semantic, interface, clinical
   - **Strict Mode**: Enable for enhanced validation
   - **Configuration**: Choose interface config (optional)

3. **Run Validation**
   - Click "✅ Validate Message"
   - View real-time validation progress
   - Review detailed results

4. **Review Results**
   - **Summary**: Overall validation status
   - **Issues List**: Detailed findings by severity
   - **Suggestions**: Recommended fixes
   - **Export**: Save validation report

#### Validation Levels Explained

- **Syntax**: HL7 format compliance (delimiters, encoding)
- **Semantic**: Field requirements and data types
- **Interface**: Vendor-specific rules and constraints
- **Clinical**: Age-appropriate prescribing, safety checks

#### Example: Validating a Message

1. Navigate to "✅ Validator" tab
2. Click "📁 Load File" and select message file
3. Check "Syntax", "Semantic", and "Clinical" levels
4. Enable "Strict Mode" for comprehensive checking
5. Click "✅ Validate Message"
6. Review validation summary in results panel
7. Expand issue details for specific problems
8. Click "📄 Export Report" to save findings

### Analyzer Panel

#### Message Analysis Workflow

1. **Load Messages for Analysis**
   - **📁 Load Files**: Import multiple .hl7 files
   - **📁 Load Directory**: Analyze entire directory
   - Supports batch processing of vendor messages

2. **Configure Analysis**
   - **Interface Name**: Name for inferred specification
   - **Message Types**: Filter specific message types
   - **Analysis Depth**: Standard or comprehensive analysis

3. **Run Analysis**
   - Click "🔍 Analyze Messages"
   - Monitor analysis progress
   - Review statistical insights

4. **Review Inferred Configuration**
   - **Message Types**: Detected message patterns
   - **Segments**: Identified HL7 segments
   - **Custom Fields**: Vendor-specific extensions
   - **Confidence Score**: Analysis reliability

5. **Generate Outputs**
   - **📄 Documentation**: Human-readable specification
   - **⚙️ Configuration**: Machine-readable config
   - **📊 Field Mapping**: Data transformation guide

#### Example: Analyzing Vendor Messages

1. Navigate to "📊 Analyzer" tab
2. Click "📁 Load Directory"
3. Select folder containing Epic HL7 messages
4. Enter interface name: "Epic Pharmacy Interface"
5. Select message types: "RDE", "ACK"
6. Choose "Comprehensive" analysis depth
7. Click "🔍 Analyze Messages"
8. Wait for analysis completion
9. Review inferred configuration details
10. Click "📄 Generate Documentation"
11. Save generated specification documents

### Theme and Accessibility

#### Theme Selection
- **Light Theme**: Professional healthcare interface
- **Dark Theme**: Reduced eye strain for long sessions
- Switch themes via "🎨" button in top navigation

#### Accessibility Features
- High contrast color schemes
- Large, readable fonts
- Keyboard navigation support
- Screen reader compatible labels
- Tooltips for all interactive elements

### GUI Troubleshooting

#### GUI Won't Launch
```bash
# Check tkinter installation
python -c "import tkinter; print('tkinter available')"

# Install tkinter if missing (Ubuntu/Debian)
sudo apt-get install python3-tkinter

# Install tkinter (macOS with Homebrew)
brew install python-tk
```

#### Import Errors in GUI
```bash
# Ensure proper installation
pip install -e .

# Test imports
python -c "from app.gui.main_window import main; print('GUI imports working')"
```

#### GUI Performance Issues
- Close unused applications to free memory
- Reduce batch message generation size
- Use CLI for large-scale operations

#### Common GUI Workflow Issues

1. **"No module named 'app'" Error**
   - Run from project root directory
   - Ensure `pip install -e .` completed successfully

2. **Theme Not Loading**
   - Check theme files in `app/gui/themes/`
   - Reset to default theme in GUI settings

3. **Generated Messages Not Showing**
   - Check error messages in status bar
   - Verify facility ID is entered
   - Try with smaller batch sizes

## Command Line Interface (CLI)

### Message Generation

#### Basic Message Types
```bash
# Prescription message (RDE_O11)
segmint generate --type RDE --facility "ST_MARYS_HOSPITAL"

# Admission message (ADT_A01)
segmint generate --type ADT --event A01 --facility "GENERAL_HOSPITAL"

# Order message (ORM_O01)
segmint generate --type ORM --facility "CLINIC_NORTH"

# Acknowledgment message (ACK)
segmint generate --type ACK --original-control-id "MSG123456"
```

#### Advanced Generation Options
```bash
# Generate with specific patient demographics
segmint generate --type RDE \
  --patient-id "PAT123456" \
  --patient-name "Smith,John,A" \
  --patient-dob "19800315" \
  --facility "MAIN_HOSPITAL"

# Generate batch of messages
segmint generate --type RDE --count 10 --output ./test_messages/

# Generate with custom configuration
segmint generate --type RDE --config ./configs/epic_config.json
```

### Workflow Generation
```bash
# New prescription workflow
segmint workflow --type new_prescription \
  --patient-count 5 \
  --output ./workflows/prescriptions/

# Patient admission workflow
segmint workflow --type patient_admission \
  --include-orders \
  --include-vitals \
  --output ./workflows/admissions/

# Medication reconciliation workflow
segmint workflow --type medication_reconciliation \
  --patient-count 3
```

### Message Analysis
```bash
# Analyze single vendor file
segmint analyze epic_messages.hl7 --name "Epic Interface"

# Analyze multiple files with filtering
segmint analyze vendor_data/*.hl7 \
  --name "Cerner Interface" \
  --message-types RDE,ADT \
  --output ./analysis/

# Generate configuration from analysis
segmint analyze vendor_messages.hl7 \
  --name "AllScripts Interface" \
  --generate-config \
  --config-output ./configs/allscripts_config.json
```

### Message Validation
```bash
# Basic validation
segmint validate message.hl7

# Multi-level validation
segmint validate message.hl7 --levels syntax semantic interface clinical

# Batch validation
segmint validate ./messages/*.hl7 --report validation_report.json

# Validation with custom rules
segmint validate message.hl7 --config ./configs/strict_validation.json
```

### Configuration Management
```bash
# List configurations
segmint config list
segmint config list --status locked
segmint config list --tags production

# Create configuration
segmint config create "epic_prod" ./configs/epic.json \
  --user "admin@hospital.com" \
  --description "Epic production interface" \
  --tags production,epic

# Lock configuration for production
segmint config lock "epic_prod" \
  --user "admin@hospital.com" \
  --reason "Production deployment"

# View configuration history
segmint config history "epic_prod"

# Compare configurations
segmint config compare "epic_v1" "epic_v2" --detailed
```

## Python API Examples

### Basic Message Generation

```python
from app.core.messages import RDEMessage
from app.synthetic.data_generator import SyntheticDataGenerator

# Create data generator
generator = SyntheticDataGenerator()

# Generate synthetic patient
patient = generator.generate_patient()

# Create prescription message
message = RDEMessage()
message.create_standard_segments(
    facility_id="MAIN_HOSPITAL",
    patient=patient
)

# Add prescription details
medication = generator.generate_medication()
message.add_prescription(medication)

# Encode to HL7 format
hl7_text = message.encode()
print(hl7_text)
```

### Configuration Analysis

```python
from app.config_analyzer import MessageAnalyzer, ConfigurationGenerator

# Analyze vendor messages
analyzer = MessageAnalyzer()

# Load sample messages
with open('vendor_messages.hl7', 'r') as f:
    messages = f.read().split('\r')

# Analyze message patterns
analysis_results = analyzer.analyze_multiple_messages(messages)

# Generate configuration
config_generator = ConfigurationGenerator()
config = config_generator.generate_from_analysis(
    analysis_results,
    interface_name="Vendor Interface",
    version="1.0.0"
)

# Save configuration
with open('vendor_config.json', 'w') as f:
    json.dump(config, f, indent=2)
```

### Validation Workflow

```python
from app.validation.message_validators import ValidationEngine
from app.validation.base_validator import ValidationLevel

# Create validation engine
validator = ValidationEngine()

# Load message to validate
with open('message.hl7', 'r') as f:
    message_text = f.read()

# Perform multi-level validation
results = validator.validate_message(
    message_text,
    levels=[
        ValidationLevel.SYNTAX,
        ValidationLevel.SEMANTIC,
        ValidationLevel.INTERFACE,
        ValidationLevel.CLINICAL
    ]
)

# Process results
for result in results:
    if result.severity == "ERROR":
        print(f"ERROR: {result.message}")
    elif result.severity == "WARNING":
        print(f"WARNING: {result.message}")
```

### Configuration Management

```python
from app.config_library.config_manager import ConfigurationManager
from app.config_library.config_comparator import ConfigurationComparator

# Initialize configuration manager
manager = ConfigurationManager("./config_library")

# Create new configuration
metadata = manager.create_configuration(
    name="epic_interface",
    config_data=epic_config,
    user="john.doe@hospital.com",
    description="Epic EMR interface configuration",
    tags=["epic", "production"]
)

# Lock for production use
manager.lock_configuration(
    "epic_interface",
    user="admin@hospital.com",
    reason="Production deployment"
)

# Compare configurations for vendor changes
comparator = ConfigurationComparator()
old_config, _ = manager.get_configuration("epic_interface_v1")
new_config, _ = manager.get_configuration("epic_interface_v2")

comparison = comparator.compare_configurations(
    old_config=old_config,
    new_config=new_config,
    old_name="Epic v1.0",
    new_name="Epic v2.0"
)

# Review changes
for diff in comparison.differences:
    if diff.impact.value >= 3:  # MAJOR or CRITICAL
        print(f"IMPORTANT: {diff.description}")
```

## Real-World Scenarios

### Scenario 1: New Vendor Integration

```bash
# Step 1: Analyze vendor sample messages
segmint analyze epic_samples.hl7 --name "Epic Interface"

# Step 2: Generate initial configuration
segmint analyze epic_samples.hl7 \
  --name "Epic Interface" \
  --generate-config \
  --config-output ./configs/epic_initial.json

# Step 3: Create managed configuration
segmint config create "epic_interface" ./configs/epic_initial.json \
  --user "integration.team@hospital.com" \
  --description "Epic EMR interface - initial version"

# Step 4: Generate test messages for validation
segmint generate --type RDE --config ./configs/epic_initial.json --count 5

# Step 5: Validate against vendor specs
segmint validate ./test_messages/*.hl7 --config ./configs/epic_initial.json

# Step 6: Lock configuration for production
segmint config lock "epic_interface" \
  --user "admin@hospital.com" \
  --reason "Production deployment approved"
```

### Scenario 2: Vendor Update Detection

```bash
# Step 1: Analyze new vendor messages
segmint analyze epic_new_samples.hl7 --name "Epic Interface v2"

# Step 2: Generate updated configuration
segmint analyze epic_new_samples.hl7 \
  --generate-config \
  --config-output ./configs/epic_v2.json

# Step 3: Create new configuration version
segmint config create "epic_interface_v2" ./configs/epic_v2.json \
  --user "integration.team@hospital.com"

# Step 4: Compare configurations to detect changes
segmint config compare "epic_interface" "epic_interface_v2" --detailed

# Step 5: Generate change impact report
segmint config compare "epic_interface" "epic_interface_v2" \
  --report ./reports/epic_v2_changes.json
```

### Scenario 3: Testing Workflow

```python
# Complete testing workflow
from app.synthetic.message_templates import MessageTemplateLibrary
from app.validation.message_validators import ValidationEngine

# Generate test scenarios
template_lib = MessageTemplateLibrary()

# Generate new prescription workflow
workflow_messages = template_lib.generate_workflow(
    workflow_type="new_prescription",
    patient_count=10
)

# Validate all messages
validator = ValidationEngine()
validation_results = []

for message in workflow_messages:
    result = validator.validate_message(message.encode())
    validation_results.append(result)

# Generate test report
test_report = {
    "messages_generated": len(workflow_messages),
    "validation_errors": sum(1 for r in validation_results if r.has_errors),
    "validation_warnings": sum(1 for r in validation_results if r.has_warnings)
}

print(f"Test Results: {test_report}")
```

## Configuration Management

### Configuration File Structure

```json
{
  "interface_configuration": {
    "metadata": {
      "name": "Epic Interface",
      "version": "1.0.0",
      "description": "Epic EMR HL7 interface",
      "created_date": "2024-01-15T10:30:00Z"
    },
    "message_types": {
      "RDE_O11": {
        "description": "Pharmacy/Treatment Encoded Order",
        "segments": ["MSH", "PID", "ORC", "RXE"],
        "required_segments": ["MSH", "PID", "ORC", "RXE"],
        "segment_configurations": {
          "MSH": {
            "sending_application": "EPIC",
            "receiving_application": "PHARMACY",
            "message_type": "RDE^O11^RDE_O11"
          }
        }
      }
    },
    "field_mappings": {
      "PID.3": {
        "description": "Patient Identifier List",
        "type": "CX",
        "required": true,
        "validation_rules": ["patient_id_format"]
      }
    },
    "validation_rules": {
      "patient_id_format": {
        "type": "regex",
        "pattern": "^[A-Z0-9]{6,10}$"
      }
    }
  }
}
```

### Configuration Version Control

```bash
# Create configuration with versioning
segmint config create "interface_config" config.json \
  --version "1.0.0" \
  --user "dev.team@hospital.com"

# Update configuration (creates new version)
segmint config update "interface_config" updated_config.json \
  --user "dev.team@hospital.com" \
  --change-note "Added new message types"

# View version history
segmint config history "interface_config"

# Rollback to previous version
segmint config rollback "interface_config" --version "1.0.0"
```

## Validation Workflows

### Syntax Validation
```bash
# Check HL7 structure and encoding
segmint validate message.hl7 --levels syntax
```

### Semantic Validation
```bash
# Check field types and constraints
segmint validate message.hl7 --levels semantic
```

### Interface Validation
```bash
# Check against vendor specifications
segmint validate message.hl7 --levels interface --config vendor_config.json
```

### Clinical Validation
```bash
# Check clinical appropriateness
segmint validate message.hl7 --levels clinical
```

### Custom Validation Rules

```python
from app.validation.base_validator import BaseValidator, ValidationResult

class CustomValidator(BaseValidator):
    """Custom validation for specific business rules."""
    
    def validate(self, message):
        results = []
        
        # Custom validation logic
        if not self._check_patient_age_appropriateness(message):
            results.append(ValidationResult(
                level="ERROR",
                message="Medication not appropriate for patient age",
                field="RXE.2"
            ))
        
        return results
```

## Troubleshooting

### Common Issues

#### Missing Dependencies
```bash
# Install all dependencies
pip install -r requirements.txt

# Install with AI capabilities
pip install langchain langchain-openai
```

#### Configuration Issues
```bash
# Validate configuration file
segmint config validate config.json

# Check configuration syntax
python -m json.tool config.json
```

#### Message Validation Errors
```bash
# Get detailed validation report
segmint validate message.hl7 --verbose --levels all

# Check specific segments
segmint validate message.hl7 --segments MSH,PID
```

### Debug Mode
```bash
# Enable debug logging
export SEGMINT_DEBUG=1
segmint generate --type RDE

# Verbose output
segmint validate message.hl7 --verbose
```

### Performance Optimization
```bash
# Batch processing for large datasets
segmint generate --type RDE --count 1000 --batch-size 100

# Parallel validation
segmint validate ./messages/*.hl7 --parallel --workers 4
```

## Support and Resources

- **Documentation**: `docs/` directory
- **Examples**: `app/examples/` directory  
- **GitHub**: https://github.com/ConnorBritain/segmint
- **Issues**: Use GitHub Issues for bug reports
- **Contact**: connor.r.england@gmail.com

## Next Steps

1. Try the examples above with your own HL7 data
2. Explore the `app/examples/` directory for more code samples
3. Read the architecture documentation in `docs/ARCHITECTURE.md`
4. Run the comprehensive demo: `python demo.py`