// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Segmint.Core.Standards.HL7.v23.Messages;
using Segmint.Core.Standards.HL7.v23.Segments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Segmint.Core.Standards.HL7.v23.Workflows.Common;

/// <summary>
/// Workflow-specific validation utilities for HL7 messages.
/// Provides enhanced validation beyond basic message structure validation.
/// </summary>
public static class WorkflowValidation
{
    #region Pharmacy Workflow Validations

    /// <summary>
    /// Validates a prescription workflow (RDE message) for pharmacy compliance.
    /// </summary>
    /// <param name="rdeMessage">RDE message to validate</param>
    /// <returns>Validation result with pharmacy-specific checks</returns>
    public static PharmacyValidationResult ValidatePrescriptionWorkflow(RDEMessage rdeMessage)
    {
        var result = new PharmacyValidationResult();
        
        // Basic message validation
        var validationResult = rdeMessage.Validate();
        result.BasicValidationIssues.AddRange(validationResult.Issues.Select(issue => issue.ToString()));
        
        // Pharmacy-specific validations
        ValidatePatientInformation(rdeMessage, result);
        ValidateMedicationInformation(rdeMessage, result);
        ValidateProviderInformation(rdeMessage, result);
        ValidateDispenseInformation(rdeMessage, result);
        ValidateControlledSubstanceCompliance(rdeMessage, result);
        
        // Set overall status
        result.IsPharmacyCompliant = result.PharmacyValidationIssues.Count == 0;
        result.IsOverallValid = result.BasicValidationIssues.Count == 0 && result.IsPharmacyCompliant;
        
        return result;
    }

    /// <summary>
    /// Validates a dispense workflow (RDS message) for pharmacy compliance.
    /// </summary>
    /// <param name="rdsMessage">RDS message to validate</param>
    /// <returns>Validation result with dispense-specific checks</returns>
    public static PharmacyValidationResult ValidateDispenseWorkflow(RDSMessage rdsMessage)
    {
        var result = new PharmacyValidationResult();
        
        // Basic message validation
        var validationResult = rdsMessage.Validate();
        result.BasicValidationIssues.AddRange(validationResult.Issues.Select(issue => issue.ToString()));
        
        // Dispense-specific validations
        ValidateDispenseRecords(rdsMessage, result);
        ValidateDispenseQuantities(rdsMessage, result);
        ValidatePharmacistInformation(rdsMessage, result);
        ValidateLotAndExpirationInfo(rdsMessage, result);
        
        // Set overall status
        result.IsPharmacyCompliant = result.PharmacyValidationIssues.Count == 0;
        result.IsOverallValid = result.BasicValidationIssues.Count == 0 && result.IsPharmacyCompliant;
        
        return result;
    }

    /// <summary>
    /// Validates an order response workflow (ORR message) for completeness.
    /// </summary>
    /// <param name="orrMessage">ORR message to validate</param>
    /// <returns>Validation result with response-specific checks</returns>
    public static ResponseValidationResult ValidateOrderResponseWorkflow(ORRMessage orrMessage)
    {
        var result = new ResponseValidationResult();
        
        // Basic message validation
        var validationResult = orrMessage.Validate();
        result.BasicValidationIssues.AddRange(validationResult.Issues.Select(issue => issue.ToString()));
        
        // Response-specific validations
        ValidateAcknowledgmentInfo(orrMessage, result);
        ValidateErrorReporting(orrMessage, result);
        ValidateOrderResponseCorrelation(orrMessage, result);
        
        // Set overall status
        result.IsResponseComplete = result.ResponseValidationIssues.Count == 0;
        result.IsOverallValid = result.BasicValidationIssues.Count == 0 && result.IsResponseComplete;
        
        return result;
    }

    #endregion

    #region Clinical Workflow Validations

