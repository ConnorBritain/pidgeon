// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Pidgeon.Core.Application.Interfaces.Generation;
using Pidgeon.Core.Application.Interfaces.Standards;
using Pidgeon.Core.Generation;

namespace Pidgeon.Core.Application.Services.Generation.Plugins;

/// <summary>
/// NCPDP-specific message generation plugin using hybrid approach.
/// Handles NCPDP SCRIPT transaction types for pharmacy workflows.
/// </summary>
internal class NCPDPMessageGenerationPlugin : IMessageGenerationPlugin
{
    private readonly Pidgeon.Core.Generation.IGenerationService _domainGenerationService;
    private readonly IStandardPluginRegistry _pluginRegistry;

    public string StandardName => "ncpdp";

    public NCPDPMessageGenerationPlugin(
        Pidgeon.Core.Generation.IGenerationService domainGenerationService,
        IStandardPluginRegistry pluginRegistry)
    {
        _domainGenerationService = domainGenerationService;
        _pluginRegistry = pluginRegistry;
    }

    /// <summary>
    /// NCPDP SCRIPT transaction types with rich healthcare context.
    /// Organized by pharmacy workflow and transaction purpose.
    /// </summary>
    private static readonly HashSet<string> _transactionTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        // === Core Prescription Workflow (Primary NCPDP Transactions) ===
        "NEWRX",         // New Prescription - Initial prescription order from prescriber to pharmacy
        "RXCHANGEREQUEST", // Prescription Change Request - Pharmacy requests changes to prescription
        "RXCHANGERESPONSE", // Prescription Change Response - Prescriber response to change request
        "REFILLREQUEST", // Refill Request - Pharmacy requests refill authorization
        "REFILLRESPONSE", // Refill Response - Prescriber approves/denies refill request
        "RXFILL",        // Prescription Fill - Pharmacy reports dispensing to prescriber
        "CANCELRX",      // Cancel Prescription - Cancel/void existing prescription order
        "CANCELRXRESPONSE", // Cancel Response - Acknowledgment of prescription cancellation

        // === Enhanced Prescription Management ===
        "RXHISTORYREQUEST",  // Prescription History Request - Request patient's medication history
        "RXHISTORYRESPONSE", // Prescription History Response - Patient medication history data
        "VERIFY",           // Prescription Verification - Verify prescription authenticity
        "STATUS",           // Status Update - General prescription status notification
        "ERROR",            // Error Notification - Report transaction processing errors
        "RESUPPLYREQUEST",  // Resupply Request - Request for medication resupply
        "RESUPPLYRESPONSE", // Resupply Response - Response to resupply request

        // === Specialty Pharmacy Transactions ===
        "PREDETERMINATION", // Prior Authorization - Insurance coverage determination
        "PRIORAUTH",       // Prior Authorization Request - Formal prior auth request
        "PRIORAUTHRESPONSE", // Prior Auth Response - Insurance decision on prior auth
        "BENEFITSCOORDINATION", // Benefits Coordination - Coordinate multiple insurance coverage
        "ELIGIBILITY",     // Insurance Eligibility - Verify patient insurance coverage
        "FORMULARY",       // Formulary Information - Drug formulary/coverage details

        // === Clinical Decision Support ===
        "DUR",            // Drug Utilization Review - Clinical appropriateness check
        "DRUGALERT",      // Drug Alert - Safety alerts and warnings
        "INTERACTION",    // Drug Interaction - Drug-drug interaction alerts
        "ALLERGY",        // Allergy Alert - Patient allergy warnings
        "CLINICALINFO",   // Clinical Information - Additional clinical context

        // === Administrative Transactions ===
        "DELIVERYRECEIPT", // Delivery Receipt - Confirm message delivery
        "ACKNOWLEDGMENT",  // General Acknowledgment - Confirm message processing
        "STRUCTURED",      // Structured Message - Complex multi-part message
        "FREEFORM",       // Free Form Message - Unstructured communication
        "RECERTIFICATION", // Recertification - Provider recertification request

