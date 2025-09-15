# Pidgeon CLI Configuration Reference

**Version**: 1.0  
**Updated**: September 14, 2025  
**Scope**: Complete configuration hierarchy and standard-specific settings  
**Status**: Strategic design for clean, logical configuration management

---

## 🎯 **Configuration Philosophy**

Pidgeon uses a **layered configuration system** that prevents nonsensical combinations while maximizing usability:

1. **Global settings** apply across all standards where logical
2. **Standard-specific settings** only apply to HL7, FHIR, or NCPDP
3. **Message-type specific settings** provide fine-grained control
4. **Smart validation** prevents impossible combinations (e.g., HL7 with JSON format)

---

## 🏗️ **Configuration Hierarchy**

### **Layer 1: Global Settings (All Standards)**
Settings that make sense across HL7, FHIR, and NCPDP:

```bash
# Data generation
pidgeon config set --global --default-count 10
pidgeon config set --global --seed 12345

# Vendor patterns (applies to all standards)
pidgeon config set --global --vendor epic
pidgeon config set --global --vendor cerner
pidgeon config set --global --vendor meditech

# Output preferences
pidgeon config set --global --output-dir ./output
pidgeon config set --global --verbose true

# Generation modes (Pro/Enterprise features)
pidgeon config set --global --mode procedural     # Free tier default
pidgeon config set --global --mode local-ai       # Pro: Use local models
pidgeon config set --global --mode api-ai         # Pro/Enterprise: Cloud AI
```

### **Layer 2: Standard-Specific Settings**

#### **HL7 v2.x Configuration**
```bash
# Format options (only pipe-delimited makes sense)
pidgeon config set --standard hl7 --default-format hl7    # Only valid option
pidgeon config set --standard hl7 --version v2.3          # Default: v2.3
pidgeon config set --standard hl7 --version v2.4          # Alternative

# HL7-specific generation
pidgeon config set --standard hl7 --default-encoding ^~\&| # Rarely changed
pidgeon config set --standard hl7 --facility "PIDGEON_HEALTH"
pidgeon config set --standard hl7 --application "PIDGEON_CLI"

# Validation modes
pidgeon config set --standard hl7 --validation-mode strict       # Exact spec compliance
pidgeon config set --standard hl7 --validation-mode compatibility # Real-world patterns
```

#### **FHIR R4 Configuration**
```bash
# Format options (only JSON-based makes sense)
pidgeon config set --standard fhir --default-format json    # Default
pidgeon config set --standard fhir --default-format ndjson  # For bulk operations

# FHIR-specific generation
pidgeon config set --standard fhir --base-url "http://hospital.example.org"
pidgeon config set --standard fhir --bundle-type collection  # Default for scenarios
pidgeon config set --standard fhir --bundle-type searchset   # For search results

# FHIR search defaults
pidgeon config set --standard fhir --search-page-size 20
pidgeon config set --standard fhir --include-references true

# Implementation Guides (Pro/Enterprise)
pidgeon config set --standard fhir --ig us-core-4.0.0      # US Core compliance
pidgeon config set --standard fhir --ig carin-bb           # CARIN Blue Button
```

#### **NCPDP SCRIPT Configuration**
```bash
# Modern SCRIPT versions (SureScripts ecosystem)
pidgeon config set --standard ncpdp --version 2023011      # Latest (CMS mandated)
pidgeon config set --standard ncpdp --version 2017071      # Current standard
pidgeon config set --standard ncpdp --version 10.6         # Legacy (deprecated)

# Format options (XML-based for modern SCRIPT)
pidgeon config set --standard ncpdp --default-format xml   # Modern SCRIPT
pidgeon config set --standard ncpdp --default-format text  # Legacy formats

# SureScripts integration
pidgeon config set --standard ncpdp --network surescripts
pidgeon config set --standard ncpdp --environment sandbox  # vs production
pidgeon config set --standard ncpdp --routing-type cpoe    # CPOE systems
pidgeon config set --standard ncpdp --routing-type pharmacy # Pharmacy systems
```

### **Layer 3: Message-Type Specific Settings**

#### **HL7 Message Types**
```bash
# ADT messages
pidgeon config set --standard hl7 --type ADT --default-trigger A01   # Admit
pidgeon config set --standard hl7 --type ADT --default-trigger A08   # Update

# ORU messages (lab results)
pidgeon config set --standard hl7 --type ORU --default-trigger R01   # Unsolicited
pidgeon config set --standard hl7 --type ORU --observation-count 3   # Default labs per message

# RDE messages (pharmacy orders)  
pidgeon config set --standard hl7 --type RDE --default-trigger O11   # Pharmacy order
pidgeon config set --standard hl7 --type RDE --include-allergies true
```