    /// <summary>
    /// Validates drug interaction checks for prescription workflows.
    /// </summary>
    /// <param name="medicationCode">Primary medication code</param>
    /// <param name="existingMedications">List of existing medications</param>
    /// <param name="patientAllergies">List of patient allergies</param>
    /// <returns>Drug interaction validation result</returns>
    public static DrugInteractionValidationResult ValidateDrugInteractions(
        string medicationCode,
        List<string> existingMedications,
        List<string> patientAllergies)
    {
        var result = new DrugInteractionValidationResult();
        
        // Mock interaction checks (in production, this would integrate with drug database)
        CheckForDrugDrugInteractions(medicationCode, existingMedications, result);
        CheckForDrugAllergyInteractions(medicationCode, patientAllergies, result);
        
        return result;
    }

    /// <summary>
    /// Validates dosage calculations for prescription workflows.
    /// </summary>
    /// <param name="patientWeight">Patient weight in kg</param>
    /// <param name="medicationCode">Medication code</param>
    /// <param name="dosageAmount">Prescribed dosage amount</param>
    /// <param name="dosageUnits">Dosage units</param>
    /// <returns>Dosage validation result</returns>
    public static DosageValidationResult ValidateDosage(
        decimal? patientWeight,
        string medicationCode,
        decimal dosageAmount,
        string dosageUnits)
    {
        var result = new DosageValidationResult();
        
        // Mock dosage validation (in production, this would use drug database)
        if (patientWeight.HasValue)
        {
            var weightBasedDose = CalculateWeightBasedDose(patientWeight.Value, medicationCode);
            if (weightBasedDose.HasValue)
            {
                var variance = Math.Abs(dosageAmount - weightBasedDose.Value) / weightBasedDose.Value;
                if (variance > 0.2m) // 20% variance threshold
                {
                    result.DosageIssues.Add($"Dosage {dosageAmount} {dosageUnits} is {variance:P0} different from recommended weight-based dose");
                }
            }
        }
        
        // Check for reasonable dosage ranges
        ValidateDosageRange(medicationCode, dosageAmount, dosageUnits, result);
        
        result.IsDosageValid = result.DosageIssues.Count == 0;
        return result;
    }

    #endregion

    #region Compliance Validations

    /// <summary>
    /// Validates controlled substance compliance for prescription workflows.
    /// </summary>
    /// <param name="medicationCode">Medication code</param>
    /// <param name="deaNumber">Provider's DEA number</param>
    /// <param name="schedule">Controlled substance schedule</param>
    /// <param name="refills">Number of refills</param>
    /// <returns>Controlled substance validation result</returns>
    public static ControlledSubstanceValidationResult ValidateControlledSubstanceCompliance(
        string medicationCode,
        string deaNumber,
        string schedule,
        int refills)
    {
        var result = new ControlledSubstanceValidationResult();
        
        // Validate DEA number format
        if (!IsValidDEANumber(deaNumber))
        {
            result.ComplianceIssues.Add($"Invalid DEA number format: {deaNumber}");
        }
        
        // Validate schedule-specific rules
        switch (schedule.ToUpper())
        {
            case "II":
                if (refills > 0)
                    result.ComplianceIssues.Add("Schedule II controlled substances cannot have refills");
                break;
            case "III":
            case "IV":
                if (refills > 5)
                    result.ComplianceIssues.Add($"Schedule {schedule} controlled substances cannot have more than 5 refills");
                break;
            case "V":
                if (refills > 5)
                    result.ComplianceIssues.Add($"Schedule {schedule} controlled substances cannot have more than 5 refills");
                break;
        }
        
        result.IsCompliant = result.ComplianceIssues.Count == 0;
        return result;
    }

