// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Segmint.Core.Standards.HL7.v23.Messages;
using Segmint.Core.Standards.HL7.v23.Segments;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Segmint.Core.Standards.HL7.v23.Workflows.Templates;

/// <summary>
/// Static factory methods for common pharmacy workflows.
/// Provides pre-configured workflow templates for 80% of pharmacy use cases with compile-time safety.
/// Part of the hybrid workflow approach - use PharmacyWorkflowBuilder for complex scenarios.
/// </summary>
public static class PharmacyWorkflows
{
    #region New Prescription Workflows

    /// <summary>
    /// Creates a new prescription order (RDE message) with essential information.
    /// </summary>
    /// <param name="patientId">Patient identifier</param>
    /// <param name="lastName">Patient's last name</param>
    /// <param name="firstName">Patient's first name</param>
    /// <param name="medicationCode">Medication code (NDC)</param>
    /// <param name="medicationName">Medication name</param>
    /// <param name="quantity">Quantity to dispense</param>
    /// <param name="units">Units (TAB, ML, etc.)</param>
    /// <param name="sig">Directions for use</param>
    /// <param name="providerId">Ordering provider ID</param>
    /// <param name="refills">Number of refills (default: 0)</param>
    /// <param name="daysSupply">Days supply (default: 30)</param>
    /// <returns>Configured RDE message for new prescription</returns>
    public static RDEMessage CreateNewPrescription(
        string patientId,
        string lastName,
        string firstName,
        string medicationCode,
        string medicationName,
        decimal quantity,
        string units,
        string sig,
        string providerId,
        int refills = 0,
        int daysSupply = 30)
    {
        var message = new RDEMessage();
        
        // Set up message header
        message.MessageHeader = new MSHSegment();
        message.MessageHeader.SetBasicInfo();
        message.MessageHeader.SetMessageType("RDE", "O01", "RDE_O01");
        message.MessageHeader.GenerateMessageControlId("RDE");
        message.MessageHeader.SetProcessingId(true);
        
        // Set up event type
        message.EventType.SetBasicInfo("O01");
        
        // Set up patient information
        message.PatientIdentification.SetBasicInfo(patientId, lastName, firstName);
        
        // Set up order control
        message.CommonOrder.SetBasicInfo("NW", orderingProvider: providerId);
        message.CommonOrder.GeneratePlacerOrderNumber("RX");
        
        // Set up pharmacy order with medication details
        message.PharmacyOrder.SetBasicMedicationInfo(medicationCode, medicationName, "NDC");
        message.PharmacyOrder.GeneratePrescriptionNumber();
        message.PharmacyOrder.SetDispensingInfo(quantity, units, refills);
        message.PharmacyOrder.ProviderPharmacyTreatmentInstructions.SetValue(sig);
        
        return message;
    }

    /// <summary>
    /// Creates a comprehensive prescription order with patient demographics and provider information.
    /// </summary>
    /// <param name="patientId">Patient identifier</param>
    /// <param name="lastName">Patient's last name</param>
    /// <param name="firstName">Patient's first name</param>
    /// <param name="dateOfBirth">Patient's date of birth</param>
    /// <param name="gender">Patient's gender</param>
    /// <param name="medicationCode">Medication code (NDC)</param>
    /// <param name="medicationName">Medication name</param>
    /// <param name="strength">Medication strength</param>
    /// <param name="strengthUnits">Strength units</param>
    /// <param name="dosageForm">Dosage form (TAB, CAP, etc.)</param>
    /// <param name="quantity">Quantity to dispense</param>
    /// <param name="units">Units</param>
    /// <param name="sig">Directions for use</param>
    /// <param name="providerId">Ordering provider ID</param>
    /// <param name="providerLastName">Provider's last name</param>
    /// <param name="providerFirstName">Provider's first name</param>
    /// <param name="refills">Number of refills</param>
    /// <param name="daysSupply">Days supply</param>
    /// <returns>Configured RDE message with comprehensive prescription details</returns>
    public static RDEMessage CreateComprehensivePrescription(
        string patientId,
        string lastName,
        string firstName,
        DateTime dateOfBirth,
        string gender,
        string medicationCode,
        string medicationName,
        decimal strength,
        string strengthUnits,
        string dosageForm,
        decimal quantity,
        string units,
        string sig,
        string providerId,
        string providerLastName,
        string providerFirstName,
        int refills = 0,
        int daysSupply = 30)
    {
        var message = CreateNewPrescription(
            patientId, lastName, firstName, medicationCode, medicationName,
            quantity, units, sig, providerId, refills, daysSupply);

        // Add comprehensive patient demographics
        message.PatientIdentification.SetBasicInfo(
            patientId, lastName, firstName, dateOfBirth: dateOfBirth, gender: gender);

        // Add provider information (ID only - names would go in separate segments)
        // OrderingProvider is IdentifierField, only supports SetValue for provider ID
        // Provider names would typically be in separate XV (Extended Person Name) segments

        // Add detailed medication information
        message.PharmacyOrder.SetBasicMedicationInfo(
            medicationCode, medicationName, "NDC", strength, strengthUnits, sig, dosageForm);

        return message;
    }

