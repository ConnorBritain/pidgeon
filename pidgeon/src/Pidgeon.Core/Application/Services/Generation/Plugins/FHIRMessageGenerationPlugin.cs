// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Pidgeon.Core.Application.Interfaces.Generation;
using Pidgeon.Core.Application.Interfaces.Standards;
using Pidgeon.Core.Generation;

namespace Pidgeon.Core.Application.Services.Generation.Plugins;

/// <summary>
/// FHIR-specific message generation plugin using hybrid approach.
/// Handles FHIR resource types with healthcare workflow organization.
/// </summary>
internal class FHIRMessageGenerationPlugin : IMessageGenerationPlugin
{
    private readonly Pidgeon.Core.Generation.IGenerationService _domainGenerationService;
    private readonly IStandardPluginRegistry _pluginRegistry;

    public string StandardName => "fhir";

    public FHIRMessageGenerationPlugin(
        Pidgeon.Core.Generation.IGenerationService domainGenerationService,
        IStandardPluginRegistry pluginRegistry)
    {
        _domainGenerationService = domainGenerationService;
        _pluginRegistry = pluginRegistry;
    }

    /// <summary>
    /// FHIR resource types with rich healthcare context.
    /// Organized by clinical workflow and resource relationships.
    /// </summary>
    private static readonly HashSet<string> _resourceTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        // === Foundation Resources (Core FHIR Infrastructure) ===
        "Patient",             // Patient Demographics - Core patient demographic and administrative information
        "Person",              // Person Registry - Administrative person record across multiple roles
        "Practitioner",        // Healthcare Provider - Individual healthcare provider (doctor, nurse, therapist)
        "PractitionerRole",    // Provider Role - Specific role/specialty of provider at organization
        "Organization",        // Healthcare Organization - Hospital, clinic, practice, or healthcare facility
        "Location",            // Physical Location - Physical place where services are provided

        // === Clinical Workflow Resources (Patient Care Journey) ===
        "Encounter",           // Patient Visit - Patient interaction with healthcare system (visit, admission)
        "EpisodeOfCare",       // Care Episode - Series of related encounters for condition management
        "Appointment",         // Scheduled Visit - Booking of healthcare service for patient
        "AppointmentResponse", // Appointment Confirmation - Response to appointment request
        "Schedule",            // Provider Schedule - Availability periods for appointments
        "Slot",                // Available Time - Specific time slot available for booking

        // === Clinical Assessment Resources (Observations & Findings) ===
        "Observation",         // Clinical Observation - Measurements, lab results, vital signs, assessments
        "DiagnosticReport",    // Diagnostic Results - Findings and interpretation of tests/studies
        "Condition",           // Patient Condition - Clinical conditions, problems, diagnoses
        "Procedure",           // Performed Procedure - Actions performed on/for patients
        "FamilyMemberHistory", // Family History - Health conditions in patient's family
        "RiskAssessment",      // Risk Evaluation - Assessment of patient health risks

        // === Medication Management Resources (Pharmacy Workflow) ===
        "Medication",              // Medication Definition - Drug/medication product information
        "MedicationKnowledge",     // Drug Information - Detailed pharmaceutical knowledge about medication
        "MedicationRequest",       // Prescription Order - Order for medication to be dispensed/administered
        "MedicationDispense",      // Medication Dispensing - Supply of medication from pharmacy
        "MedicationAdministration", // Medication Given - Record of medication administered to patient
        "MedicationStatement",     // Medication History - Patient's medication usage history

        // === Care Coordination Resources (Treatment Planning) ===
        "CarePlan",        // Treatment Plan - Comprehensive plan for patient care
        "CareTeam",        // Care Team - Group of providers collaborating on patient care
        "Goal",            // Treatment Goal - Desired outcomes for patient care
        "ServiceRequest",  // Service Order - Request for procedure, diagnostic, or referral
        "Task",            // Care Task - Work item to be completed in patient care

        // === Specimen & Diagnostics Resources (Laboratory Workflow) ===
        "Specimen",      // Laboratory Specimen - Sample collected from patient for testing
        "ImagingStudy",  // Imaging Examination - DICOM imaging study (X-ray, CT, MRI)
        "Media",         // Medical Media - Photo, video, or audio recording
        "BodyStructure", // Body Structure - Anatomical location or structure

