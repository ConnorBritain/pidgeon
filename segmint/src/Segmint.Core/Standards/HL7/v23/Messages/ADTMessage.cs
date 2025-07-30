// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.


using Segmint.Core.HL7;
using Segmint.Core.Standards.HL7.v23.Segments;
using System.Collections.Generic;
using System.Linq;
using System;
namespace Segmint.Core.Standards.HL7.v23.Messages;

/// <summary>
/// Represents an HL7 ADT (Admit/Discharge/Transfer) message.
/// Used for patient admission, discharge, transfer, and registration events.
/// </summary>
public class ADTMessage : HL7Message
{
    private string _triggerEvent = "A01";

    /// <inheritdoc />
    public override string MessageType => "ADT";

    /// <inheritdoc />
    public override string TriggerEvent => _triggerEvent;

    /// <summary>
    /// Gets or sets the message header segment.
    /// </summary>
    public override MSHSegment? MessageHeader { get; set; }

    /// <summary>
    /// Gets or sets the event type segment.
    /// </summary>
    public EVNSegment? EventType { get; set; }

    /// <summary>
    /// Gets or sets the patient identification segment.
    /// </summary>
    public PIDSegment? PatientIdentification { get; set; }

    /// <summary>
    /// Gets or sets the patient visit segment.
    /// </summary>
    public PV1Segment? PatientVisit { get; set; }

    // Standard HL7 segment shortcuts using official segment IDs
    /// <summary>
    /// Gets the message header segment using standard HL7 segment ID.
    /// </summary>
    public MSHSegment? MSH => MessageHeader;

    /// <summary>
    /// Gets the event type segment using standard HL7 segment ID.
    /// </summary>
    public EVNSegment? EVN => EventType;

    /// <summary>
    /// Gets the patient identification segment using standard HL7 segment ID.
    /// </summary>
    public PIDSegment? PID => PatientIdentification;

    /// <summary>
    /// Gets the patient visit segment using standard HL7 segment ID.
    /// </summary>
    public PV1Segment? PV1 => PatientVisit;

    /// <summary>
    /// Initializes a new instance of the <see cref="ADTMessage"/> class.
    /// </summary>
    /// <param name="triggerEvent">The trigger event (default: A01).</param>
    public ADTMessage(string triggerEvent = "A01")
    {
        _triggerEvent = triggerEvent;
        InitializeSegments();
    }

    /// <summary>
    /// Initializes the required segments for an ADT message.
    /// </summary>
    private void InitializeSegments()
    {
        // Create required segments
        MessageHeader = new MSHSegment();
        EventType = new EVNSegment();
        PatientIdentification = new PIDSegment();
        PatientVisit = new PV1Segment();

        // Set up message header
        MessageHeader.SetMessageType(MessageType, TriggerEvent);
        MessageHeader.SetProcessingId(true); // P = Production
        MessageHeader.SetVersionId("2.3");

        // Set up event type
        EventType.SetEventTypeCode(TriggerEvent);
        EventType.SetRecordedDateTime(DateTime.Now);

        // Add segments to the message
        AddSegment(MessageHeader);
        AddSegment(EventType);
        AddSegment(PatientIdentification);
        AddSegment(PatientVisit);
    }

    /// <summary>
    /// Creates a new ADT message for patient admission (A01).
    /// </summary>
    /// <returns>A new <see cref="ADTMessage"/> instance.</returns>
    public static ADTMessage CreateAdmitPatient()
    {
        return new ADTMessage("A01");
    }

    /// <summary>
    /// Creates a new ADT message for patient transfer (A02).
    /// </summary>
    /// <returns>A new <see cref="ADTMessage"/> instance.</returns>
    public static ADTMessage CreateTransferPatient()
    {
        return new ADTMessage("A02");
    }

    /// <summary>
    /// Creates a new ADT message for patient discharge (A03).
    /// </summary>
    /// <returns>A new <see cref="ADTMessage"/> instance.</returns>
    public static ADTMessage CreateDischargePatient()
    {
        return new ADTMessage("A03");
    }

