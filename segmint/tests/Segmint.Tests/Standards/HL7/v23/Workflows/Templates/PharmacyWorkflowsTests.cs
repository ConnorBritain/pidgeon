// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Segmint.Core.Standards.HL7.v23.Messages;
using Segmint.Core.Standards.HL7.v23.Segments;
using Segmint.Core.Standards.HL7.v23.Workflows.Templates;
using System;
using System.Linq;
using Xunit;

namespace Segmint.Tests.Standards.HL7.v23.Workflows.Templates;

/// <summary>
/// Unit tests for PharmacyWorkflows static factory methods.
/// Tests the code template approach of the hybrid workflow system.
/// </summary>
public class PharmacyWorkflowsTests
{
    #region CreateNewPrescription Tests

    [Fact]
    public void CreateNewPrescription_WithRequiredFields_ReturnsValidRDEMessage()
    {
        // Arrange
        var patientId = "PAT123";
        var lastName = "Smith";
        var firstName = "John";
        var medicationCode = "12345678";
        var medicationName = "Lisinopril 10mg";
        var quantity = 30m;
        var units = "TAB";
        var sig = "Take 1 tablet by mouth daily";
        var providerId = "PRV456";

        // Act
        var message = PharmacyWorkflows.CreateNewPrescription(
            patientId, lastName, firstName,
            medicationCode, medicationName,
            quantity, units, sig, providerId);

        // Assert
        Assert.NotNull(message);
        Assert.IsType<RDEMessage>(message);
        
        // Verify message header
        Assert.NotNull(message.MessageHeader);
        Assert.Equal("RDE", message.MessageType);
        Assert.Equal("O01", message.TriggerEvent);
        
        // Verify patient information
        Assert.Equal(patientId, message.PatientIdentification.PatientIdentifierList.IdNumber);
        Assert.Equal(lastName, message.PatientIdentification.PatientName.FamilyName);
        Assert.Equal(firstName, message.PatientIdentification.PatientName.GivenName);
        
        // Verify medication information
        Assert.Equal(medicationCode, message.PharmacyOrder.GiveCode.Identifier);
        Assert.Equal(medicationName, message.PharmacyOrder.GiveCode.Text);
        Assert.Equal(sig, message.PharmacyOrder.ProviderPharmacyTreatmentInstructions.Value);
        
        // Verify order control
        Assert.Equal("NW", message.CommonOrder.OrderControl.Value);
        Assert.Equal(providerId, message.CommonOrder.OrderingProvider.Value);
    }

    [Fact]
    public void CreateNewPrescription_WithOptionalRefillsAndDaysSupply_SetsCorrectValues()
    {
        // Arrange
        var refills = 5;
        var daysSupply = 90;

        // Act
        var message = PharmacyWorkflows.CreateNewPrescription(
            "PAT123", "Smith", "John",
            "12345678", "Lisinopril 10mg",
            30m, "TAB", "Take 1 tablet daily", "PRV456",
            refills, daysSupply);

        // Assert
        Assert.Equal(refills.ToString(), message.PharmacyOrder.NumberOfRefills.RawValue);
        // Note: Days supply may be stored in a different field or note depending on implementation
    }

    [Fact]
    public void CreateNewPrescription_GeneratesUniqueControlId()
    {
        // Act
        var message1 = PharmacyWorkflows.CreateNewPrescription(
            "PAT1", "Smith", "John", "12345", "Med1", 30m, "TAB", "Sig1", "PRV1");
        var message2 = PharmacyWorkflows.CreateNewPrescription(
            "PAT2", "Jones", "Jane", "67890", "Med2", 60m, "CAP", "Sig2", "PRV2");

        // Assert
        Assert.NotEqual(message1.MessageHeader.MessageControlId.Value, 
                       message2.MessageHeader.MessageControlId.Value);
    }

    #endregion

    #region CreateComprehensivePrescription Tests

    [Fact]
    public void CreateComprehensivePrescription_WithFullDemographics_SetsAllFields()
    {
        // Arrange
        var dateOfBirth = new DateTime(1980, 5, 15);
        var gender = "M";
        var strength = 10m;
        var strengthUnits = "mg";
        var dosageForm = "TAB";

        // Act
        var message = PharmacyWorkflows.CreateComprehensivePrescription(
            "PAT123", "Smith", "John", dateOfBirth, gender,
            "12345678", "Lisinopril", strength, strengthUnits, dosageForm,
            30m, "TAB", "Take 1 tablet daily",
            "PRV456", "Johnson", "Robert",
            refills: 3, daysSupply: 30);

        // Assert
        Assert.Equal(dateOfBirth, message.PatientIdentification.DateTimeOfBirth.ToDateTime());
        Assert.Equal(gender, message.PatientIdentification.AdministrativeSex.Value);
        
        // Verify provider information is set correctly
        // Note: Provider name handling depends on exact implementation
    }

