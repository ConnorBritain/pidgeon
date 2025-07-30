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
/// End-to-end integration tests for complete pharmacy workflows.
/// Tests the full RDE → ORR → RDS workflow cycle.
/// </summary>
public class EndToEndWorkflowTests
{
    #region Basic Workflow Tests

    [Fact]
    public void BasicPharmacyWorkflow_RDE_to_ORR_to_RDS_Success()
    {
        // Step 1: Create a prescription order (RDE)
        var rdeMessage = PharmacyWorkflows.CreateNewPrescription(
            "PAT123", "Smith", "John",
            "12345678", "Lisinopril 10mg",
            30m, "TAB", "Take 1 tablet by mouth daily",
            "PRV456", refills: 3, daysSupply: 30);

        // Assert RDE message is valid
        Assert.NotNull(rdeMessage);
        Assert.Equal("RDE", rdeMessage.MessageType);
        Assert.Equal("PAT123", rdeMessage.PatientIdentification.PatientIdentifierList.IdNumber);
        Assert.Equal("12345678", rdeMessage.PharmacyOrder.GiveCode.Identifier);
        
        // Step 2: Create an acceptance response (ORR)
        var originalControlId = rdeMessage.MessageHeader.MessageControlId.Value;
        var placerOrderNumber = rdeMessage.CommonOrder.PlacerOrderNumber.Value;
        var fillerOrderNumber = "RX789012";
        
        var orrMessage = PharmacyWorkflows.CreateAcceptanceResponse(
            originalControlId, placerOrderNumber, fillerOrderNumber, 
            "Order accepted for processing");

        // Assert ORR message is valid
        Assert.NotNull(orrMessage);
        Assert.Equal("ORR", orrMessage.MessageType);
        Assert.Equal("AA", orrMessage.MessageAcknowledgment.AcknowledgmentCode.Value);
        Assert.Equal(originalControlId, orrMessage.MessageAcknowledgment.MessageControlId.Value);
        
        // Step 3: Create a dispense record (RDS)
        var rdsMessage = PharmacyWorkflows.CreateDispenseRecord(
            "PAT123", "Smith", "John",
            "RX2024031500123", "12345678", "Lisinopril 10mg",
            30m, "TAB", "Jane Doe, RPh");

        // Assert RDS message is valid
        Assert.NotNull(rdsMessage);
        Assert.Equal("RDS", rdsMessage.MessageType);
        Assert.Equal("PAT123", rdsMessage.PatientIdentification.PatientIdentifierList.IdNumber);
        Assert.NotEmpty(rdsMessage.Dispenses);
        
        // Step 4: Verify workflow summary information
        var rdeSummary = rdeMessage.GetWorkflowSummary();
        var orrSummary = orrMessage.GetWorkflowSummary();
        var rdsSummary = rdsMessage.GetWorkflowSummary();
        
        Assert.Equal("Prescription Order", rdeSummary.WorkflowType);
        Assert.Equal("Order Response", orrSummary.WorkflowType);
        Assert.Equal("Medication Dispense", rdsSummary.WorkflowType);
        
        // All messages should have same patient
        Assert.Equal("PAT123", rdeSummary.PatientId);
        Assert.Equal("PAT123", rdsSummary.PatientId);
    }

