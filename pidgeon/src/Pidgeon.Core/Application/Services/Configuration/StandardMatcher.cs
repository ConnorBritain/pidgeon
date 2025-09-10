// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Pidgeon.Core.Domain.Configuration.Entities;

namespace Pidgeon.Core.Application.Services.Configuration;

/// <summary>
/// Service responsible for determining if vendor specifications support specific healthcare standards.
/// Single responsibility: Standard compatibility logic and version matching.
/// </summary>
internal static class StandardMatcher
{
    /// <summary>
    /// Determines if a vendor specification supports the requested healthcare standard.
    /// </summary>
    /// <param name="specification">Vendor specification to check</param>
    /// <param name="requestedStandard">Healthcare standard to match against</param>
    /// <returns>True if the specification supports the standard</returns>
    public static bool SupportsStandard(VendorSpecification specification, string requestedStandard)
    {
        var specStandard = specification.Specification.Standard;
        var standardUpper = requestedStandard.ToUpperInvariant();
        
        return standardUpper switch
        {
            // HL7 v2.x family
            "HL7" => specStandard.Contains("HL7", StringComparison.OrdinalIgnoreCase),
            "HL7V21" or "HL7V2.1" => specStandard.Contains("2.1"),
            "HL7V22" or "HL7V2.2" => specStandard.Contains("2.2"),
            "HL7V23" or "HL7V2.3" => specStandard.Contains("2.3"),
            "HL7V24" or "HL7V2.4" => specStandard.Contains("2.4"),
            "HL7V25" or "HL7V2.5" => specStandard.Contains("2.5"),
            "HL7V26" or "HL7V2.6" => specStandard.Contains("2.6"),
            "HL7V27" or "HL7V2.7" => specStandard.Contains("2.7"),
            "HL7V28" or "HL7V2.8" => specStandard.Contains("2.8"),
            
            // FHIR family
            "FHIR" => specStandard.Contains("FHIR", StringComparison.OrdinalIgnoreCase),
            "FHIRR4" or "FHIR_R4" => specStandard.Contains("R4"),
            "FHIRR5" or "FHIR_R5" => specStandard.Contains("R5"),
            "FHIRSTU3" or "FHIR_STU3" => specStandard.Contains("STU3"),
            
            // NCPDP family
            "NCPDP" => specStandard.Contains("NCPDP", StringComparison.OrdinalIgnoreCase),
            
            // NCPDP SCRIPT (prescriptions via SureScripts, etc.) - Current versions only
            "NCPDP_SCRIPT" or "NCPDPSCRIPT" => specStandard.Contains("SCRIPT", StringComparison.OrdinalIgnoreCase),
            "NCPDP_SCRIPT_2017071" => specStandard.Contains("SCRIPT_2017071"),
            "NCPDP_SCRIPT_2023011" => specStandard.Contains("SCRIPT_2023011"), // Latest as of 2024
            
            // NCPDP Claims/Billing (D.0, F1, F2)
            "NCPDP_D0" or "NCPDPD0" => specStandard.Contains("D.0"),
            "NCPDP_F1" or "NCPDPF1" => specStandard.Contains("F1"),
            "NCPDP_F2" or "NCPDPF2" => specStandard.Contains("F2"),
            
            // DICOM family (for imaging interfaces)
            "DICOM" => specStandard.Contains("DICOM", StringComparison.OrdinalIgnoreCase),
            
            // Default to include if unsure - allows for extensibility
            _ => true
        };
    }

    /// <summary>
    /// Gets all supported standards that match the specification.
    /// Useful for discovery and compatibility reporting.
    /// </summary>
    /// <param name="specification">Vendor specification to analyze</param>
    /// <returns>List of standards this specification supports</returns>
    public static IReadOnlyList<string> GetSupportedStandards(VendorSpecification specification)
    {
        var supported = new List<string>();
        var specStandard = specification.Specification.Standard;
        
        // Check HL7 versions
        if (specStandard.Contains("HL7", StringComparison.OrdinalIgnoreCase))
        {
            supported.Add("HL7");
            if (specStandard.Contains("2.3")) supported.Add("HL7v2.3");
            if (specStandard.Contains("2.4")) supported.Add("HL7v2.4");
            if (specStandard.Contains("2.5")) supported.Add("HL7v2.5");
            // Add others as needed
        }
        
        // Check FHIR versions  
        if (specStandard.Contains("FHIR", StringComparison.OrdinalIgnoreCase))
        {
            supported.Add("FHIR");
            if (specStandard.Contains("R4")) supported.Add("FHIR_R4");
            if (specStandard.Contains("R5")) supported.Add("FHIR_R5");
            if (specStandard.Contains("STU3")) supported.Add("FHIR_STU3");
        }
        
        // Check NCPDP versions
        if (specStandard.Contains("NCPDP", StringComparison.OrdinalIgnoreCase))
        {
            supported.Add("NCPDP");
            if (specStandard.Contains("SCRIPT"))
            {
                supported.Add("NCPDP_SCRIPT");
                if (specStandard.Contains("2017071")) supported.Add("NCPDP_SCRIPT_2017071");
                if (specStandard.Contains("2023011")) supported.Add("NCPDP_SCRIPT_2023011");
            }
        }
        
        return supported;
    }

    /// <summary>
    /// Checks if two standards are compatible (e.g., different versions of same family).
    /// </summary>
    /// <param name="standard1">First standard</param>
    /// <param name="standard2">Second standard</param>
    /// <returns>True if standards are compatible</returns>
    public static bool AreStandardsCompatible(string standard1, string standard2)
    {
        var std1 = standard1.ToUpperInvariant();
        var std2 = standard2.ToUpperInvariant();
        
        // Same standard
        if (std1 == std2) return true;
        
        // HL7 family compatibility
        if (std1.Contains("HL7") && std2.Contains("HL7")) return true;
        
        // FHIR family compatibility  
        if (std1.Contains("FHIR") && std2.Contains("FHIR")) return true;
        
        // NCPDP family compatibility
        if (std1.Contains("NCPDP") && std2.Contains("NCPDP")) return true;
        
        return false;
    }
}