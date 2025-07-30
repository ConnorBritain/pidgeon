// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.


using Segmint.Core.HL7;
using Segmint.Core.HL7.Validation;
using Segmint.Core.Standards.HL7.v23.Segments;
using System.Collections.Generic;
using System;
namespace Segmint.Core.Standards.HL7.v23.Messages;

/// <summary>
/// Represents an HL7 RDE (Pharmacy Order) message.
/// This message is used to communicate pharmacy orders between systems.
/// Structure: MSH EVN PID [PV1] ORC RXE [RXR] [RXC] [NTE]
/// </summary>
public class RDEMessage : HL7Message
{
    /// <inheritdoc />
    public override string MessageType => "RDE";

    /// <inheritdoc />
    public override string TriggerEvent => "O01";

    /// <summary>
    /// Initializes a new instance of the <see cref="RDEMessage"/> class.
    /// </summary>
    public RDEMessage()
    {
    }

    /// <summary>
    /// Gets or sets the message header segment (MSH) - Required.
    /// </summary>
    public override MSHSegment? MessageHeader
    {
        get => GetSegment<MSHSegment>();
        set 
        { 
            if (value != null) 
                ReplaceOrAddSegment(value, 0);
        }
    }

    /// <summary>
    /// Gets or sets the event type segment (EVN) - Required.
    /// </summary>
    public EVNSegment EventType
    {
        get => GetSegment<EVNSegment>() ?? throw new InvalidOperationException("EVN segment is required");
        set => ReplaceOrAddSegment(value, 1);
    }

    /// <summary>
    /// Gets or sets the patient identification segment (PID) - Required.
    /// </summary>
    public PIDSegment PatientIdentification
    {
        get => GetSegment<PIDSegment>() ?? throw new InvalidOperationException("PID segment is required");
        set => ReplaceOrAddSegment(value, 2);
    }

    /// <summary>
    /// Gets or sets the common order segment (ORC) - Required.
    /// </summary>
    public ORCSegment CommonOrder
    {
        get => GetSegment<ORCSegment>() ?? throw new InvalidOperationException("ORC segment is required");
        set => ReplaceOrAddSegment(value, GetORCIndex());
    }

    /// <summary>
    /// Gets or sets the pharmacy order segment (RXE) - Required.
    /// </summary>
    public RXESegment PharmacyOrder
    {
        get => GetSegment<RXESegment>() ?? throw new InvalidOperationException("RXE segment is required");
        set => ReplaceOrAddSegment(value, GetRXEIndex());
    }

    /// <inheritdoc />
    protected override void InitializeMessage()
    {
        // Create required segments in proper order
        AddSegment(new MSHSegment());
        AddSegment(new EVNSegment());
        AddSegment(new PIDSegment());
        AddSegment(new ORCSegment());
        AddSegment(new RXESegment());
    }

    /// <summary>
    /// Sets up a basic RDE message with minimal required information.
    /// </summary>
    /// <param name="sendingApplication">The sending application name.</param>
    /// <param name="sendingFacility">The sending facility name.</param>
    /// <param name="receivingApplication">The receiving application name.</param>
    /// <param name="receivingFacility">The receiving facility name.</param>
    /// <param name="patientId">The patient identifier.</param>
    /// <param name="patientFirstName">The patient's first name.</param>
    /// <param name="patientLastName">The patient's last name.</param>
    /// <param name="drugCode">The drug code.</param>
    /// <param name="drugName">The drug name.</param>
    /// <param name="orderingProvider">The ordering provider ID.</param>
    /// <param name="isProduction">Whether this is a production message.</param>
    public void SetupBasicOrder(
        string sendingApplication,
        string sendingFacility,
        string receivingApplication,
        string receivingFacility,
        string patientId,
        string patientFirstName,
        string patientLastName,
        string drugCode,
        string drugName,
        string? orderingProvider = null,
        bool isProduction = false)
    {
        // Setup MSH
        if (MessageHeader == null)
            MessageHeader = new MSHSegment();
        
        MessageHeader.SetBasicInfo(
            sendingApplication,
            sendingFacility,
            receivingApplication,
            receivingFacility);
        MessageHeader.SetMessageType("RDE", "O01");
        MessageHeader.GenerateMessageControlId("RDE");
        MessageHeader.SetProcessingId(isProduction);

        // Setup EVN
        EventType.SetBasicInfo("O01");

        // Setup PID
        PatientIdentification.SetBasicInfo(
            patientId,
            patientLastName,
            patientFirstName);

        // Setup ORC
        CommonOrder.SetBasicInfo("NW", orderingProvider: orderingProvider);
        CommonOrder.GeneratePlacerOrderNumber();

        // Setup RXE
        PharmacyOrder.SetBasicMedicationInfo(
            drugCode,
            drugName,
            "NDC");
        PharmacyOrder.GeneratePrescriptionNumber();
    }

