# P0.2 De-identification Strategy

**Phase**: P0 MVP Development - Week 3  
**Feature**: On-Premises De-identification Engine  
**Status**: Architecture Design Phase  
**Created**: September 5, 2025  

---

## <� **Strategic Value**

**North Star Alignment**: Directly addresses "Realistic scenario testing without the PHI compliance nightmare"  
**Differentiation**: No competitor offers on-premises de-identification integrated with synthetic generation  
**User Segment**: Unlocks "real data" users who need to test with production-like scenarios  

---

## =� **Core Requirements**

### **Functional Requirements**
1. **Deterministic ID remapping** - Consistent patient/encounter IDs across messages
2. **Date shifting** - Configurable offset (�N days) with relationship preservation  
3. **Cross-message consistency** - Same patient � same synthetic ID across all messages
4. **Fully on-premises** - Zero cloud dependencies, no data leaves the organization
5. **Referential integrity** - Preserve relationships between related messages (orders � results)

### **Performance Requirements**
- Process 1,000+ messages maintaining cross-message consistency
- <100ms processing time per message
- Zero false positives on PHI detection
- Deterministic output with salt-based hashing

### **CLI Interface**
```bash
# Basic de-identification
pidgeon deident --in ./samples --out ./synthetic --date-shift 30d

# With team-specific salt for deterministic output
pidgeon deident --in msg.hl7 --out msg_safe.hl7 --salt "team-seed" --preview

# Batch processing with consistency
pidgeon deident --in ./prod_messages --out ./test_data --map-file id_mappings.json
```

---

## <� **Architecture Design**

### **Four-Domain Architecture Alignment**

```csharp
// Domain/DeIdentification/
namespace Pidgeon.Core.Domain.DeIdentification
{
    // Core de-identification context maintaining state across messages
    public record DeIdentificationContext(
        string Salt,                          // Team-specific salt for deterministic hashing
        TimeSpan DateShift,                   // Date offset to apply
        Dictionary<string, string> IdMap,     // Original ID � Synthetic ID mapping
        HashSet<string> ProcessedIdentifiers  // Track seen identifiers for consistency
    );

    // Domain service interface
    public interface IDeIdentificationService
    {
        Result<string> DeIdentifyMessage(string message, DeIdentificationContext context);
        Result<DeIdentificationContext> CreateContext(DeIdentificationOptions options);
        Result<DeIdentificationReport> ValidateDeIdentification(string original, string deidentified);
    }

    // Options for de-identification process
    public record DeIdentificationOptions(
        string? Salt,
        TimeSpan? DateShift,
        bool PreserveRelationships,
        bool GenerateReport,
        DeIdentificationMethod Method // SafeHarbor, Expert, Synthetic
    );
}

// Application/Services/DeIdentification/
namespace Pidgeon.Core.Application.Services.DeIdentification
{
    public interface IDeIdentificationEngine  
    {
        Task<Result<DeIdentificationResult>> ProcessAsync(
            string inputPath, 
            string outputPath,
            DeIdentificationOptions options);
        
        Task<Result<BatchDeIdentificationResult>> ProcessBatchAsync(
            string inputDirectory,
            string outputDirectory,
            DeIdentificationOptions options);
    }
}

// Infrastructure/Standards/HL7/v23/DeIdentification/
namespace Pidgeon.Core.Infrastructure.Standards.HL7.v23.DeIdentification
{
    public class HL7v23DeIdentifier : IStandardDeIdentifier
    {
        private readonly IPhiDetector _phiDetector;
        private readonly IIdentifierGenerator _idGenerator;
        
        public Result<string> DeIdentifySegment(string segment, DeIdentificationContext context)
        {
            // Segment-specific de-identification logic
        }
    }
}
```

### **PHI Detection Strategy**

**HL7 Fields Requiring De-identification**:
- **PID.3**: Patient Identifier List (MRN, SSN, etc.)
- **PID.5**: Patient Name
- **PID.7**: Date of Birth (shift dates)
- **PID.8**: Administrative Sex (keep - not PHI)
- **PID.11**: Patient Address
- **PID.13**: Phone Number - Home
- **PID.14**: Phone Number - Business
- **PID.19**: SSN Number
- **NK1**: Next of Kin (all identifying fields)
- **PV1.8**: Referring Doctor
- **PV1.9**: Consulting Doctor
- **PV1.17**: Admitting Doctor
- **IN1**: Insurance information (policy numbers)
- **GT1**: Guarantor information

---

## = **Compliance Strategy**

### **HIPAA Safe Harbor Method (§164.514(b)(2))**

