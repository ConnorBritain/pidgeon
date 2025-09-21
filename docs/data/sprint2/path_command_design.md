# Pidgeon Path Command Design Specification

**Version**: 1.0
**Created**: January 20, 2025
**Sprint**: Sprint 2 - Priority 2
**Status**: Design specification for implementation

---

## 🎯 **Executive Summary**

The `pidgeon path` command will be Pidgeon's **semantic path discovery and exploration tool**, enabling users to understand, discover, and validate cross-standard field paths without memorizing specific segment positions or JSON structures.

**Key Value**: Transforms Pidgeon's cross-standard abstraction from "magic that just works" into "discoverable, learnable, and transparent tooling" that increases user confidence and adoption.

---

## 🧬 **Semantic Paths Primer**

### **What Are Semantic Paths?**
Semantic paths are Pidgeon's **human-friendly, standard-agnostic field references** that abstract away the complexity of healthcare standards:

```bash
# Instead of memorizing:
HL7:   PID.3.1                                    # Medical Record Number
FHIR:  Patient.identifier[?(@.type.coding[0].code=='MR')].value
NCPDP: Not applicable

# Users can simply use:
patient.mrn                                        # Works across all standards
```

### **Current State: Hidden Magic**
Semantic paths work transparently in existing commands but aren't discoverable:
```bash
# These work today but users must "know" the paths exist:
pidgeon set my-session patient.mrn "MR123456"
pidgeon set my-session encounter.location "ER-1"
pidgeon set my-session provider.name "Dr. Smith"
```

### **Desired State: Discoverable Intelligence**
The `path` command makes semantic paths discoverable and educational:
```bash
# Discover what's possible:
pidgeon path list "ADT^A01"                        # Show all available paths
pidgeon path search "phone"                        # Find phone-related paths
pidgeon path resolve patient.mrn "ADT^A01"         # See how it maps to HL7/FHIR
```

---

## 📋 **Command Structure & Design**

### **Core Command Syntax**
Following Pidgeon's CLI philosophy of **natural language with smart defaults**:

```bash
pidgeon path <subcommand> [options]
```

### **Subcommands Overview**

| Subcommand | Purpose | Free/Pro | Primary User Types |
|------------|---------|----------|-------------------|
| `list` | Discover available paths for message type | Free | All users |
| `resolve` | Show how semantic path maps to standards | Free | All users |
| `validate` | Check if path is valid for message type | Free | All users |
| `search` | Find paths by description/keyword | Free | All users |
| `compare` | Cross-standard path comparison | Free | Developers, Consultants |

---

## 🔍 **Detailed Subcommand Specifications**

### **pidgeon path list**
**Purpose**: Discover all available semantic paths for a message type

```bash
USAGE
  pidgeon path list [message-type] [options]

POSITIONAL ARGUMENTS
  message-type                 HL7 message (ADT^A01), FHIR resource (Patient), or NCPDP type (NewRx)
                              If omitted, shows universal paths available across all standards

OPTIONS
  -s, --standard <name>        Show paths for specific standard (hl7v23|fhirv4|ncpdp)
  -c, --category <type>        Filter by category: patient|encounter|provider|medication|all (default: all)
  -f, --format <fmt>           Output format: table|json|csv (default: table)
      --descriptions           Include field descriptions (verbose)
      --examples               Show example values for each path
  -o, --output <path>          Write results to file (default: stdout)

SMART DEFAULTS
  • Auto-detects standard from message type (ADT^A01 → HL7, Patient → FHIR)
  • Uses user's configured default standard when message type is ambiguous
  • Groups results by logical categories (patient, encounter, etc.)

EXAMPLES
  pidgeon path list                                 # Universal paths across all standards
  pidgeon path list "ADT^A01"                       # All paths available for ADT messages
  pidgeon path list Patient                         # All paths for FHIR Patient resource
  pidgeon path list "ADT^A01" --category patient    # Only patient-related paths
  pidgeon path list --standard fhirv4               # All FHIR paths regardless of message type
  pidgeon path list "ORU^R01" --descriptions --examples  # Verbose output with examples
```

