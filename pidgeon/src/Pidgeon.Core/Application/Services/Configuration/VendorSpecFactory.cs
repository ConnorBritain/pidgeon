// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Pidgeon.Core.Domain.Configuration.Entities;

namespace Pidgeon.Core.Application.Services.Configuration;

/// <summary>
/// Factory for creating default vendor specifications.
/// Single responsibility: Generate baseline vendor configurations for common healthcare interfaces.
/// </summary>
internal static class VendorSpecFactory
{
    /// <summary>
    /// Creates default vendor specifications covering major healthcare interface types.
    /// </summary>
    /// <returns>Collection of default vendor specifications</returns>
    public static IEnumerable<VendorSpecification> CreateDefaultSpecifications()
    {
        yield return CreateEhrPharmacySpec();
        yield return CreateLabEhrSpec();
        yield return CreateNcpdpScriptSpec();
        yield return CreateEpicSpec();
        yield return CreateCernerSpec();
        yield return CreateImagingSpec();
    }

    private static VendorSpecification CreateEhrPharmacySpec()
    {
        return new VendorSpecification
        {
            Id = "generic-ehr-pharmacy",
            Specification = new SpecificationInfo
            {
                Name = "Generic EHR to Pharmacy Interface",
                VendorName = "Generic",
                Standard = "HL7v2.3",
                InterfaceType = "EHR_to_Pharmacy",
                Confidential = false,
                Description = "Standard EHR to pharmacy interface specification template"
            },
            DetectionInfo = new VendorDetectionInfo
            {
                CommonApplicationNames = { "EHR", "EMR", "EPIC", "CERNER" },
                CommonFacilityPatterns = { "*HOSPITAL*", "*CLINIC*", "*HEALTH*" },
                MessageTypePatterns = { "ADT^A01", "ADT^A03", "ORM^O01" },
                BaseConfidence = 0.7
            },
            MessagesToReceiver = new Dictionary<string, MessageTypeSpec>
            {
                ["ADT"] = SpecificationBuilder.CreateBasicADTSpec(),
                ["ORM"] = SpecificationBuilder.CreateBasicORMSpec()
            }
        };
    }

    private static VendorSpecification CreateLabEhrSpec()
    {
        return new VendorSpecification
        {
            Id = "generic-lab-ehr",
            Specification = new SpecificationInfo
            {
                Name = "Generic Lab to EHR Interface",
                VendorName = "Generic",
                Standard = "HL7v2.5",
                InterfaceType = "Lab_to_EHR",
                Confidential = false,
                Description = "Standard lab results interface specification template"
            },
            DetectionInfo = new VendorDetectionInfo
            {
                CommonApplicationNames = { "LAB", "LIS", "LABCORP", "QUEST" },
                CommonFacilityPatterns = { "*LAB*", "*QUEST*", "*LABCORP*" },
                MessageTypePatterns = { "ORU^R01", "ORU^R03" },
                BaseConfidence = 0.8
            },
            MessagesToReceiver = new Dictionary<string, MessageTypeSpec>
            {
                ["ORU"] = SpecificationBuilder.CreateBasicORUSpec()
            }
        };
    }

    private static VendorSpecification CreateNcpdpScriptSpec()
    {
        return new VendorSpecification
        {
            Id = "generic-ncpdp-script",
            Specification = new SpecificationInfo
            {
                Name = "Generic NCPDP SCRIPT Interface",
                VendorName = "Generic",
                Standard = "NCPDP_SCRIPT_2017071", // Current production standard
                InterfaceType = "EHR_to_Pharmacy",
                Confidential = false,
                Description = "NCPDP SCRIPT prescription ordering via SureScripts (current production standard)",
                DocumentVersion = "Production until 2028, then 2023011 required"
            },
            DetectionInfo = new VendorDetectionInfo
            {
                CommonApplicationNames = { "SURESCRIPTS", "SCRIPT", "NCPDP" },
                CommonFacilityPatterns = { "*SCRIPT*", "*SURESCRIPTS*" },
                MessageTypePatterns = { "NEWRX", "REFILL", "CANCEL" },
                BaseConfidence = 0.9
            },
            MessagesToReceiver = new Dictionary<string, MessageTypeSpec>
            {
                ["NEWRX"] = SpecificationBuilder.CreateBasicSCRIPTSpec(),
                ["REFILL"] = SpecificationBuilder.CreateBasicSCRIPTSpec(),
                ["CANCEL"] = SpecificationBuilder.CreateBasicSCRIPTSpec()
            }
        };
    }