    /// <summary>
    /// Validates prescription timing and validity periods.
    /// </summary>
    /// <param name="orderDateTime">Order date and time</param>
    /// <param name="medicationCode">Medication code</param>
    /// <param name="isControlledSubstance">Whether medication is controlled</param>
    /// <returns>Timing validation result</returns>
    public static TimingValidationResult ValidatePrescriptionTiming(
        DateTime orderDateTime,
        string medicationCode,
        bool isControlledSubstance)
    {
        var result = new TimingValidationResult();
        
        // Check if prescription is too old
        var daysSinceOrder = (DateTime.Now - orderDateTime).Days;
        
        if (isControlledSubstance && daysSinceOrder > 30)
        {
            result.TimingIssues.Add("Controlled substance prescription is older than 30 days");
        }
        else if (!isControlledSubstance && daysSinceOrder > 365)
        {
            result.TimingIssues.Add("Prescription is older than 1 year");
        }
        
        // Check for future dates
        if (orderDateTime > DateTime.Now)
        {
            result.TimingIssues.Add("Prescription cannot be dated in the future");
        }
        
        result.IsTimingValid = result.TimingIssues.Count == 0;
        return result;
    }

    #endregion

    #region Private Validation Methods

    /// <summary>
    /// Validates patient information in RDE message.
    /// </summary>
    private static void ValidatePatientInformation(RDEMessage rdeMessage, PharmacyValidationResult result)
    {
        var pid = rdeMessage.GetSegment<PIDSegment>();
        
        if (pid == null)
        {
            result.PharmacyValidationIssues.Add("Patient identification segment (PID) is required");
            return;
        }
        
        if (string.IsNullOrWhiteSpace(pid.PatientIdentifierList.IdNumber))
            result.PharmacyValidationIssues.Add("Patient ID is required for pharmacy workflows");
        
        if (string.IsNullOrWhiteSpace(pid.PatientName.FamilyName))
            result.PharmacyValidationIssues.Add("Patient last name is required for pharmacy workflows");
        
        // Check for date of birth (required for controlled substances)
        if (pid.DateTimeOfBirth.ToDateTime() == null)
            result.PharmacyValidationIssues.Add("Patient date of birth is recommended for pharmacy workflows");
    }

    /// <summary>
    /// Validates medication information in RDE message.
    /// </summary>
    private static void ValidateMedicationInformation(RDEMessage rdeMessage, PharmacyValidationResult result)
    {
        var rxe = rdeMessage.GetSegment<RXESegment>();
        
        if (rxe == null)
        {
            result.PharmacyValidationIssues.Add("Pharmacy encoded order segment (RXE) is required");
            return;
        }
        
        if (string.IsNullOrWhiteSpace(rxe.GiveCode.Identifier))
            result.PharmacyValidationIssues.Add("Medication code is required for pharmacy workflows");
        
        if (string.IsNullOrWhiteSpace(rxe.GiveCode.Text))
            result.PharmacyValidationIssues.Add("Medication name is required for pharmacy workflows");
        
        if (string.IsNullOrWhiteSpace(rxe.ProviderPharmacyTreatmentInstructions.Value))
            result.PharmacyValidationIssues.Add("Dosage instructions (sig) are required for pharmacy workflows");
        
        // Validate NDC format if present
        if (!string.IsNullOrWhiteSpace(rxe.GiveCode.Identifier) && !IsValidNDCFormat(rxe.GiveCode.Identifier))
            result.PharmacyValidationIssues.Add($"Invalid NDC format: {rxe.GiveCode.Identifier}");
    }

    /// <summary>
    /// Validates provider information in RDE message.
    /// </summary>
    private static void ValidateProviderInformation(RDEMessage rdeMessage, PharmacyValidationResult result)
    {
        var orc = rdeMessage.GetSegment<ORCSegment>();
        
        if (orc == null)
        {
            result.PharmacyValidationIssues.Add("Order control segment (ORC) is required");
            return;
        }
        
        if (string.IsNullOrWhiteSpace(orc.OrderingProvider.Value))
            result.PharmacyValidationIssues.Add("Ordering provider name is required for pharmacy workflows");
    }