    /// <summary>
    /// Creates a controlled substance prescription with enhanced tracking.
    /// </summary>
    /// <param name="patientId">Patient identifier</param>
    /// <param name="lastName">Patient's last name</param>
    /// <param name="firstName">Patient's first name</param>
    /// <param name="dateOfBirth">Patient's date of birth</param>
    /// <param name="medicationCode">Controlled substance NDC code</param>
    /// <param name="medicationName">Controlled substance name</param>
    /// <param name="quantity">Quantity to dispense</param>
    /// <param name="units">Units</param>
    /// <param name="sig">Directions for use</param>
    /// <param name="providerId">Ordering provider ID</param>
    /// <param name="providerDeaNumber">Provider's DEA number</param>
    /// <param name="controlledSubstanceSchedule">DEA schedule (II, III, IV, V)</param>
    /// <param name="refills">Number of refills (controlled substances have limits)</param>
    /// <returns>Configured RDE message for controlled substance</returns>
    public static RDEMessage CreateControlledSubstancePrescription(
        string patientId,
        string lastName,
        string firstName,
        DateTime dateOfBirth,
        string medicationCode,
        string medicationName,
        decimal quantity,
        string units,
        string sig,
        string providerId,
        string providerDeaNumber,
        string controlledSubstanceSchedule,
        int refills = 0)
    {
        var message = CreateNewPrescription(
            patientId, lastName, firstName, medicationCode, medicationName,
            quantity, units, sig, providerId, refills);

        // Add patient demographics (required for controlled substances)
        message.PatientIdentification.SetBasicInfo(
            patientId, lastName, firstName, dateOfBirth: dateOfBirth);

        // Add provider DEA information
        message.PharmacyOrder.SetProviderInfo(orderingProviderDEA: providerDeaNumber);

        // Add controlled substance note
        var note = new NTESegment();
        note.Comment.SetValue($"Controlled Substance Schedule {controlledSubstanceSchedule} - DEA: {providerDeaNumber}");
        message.InsertSegment(message.SegmentCount, note);

        return message;
    }

    #endregion

    #region Order Response Workflows

    /// <summary>
    /// Creates an order acceptance response (ORR message).
    /// </summary>
    /// <param name="originalMessageControlId">Control ID from the original order message</param>
    /// <param name="placerOrderNumber">Original placer order number</param>
    /// <param name="fillerOrderNumber">Assigned filler order number</param>
    /// <param name="acceptanceMessage">Optional acceptance message</param>
    /// <param name="estimatedFillTime">Estimated time to fill</param>
    /// <returns>Configured ORR message for order acceptance</returns>
    public static ORRMessage CreateAcceptanceResponse(
        string originalMessageControlId,
        string placerOrderNumber,
        string? fillerOrderNumber = null,
        string? acceptanceMessage = null,
        DateTime? estimatedFillTime = null)
    {
        var message = new ORRMessage();
        
        // Set acceptance acknowledgment
        message.SetAcceptedAcknowledgment(originalMessageControlId, 
            acceptanceMessage ?? "Order accepted for processing");
        
        // Accept the pharmacy order
        message.AcceptPharmacyOrder(placerOrderNumber, fillerOrderNumber, 
            acceptanceMessage, estimatedFillTime);
        
        return message;
    }

    /// <summary>
    /// Creates an order rejection response with detailed errors.
    /// </summary>
    /// <param name="originalMessageControlId">Control ID from the original order message</param>
    /// <param name="placerOrderNumber">Original placer order number</param>
    /// <param name="rejectionReason">Primary rejection reason</param>
    /// <param name="detailedErrors">Detailed error information</param>
    /// <returns>Configured ORR message for order rejection</returns>
    public static ORRMessage CreateRejectionResponse(
        string originalMessageControlId,
        string placerOrderNumber,
        string rejectionReason,
        List<(string code, string description, string severity)>? detailedErrors = null)
    {
        var message = new ORRMessage();
        
        // Set rejection acknowledgment
        message.SetRejectedAcknowledgment(originalMessageControlId, rejectionReason);
        
        // Reject the pharmacy order
        message.RejectPharmacyOrder(placerOrderNumber, rejectionReason, detailedErrors);
        
        return message;
    }

