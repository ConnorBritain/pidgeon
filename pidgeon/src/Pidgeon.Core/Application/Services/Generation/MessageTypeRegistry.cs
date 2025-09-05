// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System.Text.RegularExpressions;

namespace Pidgeon.Core.Application.Services.Generation;

/// <summary>
/// Registry for mapping message types to their healthcare standards.
/// Enables smart inference: pidgeon generate ADT^A01 (HL7 inferred from message type pattern).
/// </summary>
public class MessageTypeRegistry
{
    private static readonly Dictionary<string, string> _exactMatches = new(StringComparer.OrdinalIgnoreCase)
    {
        // HL7 v2 Messages - ADT (Admit/Discharge/Transfer)
        ["ADT^A01"] = "hl7", // Admit/visit notification
        ["ADT^A02"] = "hl7", // Transfer a patient
        ["ADT^A03"] = "hl7", // Discharge/end visit
        ["ADT^A04"] = "hl7", // Register a patient
        ["ADT^A05"] = "hl7", // Pre-admit a patient
        ["ADT^A06"] = "hl7", // Change an outpatient to an inpatient
        ["ADT^A07"] = "hl7", // Change an inpatient to an outpatient
        ["ADT^A08"] = "hl7", // Update patient information
        ["ADT^A09"] = "hl7", // Patient departing - tracking
        ["ADT^A10"] = "hl7", // Patient arriving - tracking
        ["ADT^A11"] = "hl7", // Cancel admit/visit notification
        ["ADT^A12"] = "hl7", // Cancel transfer
        ["ADT^A13"] = "hl7", // Cancel discharge/end visit
        ["ADT^A14"] = "hl7", // Pending admit
        ["ADT^A15"] = "hl7", // Pending transfer
        ["ADT^A16"] = "hl7", // Pending discharge
        
        // HL7 v2 Messages - ORU (Observation Result)
        ["ORU^R01"] = "hl7", // Unsolicited transmission of an observation message
        ["ORU^R03"] = "hl7", // Display oriented results, query/unsolicited update
        ["ORU^R04"] = "hl7", // Response to query; transmission of requested observation
        
        // HL7 v2 Messages - ORM/RDE (Order Entry)
        ["ORM^O01"] = "hl7", // Order message
        ["ORM^O02"] = "hl7", // Order response (general order acknowledgment)
        ["ORM^O03"] = "hl7", // Diet order
        ["RDE^O11"] = "hl7", // Pharmacy/treatment encoded order
        ["RDE^O25"] = "hl7", // Pharmacy/treatment refill authorization request
        
        // HL7 v2 Messages - SIU (Scheduling)
        ["SIU^S12"] = "hl7", // New appointment booking
        ["SIU^S13"] = "hl7", // Appointment rescheduling
        ["SIU^S14"] = "hl7", // Appointment modification
        ["SIU^S15"] = "hl7", // Appointment cancellation
        
        // HL7 v2 Messages - MDM (Medical Document Management)
        ["MDM^T02"] = "hl7", // Original document notification
        ["MDM^T04"] = "hl7", // Document edit notification
        ["MDM^T06"] = "hl7", // Document addendum notification
        ["MDM^T08"] = "hl7", // Document edit notification
        
        // HL7 v2 Messages - DFT (Detailed Financial Transaction)
        ["DFT^P03"] = "hl7", // Post detail financial transaction
        ["DFT^P11"] = "hl7", // Post detail financial transaction - expanded
        
        // HL7 v2 Messages - BAR (Add/Change Billing Account) 
        ["BAR^P01"] = "hl7", // Add patient accounts
        ["BAR^P02"] = "hl7", // Purge patient accounts
        ["BAR^P05"] = "hl7", // Update account
        ["BAR^P06"] = "hl7", // End account
        
        // HL7 v2 Messages - VXU (Vaccination Update)
        ["VXU^V04"] = "hl7", // Vaccination record update
        
        // HL7 v2 Messages - QRY (Query)
        ["QRY^A19"] = "hl7", // Patient query
        ["QRY^Q02"] = "hl7", // Query for results of previous query
        
        // FHIR R4 Resources - Core Patient/Demographics
        ["Patient"] = "fhir",        // Patient demographics and identifiers
        ["Person"] = "fhir",         // Generic person (not necessarily a patient)
        ["RelatedPerson"] = "fhir",  // Person related to the patient (emergency contact, etc.)
        
        // FHIR R4 Resources - Clinical Observations
        ["Observation"] = "fhir",       // Clinical observations and vital signs
        ["DiagnosticReport"] = "fhir",  // Lab results, imaging reports
        ["Condition"] = "fhir",         // Diagnoses, problems, concerns
        ["AllergyIntolerance"] = "fhir", // Allergies and adverse reactions
        ["FamilyMemberHistory"] = "fhir", // Family history and genetic information
        
        // FHIR R4 Resources - Medications & Procedures  
        ["MedicationRequest"] = "fhir",      // Prescription orders
        ["MedicationAdministration"] = "fhir", // Record of medication given to patient
        ["MedicationDispense"] = "fhir",     // Dispensing of medication by pharmacy
        ["MedicationStatement"] = "fhir",    // Patient's use of medication
        ["Procedure"] = "fhir",              // Procedures performed on patient
        ["ServiceRequest"] = "fhir",         // Orders for services/procedures
        ["ImagingStudy"] = "fhir",           // DICOM imaging studies
        
        // FHIR R4 Resources - Care Management
        ["Encounter"] = "fhir",      // Patient visits, admissions, episodes
        ["EpisodeOfCare"] = "fhir",  // Care delivery over time period
        ["CareTeam"] = "fhir",       // Care team participants
        ["CarePlan"] = "fhir",       // Care plans and treatment protocols
        ["Goal"] = "fhir",           // Patient care goals
        ["RiskAssessment"] = "fhir", // Risk assessments and predictions
        
        // FHIR R4 Resources - Administrative
        ["Organization"] = "fhir",     // Healthcare organizations
        ["Location"] = "fhir",         // Physical locations
        ["Practitioner"] = "fhir",     // Healthcare providers
        ["PractitionerRole"] = "fhir", // Provider roles and specialties
        ["HealthcareService"] = "fhir", // Services offered by organization
        
        // FHIR R4 Resources - Infrastructure
        ["Bundle"] = "fhir",           // Collection of resources
        ["Composition"] = "fhir",      // Clinical documents
        ["DocumentReference"] = "fhir", // Document metadata and links
        ["Device"] = "fhir",           // Medical devices
        ["DeviceRequest"] = "fhir",    // Device use requests
        ["Immunization"] = "fhir",     // Vaccination records
        
        // FHIR R4 Resources - Scheduling & Workflow
        ["Appointment"] = "fhir",         // Scheduled appointments
        ["AppointmentResponse"] = "fhir", // Appointment responses/confirmations
        ["Schedule"] = "fhir",            // Provider/service schedules
        ["Slot"] = "fhir",                // Available appointment slots
        ["Task"] = "fhir",                // Workflow tasks
        ["Communication"] = "fhir",       // Patient communications
        
        // NCPDP SCRIPT Standard - Core Transactions
        ["NewRx"] = "ncpdp",              // New prescription
        ["RxChangeRequest"] = "ncpdp",    // Request to change prescription
        ["RxChangeResponse"] = "ncpdp",   // Response to prescription change request
        ["CancelRx"] = "ncpdp",           // Cancel prescription request
        ["CancelRxResponse"] = "ncpdp",   // Response to prescription cancellation
        ["RxFill"] = "ncpdp",             // Prescription fill notification
        ["RxHistoryRequest"] = "ncpdp",   // Request prescription history
        ["RxHistoryResponse"] = "ncpdp",  // Prescription history data
        ["Verify"] = "ncpdp",             // Verification message
        ["Error"] = "ncpdp",              // Error notification
        ["Status"] = "ncpdp",             // Status notification
        
        // NCPDP SCRIPT Standard - Prior Authorization
        ["PAInitiationRequest"] = "ncpdp",  // Prior authorization request
        ["PAInitiationResponse"] = "ncpdp", // Prior authorization response
        
        // NCPDP SCRIPT Standard - Formulary & Benefits
        ["GetMessage"] = "ncpdp",         // Retrieve message request
        ["FormularyRequest"] = "ncpdp",   // Formulary information request
        ["FormularyResponse"] = "ncpdp"   // Formulary information response
    };

