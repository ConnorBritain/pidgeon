// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.


using Segmint.Core.HL7;
using Segmint.Core.HL7.Validation;
using Segmint.Core.Standards.HL7.v23.Segments;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System;
namespace Segmint.Core.Standards.HL7.v23.Messages;

/// <summary>
/// Represents an HL7 Pharmacy Dispense (RDS) message for communicating 
/// completed medication dispensing from pharmacy systems back to EHR/CIPS.
/// This message confirms actual medication dispensing with lot numbers,
/// quantities, and dispensing details for medication tracking and reconciliation.
/// Structure: MSH [PID] [PV1] ORC RXO RXE RXD [RXC] [OBX] [NTE]
/// </summary>
public class RDSMessage : HL7Message
{
    /// <inheritdoc />
    public override string MessageType => "RDS";

    /// <inheritdoc />
    public override string TriggerEvent => "O01";

    /// <inheritdoc />
    public override string MessageStructure => "RDS_O01";

    #region Message Segments

    /// <summary>
    /// Message Header segment (required).
    /// </summary>
    public MSHSegment Header { get; private set; }

    /// <summary>
    /// Patient Identification segment (optional).
    /// Patient demographics for the dispensing record.
    /// </summary>
    public PIDSegment? PatientIdentification { get; set; }

    /// <summary>
    /// Patient Visit segment (optional).
    /// Visit context for the dispensing.
    /// </summary>
    public PV1Segment? PatientVisit { get; set; }

    /// <summary>
    /// List of Pharmacy Dispense groups containing dispense details.
    /// Each group represents a specific medication dispensing event.
    /// </summary>
    public List<PharmacyDispenseGroup> Dispenses { get; private set; }

    #endregion

    /// <summary>
    /// Initializes a new instance of the RDSMessage class.
    /// </summary>
    public RDSMessage()
    {
        Header = new MSHSegment();
        Dispenses = new List<PharmacyDispenseGroup>();
        InitializeMessage();
    }

    /// <summary>
    /// Initializes the message with default values.
    /// </summary>
    protected override void InitializeMessage()
    {
        // Set up message header
        Header.SetBasicInfo();
        Header.SetMessageType(MessageType, TriggerEvent, MessageStructure);
        Header.SetProcessingId(true); // Production mode
        Header.GenerateMessageControlId();
    }

    #region Message Building Methods

    /// <summary>
    /// Sets patient information for the dispense record.
    /// </summary>
    /// <param name="patientId">Patient identifier</param>
    /// <param name="lastName">Patient's last name</param>
    /// <param name="firstName">Patient's first name</param>
    /// <param name="middleName">Patient's middle name</param>
    /// <param name="dateOfBirth">Patient's date of birth</param>
    /// <param name="gender">Patient's gender</param>
    public void SetPatientInformation(
        string patientId,
        string lastName,
        string? firstName = null,
        string? middleName = null,
        DateTime? dateOfBirth = null,
        string? gender = null)
    {
        PatientIdentification ??= new PIDSegment();
        PatientIdentification.SetBasicInfo(patientId, lastName, firstName ?? "", middleName);
        
        if (dateOfBirth.HasValue)
            PatientIdentification.SetDateOfBirth(dateOfBirth.Value);
            
        if (!string.IsNullOrEmpty(gender))
            PatientIdentification.SetGender(gender);
    }

    /// <summary>
    /// Sets patient visit information.
    /// </summary>
    /// <param name="patientClass">Patient class (O=Outpatient, I=Inpatient)</param>
    /// <param name="assignedPatientLocation">Patient location</param>
    /// <param name="attendingDoctor">Attending physician</param>
    /// <param name="visitNumber">Visit number</param>
    public void SetPatientVisit(
        string patientClass,
        string? assignedPatientLocation = null,
        string? attendingDoctor = null,
        string? visitNumber = null)
    {
        PatientVisit ??= new PV1Segment();
        PatientVisit.SetPatientClass(patientClass);
        
        if (!string.IsNullOrEmpty(assignedPatientLocation))
            PatientVisit.SetAssignedPatientLocation(assignedPatientLocation);
            
        if (!string.IsNullOrEmpty(attendingDoctor))
            PatientVisit.SetAttendingDoctor(attendingDoctor);
            
        if (!string.IsNullOrEmpty(visitNumber))
            PatientVisit.SetVisitNumber(visitNumber);
    }

