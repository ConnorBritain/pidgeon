// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Segmint.Core.Standards.HL7.v23.Messages;
using Segmint.Core.Standards.HL7.v23.Segments;
using Segmint.Core.Standards.HL7.v23.Workflows.Builders;
using Segmint.Core.Standards.HL7.v23.Workflows.Common;
using Segmint.Core.Standards.HL7.v23.Workflows.Templates;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Segmint.Tests.Standards.HL7.v23.Workflows;

/// <summary>
/// Integration tests for workflow validation functionality.
/// Tests validation rules, error handling, and workflow-specific validation logic.
/// </summary>
public class WorkflowValidationTests
{
    #region RDE Message Validation Tests

    [Fact]
    public void ValidateRDEWorkflow_ValidPrescription_ReturnsNoErrors()
    {
        // Arrange
        var rdeMessage = PharmacyWorkflows.CreateNewPrescription(
            "PAT123", "Smith", "John",
            "12345678", "Lisinopril 10mg",
            30m, "TAB", "Take 1 tablet by mouth daily",
            "PRV456");

        // Act
        var validationResult = rdeMessage.ValidateWorkflow();

        // Assert
        Assert.True(validationResult.IsValid);
        Assert.False(validationResult.HasWarnings);
        Assert.False(validationResult.HasCriticalErrors);
        Assert.Empty(validationResult.ValidationIssues);
    }

    [Fact]
    public void ValidateRDEWorkflow_MissingMedicationCode_ReturnsError()
    {
        // Arrange
        var rdeMessage = PharmacyWorkflows.CreateNewPrescription(
            "PAT123", "Smith", "John",
            "", "Lisinopril 10mg", // Empty medication code
            30m, "TAB", "Take 1 tablet by mouth daily",
            "PRV456");

        // Act
        var validationResult = rdeMessage.ValidateWorkflow();

        // Assert
        Assert.False(validationResult.IsValid);
        Assert.Contains("Prescription workflow requires medication code", validationResult.ValidationIssues);
    }

    [Fact]
    public void ValidateRDEWorkflow_MissingPatientId_ReturnsError()
    {
        // Arrange
        var rdeMessage = PharmacyWorkflows.CreateNewPrescription(
            "", "Smith", "John", // Empty patient ID
            "12345678", "Lisinopril 10mg",
            30m, "TAB", "Take 1 tablet by mouth daily",
            "PRV456");

        // Act
        var validationResult = rdeMessage.ValidateWorkflow();

        // Assert
        Assert.False(validationResult.IsValid);
        Assert.Contains("Prescription workflow requires patient ID", validationResult.ValidationIssues);
    }

    [Fact]
    public void ValidateRDEWorkflow_MissingInstructions_ReturnsError()
    {
        // Arrange
        var rdeMessage = PharmacyWorkflows.CreateNewPrescription(
            "PAT123", "Smith", "John",
            "12345678", "Lisinopril 10mg",
            30m, "TAB", "", // Empty instructions
            "PRV456");

        // Act
        var validationResult = rdeMessage.ValidateWorkflow();

        // Assert
        Assert.False(validationResult.IsValid);
        Assert.Contains("Prescription workflow requires dosage instructions", validationResult.ValidationIssues);
    }

    [Fact]
    public void ValidateRDEWorkflow_BuilderWithMissingRequiredFields_ReturnsMultipleErrors()
    {
        // Arrange
        var rdeMessage = PharmacyWorkflowBuilder.NewPrescription()
            .WithPatient("", "Smith", "John") // Missing patient ID
            .WithMedication("", "Lisinopril 10mg") // Missing medication code
            .WithInstructions("") // Missing instructions
            .BuildWithWarnings();

        // Act
        var validationResult = rdeMessage.ValidateWorkflow();

        // Assert
        Assert.False(validationResult.IsValid);
        Assert.Contains("Prescription workflow requires patient ID", validationResult.ValidationIssues);
        Assert.Contains("Prescription workflow requires medication code", validationResult.ValidationIssues);
        Assert.Contains("Prescription workflow requires dosage instructions", validationResult.ValidationIssues);
    }