**Sample Output (Table Format)**:
```
📋 Available Semantic Paths: ADT^A01 (HL7v23)

PATIENT DEMOGRAPHICS
  patient.mrn              Medical Record Number
  patient.lastName         Patient Last Name
  patient.firstName        Patient First Name
  patient.dateOfBirth      Date of Birth
  patient.sex              Administrative Sex
  patient.phoneNumber      Primary Phone Number
  patient.address          Complete Address
  patient.address.street   Street Address (component)
  patient.address.city     City (component)
  patient.address.state    State/Province (component)
  patient.address.zip      ZIP/Postal Code (component)

ENCOUNTER INFORMATION
  encounter.patientClass   Patient Class (I|O|E|R)
  encounter.location       Assigned Patient Location
  encounter.attendingProvider  Attending Doctor
  encounter.admissionDate  Admit Date/Time
  encounter.dischargeDate  Discharge Date/Time

MESSAGE CONTROL
  message.sendingApplication    Sending Application
  message.sendingFacility      Sending Facility
  message.messageType          Message Type
  message.timestamp            Message Date/Time

💡 Use 'pidgeon path resolve <path> "ADT^A01"' to see standard-specific mappings
💡 Use 'pidgeon path search <keyword>' to find paths by description
```

---

### **pidgeon path resolve**
**Purpose**: Show how a semantic path maps to standard-specific field locations

```bash
USAGE
  pidgeon path resolve <semantic-path> <message-type> [options]

POSITIONAL ARGUMENTS
  semantic-path            Semantic path to resolve (e.g., patient.mrn, encounter.location)
  message-type            Target message type (ADT^A01, Patient, NewRx)

OPTIONS
  -s, --standard <name>    Show mapping for specific standard only
      --all-standards      Show mappings across all supported standards
  -f, --format <fmt>       Output format: table|json (default: table)
      --detailed           Include field type, validation rules, and examples
      --path-only          Output only the resolved path (useful for scripting)

EXAMPLES
  pidgeon path resolve patient.mrn "ADT^A01"                    # Default: user's configured standard
  pidgeon path resolve patient.mrn "ADT^A01" --all-standards   # Cross-standard comparison
  pidgeon path resolve encounter.location Patient              # Cross-standard concept (may not apply)
  pidgeon path resolve patient.phoneNumber "ADT^A01" --detailed # Verbose with validation info
  pidgeon path resolve patient.mrn "ADT^A01" --path-only       # Script-friendly output: "PID.3"
```

**Sample Output (Cross-Standard)**:
```
🔍 Path Resolution: patient.mrn → Medical Record Number

MESSAGE TYPE: ADT^A01

HL7v23:    PID.3
           │ Field Type: CX (Extended Composite ID)
           │ Description: Patient Identifier List
           │ Components: ID^CheckDigit^CheckDigitScheme^AssigningAuthority
           │ Example: "MR123456^^^EPIC"

FHIRv4:    Patient.identifier[?(@.type.coding[0].code=='MR')].value
           │ Field Type: string
           │ Description: Medical Record Number identifier
           │ Path Notes: Filters for identifiers with type='MR'
           │ Example: "MR123456"

NCPDPv2017071: Not applicable to pharmacy transactions

✅ Validation: patient.mrn is valid for ADT^A01 messages
💡 Use 'pidgeon set my-session patient.mrn "MR123456"' to lock this value
```

---

### **pidgeon path validate**
**Purpose**: Check if a semantic path is valid for a given message type

```bash
USAGE
  pidgeon path validate <semantic-path> <message-type> [options]

POSITIONAL ARGUMENTS
  semantic-path           Path to validate (e.g., medication.dosage)
  message-type           Target message type (ADT^A01, Patient, etc.)

OPTIONS
  -s, --standard <name>   Validate against specific standard
      --suggestions       Show related/alternative paths when invalid
  -f, --format <fmt>      Output format: text|json (default: text)

EXAMPLES
  pidgeon path validate patient.mrn "ADT^A01"           # ✅ Valid
  pidgeon path validate medication.dosage "ADT^A01"    # ❌ Invalid with suggestions
  pidgeon path validate encounter.location Patient     # ✅ Valid (FHIR Encounter reference)
```

**Sample Output (Invalid Path)**:
```
❌ Invalid Path: medication.dosage for ADT^A01

REASON: Medication paths are not applicable to admission/discharge/transfer messages

💡 SUGGESTIONS:
  For ADT^A01 messages, consider:
  • patient.mrn              (Medical Record Number)
  • encounter.location       (Patient Location)
  • encounter.patientClass   (Inpatient/Outpatient)
  • provider.attending       (Attending Physician)

  For medication information, use:
  • RDE^O11 messages (Pharmacy/Treatment Encoded Order)
  • FHIR MedicationRequest resources
  • NCPDP NewRx transactions

🔍 Use 'pidgeon path list "RDE^O11"' to see medication-related paths
```