    [Fact]
    public void BasicPharmacyWorkflow_RDE_to_ORR_Rejection()
    {
        // Step 1: Create a prescription order (RDE)
        var rdeMessage = PharmacyWorkflows.CreateNewPrescription(
            "PAT123", "Smith", "John",
            "99999999", "Expensive Drug",
            30m, "TAB", "Take 1 tablet by mouth daily",
            "PRV456");

        // Step 2: Create a rejection response (ORR)
        var originalControlId = rdeMessage.MessageHeader.MessageControlId.Value;
        var placerOrderNumber = rdeMessage.CommonOrder.PlacerOrderNumber.Value;
        var rejectionReason = "Drug not covered by insurance";
        
        var detailedErrors = new List<(string code, string description, string severity)>
        {
            ("DRUG_NOT_COVERED", "Medication not on formulary", "E"),
            ("PRIOR_AUTH_REQUIRED", "Prior authorization required", "E")
        };
        
        var orrMessage = PharmacyWorkflows.CreateRejectionResponse(
            originalControlId, placerOrderNumber, rejectionReason, detailedErrors);

        // Assert ORR rejection message is valid
        Assert.NotNull(orrMessage);
        Assert.Equal("ORR", orrMessage.MessageType);
        Assert.Equal("AR", orrMessage.MessageAcknowledgment.AcknowledgmentCode.Value);
        Assert.Equal(originalControlId, orrMessage.MessageAcknowledgment.MessageControlId.Value);
        Assert.Equal(rejectionReason, orrMessage.MessageAcknowledgment.TextMessage.Value);
        
        // Verify error details
        Assert.Equal(2, orrMessage.Errors.Count);
        Assert.Equal("DRUG_NOT_COVERED", orrMessage.Errors[0].ErrorCodeAndLocation.Identifier);
        Assert.Equal("PRIOR_AUTH_REQUIRED", orrMessage.Errors[1].ErrorCodeAndLocation.Identifier);
        
        // Step 3: Verify workflow information
        var orrResponseInfo = orrMessage.GetOrderResponseInfo();
        Assert.True(orrResponseInfo.HasErrors);
        Assert.Equal(2, orrResponseInfo.ErrorCount);
        Assert.Equal("AR", orrResponseInfo.AcknowledgmentCode);
    }

    #endregion

    #region Complex Workflow Tests

    [Fact]
    public void ComplexPharmacyWorkflow_ControlledSubstance_FullCycle()
    {
        // Step 1: Create a controlled substance prescription using builder
        var rdeMessage = PharmacyWorkflowBuilder.NewPrescription()
            .WithPatientDemographics("PAT123", "Smith", "John", "Robert", 
                new DateTime(1975, 3, 20), "M", "123456789")
            .WithMedication("00406055705", "Oxycodone 5mg", 5m, "mg", "TAB")
            .WithInstructions("Take 1 tablet every 6 hours as needed for pain", 20m, "TAB", 0, 5)
            .WithProvider("PRV456", "Johnson", "Robert", "MD")
            .AsControlledSubstance("II", "BJ1234567")
            .WithPriority("S")
            .WithNote("Patient has chronic pain condition", "CLINICAL")
            .Build();

        // Assert controlled substance prescription
        Assert.NotNull(rdeMessage);
        Assert.Equal("00406055705", rdeMessage.PharmacyOrder.GiveCode.Identifier);
        Assert.Equal("Oxycodone 5mg", rdeMessage.PharmacyOrder.GiveCode.Text);
        
        // Verify controlled substance notes
        var notes = rdeMessage.Where(s => s.SegmentId == "NTE").Cast<NTESegment>().ToList();
        Assert.True(notes.Any(n => n.Comment.Value.Contains("Schedule II")));
        Assert.True(notes.Any(n => n.Comment.Value.Contains("BJ1234567")));
        Assert.True(notes.Any(n => n.Comment.Value.Contains("Priority: S")));
        Assert.True(notes.Any(n => n.Comment.Value.Contains("chronic pain condition")));
        
        // Step 2: Create acceptance response with estimated fill time
        var estimatedFillTime = DateTime.Now.AddMinutes(45);
        var orrMessage = PharmacyWorkflows.CreateAcceptanceResponse(
            rdeMessage.MessageHeader.MessageControlId.Value,
            rdeMessage.CommonOrder.PlacerOrderNumber.Value,
            "CS789012",
            "Controlled substance order accepted",
            estimatedFillTime);

        // Assert acceptance response
        Assert.NotNull(orrMessage);
        Assert.Equal("AA", orrMessage.MessageAcknowledgment.AcknowledgmentCode.Value);
        
        // Step 3: Create dispense record with lot number and expiration
        var lotNumber = "LOT123456";
        var expirationDate = new DateTime(2025, 12, 31);
        var manufacturer = "Mallinckrodt Pharmaceuticals";
        var refillsRemaining = 0; // No refills for Schedule II
        
        var rdsMessage = PharmacyWorkflows.CreateDispenseRecord(
            "PAT123", "Smith", "John",
            "CS2024031500123", "00406055705", "Oxycodone 5mg",
            20m, "TAB", "Jane Doe, RPh",
            lotNumber, expirationDate, manufacturer, refillsRemaining);

        // Assert dispense record
        Assert.NotNull(rdsMessage);
        Assert.Equal("RDS", rdsMessage.MessageType);
        Assert.NotEmpty(rdsMessage.Dispenses);
        
        // Verify dispense details
        var dispenseInfo = rdsMessage.GetDispenseInfo();
        Assert.NotEmpty(dispenseInfo);
        Assert.Equal("00406055705", dispenseInfo[0].MedicationCode);
        Assert.Equal("Oxycodone 5mg", dispenseInfo[0].MedicationName);
        Assert.Equal("20", dispenseInfo[0].DispensedAmount);
        
        // Step 4: Validate complete workflow
        var rdeValidation = rdeMessage.ValidateWorkflow();
        var orrValidation = orrMessage.ValidateWorkflow();
        var rdsValidation = rdsMessage.ValidateWorkflow();
        
        Assert.True(rdeValidation.IsValid);
        Assert.True(orrValidation.IsValid);
        Assert.True(rdsValidation.IsValid);
    }