    /// <summary>
    /// Sets comprehensive medication order information.
    /// </summary>
    /// <param name="drugCode">The drug code (NDC, RxNorm, etc.).</param>
    /// <param name="drugName">The drug name.</param>
    /// <param name="strength">The drug strength.</param>
    /// <param name="strengthUnits">The units for strength.</param>
    /// <param name="dosageForm">The dosage form.</param>
    /// <param name="dispenseQuantity">Quantity to dispense.</param>
    /// <param name="dispenseUnits">Units for dispense quantity.</param>
    /// <param name="refills">Number of refills.</param>
    /// <param name="sig">Directions for use.</param>
    /// <param name="daysSupply">Days supply.</param>
    public void SetMedicationDetails(
        string drugCode,
        string drugName,
        decimal? strength = null,
        string? strengthUnits = null,
        string? dosageForm = null,
        decimal? dispenseQuantity = null,
        string? dispenseUnits = null,
        int? refills = null,
        string? sig = null,
        int? daysSupply = null)
    {
        PharmacyOrder.SetBasicMedicationInfo(
            drugCode,
            drugName,
            "NDC",
            strength,
            strengthUnits,
            sig,
            dosageForm);

        if (dispenseQuantity.HasValue)
        {
            PharmacyOrder.SetDispensingInfo(
                dispenseQuantity,
                dispenseUnits ?? strengthUnits,
                refills);
        }
    }

    /// <summary>
    /// Sets patient demographic information.
    /// </summary>
    /// <param name="patientId">The patient identifier.</param>
    /// <param name="firstName">The patient's first name.</param>
    /// <param name="lastName">The patient's last name.</param>
    /// <param name="middleName">The patient's middle name.</param>
    /// <param name="dateOfBirth">The patient's date of birth.</param>
    /// <param name="gender">The patient's gender.</param>
    /// <param name="accountNumber">The patient account number.</param>
    /// <param name="street">The street address.</param>
    /// <param name="city">The city.</param>
    /// <param name="state">The state.</param>
    /// <param name="postalCode">The postal code.</param>
    /// <param name="homePhone">The home phone number.</param>
    public void SetPatientInfo(
        string patientId,
        string firstName,
        string lastName,
        string? middleName = null,
        DateTime? dateOfBirth = null,
        string? gender = null,
        string? accountNumber = null,
        string? street = null,
        string? city = null,
        string? state = null,
        string? postalCode = null,
        string? homePhone = null)
    {
        PatientIdentification.SetBasicInfo(
            patientId,
            lastName,
            firstName,
            middleName,
            dateOfBirth,
            gender,
            accountNumber);

        if (!string.IsNullOrEmpty(street) || !string.IsNullOrEmpty(city))
        {
            PatientIdentification.SetAddress(street, city, state, postalCode);
        }

        if (!string.IsNullOrEmpty(homePhone))
        {
            PatientIdentification.SetPhoneNumbers(homePhone);
        }
    }