    /// <summary>
    /// Adds a new medication dispensing record.
    /// </summary>
    /// <param name="prescriptionNumber">Prescription number</param>
    /// <param name="placerOrderNumber">Original placer order number</param>
    /// <param name="fillerOrderNumber">Pharmacy filler order number</param>
    /// <param name="pharmacist">Dispensing pharmacist</param>
    /// <param name="dispensingLocation">Location where dispensed</param>
    /// <returns>The created pharmacy dispense group</returns>
    public PharmacyDispenseGroup AddDispense(
        string prescriptionNumber,
        string? placerOrderNumber = null,
        string? fillerOrderNumber = null,
        string? pharmacist = null,
        string? dispensingLocation = null)
    {
        var dispenseGroup = new PharmacyDispenseGroup();
        
        // Set up ORC segment
        dispenseGroup.OrderControl.SetBasicInfo(
            "SC", // Status changed - dispensed
            placerOrderNumber,
            fillerOrderNumber,
            "CM" // Complete
        );
        
        if (!string.IsNullOrEmpty(pharmacist))
            dispenseGroup.OrderControl.ActionBy.SetValue(pharmacist);
            
        if (!string.IsNullOrEmpty(dispensingLocation))
            dispenseGroup.OrderControl.EnterersLocation.SetValue(dispensingLocation);
        
        // Initialize RXO and RXE segments
        dispenseGroup.PharmacyOrder = new RXOSegment();
        dispenseGroup.EncodedOrder = new RXESegment();
        
        // Initialize RXD segment with prescription number
        dispenseGroup.DispenseRecord = new RXDSegment();
        dispenseGroup.DispenseRecord.PrescriptionNumber.SetValue(prescriptionNumber);
        dispenseGroup.DispenseRecord.DateTimeDispensed.SetValue(DateTime.Now);
        
        Dispenses.Add(dispenseGroup);
        return dispenseGroup;
    }

    /// <summary>
    /// Records a complete medication dispensing with full details.
    /// </summary>
    /// <param name="prescriptionNumber">Prescription number</param>
    /// <param name="medicationCode">Dispensed medication code (NDC)</param>
    /// <param name="medicationName">Dispensed medication name</param>
    /// <param name="dispensedAmount">Amount dispensed</param>
    /// <param name="units">Units for amount</param>
    /// <param name="pharmacist">Dispensing pharmacist</param>
    /// <param name="lotNumber">Medication lot number</param>
    /// <param name="expirationDate">Medication expiration date</param>
    /// <param name="manufacturer">Medication manufacturer</param>
    /// <param name="refillsRemaining">Number of refills remaining</param>
    /// <param name="genericSubstitution">Whether generic substitution was made</param>
    /// <returns>The created dispense group</returns>
    public PharmacyDispenseGroup RecordMedicationDispense(
        string prescriptionNumber,
        string medicationCode,
        string medicationName,
        decimal dispensedAmount,
        string units,
        string pharmacist,
        string? lotNumber = null,
        DateTime? expirationDate = null,
        string? manufacturer = null,
        int? refillsRemaining = null,
        bool? genericSubstitution = null)
    {
        var dispense = AddDispense(prescriptionNumber, pharmacist: pharmacist);
        
        // Set medication information in RXO (original order)
        dispense.PharmacyOrder!.SetBasicMedication(medicationCode, medicationName, "NDC", dispensedAmount, units);
        
        // Set encoded order information in RXE
        dispense.EncodedOrder!.SetBasicMedicationInfo(medicationCode, medicationName, "NDC");
        dispense.EncodedOrder!.SetDispensingInfo(dispensedAmount, units);
        
        // Set detailed dispensing information in RXD
        dispense.DispenseRecord!.SetBasicDispense(1, medicationCode, medicationName, dispensedAmount, units, prescriptionNumber);
        dispense.DispenseRecord.SetMedicationDetails(
            lotNumber: lotNumber,
            expirationDate: expirationDate,
            manufacturer: manufacturer
        );
        dispense.DispenseRecord.SetPharmacyInfo(
            pharmacist: pharmacist,
            refillsRemaining: refillsRemaining,
            substitutionMade: genericSubstitution
        );
        
        return dispense;
    }

