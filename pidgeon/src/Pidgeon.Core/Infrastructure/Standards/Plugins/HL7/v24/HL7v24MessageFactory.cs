// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Pidgeon.Core.Domain.Clinical.Entities;
using Pidgeon.Core.Application.Interfaces.Standards;
using Pidgeon.Core;

namespace Pidgeon.Core.Standards.HL7v24Plugin;

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
    public Result<IStandardMessage> CreatePatientAdmission(Patient patient, MessageOptions? options = null)
    {
        // TODO: Implement HL7 v2.4 specific patient admission message creation
        return Error.Create("NOT_IMPLEMENTED", "HL7v24 patient admission creation not yet implemented", "HL7v24MessageFactory");
    }

    /// <summary>
    /// Creates an HL7 ADT^A03 (Patient Discharge) message for v2.4.
    /// </summary>
    public Result<IStandardMessage> CreatePatientDischarge(Patient patient, Encounter encounter, MessageOptions? options = null)
    {
        // TODO: Implement HL7 v2.4 specific patient discharge message creation
        return Error.Create("NOT_IMPLEMENTED", "HL7v24 patient discharge creation not yet implemented", "HL7v24MessageFactory");
    }

    /// <summary>
    /// Creates an HL7 RDE^O11 (Pharmacy/Treatment Encoded Order) message for v2.4.
    /// </summary>
    public Result<IStandardMessage> CreatePrescription(Prescription prescription, MessageOptions? options = null)
    {
        // TODO: Implement HL7 v2.4 specific prescription message creation
        return Error.Create("NOT_IMPLEMENTED", "HL7v24 prescription creation not yet implemented", "HL7v24MessageFactory");
    }

    /// <summary>
    /// Creates an HL7 ORM^O01 (General Order) message for lab orders in v2.4.
    /// </summary>
    public Result<IStandardMessage> CreateLabOrder(object order, MessageOptions? options = null)
    {
        // TODO: Implement HL7 v2.4 specific lab order message creation
        return Error.Create("NOT_IMPLEMENTED", "HL7v24 lab order creation not yet implemented", "HL7v24MessageFactory");
    }

    /// <summary>
    /// Creates an HL7 ORU^R01 (Unsolicited Observation) message for lab results in v2.4.
    /// </summary>
    public Result<IStandardMessage> CreateLabResult(object result, MessageOptions? options = null)
    {
        // TODO: Implement HL7 v2.4 specific lab result message creation
        return Error.Create("NOT_IMPLEMENTED", "HL7v24 lab result creation not yet implemented", "HL7v24MessageFactory");
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