    /// <summary>
    /// Creates a new ADT message for patient registration (A04).
    /// </summary>
    /// <returns>A new <see cref="ADTMessage"/> instance.</returns>
    public static ADTMessage CreateRegisterPatient()
    {
        return new ADTMessage("A04");
    }

    /// <summary>
    /// Creates a new ADT message for patient pre-admission (A05).
    /// </summary>
    /// <returns>A new <see cref="ADTMessage"/> instance.</returns>
    public static ADTMessage CreatePreAdmitPatient()
    {
        return new ADTMessage("A05");
    }

    /// <summary>
    /// Creates a new ADT message for patient update (A08).
    /// </summary>
    /// <returns>A new <see cref="ADTMessage"/> instance.</returns>
    public static ADTMessage CreateUpdatePatient()
    {
        return new ADTMessage("A08");
    }

    /// <summary>
    /// Creates a new ADT message for patient cancel admit (A11).
    /// </summary>
    /// <returns>A new <see cref="ADTMessage"/> instance.</returns>
    public static ADTMessage CreateCancelAdmit()
    {
        return new ADTMessage("A11");
    }

    /// <summary>
    /// Creates a new ADT message for patient cancel discharge (A13).
    /// </summary>
    /// <returns>A new <see cref="ADTMessage"/> instance.</returns>
    public static ADTMessage CreateCancelDischarge()
    {
        return new ADTMessage("A13");
    }

    /// <summary>
    /// Sets the patient demographics information.
    /// </summary>
    /// <param name="patientId">The patient ID.</param>
    /// <param name="lastName">The patient's last name.</param>
    /// <param name="firstName">The patient's first name.</param>
    /// <param name="middleName">The patient's middle name (optional).</param>
    /// <param name="dateOfBirth">The patient's date of birth (optional).</param>
    /// <param name="gender">The patient's gender (optional).</param>
    /// <param name="ssn">The patient's social security number (optional).</param>
    public void SetPatientDemographics(string patientId, string lastName, string firstName, 
        string? middleName = null, DateTime? dateOfBirth = null, string? gender = null, string? ssn = null)
    {
        if (PatientIdentification == null)
            throw new InvalidOperationException("Patient identification segment is not initialized.");

        PatientIdentification.SetPatientId(patientId);
        PatientIdentification.SetPatientName(lastName, firstName, middleName);
        
        if (dateOfBirth.HasValue)
        {
            PatientIdentification.SetDateOfBirth(dateOfBirth.Value);
        }
        
        if (!string.IsNullOrEmpty(gender))
        {
            PatientIdentification.SetGender(gender);
        }
        
        if (!string.IsNullOrEmpty(ssn))
        {
            PatientIdentification.SetSocialSecurityNumber(ssn);
        }
    }

    /// <summary>
    /// Sets the patient address information.
    /// </summary>
    /// <param name="streetAddress">The street address.</param>
    /// <param name="city">The city.</param>
    /// <param name="state">The state.</param>
    /// <param name="zipCode">The ZIP code.</param>
    /// <param name="country">The country (optional).</param>
    public void SetPatientAddress(string streetAddress, string city, string state, string zipCode, string? country = null)
    {
        if (PatientIdentification == null)
            throw new InvalidOperationException("Patient identification segment is not initialized.");

        PatientIdentification.SetPatientAddress(streetAddress, city, state, zipCode, country);
    }

    /// <summary>
    /// Sets the patient visit information.
    /// </summary>
    /// <param name="patientClass">The patient class (E=Emergency, I=Inpatient, O=Outpatient, etc.).</param>
    /// <param name="assignedPatientLocation">The assigned patient location (optional).</param>
    /// <param name="attendingDoctor">The attending doctor (optional).</param>
    /// <param name="admissionType">The admission type (optional).</param>
    /// <param name="visitNumber">The visit number (optional).</param>
    public void SetPatientVisit(string patientClass, string? assignedPatientLocation = null, 
        string? attendingDoctor = null, string? admissionType = null, string? visitNumber = null)
    {
        if (PatientVisit == null)
            throw new InvalidOperationException("Patient visit segment is not initialized.");

        PatientVisit.SetPatientClass(patientClass);
        
        if (!string.IsNullOrEmpty(assignedPatientLocation))
        {
            PatientVisit.SetAssignedPatientLocation(assignedPatientLocation);
        }
        
        if (!string.IsNullOrEmpty(attendingDoctor))
        {
            var parts = attendingDoctor.Split(' ');
            if (parts.Length >= 2)
            {
                PatientVisit.SetAttendingDoctor(parts[0], parts[1]);
            }
            else
            {
                PatientVisit.SetAttendingDoctor(attendingDoctor);
            }
        }
        
        if (!string.IsNullOrEmpty(admissionType))
        {
            PatientVisit.SetAdmissionType(admissionType);
        }
        
        if (!string.IsNullOrEmpty(visitNumber))
        {
            PatientVisit.SetVisitNumber(visitNumber);
        }
    }

