# Current Codebase → Four-Domain Architecture Mapping

## **Current State Analysis**

### **What We Already Have (Partially Organized):**

```yaml
Pidgeon.Core/
  Domain/
    DataTypes/
      HL7/ → Should move to Messaging Domain
        - CE_CodedElement.cs
        - CWE_CodedWithExceptions.cs  
        - CX_ExtendedCompositeId.cs
        - DR_DateRange.cs
        - FN_FamilyName.cs
        - HD_HierarchicDesignator.cs
        - XPN_ExtendedPersonName.cs
      Universal/ → Should move to Clinical Domain
        - Medication.cs
        - Patient.cs
        - Provider.cs
    Messages/
      HL7/ → Messaging Domain (correct!)
        - HL7Message.cs
      FHIR/ → Messaging Domain (correct!)
        - FHIRBundle.cs
      NCPDP/ → Messaging Domain (correct!)
        - NCPDPTransaction.cs
      HealthcareMessage.cs → Messaging Domain (correct!)
    ValueObjects/Universal/ → Clinical Domain (empty, needs population)

  Services/Configuration/ → Configuration Domain (excellent! already organized)
    - AnalysisResults.cs
    - ConfidenceCalculator.cs
    - ConfigurationCatalog.cs
    - ConfigurationInferenceService.cs
    - ConfigurationValidationService.cs
    - FieldPatternAnalyzer.cs
    - FormatDeviationDetector.cs
    - MessagePatternAnalyzer.cs
    - VendorDetectionService.cs
    - VendorPatternRepository.cs
    + All interfaces

  Types/ → Configuration Domain (should be moved there)
    - ConfigurationAddress.cs
    - ConfigurationMetadata.cs
    - FieldPatterns.cs
    - VendorConfiguration.cs
    - VendorDetectionPattern.cs
    - VendorSignature.cs
    + Others

  Standards/HL7/v23/ → Should become Messaging Domain HL7 subfolder
    Fields/, Segments/, Messages/, Configuration/
    
  Services/ (Core services) → Need analysis for domain assignment
    - GenerationService.cs
    - MessageService.cs  
    - TransformationService.cs
    - ValidationService.cs
```

## **Four-Domain Target Structure**