#### **FHIR Resource Types**
```bash
# Patient resources
pidgeon config set --standard fhir --type Patient --include-defaults "general-practitioner,organization"
pidgeon config set --standard fhir --type Patient --identifier-system "http://hospital.example.org"

# Observation resources
pidgeon config set --standard fhir --type Observation --category vital-signs  # vs survey, exam, etc.
pidgeon config set --standard fhir --type Observation --default-status final

# Bundle resources
pidgeon config set --standard fhir --type Bundle --max-entries 50    # Performance limit
pidgeon config set --standard fhir --type Bundle --include-meta true # Add Bundle metadata
```

#### **NCPDP Transaction Types**
```bash
# Modern SCRIPT transactions (v2017071, v2023011)
pidgeon config set --standard ncpdp --type NewRx --include-prescriber-address true
pidgeon config set --standard ncpdp --type NewRx --include-patient-allergies true
pidgeon config set --standard ncpdp --type NewRx --compound-capable true  # Multi-ingredient support

# SureScripts-specific
pidgeon config set --standard ncpdp --type RxFill --include-pbm-info true
pidgeon config set --standard ncpdp --type CancelRx --reason-required true
```

---

## 🚫 **Smart Validation Rules**

Pidgeon prevents nonsensical configurations with helpful error messages:

### **Format Validation**
```bash
# ❌ This errors with helpful guidance
pidgeon config set --standard hl7 --default-format json
# Error: HL7 messages are pipe-delimited, not JSON. 
# Suggestion: Did you mean --standard fhir?

# ❌ This errors 
pidgeon config set --standard fhir --default-format hl7
# Error: FHIR resources are JSON-based, not pipe-delimited.
# Suggestion: Did you mean --standard hl7?

# ❌ This errors
pidgeon config set --standard ncpdp --default-format ndjson  
# Error: NCPDP SCRIPT uses XML format, not NDJSON.
# Suggestion: Use --default-format xml
```

### **Version Compatibility**
```bash
# ❌ This warns about deprecation
pidgeon config set --standard ncpdp --version 10.6
# Warning: NCPDP SCRIPT v10.6 was sunset by CMS on December 31, 2019.
# Recommendation: Use --version 2017071 or --version 2023011 for modern compliance.

# ❌ This errors on impossible combinations
pidgeon config set --standard hl7 --version v3.0
# Error: HL7 v3.0 is not supported. 
# Supported versions: v2.3, v2.4, v2.5
```

### **Standard-Specific Features**
```bash
# ❌ This errors - bundles are FHIR-specific
pidgeon config set --standard hl7 --bundle-type collection
# Error: Bundles are a FHIR concept, not applicable to HL7 v2.x messages.
# Suggestion: Use --standard fhir for Bundle configuration.

# ❌ This errors - _include is FHIR search specific  
pidgeon config set --standard ncpdp --include-defaults "patient"
# Error: _include parameters are FHIR search-specific.
# Suggestion: Use --standard fhir for search configuration.
```

---

## 🏥 **Clinical Scenarios Configuration**

Clinical scenarios can span multiple standards, with intelligent defaults:

### **Multi-Standard Scenario Support**
```bash
# Default to FHIR (most natural for bundled scenarios)
pidgeon config set --scenario admission-with-labs --default-standard fhir

# HL7 mode: Sequential messages representing the same workflow
pidgeon config set --scenario admission-with-labs --hl7-sequence "ADT^A01,ORU^R01,ORU^R01"

# NCPDP mode: Prescription-focused scenarios only
pidgeon config set --scenario diabetes-management --ncpdp-sequence "NewRx,RxFill,DrugHistory"
```

### **Scenario-Specific Settings**
```bash
# Admission scenarios
pidgeon config set --scenario admission-with-labs --observation-types "CBC,BMP,Lipid"
pidgeon config set --scenario admission-with-labs --encounter-type inpatient

# Diabetes management
pidgeon config set --scenario diabetes-management --include-a1c true
pidgeon config set --scenario diabetes-management --medication-classes "metformin,insulin"

# Emergency visits
pidgeon config set --scenario emergency-visit --acuity-levels "3,4,5"  # ESI levels
pidgeon config set --scenario emergency-visit --observation-count 8     # Typical ER workup
```

---

## 📂 **Configuration Storage & Profiles**

### **Configuration File Locations**
```bash
# Global configuration
~/.pidgeon/config.json                    # User-wide settings
~/.pidgeon/profiles/                      # Named profiles

# Project-specific configuration  
./pidgeon.json                           # Project settings (override global)
./profiles/                              # Project profiles
```

