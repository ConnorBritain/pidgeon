// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Segmint.Core.Standards.HL7.v23.Messages;
using Segmint.Core.Standards.HL7.v23.Segments;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Segmint.Core.Standards.HL7.v23.Workflows.Builders;

/// <summary>
/// Fluent interface builder for complex prescription scenarios.
/// Part of the hybrid workflow approach - use PharmacyWorkflows for simple scenarios.
/// Provides maximum flexibility for 20% of complex use cases.
/// </summary>
public class PrescriptionBuilder
{
    private readonly RDEMessage _message;
    private readonly List<string> _validationErrors;
    private bool _isBuilt = false;

    /// <summary>
    /// Initializes a new instance of the PrescriptionBuilder class.
    /// </summary>
    internal PrescriptionBuilder()
    {
        _message = new RDEMessage();
        _validationErrors = new List<string>();
        
        // Set up default message header
        _message.MessageHeader = new MSHSegment();
        _message.MessageHeader.SetBasicInfo();
        _message.MessageHeader.SetMessageType("RDE", "O01", "RDE_O01");
        _message.MessageHeader.SetProcessingId(true);
        _message.MessageHeader.GenerateMessageControlId("RDE");
        
        // Set up default event type
        _message.EventType.SetBasicInfo("O01");
        
        // Set up default order control
        _message.CommonOrder.SetBasicInfo("NW");
        _message.CommonOrder.GeneratePlacerOrderNumber("RX");
        
        // Set up default pharmacy order
        _message.PharmacyOrder.GeneratePrescriptionNumber();
    }

    #region Patient Information

    /// <summary>
    /// Sets basic patient information.
    /// </summary>
    /// <param name="patientId">Patient identifier</param>
    /// <param name="lastName">Patient's last name</param>
    /// <param name="firstName">Patient's first name</param>
    /// <returns>PrescriptionBuilder for method chaining</returns>
    public PrescriptionBuilder WithPatient(string patientId, string lastName, string firstName)
    {
        EnsureNotBuilt();
        
        if (string.IsNullOrWhiteSpace(patientId))
            _validationErrors.Add("Patient ID is required");
        if (string.IsNullOrWhiteSpace(lastName))
            _validationErrors.Add("Patient last name is required");
        
        _message.PatientIdentification.SetBasicInfo(patientId, lastName, firstName);
        return this;
    }

    /// <summary>
    /// Sets comprehensive patient demographics.
    /// </summary>
    /// <param name="patientId">Patient identifier</param>
    /// <param name="lastName">Patient's last name</param>
    /// <param name="firstName">Patient's first name</param>
    /// <param name="middleName">Patient's middle name</param>
    /// <param name="dateOfBirth">Patient's date of birth</param>
    /// <param name="gender">Patient's gender</param>
    /// <param name="ssn">Patient's social security number</param>
    /// <returns>PrescriptionBuilder for method chaining</returns>
    public PrescriptionBuilder WithPatientDemographics(
        string patientId,
        string lastName,
        string firstName,
        string? middleName = null,
        DateTime? dateOfBirth = null,
        string? gender = null,
        string? ssn = null)
    {
        EnsureNotBuilt();
        
        _message.PatientIdentification.SetBasicInfo(
            patientId, lastName, firstName, middleName, dateOfBirth, gender, ssn);
        return this;
    }

    /// <summary>
    /// Sets patient address information.
    /// </summary>
    /// <param name="street">Street address</param>
    /// <param name="city">City</param>
    /// <param name="state">State</param>
    /// <param name="zipCode">ZIP code</param>
    /// <param name="country">Country</param>
    /// <returns>PrescriptionBuilder for method chaining</returns>
    public PrescriptionBuilder WithPatientAddress(
        string street,
        string city,
        string state,
        string zipCode,
        string? country = null)
    {
        EnsureNotBuilt();
        
        _message.PatientIdentification.SetAddress(street, city, state, zipCode, country);
        return this;
    }