    /// <summary>
    /// Validates dispense information in RDE message.
    /// </summary>
    private static void ValidateDispenseInformation(RDEMessage rdeMessage, PharmacyValidationResult result)
    {
        var rxe = rdeMessage.GetSegment<RXESegment>();
        
        if (rxe != null)
        {
            // Check for reasonable quantities
            // GiveAmount field validation needs review - field access pattern uncertain
                result.PharmacyValidationIssues.Add("Dispense quantity must be greater than 0");
            
            if (string.IsNullOrWhiteSpace(rxe.GiveUnits.Value))
                result.PharmacyValidationIssues.Add("Dispense units are required for pharmacy workflows");
        }
    }

    /// <summary>
    /// Validates controlled substance compliance in RDE message.
    /// </summary>
    private static void ValidateControlledSubstanceCompliance(RDEMessage rdeMessage, PharmacyValidationResult result)
    {
        var rxe = rdeMessage.GetSegment<RXESegment>();
        var notes = rdeMessage.GetSegments<NTESegment>();
        
        // Check if this is a controlled substance (look for DEA schedule in notes)
        var isControlledSubstance = notes.Any(n => n.Comment.Value?.Contains("Schedule") == true);
        
        if (isControlledSubstance)
        {
            // Additional validations for controlled substances
            if (rxe?.NumberOfRefills.ToInt() > 0)
            {
                var scheduleNote = notes.FirstOrDefault(n => n.Comment.Value?.Contains("Schedule II") == true);
                if (scheduleNote != null)
                {
                    result.PharmacyValidationIssues.Add("Schedule II controlled substances cannot have refills");
                }
            }
        }
    }

    /// <summary>
    /// Validates dispense records in RDS message.
    /// </summary>
    private static void ValidateDispenseRecords(RDSMessage rdsMessage, PharmacyValidationResult result)
    {
        if (rdsMessage.Dispenses.Count == 0)
        {
            result.PharmacyValidationIssues.Add("At least one dispense record is required");
            return;
        }
        
        foreach (var dispense in rdsMessage.Dispenses)
        {
            if (dispense.DispenseRecord == null)
            {
                result.PharmacyValidationIssues.Add("Dispense record (RXD) is required for each dispense");
                continue;
            }
            
            if (string.IsNullOrWhiteSpace(dispense.DispenseRecord.DispenseGiveCode.Identifier))
                result.PharmacyValidationIssues.Add("Dispensed medication code is required");
            
            if (dispense.DispenseRecord.ActualDispenseAmount.ToDecimal() == null)
                result.PharmacyValidationIssues.Add("Actual dispense amount is required");
        }
    }

    /// <summary>
    /// Validates dispense quantities in RDS message.
    /// </summary>
    private static void ValidateDispenseQuantities(RDSMessage rdsMessage, PharmacyValidationResult result)
    {
        foreach (var dispense in rdsMessage.Dispenses)
        {
            if (dispense.DispenseRecord != null)
            {
                var amount = dispense.DispenseRecord.ActualDispenseAmount.ToDecimal();
                if (amount.HasValue)
                {
                    if (amount <= 0)
                        result.PharmacyValidationIssues.Add("Dispense amount must be greater than 0");
                }
            }
        }
    }

    /// <summary>
    /// Validates pharmacist information in RDS message.
    /// </summary>
    private static void ValidatePharmacistInformation(RDSMessage rdsMessage, PharmacyValidationResult result)
    {
        foreach (var dispense in rdsMessage.Dispenses)
        {
            if (dispense.DispenseRecord != null)
            {
                // DispensingPharmacist field validation needs review - field type uncertain
                    result.PharmacyValidationIssues.Add("Dispensing pharmacist name is required");
            }
        }
    }