**Requirements**: Remove all 18 identifiers specified by HIPAA:

1. **Names** - Full removal required
2. **Geographic subdivisions** smaller than state (street, city, county, ZIP except first 3 digits if population >20,000)
3. **Date elements** (except year) - birth, admission, discharge, death dates
4. **Phone numbers** - All telephone numbers
5. **Fax numbers** - All facsimile numbers
6. **Email addresses** - All electronic mail addresses
7. **Social Security numbers** - Full SSN removal
8. **Medical record numbers** - MRN, patient ID
9. **Health plan beneficiary numbers** - Insurance member IDs
10. **Account numbers** - Patient account numbers
11. **Certificate/license numbers** - Professional licenses, certificates
12. **Vehicle identifiers** - License plates, VIN numbers
13. **Device identifiers** - Serial numbers for medical devices
14. **Web URLs** - Any web addresses
15. **IP addresses** - Internet protocol addresses
16. **Biometric identifiers** - Fingerprints, voiceprints, retinal scans
17. **Full-face photographs** - And comparable images
18. **Any other unique identifying number, characteristic, or code**

**HL7 Field Mapping**:
```
PID.3  → Medical record numbers (#8)
PID.5  → Names (#1)
PID.7  → Date of birth (#3)
PID.11 → Address (#2)
PID.13 → Phone numbers (#4)
PID.19 → SSN (#7)
NK1.*  → Next of kin (names, phones)
PV1.8  → Referring doctor names (#1)
IN1.*  → Insurance IDs (#9)
```

**Implementation**: Deterministic replacement with synthetic values maintaining format

### **Expert Determination Method (§164.514(b)(1))**

**Requirement**: Statistical analysis showing "very small" re-identification risk

**Approach**: K-anonymity with L-diversity
- **K-anonymity (k≥5)**: Each record indistinguishable from at least 4 others
- **L-diversity (l≥3)**: At least 3 different values for sensitive attributes
- **Quasi-identifiers**: Age ranges, partial ZIP, admission month/year
- **Risk threshold**: <0.04% re-identification probability

**Implementation**:
```csharp
public class StatisticalDeIdentifier
{
    private const int MinimumKAnonymity = 5;
    private const int MinimumLDiversity = 3;
    
    public Result<DeIdentificationMetrics> AssessRisk(Message[] messages)
    {
        // Group by quasi-identifiers
        // Measure k-anonymity per group
        // Ensure l-diversity for diagnoses
        // Calculate re-identification risk
    }
}
```

### **Synthetic Data Approach (Pidgeon Hybrid)**

**Strategy**: Combine deterministic replacement with realistic synthetic generation

**Components**:
1. **Deterministic mapping**: Salt-based hashing for ID consistency
2. **Realistic names**: Synthetic names from census data
3. **Date shifting**: Consistent offset maintaining temporal relationships
4. **Address generation**: Valid but synthetic addresses
5. **Format preservation**: Maintain HL7 field formats

**Advantages**:
- Maintains clinical scenario integrity
- Zero re-identification risk
- Preserves statistical properties
- Enables realistic testing

---

## =� **Implementation Plan**

### **Phase 1: Core De-identification (Week 3, Days 1-2)**
1. Create domain models for de-identification context
2. Implement PHI detection patterns for HL7 fields
3. Build deterministic ID mapping with salt-based hashing
4. Add date shifting with relationship preservation

### **Phase 2: Cross-Message Consistency (Week 3, Days 3-4)**  
1. Implement context persistence across messages
2. Build ID mapping cache for batch processing
3. Add referential integrity validation
4. Create relationship preservation logic

### **Phase 3: CLI Integration (Week 3, Days 4-5)**
1. Implement `pidgeon deident` command
2. Add preview mode for validation
3. Create batch processing support
4. Generate de-identification reports

### **Phase 4: Testing & Validation (Week 3, Day 5)**
1. Unit tests for PHI detection
2. Integration tests for cross-message consistency  
3. Performance tests for 1,000+ message batches
4. Compliance validation against standards

---

##  **Success Criteria**

### **Technical Metrics**
- [ ] 100% PHI removal rate (zero false negatives)
- [ ] <5% false positive rate (non-PHI marked as PHI)
- [ ] <100ms per message processing time
- [ ] Deterministic output with same salt
- [ ] Cross-message ID consistency maintained

### **User Experience**
- [ ] Single command to de-identify entire directories
- [ ] Preview mode shows what will be changed
- [ ] Clear reporting of de-identification actions
- [ ] Export/import of ID mapping files
- [ ] Integration with generation engine for hybrid workflows