    /// <summary>
    /// Sets order control and provider information.
    /// </summary>
    /// <param name="orderControl">The order control code (NW, CA, DC, etc.).</param>
    /// <param name="orderingProvider">The ordering provider ID.</param>
    /// <param name="orderingProviderDEA">The ordering provider's DEA number.</param>
    /// <param name="enteredBy">The user who entered the order.</param>
    /// <param name="orderEffectiveDate">When the order becomes effective.</param>
    /// <param name="pharmacistId">The pharmacist verifier ID.</param>
    public void SetOrderInfo(
        string? orderControl = null,
        string? orderingProvider = null,
        string? orderingProviderDEA = null,
        string? enteredBy = null,
        DateTime? orderEffectiveDate = null,
        string? pharmacistId = null)
    {
        if (!string.IsNullOrEmpty(orderControl))
        {
            CommonOrder.SetBasicInfo(orderControl, orderingProvider: orderingProvider, enteredBy: enteredBy);
        }

        if (orderEffectiveDate.HasValue)
        {
            CommonOrder.SetTiming(orderEffectiveDate);
        }

        if (!string.IsNullOrEmpty(orderingProviderDEA) || !string.IsNullOrEmpty(pharmacistId))
        {
            PharmacyOrder.SetProviderInfo(
                orderingProviderDEA: orderingProviderDEA,
                pharmacistVerifierId: pharmacistId);
        }
    }

    /// <summary>
    /// Gets the expected index for the ORC segment.
    /// </summary>
    private int GetORCIndex()
    {
        // ORC comes after MSH, EVN, PID (and optional PV1)
        var baseIndex = 3;
        
        // Check if PV1 exists
        var pv1 = GetSegment("PV1");
        if (pv1 != null)
            baseIndex++;
            
        return baseIndex;
    }

    /// <summary>
    /// Gets the expected index for the RXE segment.
    /// </summary>
    private int GetRXEIndex()
    {
        // RXE comes after ORC
        return GetORCIndex() + 1;
    }

    /// <summary>
    /// Replaces an existing segment or adds it at the specified index.
    /// </summary>
    private void ReplaceOrAddSegment(HL7Segment segment, int preferredIndex)
    {
        // Find existing segment of this type
        var existingIndex = -1;
        for (int i = 0; i < SegmentCount; i++)
        {
            if (this[i].SegmentId == segment.SegmentId)
            {
                existingIndex = i;
                break;
            }
        }

        if (existingIndex >= 0)
        {
            // Replace existing segment
            this[existingIndex] = segment;
        }
        else
        {
            // Insert at preferred index
            if (preferredIndex < SegmentCount)
            {
                InsertSegment(preferredIndex, segment);
            }
            else
            {
                AddSegment(segment);
            }
        }
    }