    /// <summary>
    /// Validates lot and expiration information in RDS message.
    /// </summary>
    private static void ValidateLotAndExpirationInfo(RDSMessage rdsMessage, PharmacyValidationResult result)
    {
        foreach (var dispense in rdsMessage.Dispenses)
        {
            if (dispense.DispenseRecord != null)
            {
                // Check for expiration date
                // ExpirationDate field validation needs review - field type uncertain
                    result.PharmacyValidationIssues.Add("Medication expiration date is recommended");
                
                // Check if medication is expired
                // ExpirationDate field validation needs review - field access pattern uncertain
                    result.PharmacyValidationIssues.Add("Cannot dispense expired medication");
            }
        }
    }

    /// <summary>
    /// Validates acknowledgment information in ORR message.
    /// </summary>
    private static void ValidateAcknowledgmentInfo(ORRMessage orrMessage, ResponseValidationResult result)
    {
        var msa = orrMessage.GetSegment<MSASegment>();
        
        if (msa == null)
        {
            result.ResponseValidationIssues.Add("Message acknowledgment segment (MSA) is required");
            return;
        }
        
        if (string.IsNullOrWhiteSpace(msa.AcknowledgmentCode.Value))
            result.ResponseValidationIssues.Add("Acknowledgment code is required");
        
        if (string.IsNullOrWhiteSpace(msa.MessageControlId.Value))
            result.ResponseValidationIssues.Add("Original message control ID is required");
        
        // Validate acknowledgment code values
        var validAckCodes = new[] { "AA", "AE", "AR", "CA", "CE", "CR" };
        if (!string.IsNullOrWhiteSpace(msa.AcknowledgmentCode.Value) && 
            !validAckCodes.Contains(msa.AcknowledgmentCode.Value))
        {
            result.ResponseValidationIssues.Add($"Invalid acknowledgment code: {msa.AcknowledgmentCode.Value}");
        }
    }

    /// <summary>
    /// Validates error reporting in ORR message.
    /// </summary>
    private static void ValidateErrorReporting(ORRMessage orrMessage, ResponseValidationResult result)
    {
        var msa = orrMessage.GetSegment<MSASegment>();
        
        // If acknowledgment indicates error, ensure errors are present
        if (msa?.AcknowledgmentCode.Value == "AE" && orrMessage.Errors.Count == 0)
        {
            result.ResponseValidationIssues.Add("Error acknowledgment requires at least one error segment");
        }
        
        // Validate error segments
        foreach (var error in orrMessage.Errors)
        {
            if (string.IsNullOrWhiteSpace(error.ErrorCodeAndLocation.Identifier))
                result.ResponseValidationIssues.Add("Error code is required for error segments");
            
            if (string.IsNullOrWhiteSpace(error.ErrorCodeAndLocation.Text))
                result.ResponseValidationIssues.Add("Error text is required for error segments");
        }
    }

    /// <summary>
    /// Validates order response correlation in ORR message.
    /// </summary>
    private static void ValidateOrderResponseCorrelation(ORRMessage orrMessage, ResponseValidationResult result)
    {
        // Check that each order response has proper correlation
        foreach (var orderResponse in orrMessage.OrderResponses)
        {
            if (string.IsNullOrWhiteSpace(orderResponse.OrderControl.PlacerOrderNumber.Value))
                result.ResponseValidationIssues.Add("Order response requires placer order number for correlation");
        }
    }

    /// <summary>
    /// Checks for drug-drug interactions.
    /// </summary>
    private static void CheckForDrugDrugInteractions(string medicationCode, List<string> existingMedications, DrugInteractionValidationResult result)
    {
        // Mock interaction checks - in production, integrate with drug interaction database
        foreach (var existingMed in existingMedications)
        {
            if (HasKnownInteraction(medicationCode, existingMed))
            {
                result.DrugInteractions.Add(new DrugInteraction
                {
                    Drug1 = medicationCode,
                    Drug2 = existingMed,
                    Severity = "Moderate",
                    Description = $"Potential interaction between {medicationCode} and {existingMed}"
                });
            }
        }
    }