    #endregion

    #region ORR Message Validation Tests

    [Fact]
    public void ValidateORRWorkflow_ValidAcceptanceResponse_ReturnsNoErrors()
    {
        // Arrange
        var orrMessage = PharmacyWorkflows.CreateAcceptanceResponse(
            "RDE_20240315_1234",
            "ORD123456",
            "RX789012",
            "Order accepted for processing");

        // Act
        var validationResult = orrMessage.ValidateWorkflow();

        // Assert
        Assert.True(validationResult.IsValid);
        Assert.False(validationResult.HasWarnings);
        Assert.False(validationResult.HasCriticalErrors);
        Assert.Empty(validationResult.ValidationIssues);
    }

    [Fact]
    public void ValidateORRWorkflow_MissingAcknowledgmentCode_ReturnsError()
    {
        // Arrange
        var orrMessage = new ORRMessage();
        orrMessage.MessageHeader.SetBasicInfo();
        orrMessage.MessageHeader.SetMessageType("ORR", "R01", "ORR_R01");
        // Missing MSA segment setup

        // Act
        var validationResult = orrMessage.ValidateWorkflow();

        // Assert
        Assert.False(validationResult.IsValid);
        Assert.Contains("Order response workflow requires acknowledgment code", validationResult.ValidationIssues);
    }

    [Fact]
    public void ValidateORRWorkflow_MissingOriginalMessageControlId_ReturnsError()
    {
        // Arrange
        var orrMessage = new ORRMessage();
        orrMessage.MessageHeader.SetBasicInfo();
        orrMessage.MessageHeader.SetMessageType("ORR", "R01", "ORR_R01");
        orrMessage.MessageAcknowledgment.AcknowledgmentCode.SetValue("AA");
        // Missing original message control ID

        // Act
        var validationResult = orrMessage.ValidateWorkflow();

        // Assert
        Assert.False(validationResult.IsValid);
        Assert.Contains("Order response workflow requires original message control ID", validationResult.ValidationIssues);
    }

    [Fact]
    public void ValidateORRWorkflow_RejectionWithErrors_ValidatesCorrectly()
    {
        // Arrange
        var detailedErrors = new List<(string code, string description, string severity)>
        {
            ("DRUG_NOT_COVERED", "Medication not on formulary", "E"),
            ("PRIOR_AUTH_REQUIRED", "Prior authorization required", "W")
        };

        var orrMessage = PharmacyWorkflows.CreateRejectionResponse(
            "RDE_20240315_1234",
            "ORD123456",
            "Multiple issues with prescription",
            detailedErrors);

        // Act
        var validationResult = orrMessage.ValidateWorkflow();

        // Assert
        Assert.True(validationResult.IsValid);
        Assert.Equal(2, orrMessage.Errors.Count);
        
        // Check error details
        var orderResponseInfo = orrMessage.GetOrderResponseInfo();
        Assert.True(orderResponseInfo.HasErrors);
        Assert.Equal(2, orderResponseInfo.ErrorCount);
        Assert.Equal("AR", orderResponseInfo.AcknowledgmentCode);
    }

    #endregion

    #region RDS Message Validation Tests

    [Fact]
    public void ValidateRDSWorkflow_ValidDispenseRecord_ReturnsNoErrors()
    {
        // Arrange
        var rdsMessage = PharmacyWorkflows.CreateDispenseRecord(
            "PAT123", "Smith", "John",
            "RX2024031500123", "12345678", "Lisinopril 10mg",
            30m, "TAB", "Jane Doe, RPh");

        // Act
        var validationResult = rdsMessage.ValidateWorkflow();

        // Assert
        Assert.True(validationResult.IsValid);
        Assert.False(validationResult.HasWarnings);
        Assert.False(validationResult.HasCriticalErrors);
        Assert.Empty(validationResult.ValidationIssues);
    }