```yaml
Target Structure:
  Domain/
    Clinical/              # Healthcare business concepts
      Entities/
        - Patient.cs       (MOVE from Domain/DataTypes/Universal/)
        - Provider.cs      (MOVE from Domain/DataTypes/Universal/)
        - Medication.cs    (MOVE from Domain/DataTypes/Universal/)
        - Prescription.cs  (CREATE new - core business entity)
        - Encounter.cs     (CREATE new)
      ValueObjects/
        - PersonName.cs    (CREATE from XPN concepts)
        - Address.cs       (CREATE from XAD concepts)  
        - PhoneNumber.cs   (CREATE from XTN concepts)
        - Dosing.cs        (CREATE new)
        - BloodType.cs     (CREATE new)
        
    Messaging/             # Wire format structures
      HL7v2/
        Messages/
          - HL7Message.cs           (MOVE from Domain/Messages/HL7/)
          - HL7_ORM_Message.cs     (CREATE - complete ORM structure)
          - HL7_ADT_Message.cs     (CREATE - complete ADT structure)
          - HL7_RDE_Message.cs     (CREATE - complete RDE structure)
        Segments/
          - MSH_MessageHeader.cs   (REFACTOR from Standards/HL7/v23/Segments/)
          - PID_PatientIdentification.cs (REFACTOR from Standards/HL7/v23/Segments/)
          - PV1_PatientVisit.cs    (CREATE new with all 52 fields)
          - ORC_CommonOrder.cs     (REFACTOR from Standards/HL7/v23/Segments/)
          - OBR_ObservationRequest.cs (CREATE new with all 47 fields)
        DataTypes/
          - CX_ExtendedCompositeId.cs (MOVE from Domain/DataTypes/HL7/)
          - XPN_ExtendedPersonName.cs (MOVE from Domain/DataTypes/HL7/)
          - CE_CodedElement.cs        (MOVE from Domain/DataTypes/HL7/)
          + All other HL7 data types
      FHIR/
        Bundles/
          - FHIRBundle.cs           (MOVE from Domain/Messages/FHIR/)
          - MedicationRequestBundle.cs (CREATE new)
        Resources/  
          - FHIR_Patient.cs         (CREATE new)
          - FHIR_MedicationRequest.cs (CREATE new)
          - FHIR_Practitioner.cs    (CREATE new)
      NCPDP/
        Transactions/
          - NCPDPTransaction.cs     (MOVE from Domain/Messages/NCPDP/)
          - NCPDP_NewRx.cs         (CREATE new)
          - NCPDP_Refill.cs        (CREATE new)
        Segments/
          - UIB_InterchangeHeader.cs (CREATE new)
          - PVD_ProviderSegment.cs   (CREATE new)
          - PTT_PatientSegment.cs    (CREATE new)
          
    Configuration/         # Vendor patterns (already well organized!)
      Entities/
        - VendorConfiguration.cs  (MOVE from Types/)
        - ConfigurationAddress.cs (MOVE from Types/)
        - FieldPattern.cs         (CREATE from FieldPatterns.cs)
        - CustomSegmentPattern.cs (CREATE new)
        - FormatDeviation.cs      (MOVE from Types/)
      Services/
        - (MOVE all from Services/Configuration/)
        
    Transformation/        # Mapping rules (mostly new)
      Entities/
        - MappingRule.cs          (CREATE new)
        - TransformationSet.cs    (CREATE new)  
        - FieldMapping.cs         (CREATE new)
        - TransformFunction.cs    (CREATE new)
      Services/
        - ITransformationExecutor.cs (CREATE new)
        - MappingRuleEngine.cs       (CREATE new)

  AntiCorruption/          # Domain boundary interfaces (all new)
    - IClinicalToMessaging.cs
    - IMessagingToClinical.cs
    - IMessagingToConfiguration.cs
    - IConfigurationToTransformation.cs
    - ITransformationExecutor.cs
```

## **Migration Actions Required**

### **Phase 1: Directory Structure Creation**
```bash
# Create new domain directories
mkdir -p Domain/Clinical/Entities
mkdir -p Domain/Clinical/ValueObjects
mkdir -p Domain/Messaging/HL7v2/Messages
mkdir -p Domain/Messaging/HL7v2/Segments
mkdir -p Domain/Messaging/HL7v2/DataTypes
mkdir -p Domain/Messaging/FHIR/Bundles
mkdir -p Domain/Messaging/FHIR/Resources
mkdir -p Domain/Messaging/NCPDP/Transactions
mkdir -p Domain/Messaging/NCPDP/Segments
mkdir -p Domain/Configuration/Entities
mkdir -p Domain/Transformation/Entities
mkdir -p Domain/Transformation/Services
mkdir -p AntiCorruption
```

### **Phase 2: Move Existing Files**
```bash
# Move Clinical domain entities
mv Domain/DataTypes/Universal/Patient.cs Domain/Clinical/Entities/
mv Domain/DataTypes/Universal/Provider.cs Domain/Clinical/Entities/
mv Domain/DataTypes/Universal/Medication.cs Domain/Clinical/Entities/

# Move Messaging domain structures
mv Domain/Messages/HL7/HL7Message.cs Domain/Messaging/HL7v2/Messages/
mv Domain/Messages/FHIR/FHIRBundle.cs Domain/Messaging/FHIR/Bundles/
mv Domain/Messages/NCPDP/NCPDPTransaction.cs Domain/Messaging/NCPDP/Transactions/
mv Domain/Messages/HealthcareMessage.cs Domain/Messaging/

# Move HL7 data types
mv Domain/DataTypes/HL7/* Domain/Messaging/HL7v2/DataTypes/

# Move Configuration domain
mv Types/VendorConfiguration.cs Domain/Configuration/Entities/
mv Types/ConfigurationAddress.cs Domain/Configuration/Entities/
mv Types/FieldPatterns.cs Domain/Configuration/Entities/
mv Types/FormatDeviation.cs Domain/Configuration/Entities/
# Move all Services/Configuration/ to Domain/Configuration/Services/

# Clean up empty directories
rmdir Domain/DataTypes/HL7
rmdir Domain/DataTypes/Universal  
rmdir Domain/DataTypes
rmdir Domain/Messages/HL7
rmdir Domain/Messages/FHIR
rmdir Domain/Messages/NCPDP
rmdir Domain/Messages
```