    [Fact]
    public void ComprehensivePharmacyWorkflow_WithInsuranceAndAllergies()
    {
        // Step 1: Create comprehensive prescription with patient details
        var rdeMessage = PharmacyWorkflows.CreateComprehensivePrescription(
            "PAT123", "Smith", "John", new DateTime(1980, 5, 15), "M",
            "12345678", "Lisinopril", 10m, "mg", "TAB",
            30m, "TAB", "Take 1 tablet by mouth daily",
            "PRV456", "Johnson", "Robert",
            refills: 5, daysSupply: 30);

        // Add allergy information
        rdeMessage.WithWorkflowNote("Patient allergic to Sulfa drugs", "ALLERGY");
        rdeMessage.WithWorkflowNote("Insurance: Blue Cross Blue Shield", "INSURANCE");
        rdeMessage.WithWorkflowNote("Copay: $5.00", "COPAY");

        // Assert comprehensive prescription
        Assert.NotNull(rdeMessage);
        Assert.Equal(new DateTime(1980, 5, 15), rdeMessage.PatientIdentification.DateTimeOfBirth.ToDateTime());
        Assert.Equal("M", rdeMessage.PatientIdentification.AdministrativeSex.Value);
        
        // Verify notes
        var notes = rdeMessage.Where(s => s.SegmentId == "NTE").Cast<NTESegment>().ToList();
        Assert.True(notes.Any(n => n.Comment.Value.Contains("Sulfa drugs")));
        Assert.True(notes.Any(n => n.Comment.Value.Contains("Blue Cross")));
        Assert.True(notes.Any(n => n.Comment.Value.Contains("$5.00")));
        
        // Step 2: Create acceptance response
        var orrMessage = PharmacyWorkflows.CreateAcceptanceResponse(
            rdeMessage.MessageHeader.MessageControlId.Value,
            rdeMessage.CommonOrder.PlacerOrderNumber.Value,
            "RX789012",
            "Order accepted - Generic substitution available");

        // Step 3: Create dispense record
        var rdsMessage = PharmacyWorkflows.CreateDispenseRecord(
            "PAT123", "Smith", "John",
            "RX2024031500123", "12345678", "Lisinopril 10mg (Generic)",
            30m, "TAB", "Jane Doe, RPh",
            "LOT789012", new DateTime(2025, 6, 30), "Teva Pharmaceuticals", 5);

        // Step 4: Verify prescription information extraction
        var prescriptionInfo = rdeMessage.GetPrescriptionInfo();
        Assert.NotNull(prescriptionInfo);
        Assert.Equal("PAT123", prescriptionInfo.PatientId);
        Assert.Equal("John Smith", prescriptionInfo.PatientName);
        Assert.Equal("12345678", prescriptionInfo.MedicationCode);
        Assert.Equal("Lisinopril", prescriptionInfo.MedicationName);
        Assert.Equal("Take 1 tablet by mouth daily", prescriptionInfo.Instructions);
        Assert.Equal("PRV456", prescriptionInfo.OrderingProvider);
        
        // Step 5: Verify order response information
        var orderResponseInfo = orrMessage.GetOrderResponseInfo();
        Assert.NotNull(orderResponseInfo);
        Assert.Equal("AA", orderResponseInfo.AcknowledgmentCode);
        Assert.Equal("Order accepted - Generic substitution available", orderResponseInfo.ResponseText);
        Assert.False(orderResponseInfo.HasErrors);
        Assert.Equal(0, orderResponseInfo.ErrorCount);
        
        // Step 6: Verify dispense information
        var dispenseInfo = rdsMessage.GetDispenseInfo();
        Assert.NotEmpty(dispenseInfo);
        Assert.Equal("12345678", dispenseInfo[0].MedicationCode);
        Assert.Equal("Lisinopril 10mg (Generic)", dispenseInfo[0].MedicationName);
        Assert.Equal("30", dispenseInfo[0].DispensedAmount);
        Assert.Equal("TAB", dispenseInfo[0].DispenseUnits);
    }