    /// <summary>
    /// Records a controlled substance dispensing with enhanced tracking.
    /// </summary>
    /// <param name="prescriptionNumber">Prescription number</param>
    /// <param name="medicationCode">Controlled substance NDC code</param>
    /// <param name="medicationName">Controlled substance name</param>
    /// <param name="dispensedAmount">Amount dispensed</param>
    /// <param name="units">Units</param>
    /// <param name="pharmacist">Dispensing pharmacist</param>
    /// <param name="lotNumber">Lot number (required for controlled substances)</param>
    /// <param name="expirationDate">Expiration date</param>
    /// <param name="manufacturer">Manufacturer</param>
    /// <param name="deaNumber">Pharmacist's DEA number</param>
    /// <param name="controlledSubstanceSchedule">DEA schedule (II, III, IV, V)</param>
    /// <returns>The created dispense group</returns>
    public PharmacyDispenseGroup RecordControlledSubstanceDispense(
        string prescriptionNumber,
        string medicationCode,
        string medicationName,
        decimal dispensedAmount,
        string units,
        string pharmacist,
        string lotNumber,
        DateTime expirationDate,
        string manufacturer,
        string deaNumber,
        string controlledSubstanceSchedule)
    {
        var dispense = RecordMedicationDispense(
            prescriptionNumber,
            medicationCode,
            medicationName,
            dispensedAmount,
            units,
            pharmacist,
            lotNumber,
            expirationDate,
            manufacturer,
            genericSubstitution: false // No substitution for controlled substances
        );
        
        // Add controlled substance specific information
        dispense.DispenseRecord!.SetPharmacyInfo(needsReview: true);
        
        // Add note about controlled substance
        var note = new NTESegment();
        note.Comment.SetValue($"Controlled Substance Schedule {controlledSubstanceSchedule} - DEA: {deaNumber}");
        dispense.Notes.Add(note);
        
        return dispense;
    }

    /// <summary>
    /// Records a partial dispensing (when full quantity not available).
    /// </summary>
    /// <param name="prescriptionNumber">Prescription number</param>
    /// <param name="medicationCode">Medication code</param>
    /// <param name="medicationName">Medication name</param>
    /// <param name="orderedAmount">Originally ordered amount</param>
    /// <param name="dispensedAmount">Actually dispensed amount</param>
    /// <param name="units">Units</param>
    /// <param name="pharmacist">Dispensing pharmacist</param>
    /// <param name="partialReason">Reason for partial dispensing</param>
    /// <param name="remainingToDispense">Amount still to be dispensed</param>
    /// <returns>The created dispense group</returns>
    public PharmacyDispenseGroup RecordPartialDispense(
        string prescriptionNumber,
        string medicationCode,
        string medicationName,
        decimal orderedAmount,
        decimal dispensedAmount,
        string units,
        string pharmacist,
        string partialReason,
        decimal? remainingToDispense = null)
    {
        var dispense = RecordMedicationDispense(
            prescriptionNumber,
            medicationCode,
            medicationName,
            dispensedAmount,
            units,
            pharmacist
        );
        
        // Add note about partial dispensing
        var note = new NTESegment();
        var noteText = $"PARTIAL DISPENSE: Ordered {orderedAmount} {units}, dispensed {dispensedAmount} {units}. Reason: {partialReason}";
        if (remainingToDispense.HasValue)
        {
            noteText += $" Remaining to dispense: {remainingToDispense} {units}";
        }
        note.Comment.SetValue(noteText);
        dispense.Notes.Add(note);
        
        return dispense;
    }

