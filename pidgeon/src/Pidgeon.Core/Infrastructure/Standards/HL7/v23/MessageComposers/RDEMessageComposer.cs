// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Microsoft.Extensions.Logging;
using Pidgeon.Core.Domain.Clinical.Entities;
using Pidgeon.Core.Generation;
using Pidgeon.Core.Infrastructure.Standards.HL7.v23.Segments;

namespace Pidgeon.Core.Infrastructure.Standards.HL7.v23.MessageComposers;

/// <summary>
/// Composes RDE (Pharmacy/Treatment Encoded Order) message family.
/// Handles RDE^O11 (Pharmacy Orders) messages with proper segment sequence.
/// </summary>
public class RDEMessageComposer
{
    private readonly MSHSegmentBuilder _mshBuilder;
    private readonly PIDSegmentBuilder _pidBuilder;
    private readonly ORCSegmentBuilder _orcBuilder;
    private readonly RXESegmentBuilder _rxeBuilder;
    private readonly ILogger<RDEMessageComposer> _logger;

    public RDEMessageComposer(
        MSHSegmentBuilder mshBuilder,
        PIDSegmentBuilder pidBuilder,
        ORCSegmentBuilder orcBuilder,
        RXESegmentBuilder rxeBuilder,
        ILogger<RDEMessageComposer> logger)
    {
        _mshBuilder = mshBuilder ?? throw new ArgumentNullException(nameof(mshBuilder));
        _pidBuilder = pidBuilder ?? throw new ArgumentNullException(nameof(pidBuilder));
        _orcBuilder = orcBuilder ?? throw new ArgumentNullException(nameof(orcBuilder));
        _rxeBuilder = rxeBuilder ?? throw new ArgumentNullException(nameof(rxeBuilder));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Composes RDE^O11 (Pharmacy Order) message.
    /// </summary>
    public Result<string> ComposeO11(Patient patient, Prescription prescription, GenerationOptions options)
    {
        try
        {
            if (patient == null)
                return Result<string>.Failure("Patient is required for RDE^O11 message");

            if (prescription == null)
                return Result<string>.Failure("Prescription is required for RDE^O11 message");

            var segments = new List<string>();
            
            // MSH segment - Message Header
            var mshInput = new MSHInput
            {
                MessageType = "RDE^O11^RDE_O11",
                TriggerEvent = "O11"
            };
            segments.Add(_mshBuilder.Build(mshInput, 1, options));
            
            // PID segment - Patient Identification
            segments.Add(_pidBuilder.Build(patient, 1, options));
            
            // ORC segment - Common Order
            var orderControl = CreateOrderControl(prescription);
            segments.Add(_orcBuilder.Build(orderControl, 1, options));
            
            // RXE segment - Pharmacy/Treatment Encoded Order
            segments.Add(_rxeBuilder.Build(prescription, 1, options));
            
            var message = string.Join("\r\n", segments);
            
            _logger.LogDebug("Composed HL7 v2.3 RDE^O11 message for patient {PatientId}", patient.Id);
            return Result<string>.Success(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to compose RDE^O11 message");
            return Result<string>.Failure($"RDE^O11 composition failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Creates order control information from prescription data.
    /// </summary>
    private static OrderControl CreateOrderControl(Prescription prescription)
    {
        return new OrderControl
        {
            OrderControlCode = "NW", // New order
            PlacerOrderNumber = prescription.Id,
            OrderStatus = "A", // Active
            TransactionDateTime = prescription.DatePrescribed,
            OrderingProvider = prescription.Prescriber,
            OrderEffectiveDateTime = prescription.DatePrescribed
        };
    }
}