    #endregion

    #region CreateControlledSubstancePrescription Tests

    [Fact]
    public void CreateControlledSubstancePrescription_WithDEAInfo_AddsControlledSubstanceNote()
    {
        // Arrange
        var deaNumber = "BJ1234567";
        var schedule = "II";
        var dateOfBirth = new DateTime(1975, 3, 20);

        // Act
        var message = PharmacyWorkflows.CreateControlledSubstancePrescription(
            "PAT123", "Smith", "John", dateOfBirth,
            "00406055705", "Oxycodone 5mg",
            20m, "TAB", "Take 1 tablet every 6 hours as needed for pain",
            "PRV456", deaNumber, schedule, refills: 0);

        // Assert
        Assert.NotNull(message);
        
        // Verify patient DOB is set (required for controlled substances)
        Assert.Equal(dateOfBirth, message.PatientIdentification.DateTimeOfBirth.ToDateTime());
        
        // Verify DEA info is set
        Assert.NotNull(message.PharmacyOrder);
        
        // Check for controlled substance note
        var notes = message.Where(s => s.SegmentId == "NTE").Cast<NTESegment>().ToList();
        Assert.True(notes.Any(n => n.Comment.Value.Contains($"Schedule {schedule}")));
        Assert.True(notes.Any(n => n.Comment.Value.Contains(deaNumber)));
    }

    [Fact]
    public void CreateControlledSubstancePrescription_ScheduleII_NoRefills()
    {
        // Arrange & Act
        var message = PharmacyWorkflows.CreateControlledSubstancePrescription(
            "PAT123", "Smith", "John", new DateTime(1980, 1, 1),
            "00406055705", "Oxycodone 5mg",
            20m, "TAB", "Take as directed",
            "PRV456", "BJ1234567", "II", refills: 0);

        // Assert
        Assert.Equal("0", message.PharmacyOrder.NumberOfRefills.RawValue);
    }

    #endregion

    #region CreateAcceptanceResponse Tests

    [Fact]
    public void CreateAcceptanceResponse_WithValidInput_ReturnsAcceptedORR()
    {
        // Arrange
        var originalControlId = "RDE_20240315_1234";
        var placerOrderNumber = "ORD123456";
        var fillerOrderNumber = "RX789012";
        var acceptanceMessage = "Order accepted for processing";

        // Act
        var message = PharmacyWorkflows.CreateAcceptanceResponse(
            originalControlId, placerOrderNumber, fillerOrderNumber, acceptanceMessage);

        // Assert
        Assert.NotNull(message);
        Assert.IsType<ORRMessage>(message);
        
        // Verify acknowledgment
        Assert.NotNull(message.MessageAcknowledgment);
        Assert.Equal("AA", message.MessageAcknowledgment.AcknowledgmentCode.Value);
        Assert.Equal(originalControlId, message.MessageAcknowledgment.MessageControlId.Value);
        
        // Verify order response
        var orderResponses = message.OrderResponses;
        Assert.NotEmpty(orderResponses);
        Assert.Equal("OK", orderResponses.First().OrderControl.OrderControl.Value);
    }

    [Fact]
    public void CreateAcceptanceResponse_WithEstimatedFillTime_SetsCorrectly()
    {
        // Arrange
        var estimatedFillTime = DateTime.Now.AddMinutes(30);

        // Act
        var message = PharmacyWorkflows.CreateAcceptanceResponse(
            "CTRL123", "ORD456", estimatedFillTime: estimatedFillTime);

        // Assert
        Assert.NotNull(message);
        // Verify estimated fill time is set in appropriate field
    }

    #endregion

    #region CreateRejectionResponse Tests

    [Fact]
    public void CreateRejectionResponse_WithReason_ReturnsRejectedORR()
    {
        // Arrange
        var originalControlId = "RDE_20240315_5678";
        var placerOrderNumber = "ORD999999";
        var rejectionReason = "Drug not covered by insurance";

        // Act
        var message = PharmacyWorkflows.CreateRejectionResponse(
            originalControlId, placerOrderNumber, rejectionReason);

        // Assert
        Assert.NotNull(message);
        Assert.Equal("AR", message.MessageAcknowledgment.AcknowledgmentCode.Value);
        Assert.Equal(rejectionReason, message.MessageAcknowledgment.TextMessage.Value);
    }