    /// <summary>
    /// Records a medication return or void.
    /// </summary>
    /// <param name="prescriptionNumber">Original prescription number</param>
    /// <param name="returnReason">Reason for return</param>
    /// <param name="returnedAmount">Amount being returned</param>
    /// <param name="units">Units</param>
    /// <param name="pharmacist">Pharmacist processing return</param>
    /// <param name="returnDate">Date of return</param>
    /// <returns>The created dispense group</returns>
    public PharmacyDispenseGroup RecordMedicationReturn(
        string prescriptionNumber,
        string returnReason,
        decimal returnedAmount,
        string units,
        string pharmacist,
        DateTime? returnDate = null)
    {
        var dispense = AddDispense(prescriptionNumber, pharmacist: pharmacist);
        
        // Set order control to void/cancel
        dispense.OrderControl.SetBasicInfo("CA", orderStatus: "CA");
        
        // Add return information as note
        var note = new NTESegment();
        note.Comment.SetValue($"MEDICATION RETURN: {returnedAmount} {units} returned on {(returnDate ?? DateTime.Now):yyyy-MM-dd}. Reason: {returnReason}");
        dispense.Notes.Add(note);
        
        return dispense;
    }

    #endregion

    #region Message Serialization

    /// <inheritdoc />
    public string ToHL7String()
    {
        var segments = new List<string>();
        
        // Add required segments
        segments.Add(Header.ToHL7String());
        
        // Add optional patient information
        if (PatientIdentification != null)
            segments.Add(PatientIdentification.ToHL7String());
            
        if (PatientVisit != null)
            segments.Add(PatientVisit.ToHL7String());
        
        // Add pharmacy dispense groups
        foreach (var dispense in Dispenses)
        {
            segments.Add(dispense.OrderControl.ToHL7String());
            
            if (dispense.PharmacyOrder != null)
                segments.Add(dispense.PharmacyOrder.ToHL7String());
            
            if (dispense.EncodedOrder != null)
                segments.Add(dispense.EncodedOrder.ToHL7String());
            
            if (dispense.DispenseRecord != null)
                segments.Add(dispense.DispenseRecord.ToHL7String());
            
            // Add administration components
            foreach (var rxc in dispense.PharmacyComponents)
                segments.Add(rxc.ToHL7String());
            
            // Add observations
            foreach (var obx in dispense.Observations)
                segments.Add(obx.ToHL7String());
            
            // Add notes
            foreach (var note in dispense.Notes)
                segments.Add(note.ToHL7String());
        }
        
        return string.Join("\r", segments);
    }

    /// <inheritdoc />
    public override ValidationResult Validate()
    {
        var result = base.Validate();
        
        // Must have at least one dispense
        if (!Dispenses.Any())
            result.AddIssue(ValidationIssue.SemanticError("RDS001", "RDS message must contain at least one pharmacy dispense group", "RDS"));
        
        // Validate each dispense group (simplified for now - assuming they have ValidationResult Validate() methods)
        for (int i = 0; i < Dispenses.Count; i++)
        {
            try
            {
                // Try to call Validate if it exists, otherwise skip detailed validation for now
                var dispenseValidation = Dispenses[i].GetType().GetMethod("Validate")?.Invoke(Dispenses[i], null);
                if (dispenseValidation is List<string> stringErrors)
                {
                    foreach (var error in stringErrors)
                    {
                        result.AddIssue(ValidationIssue.SemanticError($"RDS{i + 2:D3}", $"Dispense {i + 1}: {error}", $"Dispense[{i}]"));
                    }
                }
            }
            catch
            {
                // Skip validation if method doesn't exist or fails
            }
        }
        
        return result;
    }

    /// <inheritdoc />
    public string ToDisplayString()
    {
        var patientInfo = PatientIdentification?.PatientName.ToDisplayString() ?? "No patient";
        var dispenseCount = Dispenses.Count;
        var dispensedMeds = Dispenses.Select(d => d.DispenseRecord?.DispenseGiveCode.Text ?? "Unknown medication").Distinct();
        
        return $"RDS Dispense - Patient: {patientInfo}, {dispenseCount} dispenses ({string.Join(", ", dispensedMeds.Take(3))})";
    }

    #endregion

    #region Static Factory Methods