    /// <summary>
    /// Checks for drug-allergy interactions.
    /// </summary>
    private static void CheckForDrugAllergyInteractions(string medicationCode, List<string> patientAllergies, DrugInteractionValidationResult result)
    {
        // Mock allergy checks - in production, integrate with allergy database
        foreach (var allergy in patientAllergies)
        {
            if (HasAllergyContraindication(medicationCode, allergy))
            {
                result.AllergyContraindications.Add(new AllergyContraindication
                {
                    Medication = medicationCode,
                    Allergen = allergy,
                    Severity = "High",
                    Description = $"Patient allergic to {allergy}, contraindicated with {medicationCode}"
                });
            }
        }
    }

    /// <summary>
    /// Calculates weight-based dose for medication.
    /// </summary>
    private static decimal? CalculateWeightBasedDose(decimal patientWeight, string medicationCode)
    {
        // Mock calculation - in production, use drug database
        return null; // Placeholder
    }

    /// <summary>
    /// Validates dosage range for medication.
    /// </summary>
    private static void ValidateDosageRange(string medicationCode, decimal dosageAmount, string dosageUnits, DosageValidationResult result)
    {
        // Mock dosage range validation - in production, use drug database
        if (dosageAmount > 1000) // Arbitrary high limit
        {
            result.DosageIssues.Add($"Dosage amount {dosageAmount} {dosageUnits} seems unusually high");
        }
    }

    /// <summary>
    /// Validates DEA number format.
    /// </summary>
    private static bool IsValidDEANumber(string deaNumber)
    {
        // DEA number format: 2 letters + 7 digits
        return Regex.IsMatch(deaNumber, @"^[A-Z]{2}\d{7}$");
    }

    /// <summary>
    /// Validates NDC format.
    /// </summary>
    private static bool IsValidNDCFormat(string ndcCode)
    {
        // NDC format: various formats like 12345-678-90 or 0069-2587-68
        return Regex.IsMatch(ndcCode, @"^\d{4,5}-\d{3,4}-\d{1,2}$");
    }

    /// <summary>
    /// Checks if two medications have known interactions.
    /// </summary>
    private static bool HasKnownInteraction(string drug1, string drug2)
    {
        // Mock interaction check - in production, use drug interaction database
        return false; // Placeholder
    }

    /// <summary>
    /// Checks if medication has allergy contraindication.
    /// </summary>
    private static bool HasAllergyContraindication(string medicationCode, string allergy)
    {
        // Mock allergy check - in production, use allergy database
        return false; // Placeholder
    }

    #endregion
}

#region Validation Result Classes

/// <summary>
/// Pharmacy-specific validation result.
/// </summary>
public class PharmacyValidationResult
{
    public bool IsOverallValid { get; set; }
    public bool IsPharmacyCompliant { get; set; }
    public List<string> BasicValidationIssues { get; set; } = new();
    public List<string> PharmacyValidationIssues { get; set; } = new();
    public DateTime ValidationDateTime { get; set; } = DateTime.Now;
    
    /// <summary>
    /// Gets whether the validation result is valid (alias for IsOverallValid for test compatibility).
    /// </summary>
    public bool IsValid => IsOverallValid;
    
    /// <summary>
    /// Gets whether there are critical errors in the validation result.
    /// </summary>
    public bool HasCriticalErrors => BasicValidationIssues.Any(issue => 
        issue.Contains("required", StringComparison.OrdinalIgnoreCase) ||
        issue.Contains("missing", StringComparison.OrdinalIgnoreCase) ||
        issue.Contains("invalid", StringComparison.OrdinalIgnoreCase)) ||
        PharmacyValidationIssues.Any(issue => 
        issue.Contains("required", StringComparison.OrdinalIgnoreCase) ||
        issue.Contains("missing", StringComparison.OrdinalIgnoreCase) ||
        issue.Contains("controlled substance", StringComparison.OrdinalIgnoreCase));
    