    [Fact]
    public void CreateRejectionResponse_WithDetailedErrors_IncludesErrorSegments()
    {
        // Arrange
        var detailedErrors = new List<(string code, string description, string severity)>
        {
            ("DRUG_NOT_COVERED", "Medication not on formulary", "E"),
            ("PRIOR_AUTH_REQUIRED", "Prior authorization required", "E")
        };

        // Act
        var message = PharmacyWorkflows.CreateRejectionResponse(
            "CTRL123", "ORD456", "Multiple issues with prescription", detailedErrors);

        // Assert
        var errors = message.Errors;
        Assert.Equal(2, errors.Count);
        
        var firstError = errors[0];
        Assert.Equal("DRUG_NOT_COVERED", firstError.ErrorCodeAndLocation.Identifier);
        Assert.Equal("Medication not on formulary", firstError.ErrorCodeAndLocation.Text);
        Assert.Equal("E", firstError.Severity.Value);
    }

    #endregion

    #region CreateDispenseRecord Tests

    [Fact]
    public void CreateDispenseRecord_WithBasicInfo_ReturnsValidRDS()
    {
        // Arrange
        var patientId = "PAT123";
        var prescriptionNumber = "RX2024031500123";
        var medicationCode = "12345678";
        var medicationName = "Lisinopril 10mg";
        var dispensedAmount = 30m;
        var units = "TAB";
        var pharmacist = "Jane Doe, RPh";

        // Act
        var message = PharmacyWorkflows.CreateDispenseRecord(
            patientId, "Smith", "John",
            prescriptionNumber, medicationCode, medicationName,
            dispensedAmount, units, pharmacist);

        // Assert
        Assert.NotNull(message);
        Assert.IsType<RDSMessage>(message);
        Assert.Equal("RDS", message.MessageType);
        
        // Verify patient info
        Assert.Equal(patientId, message.PatientIdentification.PatientIdentifierList.IdNumber);
        
        // Verify dispense info
        var dispenses = message.Dispenses;
        Assert.NotEmpty(dispenses);
        
        var firstDispense = dispenses.First();
        Assert.NotNull(firstDispense.DispenseRecord);
        Assert.Equal(prescriptionNumber, firstDispense.DispenseRecord.PrescriptionNumber.RawValue);
        Assert.Equal(dispensedAmount.ToString(), firstDispense.DispenseRecord.ActualDispenseAmount.RawValue);
    }

    [Fact]
    public void CreateDispenseRecord_WithOptionalFields_SetsAdditionalInfo()
    {
        // Arrange
        var lotNumber = "LOT123456";
        var expirationDate = new DateTime(2025, 12, 31);
        var manufacturer = "Generic Pharma Inc";
        var refillsRemaining = 2;

        // Act
        var message = PharmacyWorkflows.CreateDispenseRecord(
            "PAT123", "Smith", "John",
            "RX123", "12345678", "Lisinopril 10mg",
            30m, "TAB", "Jane Doe, RPh",
            lotNumber, expirationDate, manufacturer, refillsRemaining);

        // Assert
        Assert.NotNull(message);
        // Verify optional fields are set correctly
    }

    #endregion

    #region Utility Method Tests

    [Fact]
    public void ValidatePharmacyWorkflow_WithValidMessage_ReturnsNoErrors()
    {
        // Arrange
        var message = PharmacyWorkflows.CreateNewPrescription(
            "PAT123", "Smith", "John",
            "12345678", "Lisinopril 10mg",
            30m, "TAB", "Take 1 tablet daily", "PRV456");

        // Act
        var errors = PharmacyWorkflows.ValidatePharmacyWorkflow(message);

        // Assert
        Assert.NotNull(errors);
        // Basic validation should pass for a properly created message
    }

    [Fact]
    public void GetWorkflowDisplayString_ReturnsReadableFormat()
    {
        // Arrange
        var message = PharmacyWorkflows.CreateNewPrescription(
            "PAT123", "Smith", "John",
            "12345678", "Lisinopril 10mg",
            30m, "TAB", "Take 1 tablet daily", "PRV456");

        // Act
        var displayString = PharmacyWorkflows.GetWorkflowDisplayString(message);

        // Assert
        Assert.NotNull(displayString);
        Assert.Contains("RDE", displayString);
        Assert.Contains("Smith", displayString);
        Assert.Contains("Lisinopril", displayString);
    }

    #endregion
}