---

### **pidgeon path search**
**Purpose**: Find semantic paths by keyword or description

```bash
USAGE
  pidgeon path search <query> [options]

POSITIONAL ARGUMENTS
  query                   Search term (e.g., "phone", "date", "medical record")

OPTIONS
  -m, --message-type <type>  Limit search to specific message type
  -s, --standard <name>      Limit search to specific standard
  -c, --category <type>      Limit search to category (patient|encounter|provider|medication)
      --exact                Exact match only (no fuzzy search)
  -f, --format <fmt>         Output format: table|json (default: table)

EXAMPLES
  pidgeon path search "phone"                              # Find all phone-related paths
  pidgeon path search "medical record"                     # Find MRN-related paths
  pidgeon path search "date" --message-type "ADT^A01"     # Date paths for ADT only
  pidgeon path search "provider" --category encounter     # Provider paths in encounter context
```

**Sample Output**:
```
🔍 Search Results: "phone" (5 matches found)

PATIENT CONTACT
  patient.phoneNumber      Primary Phone Number (Home)
  patient.phoneWork        Work Phone Number
  patient.phoneMobile      Mobile Phone Number

PROVIDER CONTACT
  provider.phoneNumber     Provider Phone Number
  provider.phoneOffice     Provider Office Phone

💡 Use 'pidgeon path resolve <path> <message-type>' for standard-specific mappings
💡 Use 'pidgeon path list' to see all available paths
```

---

### **pidgeon path compare**
**Purpose**: Compare how the same concept maps across different standards

```bash
USAGE
  pidgeon path compare <semantic-path> [options]

POSITIONAL ARGUMENTS
  semantic-path           Path to compare across standards

OPTIONS
      --standards <list>  Comma-separated standards to compare (default: all)
      --message-types <list>  Compare across multiple message types
  -f, --format <fmt>     Output format: table|json (default: table)

EXAMPLES
  pidgeon path compare patient.mrn                                    # MRN across all standards
  pidgeon path compare patient.address --standards hl7v23,fhirv4     # Address in HL7 vs FHIR
  pidgeon path compare encounter.location --message-types "ADT^A01,Patient,Encounter"
```

---

## 👥 **User Type Value Propositions**

### **🩺 Healthcare Developers** (Primary Users)
**Pain Points Addressed**:
- "I know HL7 but I'm new to FHIR - what's the equivalent of PID.3?"
- "What fields can I actually set for an ADT message?"
- "I want to lock patient phone numbers but don't know the path syntax"

**Value Delivered**:
```bash
# Discovery: What can I do with this message type?
pidgeon path list "ADT^A01" --category patient

# Learning: How does this translate to different standards?
pidgeon path resolve patient.phoneNumber "ADT^A01" --all-standards

# Validation: Can I use this path for my use case?
pidgeon path validate patient.medications "ADT^A01"  # ❌ with suggestions
```

### **🔧 Integration Consultants** (Secondary Users)
**Pain Points Addressed**:
- "Client uses Epic HL7 but needs FHIR output - show me the mapping"
- "What fields are available for this vendor's ADT implementation?"
- "I need to document field mappings for the integration specification"

**Value Delivered**:
```bash
# Cross-standard consulting
pidgeon path compare patient.mrn --format json > mrn_mapping.json

# Vendor-specific discovery
pidgeon path list "ADT^A01" --descriptions --examples --output epic_adt_fields.csv

# Documentation generation
pidgeon path resolve patient.address "ADT^A01" --detailed --format json
```

### **📊 Healthcare Informaticists** (Secondary Users)
**Pain Points Addressed**:
- "I need to validate our interface sends all required patient demographics"
- "What's the standardized way to reference encounter location across systems?"
- "Help me understand what data elements are available for analysis"

**Value Delivered**:
```bash
# Requirements validation
pidgeon path list "ADT^A01" --category patient > patient_requirements.txt

# Standardization analysis
pidgeon path search "location" --format json

# Cross-system compatibility
pidgeon path compare encounter.attendingProvider --standards hl7v23,fhirv4
```

### **🏥 Healthcare Administrators** (Tertiary Users)
**Pain Points Addressed**:
- "What data elements do we need to capture for meaningful use?"
- "Can our current system support these new reporting requirements?"
- "I need to understand what fields are available for quality metrics"

