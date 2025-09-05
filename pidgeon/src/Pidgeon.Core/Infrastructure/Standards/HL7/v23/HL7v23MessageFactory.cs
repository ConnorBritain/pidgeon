// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System.Text;
using System.Threading;
using Pidgeon.Core.Domain.Clinical.Entities;
using Pidgeon.Core.Generation;
using Microsoft.Extensions.Logging;

namespace Pidgeon.Core.Infrastructure.Standards.HL7.v23;

/// <summary>
/// Factory for generating HL7 v2.3 standards-compliant messages.
/// Delegates to HL7v23MessageComposer for clean segment-based composition.
/// </summary>
public class HL7v23MessageFactory : IHL7MessageFactory
{
    private readonly ILogger<HL7v23MessageFactory> _logger;
    private readonly HL7v23MessageComposer _composer;
    
    public HL7v23MessageFactory(ILogger<HL7v23MessageFactory> logger, HL7v23MessageComposer composer)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _composer = composer ?? throw new ArgumentNullException(nameof(composer));
    }

    public Result<string> GenerateADT_A01(Patient patient, Encounter encounter, GenerationOptions options)
    {
        return _composer.ComposeADT_A01(patient, encounter, options);
    }

    public Result<string> GenerateADT_A08(Patient patient, Encounter encounter, GenerationOptions options)
    {
        return _composer.ComposeADT_A08(patient, encounter, options);
    }

    public Result<string> GenerateADT_A03(Patient patient, Encounter encounter, GenerationOptions options)
    {
        return _composer.ComposeADT_A03(patient, encounter, options);
    }

    public Result<string> GenerateORU_R01(Patient patient, ObservationResult observation, GenerationOptions options)
    {
        return _composer.ComposeORU_R01(patient, observation, options);
    }

    public Result<string> GenerateRDE_O11(Patient patient, Prescription prescription, GenerationOptions options)
    {
        return _composer.ComposeRDE_O11(patient, prescription, options);
    }

    public Result<string> GenerateORM_O01(Patient patient, Order order, GenerationOptions options)
    {
        // TODO: Implement ORM_O01 in message composer
        return Result<string>.Failure("ORM^O01 not yet implemented in new architecture");
    }

}