    /// <summary>
    /// Sets the admission date and time.
    /// </summary>
    /// <param name="admissionDateTime">The admission date and time.</param>
    public void SetAdmissionDateTime(DateTime admissionDateTime)
    {
        if (PatientVisit == null)
            throw new InvalidOperationException("Patient visit segment is not initialized.");

        PatientVisit.SetAdmitDateTime(admissionDateTime);
    }

    /// <summary>
    /// Sets the discharge date and time.
    /// </summary>
    /// <param name="dischargeDateTime">The discharge date and time.</param>
    public void SetDischargeDateTime(DateTime dischargeDateTime)
    {
        if (PatientVisit == null)
            throw new InvalidOperationException("Patient visit segment is not initialized.");

        PatientVisit.SetDischargeDateTime(dischargeDateTime);
    }

    /// <summary>
    /// Sets the sending application information.
    /// </summary>
    /// <param name="sendingApplication">The sending application name.</param>
    /// <param name="sendingFacility">The sending facility name.</param>
    public void SetSendingApplication(string sendingApplication, string? sendingFacility = null)
    {
        if (MessageHeader == null)
            throw new InvalidOperationException("Message header segment is not initialized.");

        MessageHeader.SetSendingApplication(sendingApplication);
        
        if (!string.IsNullOrEmpty(sendingFacility))
        {
            MessageHeader.SetSendingFacility(sendingFacility);
        }
    }

    /// <summary>
    /// Sets the receiving application information.
    /// </summary>
    /// <param name="receivingApplication">The receiving application name.</param>
    /// <param name="receivingFacility">The receiving facility name.</param>
    public void SetReceivingApplication(string receivingApplication, string? receivingFacility = null)
    {
        if (MessageHeader == null)
            throw new InvalidOperationException("Message header segment is not initialized.");

        MessageHeader.SetReceivingApplication(receivingApplication);
        
        if (!string.IsNullOrEmpty(receivingFacility))
        {
            MessageHeader.SetReceivingFacility(receivingFacility);
        }
    }

    /// <summary>
    /// Adds a note to the message.
    /// </summary>
    /// <param name="noteText">The note text.</param>
    /// <param name="noteType">The note type (P=Patient, L=Physician, etc.).</param>
    public void AddNote(string noteText, string noteType = "P")
    {
        var noteSegment = noteType switch
        {
            "P" => NTESegment.CreatePatientNote(noteText),
            "L" => NTESegment.CreatePhysicianNote(noteText),
            "N" => NTESegment.CreateNursingNote(noteText),
            _ => NTESegment.CreateGeneralNote(noteText, noteType)
        };
        
        AddSegment(noteSegment);
    }