    private static VendorSpecification CreateEpicSpec()
    {
        return new VendorSpecification
        {
            Id = "epic-systems",
            Specification = new SpecificationInfo
            {
                Name = "Epic Systems EHR Interface",
                VendorName = "Epic",
                Standard = "HL7v2.3",
                InterfaceType = "EHR_to_EHR",
                Confidential = false,
                Description = "Epic EHR interface specification with Hyperspace integration"
            },
            DetectionInfo = new VendorDetectionInfo
            {
                CommonApplicationNames = { "EPIC", "HYPERSPACE", "EHR" },
                CommonFacilityPatterns = { "*EPIC*", "*_PROD", "*_TEST" },
                MessageTypePatterns = { "ADT^A01", "ADT^A08", "ORM^O01", "ORU^R01" },
                BaseConfidence = 0.9
            },
            CommonDeviations = new List<VendorDeviation>
            {
                new()
                {
                    Type = "FieldFormat",
                    Location = "MSH.3",
                    Description = "Epic often includes environment suffixes (_PROD, _TEST)",
                    Frequency = 0.8,
                    Severity = "Info"
                }
            }
        };
    }

    private static VendorSpecification CreateCernerSpec()
    {
        return new VendorSpecification
        {
            Id = "cerner-millennium",
            Specification = new SpecificationInfo
            {
                Name = "Cerner Millennium EHR Interface",
                VendorName = "Cerner",
                Standard = "HL7v2.5",
                InterfaceType = "EHR_to_EHR",
                Confidential = false,
                Description = "Cerner Millennium EHR interface specification (Oracle Health)"
            },
            DetectionInfo = new VendorDetectionInfo
            {
                CommonApplicationNames = { "CERNER", "MILLENNIUM", "POWERCHART" },
                CommonFacilityPatterns = { "*CERNER*", "*MILLENNIUM*" },
                MessageTypePatterns = { "ADT^A01", "ADT^A08", "ORM^O01", "ORU^R01" },
                BaseConfidence = 0.9
            },
            CommonDeviations = new List<VendorDeviation>
            {
                new()
                {
                    Type = "FieldPresence",
                    Location = "PID.4",
                    Description = "Cerner often includes multiple patient IDs in PID.4",
                    Frequency = 0.7,
                    Severity = "Info"
                }
            }
        };
    }

    private static VendorSpecification CreateImagingSpec()
    {
        return new VendorSpecification
        {
            Id = "generic-imaging-ris",
            Specification = new SpecificationInfo
            {
                Name = "Generic Imaging RIS Interface",
                VendorName = "Generic",
                Standard = "HL7v2.5",
                InterfaceType = "Imaging_to_EHR",
                Confidential = false,
                Description = "Standard radiology information system interface"
            },
            DetectionInfo = new VendorDetectionInfo
            {
                CommonApplicationNames = { "RIS", "PACS", "IMAGING", "RADIOLOGY" },
                CommonFacilityPatterns = { "*RAD*", "*IMAGING*", "*XRAY*" },
                MessageTypePatterns = { "ORM^O01", "ORU^R01", "SIU^S12" },
                BaseConfidence = 0.8
            },
            MessagesToReceiver = new Dictionary<string, MessageTypeSpec>
            {
                ["ORU"] = SpecificationBuilder.CreateImagingORUSpec(),
                ["SIU"] = SpecificationBuilder.CreateBasicSIUSpec()
            }
        };
    }
}