    /// <summary>
    /// Creates a drug interaction response with interaction details.
    /// </summary>
    /// <param name="originalMessageControlId">Control ID from the original order message</param>
    /// <param name="placerOrderNumber">Original placer order number</param>
    /// <param name="interactions">List of detected drug interactions</param>
    /// <param name="allowOverride">Whether override is permitted</param>
    /// <returns>Configured ORR message with drug interaction information</returns>
    public static ORRMessage CreateDrugInteractionResponse(
        string originalMessageControlId,
        string placerOrderNumber,
        List<(string severity, string drug1, string drug2, string effect)> interactions,
        bool allowOverride = true)
    {
        return ORRMessage.CreateDrugInteractionResponse(
            originalMessageControlId, placerOrderNumber, interactions, allowOverride);
    }

    /// <summary>
    /// Creates an allergy contraindication response.
    /// </summary>
    /// <param name="originalMessageControlId">Control ID from the original order message</param>
    /// <param name="placerOrderNumber">Original placer order number</param>
    /// <param name="allergen">Allergen substance</param>
    /// <param name="medication">Contraindicated medication</param>
    /// <param name="reactionType">Type of allergic reaction</param>
    /// <param name="allowOverride">Whether override is permitted</param>
    /// <returns>Configured ORR message for allergy contraindication</returns>
    public static ORRMessage CreateAllergyContraindicationResponse(
        string originalMessageControlId,
        string placerOrderNumber,
        string allergen,
        string medication,
        string reactionType,
        bool allowOverride = false)
    {
        var message = new ORRMessage();
        
        // Set acknowledgment based on override permission
        if (allowOverride)
        {
            message.SetAcceptedAcknowledgment(originalMessageControlId, "Allergy warning - override permitted");
        }
        else
        {
            message.SetRejectedAcknowledgment(originalMessageControlId, "Allergy contraindication detected");
        }
        
        // Add allergy error
        message.AddAllergyError(allergen, medication, reactionType);
        
        // Add order response
        message.AddOrderResponse(
            allowOverride ? "OK" : "NA",
            placerOrderNumber,
            orderStatus: allowOverride ? "A" : "R",
            responseReason: $"Allergy to {allergen} - {reactionType}");
        
        return message;
    }

    #endregion

    #region Dispensing Workflows

    /// <summary>
    /// Creates a medication dispense record (RDS message).
    /// </summary>
    /// <param name="patientId">Patient identifier</param>
    /// <param name="lastName">Patient's last name</param>
    /// <param name="firstName">Patient's first name</param>
    /// <param name="prescriptionNumber">Prescription number</param>
    /// <param name="medicationCode">Dispensed medication code</param>
    /// <param name="medicationName">Dispensed medication name</param>
    /// <param name="dispensedAmount">Amount dispensed</param>
    /// <param name="units">Units</param>
    /// <param name="pharmacist">Dispensing pharmacist</param>
    /// <param name="lotNumber">Medication lot number</param>
    /// <param name="expirationDate">Medication expiration date</param>
    /// <param name="manufacturer">Medication manufacturer</param>
    /// <param name="refillsRemaining">Number of refills remaining</param>
    /// <returns>Configured RDS message for medication dispensing</returns>
    public static RDSMessage CreateDispenseRecord(
        string patientId,
        string lastName,
        string firstName,
        string prescriptionNumber,
        string medicationCode,
        string medicationName,
        decimal dispensedAmount,
        string units,
        string pharmacist,
        string? lotNumber = null,
        DateTime? expirationDate = null,
        string? manufacturer = null,
        int? refillsRemaining = null)
    {
        return RDSMessage.CreateBasicDispense(
            patientId, lastName, firstName, prescriptionNumber,
            medicationCode, medicationName, dispensedAmount, units, pharmacist);
    }