        // === Healthcare Devices Resources (Medical Equipment) ===
        "Device",           // Medical Device - Instrument/equipment used in healthcare
        "DeviceRequest",    // Device Order - Request for medical device/equipment
        "DeviceUseStatement", // Device Usage - Record of medical device usage

        // === Administrative Resources (Healthcare Operations) ===
        "Account",             // Financial Account - Billing account for healthcare services
        "Coverage",            // Insurance Coverage - Patient's insurance/payment coverage
        "ExplanationOfBenefit", // Insurance Claim - Insurance claim adjudication
        "Claim",               // Billing Claim - Request for payment for services
        "Invoice",             // Healthcare Invoice - Bill for healthcare services

        // === Research & Quality Resources (Healthcare Analytics) ===
        "ResearchStudy",   // Clinical Study - Research study or clinical trial
        "ResearchSubject", // Study Participant - Patient enrolled in research study
        "Measure",         // Quality Measure - Clinical quality measurement definition
        "MeasureReport",   // Quality Report - Results of quality measure evaluation

        // === Communication Resources (Information Exchange) ===
        "Communication",        // Healthcare Communication - Record of communication between parties
        "CommunicationRequest", // Communication Order - Request for communication/notification
        "DocumentReference",    // Clinical Document - Reference to clinical document
        "DocumentManifest",     // Document Collection - Collection of related documents

        // === Consent & Legal Resources (Privacy & Authorization) ===
        "Consent",    // Patient Consent - Permission for treatment/data sharing
        "Contract",   // Healthcare Contract - Legal agreement related to healthcare
        "Provenance", // Information Source - Record of information origin and changes
        "AuditEvent", // Access Audit - Record of system access and data usage

