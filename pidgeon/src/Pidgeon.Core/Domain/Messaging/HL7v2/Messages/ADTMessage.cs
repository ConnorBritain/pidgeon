// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Pidgeon.Core;
using Pidgeon.Core.Application.DTOs;
using Pidgeon.Core.Domain.Messaging.HL7v2.Segments;
using Pidgeon.Core.Application.Common;

namespace Pidgeon.Core.Domain.Messaging.HL7v2.Messages;

/// <summary>
/// ADT - Admission, Discharge, Transfer Message.
/// Used for patient registration, admission, discharge, and transfer events.
/// </summary>
public class ADTMessage : HL7Message
{
    public override HL7MessageType MessageType { get; set; } = HL7MessageType.Common.ADT_A01;

    /// <summary>
    /// Gets the PID (Patient Identification) segment.
    /// </summary>
    public PIDSegment PID => GetSegment<PIDSegment>("PID")!;

    /// <summary>
    /// Gets the PV1 (Patient Visit) segment.
    /// </summary>
    public PV1Segment? PV1 => GetSegment<PV1Segment>("PV1");

    /// <summary>
    /// Gets the trigger event (e.g., A01, A03, A08).
    /// </summary>
    public string? TriggerEvent => MSH?.GetMessageTypeComponents()?.TriggerEvent;

    public override void InitializeMessage()
    {
        // MSH - Message Header (required for all HL7 messages)
        var msh = MSHSegment.Create(
            messageType: "ADT^A01",
            messageControlId: GenerateControlId()
        );
        AddSegment(msh);

        // PID - Patient Identification (required for ADT messages)
        AddSegment(new PIDSegment());

        // PV1 - Patient Visit (recommended for ADT messages to provide encounter context)
        AddSegment(new PV1Segment());
    }

    /// <summary>
    /// Creates an ADT^A01 (Patient Admission) message from domain objects.
    /// </summary>
    /// <param name="patient">The patient being admitted</param>
    /// <param name="encounter">The encounter/admission details</param>
    /// <param name="sendingApplication">Sending application name</param>
    /// <param name="sendingFacility">Sending facility name</param>
    /// <param name="receivingApplication">Receiving application name</param>
    /// <param name="receivingFacility">Receiving facility name</param>
    /// <returns>A result containing the populated ADT message</returns>
    public static Result<ADTMessage> CreateAdmission(
        PatientDto patient,
        EncounterDto? encounter = null,
        string? sendingApplication = null,
        string? sendingFacility = null,
        string? receivingApplication = null,
        string? receivingFacility = null)
    {
        try
        {
            // TODO: Replace with proper factory pattern - this is temporary for CS9035 fix
            var message = new ADTMessage()
            {
                MessageControlId = Guid.NewGuid().ToString(),
                Timestamp = DateTime.UtcNow,
                SendingSystem = sendingApplication ?? "PIDGEON",
                ReceivingSystem = receivingApplication ?? "UNKNOWN",
                Standard = "HL7", // TODO: Should come from plugin
                Version = "2.3",  // TODO: Should come from plugin
                MessageType = HL7MessageType.Common.ADT_A01
            };

            // Configure MSH segment
            var msh = message.MSH;
            msh.SetMessageType("ADT", "A01");

            if (sendingApplication != null)
                msh.SendingApplication.SetValue(sendingApplication);
            if (sendingFacility != null)
                msh.SendingFacility.SetValue(sendingFacility);
            if (receivingApplication != null)
                msh.ReceivingApplication.SetValue(receivingApplication);
            if (receivingFacility != null)
                msh.ReceivingFacility.SetValue(receivingFacility);

            // Populate PID segment from patient
            var pidResult = message.PID.PopulateFromPatient(patient);
            if (pidResult.IsFailure)
                return Result<ADTMessage>.Failure($"Failed to populate patient information: {pidResult.Error}");

            // Populate PV1 segment from encounter if provided
            if (encounter != null && message.PV1 != null)
            {
                var pv1 = PV1Segment.Create(encounter); // Provider will be taken from encounter.Provider
                message.Segments.RemoveAll(s => s is PV1Segment);
                message.AddSegment(pv1); // Add populated PV1
            }

            return Result<ADTMessage>.Success(message);
        }
        catch (Exception ex)
        {
            return Result<ADTMessage>.Failure($"Failed to create ADT message: {ex.Message}");
        }
    }

