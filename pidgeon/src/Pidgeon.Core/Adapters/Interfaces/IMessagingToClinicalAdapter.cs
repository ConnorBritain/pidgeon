// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Pidgeon.Core.Domain.Clinical.Entities;
using Pidgeon.Core.Domain.Messaging;
using Pidgeon.Core.Domain.Messaging.HL7v2.Messages;
using Pidgeon.Core.Domain.Messaging.FHIR.Bundles;
using Pidgeon.Core.Domain.Messaging.NCPDP.Transactions;

namespace Pidgeon.Core.Adapters.Interfaces;

/// <summary>
/// Adapter interface for translating between Messaging Domain and Clinical Domain.
/// Handles the architectural boundary between wire format structures and healthcare business concepts.
/// 
/// DOMAIN BOUNDARY: Messaging → Clinical
/// - INPUT: Messaging Domain types (HL7Message, FHIRBundle, NCPDPTransaction)
/// - OUTPUT: Clinical Domain types (Patient, Prescription, Provider, Encounter)
/// 
/// RESPONSIBILITY: Extract clinical meaning from standard-specific message structures
/// while handling format variations and maintaining healthcare semantic integrity.
/// </summary>
public interface IMessagingToClinicalAdapter
{
    /// <summary>
    /// Extracts patient information from an HL7 message.
    /// Parses PID segment and related demographic information into clinical patient entity.
    /// </summary>
    /// <param name="message">HL7 message containing patient data</param>
    /// <returns>Clinical patient entity with normalized healthcare information</returns>
    Task<Patient?> ExtractPatientFromHL7Async(HL7Message message);

    /// <summary>
    /// Extracts prescription information from an HL7 RDE/RDS message.
    /// Parses ORC, RXE, RXR segments into clinical prescription entity.
    /// </summary>
    /// <param name="message">HL7 prescription message</param>
    /// <returns>Clinical prescription entity with medication, dosing, and provider details</returns>
    Task<Prescription?> ExtractPrescriptionFromHL7Async(HL7Message message);

    /// <summary>
    /// Extracts patient information from a FHIR Bundle.
    /// Locates Patient resource and converts to clinical domain representation.
    /// </summary>
    /// <param name="bundle">FHIR Bundle potentially containing Patient resource</param>
    /// <returns>Clinical patient entity extracted from FHIR data</returns>
    Task<Patient?> ExtractPatientFromFHIRAsync(FHIRBundle bundle);

    /// <summary>
    /// Extracts prescription information from a FHIR Bundle.
    /// Locates MedicationRequest resources and converts to clinical domain representation.
    /// </summary>
    /// <param name="bundle">FHIR Bundle potentially containing MedicationRequest resources</param>
    /// <returns>Collection of clinical prescription entities</returns>
    Task<IEnumerable<Prescription>> ExtractPrescriptionsFromFHIRAsync(FHIRBundle bundle);

    /// <summary>
    /// Extracts prescription information from an NCPDP transaction.
    /// Parses NCPDP segments into clinical prescription entity.
    /// </summary>
    /// <param name="transaction">NCPDP transaction message</param>
    /// <returns>Clinical prescription entity with pharmacy-specific details</returns>
    Task<Prescription?> ExtractPrescriptionFromNCPDPAsync(NCPDPTransaction transaction);

    /// <summary>
    /// Extracts encounter information from an HL7 ADT message.
    /// Parses PV1 segment and related visit information into clinical encounter entity.
    /// </summary>
    /// <param name="message">HL7 ADT message containing encounter data</param>
    /// <returns>Clinical encounter entity with admission/discharge details</returns>
    Task<Encounter?> ExtractEncounterFromHL7Async(HL7Message message);
}