    /// <summary>
    /// Gets the event type description.
    /// </summary>
    /// <returns>A human-readable description of the event type.</returns>
    public string GetEventTypeDescription()
    {
        return TriggerEvent switch
        {
            "A01" => "Admit Patient",
            "A02" => "Transfer Patient",
            "A03" => "Discharge Patient",
            "A04" => "Register Patient",
            "A05" => "Pre-Admit Patient",
            "A06" => "Change Outpatient to Inpatient",
            "A07" => "Change Inpatient to Outpatient",
            "A08" => "Update Patient Information",
            "A09" => "Patient Departing - Tracking",
            "A10" => "Patient Arriving - Tracking",
            "A11" => "Cancel Admit",
            "A12" => "Cancel Transfer",
            "A13" => "Cancel Discharge",
            "A14" => "Pending Admit",
            "A15" => "Pending Transfer",
            "A16" => "Pending Discharge",
            "A17" => "Swap Patients",
            "A18" => "Merge Patient Information",
            "A19" => "Patient Query",
            "A20" => "Bed Status Update",
            "A21" => "Patient Goes on Leave of Absence",
            "A22" => "Patient Returns from Leave of Absence",
            "A23" => "Delete Patient Record",
            "A24" => "Link Patient Information",
            "A25" => "Cancel Pending Discharge",
            "A26" => "Cancel Pending Transfer",
            "A27" => "Cancel Pending Admit",
            "A28" => "Add Person Information",
            "A29" => "Delete Person Information",
            "A30" => "Merge Person Information",
            "A31" => "Update Person Information",
            "A32" => "Cancel Patient Arriving - Tracking",
            "A33" => "Cancel Patient Departing - Tracking",
            "A34" => "Merge Patient Information - Patient ID Only",
            "A35" => "Merge Patient Information - Account Number Only",
            "A36" => "Merge Patient Information - Patient ID and Account Number",
            "A37" => "Unlink Patient Information",
            _ => TriggerEvent
        };
    }

    /// <summary>
    /// Gets a formatted display string for this message.
    /// </summary>
    /// <returns>A human-readable representation of the message.</returns>
    public string ToDisplayString()
    {
        var patientName = PatientIdentification?.PatientName?.DisplayName ?? "Unknown";
        var patientId = PatientIdentification?.PatientIdentifierList?.IdNumber ?? "Unknown";
        var eventDescription = GetEventTypeDescription();
        
        return $"ADT {eventDescription} - {patientName} (ID: {patientId})";
    }

    /// <summary>
    /// Creates a copy of this message.
    /// </summary>
    /// <returns>A new instance with the same segment values.</returns>
    public override HL7Message Clone()
    {
        var cloned = new ADTMessage(TriggerEvent);
        
        // Clear default segments
        cloned.ClearSegments();
        
        // Clone all segments
        foreach (var segment in this)
        {
            cloned.AddSegment(segment.Clone());
        }
        
        // Update references
        cloned.MessageHeader = cloned.GetSegment<MSHSegment>();
        cloned.EventType = cloned.GetSegment<EVNSegment>();
        cloned.PatientIdentification = cloned.GetSegment<PIDSegment>();
        cloned.PatientVisit = cloned.GetSegment<PV1Segment>();
        
        return cloned;
    }

    /// <inheritdoc />
    protected override void InitializeMessage()
    {
        InitializeSegments();
    }

    #region Pharmacy-Specific Methods

    /// <summary>
    /// Gets or sets the list of diagnosis segments for pharmacy context.
    /// Used for medication reconciliation and clinical decision support.
    /// </summary>
    public List<DG1Segment> Diagnoses { get; set; } = new List<DG1Segment>();

    /// <summary>
    /// Gets or sets the list of allergy segments for pharmacy safety.
    /// Critical for medication contraindication checking.
    /// </summary>
    public List<AL1Segment> Allergies { get; set; } = new List<AL1Segment>();

    /// <summary>
    /// Gets or sets the list of insurance segments for pharmacy billing.
    /// Used for medication coverage verification.
    /// </summary>
    public List<IN1Segment> Insurance { get; set; } = new List<IN1Segment>();

    /// <summary>
    /// Adds primary diagnosis for medication reconciliation context.
    /// </summary>
    /// <param name="icd10Code">ICD-10 diagnosis code</param>
    /// <param name="description">Diagnosis description</param>
    /// <param name="diagnosingClinician">Clinician who made diagnosis</param>
    public void AddPrimaryDiagnosis(string icd10Code, string description, string? diagnosingClinician = null)
    {
        var diagnosis = DG1Segment.CreatePrimaryICD10(icd10Code, description, diagnosingClinician);
        Diagnoses.Insert(0, diagnosis); // Primary diagnosis goes first
    }

