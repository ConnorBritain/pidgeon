// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Pidgeon.Core.Domain.Clinical.Entities;
using Pidgeon.Core.Generation;

namespace Pidgeon.Core.Infrastructure.Standards.HL7.v23;

/// <summary>
/// Factory interface for generating HL7 v2.3 standards-compliant messages.
/// Follows HL7.org v2.3 specification for segment structure and field requirements.
/// </summary>
public interface IHL7MessageFactory
{
    /// <summary>
    /// Generates an HL7 v2.3 compliant ADT^A01 (Admit/Visit Notification) message.
    /// Required segments: MSH, EVN, PID, PV1
    /// </summary>
    Result<string> GenerateADT_A01(Patient patient, Encounter encounter, GenerationOptions options);

    /// <summary>
    /// Generates an HL7 v2.3 compliant ADT^A08 (Update Patient Information) message.
    /// Required segments: MSH, EVN, PID, PV1
    /// </summary>
    Result<string> GenerateADT_A08(Patient patient, Encounter encounter, GenerationOptions options);

    /// <summary>
    /// Generates an HL7 v2.3 compliant ADT^A03 (Discharge Patient) message.
    /// Required segments: MSH, EVN, PID, PV1
    /// </summary>
    Result<string> GenerateADT_A03(Patient patient, Encounter encounter, GenerationOptions options);

    /// <summary>
    /// Generates an HL7 v2.3 compliant ORU^R01 (Observation Result) message.
    /// Required segments: MSH, PID, OBR, OBX
    /// </summary>
    Result<string> GenerateORU_R01(Patient patient, ObservationResult observation, GenerationOptions options);

    /// <summary>
    /// Generates an HL7 v2.3 compliant RDE^O11 (Pharmacy Order) message.
    /// Required segments: MSH, PID, RXE
    /// </summary>
    Result<string> GenerateRDE_O11(Patient patient, Prescription prescription, GenerationOptions options);

    /// <summary>
    /// Generates an HL7 v2.3 compliant ORM^O01 (Order Management) message.
    /// Required segments: MSH, PID, ORC, OBR
    /// </summary>
    Result<string> GenerateORM_O01(Patient patient, Order order, GenerationOptions options);
}

/// <summary>
/// Represents an observation result for lab reporting.
/// </summary>
public record ObservationResult
{
    public required string Id { get; init; }
    public required string TestName { get; init; }
    public required string TestCode { get; init; }
    public required string Value { get; init; }
    public required string Units { get; init; }
    public string? ReferenceRange { get; init; }
    public string? Status { get; init; } = "F"; // F=Final
    public DateTime? CollectionTime { get; init; }
    public Provider? OrderingProvider { get; init; }
}

/// <summary>
/// Represents a clinical order for order management messages.
/// </summary>
public record Order
{
    public required string Id { get; init; }
    public required string OrderType { get; init; } // LAB, RAD, PHARM, etc.
    public required string Description { get; init; }
    public required string OrderCode { get; init; }
    public string? Priority { get; init; } = "R"; // R=Routine, S=Stat, A=ASAP
    public string? Status { get; init; } = "NW"; // NW=New, IP=In Progress, CM=Complete
    public DateTime? OrderDateTime { get; init; }
    public Provider? OrderingProvider { get; init; }
}