    #endregion

    #region Workflow State Tests

    [Fact]
    public void WorkflowStateTransitions_TrackMessageFlow()
    {
        var workflowStates = new List<WorkflowSummary>();
        
        // State 1: Prescription Created
        var rdeMessage = PharmacyWorkflows.CreateNewPrescription(
            "PAT123", "Smith", "John",
            "12345678", "Lisinopril 10mg",
            30m, "TAB", "Take 1 tablet by mouth daily",
            "PRV456");
        
        workflowStates.Add(rdeMessage.GetWorkflowSummary());
        
        // State 2: Order Accepted
        var orrMessage = PharmacyWorkflows.CreateAcceptanceResponse(
            rdeMessage.MessageHeader.MessageControlId.Value,
            rdeMessage.CommonOrder.PlacerOrderNumber.Value,
            "RX789012",
            "Order accepted for processing");
        
        workflowStates.Add(orrMessage.GetWorkflowSummary());
        
        // State 3: Medication Dispensed
        var rdsMessage = PharmacyWorkflows.CreateDispenseRecord(
            "PAT123", "Smith", "John",
            "RX2024031500123", "12345678", "Lisinopril 10mg",
            30m, "TAB", "Jane Doe, RPh");
        
        workflowStates.Add(rdsMessage.GetWorkflowSummary());
        
        // Verify workflow progression
        Assert.Equal(3, workflowStates.Count);
        Assert.Equal("Prescription Order", workflowStates[0].WorkflowType);
        Assert.Equal("Order Response", workflowStates[1].WorkflowType);
        Assert.Equal("Medication Dispense", workflowStates[2].WorkflowType);
        
        // Verify patient consistency across workflow
        Assert.All(workflowStates, state => Assert.Equal("PAT123", state.PatientId));
        Assert.All(workflowStates, state => Assert.Equal("John Smith", state.PatientName));
        
        // Verify message types
        Assert.Equal("RDE", workflowStates[0].MessageType);
        Assert.Equal("ORR", workflowStates[1].MessageType);
        Assert.Equal("RDS", workflowStates[2].MessageType);
    }