### **Compliance**
- [ ] Meets HIPAA Safe Harbor requirements
- [ ] Provides audit trail of de-identification
- [ ] Supports organizational salt management
- [ ] No cloud dependencies or data transmission

---

## =� **Future Enhancements**

### **P1 Improvements**
- Machine learning-based PHI detection
- Custom field mapping rules
- FHIR resource de-identification
- DICOM image de-identification
- Configurable de-identification policies

### **P2 Advanced Features**
- Statistical disclosure control
- Differential privacy options
- Re-identification risk assessment
- Compliance reporting dashboards
- Team-shared salt management

---

## =� **References**

- HIPAA Privacy Rule: 45 CFR �164.514(b)
- HHS Guidance on De-identification of PHI
### **HIPAA Regulatory Sources**
- **HIPAA Privacy Rule**: 45 CFR Section 164.514(b) - De-identification of Protected Health Information
- **HHS Guidance**: Methods for De-identification of PHI (www.hhs.gov)
- **Safe Harbor Method**: Section 164.514(b)(2) - Removal of 18 identifiers
- **Expert Determination**: Section 164.514(b)(1) - Statistical methods for very small risk

### **Technical Standards**
- **HL7 International**: Best practices for de-identification in healthcare messages
- **ISO Standards**: Healthcare information privacy and security frameworks
- **NIST Guidelines**: Privacy-preserving data sharing techniques

### **Statistical Methods Research**
- **K-Anonymity**: "A Globally Optimal k-Anonymity Method for the De-Identification of Health Data" (JAMIA, 2009)
- **L-Diversity**: "l-Diversity: Privacy Beyond k-Anonymity" (ACM TKDD, 2007)
- **T-Closeness**: "t-Closeness: Privacy Beyond k-Anonymity and l-Diversity" (ICDE, 2007)
- **Differential Privacy**: Modern approaches to statistical disclosure control

### **Implementation Guidance**
- **2025 Updates**: Recent clarifications on social media handles, LGBTQ status, emotional support animals
- **ZIP Code Rules**: First 3 digits allowed if geographic unit contains 20,000+ people
- **Date Handling**: Year may be retained; all other date elements must be removed or shifted
- **Free Text Fields**: Special attention required for unstructured data in HL7 messages

### **Important Considerations**
- Safe Harbor list established in 1999 - additional modern identifiers may need consideration
- Both Safe Harbor and Expert Determination retain "very small" but non-zero re-identification risk
- Organizations should seek compliance expertise for production implementations
- Regular audits recommended to ensure continued compliance with evolving standards

---

## 🎯 **Pidgeon Implementation Recommendations**

### **Recommended Approach: Hybrid Safe Harbor + Synthetic**

**Rationale**: Combine HIPAA Safe Harbor compliance with intelligent synthetic data generation

**Key Benefits**:
1. **100% PHI removal guarantee** - Exceeds Safe Harbor by replacing ALL identifiers
2. **Maintains scenario integrity** - Relationships and patterns preserved
3. **Deterministic consistency** - Same patient gets same synthetic ID across messages
4. **Zero re-identification risk** - Synthetic data has no connection to real individuals
5. **No statistical analysis required** - Simpler than Expert Determination

### **Implementation Strategy**

#### **Phase 1: Safe Harbor Compliance (Required)**
- Remove all 18 HIPAA identifiers from HL7 messages
- Map HL7 fields to HIPAA identifier categories
- Validate complete PHI removal with scanning algorithms

#### **Phase 2: Synthetic Replacement (Value-Add)**
- Replace removed data with realistic synthetic values
- Use deterministic generation for consistency
- Maintain format compliance with HL7 specifications

#### **Phase 3: Consistency Preservation (Differentiator)**
- Track ID mappings across message batches
- Preserve temporal relationships with date shifting
- Maintain referential integrity between related messages

### **Compliance Validation**

**Built-in Verification**:
1. **PHI Scanner**: Detect any remaining identifiers
2. **Format Validator**: Ensure HL7 structure preserved
3. **Consistency Checker**: Verify cross-message ID mapping
4. **Audit Report**: Document all de-identification actions

### **Why This Approach for Pidgeon**

1. **Meets legal requirements**: Full HIPAA Safe Harbor compliance
2. **Exceeds expectations**: Goes beyond minimum with synthetic replacement
3. **Preserves utility**: Test scenarios remain realistic and valid
4. **Simple to implement**: No complex statistical analysis required
5. **Deterministic output**: Same input + salt = same output every time
6. **Fast processing**: <100ms per message with no ML dependencies