    /// <inheritdoc />
    public override ValidationResult Validate()
    {
        var result = base.Validate();

        // Validate RDE-specific requirements
        if (GetSegment<MSHSegment>() == null)
            result.AddIssue(ValidationIssue.SemanticError("RDE001", "RDE message must contain an MSH segment", "MSH"));

        if (GetSegment<EVNSegment>() == null)
            result.AddIssue(ValidationIssue.SemanticError("RDE002", "RDE message must contain an EVN segment", "EVN"));

        if (GetSegment<PIDSegment>() == null)
            result.AddIssue(ValidationIssue.SemanticError("RDE003", "RDE message must contain a PID segment", "PID"));

        if (GetSegment<ORCSegment>() == null)
            result.AddIssue(ValidationIssue.SemanticError("RDE004", "RDE message must contain an ORC segment", "ORC"));

        if (GetSegment<RXESegment>() == null)
            result.AddIssue(ValidationIssue.SemanticError("RDE005", "RDE message must contain an RXE segment", "RXE"));

        // Validate message structure order
        var mshIndex = FindSegmentIndex("MSH");
        var evnIndex = FindSegmentIndex("EVN");
        var pidIndex = FindSegmentIndex("PID");
        var orcIndex = FindSegmentIndex("ORC");
        var rxeIndex = FindSegmentIndex("RXE");

        if (mshIndex != 0)
            result.AddIssue(ValidationIssue.SemanticError("RDE006", "MSH segment must be first", "MSH"));

        if (evnIndex >= 0 && evnIndex <= mshIndex)
            result.AddIssue(ValidationIssue.SemanticError("RDE007", "EVN segment must come after MSH", "EVN"));

        if (pidIndex >= 0 && pidIndex <= evnIndex)
            result.AddIssue(ValidationIssue.SemanticError("RDE008", "PID segment must come after EVN", "PID"));

        if (orcIndex >= 0 && orcIndex <= pidIndex)
            result.AddIssue(ValidationIssue.SemanticError("RDE009", "ORC segment must come after PID", "ORC"));

        if (rxeIndex >= 0 && rxeIndex <= orcIndex)
            result.AddIssue(ValidationIssue.SemanticError("RDE010", "RXE segment must come after ORC", "RXE"));

        return result;
    }


    /// <summary>
    /// Finds the index of the first segment with the specified ID.
    /// </summary>
    private int FindSegmentIndex(string segmentId)
    {
        for (int i = 0; i < SegmentCount; i++)
        {
            if (this[i].SegmentId == segmentId)
                return i;
        }
        return -1;
    }

    /// <inheritdoc />
    public override HL7Message Clone()
    {
        var clone = new RDEMessage();
        
        // Clear default segments
        while (clone.SegmentCount > 0)
        {
            clone.RemoveSegmentAt(0);
        }
        
        // Copy all segments
        for (int i = 0; i < SegmentCount; i++)
        {
            clone.AddSegment(this[i].Clone());
        }
        
        return clone;
    }

    /// <summary>
    /// Creates a basic RDE message with standard pharmacy order information.
    /// </summary>
    /// <param name="sendingApp">The sending application.</param>
    /// <param name="sendingFacility">The sending facility.</param>
    /// <param name="receivingApp">The receiving application.</param>
    /// <param name="receivingFacility">The receiving facility.</param>
    /// <param name="patientId">The patient ID.</param>
    /// <param name="patientFirstName">Patient's first name.</param>
    /// <param name="patientLastName">Patient's last name.</param>
    /// <param name="drugCode">The drug code (NDC).</param>
    /// <param name="drugName">The drug name.</param>
    /// <param name="orderingProvider">The ordering provider ID.</param>
    /// <param name="isProduction">Whether this is a production message.</param>
    /// <returns>A configured RDE message.</returns>
    public static RDEMessage CreateBasic(
        string sendingApp,
        string sendingFacility,
        string receivingApp,
        string receivingFacility,
        string patientId,
        string patientFirstName,
        string patientLastName,
        string drugCode,
        string drugName,
        string? orderingProvider = null,
        bool isProduction = false)
    {
        var message = new RDEMessage();
        
        message.SetupBasicOrder(
            sendingApp,
            sendingFacility,
            receivingApp,
            receivingFacility,
            patientId,
            patientFirstName,
            patientLastName,
            drugCode,
            drugName,
            orderingProvider,
            isProduction);
            
        return message;
    }

    /// <summary>
    /// Sets patient demographic information (test compatibility method).
    /// </summary>
    /// <param name="patientId">The patient identifier.</param>
    /// <param name="lastName">The patient's last name.</param>
    /// <param name="firstName">The patient's first name.</param>
    /// <param name="middleName">The patient's middle name.</param>
    /// <param name="dateOfBirth">The patient's date of birth.</param>
    /// <param name="gender">The patient's gender.</param>
    /// <param name="ssn">The patient's social security number.</param>
    public void SetPatientDemographics(
        string patientId,
        string lastName,
        string firstName,
        string? middleName = null,
        DateTime? dateOfBirth = null,
        string? gender = null,
        string? ssn = null)
    {
        PatientIdentification.SetBasicInfo(
            patientId,
            lastName,
            firstName,
            middleName,
            dateOfBirth,
            gender,
            ssn);
    }

