// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Pidgeon.Core.Application.Interfaces.Standards;
using Pidgeon.Core;
using Pidgeon.Core.Application.DTOs;
using Pidgeon.Core.Domain.Messaging.HL7v2.Messages;
using Pidgeon.Core.Domain.Messaging.HL7v2.Segments;

namespace Pidgeon.Core.Infrastructure.Standards.Plugins.HL7.v24;

/// <summary>
/// Message factory implementation for HL7 v2.4 standard.
/// Creates HL7 v2.4 compliant messages using plugin-provided metadata.
/// </summary>
public class HL7v24MessageFactory : IStandardMessageFactory
{
    private readonly IStandardPlugin _plugin;

    public HL7v24MessageFactory(IStandardPlugin plugin)
    {
        _plugin = plugin ?? throw new ArgumentNullException(nameof(plugin));
    }

    /// <summary>
    /// Creates an HL7 ADT^A01 (Patient Admission) message for v2.4.
    /// </summary>
    public Result<IStandardMessage> CreatePatientAdmission(PatientDto patient, MessageOptions? options = null)
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

            // HL7 v2.4-specific message construction
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
                
                // Set HL7 v2.4 specific version
                msh.VersionId.SetValue("2.4");
            }

            // Populate PID segment from patient DTO
            if (message.PID == null)
            {
                return Error.Create("ADT_CREATION_FAILED", "PID segment was not initialized in ADT message", "HL7v24MessageFactory");
            }
            
            if (patient == null)
            {
                return Error.Create("ADT_CREATION_FAILED", "Patient DTO is null", "HL7v24MessageFactory");
            }
            
            var pidResult = message.PID.PopulateFromPatient(patient);
            if (pidResult.IsFailure)
                return Error.Create("ADT_PATIENT_POPULATION_FAILED",
                    $"Failed to populate patient information: {pidResult.Error.Message}", "HL7v24MessageFactory");

            return Result<IStandardMessage>.Success(message);
        }
        catch (Exception ex)
        {
            return Error.Create("ADT_CREATION_FAILED", $"Failed to create HL7v24 ADT message: {ex.Message}", "HL7v24MessageFactory");
        }
    }

    /// <summary>
    /// Creates an HL7 ADT^A03 (Patient Discharge) message for v2.4.
    /// </summary>
    public Result<IStandardMessage> CreatePatientDischarge(PatientDto patient, EncounterDto encounter, MessageOptions? options = null)
    {
        var result = CreatePatientAdmission(patient, options);
        if (result.IsSuccess && result.Value is ADTMessage adtMessage)
        {
            // Change trigger event to A03 (Discharge)
            adtMessage.MSH?.SetMessageType("ADT", "A03");
            
            // Populate PV1 segment from encounter DTO for HL7 v2.4
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
    /// Creates an HL7 RDE^O11 (Pharmacy/Treatment Encoded Order) message for v2.4.
    /// </summary>
    public Result<IStandardMessage> CreatePrescription(PrescriptionDto prescription, MessageOptions? options = null)
    {
        try
        {
            var sendingApp = options?.SendingApplication ?? GetDefaultSendingApp();
            var receivingApp = options?.ReceivingApplication ?? "UNKNOWN";
            
            // Use existing RDEMessage factory method with DTO
            var rdeResult = RDEMessage.CreatePharmacyOrder(
                prescription,
                sendingApplication: sendingApp,
                sendingFacility: options?.SendingFacility,
                receivingApplication: receivingApp,
                receivingFacility: options?.ReceivingFacility);
                
            if (rdeResult.IsFailure)
                return Result<IStandardMessage>.Failure(rdeResult.Error);

            var message = rdeResult.Value;

            // Configure HL7 v2.4 specific properties
            message.MessageControlId = options?.MessageControlId ?? Guid.NewGuid().ToString();
            message.Timestamp = options?.Timestamp ?? DateTime.UtcNow;
            message.SendingSystem = sendingApp;
            message.ReceivingSystem = receivingApp;
            message.Standard = _plugin.StandardName;
            message.Version = _plugin.StandardVersion.ToString();
            
            // Set HL7 v2.4 specific version in MSH
            if (message.MSH != null)
            {
                message.MSH.VersionId.SetValue("2.4");
            }

            return Result<IStandardMessage>.Success(message);
        }
        catch (Exception ex)
        {
            return Error.Create("RDE_CREATION_FAILED", $"Failed to create HL7v24 RDE message: {ex.Message}", "HL7v24MessageFactory");
        }
    }

    /// <summary>
    /// Creates an HL7 ORM^O01 (General Order) message for lab orders in v2.4.
    /// </summary>
    public Result<IStandardMessage> CreateLabOrder(object order, MessageOptions? options = null)
    {
        // Lab order creation requires LabOrder domain entity which doesn't exist yet
        // This method is implemented as a placeholder for future development
        return Error.Create("DOMAIN_ENTITY_MISSING", 
            "Lab order creation requires LabOrder domain entity - not yet implemented in clinical domain", 
            "HL7v24MessageFactory");
    }

    /// <summary>
    /// Creates an HL7 ORU^R01 (Unsolicited Observation) message for lab results in v2.4.
    /// </summary>
    public Result<IStandardMessage> CreateLabResult(object result, MessageOptions? options = null)
    {
        // Lab result creation requires LabResult domain entity which doesn't exist yet
        // This method is implemented as a placeholder for future development
        return Error.Create("DOMAIN_ENTITY_MISSING", 
            "Lab result creation requires LabResult domain entity - not yet implemented in clinical domain", 
            "HL7v24MessageFactory");
    }

    /// <summary>
    /// Checks if this factory supports creating the specified message type.
    /// </summary>
    public bool SupportsMessageType(string messageType)
    {
        var supportedTypes = new[] { "ADT", "ORM", "RDE", "ORU", "ACK", "QBP", "RSP" };
        return supportedTypes.Contains(messageType, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Creates a custom HL7 v2.4 message for standard-specific scenarios.
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
                $"Failed to create custom message '{messageType}': {ex.Message}", "HL7v24MessageFactory");
        }
    }

    private string GetDefaultSendingApp() => $"PIDGEON-{_plugin.StandardName}v{_plugin.StandardVersion}";
}