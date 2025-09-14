// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Pidgeon.Core.Application.Interfaces.Generation;
using Pidgeon.Core.Application.Interfaces.Standards;
using Pidgeon.Core.Application.DTOs;
using Pidgeon.Core.Generation;
using Pidgeon.Core.Standards;
using Pidgeon.Core.Domain.Clinical.Entities;
using Pidgeon.Core.Infrastructure.Standards.HL7.v23;

namespace Pidgeon.Core.Application.Services.Generation.Plugins;

/// <summary>
/// HL7-specific message generation plugin using hybrid approach.
/// Combines rich healthcare context with clean workflow-based organization.
/// </summary>
internal class HL7MessageGenerationPlugin : IMessageGenerationPlugin
{
    private readonly Pidgeon.Core.Generation.IGenerationService _domainGenerationService;
    private readonly IStandardPluginRegistry _pluginRegistry;
    private readonly IHL7MessageFactory _hl7MessageFactory;

    public string StandardName => "hl7";

    public HL7MessageGenerationPlugin(
        Pidgeon.Core.Generation.IGenerationService domainGenerationService,
        IStandardPluginRegistry pluginRegistry,
        IHL7MessageFactory hl7MessageFactory)
    {
        _domainGenerationService = domainGenerationService;
        _pluginRegistry = pluginRegistry;
        _hl7MessageFactory = hl7MessageFactory;
    }

    /// <summary>
    /// HL7 message types with rich healthcare context.
    /// Organized by clinical workflow families for better understanding.
    /// </summary>
    private static readonly HashSet<string> _messageTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        // === ADT - Admit/Discharge/Transfer (Patient Movement) ===
        "ADT^A01", // Patient Admission - Notifies all systems when a patient is admitted to the facility
        "ADT^A02", // Patient Transfer - Notifies systems when patient moves between units/rooms
        "ADT^A03", // Patient Discharge - Notifies all systems when patient is discharged from facility
        "ADT^A04", // Patient Registration - Registers outpatient for services without admission
        "ADT^A05", // Pre-Admission - Pre-registers patient for future admission
        "ADT^A08", // Patient Information Update - Updates patient demographics or visit information
        "ADT^A11", // Cancel Admit - Cancels a previous admission notification
        "ADT^A12", // Cancel Transfer - Cancels a previous transfer notification
        "ADT^A13", // Cancel Discharge - Cancels a previous discharge notification
        "ADT",     // Generic ADT Message - Admit/Discharge/Transfer message (specify trigger event)

        // === ORM - Order Management (Clinical Orders) ===
        "ORM^O01", // General Order - Places new orders for lab tests, medications, procedures
        "ORM^O02", // Order Response - Confirms receipt and processing of orders
        "ORM^O03", // Diet Order - Specific order for dietary/nutrition requirements
        "ORM",     // Generic Order Message - Order management message (specify trigger event)

        // === ORU - Observation Results (Test Results/Reports) ===
        "ORU^R01", // Unsolicited Observation - Lab results, vital signs, diagnostic reports
        "ORU^R02", // Query Response - Results sent in response to a query
        "ORU^R03", // Display Response - Results formatted for display purposes
        "ORU^R04", // Response to Query/Display - Combined query response and display data
        "ORU",     // Generic Result Message - Observation result message (specify trigger event)

        // === RDE - Pharmacy Orders (Medication Management) ===
        "RDE^O11", // Pharmacy/Treatment Encoded Order - Detailed pharmacy order with dosing instructions
        "RDE^O25", // Refill Authorization Request - Request for medication refill authorization
        "RDE",     // Generic Pharmacy Order - Pharmacy order message (specify trigger event)

        // === RGV - Pharmacy Give (Medication Administration) ===
        "RGV^O15", // Pharmacy/Treatment Give - Records medication administration to patient
        "RGV",     // Generic Pharmacy Give - Medication administration message

        // === RAS - Pharmacy Administration (Medication Status) ===
        "RAS^O17", // Pharmacy/Treatment Administration - Pharmacy administration and status updates
        "RAS",     // Generic Administration Message - Pharmacy administration status