    /// <summary>
    /// Sets patient address information (test compatibility method).
    /// </summary>
    /// <param name="streetAddress">The street address.</param>
    /// <param name="city">The city.</param>
    /// <param name="state">The state or province.</param>
    /// <param name="zipCode">The ZIP or postal code.</param>
    /// <param name="country">The country.</param>
    public void SetPatientAddress(
        string streetAddress,
        string city,
        string state,
        string zipCode,
        string? country = null)
    {
        PatientIdentification.SetAddress(streetAddress, city, state, zipCode, country);
    }

    /// <summary>
    /// Sets medication information (test compatibility method).
    /// </summary>
    /// <param name="drugCode">The drug code.</param>
    /// <param name="drugName">The drug name.</param>
    /// <param name="strength">The drug strength.</param>
    /// <param name="strengthUnits">The strength units.</param>
    /// <param name="route">The route of administration.</param>
    public void SetMedication(
        string drugCode,
        string drugName,
        string? strength = null,
        string? strengthUnits = null,
        string? route = null)
    {
        PharmacyOrder.SetBasicMedicationInfo(
            drugCode,
            drugName,
            "NDC",
            strength != null ? decimal.Parse(strength) : null,
            strengthUnits);
            
        if (!string.IsNullOrEmpty(route))
        {
            PharmacyOrder.GiveUnits.SetValue(route);
        }
    }

    /// <summary>
    /// Sets dosage instructions (test compatibility method).
    /// </summary>
    /// <param name="instructions">The dosage instructions.</param>
    public void SetDosageInstructions(string instructions)
    {
        PharmacyOrder.ProviderPharmacyTreatmentInstructions.SetValue(instructions);
    }

    /// <summary>
    /// Sets quantity and timing information (test compatibility method).
    /// </summary>
    /// <param name="quantity">The quantity.</param>
    /// <param name="interval">The interval.</param>
    /// <param name="text">The timing text.</param>
    public void SetQuantityTiming(string quantity, string interval, string text)
    {
        PharmacyOrder.QuantityTiming.SetValue($"{quantity}^{interval}^{text}");
    }

    /// <summary>
    /// Sets order control (test compatibility method).
    /// </summary>
    /// <param name="orderControl">The order control code.</param>
    public void SetOrderControl(string orderControl)
    {
        CommonOrder.OrderControl.SetValue(orderControl);
    }

    /// <summary>
    /// Sets ordering provider information (test compatibility method).
    /// </summary>
    /// <param name="lastName">The provider's last name.</param>
    /// <param name="firstName">The provider's first name.</param>
    /// <param name="suffix">The provider's suffix.</param>
    public void SetOrderingProvider(string lastName, string firstName, string? suffix = null)
    {
        CommonOrder.OrderingProvider.SetValue($"{lastName}^{firstName}^{suffix}");
    }

    /// <summary>
    /// Sets sending application information (test compatibility method).
    /// </summary>
    /// <param name="application">The sending application.</param>
    /// <param name="facility">The sending facility.</param>
    public void SetSendingApplication(string application, string facility)
    {
        if (MessageHeader == null)
            MessageHeader = new MSHSegment();
            
        MessageHeader.SendingApplication.SetValue(application);
        MessageHeader.SendingFacility.SetValue(facility);
    }