    private static readonly Regex _hl7Pattern = new(@"^[A-Z]{3}\^[A-Z]\d{2}$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Infers the healthcare standard from a message type (case-insensitive).
    /// </summary>
    /// <param name="messageType">Message type (e.g., ADT^A01, Patient, NewRx, adt^a01, patient, newrx)</param>
    /// <returns>Standard name (hl7, fhir, ncpdp) or null if cannot be inferred</returns>
    public static string? InferStandard(string messageType)
    {
        if (string.IsNullOrWhiteSpace(messageType))
            return null;

        // Try exact match first (case-insensitive dictionary handles this)
        if (_exactMatches.TryGetValue(messageType, out var standard))
            return standard;

        // Try pattern matching for HL7 (case-insensitive regex handles custom message types)
        if (_hl7Pattern.IsMatch(messageType))
            return "hl7";

        // No inference possible
        return null;
    }

    /// <summary>
    /// Validates that a message type is supported for the given standard.
    /// </summary>
    /// <param name="messageType">Message type to validate</param>
    /// <param name="standard">Target standard</param>
    /// <returns>True if the combination is valid</returns>
    public static bool IsValidForStandard(string messageType, string standard)
    {
        if (string.IsNullOrWhiteSpace(messageType) || string.IsNullOrWhiteSpace(standard))
            return false;

        var inferredStandard = InferStandard(messageType);
        return string.Equals(inferredStandard, standard, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets all supported message types for a standard.
    /// </summary>
    /// <param name="standard">Standard name (hl7, fhir, ncpdp)</param>
    /// <returns>List of supported message types</returns>
    public static IEnumerable<string> GetMessageTypesForStandard(string standard)
    {
        if (string.IsNullOrWhiteSpace(standard))
            return Enumerable.Empty<string>();

        return _exactMatches
            .Where(kvp => string.Equals(kvp.Value, standard, StringComparison.OrdinalIgnoreCase))
            .Select(kvp => kvp.Key)
            .OrderBy(type => type);
    }

    /// <summary>
    /// Gets all supported standards.
    /// </summary>
    /// <returns>List of supported standards</returns>
    public static IEnumerable<string> GetSupportedStandards()
    {
        return _exactMatches.Values.Distinct().OrderBy(s => s);
    }
}