### **Profile Management**
```bash
# Create named profiles for different contexts
pidgeon config profile create epic-dev
pidgeon config profile create cerner-prod
pidgeon config profile create testing

# Set profile-specific configurations
pidgeon config set --profile epic-dev --vendor epic --standard hl7 --version v2.3
pidgeon config set --profile cerner-prod --vendor cerner --standard fhir

# Use profiles
pidgeon generate Patient --profile epic-dev    # Use epic-dev profile for this command
pidgeon config use --profile cerner-prod       # Set as active profile for session
```

### **Configuration Inheritance**
```
Command-line options
    ↓ overrides
Profile settings (if specified)
    ↓ overrides  
Project configuration (./pidgeon.json)
    ↓ overrides
Global configuration (~/.pidgeon/config.json)
    ↓ overrides
Built-in defaults
```

---

## 🔧 **Configuration Commands**

### **Setting Configuration**
```bash
# Set global configuration
pidgeon config set --global --default-count 10

# Set standard-specific configuration
pidgeon config set --standard fhir --default-format json

# Set message-type specific configuration
pidgeon config set --standard hl7 --type ADT --default-trigger A01

# Set profile-specific configuration
pidgeon config set --profile epic-dev --vendor epic
```

### **Viewing Configuration**
```bash
# Show all configuration
pidgeon config show

# Show specific standard configuration
pidgeon config show --standard fhir

# Show specific profile
pidgeon config show --profile epic-dev

# Show effective configuration (with inheritance)
pidgeon config show --effective --profile epic-dev
```

### **Managing Profiles**
```bash
# List profiles
pidgeon config profile list

# Create new profile
pidgeon config profile create my-profile

# Copy existing profile
pidgeon config profile copy epic-dev epic-staging

# Delete profile
pidgeon config profile delete old-profile

# Set active profile
pidgeon config use --profile epic-dev
```

### **Validation & Testing**
```bash
# Validate configuration
pidgeon config validate

# Test configuration with sample generation
pidgeon config test --standard fhir --type Patient

# Show what a command would do with current config
pidgeon generate Patient --dry-run
```

---

## 🏷️ **NCPDP SCRIPT Version Reference**

### **Modern SCRIPT Landscape (2025)**

#### **SCRIPT v2023011 (Latest - CMS Mandated)**
- **Status**: Current CMS requirement for Medicare Part D
- **SureScripts**: Full support and industry migration leadership
- **Features**: Enhanced patient safety, workflow efficiency improvements
- **Use Cases**: Modern CPOE systems, SureScripts network integration

#### **SCRIPT v2017071 (Current Standard)**  
- **Status**: Widely deployed, being superseded by v2023011
- **Migration**: Industry transition ongoing through 2024-2025
- **Features**: Improved prescription accuracy, compounded prescriptions (up to 25 ingredients)
- **Enhanced Data**: Patient allergies, international addresses, preferred language

#### **SCRIPT v10.6 (Legacy - Deprecated)**
- **Status**: Sunset by CMS December 31, 2019
- **Usage**: Legacy system support only
- **Migration**: All production systems should use v2017071 or v2023011

### **SureScripts Integration Considerations**
```bash
# Modern SureScripts configuration
pidgeon config set --standard ncpdp --version 2023011
pidgeon config set --standard ncpdp --network surescripts
pidgeon config set --standard ncpdp --environment sandbox

# CPOE system integration
pidgeon config set --standard ncpdp --routing-type cpoe
pidgeon config set --standard ncpdp --prescriber-validation true

# Pharmacy system integration
pidgeon config set --standard ncpdp --routing-type pharmacy
pidgeon config set --standard ncpdp --formulary-checking true
```

---

## 💡 **Best Practices**

### **Development Workflow**
1. **Start with global defaults** for your most common use cases
2. **Create profiles for different environments** (dev, staging, prod)
3. **Use standard-specific settings** to optimize for your primary standards
4. **Override at command level** for one-off testing

### **Team Collaboration**
1. **Commit project configuration** (`./pidgeon.json`) to version control
2. **Document team profiles** in your project README
3. **Use consistent vendor patterns** across team members
4. **Validate configurations** in CI/CD pipelines

### **Testing Strategy**
1. **Separate profiles for testing** vs production-like data
2. **Use deterministic seeds** for reproducible test data
3. **Configure appropriate validation modes** for your testing phase
4. **Test cross-standard scenarios** with multi-standard workflows

---

**Philosophy**: Configuration should be **discoverable**, **validating**, and **context-aware**. Every setting should have a clear purpose and prevent invalid combinations through helpful validation messages.