### **Phase 3: Create New Entities**
```csharp
// Clinical Domain
Domain/Clinical/Entities/Prescription.cs
Domain/Clinical/Entities/Encounter.cs
Domain/Clinical/ValueObjects/PersonName.cs
Domain/Clinical/ValueObjects/Address.cs
Domain/Clinical/ValueObjects/PhoneNumber.cs

// Messaging Domain - Complete message structures
Domain/Messaging/HL7v2/Messages/HL7_ORM_Message.cs
Domain/Messaging/HL7v2/Messages/HL7_ADT_Message.cs
Domain/Messaging/HL7v2/Segments/PV1_PatientVisit.cs
Domain/Messaging/HL7v2/Segments/OBR_ObservationRequest.cs

// Transformation Domain (all new)
Domain/Transformation/Entities/MappingRule.cs
Domain/Transformation/Entities/TransformationSet.cs
Domain/Transformation/Services/MappingRuleEngine.cs

// Anti-Corruption Layer (all new)
AntiCorruption/IClinicalToMessaging.cs
AntiCorruption/IMessagingToClinical.cs
```

### **Phase 4: Refactor Services**
```csharp
// Update service dependencies to use single domains
Services/GenerationService.cs → Uses Clinical domain only
Services/MessageService.cs → Uses Messaging domain only
Services/ValidationService.cs → Uses Configuration domain only
Services/TransformationService.cs → Uses Transformation domain only

// Move configuration services
mv Services/Configuration/* Domain/Configuration/Services/
```

## **What's Already Good**

✅ **Configuration Intelligence**: Already well organized in `Services/Configuration/` - just needs to move to `Domain/Configuration/Services/`

✅ **Message Foundation**: Basic `HL7Message`, `FHIRBundle`, `NCPDPTransaction` structures exist

✅ **HL7 Data Types**: Complete set of HL7 composite data types already implemented

✅ **Plugin Architecture**: Standards organization under `Standards/` is clean

✅ **Service Interfaces**: Core service interfaces already defined

## **What Needs Major Work**

❌ **Clinical Domain**: Missing core business entities like `Prescription`, `Encounter`

❌ **Complete Message Structures**: Need full `HL7_ORM_Message` with all segments

❌ **Transformation Domain**: Completely missing - all new code needed

❌ **Anti-Corruption Layer**: Completely missing - all new interfaces needed

❌ **Value Objects**: Clinical domain value objects need creation

## **Priority Order**

1. **Week 1**: Create directory structure + move existing files
2. **Week 1**: Create Clinical domain entities (Prescription, Encounter)  
3. **Week 1**: Create basic Transformation domain (MappingRule, TransformationSet)
4. **Week 1**: Create one complete message structure (HL7_ORM_Message)
5. **Week 2**: Create Anti-Corruption Layer interfaces
6. **Week 2**: Refactor services to use single domains
7. **Week 3**: Complete remaining message structures
8. **Week 4**: Performance optimization and testing

## **Risk Assessment**

**LOW RISK** (just moving files):
- Moving existing entities to correct domains
- Moving HL7 data types to messaging domain
- Moving configuration services

**MEDIUM RISK** (refactoring):
- Updating service dependencies 
- Creating Anti-Corruption Layer interfaces
- Namespace updates across solution

**HIGH RISK** (new development):
- Complete message structures (HL7_ORM_Message with all segments)
- Transformation domain implementation
- Anti-Corruption Layer implementation

**Next Step**: Start with LOW RISK moves to get directory structure in place, then incrementally add new functionality.