// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Microsoft.Extensions.Logging;
using Pidgeon.Core.Domain.Clinical.Entities;
using Pidgeon.Core.Generation;
using Pidgeon.Core.Infrastructure.Standards.HL7.v23.Segments;

namespace Pidgeon.Core.Infrastructure.Standards.HL7.v23.MessageComposers;

/// <summary>
/// Composes ADT (Admission, Discharge, Transfer) message family.
/// Handles ADT^A01 (Admit), ADT^A03 (Discharge), ADT^A08 (Update) messages.
/// </summary>
public class ADTMessageComposer
{
    private readonly MSHSegmentBuilder _mshBuilder;
    private readonly PIDSegmentBuilder _pidBuilder;
    private readonly EVNSegmentBuilder _evnBuilder;
    private readonly PV1SegmentBuilder _pv1Builder;
    private readonly ILogger<ADTMessageComposer> _logger;

    public ADTMessageComposer(
        MSHSegmentBuilder mshBuilder,
        PIDSegmentBuilder pidBuilder,
        EVNSegmentBuilder evnBuilder,
        PV1SegmentBuilder pv1Builder,
        ILogger<ADTMessageComposer> logger)
    {
        _mshBuilder = mshBuilder ?? throw new ArgumentNullException(nameof(mshBuilder));
        _pidBuilder = pidBuilder ?? throw new ArgumentNullException(nameof(pidBuilder));
        _evnBuilder = evnBuilder ?? throw new ArgumentNullException(nameof(evnBuilder));
        _pv1Builder = pv1Builder ?? throw new ArgumentNullException(nameof(pv1Builder));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Composes ADT^A01 (Admit/Visit Notification) message.
    /// </summary>
    public Result<string> ComposeA01(Patient patient, Encounter encounter, GenerationOptions options)
    {
        return ComposeADT("A01", "ADT^A01^ADT_A01", patient, encounter, options);
    }

    /// <summary>
    /// Composes ADT^A03 (Discharge Patient) message.
    /// </summary>
    public Result<string> ComposeA03(Patient patient, Encounter encounter, GenerationOptions options)
    {
        return ComposeADT("A03", "ADT^A03^ADT_A03", patient, encounter, options);
    }

    /// <summary>
    /// Composes ADT^A08 (Update Patient Information) message.
    /// </summary>
    public Result<string> ComposeA08(Patient patient, Encounter encounter, GenerationOptions options)
    {
        return ComposeADT("A08", "ADT^A08^ADT_A08", patient, encounter, options);
    }

    /// <summary>
    /// Common ADT message composition logic.
    /// All ADT messages have the same structure: MSH, EVN, PID, PV1
    /// </summary>
    private Result<string> ComposeADT(string eventCode, string messageType, Patient patient, Encounter encounter, GenerationOptions options)
    {
        try
        {
            if (patient == null)
                return Result<string>.Failure($"Patient is required for ADT^{eventCode} message");

            var segments = new List<string>();
            
            // MSH segment - Message Header
            var mshInput = new MSHInput
            {
                MessageType = messageType,
                TriggerEvent = eventCode
            };
            segments.Add(_mshBuilder.Build(mshInput, 1, options));
            
            // EVN segment - Event Type
            var evnInput = new EVNInput
            {
                EventTypeCode = eventCode,
                RecordedDateTime = DateTime.Now
            };
            segments.Add(_evnBuilder.Build(evnInput, 1, options));
            
            // PID segment - Patient Identification
            segments.Add(_pidBuilder.Build(patient, 1, options));
            
            // PV1 segment - Patient Visit
            var encounterToUse = encounter ?? CreateDefaultEncounter(patient);
            segments.Add(_pv1Builder.Build(encounterToUse, 1, options));
            
            var message = string.Join("\r\n", segments);
            
            _logger.LogDebug("Composed HL7 v2.3 ADT^{EventCode} message for patient {PatientId}", eventCode, patient.Id);
            return Result<string>.Success(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to compose ADT^{EventCode} message", eventCode);
            return Result<string>.Failure($"ADT^{eventCode} composition failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Creates a default encounter when none is provided.
    /// </summary>
    private static Encounter CreateDefaultEncounter(Patient patient)
    {
        var defaultProvider = new Provider
        {
            Id = "DOC001",
            Name = PersonName.Create("Smith", "John"),
            Degree = "MD",
            Specialty = "Internal Medicine",
            NpiNumber = "1234567890"
        };

        return new Encounter
        {
            Id = Guid.NewGuid().ToString("N")[..10].ToUpperInvariant(),
            Patient = patient,
            Provider = defaultProvider,
            Type = EncounterType.Inpatient,
            Status = EncounterStatus.Finished,
            StartTime = DateTime.Now,
            Location = "WARD^101^A",
            Priority = EncounterPriority.Routine
        };
    }
}