**Value Delivered**:
```bash
# Compliance field discovery
pidgeon path search "date" --category encounter

# Capability assessment
pidgeon path list Patient --standard fhirv4 --category patient

# Reporting field identification
pidgeon path search "provider" --descriptions
```

---

## 🚀 **Implementation Strategy**

### **Phase 1: Core Infrastructure** (Week 1)
- [ ] Create `PathCommand` class following existing CLI patterns
- [ ] Implement `list` subcommand with basic table output
- [ ] Wire up to existing `IFieldPathResolver` infrastructure
- [ ] Add to service registration and CLI help

### **Phase 2: Discovery Features** (Week 2)
- [ ] Implement `resolve` subcommand with cross-standard output
- [ ] Add `search` functionality with fuzzy matching
- [ ] Implement filtering (category, standard, message type)
- [ ] Add output format options (table, JSON, CSV)

### **Phase 3: Advanced Features** (Week 3)
- [ ] Implement `validate` subcommand with suggestions
- [ ] Add `compare` subcommand for cross-standard analysis
- [ ] Include detailed field information (types, examples, validation rules)
- [ ] Add script-friendly output modes (--path-only)

### **Phase 4: Polish & Integration** (Week 4)
- [ ] Add comprehensive help and examples
- [ ] Integrate with smart defaults from configuration
- [ ] Add shell completion for semantic paths
- [ ] Update CLI documentation and help text

---

## 🎯 **Success Metrics**

### **Adoption Indicators**
- **Discovery Usage**: `pidgeon path list` becomes top 5 most-used command
- **Learning Velocity**: New users complete first successful `pidgeon set` within 5 minutes
- **Cross-Standard Confidence**: Users successfully use semantic paths across HL7/FHIR without documentation

### **Business Impact**
- **CLI Stickiness**: Increased daily active usage of CLI commands
- **Template Creation**: More users create and share session templates (marketplace growth)
- **Support Reduction**: Decreased questions about "what fields can I set?"

### **Technical Quality**
- **Response Speed**: All path operations complete in <100ms
- **Coverage**: 90%+ of commonly-used healthcare fields have semantic path mappings
- **Accuracy**: Path resolutions are 100% accurate across supported standards

---

## 🎨 **CLI Design Philosophy Alignment**

### **Natural Language Approach**
```bash
# Reads like natural language
pidgeon path list "ADT^A01"                 # "show me paths for ADT messages"
pidgeon path search "phone"                 # "find phone-related paths"
pidgeon path resolve patient.mrn "ADT^A01"  # "how does patient MRN resolve for ADT?"
```

### **Smart Defaults & Inference**
- Auto-detect standard from message type (ADT^A01 → HL7, Patient → FHIR)
- Use user's configured standard preferences when ambiguous
- Intelligent output formatting based on terminal width and capabilities
- Smart categorization and grouping of results

### **Balanced Verbosity**
- **Terse Mode**: `--path-only` for scripting integration
- **Standard Mode**: Clean table output with essential information
- **Verbose Mode**: `--descriptions --examples` for learning and documentation
- **Machine Mode**: `--format json` for automation and integration

### **Discoverability**
- Comprehensive help text with real-world examples
- Suggestion system when paths are invalid or not found
- Cross-references to related commands (`pidgeon set`, `pidgeon generate`)
- Progressive disclosure (show more detail as users get more specific)

### **Cross-Standard Intelligence**
- Unified semantic paths work across HL7, FHIR, and NCPDP
- Clear indication when paths are not applicable to certain standards
- Educational cross-standard comparisons to build user mental models
- Consistent behavior regardless of which standard user primarily works with

---

## 💡 **Integration Points**

### **Existing Command Enhancement**
- `pidgeon set` tab completion for semantic paths
- `pidgeon generate` path validation and suggestions
- `pidgeon session export` includes path documentation in templates

### **GUI Integration**
- CLI path discovery results viewable in GUI field picker
- GUI field selections export equivalent `pidgeon path` commands
- Consistent semantic path experience across CLI and GUI

### **Documentation Generation**
- Export path mappings for integration specifications
- Generate vendor-specific field reference documents
- Create custom semantic path extensions for organization-specific needs

---

**Design Philosophy**: Make Pidgeon's cross-standard magic discoverable, learnable, and trustworthy. Every user should feel confident they're using the right path for their use case, regardless of their healthcare standards expertise level.