    /// <summary>
    /// Adds secondary diagnosis for comprehensive clinical context.
    /// </summary>
    /// <param name="icd10Code">ICD-10 diagnosis code</param>
    /// <param name="description">Diagnosis description</param>
    /// <param name="priority">Diagnosis priority ranking</param>
    public void AddSecondaryDiagnosis(string icd10Code, string description, int? priority = null)
    {
        var setId = Diagnoses.Count + 1;
        var diagnosisPriority = priority ?? setId + 1; // Primary is 1, secondary starts at 2
        var diagnosis = DG1Segment.CreateSecondaryDiagnosis(setId, "ICD10", icd10Code, description, diagnosisPriority);
        Diagnoses.Add(diagnosis);
    }

    /// <summary>
    /// Adds medication allergy information for pharmacy safety screening.
    /// </summary>
    /// <param name="allergen">Allergen substance</param>
    /// <param name="allergyType">Type of allergy (DA=Drug Allergy, FA=Food Allergy)</param>
    /// <param name="severity">Severity (MI=Mild, MO=Moderate, SV=Severe)</param>
    /// <param name="reaction">Allergic reaction description</param>
    /// <param name="onsetDate">When allergy was first observed</param>
    public void AddMedicationAllergy(
        string allergen,
        string allergyType = "DA",
        string severity = "MO",
        string? reaction = null,
        DateTime? onsetDate = null)
    {
        var allergy = new AL1Segment();
        allergy.SetBasicAllergy(
            Allergies.Count + 1,
            allergyType,
            allergen,
            severity,
            reaction,
            onsetDate?.ToString("yyyyMMdd")
        );
        Allergies.Add(allergy);
    }

    /// <summary>
    /// Adds primary insurance information for medication coverage verification.
    /// </summary>
    /// <param name="planName">Insurance plan name</param>
    /// <param name="companyName">Insurance company name</param>
    /// <param name="memberId">Member ID</param>
    /// <param name="groupNumber">Group number</param>
    /// <param name="effectiveDate">Coverage effective date</param>
    /// <param name="expirationDate">Coverage expiration date</param>
    public void AddPrimaryInsurance(
        string planName,
        string companyName,
        string memberId,
        string? groupNumber = null,
        DateTime? effectiveDate = null,
        DateTime? expirationDate = null)
    {
        var insurance = IN1Segment.CreateBasicInsurance(1, planName, companyName, memberId, groupNumber);
        if (effectiveDate.HasValue || expirationDate.HasValue)
        {
            insurance.SetBasicInsurance(1, planName, planName, companyName, companyName, memberId, groupNumber, effectiveDate, expirationDate);
        }
        Insurance.Insert(0, insurance); // Primary insurance goes first
    }

    /// <summary>
    /// Creates a medication reconciliation discharge message (A03) with clinical context.
    /// Used when patient is discharged to trigger medication review in pharmacy systems.
    /// </summary>
    /// <param name="patientId">Patient identifier</param>
    /// <param name="lastName">Patient last name</param>
    /// <param name="firstName">Patient first name</param>
    /// <param name="dischargeLocation">Where patient is being discharged</param>
    /// <param name="attendingPhysician">Attending physician</param>
    /// <param name="primaryDiagnosis">Primary discharge diagnosis</param>
    /// <param name="dischargeDateTime">Discharge date and time</param>
    /// <returns>Configured ADT message for medication reconciliation</returns>
    public static ADTMessage CreateMedicationReconciliationDischarge(
        string patientId,
        string lastName,
        string firstName,
        string dischargeLocation,
        string attendingPhysician,
        (string code, string description) primaryDiagnosis,
        DateTime? dischargeDateTime = null)
    {
        var adt = CreateDischargePatient();
        
        // Set patient demographics
        adt.SetPatientDemographics(patientId, lastName, firstName);
        
        // Set visit information for discharge
        adt.SetPatientVisit("I", dischargeLocation, attendingPhysician); // Inpatient being discharged
        
        // Add primary diagnosis for medication context
        adt.AddPrimaryDiagnosis(primaryDiagnosis.code, primaryDiagnosis.description, attendingPhysician);
        
        // Set discharge timing
        if (adt.EventType != null)
        {
            adt.EventType.SetRecordedDateTime(dischargeDateTime ?? DateTime.Now);
        }
        
        return adt;
    }