    [Fact]
    public void WorkflowValidation_EndToEndValidation()
    {
        // Create a complete workflow
        var rdeMessage = PharmacyWorkflows.CreateNewPrescription(
            "PAT123", "Smith", "John",
            "12345678", "Lisinopril 10mg",
            30m, "TAB", "Take 1 tablet by mouth daily",
            "PRV456");
        
        var orrMessage = PharmacyWorkflows.CreateAcceptanceResponse(
            rdeMessage.MessageHeader.MessageControlId.Value,
            rdeMessage.CommonOrder.PlacerOrderNumber.Value,
            "RX789012");
        
        var rdsMessage = PharmacyWorkflows.CreateDispenseRecord(
            "PAT123", "Smith", "John",
            "RX2024031500123", "12345678", "Lisinopril 10mg",
            30m, "TAB", "Jane Doe, RPh");
        
        // Validate all messages
        var rdeValidation = rdeMessage.ValidateWorkflow();
        var orrValidation = orrMessage.ValidateWorkflow();
        var rdsValidation = rdsMessage.ValidateWorkflow();
        
        // All should be valid
        Assert.True(rdeValidation.IsValid);
        Assert.True(orrValidation.IsValid);
        Assert.True(rdsValidation.IsValid);
        
        // All should be ready for transmission
        Assert.True(rdeMessage.IsReadyForTransmission());
        Assert.True(orrMessage.IsReadyForTransmission());
        Assert.True(rdsMessage.IsReadyForTransmission());
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public void ErrorHandlingWorkflow_InvalidMedication_ProperErrorResponse()
    {
        // Step 1: Create prescription with invalid medication
        var rdeMessage = PharmacyWorkflows.CreateNewPrescription(
            "PAT123", "Smith", "John",
            "INVALID123", "Unknown Drug",
            30m, "TAB", "Take 1 tablet by mouth daily",
            "PRV456");

        // Step 2: Create error response
        var detailedErrors = new List<(string code, string description, string severity)>
        {
            ("INVALID_DRUG_CODE", "Drug code not found in formulary", "E"),
            ("DRUG_LOOKUP_FAILED", "Unable to verify drug information", "W")
        };
        
        var orrMessage = PharmacyWorkflows.CreateRejectionResponse(
            rdeMessage.MessageHeader.MessageControlId.Value,
            rdeMessage.CommonOrder.PlacerOrderNumber.Value,
            "Invalid medication code",
            detailedErrors);

        // Step 3: Verify error handling
        var orderResponseInfo = orrMessage.GetOrderResponseInfo();
        Assert.True(orderResponseInfo.HasErrors);
        Assert.Equal(2, orderResponseInfo.ErrorCount);
        Assert.Equal("AR", orderResponseInfo.AcknowledgmentCode);
        
        // Verify error details
        var errors = orderResponseInfo.Errors;
        Assert.Equal("INVALID_DRUG_CODE", errors[0].ErrorCode);
        Assert.Equal("Drug code not found in formulary", errors[0].ErrorText);
        Assert.Equal("E", errors[0].Severity);
        
        Assert.Equal("DRUG_LOOKUP_FAILED", errors[1].ErrorCode);
        Assert.Equal("Unable to verify drug information", errors[1].ErrorText);
        Assert.Equal("W", errors[1].Severity);
    }

    [Fact]
    public void PartialDispenseWorkflow_PartialFillScenario()
    {
        // Step 1: Create prescription for 90 tablets
        var rdeMessage = PharmacyWorkflows.CreateNewPrescription(
            "PAT123", "Smith", "John",
            "12345678", "Lisinopril 10mg",
            90m, "TAB", "Take 1 tablet by mouth daily",
            "PRV456", refills: 3);

        // Step 2: Accept order but note partial fill
        var orrMessage = PharmacyWorkflows.CreateAcceptanceResponse(
            rdeMessage.MessageHeader.MessageControlId.Value,
            rdeMessage.CommonOrder.PlacerOrderNumber.Value,
            "RX789012",
            "Order accepted - Partial fill: 30 tablets available");

        // Step 3: Dispense partial amount
        var rdsMessage = PharmacyWorkflows.CreateDispenseRecord(
            "PAT123", "Smith", "John",
            "RX2024031500123", "12345678", "Lisinopril 10mg",
            30m, "TAB", "Jane Doe, RPh", // Only 30 tablets dispensed
            "LOT123456", new DateTime(2025, 12, 31), "Generic Pharma", 3);

        // Add note about partial fill
        rdsMessage.WithWorkflowNote("Partial fill: 30 of 90 tablets dispensed. Remaining 60 tablets to follow.", "PARTIAL_FILL");

        // Step 4: Verify partial dispense
        var dispenseInfo = rdsMessage.GetDispenseInfo();
        Assert.NotEmpty(dispenseInfo);
        Assert.Equal("30", dispenseInfo[0].DispensedAmount);
        
        // Verify partial fill note
        var notes = rdsMessage.Where(s => s.SegmentId == "NTE").Cast<NTESegment>().ToList();
        Assert.True(notes.Any(n => n.Comment.Value.Contains("Partial fill")));
        Assert.True(notes.Any(n => n.Comment.Value.Contains("30 of 90 tablets")));
    }

    #endregion

    #region Performance Tests

    [Fact]
    public void HighVolumeWorkflow_ProcessMultiplePrescriptions()
    {
        var prescriptions = new List<string>
        {
            "Lisinopril 10mg", "Metformin 500mg", "Atorvastatin 20mg",
            "Amlodipine 5mg", "Metoprolol 50mg", "Omeprazole 20mg"
        };

        var workflowMessages = new List<(RDEMessage rde, ORRMessage orr, RDSMessage rds)>();

        // Process multiple prescriptions
        for (int i = 0; i < prescriptions.Count; i++)
        {
            var patientId = $"PAT{i + 1:D3}";
            var medicationCode = $"1234567{i}";
            var providerId = $"PRV{i + 1:D3}";
            
            // Create RDE
            var rdeMessage = PharmacyWorkflows.CreateNewPrescription(
                patientId, "TestPatient", $"Patient{i + 1}",
                medicationCode, prescriptions[i],
                30m, "TAB", "Take 1 tablet by mouth daily",
                providerId);

            // Create ORR
            var orrMessage = PharmacyWorkflows.CreateAcceptanceResponse(
                rdeMessage.MessageHeader.MessageControlId.Value,
                rdeMessage.CommonOrder.PlacerOrderNumber.Value,
                $"RX{i + 1:D6}");

            // Create RDS
            var rdsMessage = PharmacyWorkflows.CreateDispenseRecord(
                patientId, "TestPatient", $"Patient{i + 1}",
                $"RX202403150{i:D4}", medicationCode, prescriptions[i],
                30m, "TAB", "Jane Doe, RPh");

            workflowMessages.Add((rdeMessage, orrMessage, rdsMessage));
        }

        // Verify all workflows were created successfully
        Assert.Equal(prescriptions.Count, workflowMessages.Count);
        
        // Verify all messages are valid
        foreach (var (rde, orr, rds) in workflowMessages)
        {
            Assert.True(rde.ValidateWorkflow().IsValid);
            Assert.True(orr.ValidateWorkflow().IsValid);
            Assert.True(rds.ValidateWorkflow().IsValid);
        }
        
        // Verify unique control IDs
        var controlIds = workflowMessages.Select(w => w.rde.MessageHeader.MessageControlId.Value).ToList();
        Assert.Equal(controlIds.Count, controlIds.Distinct().Count());
    }

    #endregion
}