    /// <summary>
    /// Creates a controlled substance dispense record with enhanced tracking.
    /// </summary>
    /// <param name="patientId">Patient identifier</param>
    /// <param name="lastName">Patient's last name</param>
    /// <param name="firstName">Patient's first name</param>
    /// <param name="prescriptionNumber">Prescription number</param>
    /// <param name="medicationCode">Controlled substance NDC code</param>
    /// <param name="medicationName">Controlled substance name</param>
    /// <param name="dispensedAmount">Amount dispensed</param>
    /// <param name="units">Units</param>
    /// <param name="pharmacist">Dispensing pharmacist</param>
    /// <param name="lotNumber">Lot number (required for controlled substances)</param>
    /// <param name="expirationDate">Expiration date</param>
    /// <param name="manufacturer">Manufacturer</param>
    /// <param name="deaNumber">Pharmacist's DEA number</param>
    /// <param name="controlledSubstanceSchedule">DEA schedule</param>
    /// <returns>Configured RDS message for controlled substance dispensing</returns>
    public static RDSMessage CreateControlledSubstanceDispense(
        string patientId,
        string lastName,
        string firstName,
        string prescriptionNumber,
        string medicationCode,
        string medicationName,
        decimal dispensedAmount,
        string units,
        string pharmacist,
        string lotNumber,
        DateTime expirationDate,
        string manufacturer,
        string deaNumber,
        string controlledSubstanceSchedule)
    {
        return RDSMessage.CreateControlledSubstanceDispense(
            patientId, lastName, firstName, prescriptionNumber,
            medicationCode, medicationName, dispensedAmount, units,
            pharmacist, lotNumber, expirationDate, manufacturer,
            deaNumber, controlledSubstanceSchedule);
    }

    /// <summary>
    /// Creates a partial dispensing record when full quantity is not available.
    /// </summary>
    /// <param name="patientId">Patient identifier</param>
    /// <param name="lastName">Patient's last name</param>
    /// <param name="firstName">Patient's first name</param>
    /// <param name="prescriptionNumber">Prescription number</param>
    /// <param name="medicationCode">Medication code</param>
    /// <param name="medicationName">Medication name</param>
    /// <param name="orderedAmount">Originally ordered amount</param>
    /// <param name="dispensedAmount">Actually dispensed amount</param>
    /// <param name="units">Units</param>
    /// <param name="pharmacist">Dispensing pharmacist</param>
    /// <param name="partialReason">Reason for partial dispensing</param>
    /// <param name="remainingToDispense">Amount still to be dispensed</param>
    /// <returns>Configured RDS message for partial dispensing</returns>
    public static RDSMessage CreatePartialDispense(
        string patientId,
        string lastName,
        string firstName,
        string prescriptionNumber,
        string medicationCode,
        string medicationName,
        decimal orderedAmount,
        decimal dispensedAmount,
        string units,
        string pharmacist,
        string partialReason,
        decimal? remainingToDispense = null)
    {
        var message = CreateDispenseRecord(
            patientId, lastName, firstName, prescriptionNumber,
            medicationCode, medicationName, dispensedAmount, units, pharmacist);

        // Add partial dispensing information through RDS message method
        message.RecordPartialDispense(
            prescriptionNumber, medicationCode, medicationName,
            orderedAmount, dispensedAmount, units, pharmacist,
            partialReason, remainingToDispense);

        return message;
    }

    /// <summary>
    /// Creates a medication return/void record.
    /// </summary>
    /// <param name="patientId">Patient identifier</param>
    /// <param name="lastName">Patient's last name</param>
    /// <param name="firstName">Patient's first name</param>
    /// <param name="prescriptionNumber">Original prescription number</param>
    /// <param name="returnReason">Reason for return</param>
    /// <param name="returnedAmount">Amount being returned</param>
    /// <param name="units">Units</param>
    /// <param name="pharmacist">Pharmacist processing return</param>
    /// <param name="returnDate">Date of return</param>
    /// <returns>Configured RDS message for medication return</returns>
    public static RDSMessage CreateMedicationReturn(
        string patientId,
        string lastName,
        string firstName,
        string prescriptionNumber,
        string returnReason,
        decimal returnedAmount,
        string units,
        string pharmacist,
        DateTime? returnDate = null)
    {
        var message = new RDSMessage();
        
        // Set patient information
        message.SetPatientInformation(patientId, lastName, firstName);
        
        // Record medication return
        message.RecordMedicationReturn(
            prescriptionNumber, returnReason, returnedAmount, units, pharmacist, returnDate);
        
        return message;
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// Validates a completed pharmacy workflow message.
    /// </summary>
    /// <param name="message">Message to validate</param>
    /// <returns>List of validation issues</returns>
    public static List<string> ValidatePharmacyWorkflow(HL7Message message)
    {
        var validationResult = message.Validate();
        return validationResult.Issues.Select(issue => issue.ToString()).ToList();
    }

    /// <summary>
    /// Generates a display string for a pharmacy workflow message.
    /// </summary>
    /// <param name="message">Message to display</param>
    /// <returns>Human-readable display string</returns>
    public static string GetWorkflowDisplayString(HL7Message message)
    {
        return message switch
        {
            RDEMessage rde => rde.ToDisplayString(),
            ORRMessage orr => orr.ToDisplayString(),
            RDSMessage rds => rds.ToDisplayString(),
            _ => message.ToString() ?? "Unknown message type"
        };
    }

    #endregion
}