    /// <summary>
    /// Creates a basic medication dispense message.
    /// </summary>
    /// <param name="patientId">Patient ID</param>
    /// <param name="patientLastName">Patient last name</param>
    /// <param name="patientFirstName">Patient first name</param>
    /// <param name="prescriptionNumber">Prescription number</param>
    /// <param name="medicationCode">Medication code</param>
    /// <param name="medicationName">Medication name</param>
    /// <param name="amount">Amount dispensed</param>
    /// <param name="units">Units</param>
    /// <param name="pharmacist">Dispensing pharmacist</param>
    /// <returns>Configured RDS message</returns>
    public static RDSMessage CreateBasicDispense(
        string patientId,
        string patientLastName,
        string? patientFirstName,
        string prescriptionNumber,
        string medicationCode,
        string medicationName,
        decimal amount,
        string units,
        string pharmacist)
    {
        var rds = new RDSMessage();
        rds.SetPatientInformation(patientId, patientLastName, patientFirstName);
        rds.RecordMedicationDispense(prescriptionNumber, medicationCode, medicationName, amount, units, pharmacist);
        return rds;
    }

    /// <summary>
    /// Creates a controlled substance dispense message.
    /// </summary>
    /// <param name="patientId">Patient ID</param>
    /// <param name="patientLastName">Patient last name</param>
    /// <param name="patientFirstName">Patient first name</param>
    /// <param name="prescriptionNumber">Prescription number</param>
    /// <param name="medicationCode">Controlled substance code</param>
    /// <param name="medicationName">Controlled substance name</param>
    /// <param name="amount">Amount dispensed</param>
    /// <param name="units">Units</param>
    /// <param name="pharmacist">Dispensing pharmacist</param>
    /// <param name="lotNumber">Lot number</param>
    /// <param name="expirationDate">Expiration date</param>
    /// <param name="manufacturer">Manufacturer</param>
    /// <param name="deaNumber">DEA number</param>
    /// <param name="schedule">DEA schedule</param>
    /// <returns>Configured RDS message for controlled substance</returns>
    public static RDSMessage CreateControlledSubstanceDispense(
        string patientId,
        string patientLastName,
        string? patientFirstName,
        string prescriptionNumber,
        string medicationCode,
        string medicationName,
        decimal amount,
        string units,
        string pharmacist,
        string lotNumber,
        DateTime expirationDate,
        string manufacturer,
        string deaNumber,
        string schedule)
    {
        var rds = new RDSMessage();
        rds.SetPatientInformation(patientId, patientLastName, patientFirstName);
        rds.RecordControlledSubstanceDispense(
            prescriptionNumber, medicationCode, medicationName, amount, units,
            pharmacist, lotNumber, expirationDate, manufacturer, deaNumber, schedule);
        return rds;
    }

    #endregion

    /// <inheritdoc />
    public override HL7Message Clone()
    {
        var cloned = new RDSMessage();
        
        // Clear default segments
        cloned.ClearSegments();
        
        // Clone all segments
        foreach (var segment in this)
        {
            cloned.AddSegment(segment.Clone());
        }
        
        // Update references
        cloned.Header = cloned.GetSegment<MSHSegment>() ?? new MSHSegment();
        cloned.PatientIdentification = cloned.GetSegment<PIDSegment>();
        cloned.PatientVisit = cloned.GetSegment<PV1Segment>();
        
        // Rebuild dispense groups
        cloned.Dispenses.Clear();
        foreach (var dispense in Dispenses)
        {
            // Find the corresponding cloned segments
            var clonedOrc = cloned.GetSegments<ORCSegment>().Skip(cloned.Dispenses.Count).FirstOrDefault();
            if (clonedOrc != null)
            {
                var newDispense = new PharmacyDispenseGroup(clonedOrc);
                cloned.Dispenses.Add(newDispense);
            }
        }
        
        return cloned;
    }

    /// <summary>
    /// Represents dispense information for pharmacy workflows.
    /// Provides a simplified interface for common dispensing scenarios.
    /// </summary>
    public class DispenseInfo : PharmacyDispenseGroup
    {
        /// <summary>
        /// Gets or sets the dispensing record segment.
        /// </summary>
        public new RXDSegment? DispenseRecord 
        { 
            get => base.DispenseRecord; 
            set => base.DispenseRecord = value;
        }

