# Pidgeon Smart Defaults

This directory contains smart default configurations that provide intelligent, context-aware defaults for healthcare message generation. These configurations eliminate the need for manual field-by-field setup for common healthcare scenarios.

## 🎯 Overview

Smart defaults provide:
- **Pre-configured scenarios** for common healthcare workflows
- **Vendor-specific patterns** for different EHR systems
- **Clinical context awareness** for realistic message generation
- **Time-based patterns** for temporal consistency
- **Intelligent randomization** weighted toward realistic clinical patterns

## 📁 Directory Structure

```
defaults/
├── README.md                     # This file
├── smart_defaults_index.json     # Master index of all configurations
├── scenarios/                    # Clinical and demographic scenarios
│   ├── pediatric_patient.json    # Pediatric patient demographics
│   ├── emergency_admission.json  # Emergency department workflows
│   ├── outpatient_lab_order.json # Laboratory order/result patterns
│   └── prescription_order.json   # E-prescribing workflows
├── vendors/                      # EHR vendor-specific patterns
│   ├── epic_defaults.json        # Epic Systems formatting
│   └── cerner_defaults.json      # Cerner/Oracle Health patterns
└── schemas/                      # JSON schemas for validation
    └── smart_defaults_schema.json # Configuration file schema
```

## 🚀 Usage Examples

### CLI Integration
```bash
# Generate with scenario defaults
pidgeon generate "ADT^A01" --scenario pediatric_patient

# Generate with vendor patterns
pidgeon generate "ORU^R01" --scenario outpatient_lab_order --vendor epic

# Generate with prescription workflow
pidgeon generate "RDE^O01" --scenario prescription_order
```

### Session Templates
```bash
# Load scenario as session template
pidgeon session load --template emergency_admission

# Apply scenario to current session
pidgeon session apply --scenario pediatric_patient

# Generate using session values
pidgeon generate "ADT^A01"
```

## 📋 Available Configurations

### Patient Demographics
- **`pediatric_patient`**: Optimized for patients 0-17 years old
  - Age-appropriate defaults and exclusions
  - Emergency contact patterns (parents/guardians)
  - Preventive care weighting

### Clinical Workflows
- **`emergency_admission`**: Emergency department patterns
  - Urgent priority indicators
  - ED-specific location codes
  - Trauma and acute care patterns

- **`outpatient_lab_order`**: Laboratory workflows
  - Common lab panels (CBC, BMP, Lipid)
  - Realistic reference ranges
  - Normal vs. abnormal result weighting

- **`prescription_order`**: E-prescribing patterns
  - Common medications by therapeutic class
  - Realistic dosing and frequency patterns
  - Compliance with prescribing regulations

### Vendor Patterns
- **`epic_defaults`**: Epic Systems optimization
  - Epic-specific ID formats
  - Comprehensive segment patterns
  - Epic naming conventions

- **`cerner_defaults`**: Cerner/Oracle Health optimization
  - Variable-length numeric IDs
  - Multum drug coding
  - Cerner-specific workflows

## 🏗️ Configuration Structure

Each smart defaults file follows this structure:

```json
{
  "name": "Human-readable name",
  "description": "Detailed description",
  "category": "patient_demographics | clinical_workflow | vendor_patterns",
  "applicable_standards": ["hl7v23", "hl7v24", "hl7v25"],
  "field_defaults": {
    "semantic.path": "default_value",
    "PID.7": "20160315"
  },
  "conditional_defaults": {
    "if_pediatric": {
      "condition": "patient.age_years < 18",
      "fields": {
        "patient.marital_status": "S"
      }
    }
  },
  "smart_randomization": {
    "weight_normal_results": 0.7,
    "generate_vital_signs": true
  }
}
```

## 🔧 Integration with Field Value Resolvers

Smart defaults integrate with Pidgeon's field value resolver chain at **Priority 85** (between semantic paths and demographics):

1. **Priority 100**: User-set semantic path values (session)
2. **Priority 90**: HL7-specific field values
3. **Priority 85**: **Smart defaults** ← *Applied here*
4. **Priority 80**: Demographic table values
5. **Priority 10**: Smart random fallback

## ✨ Creating Custom Configurations

1. **Follow the schema**: Use `schemas/smart_defaults_schema.json` for validation
2. **Choose appropriate category**: `patient_demographics`, `clinical_workflow`, or `vendor_patterns`
3. **Use semantic paths**: Prefer `patient.mrn` over `PID.3` for maintainability
4. **Include usage notes**: Document the intended use cases
5. **Test thoroughly**: Verify with actual message generation

### Example Custom Configuration
```json
{
  "name": "Cardiology Outpatient",
  "description": "Smart defaults for cardiology clinic visits",
  "category": "clinical_workflow",
  "applicable_standards": ["hl7v23", "hl7v24"],
  "field_defaults": {
    "encounter.department": "Cardiology",
    "encounter.specialty": "CV",
    "provider.specialty": "Cardiologist"
  },
  "smart_randomization": {
    "weight_cardiac_conditions": 0.9,
    "weight_cardiac_medications": 0.8
  }
}
```

## 🎭 Advanced Features

### Conditional Logic
Set field values based on logical conditions:
```json
"conditional_defaults": {
  "if_emergency": {
    "condition": "encounter.class == 'E'",
    "fields": {
      "encounter.priority": "UR",
      "encounter.acuity": "2"
    }
  }
}
```

### Time Patterns
Use relative time patterns for temporal consistency:
```json
"time_patterns": {
  "encounter.admission_time": "current_time",
  "specimen.collection_time": "current_time_minus_1_hour",
  "result.verified_time": "current_time_plus_15_minutes"
}
```

### Field Patterns
Define formatting patterns for consistent value generation:
```json
"field_patterns": {
  "patient_id": {
    "pattern": "\\d{10}",
    "example": "1234567890",
    "description": "10-digit numeric patient ID"
  }
}
```

## 🔮 Future Enhancements

- **AI-Enhanced Generation**: Machine learning from real message patterns
- **Specialty Workflows**: Oncology, pediatrics, mental health configurations
- **Dynamic Adaptation**: Smart defaults that learn from usage patterns
- **FHIR Integration**: Smart defaults for FHIR R4/R5 resources
- **Regulatory Compliance**: Built-in compliance patterns for different regions

## 📚 Resources

- **Schema Documentation**: `schemas/smart_defaults_schema.json`
- **Master Index**: `smart_defaults_index.json`
- **CLI Documentation**: See main Pidgeon CLI help
- **Field Path Reference**: Pidgeon semantic path documentation

---

💡 **Tip**: Start with existing scenarios and customize them for your specific use cases rather than building from scratch.