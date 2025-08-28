// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Pidgeon.Core.Domain.Clinical.Entities;
using Pidgeon.Core.Domain.Messaging.HL7v2.Segments;
using Pidgeon.Core.Standards.Common;

namespace Pidgeon.Core.Domain.Messaging.HL7v2.Messages;

/// <summary>
/// RDE - Pharmacy/Treatment Encoded Order Message.
/// Used to communicate pharmacy orders and prescriptions between systems.
/// Message structure: MSH PID ORC RXE [RXR] [RXC]
/// </summary>
public class RDEMessage : HL7Message
{
    public override required HL7MessageType MessageType { get; set; } = HL7MessageType.Common.RDE_O11;

    /// <summary>
    /// Gets the PID (Patient Identification) segment.
    /// </summary>
    public PIDSegment PID => GetSegment<PIDSegment>()!;

    /// <summary>
    /// Gets the ORC (Common Order) segment.
    /// </summary>
    public ORCSegment? ORC => GetSegment<ORCSegment>();

    /// <summary>
    /// Gets the RXE (Pharmacy/Treatment Encoded Order) segment.
    /// </summary>
    public RXESegment? RXE => GetSegment<RXESegment>();

    /// <summary>
    /// Gets the RXR (Pharmacy/Treatment Route) segment.
    /// </summary>
    public RXRSegment? RXR => GetSegment<RXRSegment>();

    /// <summary>
    /// Gets the trigger event (O01 for order).
    /// </summary>
    public string? TriggerEvent => MSH?.GetMessageTypeComponents()?.TriggerEvent;

    public override void InitializeMessage()
    {
        // MSH - Message Header (required for all HL7 messages)
        var msh = MSHSegment.Create(
            messageType: "RDE^O01",
            messageControlId: GenerateControlId()
        );
        AddSegment(msh);

        // PID - Patient Identification (required for RDE messages)
        AddSegment(new PIDSegment());
        
        // ORC - Common Order (required for order messages)
        AddSegment(new ORCSegment());
        
        // RXE - Pharmacy/Treatment Encoded Order (required for RDE)
        AddSegment(new RXESegment());
        
        // RXR - Pharmacy/Treatment Route (optional but common)
        AddSegment(new RXRSegment());
    }

    /// <summary>
    /// Creates an RDE^O01 (Pharmacy Order) message from domain objects.
    /// </summary>
    /// <param name="prescription">The prescription to encode</param>
    /// <param name="sendingApplication">Sending application name</param>
    /// <param name="sendingFacility">Sending facility name</param>
    /// <param name="receivingApplication">Receiving application name</param>
    /// <param name="receivingFacility">Receiving facility name</param>
    /// <returns>A result containing the populated RDE message</returns>
    public static Result<RDEMessage> CreatePharmacyOrder(
        Prescription prescription,
        string? sendingApplication = null,
        string? sendingFacility = null,
        string? receivingApplication = null,
        string? receivingFacility = null)
    {
        try
        {
            var message = new RDEMessage();

            // Configure MSH segment
            var msh = message.MSH;
            msh.SetMessageType("RDE", "O01");

            if (sendingApplication != null)
                msh.SendingApplication.SetValue(sendingApplication);
            if (sendingFacility != null)
                msh.SendingFacility.SetValue(sendingFacility);
            if (receivingApplication != null)
                msh.ReceivingApplication.SetValue(receivingApplication);
            if (receivingFacility != null)
                msh.ReceivingFacility.SetValue(receivingFacility);

            // Populate PID segment from patient
            var pidResult = message.PID.PopulateFromPatient(prescription.Patient);
            if (pidResult.IsFailure)
                return Error.Create("RDE_PATIENT_POPULATION_FAILED", 
                    $"Failed to populate patient information: {pidResult.Error.Message}", "RDEMessage");

            // Populate ORC segment (Common Order)
            if (message.ORC != null)
            {
                var orcResult = message.ORC.PopulateFromPrescription(prescription);
                if (orcResult.IsFailure)
                    return Error.Create("RDE_ORDER_POPULATION_FAILED",
                        $"Failed to populate order information: {orcResult.Error.Message}", "RDEMessage");
            }

            // Populate RXE segment (Pharmacy Encoded Order)
            if (message.RXE != null)
            {
                var rxeResult = message.RXE.PopulateFromPrescription(prescription);
                if (rxeResult.IsFailure)
                    return Error.Create("RDE_PRESCRIPTION_POPULATION_FAILED",
                        $"Failed to populate prescription information: {rxeResult.Error.Message}", "RDEMessage");
            }

            // Populate RXR segment (Route)
            if (message.RXR != null)
            {
                var route = prescription.Dosage.Route.ToString();
                var rxrResult = message.RXR.SetRoute(route);
                if (rxrResult.IsFailure)
                    return Error.Create("RDE_ROUTE_POPULATION_FAILED",
                        $"Failed to populate route information: {rxrResult.Error.Message}", "RDEMessage");
            }

            return Result<RDEMessage>.Success(message);
        }
        catch (Exception ex)
        {
            return Error.Create("RDE_CREATION_FAILED", $"Failed to create RDE message: {ex.Message}", "RDEMessage");
        }
    }

    protected override HL7Segment? CreateSegmentFromId(string segmentId)
    {
        return segmentId switch
        {
            "PID" => new PIDSegment(),
            "ORC" => new ORCSegment(),
            "RXE" => new RXESegment(),
            "RXR" => new RXRSegment(),
            _ => base.CreateSegmentFromId(segmentId)
        };
    }

    protected override Result<HL7Message> ValidateMessageStructure()
    {
        // Call base validation first (ensures MSH is present)
        var baseResult = base.ValidateMessageStructure();
        if (baseResult.IsFailure)
            return baseResult;

        // RDE-specific validation: must have PID segment
        if (!Segments.Any(s => s is PIDSegment))
            return Error.Validation("RDE message must contain a PID segment", "MessageStructure");

        // RDE-specific validation: must have RXE segment
        if (!Segments.Any(s => s is RXESegment))
            return Error.Validation("RDE message must contain an RXE segment", "MessageStructure");

        // Ensure MSH has correct message type
        var messageTypeComponents = MSH?.GetMessageTypeComponents();
        if (messageTypeComponents?.MessageCode != "RDE")
            return Error.Validation("Message type must be RDE for RDE messages", "MessageStructure");

        return Result<HL7Message>.Success(this);
    }


    /// <summary>
    /// Generates a unique message control ID.
    /// </summary>
    private static string GenerateControlId()
    {
        return $"RDE{DateTime.UtcNow:yyyyMMddHHmmss}{Random.Shared.Next(1000, 9999)}";
    }

    /// <summary>
    /// Gets a display representation of the RDE message.
    /// </summary>
    /// <returns>Human-readable message summary</returns>
    public override string GetDisplaySummary()
    {
        var patientInfo = PID?.GetPatientDisplay() ?? "Unknown Patient";
        var drugInfo = RXE?.GetDrugDisplay() ?? "Unknown Drug";
        var controlId = MSH?.MessageControlId.Value ?? "Unknown";
        
        return $"Pharmacy Order: {drugInfo} for {patientInfo} [Control ID: {controlId}]";
    }
}