        // === Bundle Resources (Data Exchange) ===
        "Bundle",          // Resource Collection - Collection of FHIR resources for exchange
        "MessageHeader",   // Message Header - Header for FHIR messaging
        "OperationOutcome" // Operation Result - Information about operation execution
    };

    public bool CanHandleMessageType(string messageType)
    {
        return !string.IsNullOrWhiteSpace(messageType) && 
               _resourceTypes.Contains(messageType);
    }

    public async Task<Result<IReadOnlyList<string>>> GenerateMessagesAsync(string messageType, int count, GenerationOptions? options = null)
    {
        try
        {
            var messages = new List<string>();
            var generationOptions = options ?? new GenerationOptions();
            
            for (int i = 0; i < count; i++)
            {
                var result = await GenerateSingleResourceAsync(messageType, generationOptions);
                
                if (!result.IsSuccess)
                    return Result<IReadOnlyList<string>>.Failure(result.Error);
                    
                messages.Add(result.Value);
            }
            
            return Result<IReadOnlyList<string>>.Success(messages);
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<string>>.Failure($"FHIR resource generation failed: {ex.Message}");
        }
    }

    public IReadOnlyList<string> GetSupportedMessageTypes()
    {
        return _resourceTypes.OrderBy(x => x).ToList();
    }

    public string GetUnsupportedMessageTypeError(string messageType)
    {
        var suggestions = new List<string>();

        // Common user mistakes with FHIR-specific guidance
        var commonSuggestions = messageType.ToLowerInvariant() switch
        {
            "adt^a01" or "adt" => "FHIR doesn't use HL7 v2 format. Use 'Encounter' for patient visits or 'Patient' for demographics",
            "rde^o11" or "rde" => "FHIR doesn't use HL7 format. Use 'MedicationRequest' for prescriptions",
            "oru^r01" or "oru" => "FHIR doesn't use HL7 format. Use 'Observation' for lab results or 'DiagnosticReport' for reports",
            "prescription" => "FHIR uses 'MedicationRequest' (order) or 'MedicationDispense' (supply) for prescriptions",
            "lab" or "labs" => "FHIR uses 'Observation' for individual lab results or 'DiagnosticReport' for lab reports",
            "visit" => "FHIR uses 'Encounter' for patient visits and healthcare interactions",
            "appointment" => "FHIR has 'Appointment' resource - you're close! Try 'Appointment'",
            "order" => "FHIR uses 'ServiceRequest' for orders, or 'MedicationRequest' for medication orders",
            "result" or "results" => "FHIR uses 'Observation' for individual results or 'DiagnosticReport' for complete reports",
            "document" => "FHIR uses 'DocumentReference' for clinical documents",
            _ => null
        };

        if (commonSuggestions != null)
        {
            suggestions.Add(commonSuggestions);
        }

        // Find similar resource names
        var similarResources = _resourceTypes
            .Where(r => r.StartsWith(messageType, StringComparison.OrdinalIgnoreCase) ||
                       messageType.StartsWith(r, StringComparison.OrdinalIgnoreCase))
            .Take(3)
            .ToList();

        if (similarResources.Any())
        {
            suggestions.Add($"Did you mean: {string.Join(", ", similarResources)}?");
        }

        var errorMessage = $"FHIR standard doesn't support resource type: {messageType}";
        if (suggestions.Any())
        {
            errorMessage += "\n\nSuggestions:\n" + string.Join("\n", suggestions.Select(s => $"  • {s}"));
        }
        else
        {
            var commonResources = new[] { "Patient", "Encounter", "Observation", "MedicationRequest", "DiagnosticReport" };
            errorMessage += $"\n\nCommon FHIR resources:\n  • {string.Join("\n  • ", commonResources)}";
        }

        return errorMessage;
    }

    /// <summary>
    /// Central routing logic - delegates to workflow-specific generation methods.
    /// </summary>
    private async Task<Result<string>> GenerateSingleResourceAsync(string resourceType, GenerationOptions options)
    {
        return resourceType.ToLowerInvariant() switch
        {
            // Foundation Resources
            "patient" => await GeneratePatientResourceAsync(options),
            "practitioner" => await GeneratePractitionerResourceAsync(options),
            "practitionerrole" => await GeneratePractitionerRoleResourceAsync(options),
            "organization" => await GenerateOrganizationResourceAsync(options),
            "location" => await GenerateLocationResourceAsync(options),

            // Clinical Workflow
            "encounter" => await GenerateEncounterResourceAsync(options),
            "episodeofcare" => await GenerateEpisodeOfCareResourceAsync(options),
            "appointment" => await GenerateAppointmentResourceAsync(options),

            // Clinical Assessment
            "observation" => await GenerateObservationResourceAsync(options),
            "diagnosticreport" => await GenerateDiagnosticReportResourceAsync(options),
            "condition" => await GenerateConditionResourceAsync(options),
            "procedure" => await GenerateProcedureResourceAsync(options),

            // Medication Management
            "medication" => await GenerateMedicationResourceAsync(options),
            "medicationrequest" => await GenerateMedicationRequestResourceAsync(options),
            "medicationdispense" => await GenerateMedicationDispenseResourceAsync(options),
            "medicationadministration" => await GenerateMedicationAdministrationResourceAsync(options),

            // Care Coordination
            "careplan" => await GenerateCarePlanResourceAsync(options),
            "careteam" => await GenerateCareTeamResourceAsync(options),
            "servicerequest" => await GenerateServiceRequestResourceAsync(options),

            // Administrative
            "account" => await GenerateAccountResourceAsync(options),
            "coverage" => await GenerateCoverageResourceAsync(options),

            // Data Exchange
            "bundle" => await GenerateBundleResourceAsync(options),
            "documentreference" => await GenerateDocumentReferenceResourceAsync(options),

            _ => Result<string>.Failure(GetUnsupportedMessageTypeError(resourceType))
        };
    }

    // === Resource Generation Methods ===

    private async Task<Result<string>> GeneratePatientResourceAsync(GenerationOptions options)
    {
        var patientResult = _domainGenerationService.GeneratePatient(options);
        if (!patientResult.IsSuccess)
            return Result<string>.Failure(patientResult.Error);
            
        var patient = patientResult.Value;
        return Result<string>.Success($"FHIR Patient: {patient.Name.DisplayName}, DOB: {patient.BirthDate:yyyy-MM-dd}, Gender: {patient.Gender}, MRN: {patient.MedicalRecordNumber}");
    }

    private async Task<Result<string>> GeneratePractitionerResourceAsync(GenerationOptions options)
    {
        var providerResult = _domainGenerationService.GenerateProvider(options);
        if (!providerResult.IsSuccess)
            return Result<string>.Failure(providerResult.Error);
            
        var provider = providerResult.Value;
        return Result<string>.Success($"FHIR Practitioner: Dr. {provider.Name.DisplayName}, Specialty: {provider.Specialty}, License: {provider.LicenseNumber}");
    }

    private async Task<Result<string>> GeneratePractitionerRoleResourceAsync(GenerationOptions options)
    {
        var providerResult = _domainGenerationService.GenerateProvider(options);
        if (!providerResult.IsSuccess)
            return Result<string>.Failure(providerResult.Error);
            
        var provider = providerResult.Value;
        return Result<string>.Success($"FHIR PractitionerRole: {provider.Name.DisplayName} as {provider.Specialty} at Healthcare Facility");
    }

    private async Task<Result<string>> GenerateOrganizationResourceAsync(GenerationOptions options)
    {
        return Result<string>.Success($"FHIR Organization: General Hospital, Type: Healthcare Provider, Status: Active");
    }

    private async Task<Result<string>> GenerateLocationResourceAsync(GenerationOptions options)
    {
        var locations = new[] { "Emergency Department", "ICU", "Medical/Surgical Unit", "Outpatient Clinic", "Operating Room" };
        var random = new Random(options.Seed ?? Environment.TickCount);
        var location = locations[random.Next(locations.Length)];
        return Result<string>.Success($"FHIR Location: {location}, Status: Active, Mode: Instance");
    }

    private async Task<Result<string>> GenerateEncounterResourceAsync(GenerationOptions options)
    {
        var encounterResult = _domainGenerationService.GenerateEncounter(options);
        if (!encounterResult.IsSuccess)
            return Result<string>.Failure(encounterResult.Error);
            
        var encounter = encounterResult.Value;
        return Result<string>.Success($"FHIR Encounter: {encounter.Type} for {encounter.Patient.Name.DisplayName} at {encounter.Location}, Period: {encounter.StartTime:yyyy-MM-dd HH:mm} - {encounter.EndTime?.ToString("yyyy-MM-dd HH:mm") ?? "ongoing"}");
    }

    private async Task<Result<string>> GenerateEpisodeOfCareResourceAsync(GenerationOptions options)
    {
        var patientResult = _domainGenerationService.GeneratePatient(options);
        if (!patientResult.IsSuccess)
            return Result<string>.Failure(patientResult.Error);
            
        var patient = patientResult.Value;
        var conditions = new[] { "Diabetes Management", "Hypertension Care", "Cancer Treatment", "Cardiac Rehabilitation" };
        var random = new Random(options.Seed ?? Environment.TickCount);
        var condition = conditions[random.Next(conditions.Length)];
        return Result<string>.Success($"FHIR EpisodeOfCare: {condition} for {patient.Name.DisplayName}, Status: Active");
    }

    private async Task<Result<string>> GenerateAppointmentResourceAsync(GenerationOptions options)
    {
        var patientResult = _domainGenerationService.GeneratePatient(options);
        if (!patientResult.IsSuccess)
            return Result<string>.Failure(patientResult.Error);
            
        var patient = patientResult.Value;
        var appointmentTypes = new[] { "Annual Physical", "Follow-up", "Consultation", "Procedure", "Lab Draw" };
        var random = new Random(options.Seed ?? Environment.TickCount);
        var appointmentType = appointmentTypes[random.Next(appointmentTypes.Length)];
        var appointmentTime = DateTime.Now.AddDays(random.Next(1, 30));
        return Result<string>.Success($"FHIR Appointment: {appointmentType} for {patient.Name.DisplayName}, Scheduled: {appointmentTime:yyyy-MM-dd HH:mm}, Status: Booked");
    }

    private async Task<Result<string>> GenerateObservationResourceAsync(GenerationOptions options)
    {
        var patientResult = _domainGenerationService.GeneratePatient(options);
        if (!patientResult.IsSuccess)
            return Result<string>.Failure(patientResult.Error);
            
        var patient = patientResult.Value;
        var observations = new[] 
        { 
            ("Blood Pressure", "120/80 mmHg"), 
            ("Heart Rate", "72 bpm"), 
            ("Temperature", "98.6 °F"), 
            ("Glucose", "95 mg/dL"),
            ("Hemoglobin A1c", "5.4%")
        };
        var random = new Random(options.Seed ?? Environment.TickCount);
        var (obsType, value) = observations[random.Next(observations.Length)];
        return Result<string>.Success($"FHIR Observation: {obsType} = {value} for {patient.Name.DisplayName}, Status: Final");
    }

    private async Task<Result<string>> GenerateDiagnosticReportResourceAsync(GenerationOptions options)
    {
        var patientResult = _domainGenerationService.GeneratePatient(options);
        if (!patientResult.IsSuccess)
            return Result<string>.Failure(patientResult.Error);
            
        var patient = patientResult.Value;
        var reports = new[] { "Complete Blood Count", "Basic Metabolic Panel", "Chest X-Ray", "Lipid Panel", "Urinalysis" };
        var random = new Random(options.Seed ?? Environment.TickCount);
        var report = reports[random.Next(reports.Length)];
        return Result<string>.Success($"FHIR DiagnosticReport: {report} for {patient.Name.DisplayName}, Status: Final, Conclusion: Normal findings");
    }

    private async Task<Result<string>> GenerateConditionResourceAsync(GenerationOptions options)
    {
        var patientResult = _domainGenerationService.GeneratePatient(options);
        if (!patientResult.IsSuccess)
            return Result<string>.Failure(patientResult.Error);
            
        var patient = patientResult.Value;
        var conditions = new[] { "Type 2 Diabetes Mellitus", "Essential Hypertension", "Hyperlipidemia", "Chronic Kidney Disease", "Asthma" };
        var random = new Random(options.Seed ?? Environment.TickCount);
        var condition = conditions[random.Next(conditions.Length)];
        return Result<string>.Success($"FHIR Condition: {condition} for {patient.Name.DisplayName}, Clinical Status: Active, Category: Problem List Item");
    }

    private async Task<Result<string>> GenerateProcedureResourceAsync(GenerationOptions options)
    {
        var patientResult = _domainGenerationService.GeneratePatient(options);
        if (!patientResult.IsSuccess)
            return Result<string>.Failure(patientResult.Error);
            
        var patient = patientResult.Value;
        var procedures = new[] { "Appendectomy", "Colonoscopy", "Cardiac Catheterization", "Knee Arthroscopy", "Cataract Surgery" };
        var random = new Random(options.Seed ?? Environment.TickCount);
        var procedure = procedures[random.Next(procedures.Length)];
        return Result<string>.Success($"FHIR Procedure: {procedure} performed on {patient.Name.DisplayName}, Status: Completed");
    }

    private async Task<Result<string>> GenerateMedicationResourceAsync(GenerationOptions options)
    {
        var medicationResult = _domainGenerationService.GenerateMedication(options);
        if (!medicationResult.IsSuccess)
            return Result<string>.Failure(medicationResult.Error);
            
        var medication = medicationResult.Value;
        return Result<string>.Success($"FHIR Medication: {medication.DisplayName} ({medication.GenericName}), Form: {medication.DosageForm}, Strength: {medication.Strength}");
    }

    private async Task<Result<string>> GenerateMedicationRequestResourceAsync(GenerationOptions options)
    {
        var prescriptionResult = _domainGenerationService.GeneratePrescription(options);
        if (!prescriptionResult.IsSuccess)
            return Result<string>.Failure(prescriptionResult.Error);
            
        var prescription = prescriptionResult.Value;
        return Result<string>.Success($"FHIR MedicationRequest: {prescription.Medication.DisplayName} {prescription.Dosage.Dose} {prescription.Dosage.DoseUnit} {prescription.Dosage.Frequency} for {prescription.Patient.Name.DisplayName}, Status: Active");
    }

    private async Task<Result<string>> GenerateMedicationDispenseResourceAsync(GenerationOptions options)
    {
        var prescriptionResult = _domainGenerationService.GeneratePrescription(options);
        if (!prescriptionResult.IsSuccess)
            return Result<string>.Failure(prescriptionResult.Error);
            
        var prescription = prescriptionResult.Value;
        return Result<string>.Success($"FHIR MedicationDispense: {prescription.Medication.DisplayName} dispensed to {prescription.Patient.Name.DisplayName}, Quantity: {prescription.Dosage.Quantity}, Status: Completed");
    }

    private async Task<Result<string>> GenerateMedicationAdministrationResourceAsync(GenerationOptions options)
    {
        var prescriptionResult = _domainGenerationService.GeneratePrescription(options);
        if (!prescriptionResult.IsSuccess)
            return Result<string>.Failure(prescriptionResult.Error);
            
        var prescription = prescriptionResult.Value;
        return Result<string>.Success($"FHIR MedicationAdministration: {prescription.Medication.DisplayName} {prescription.Dosage.Dose} {prescription.Dosage.DoseUnit} administered to {prescription.Patient.Name.DisplayName}, Status: Completed");
    }

    private async Task<Result<string>> GenerateCarePlanResourceAsync(GenerationOptions options)
    {
        var patientResult = _domainGenerationService.GeneratePatient(options);
        if (!patientResult.IsSuccess)
            return Result<string>.Failure(patientResult.Error);
            
        var patient = patientResult.Value;
        var carePlans = new[] { "Diabetes Management Plan", "Cardiac Rehabilitation Program", "Post-Surgical Recovery Plan", "Chronic Pain Management" };
        var random = new Random(options.Seed ?? Environment.TickCount);
        var carePlan = carePlans[random.Next(carePlans.Length)];
        return Result<string>.Success($"FHIR CarePlan: {carePlan} for {patient.Name.DisplayName}, Status: Active");
    }

    private async Task<Result<string>> GenerateCareTeamResourceAsync(GenerationOptions options)
    {
        var patientResult = _domainGenerationService.GeneratePatient(options);
        if (!patientResult.IsSuccess)
            return Result<string>.Failure(patientResult.Error);
            
        var patient = patientResult.Value;
        return Result<string>.Success($"FHIR CareTeam: Multidisciplinary team for {patient.Name.DisplayName} - Primary Care Physician, Specialist, Nurse, Social Worker, Status: Active");
    }

    private async Task<Result<string>> GenerateServiceRequestResourceAsync(GenerationOptions options)
    {
        var patientResult = _domainGenerationService.GeneratePatient(options);
        if (!patientResult.IsSuccess)
            return Result<string>.Failure(patientResult.Error);
            
        var patient = patientResult.Value;
        var services = new[] { "MRI Brain", "Echocardiogram", "Physical Therapy", "Cardiology Consultation", "Mammogram" };
        var random = new Random(options.Seed ?? Environment.TickCount);
        var service = services[random.Next(services.Length)];
        return Result<string>.Success($"FHIR ServiceRequest: {service} ordered for {patient.Name.DisplayName}, Status: Active, Priority: Routine");
    }

    private async Task<Result<string>> GenerateAccountResourceAsync(GenerationOptions options)
    {
        var patientResult = _domainGenerationService.GeneratePatient(options);
        if (!patientResult.IsSuccess)
            return Result<string>.Failure(patientResult.Error);
            
        var patient = patientResult.Value;
        return Result<string>.Success($"FHIR Account: Billing account for {patient.Name.DisplayName}, Status: Active, Type: Patient Account");
    }

    private async Task<Result<string>> GenerateCoverageResourceAsync(GenerationOptions options)
    {
        var patientResult = _domainGenerationService.GeneratePatient(options);
        if (!patientResult.IsSuccess)
            return Result<string>.Failure(patientResult.Error);
            
        var patient = patientResult.Value;
        var insurers = new[] { "Blue Cross Blue Shield", "Aetna", "Cigna", "UnitedHealthcare", "Medicare" };
        var random = new Random(options.Seed ?? Environment.TickCount);
        var insurer = insurers[random.Next(insurers.Length)];
        return Result<string>.Success($"FHIR Coverage: {insurer} insurance for {patient.Name.DisplayName}, Status: Active, Type: Medical");
    }

    private async Task<Result<string>> GenerateBundleResourceAsync(GenerationOptions options)
    {
        return Result<string>.Success($"FHIR Bundle: Collection of related resources, Type: Collection, Total: 5 resources");
    }

    private async Task<Result<string>> GenerateDocumentReferenceResourceAsync(GenerationOptions options)
    {
        var patientResult = _domainGenerationService.GeneratePatient(options);
        if (!patientResult.IsSuccess)
            return Result<string>.Failure(patientResult.Error);
            
        var patient = patientResult.Value;
        var docTypes = new[] { "Progress Note", "Discharge Summary", "Consultation Report", "Lab Report", "Radiology Report" };
        var random = new Random(options.Seed ?? Environment.TickCount);
        var docType = docTypes[random.Next(docTypes.Length)];
        return Result<string>.Success($"FHIR DocumentReference: {docType} for {patient.Name.DisplayName}, Status: Current, Format: PDF");
    }
}