        // === SIU - Scheduling (Appointment Management) ===
        "SIU^S12", // New Appointment Booking - Creates new patient appointment
        "SIU^S13", // Appointment Rescheduling - Modifies existing appointment time/date
        "SIU^S14", // Appointment Modification - Updates appointment details
        "SIU^S15", // Appointment Cancellation - Cancels existing patient appointment
        "SIU^S17", // Appointment Deletion - Removes appointment from system
        "SIU",     // Generic Scheduling Message - Scheduling information message

        // === MDM - Medical Document Management (Clinical Documentation) ===
        "MDM^T02", // Original Document Notification - New clinical document created
        "MDM^T04", // Document Edit Notification - Existing document has been modified
        "MDM^T06", // Document Addendum Notification - Addendum added to existing document
        "MDM^T08", // Document Edit Completion - Document editing process completed
        "MDM^T10", // Document Replacement - Document replaced with new version
        "MDM",     // Generic Document Message - Medical document management message

        // === QBP/RSP - Query/Response (Data Requests) ===
        "QBP^Q11", // Query by Parameter - Request for specific patient/clinical data
        "QBP^Q21", // Find Candidates Query - Search for patients matching criteria
        "QBP^Q22", // Find Personnel Query - Search for staff/provider information
        "RSP^K11", // Segment Pattern Response - Response to query with requested data
        "RSP^K21", // Find Candidates Response - Patient search results
        "RSP^K22", // Find Personnel Response - Staff search results

        // === ACK - Acknowledgment (System Communication) ===
        "ACK",     // General Acknowledgment - Confirms receipt and processing status of messages

