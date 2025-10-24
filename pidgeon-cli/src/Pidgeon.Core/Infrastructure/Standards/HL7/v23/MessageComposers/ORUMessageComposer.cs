// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Microsoft.Extensions.Logging;
using Pidgeon.Core.Domain.Clinical.Entities;
using Pidgeon.Core.Generation;
using Pidgeon.Core.Infrastructure.Standards.HL7.v23.Segments;

namespace Pidgeon.Core.Infrastructure.Standards.HL7.v23.MessageComposers;

/// <summary>
/// Composes ORU (Observation Result) message family.
/// Handles ORU^R01 (Lab Results) messages with proper segment sequence.
/// </summary>
public class ORUMessageComposer
{
    private readonly MSHSegmentBuilder _mshBuilder;
    private readonly PIDSegmentBuilder _pidBuilder;
    private readonly OBRSegmentBuilder _obrBuilder;
    private readonly OBXSegmentBuilder _obxBuilder;
    private readonly ILogger<ORUMessageComposer> _logger;

    public ORUMessageComposer(
        MSHSegmentBuilder mshBuilder,
        PIDSegmentBuilder pidBuilder,
        OBRSegmentBuilder obrBuilder,
        OBXSegmentBuilder obxBuilder,
        ILogger<ORUMessageComposer> logger)
    {
        _mshBuilder = mshBuilder ?? throw new ArgumentNullException(nameof(mshBuilder));
        _pidBuilder = pidBuilder ?? throw new ArgumentNullException(nameof(pidBuilder));
        _obrBuilder = obrBuilder ?? throw new ArgumentNullException(nameof(obrBuilder));
        _obxBuilder = obxBuilder ?? throw new ArgumentNullException(nameof(obxBuilder));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Composes ORU^R01 (Lab Results) message.
    /// </summary>
    public Result<string> ComposeR01(Patient patient, ObservationResult observation, GenerationOptions options)
    {
        try
        {
            if (patient == null)
                return Result<string>.Failure("Patient is required for ORU^R01 message");

            if (observation == null)
                return Result<string>.Failure("Observation result is required for ORU^R01 message");

            var segments = new List<string>();
            
            // MSH segment - Message Header
            var mshInput = new MSHInput
            {
                MessageType = "ORU^R01^ORU_R01",
                TriggerEvent = "R01"
            };
            segments.Add(_mshBuilder.Build(mshInput, 1, options));
            
            // PID segment - Patient Identification
            segments.Add(_pidBuilder.Build(patient, 1, options));
            
            // OBR segment - Observation Request
            var observationRequest = CreateObservationRequest(observation);
            segments.Add(_obrBuilder.Build(observationRequest, 1, options));
            
            // OBX segment - Observation Result
            segments.Add(_obxBuilder.Build(observation, 1, options));
            
            var message = string.Join("\r\n", segments);
            
            _logger.LogDebug("Composed HL7 v2.3 ORU^R01 message for patient {PatientId}", patient.Id);
            return Result<string>.Success(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to compose ORU^R01 message");
            return Result<string>.Failure($"ORU^R01 composition failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Creates an observation request from the observation result data.
    /// In ORU messages, OBR describes the test that was ordered.
    /// </summary>
    private static ObservationRequest CreateObservationRequest(ObservationResult observation)
    {
        return new ObservationRequest
        {
            TestCode = observation.ObservationCode ?? "LAB",
            TestDescription = observation.ObservationDescription ?? "Laboratory Test",
            CodingSystem = observation.CodingSystem ?? "LN",
            ObservationDateTime = observation.ObservationDateTime ?? DateTime.Now,
            OrderingProvider = observation.Provider
        };
    }
}