    /// <summary>
    /// Creates a patient update message (A08) for pharmacy notification of changes.
    /// Used when patient information changes that might affect medication orders.
    /// </summary>
    /// <param name="patientId">Patient identifier</param>
    /// <param name="lastName">Updated patient last name</param>
    /// <param name="firstName">Updated patient first name</param>
    /// <param name="dateOfBirth">Updated date of birth</param>
    /// <param name="gender">Updated gender</param>
    /// <param name="updateReason">Reason for the update</param>
    /// <returns>Configured ADT message for patient update</returns>
    public static ADTMessage CreatePharmacyPatientUpdate(
        string patientId,
        string lastName,
        string firstName,
        DateTime? dateOfBirth = null,
        string? gender = null,
        string? updateReason = null)
    {
        var adt = CreateUpdatePatient();
        
        // Set updated patient demographics
        adt.SetPatientDemographics(patientId, lastName, firstName, dateOfBirth: dateOfBirth, gender: gender);
        
        // Set visit context as outpatient update
        adt.SetPatientVisit("O", "REGISTRATION");
        
        // Add update reason if provided
        if (!string.IsNullOrEmpty(updateReason) && adt.EventType != null)
        {
            adt.EventType.EventReasonCode.SetValue(updateReason);
        }
        
        return adt;
    }

    /// <summary>
    /// Validates pharmacy-specific segments and business rules.
    /// </summary>
    /// <returns>List of pharmacy-related validation issues</returns>
    public List<string> ValidatePharmacyContext()
    {
        var issues = new List<string>();
        
        // Validate diagnosis segments
        for (int i = 0; i < Diagnoses.Count; i++)
        {
            var diagnosisIssues = Diagnoses[i].Validate();
            issues.AddRange(diagnosisIssues.Select(issue => $"Diagnosis {i + 1}: {issue}"));
        }
        
        // Validate allergy segments
        for (int i = 0; i < Allergies.Count; i++)
        {
            var allergyIssues = Allergies[i].Validate();
            issues.AddRange(allergyIssues.Select(issue => $"Allergy {i + 1}: {issue}"));
        }
        
        // Validate insurance segments
        for (int i = 0; i < Insurance.Count; i++)
        {
            var insuranceIssues = Insurance[i].Validate();
            issues.AddRange(insuranceIssues.Select(issue => $"Insurance {i + 1}: {issue}"));
        }
        
        // Business rule: Discharge messages should have at least one diagnosis
        if (TriggerEvent == "A03" && !Diagnoses.Any())
        {
            issues.Add("Discharge messages (A03) should include at least one diagnosis for medication reconciliation");
        }
        
        // Business rule: Check for drug allergy completeness
        var drugAllergies = Allergies.Where(a => a.AllergenTypeCode.Identifier == "DA").ToList();
        foreach (var drugAllergy in drugAllergies)
        {
            if (string.IsNullOrEmpty(drugAllergy.AllergyReactionCode.RawValue))
            {
                issues.Add($"Drug allergy to {drugAllergy.AllergenCodeMnemonicDescription.Identifier} should include reaction description for pharmacy safety");
            }
        }
        
        return issues;
    }

    /// <summary>
    /// Gets a pharmacy-ready display string with clinical context.
    /// </summary>
    /// <returns>Enhanced display string with pharmacy information</returns>
    public string ToPharmacyDisplayString()
    {
        var baseInfo = ToDisplayString();
        var diagnosisCount = Diagnoses.Count;
        var allergyCount = Allergies.Count;
        var insuranceCount = Insurance.Count;
        
        var contextInfo = new List<string>();
        if (diagnosisCount > 0) contextInfo.Add($"{diagnosisCount} diagnoses");
        if (allergyCount > 0) contextInfo.Add($"{allergyCount} allergies");
        if (insuranceCount > 0) contextInfo.Add($"{insuranceCount} insurance plans");
        
        var context = contextInfo.Any() ? $" [{string.Join(", ", contextInfo)}]" : "";
        return baseInfo + context;
    }

    #endregion
}
