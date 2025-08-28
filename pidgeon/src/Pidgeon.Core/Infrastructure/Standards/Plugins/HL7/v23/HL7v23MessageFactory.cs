// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Pidgeon.Core.Domain.Clinical.Entities;
using Pidgeon.Core.Domain.Messaging.HL7v2.Messages;
using Pidgeon.Core.Domain.Messaging.HL7v2.Segments;
using Pidgeon.Core.Infrastructure.Standards.Abstractions;
using Pidgeon.Core.Standards.Common;
using Pidgeon.Core;

namespace Pidgeon.Core.Infrastructure.Standards.Plugins.HL7.v23;

/// <summary>
/// Message factory implementation for HL7 v2.3 standard.
/// Creates HL7 v2.3 compliant messages using plugin-provided metadata.
/// </summary>
public class HL7v23MessageFactory : IStandardMessageFactory
{
    private readonly IStandardPlugin _plugin;

    public HL7v23MessageFactory(IStandardPlugin plugin)
    {
        _plugin = plugin ?? throw new ArgumentNullException(nameof(plugin));
    }

    /// <summary>
    /// Creates an HL7 ADT^A01 (Patient Admission) message.
    /// </summary>
    public Result<IStandardMessage> CreatePatientAdmission(Patient patient, MessageOptions? options = null)
    {
        try
        {
            var message = new ADTMessage()
            {
                MessageControlId = options?.MessageControlId ?? Guid.NewGuid().ToString(),
                Timestamp = options?.Timestamp ?? DateTime.UtcNow,
                SendingSystem = options?.SendingApplication ?? GetDefaultSendingApp(),
                ReceivingSystem = options?.ReceivingApplication ?? "UNKNOWN",
                Standard = _plugin.StandardName,
                Version = _plugin.StandardVersion.ToString(),
                MessageType = HL7MessageType.Common.ADT_A01
            };

            // HL7-specific message construction
            message.InitializeMessage();

            // Configure MSH segment with plugin knowledge
            var msh = message.MSH;
            if (msh != null)
            {
                msh.SetMessageType("ADT", "A01");
                msh.SendingApplication.SetValue(message.SendingSystem);
                msh.ReceivingApplication.SetValue(message.ReceivingSystem);
                
                if (options?.SendingFacility != null)
                    msh.SendingFacility.SetValue(options.SendingFacility);
                if (options?.ReceivingFacility != null)
                    msh.ReceivingFacility.SetValue(options.ReceivingFacility);
            }

            // Populate PID segment from patient
            var pidResult = message.PID.PopulateFromPatient(patient);
            if (pidResult.IsFailure)
                return Error.Create("ADT_PATIENT_POPULATION_FAILED",
                    $"Failed to populate patient information: {pidResult.Error.Message}", "HL7v23MessageFactory");

            return Result<IStandardMessage>.Success(message);
        }
        catch (Exception ex)
        {
            return Error.Create("ADT_CREATION_FAILED", $"Failed to create ADT message: {ex.Message}", "HL7v23MessageFactory");
        }
    }

    /// <summary>
    /// Creates an HL7 ADT^A03 (Patient Discharge) message.
    /// </summary>
    public Result<IStandardMessage> CreatePatientDischarge(Patient patient, Encounter encounter, MessageOptions? options = null)
    {
        var result = CreatePatientAdmission(patient, options);
        if (result.IsSuccess && result.Value is ADTMessage adtMessage)
        {
            // Change trigger event to A03 (Discharge)
            adtMessage.MSH?.SetMessageType("ADT", "A03");
            
            // Populate PV1 segment from encounter
            if (adtMessage.PV1 != null)
            {
                var pv1 = PV1Segment.Create(encounter);
                adtMessage.Segments.RemoveAll(s => s is PV1Segment);
                adtMessage.AddSegment(pv1);
            }
        }
        return result;
    }