        // === Reporting and Analytics ===
        "REPORTREQUEST",  // Report Request - Request for prescription reports
        "REPORTRESPONSE", // Report Response - Prescription reporting data
        "QUALITY",        // Quality Metrics - Quality measure reporting
        "AUDIT",          // Audit Information - Transaction audit trail
        "STATISTICS"      // Statistics - Usage and performance statistics
    };

    public bool CanHandleMessageType(string messageType)
    {
        return !string.IsNullOrWhiteSpace(messageType) && 
               _transactionTypes.Contains(messageType);
    }

    public async Task<Result<IReadOnlyList<string>>> GenerateMessagesAsync(string messageType, int count, GenerationOptions? options = null)
    {
        try
        {
            var messages = new List<string>();
            var generationOptions = options ?? new GenerationOptions();
            
            for (int i = 0; i < count; i++)
            {
                var result = await GenerateSingleTransactionAsync(messageType, generationOptions);
                
                if (!result.IsSuccess)
                    return Result<IReadOnlyList<string>>.Failure(result.Error);
                    
                messages.Add(result.Value);
            }
            
            return Result<IReadOnlyList<string>>.Success(messages);
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<string>>.Failure($"NCPDP transaction generation failed: {ex.Message}");
        }
    }

    public IReadOnlyList<string> GetSupportedMessageTypes()
    {
        return _transactionTypes.OrderBy(x => x).ToList();
    }

    public string GetUnsupportedMessageTypeError(string messageType)
    {
        var suggestions = new List<string>();

        // Common user mistakes with NCPDP-specific guidance
        var commonSuggestions = messageType.ToLowerInvariant() switch
        {
            "adt^a01" or "adt" => "NCPDP doesn't use HL7 v2 format. Use 'NEWRX' for new prescriptions",
            "rde^o11" or "rde" => "NCPDP doesn't use HL7 format. Use 'NEWRX' for prescription orders",
            "patient" => "NCPDP focuses on prescriptions. Use 'NEWRX' for new prescriptions or 'RXHISTORYREQUEST' for patient history",
            "prescription" => "NCPDP uses 'NEWRX' (new prescription), 'REFILLREQUEST' (refill), or 'RXFILL' (dispensing)",
            "medicationrequest" => "NCPDP doesn't use FHIR format. Use 'NEWRX' for prescription requests",
            "order" => "NCPDP uses 'NEWRX' for new prescription orders",
            "refill" => "NCPDP uses 'REFILLREQUEST' (request) or 'REFILLRESPONSE' (approval) for refills",
            "cancel" => "NCPDP uses 'CANCELRX' to cancel prescriptions",
            "fill" => "NCPDP uses 'RXFILL' to report prescription dispensing",
            "status" => "NCPDP has 'STATUS' transaction for prescription status updates",
            _ => null
        };

        if (commonSuggestions != null)
        {
            suggestions.Add(commonSuggestions);
        }

        // Find similar transaction names
        var similarTransactions = _transactionTypes
            .Where(t => t.StartsWith(messageType, StringComparison.OrdinalIgnoreCase) ||
                       messageType.StartsWith(t, StringComparison.OrdinalIgnoreCase) ||
                       t.Contains(messageType, StringComparison.OrdinalIgnoreCase))
            .Take(3)
            .ToList();

        if (similarTransactions.Any())
        {
            suggestions.Add($"Did you mean: {string.Join(", ", similarTransactions)}?");
        }

        var errorMessage = $"NCPDP standard doesn't support transaction type: {messageType}";
        if (suggestions.Any())
        {
            errorMessage += "\n\nSuggestions:\n" + string.Join("\n", suggestions.Select(s => $"  • {s}"));
        }
        else
        {
            var commonTransactions = new[] { "NEWRX", "RXFILL", "CANCELRX", "REFILLREQUEST", "RXHISTORYREQUEST" };
            errorMessage += $"\n\nCommon NCPDP transactions:\n  • {string.Join("\n  • ", commonTransactions)}";
        }

        return errorMessage;
    }

    /// <summary>
    /// Central routing logic - delegates to workflow-specific generation methods.
    /// </summary>
    private async Task<Result<string>> GenerateSingleTransactionAsync(string transactionType, GenerationOptions options)
    {
        return transactionType.ToUpperInvariant() switch
        {
            // Core Prescription Workflow
            "NEWRX" => await GenerateNewPrescriptionAsync(options),
            "RXCHANGEREQUEST" => await GenerateChangeRequestAsync(options),
            "RXCHANGERESPONSE" => await GenerateChangeResponseAsync(options),
            "REFILLREQUEST" => await GenerateRefillRequestAsync(options),
            "REFILLRESPONSE" => await GenerateRefillResponseAsync(options),
            "RXFILL" => await GenerateRxFillAsync(options),
            "CANCELRX" => await GenerateCancelRxAsync(options),
            "CANCELRXRESPONSE" => await GenerateCancelResponseAsync(options),

            // Enhanced Prescription Management
            "RXHISTORYREQUEST" => await GenerateHistoryRequestAsync(options),
            "RXHISTORYRESPONSE" => await GenerateHistoryResponseAsync(options),
            "VERIFY" => await GenerateVerificationAsync(options),
            "STATUS" => await GenerateStatusUpdateAsync(options),
            "ERROR" => await GenerateErrorNotificationAsync(options),

            // Specialty Pharmacy
            "PREDETERMINATION" => await GeneratePredeterminationAsync(options),
            "PRIORAUTH" => await GeneratePriorAuthAsync(options),
            "PRIORAUTHRESPONSE" => await GeneratePriorAuthResponseAsync(options),
            "ELIGIBILITY" => await GenerateEligibilityAsync(options),
            "FORMULARY" => await GenerateFormularyAsync(options),

            // Clinical Decision Support
            "DUR" => await GenerateDurReviewAsync(options),
            "DRUGALERT" => await GenerateDrugAlertAsync(options),
            "INTERACTION" => await GenerateInteractionAlertAsync(options),
            "ALLERGY" => await GenerateAllergyAlertAsync(options),
            "CLINICALINFO" => await GenerateClinicalInfoAsync(options),

            // Administrative
            "ACKNOWLEDGMENT" => await GenerateAcknowledgmentAsync(options),
            "STRUCTURED" => await GenerateStructuredMessageAsync(options),
            "FREEFORM" => await GenerateFreeFormMessageAsync(options),

            _ => Result<string>.Failure(GetUnsupportedMessageTypeError(transactionType))
        };
    }

    // === Core Prescription Workflow Methods ===

    private async Task<Result<string>> GenerateNewPrescriptionAsync(GenerationOptions options)
    {
        var prescriptionResult = _domainGenerationService.GeneratePrescription(options);
        if (!prescriptionResult.IsSuccess)
            return Result<string>.Failure(prescriptionResult.Error);
            
        var prescription = prescriptionResult.Value;
        return Result<string>.Success($"NCPDP NEWRX: New prescription for {prescription.Medication.DisplayName} {prescription.Dosage.Dose} {prescription.Dosage.DoseUnit} {prescription.Dosage.Frequency} - Patient: {prescription.Patient.Name.DisplayName}");
    }

    private async Task<Result<string>> GenerateChangeRequestAsync(GenerationOptions options)
    {
        var prescriptionResult = _domainGenerationService.GeneratePrescription(options);
        if (!prescriptionResult.IsSuccess)
            return Result<string>.Failure(prescriptionResult.Error);
            
        var prescription = prescriptionResult.Value;
        return Result<string>.Success($"NCPDP RXCHANGEREQUEST: Pharmacy requesting change to {prescription.Medication.DisplayName} for {prescription.Patient.Name.DisplayName} - Reason: Formulary alternative needed");
    }

    private async Task<Result<string>> GenerateChangeResponseAsync(GenerationOptions options)
    {
        var prescriptionResult = _domainGenerationService.GeneratePrescription(options);
        if (!prescriptionResult.IsSuccess)
            return Result<string>.Failure(prescriptionResult.Error);
            
        var prescription = prescriptionResult.Value;
        return Result<string>.Success($"NCPDP RXCHANGERESPONSE: Prescriber approves change to {prescription.Medication.DisplayName} for {prescription.Patient.Name.DisplayName}");
    }

    private async Task<Result<string>> GenerateRefillRequestAsync(GenerationOptions options)
    {
        var prescriptionResult = _domainGenerationService.GeneratePrescription(options);
        if (!prescriptionResult.IsSuccess)
            return Result<string>.Failure(prescriptionResult.Error);
            
        var prescription = prescriptionResult.Value;
        return Result<string>.Success($"NCPDP REFILLREQUEST: Refill requested for {prescription.Medication.DisplayName} - Patient: {prescription.Patient.Name.DisplayName}");
    }

    private async Task<Result<string>> GenerateRefillResponseAsync(GenerationOptions options)
    {
        var prescriptionResult = _domainGenerationService.GeneratePrescription(options);
        if (!prescriptionResult.IsSuccess)
            return Result<string>.Failure(prescriptionResult.Error);
            
        var prescription = prescriptionResult.Value;
        return Result<string>.Success($"NCPDP REFILLRESPONSE: Refill approved for {prescription.Medication.DisplayName} - Patient: {prescription.Patient.Name.DisplayName}");
    }

    private async Task<Result<string>> GenerateRxFillAsync(GenerationOptions options)
    {
        var prescriptionResult = _domainGenerationService.GeneratePrescription(options);
        if (!prescriptionResult.IsSuccess)
            return Result<string>.Failure(prescriptionResult.Error);
            
        var prescription = prescriptionResult.Value;
        return Result<string>.Success($"NCPDP RXFILL: {prescription.Medication.DisplayName} dispensed to {prescription.Patient.Name.DisplayName} - Quantity: {prescription.Dosage.Quantity}, Date: {DateTime.Now:yyyy-MM-dd}");
    }

    private async Task<Result<string>> GenerateCancelRxAsync(GenerationOptions options)
    {
        var prescriptionResult = _domainGenerationService.GeneratePrescription(options);
        if (!prescriptionResult.IsSuccess)
            return Result<string>.Failure(prescriptionResult.Error);
            
        var prescription = prescriptionResult.Value;
        return Result<string>.Success($"NCPDP CANCELRX: Prescription cancelled - {prescription.Medication.DisplayName} for {prescription.Patient.Name.DisplayName}");
    }

    private async Task<Result<string>> GenerateCancelResponseAsync(GenerationOptions options)
    {
        return Result<string>.Success($"NCPDP CANCELRXRESPONSE: Prescription cancellation acknowledged - Status: Cancelled");
    }

    // === Enhanced Prescription Management Methods ===

    private async Task<Result<string>> GenerateHistoryRequestAsync(GenerationOptions options)
    {
        var patientResult = _domainGenerationService.GeneratePatient(options);
        if (!patientResult.IsSuccess)
            return Result<string>.Failure(patientResult.Error);
            
        var patient = patientResult.Value;
        return Result<string>.Success($"NCPDP RXHISTORYREQUEST: Medication history requested for {patient.Name.DisplayName} (DOB: {patient.BirthDate:yyyy-MM-dd})");
    }

    private async Task<Result<string>> GenerateHistoryResponseAsync(GenerationOptions options)
    {
        var patientResult = _domainGenerationService.GeneratePatient(options);
        if (!patientResult.IsSuccess)
            return Result<string>.Failure(patientResult.Error);
            
        var patient = patientResult.Value;
        return Result<string>.Success($"NCPDP RXHISTORYRESPONSE: Medication history for {patient.Name.DisplayName} - 3 active prescriptions, last fill: {DateTime.Now.AddDays(-5):yyyy-MM-dd}");
    }

    private async Task<Result<string>> GenerateVerificationAsync(GenerationOptions options)
    {
        return Result<string>.Success($"NCPDP VERIFY: Prescription verification request - Status: Authentic");
    }

    private async Task<Result<string>> GenerateStatusUpdateAsync(GenerationOptions options)
    {
        var statuses = new[] { "Received", "In Progress", "Ready for Pickup", "Dispensed", "Partially Filled" };
        var random = new Random(options.Seed ?? Environment.TickCount);
        var status = statuses[random.Next(statuses.Length)];
        return Result<string>.Success($"NCPDP STATUS: Prescription status update - Current status: {status}");
    }

    private async Task<Result<string>> GenerateErrorNotificationAsync(GenerationOptions options)
    {
        var errors = new[] { "Invalid DEA Number", "Patient Not Found", "Drug Not Covered", "Prior Authorization Required", "Quantity Exceeded" };
        var random = new Random(options.Seed ?? Environment.TickCount);
        var error = errors[random.Next(errors.Length)];
        return Result<string>.Success($"NCPDP ERROR: Transaction error - {error}");
    }

    // === Specialty Pharmacy Methods ===

    private async Task<Result<string>> GeneratePredeterminationAsync(GenerationOptions options)
    {
        var prescriptionResult = _domainGenerationService.GeneratePrescription(options);
        if (!prescriptionResult.IsSuccess)
            return Result<string>.Failure(prescriptionResult.Error);
            
        var prescription = prescriptionResult.Value;
        return Result<string>.Success($"NCPDP PREDETERMINATION: Coverage check for {prescription.Medication.DisplayName} - Patient: {prescription.Patient.Name.DisplayName}, Result: Covered with $25 copay");
    }

    private async Task<Result<string>> GeneratePriorAuthAsync(GenerationOptions options)
    {
        var prescriptionResult = _domainGenerationService.GeneratePrescription(options);
        if (!prescriptionResult.IsSuccess)
            return Result<string>.Failure(prescriptionResult.Error);
            
        var prescription = prescriptionResult.Value;
        return Result<string>.Success($"NCPDP PRIORAUTH: Prior authorization request for {prescription.Medication.DisplayName} - Patient: {prescription.Patient.Name.DisplayName}");
    }

    private async Task<Result<string>> GeneratePriorAuthResponseAsync(GenerationOptions options)
    {
        var prescriptionResult = _domainGenerationService.GeneratePrescription(options);
        if (!prescriptionResult.IsSuccess)
            return Result<string>.Failure(prescriptionResult.Error);
            
        var prescription = prescriptionResult.Value;
        return Result<string>.Success($"NCPDP PRIORAUTHRESPONSE: Prior authorization approved for {prescription.Medication.DisplayName} - Valid through: {DateTime.Now.AddYears(1):yyyy-MM-dd}");
    }

    private async Task<Result<string>> GenerateEligibilityAsync(GenerationOptions options)
    {
        var patientResult = _domainGenerationService.GeneratePatient(options);
        if (!patientResult.IsSuccess)
            return Result<string>.Failure(patientResult.Error);
            
        var patient = patientResult.Value;
        return Result<string>.Success($"NCPDP ELIGIBILITY: Insurance eligibility verified for {patient.Name.DisplayName} - Active coverage, $15 copay tier");
    }

    private async Task<Result<string>> GenerateFormularyAsync(GenerationOptions options)
    {
        var medicationResult = _domainGenerationService.GenerateMedication(options);
        if (!medicationResult.IsSuccess)
            return Result<string>.Failure(medicationResult.Error);
            
        var medication = medicationResult.Value;
        return Result<string>.Success($"NCPDP FORMULARY: Formulary information for {medication.DisplayName} - Tier 2, Preferred brand, Prior auth required");
    }

    // === Clinical Decision Support Methods ===

    private async Task<Result<string>> GenerateDurReviewAsync(GenerationOptions options)
    {
        var prescriptionResult = _domainGenerationService.GeneratePrescription(options);
        if (!prescriptionResult.IsSuccess)
            return Result<string>.Failure(prescriptionResult.Error);
            
        var prescription = prescriptionResult.Value;
        return Result<string>.Success($"NCPDP DUR: Drug utilization review for {prescription.Medication.DisplayName} - No clinical issues detected");
    }

    private async Task<Result<string>> GenerateDrugAlertAsync(GenerationOptions options)
    {
        var medicationResult = _domainGenerationService.GenerateMedication(options);
        if (!medicationResult.IsSuccess)
            return Result<string>.Failure(medicationResult.Error);
            
        var medication = medicationResult.Value;
        return Result<string>.Success($"NCPDP DRUGALERT: Safety alert for {medication.DisplayName} - Monitor for signs of drowsiness");
    }

    private async Task<Result<string>> GenerateInteractionAlertAsync(GenerationOptions options)
    {
        var medicationResult = _domainGenerationService.GenerateMedication(options);
        if (!medicationResult.IsSuccess)
            return Result<string>.Failure(medicationResult.Error);
            
        var medication = medicationResult.Value;
        return Result<string>.Success($"NCPDP INTERACTION: Drug interaction alert - {medication.DisplayName} may interact with Warfarin, monitor INR levels");
    }

    private async Task<Result<string>> GenerateAllergyAlertAsync(GenerationOptions options)
    {
        var patientResult = _domainGenerationService.GeneratePatient(options);
        if (!patientResult.IsSuccess)
            return Result<string>.Failure(patientResult.Error);
            
        var patient = patientResult.Value;
        return Result<string>.Success($"NCPDP ALLERGY: Allergy alert for {patient.Name.DisplayName} - Patient allergic to Penicillin");
    }

    private async Task<Result<string>> GenerateClinicalInfoAsync(GenerationOptions options)
    {
        var patientResult = _domainGenerationService.GeneratePatient(options);
        if (!patientResult.IsSuccess)
            return Result<string>.Failure(patientResult.Error);
            
        var patient = patientResult.Value;
        return Result<string>.Success($"NCPDP CLINICALINFO: Clinical information for {patient.Name.DisplayName} - Diagnosis: Type 2 Diabetes, Renal function: Normal");
    }

    // === Administrative Methods ===

    private async Task<Result<string>> GenerateAcknowledgmentAsync(GenerationOptions options)
    {
        return Result<string>.Success($"NCPDP ACKNOWLEDGMENT: Message received and processed successfully - Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
    }

    private async Task<Result<string>> GenerateStructuredMessageAsync(GenerationOptions options)
    {
        return Result<string>.Success($"NCPDP STRUCTURED: Multi-part structured message - Contains prescription, patient, and insurance data");
    }

    private async Task<Result<string>> GenerateFreeFormMessageAsync(GenerationOptions options)
    {
        var messages = new[] 
        { 
            "Please call pharmacy for prescription pickup instructions",
            "Generic substitution available - please confirm",
            "Prescription ready for delivery - patient preferences noted",
            "Insurance requires step therapy documentation"
        };
        var random = new Random(options.Seed ?? Environment.TickCount);
        var message = messages[random.Next(messages.Length)];
        return Result<string>.Success($"NCPDP FREEFORM: {message}");
    }
}