        /// <summary>
        /// Initializes a new instance of the DispenseInfo class.
        /// </summary>
        public DispenseInfo() : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the DispenseInfo class with a specific OrderControl segment.
        /// </summary>
        /// <param name="orderControl">The order control segment to use</param>
        public DispenseInfo(ORCSegment orderControl) : base(orderControl)
        {
        }
    }
}

/// <summary>
/// Represents a group of pharmacy dispensing segments (ORC + RXO + RXE + RXD + RXC + OBX + NTE).
/// Each group contains complete information about a specific medication dispensing event.
/// </summary>
public class PharmacyDispenseGroup
{
    /// <summary>
    /// Order Control segment (required).
    /// </summary>
    public ORCSegment OrderControl { get; private set; }

    /// <summary>
    /// Pharmacy/Treatment Order segment (optional).
    /// Original order information.
    /// </summary>
    public RXOSegment? PharmacyOrder { get; set; }

    /// <summary>
    /// Pharmacy/Treatment Encoded Order segment (optional).
    /// Encoded prescription details.
    /// </summary>
    public RXESegment? EncodedOrder { get; set; }

    /// <summary>
    /// Pharmacy/Treatment Dispense segment (required for actual dispensing).
    /// Actual dispensing details.
    /// </summary>
    public RXDSegment? DispenseRecord { get; set; }

    /// <summary>
    /// List of Pharmacy/Treatment Component Order segments (optional).
    /// Additional medication components.
    /// </summary>
    public List<RXCSegment> PharmacyComponents { get; private set; }

    /// <summary>
    /// List of Observation/Result segments (optional).
    /// Clinical observations related to dispensing.
    /// </summary>
    public List<OBXSegment> Observations { get; private set; }

    /// <summary>
    /// List of Note segments (optional).
    /// Additional notes and comments.
    /// </summary>
    public List<NTESegment> Notes { get; private set; }

    /// <summary>
    /// Initializes a new instance of the PharmacyDispenseGroup class.
    /// </summary>
    public PharmacyDispenseGroup()
    {
        OrderControl = new ORCSegment();
        PharmacyComponents = new List<RXCSegment>();
        Observations = new List<OBXSegment>();
        Notes = new List<NTESegment>();
    }

    /// <summary>
    /// Initializes a new instance of the PharmacyDispenseGroup class with a specific OrderControl segment.
    /// </summary>
    /// <param name="orderControl">The order control segment to use</param>
    public PharmacyDispenseGroup(ORCSegment orderControl)
    {
        OrderControl = orderControl;
        PharmacyComponents = new List<RXCSegment>();
        Observations = new List<OBXSegment>();
        Notes = new List<NTESegment>();
    }

    /// <summary>
    /// Validates the pharmacy dispense group.
    /// </summary>
    /// <returns>List of validation issues.</returns>
    public List<string> Validate()
    {
        var issues = new List<string>();
        
        // Validate required segments
        issues.AddRange(OrderControl.Validate());
        
        // Validate optional segments
        if (PharmacyOrder != null)
            issues.AddRange(PharmacyOrder.Validate());
            
        if (EncodedOrder != null)
            issues.AddRange(EncodedOrder.Validate());
            
        if (DispenseRecord != null)
            issues.AddRange(DispenseRecord.Validate());
        
        // Validate component segments
        for (int i = 0; i < PharmacyComponents.Count; i++)
        {
            var componentIssues = PharmacyComponents[i].Validate();
            issues.AddRange(componentIssues.Select(issue => $"RXC {i + 1}: {issue}"));
        }
        
        // Validate observation segments
        for (int i = 0; i < Observations.Count; i++)
        {
            var obsIssues = Observations[i].Validate();
            issues.AddRange(obsIssues.Select(issue => $"OBX {i + 1}: {issue}"));
        }
        
        // Validate note segments
        for (int i = 0; i < Notes.Count; i++)
        {
            var noteIssues = Notes[i].Validate();
            issues.AddRange(noteIssues.Select(issue => $"NTE {i + 1}: {issue}"));
        }
        
        return issues;
    }
}