    /// <summary>
    /// Sets patient phone numbers.
    /// </summary>
    /// <param name="homePhone">Home phone number</param>
    /// <param name="workPhone">Work phone number</param>
    /// <returns>PrescriptionBuilder for method chaining</returns>
    public PrescriptionBuilder WithPatientPhones(string? homePhone = null, string? workPhone = null)
    {
        EnsureNotBuilt();
        
        if (!string.IsNullOrWhiteSpace(homePhone))
            _message.PatientIdentification.SetPhoneNumbers(homePhone);
        
        return this;
    }

    #endregion

    #region Medication Information

    /// <summary>
    /// Sets basic medication information.
    /// </summary>
    /// <param name="medicationCode">Medication code (NDC)</param>
    /// <param name="medicationName">Medication name</param>
    /// <param name="strength">Medication strength</param>
    /// <param name="strengthUnits">Strength units</param>
    /// <param name="dosageForm">Dosage form</param>
    /// <returns>PrescriptionBuilder for method chaining</returns>
    public PrescriptionBuilder WithMedication(
        string medicationCode,
        string medicationName,
        decimal? strength = null,
        string? strengthUnits = null,
        string? dosageForm = null)
    {
        EnsureNotBuilt();
        
        if (string.IsNullOrWhiteSpace(medicationCode))
            _validationErrors.Add("Medication code is required");
        if (string.IsNullOrWhiteSpace(medicationName))
            _validationErrors.Add("Medication name is required");
        
        _message.PharmacyOrder.SetBasicMedicationInfo(
            medicationCode, medicationName, "NDC", strength, strengthUnits, dosageForm: dosageForm);
        
        return this;
    }

    /// <summary>
    /// Sets dispensing information.
    /// </summary>
    /// <param name="quantity">Quantity to dispense</param>
    /// <param name="units">Units</param>
    /// <param name="refills">Number of refills</param>
    /// <returns>PrescriptionBuilder for method chaining</returns>
    public PrescriptionBuilder WithDispensing(decimal quantity, string units, int refills = 0)
    {
        EnsureNotBuilt();
        
        if (quantity <= 0)
            _validationErrors.Add("Quantity must be greater than 0");
        if (string.IsNullOrWhiteSpace(units))
            _validationErrors.Add("Units are required");
        
        _message.PharmacyOrder.SetDispensingInfo(quantity, units, refills);
        return this;
    }

    /// <summary>
    /// Sets dosage instructions.
    /// </summary>
    /// <param name="sig">Directions for use</param>
    /// <returns>PrescriptionBuilder for method chaining</returns>
    public PrescriptionBuilder WithInstructions(string sig)
    {
        EnsureNotBuilt();
        
        if (string.IsNullOrWhiteSpace(sig))
            _validationErrors.Add("Dosage instructions (sig) are required");
        
        _message.PharmacyOrder.ProviderPharmacyTreatmentInstructions.SetValue(sig);
        return this;
    }

    /// <summary>
    /// Sets complete dosage and dispensing information.
    /// </summary>
    /// <param name="sig">Directions for use</param>
    /// <param name="quantity">Quantity to dispense</param>
    /// <param name="units">Units</param>
    /// <param name="refills">Number of refills</param>
    /// <param name="daysSupply">Days supply</param>
    /// <returns>PrescriptionBuilder for method chaining</returns>
    public PrescriptionBuilder WithInstructions(
        string sig,
        decimal quantity,
        string units,
        int refills = 0,
        int daysSupply = 30)
    {
        EnsureNotBuilt();
        
        WithInstructions(sig);
        WithDispensing(quantity, units, refills);
        
        // Note: Days supply is not directly supported in RXE segment in v2.3
        // but could be added as a note if needed
        
        return this;
    }

    #endregion

    #region Provider Information

