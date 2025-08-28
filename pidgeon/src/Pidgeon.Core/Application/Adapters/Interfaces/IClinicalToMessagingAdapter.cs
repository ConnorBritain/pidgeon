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
/// Adapter interface for translating between Clinical Domain and Messaging Domain.
/// Handles the architectural boundary between healthcare business concepts and wire format structures.
/// 
/// DOMAIN BOUNDARY: Clinical → Messaging
/// - INPUT: Clinical Domain types (Patient, Prescription, Provider, Encounter)
/// - OUTPUT: Messaging Domain types (HL7Message, FHIRBundle, NCPDPTransaction)
/// 
/// RESPONSIBILITY: Transform clinical concepts into standard-specific message structures
/// while preserving semantic meaning and healthcare context.
/// </summary>
public interface IClinicalToMessagingAdapter
{
    /// <summary>
    /// Creates an HL7 ADT (Admission/Discharge/Transfer) message from clinical data.
    /// Transforms patient and encounter information into HL7 v2.3 message structure.
    /// </summary>
    /// <param name="patient">Clinical patient information</param>
    /// <param name="encounter">Clinical encounter details</param>
    /// <param name="eventType">ADT event type (A01=Admit, A02=Transfer, A03=Discharge, etc.)</param>
    /// <returns>Complete HL7 ADT message with proper segment structure</returns>
    Task<HL7Message> CreateAdmissionMessageAsync(Patient patient, Encounter encounter, string eventType = "A01");

    /// <summary>
    /// Creates an HL7 RDE (Prescription Order) message from clinical prescription data.
    /// Transforms prescription information into HL7 pharmacy message structure.
    /// </summary>
    /// <param name="prescription">Clinical prescription details</param>
    /// <returns>Complete HL7 RDE message with ORC, RXE, and RXR segments</returns>
    Task<HL7Message> CreatePrescriptionOrderAsync(Prescription prescription);

    /// <summary>
    /// Creates a FHIR Bundle containing relevant resources from clinical data.
    /// Transforms clinical concepts into FHIR R4 resource structure.
    /// </summary>
    /// <param name="patient">Clinical patient information</param>
    /// <param name="prescriptions">Related prescriptions (optional)</param>
    /// <param name="bundleType">Type of FHIR bundle to create</param>
    /// <returns>FHIR Bundle with Patient, MedicationRequest, and related resources</returns>
    Task<FHIRBundle> CreateFHIRBundleAsync(Patient patient, IEnumerable<Prescription>? prescriptions = null, string bundleType = "collection");

    /// <summary>
    /// Creates an NCPDP SCRIPT message from clinical prescription data.
    /// Transforms prescription information into NCPDP pharmacy transaction format.
    /// </summary>
    /// <param name="prescription">Clinical prescription details</param>
    /// <param name="transactionType">NCPDP transaction type (NewRx, Refill, etc.)</param>
    /// <returns>NCPDP transaction message with proper XML structure</returns>
    Task<NCPDPTransaction> CreateNCPDPTransactionAsync(Prescription prescription, string transactionType = "NewRx");
}