    /// <summary>
    /// Creates an ADT^A03 (Patient Discharge) message.
    /// </summary>
    /// <param name="patient">The patient being discharged</param>
    /// <param name="encounter">The encounter/discharge details</param>
    /// <param name="sendingApplication">Sending application name</param>
    /// <param name="sendingFacility">Sending facility name</param>
    /// <param name="receivingApplication">Receiving application name</param>
    /// <param name="receivingFacility">Receiving facility name</param>
    /// <returns>A result containing the populated ADT message</returns>
    public static Result<ADTMessage> CreateDischarge(
        PatientDto patient,
        EncounterDto? encounter = null,
        string? sendingApplication = null,
        string? sendingFacility = null,
        string? receivingApplication = null,
        string? receivingFacility = null)
    {
        var result = CreateAdmission(patient, encounter, sendingApplication, sendingFacility, receivingApplication, receivingFacility);
        if (result.IsSuccess)
        {
            // Change trigger event to A03 (Discharge)
            result.Value.MSH.SetMessageType("ADT", "A03");
        }
        return result;
    }

    /// <summary>
    /// Creates an ADT^A08 (Update Patient Information) message.
    /// </summary>
    /// <param name="patient">The patient with updated information</param>
    /// <param name="encounter">The encounter details</param>
    /// <param name="sendingApplication">Sending application name</param>
    /// <param name="sendingFacility">Sending facility name</param>
    /// <param name="receivingApplication">Receiving application name</param>
    /// <param name="receivingFacility">Receiving facility name</param>
    /// <returns>A result containing the populated ADT message</returns>
    public static Result<ADTMessage> CreatePatientUpdate(
        PatientDto patient,
        EncounterDto? encounter = null,
        string? sendingApplication = null,
        string? sendingFacility = null,
        string? receivingApplication = null,
        string? receivingFacility = null)
    {
        var result = CreateAdmission(patient, encounter, sendingApplication, sendingFacility, receivingApplication, receivingFacility);
        if (result.IsSuccess)
        {
            // Change trigger event to A08 (Update Patient Information)
            result.Value.MSH.SetMessageType("ADT", "A08");
        }
        return result;
    }

    protected override HL7Segment? CreateSegmentFromId(string segmentId)
    {
        return segmentId switch
        {
            "MSH" => new MSHSegment(),
            "PID" => new PIDSegment(),
            "PV1" => new PV1Segment(),
            _ => base.CreateSegmentFromId(segmentId)
        };
    }

    protected override Result<HL7Message> ValidateMessageStructure()
    {
        // Call base validation first (ensures MSH is present)
        var baseResult = base.ValidateMessageStructure();
        if (baseResult.IsFailure)
            return baseResult;

        // ADT-specific validation: must have PID segment
        if (!Segments.Any(s => s is PIDSegment))
            return Error.Validation("ADT message must contain a PID segment", "MessageStructure");

        // Ensure MSH has correct message type
        var messageTypeComponents = MSH?.GetMessageTypeComponents();
        if (messageTypeComponents?.MessageCode != "ADT")
            return Error.Validation("Message type must be ADT for ADT messages", "MessageStructure");

        return Result<HL7Message>.Success(this);
    }


    /// <summary>
    /// Generates a unique message control ID.
    /// </summary>
    private static string GenerateControlId()
    {
        return $"ADT{DateTime.UtcNow:yyyyMMddHHmmss}{Random.Shared.Next(1000, 9999)}";
    }

    /// <summary>
    /// Gets a display representation of the ADT message.
    /// </summary>
    /// <returns>Human-readable message summary</returns>
    public override string GetDisplaySummary()
    {
        var triggerEvent = TriggerEvent;
        var eventDescription = triggerEvent switch
        {
            "A01" => "Patient Admission",
            "A02" => "Patient Transfer", 
            "A03" => "Patient Discharge",
            "A04" => "Patient Registration",
            "A08" => "Patient Update",
            _ => $"ADT Event {triggerEvent}"
        };

        var patientInfo = PID?.GetPatientDisplay() ?? "Unknown Patient";
        var controlId = MSH?.MessageControlId.Value ?? "Unknown";
        
        return $"{eventDescription}: {patientInfo} [Control ID: {controlId}]";
    }
}