    /// <summary>
    /// Sets ordering provider information.
    /// </summary>
    /// <param name="providerId">Provider ID</param>
    /// <param name="lastName">Provider's last name</param>
    /// <param name="firstName">Provider's first name</param>
    /// <param name="suffix">Provider's suffix</param>
    /// <returns>PrescriptionBuilder for method chaining</returns>
    public PrescriptionBuilder WithProvider(
        string providerId,
        string lastName,
        string firstName,
        string? suffix = null)
    {
        EnsureNotBuilt();
        
        if (string.IsNullOrWhiteSpace(providerId))
            _validationErrors.Add("Provider ID is required");
        
        _message.CommonOrder.SetBasicInfo("NW", orderingProvider: providerId);
        // OrderingProvider is IdentifierField, only supports SetValue for provider ID
        // Provider names would typically be in separate XV (Extended Person Name) segments
        // For now, we'll use the provider ID that was already set
        
        return this;
    }

    /// <summary>
    /// Sets provider DEA information for controlled substances.
    /// </summary>
    /// <param name="deaNumber">Provider's DEA number</param>
    /// <returns>PrescriptionBuilder for method chaining</returns>
    public PrescriptionBuilder WithProviderDEA(string deaNumber)
    {
        EnsureNotBuilt();
        
        if (string.IsNullOrWhiteSpace(deaNumber))
            _validationErrors.Add("DEA number is required for controlled substances");
        
        _message.PharmacyOrder.SetProviderInfo(orderingProviderDEA: deaNumber);
        return this;
    }

    #endregion

    #region Order Control

    /// <summary>
    /// Sets order control information.
    /// </summary>
    /// <param name="orderControl">Order control code (NW, CA, DC, etc.)</param>
    /// <param name="orderStatus">Order status</param>
    /// <returns>PrescriptionBuilder for method chaining</returns>
    public PrescriptionBuilder WithOrderControl(string orderControl, string? orderStatus = null)
    {
        EnsureNotBuilt();
        
        _message.CommonOrder.SetBasicInfo(orderControl, orderStatus: orderStatus);
        return this;
    }

    /// <summary>
    /// Sets order timing information.
    /// </summary>
    /// <param name="orderDateTime">Order date and time</param>
    /// <param name="effectiveDateTime">Effective date and time</param>
    /// <returns>PrescriptionBuilder for method chaining</returns>
    public PrescriptionBuilder WithOrderTiming(DateTime? orderDateTime = null, DateTime? effectiveDateTime = null)
    {
        EnsureNotBuilt();
        
        if (effectiveDateTime.HasValue)
            _message.CommonOrder.SetTiming(effectiveDateTime);
        
        return this;
    }

    /// <summary>
    /// Sets custom placer order number.
    /// </summary>
    /// <param name="placerOrderNumber">Custom placer order number</param>
    /// <returns>PrescriptionBuilder for method chaining</returns>
    public PrescriptionBuilder WithPlacerOrderNumber(string placerOrderNumber)
    {
        EnsureNotBuilt();
        
        if (string.IsNullOrWhiteSpace(placerOrderNumber))
            _validationErrors.Add("Placer order number cannot be empty");
        
        _message.CommonOrder.PlacerOrderNumber.SetValue(placerOrderNumber);
        return this;
    }

    #endregion

    #region Priority and Special Instructions

    /// <summary>
    /// Sets order priority.
    /// </summary>
    /// <param name="priority">Priority code (R=Routine, S=Stat, A=ASAP)</param>
    /// <returns>PrescriptionBuilder for method chaining</returns>
    public PrescriptionBuilder WithPriority(string priority)
    {
        EnsureNotBuilt();
        
        // Note: Priority is not directly in RXE segment but could be added to ORC if needed
        // For now, we'll add it as a note
        if (!string.IsNullOrWhiteSpace(priority))
        {
            var note = new NTESegment();
            note.Comment.SetValue($"Priority: {priority}");
            _message.InsertSegment(_message.SegmentCount, note);
        }
        
        return this;
    }

    /// <summary>
    /// Adds a note to the prescription.
    /// </summary>
    /// <param name="noteText">Note text</param>
    /// <param name="noteType">Note type/source</param>
    /// <returns>PrescriptionBuilder for method chaining</returns>
    public PrescriptionBuilder WithNote(string noteText, string? noteType = null)
    {
        EnsureNotBuilt();
        
        if (!string.IsNullOrWhiteSpace(noteText))
        {
            var note = new NTESegment();
            note.Comment.SetValue(noteText);
            if (!string.IsNullOrWhiteSpace(noteType))
                note.SourceOfComment.SetValue(noteType);
            _message.InsertSegment(_message.SegmentCount, note);
        }
        
        return this;
    }

