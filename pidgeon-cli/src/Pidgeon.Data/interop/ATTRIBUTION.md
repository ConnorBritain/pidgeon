# HL7 v2-to-FHIR Mapping Attribution

## License and Source

This directory contains mappings from the official HL7 v2-to-FHIR Implementation Guide, used under the Apache License 2.0.

**Source Repository**: https://github.com/HL7/v2-to-fhir
**License**: Apache License 2.0 (see LICENSE file)
**HL7 Organization**: Copyright © HL7® International

## Usage in Pidgeon

These mappings are used to automatically generate semantic paths for the Pidgeon healthcare interoperability platform. The original CSV files are used as-is to:

1. **Parse segment mappings** (e.g., PID→Patient, PV1→Encounter) into semantic paths
2. **Extract datatype conversions** (e.g., CX→Identifier, XPN→HumanName)
3. **Map vocabulary codes** between HL7 v2 tables and FHIR CodeSystems
4. **Generate conditional logic** from IF/THEN mapping statements

## Update Strategy

We use git subtree to pull updates directly from the official HL7 repository:
```bash
git subtree pull --prefix=data/interop/hl7-fhir-mappings hl7-v2-fhir master --squash
```

This preserves original file names and structure, ensuring we stay synchronized with official HL7 standards work while maintaining clear attribution.

## Compliance Requirements

Per Apache License 2.0:
- ✅ **License Notice**: Original LICENSE file preserved
- ✅ **Attribution**: This ATTRIBUTION.md file documents source and usage
- ✅ **No Modifications**: Original CSV files used unchanged
- ✅ **Copyright Notice**: HL7 International copyright respected

## Original Work

The HL7 v2-to-FHIR mappings represent years of expert healthcare standards work by the HL7 International community. We are grateful to use this authoritative source for ensuring standards compliance in our semantic path system.