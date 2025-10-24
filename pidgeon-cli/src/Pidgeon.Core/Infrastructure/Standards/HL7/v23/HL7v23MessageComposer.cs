// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Microsoft.Extensions.Logging;
using Pidgeon.Core.Domain.Clinical.Entities;
using Pidgeon.Core.Generation;
using Pidgeon.Core.Infrastructure.Standards.HL7.v23.Segments;
using Pidgeon.Core.Infrastructure.Standards.HL7.v23.MessageComposers;

namespace Pidgeon.Core.Infrastructure.Standards.HL7.v23;

/// <summary>
/// Orchestrates HL7 v2.3 message composition by delegating to message-specific composers.
/// This is a thin orchestrator that routes to focused message family composers.
/// </summary>
public class HL7v23MessageComposer
{
    private readonly ADTMessageComposer _adtComposer;
    private readonly ORUMessageComposer _oruComposer;
    private readonly RDEMessageComposer _rdeComposer;
    private readonly ILogger<HL7v23MessageComposer> _logger;

    public HL7v23MessageComposer(
        ADTMessageComposer adtComposer,
        ORUMessageComposer oruComposer,
        RDEMessageComposer rdeComposer,
        ILogger<HL7v23MessageComposer> logger)
    {
        _adtComposer = adtComposer ?? throw new ArgumentNullException(nameof(adtComposer));
        _oruComposer = oruComposer ?? throw new ArgumentNullException(nameof(oruComposer));
        _rdeComposer = rdeComposer ?? throw new ArgumentNullException(nameof(rdeComposer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Composes an HL7 v2.3 ADT^A01 (Admit/Visit Notification) message.
    /// </summary>
    public Result<string> ComposeADT_A01(Patient patient, Encounter encounter, GenerationOptions options)
    {
        return _adtComposer.ComposeA01(patient, encounter, options);
    }

    /// <summary>
    /// Composes an HL7 v2.3 ADT^A03 (Discharge Patient) message.
    /// </summary>
    public Result<string> ComposeADT_A03(Patient patient, Encounter encounter, GenerationOptions options)
    {
        return _adtComposer.ComposeA03(patient, encounter, options);
    }

    /// <summary>
    /// Composes an HL7 v2.3 ADT^A08 (Update Patient Information) message.
    /// </summary>
    public Result<string> ComposeADT_A08(Patient patient, Encounter encounter, GenerationOptions options)
    {
        return _adtComposer.ComposeA08(patient, encounter, options);
    }

    /// <summary>
    /// Composes an HL7 v2.3 ORU^R01 (Lab Results) message.
    /// </summary>
    public Result<string> ComposeORU_R01(Patient patient, ObservationResult observation, GenerationOptions options)
    {
        return _oruComposer.ComposeR01(patient, observation, options);
    }

    /// <summary>
    /// Composes an HL7 v2.3 RDE^O11 (Pharmacy Order) message.
    /// </summary>
    public Result<string> ComposeRDE_O11(Patient patient, Prescription prescription, GenerationOptions options)
    {
        return _rdeComposer.ComposeO11(patient, prescription, options);
    }
}