    /// <summary>
    /// Marks this as a controlled substance prescription.
    /// </summary>
    /// <param name="schedule">DEA schedule (II, III, IV, V)</param>
    /// <param name="deaNumber">Provider's DEA number</param>
    /// <returns>PrescriptionBuilder for method chaining</returns>
    public PrescriptionBuilder AsControlledSubstance(string schedule, string deaNumber)
    {
        EnsureNotBuilt();
        
        WithProviderDEA(deaNumber);
        WithNote($"Controlled Substance Schedule {schedule} - DEA: {deaNumber}", "CONTROLLED_SUBSTANCE");
        
        return this;
    }

    #endregion

    #region Build and Validation

    /// <summary>
    /// Builds the prescription message.
    /// </summary>
    /// <returns>Configured RDE message</returns>
    /// <exception cref="InvalidOperationException">Thrown if validation fails or message already built</exception>
    public RDEMessage Build()
    {
        EnsureNotBuilt();
        
        // Validate required fields
        ValidateRequiredFields();
        
        // Check for validation errors
        if (_validationErrors.Count > 0)
        {
            throw new InvalidOperationException($"Prescription validation failed: {string.Join(", ", _validationErrors)}");
        }
        
        // Mark as built
        _isBuilt = true;
        
        return _message;
    }

    /// <summary>
    /// Validates the prescription and returns any errors without building.
    /// </summary>
    /// <returns>List of validation errors</returns>
    public List<string> ValidateOnly()
    {
        var errors = new List<string>(_validationErrors);
        
        // Add message-level validation
        var validationResult = _message.Validate();
        errors.AddRange(validationResult.Issues.Select(issue => issue.ToString()));
        
        return errors;
    }

    /// <summary>
    /// Builds the prescription message even if there are validation warnings.
    /// </summary>
    /// <returns>Configured RDE message</returns>
    /// <exception cref="InvalidOperationException">Thrown if message already built</exception>
    public RDEMessage BuildWithWarnings()
    {
        EnsureNotBuilt();
        _isBuilt = true;
        return _message;
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Ensures the message has not been built yet.
    /// </summary>
    private void EnsureNotBuilt()
    {
        if (_isBuilt)
            throw new InvalidOperationException("Message has already been built. Create a new builder instance.");
    }

    /// <summary>
    /// Validates required fields for prescription.
    /// </summary>
    private void ValidateRequiredFields()
    {
        // Check patient ID
        if (string.IsNullOrWhiteSpace(_message.PatientIdentification.PatientIdentifierList.IdNumber))
            _validationErrors.Add("Patient ID is required");
        
        // Check patient name
        if (string.IsNullOrWhiteSpace(_message.PatientIdentification.PatientName.FamilyName))
            _validationErrors.Add("Patient last name is required");
        
        // Check medication code
        if (string.IsNullOrWhiteSpace(_message.PharmacyOrder.GiveCode.Identifier))
            _validationErrors.Add("Medication code is required");
        
        // Check medication name
        if (string.IsNullOrWhiteSpace(_message.PharmacyOrder.GiveCode.Text))
            _validationErrors.Add("Medication name is required");
        
        // Check instructions
        if (string.IsNullOrWhiteSpace(_message.PharmacyOrder.ProviderPharmacyTreatmentInstructions.Value))
            _validationErrors.Add("Dosage instructions (sig) are required");
    }

    #endregion
}

/// <summary>
/// Entry point for prescription builder workflows.
/// </summary>
public static class PharmacyWorkflowBuilder
{
    /// <summary>
    /// Creates a new prescription builder.
    /// </summary>
    /// <returns>New PrescriptionBuilder instance</returns>
    public static PrescriptionBuilder NewPrescription()
    {
        return new PrescriptionBuilder();
    }
}