    [Fact]
    public void ValidateRDSWorkflow_NoDispenseRecords_ReturnsError()
    {
        // Arrange
        var rdsMessage = new RDSMessage();
        rdsMessage.MessageHeader.SetBasicInfo();
        rdsMessage.MessageHeader.SetMessageType("RDS", "O01", "RDS_O01");
        rdsMessage.PatientIdentification.SetBasicInfo("PAT123", "Smith", "John");
        // No dispense records added

        // Act
        var validationResult = rdsMessage.ValidateWorkflow();

        // Assert
        Assert.False(validationResult.IsValid);
        Assert.Contains("Dispense workflow requires at least one dispense record", validationResult.ValidationIssues);
    }

    [Fact]
    public void ValidateRDSWorkflow_MissingDispenseAmount_ReturnsError()
    {
        // Arrange
        var rdsMessage = new RDSMessage();
        rdsMessage.MessageHeader.SetBasicInfo();
        rdsMessage.MessageHeader.SetMessageType("RDS", "O01", "RDS_O01");
        rdsMessage.PatientIdentification.SetBasicInfo("PAT123", "Smith", "John");
        
        // Add dispense record without amount
        var dispenseRecord = new RDSMessage.DispenseInfo();
        dispenseRecord.DispenseRecord = new RXDSegment();
        dispenseRecord.DispenseRecord.DispenseGiveCode.SetValue("12345678^Lisinopril 10mg");
        // Missing dispense amount
        rdsMessage.Dispenses.Add(dispenseRecord);

        // Act
        var validationResult = rdsMessage.ValidateWorkflow();

        // Assert
        Assert.False(validationResult.IsValid);
        Assert.Contains("Dispense workflow requires dispense amount", validationResult.ValidationIssues);
    }

    #endregion

    #region Workflow Extension Validation Tests

    [Fact]
    public void IsReadyForTransmission_ValidMessage_ReturnsTrue()
    {
        // Arrange
        var rdeMessage = PharmacyWorkflows.CreateNewPrescription(
            "PAT123", "Smith", "John",
            "12345678", "Lisinopril 10mg",
            30m, "TAB", "Take 1 tablet by mouth daily",
            "PRV456");

        // Act
        var isReady = rdeMessage.IsReadyForTransmission();

        // Assert
        Assert.True(isReady);
    }

    [Fact]
    public void IsReadyForTransmission_InvalidMessage_ReturnsFalse()
    {
        // Arrange
        var rdeMessage = PharmacyWorkflows.CreateNewPrescription(
            "", "Smith", "John", // Missing patient ID
            "12345678", "Lisinopril 10mg",
            30m, "TAB", "Take 1 tablet by mouth daily",
            "PRV456");

        // Act
        var isReady = rdeMessage.IsReadyForTransmission();

        // Assert
        Assert.False(isReady);
    }

    [Fact]
    public void WorkflowSummary_ExtractsCorrectInformation()
    {
        // Arrange
        var rdeMessage = PharmacyWorkflows.CreateNewPrescription(
            "PAT123", "Smith", "John",
            "12345678", "Lisinopril 10mg",
            30m, "TAB", "Take 1 tablet by mouth daily",
            "PRV456");

        // Act
        var summary = rdeMessage.GetWorkflowSummary();

        // Assert
        Assert.Equal("RDE", summary.MessageType);
        Assert.Equal("O01", summary.TriggerEvent);
        Assert.Equal("PAT123", summary.PatientId);
        Assert.Equal("John Smith", summary.PatientName);
        Assert.Equal("Prescription Order", summary.WorkflowType);
        Assert.Equal("Valid", summary.ValidationStatus);
        Assert.NotNull(summary.MessageControlId);
        Assert.NotNull(summary.MessageDateTime);
    }

    #endregion

    #region Pharmacy-Specific Validation Tests