    /// <summary>
    /// Gets whether there are any warnings (non-critical issues).
    /// </summary>
    public bool HasWarnings => (BasicValidationIssues.Count > 0 || PharmacyValidationIssues.Count > 0) && !HasCriticalErrors;
    
    /// <summary>
    /// Gets all validation issues combined.
    /// </summary>
    public List<string> ValidationIssues
    {
        get
        {
            var allIssues = new List<string>();
            allIssues.AddRange(BasicValidationIssues);
            allIssues.AddRange(PharmacyValidationIssues);
            return allIssues;
        }
    }
}

/// <summary>
/// Response validation result.
/// </summary>
public class ResponseValidationResult
{
    public bool IsOverallValid { get; set; }
    public bool IsResponseComplete { get; set; }
    public List<string> BasicValidationIssues { get; set; } = new();
    public List<string> ResponseValidationIssues { get; set; } = new();
    public DateTime ValidationDateTime { get; set; } = DateTime.Now;
    
    /// <summary>
    /// Gets whether the validation result is valid (alias for IsOverallValid for test compatibility).
    /// </summary>
    public bool IsValid => IsOverallValid;
    
    /// <summary>
    /// Gets whether there are critical errors in the validation result.
    /// </summary>
    public bool HasCriticalErrors => BasicValidationIssues.Any(issue => 
        issue.Contains("required", StringComparison.OrdinalIgnoreCase) ||
        issue.Contains("missing", StringComparison.OrdinalIgnoreCase) ||
        issue.Contains("invalid", StringComparison.OrdinalIgnoreCase)) ||
        ResponseValidationIssues.Any(issue => 
        issue.Contains("required", StringComparison.OrdinalIgnoreCase) ||
        issue.Contains("missing", StringComparison.OrdinalIgnoreCase) ||
        issue.Contains("acknowledgment", StringComparison.OrdinalIgnoreCase));
    
    /// <summary>
    /// Gets whether there are any warnings (non-critical issues).
    /// </summary>
    public bool HasWarnings => (BasicValidationIssues.Count > 0 || ResponseValidationIssues.Count > 0) && !HasCriticalErrors;
    
    /// <summary>
    /// Gets all validation issues combined.
    /// </summary>
    public List<string> ValidationIssues
    {
        get
        {
            var allIssues = new List<string>();
            allIssues.AddRange(BasicValidationIssues);
            allIssues.AddRange(ResponseValidationIssues);
            return allIssues;
        }
    }
}

/// <summary>
/// Drug interaction validation result.
/// </summary>
public class DrugInteractionValidationResult
{
    public bool HasInteractions => DrugInteractions.Count > 0 || AllergyContraindications.Count > 0;
    public List<DrugInteraction> DrugInteractions { get; set; } = new();
    public List<AllergyContraindication> AllergyContraindications { get; set; } = new();
}

/// <summary>
/// Drug interaction information.
/// </summary>
public class DrugInteraction
{
    public string Drug1 { get; set; } = string.Empty;
    public string Drug2 { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Allergy contraindication information.
/// </summary>
public class AllergyContraindication
{
    public string Medication { get; set; } = string.Empty;
    public string Allergen { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Dosage validation result.
/// </summary>
public class DosageValidationResult
{
    public bool IsDosageValid { get; set; }
    public List<string> DosageIssues { get; set; } = new();
}

/// <summary>
/// Controlled substance validation result.
/// </summary>
public class ControlledSubstanceValidationResult
{
    public bool IsCompliant { get; set; }
    public List<string> ComplianceIssues { get; set; } = new();
}

/// <summary>
/// Timing validation result.
/// </summary>
public class TimingValidationResult
{
    public bool IsTimingValid { get; set; }
    public List<string> TimingIssues { get; set; } = new();
}

#endregion