    /// <summary>
    /// Sets receiving application information (test compatibility method).
    /// </summary>
    /// <param name="application">The receiving application.</param>
    /// <param name="facility">The receiving facility.</param>
    public void SetReceivingApplication(string application, string facility)
    {
        if (MessageHeader == null)
            MessageHeader = new MSHSegment();
            
        MessageHeader.ReceivingApplication.SetValue(application);
        MessageHeader.ReceivingFacility.SetValue(facility);
    }

    /// <summary>
    /// Adds a note segment (test compatibility method).
    /// </summary>
    /// <param name="comment">The note comment.</param>
    /// <param name="source">The source of comment.</param>
    public void AddNote(string comment, string? source = null)
    {
        var note = new NTESegment();
        note.Comment.SetValue(comment);
        if (!string.IsNullOrEmpty(source))
            note.SourceOfComment.SetValue(source);
        AddSegment(note);
    }

    /// <summary>
    /// Adds a route segment (test compatibility method).
    /// </summary>
    /// <param name="routeCode">The route code.</param>
    /// <param name="routeText">The route text.</param>
    /// <param name="site">The administration site.</param>
    public void AddRoute(string routeCode, string? routeText = null, string? site = null)
    {
        var route = new RXRSegment();
        route.Route.SetComponents(routeCode, routeText ?? routeCode);
        if (!string.IsNullOrEmpty(site))
            route.AdministrationSite.SetValue(site);
        AddSegment(route);
    }

    /// <summary>
    /// Returns a display string for this message (test compatibility method).
    /// </summary>
    /// <returns>A formatted display string.</returns>
    public string ToDisplayString()
    {
        var patientName = PatientIdentification?.PatientName?.DisplayName ?? "Unknown Patient";
        var patientId = PatientIdentification?.PatientIdentifierList?.IdNumber ?? "Unknown";
        var medication = PharmacyOrder?.GiveCode?.Text ?? "Unknown Medication";
        
        return $"RDE Pharmacy Order - {patientName} (ID: {patientId}) - {medication}";
    }

    /// <summary>
    /// Creates a comprehensive RDE message with full medication details.
    /// </summary>
    /// <param name="sendingApp">The sending application.</param>
    /// <param name="sendingFacility">The sending facility.</param>
    /// <param name="receivingApp">The receiving application.</param>
    /// <param name="receivingFacility">The receiving facility.</param>
    /// <param name="patientId">The patient ID.</param>
    /// <param name="patientFirstName">Patient's first name.</param>
    /// <param name="patientLastName">Patient's last name.</param>
    /// <param name="drugCode">The drug code (NDC).</param>
    /// <param name="drugName">The drug name.</param>
    /// <param name="strength">Drug strength.</param>
    /// <param name="strengthUnits">Strength units.</param>
    /// <param name="dosageForm">Dosage form.</param>
    /// <param name="dispenseQty">Quantity to dispense.</param>
    /// <param name="refills">Number of refills.</param>
    /// <param name="sig">Directions for use.</param>
    /// <param name="orderingProvider">The ordering provider ID.</param>
    /// <param name="isProduction">Whether this is a production message.</param>
    /// <returns>A configured RDE message.</returns>
    public static RDEMessage CreateComprehensive(
        string sendingApp,
        string sendingFacility,
        string receivingApp,
        string receivingFacility,
        string patientId,
        string patientFirstName,
        string patientLastName,
        string drugCode,
        string drugName,
        decimal strength,
        string strengthUnits,
        string dosageForm,
        decimal dispenseQty,
        int refills,
        string sig,
        string? orderingProvider = null,
        bool isProduction = false)
    {
        var message = CreateBasic(
            sendingApp,
            sendingFacility,
            receivingApp,
            receivingFacility,
            patientId,
            patientFirstName,
            patientLastName,
            drugCode,
            drugName,
            orderingProvider,
            isProduction);

        message.SetMedicationDetails(
            drugCode,
            drugName,
            strength,
            strengthUnits,
            dosageForm,
            dispenseQty,
            strengthUnits,
            refills,
            sig);

        return message;
    }
}