    [Fact]
    public void ValidatePharmacyWorkflow_ControlledSubstance_RequiresDEA()
    {
        // Arrange
        var rdeMessage = PharmacyWorkflowBuilder.NewPrescription()
            .WithPatient("PAT123", "Smith", "John")
            .WithMedication("00406055705", "Oxycodone 5mg")
            .WithInstructions("Take 1 tablet every 6 hours as needed for pain")
            .WithProvider("PRV456", "Johnson", "Robert")
            .AsControlledSubstance("II", "BJ1234567")
            .Build();

        // Act
        var validationResult = WorkflowValidation.ValidatePrescriptionWorkflow(rdeMessage);

        // Assert
        Assert.True(validationResult.IsValid);
        Assert.False(validationResult.HasCriticalErrors);
        
        // Verify controlled substance note is present
        var notes = rdeMessage.Where(s => s.SegmentId == "NTE").Cast<NTESegment>().ToList();
        Assert.True(notes.Any(n => n.Comment.Value.Contains("Schedule II")));
        Assert.True(notes.Any(n => n.Comment.Value.Contains("BJ1234567")));
    }

    [Fact]
    public void ValidatePharmacyWorkflow_ControlledSubstanceScheduleII_NoRefills()
    {
        // Arrange
        var rdeMessage = PharmacyWorkflows.CreateControlledSubstancePrescription(
            "PAT123", "Smith", "John", new DateTime(1980, 1, 1),
            "00406055705", "Oxycodone 5mg",
            20m, "TAB", "Take as directed",
            "PRV456", "BJ1234567", "II", refills: 0);

        // Act
        var validationResult = WorkflowValidation.ValidatePrescriptionWorkflow(rdeMessage);

        // Assert
        Assert.True(validationResult.IsValid);
        Assert.Equal("0", rdeMessage.PharmacyOrder.NumberOfRefills.RawValue);
    }

    [Fact]
    public void ValidatePharmacyWorkflow_PatientDateOfBirth_ValidatesCorrectly()
    {
        // Arrange
        var rdeMessage = PharmacyWorkflows.CreateComprehensivePrescription(
            "PAT123", "Smith", "John", new DateTime(1980, 5, 15), "M",
            "12345678", "Lisinopril", 10m, "mg", "TAB",
            30m, "TAB", "Take 1 tablet by mouth daily",
            "PRV456", "Johnson", "Robert");

        // Act
        var validationResult = WorkflowValidation.ValidatePrescriptionWorkflow(rdeMessage);

        // Assert
        Assert.True(validationResult.IsValid);
        Assert.Equal(new DateTime(1980, 5, 15), rdeMessage.PatientIdentification.DateTimeOfBirth.ToDateTime());
        Assert.Equal("M", rdeMessage.PatientIdentification.AdministrativeSex.Value);
    }

    #endregion

    #region Cross-Message Validation Tests

    [Fact]
    public void ValidateWorkflowContinuity_RDE_to_ORR_MessageControlIds()
    {
        // Arrange
        var rdeMessage = PharmacyWorkflows.CreateNewPrescription(
            "PAT123", "Smith", "John",
            "12345678", "Lisinopril 10mg",
            30m, "TAB", "Take 1 tablet by mouth daily",
            "PRV456");

        var orrMessage = PharmacyWorkflows.CreateAcceptanceResponse(
            rdeMessage.MessageHeader.MessageControlId.Value,
            rdeMessage.CommonOrder.PlacerOrderNumber.Value,
            "RX789012");

        // Act
        var rdeValidation = rdeMessage.ValidateWorkflow();
        var orrValidation = orrMessage.ValidateWorkflow();

        // Assert
        Assert.True(rdeValidation.IsValid);
        Assert.True(orrValidation.IsValid);
        
        // Verify message control ID continuity
        Assert.Equal(rdeMessage.MessageHeader.MessageControlId.Value,
                     orrMessage.MessageAcknowledgment.MessageControlId.Value);
    }