        // === BAR - Add/Change Billing (Financial) ===
        "BAR^P01", // Add Patient Accounts - Creates new billing account for patient
        "BAR^P02", // Purge Patient Accounts - Removes patient billing account
        "BAR^P05", // Update Account - Modifies existing patient account information
        "BAR"      // Generic Billing Message - Billing account management message
    };

    public bool CanHandleMessageType(string messageType)
    {
        if (string.IsNullOrWhiteSpace(messageType))
            return false;

        // Check exact match first
        if (_messageTypes.Contains(messageType))
            return true;

        // Check base type (ADT^A01 -> ADT)
        var baseType = ExtractBaseMessageType(messageType);
        return _messageTypes.Contains(baseType);
    }

    public async Task<Result<IReadOnlyList<string>>> GenerateMessagesAsync(string messageType, int count, GenerationOptions? options = null)
    {
        try
        {
            var messages = new List<string>();
            var generationOptions = options ?? new GenerationOptions();
            
            for (int i = 0; i < count; i++)
            {
                var result = await GenerateSingleMessageAsync(messageType, generationOptions);
                
                if (!result.IsSuccess)
                    return Result<IReadOnlyList<string>>.Failure(result.Error);
                    
                messages.Add(result.Value);
            }
            
            return Result<IReadOnlyList<string>>.Success(messages);
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<string>>.Failure($"HL7 message generation failed: {ex.Message}");
        }
    }

    public IReadOnlyList<string> GetSupportedMessageTypes()
    {
        return _messageTypes.OrderBy(x => x).ToList();
    }

    public string GetUnsupportedMessageTypeError(string messageType)
    {
        var baseType = ExtractBaseMessageType(messageType);
        var suggestions = new List<string>();

        // Check for common HL7 patterns and provide intelligent suggestions
        if (messageType.Contains("^"))
        {
            var supportedForBase = _messageTypes.Where(mt => mt.StartsWith($"{baseType}^", StringComparison.OrdinalIgnoreCase))
                                                .Take(3)
                                                .ToList();
            
            if (supportedForBase.Any())
                suggestions.Add($"HL7 supports these {baseType} messages: {string.Join(", ", supportedForBase)}");
        }
        else
        {
            // Common user mistakes with helpful HL7-specific guidance
            var commonSuggestions = messageType.ToLowerInvariant() switch
            {
                "patient" => "HL7 doesn't have 'patient' messages. Use ADT^A01 (Admission), ADT^A08 (Update), or ADT^A03 (Discharge)",
                "prescription" or "medication" => "HL7 uses RDE^O11 (Pharmacy Order) or RGV^O15 (Medication Administration) for prescriptions",
                "lab" or "labs" or "results" => "HL7 uses ORU^R01 (Lab Results) or ORU^R03 (Display Results) for laboratory data",
                "order" or "orders" => "HL7 uses ORM^O01 (General Order) for clinical orders",
                "appointment" => "HL7 uses SIU^S12 (New Appointment) or SIU^S13 (Reschedule) for scheduling",
                "document" => "HL7 uses MDM^T02 (New Document) or MDM^T04 (Document Edit) for clinical documents",
                _ => null
            };

            if (commonSuggestions != null)
                suggestions.Add(commonSuggestions);
        }

        var errorMessage = $"HL7 standard doesn't support message type: {messageType}";
        if (suggestions.Any())
        {
            errorMessage += "\n\nSuggestions:\n" + string.Join("\n", suggestions.Select(s => $"  • {s}"));
        }
        else
        {
            var commonTypes = _messageTypes.Where(mt => mt.Contains("^"))
                                          .Take(5)
                                          .ToList();
            errorMessage += $"\n\nCommon HL7 message types:\n  • {string.Join("\n  • ", commonTypes)}";
        }

        return errorMessage;
    }

    /// <summary>
    /// Central routing logic - delegates to workflow-specific generation methods.
    /// </summary>
    private async Task<Result<string>> GenerateSingleMessageAsync(string messageType, GenerationOptions options)
    {
        var baseType = ExtractBaseMessageType(messageType);
        
        return baseType.ToUpperInvariant() switch
        {
            "ADT" => await GenerateAdmitDischargeTransferAsync(messageType, options),
            "ORM" => await GenerateOrderManagementAsync(messageType, options),
            "ORU" => await GenerateObservationResultsAsync(messageType, options),
            "RDE" => await GeneratePharmacyOrderAsync(messageType, options),
            "RGV" => await GeneratePharmacyGiveAsync(messageType, options),
            "RAS" => await GeneratePharmacyAdministrationAsync(messageType, options),
            "SIU" => await GenerateSchedulingAsync(messageType, options),
            "MDM" => await GenerateMedicalDocumentAsync(messageType, options),
            "QBP" => await GenerateQueryAsync(messageType, options),
            "RSP" => await GenerateQueryResponseAsync(messageType, options),
            "ACK" => await GenerateAcknowledgmentAsync(messageType, options),
            "BAR" => await GenerateBillingAsync(messageType, options),
            _ => Result<string>.Failure(GetUnsupportedMessageTypeError(messageType))
        };
    }

    // === Clinical Workflow Generation Methods ===

    /// <summary>
    /// ADT - Admit/Discharge/Transfer workflow
    /// Handles patient movement throughout healthcare facility.
    /// </summary>
    private async Task<Result<string>> GenerateAdmitDischargeTransferAsync(string messageType, GenerationOptions options)
    {
        var encounterResult = _domainGenerationService.GenerateEncounter(options);
        if (!encounterResult.IsSuccess)
            return Result<string>.Failure(encounterResult.Error);
            
        var encounter = encounterResult.Value;
        var patient = encounter.Patient ?? _domainGenerationService.GeneratePatient(options).Value;
        
        if (patient == null)
            return Result<string>.Failure("Patient generation failed for ADT message");

        // Use standards-compliant HL7 v2.3 message factory
        return messageType switch
        {
            "ADT^A01" => _hl7MessageFactory.GenerateADT_A01(patient, encounter, options),
            "ADT^A08" => _hl7MessageFactory.GenerateADT_A08(patient, encounter, options),
            "ADT^A03" => _hl7MessageFactory.GenerateADT_A03(patient, encounter, options),
            _ => Result<string>.Failure($"ADT message type {messageType} not yet implemented")
        };
    }

    /// <summary>
    /// ORM - Order Management workflow  
    /// Handles clinical orders (lab tests, procedures, medications).
    /// </summary>
    private async Task<Result<string>> GenerateOrderManagementAsync(string messageType, GenerationOptions options)
    {
        var prescriptionResult = _domainGenerationService.GeneratePrescription(options);
        if (!prescriptionResult.IsSuccess)
            return Result<string>.Failure(prescriptionResult.Error);
            
        var prescription = prescriptionResult.Value;
        return Result<string>.Success($"ORM Message: Clinical order for {prescription.Patient.Name.DisplayName} - {prescription.Medication.DisplayName} ({messageType})");
    }

    /// <summary>
    /// ORU - Observation Results workflow
    /// Handles lab results, vital signs, diagnostic reports.
    /// </summary>
    private async Task<Result<string>> GenerateObservationResultsAsync(string messageType, GenerationOptions options)
    {
        var patientResult = _domainGenerationService.GeneratePatient(options);
        if (!patientResult.IsSuccess)
            return Result<string>.Failure(patientResult.Error);
            
        var patient = patientResult.Value;
        
        // Generate realistic observation result using service
        var observationResult = _domainGenerationService.GenerateObservationResult(options);
        if (!observationResult.IsSuccess)
            return Result<string>.Failure(observationResult.Error);
            
        var observation = observationResult.Value;
        
        return messageType switch
        {
            "ORU^R01" => _hl7MessageFactory.GenerateORU_R01(patient, observation, options),
            _ => Result<string>.Failure($"ORU message type {messageType} not yet implemented")
        };
    }

    /// <summary>
    /// RDE - Pharmacy Order workflow
    /// Handles detailed pharmacy orders with dosing instructions.
    /// </summary>
    private async Task<Result<string>> GeneratePharmacyOrderAsync(string messageType, GenerationOptions options)
    {
        var prescriptionResult = _domainGenerationService.GeneratePrescription(options);
        if (!prescriptionResult.IsSuccess)
            return Result<string>.Failure(prescriptionResult.Error);
            
        var prescription = prescriptionResult.Value;
        
        return messageType switch
        {
            "RDE^O11" => _hl7MessageFactory.GenerateRDE_O11(prescription.Patient, prescription, options),
            _ => Result<string>.Failure($"RDE message type {messageType} not yet implemented")
        };
    }

    /// <summary>
    /// RGV - Pharmacy Give workflow
    /// Records medication administration to patients.
    /// </summary>
    private async Task<Result<string>> GeneratePharmacyGiveAsync(string messageType, GenerationOptions options)
    {
        var prescriptionResult = _domainGenerationService.GeneratePrescription(options);
        if (!prescriptionResult.IsSuccess)
            return Result<string>.Failure(prescriptionResult.Error);
            
        var prescription = prescriptionResult.Value;
        return Result<string>.Success($"RGV Message: Medication administered - {prescription.Medication.DisplayName} given to {prescription.Patient.Name.DisplayName} at {DateTime.Now:HH:mm} ({messageType})");
    }

    /// <summary>
    /// RAS - Pharmacy Administration workflow
    /// Handles pharmacy administration status updates.
    /// </summary>
    private async Task<Result<string>> GeneratePharmacyAdministrationAsync(string messageType, GenerationOptions options)
    {
        var prescriptionResult = _domainGenerationService.GeneratePrescription(options);
        if (!prescriptionResult.IsSuccess)
            return Result<string>.Failure(prescriptionResult.Error);
            
        var prescription = prescriptionResult.Value;
        return Result<string>.Success($"RAS Message: Pharmacy administration status - {prescription.Medication.DisplayName} for {prescription.Patient.Name.DisplayName} ({messageType})");
    }

    /// <summary>
    /// SIU - Scheduling workflow
    /// Handles appointment booking, rescheduling, cancellation.
    /// </summary>
    private async Task<Result<string>> GenerateSchedulingAsync(string messageType, GenerationOptions options)
    {
        var patientResult = _domainGenerationService.GeneratePatient(options);
        if (!patientResult.IsSuccess)
            return Result<string>.Failure(patientResult.Error);
            
        var patient = patientResult.Value;
        var appointmentTypes = new[] { "Annual Physical", "Follow-up Visit", "Lab Draw", "Specialist Consultation", "Preventive Care" };
        var random = new Random(options.Seed ?? Environment.TickCount);
        var appointmentType = appointmentTypes[random.Next(appointmentTypes.Length)];
        var appointmentTime = DateTime.Now.AddDays(random.Next(1, 30)).ToString("yyyy-MM-dd HH:mm");
        
        return Result<string>.Success($"SIU Message: {appointmentType} scheduled for {patient.Name.DisplayName} on {appointmentTime} ({messageType})");
    }

    /// <summary>
    /// MDM - Medical Document Management workflow
    /// Handles clinical document creation, editing, addendums.
    /// </summary>
    private async Task<Result<string>> GenerateMedicalDocumentAsync(string messageType, GenerationOptions options)
    {
        var patientResult = _domainGenerationService.GeneratePatient(options);
        if (!patientResult.IsSuccess)
            return Result<string>.Failure(patientResult.Error);
            
        var patient = patientResult.Value;
        var documentTypes = new[] { "Progress Note", "Discharge Summary", "Operative Report", "Consultation Note", "History & Physical" };
        var random = new Random(options.Seed ?? Environment.TickCount);
        var documentType = documentTypes[random.Next(documentTypes.Length)];
        
        return Result<string>.Success($"MDM Message: {documentType} created for {patient.Name.DisplayName} ({messageType})");
    }

    /// <summary>
    /// QBP - Query workflow
    /// Handles data requests and patient searches.
    /// </summary>
    private async Task<Result<string>> GenerateQueryAsync(string messageType, GenerationOptions options)
    {
        return Result<string>.Success($"QBP Message: Query request for patient data ({messageType})");
    }

    /// <summary>
    /// RSP - Query Response workflow  
    /// Handles responses to data queries.
    /// </summary>
    private async Task<Result<string>> GenerateQueryResponseAsync(string messageType, GenerationOptions options)
    {
        var patientResult = _domainGenerationService.GeneratePatient(options);
        if (!patientResult.IsSuccess)
            return Result<string>.Failure(patientResult.Error);
            
        var patient = patientResult.Value;
        return Result<string>.Success($"RSP Message: Query response - Found patient {patient.Name.DisplayName} (ID: {patient.Id}) ({messageType})");
    }

    /// <summary>
    /// ACK - Acknowledgment workflow
    /// System-to-system communication confirmations.
    /// </summary>
    private async Task<Result<string>> GenerateAcknowledgmentAsync(string messageType, GenerationOptions options)
    {
        return Result<string>.Success($"ACK Message: Application Accept - Message received and processed successfully ({messageType})");
    }

    /// <summary>
    /// BAR - Billing workflow
    /// Financial account management messages.
    /// </summary>
    private async Task<Result<string>> GenerateBillingAsync(string messageType, GenerationOptions options)
    {
        var patientResult = _domainGenerationService.GeneratePatient(options);
        if (!patientResult.IsSuccess)
            return Result<string>.Failure(patientResult.Error);
            
        var patient = patientResult.Value;
        return Result<string>.Success($"BAR Message: Billing account created for {patient.Name.DisplayName} ({messageType})");
    }

    // === Helper Methods ===

    private static string ExtractBaseMessageType(string messageType)
    {
        if (string.IsNullOrWhiteSpace(messageType))
            return messageType;
            
        // Handle HL7 v2 format: ADT^A01 -> ADT
        var caretIndex = messageType.IndexOf('^');
        return caretIndex > 0 ? messageType.Substring(0, caretIndex) : messageType;
    }

    private static MessageOptions CreateMessageOptions()
    {
        return new MessageOptions
        {
            Timestamp = DateTime.UtcNow,
            SendingApplication = "PIDGEON",
            ReceivingApplication = "UNKNOWN"
        };
    }

}