    /// <summary>
    /// Creates an HL7 RDE^O11 (Pharmacy/Treatment Encoded Order) message.
    /// </summary>
    public Result<IStandardMessage> CreatePrescription(Prescription prescription, MessageOptions? options = null)
    {
        try
        {
            var rdeResult = _plugin.CreateMessage("RDE");
            if (rdeResult.IsFailure)
                return rdeResult;

            var message = rdeResult.Value as RDEMessage;
            if (message == null)
                return Error.Create("RDE_CREATION_FAILED", "Created message is not an RDEMessage", "HL7v23MessageFactory");

            // Configure message properties from plugin
            message.MessageControlId = options?.MessageControlId ?? Guid.NewGuid().ToString();
            message.Timestamp = options?.Timestamp ?? DateTime.UtcNow;
            message.SendingSystem = options?.SendingApplication ?? GetDefaultSendingApp();
            message.ReceivingSystem = options?.ReceivingApplication ?? "UNKNOWN";
            message.Standard = _plugin.StandardName;
            message.Version = _plugin.StandardVersion.ToString();

            // TODO: Implement prescription population logic
            // This would populate RXE, RXR, PID segments from prescription data

            return Result<IStandardMessage>.Success(message);
        }
        catch (Exception ex)
        {
            return Error.Create("RDE_CREATION_FAILED", $"Failed to create RDE message: {ex.Message}", "HL7v23MessageFactory");
        }
    }

    /// <summary>
    /// Creates an HL7 ORM^O01 (General Order) message for lab orders.
    /// </summary>
    public Result<IStandardMessage> CreateLabOrder(object order, MessageOptions? options = null)
    {
        // TODO: Implement lab order message creation
        // This would create ORM^O01 messages with OBR segments
        return Error.Create("NOT_IMPLEMENTED", "Lab order creation not yet implemented for HL7v23", "HL7v23MessageFactory");
    }

    /// <summary>
    /// Creates an HL7 ORU^R01 (Unsolicited Observation) message for lab results.
    /// </summary>
    public Result<IStandardMessage> CreateLabResult(object result, MessageOptions? options = null)
    {
        // TODO: Implement lab result message creation  
        // This would create ORU^R01 messages with OBX segments
        return Error.Create("NOT_IMPLEMENTED", "Lab result creation not yet implemented for HL7v23", "HL7v23MessageFactory");
    }

    /// <summary>
    /// Checks if this factory supports creating the specified message type.
    /// </summary>
    public bool SupportsMessageType(string messageType)
    {
        var supportedTypes = new[] { "ADT^A01", "ADT^A03", "ADT^A08", "RDE^O11", "ADT", "RDE" };
        return supportedTypes.Contains(messageType, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Creates a custom HL7 v2.3 message for standard-specific scenarios.
    /// </summary>
    public Result<IStandardMessage> CreateCustomMessage(string messageType, object data, MessageOptions? options = null)
    {
        try
        {
            var createResult = _plugin.CreateMessage(messageType);
            if (createResult.IsFailure)
                return createResult;

            var message = createResult.Value;
            
            // Configure message properties from plugin
            message.MessageControlId = options?.MessageControlId ?? Guid.NewGuid().ToString();
            message.Timestamp = options?.Timestamp ?? DateTime.UtcNow;
            message.SendingSystem = options?.SendingApplication ?? GetDefaultSendingApp();
            message.ReceivingSystem = options?.ReceivingApplication ?? "UNKNOWN";
            message.Standard = _plugin.StandardName;
            message.Version = _plugin.StandardVersion.ToString();

            return Result<IStandardMessage>.Success(message);
        }
        catch (Exception ex)
        {
            return Error.Create("CUSTOM_MESSAGE_CREATION_FAILED", 
                $"Failed to create custom message '{messageType}': {ex.Message}", "HL7v23MessageFactory");
        }
    }

    private string GetDefaultSendingApp() => $"PIDGEON-{_plugin.StandardName}v{_plugin.StandardVersion}";
}