    [Fact]
    public void ValidateWorkflowContinuity_PatientConsistency_AcrossMessages()
    {
        // Arrange
        var patientId = "PAT123";
        var lastName = "Smith";
        var firstName = "John";

        var rdeMessage = PharmacyWorkflows.CreateNewPrescription(
            patientId, lastName, firstName,
            "12345678", "Lisinopril 10mg",
            30m, "TAB", "Take 1 tablet by mouth daily",
            "PRV456");

        var rdsMessage = PharmacyWorkflows.CreateDispenseRecord(
            patientId, lastName, firstName,
            "RX2024031500123", "12345678", "Lisinopril 10mg",
            30m, "TAB", "Jane Doe, RPh");

        // Act
        var rdeSummary = rdeMessage.GetWorkflowSummary();
        var rdsSummary = rdsMessage.GetWorkflowSummary();

        // Assert
        Assert.Equal(rdeSummary.PatientId, rdsSummary.PatientId);
        Assert.Equal(rdeSummary.PatientName, rdsSummary.PatientName);
        Assert.Equal(patientId, rdeSummary.PatientId);
        Assert.Equal(patientId, rdsSummary.PatientId);
        Assert.Equal("John Smith", rdeSummary.PatientName);
        Assert.Equal("John Smith", rdsSummary.PatientName);
    }

    #endregion

    #region Performance and Stress Tests

    [Fact]
    public void ValidateWorkflow_HighVolumeValidation_PerformsWell()
    {
        // Arrange
        var messages = new List<RDEMessage>();
        for (int i = 0; i < 100; i++)
        {
            var message = PharmacyWorkflows.CreateNewPrescription(
                $"PAT{i:D3}", "TestPatient", $"Patient{i}",
                $"1234567{i % 10}", $"Medication{i}",
                30m, "TAB", "Take 1 tablet by mouth daily",
                $"PRV{i:D3}");
            messages.Add(message);
        }

        // Act
        var startTime = DateTime.Now;
        var validationResults = messages.Select(m => m.ValidateWorkflow()).ToList();
        var endTime = DateTime.Now;

        // Assert
        Assert.Equal(100, validationResults.Count);
        Assert.All(validationResults, result => Assert.True(result.IsValid));
        
        // Performance assertion - should complete in reasonable time
        var duration = endTime - startTime;
        Assert.True(duration.TotalSeconds < 5, $"Validation took {duration.TotalSeconds} seconds");
    }

    [Fact]
    public void ValidateWorkflow_ComplexMessageWithManySegments_ValidatesCorrectly()
    {
        // Arrange
        var rdeMessage = PharmacyWorkflowBuilder.NewPrescription()
            .WithPatientDemographics("PAT123", "Smith", "John", "Robert", 
                new DateTime(1980, 5, 15), "M", "123456789")
            .WithPatientAddress("123 Main St", "Anytown", "CA", "12345", "USA")
            .WithPatientPhones("555-1234", "555-5678")
            .WithMedication("00406055705", "Oxycodone 5mg", 5m, "mg", "TAB")
            .WithInstructions("Take 1 tablet every 6 hours as needed for pain", 20m, "TAB", 0, 5)
            .WithProvider("PRV456", "Johnson", "Robert", "MD")
            .AsControlledSubstance("II", "BJ1234567")
            .WithPriority("S")
            .WithNote("Patient allergic to sulfa drugs", "ALLERGY")
            .WithNote("Insurance: Blue Cross Blue Shield", "INSURANCE")
            .WithNote("Copay: $10.00", "COPAY")
            .WithNote("Patient counseled on medication", "COUNSELING")
            .Build();

        // Act
        var validationResult = rdeMessage.ValidateWorkflow();

        // Assert
        Assert.True(validationResult.IsValid);
        Assert.False(validationResult.HasCriticalErrors);
        
        // Verify all segments are present and valid
        var segments = rdeMessage.ToList();
        Assert.True(segments.Count > 10); // Should have many segments
        
        // Verify specific segments
        Assert.NotNull(rdeMessage.GetSegment<MSHSegment>());
        Assert.NotNull(rdeMessage.GetSegment<PIDSegment>());
        Assert.NotNull(rdeMessage.GetSegment<ORCSegment>());
        Assert.NotNull(rdeMessage.GetSegment<RXESegment>());
        
        // Verify notes
        var notes = segments.Where(s => s.SegmentId == "NTE").Cast<NTESegment>().ToList();
        Assert.True(notes.Count >= 5); // Should have multiple notes
    }

    #endregion
}