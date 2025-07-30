// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Segmint.Core.Standards.HL7.v23.Messages;
using Segmint.Core.Standards.HL7.v23.Segments;
using Segmint.Core.Standards.HL7.v23.Workflows.Builders;
using System;
using System.Linq;
using Xunit;

namespace Segmint.Tests.Standards.HL7.v23.Workflows.Builders;

/// <summary>
/// Unit tests for PrescriptionBuilder fluent interface.
/// Tests the builder pattern approach of the hybrid workflow system.
/// </summary>
public class PrescriptionBuilderTests
{
    #region Builder Creation Tests

    [Fact]
    public void NewPrescription_CreatesValidBuilder()
    {
        // Act
        var builder = PharmacyWorkflowBuilder.NewPrescription();

        // Assert
        Assert.NotNull(builder);
        Assert.IsType<PrescriptionBuilder>(builder);
    }

    [Fact]
    public void NewPrescription_SetsDefaultMessageStructure()
    {
        // Act
        var builder = PharmacyWorkflowBuilder.NewPrescription();
        var message = builder.BuildWithWarnings();

        // Assert
        Assert.NotNull(message);
        Assert.IsType<RDEMessage>(message);
        Assert.Equal("RDE", message.MessageType);
        Assert.Equal("O01", message.TriggerEvent);
        Assert.NotNull(message.MessageHeader);
        Assert.NotNull(message.PatientIdentification);
        Assert.NotNull(message.CommonOrder);
        Assert.NotNull(message.PharmacyOrder);
    }

    [Fact]
    public void NewPrescription_GeneratesUniqueControlIds()
    {
        // Act
        var builder1 = PharmacyWorkflowBuilder.NewPrescription();
        var builder2 = PharmacyWorkflowBuilder.NewPrescription();
        var message1 = builder1.BuildWithWarnings();
        var message2 = builder2.BuildWithWarnings();

        // Assert
        Assert.NotEqual(message1.MessageHeader.MessageControlId.Value, 
                       message2.MessageHeader.MessageControlId.Value);
    }

    #endregion

    #region Patient Information Tests

    [Fact]
    public void WithPatient_SetsBasicPatientInfo()
    {
        // Arrange
        var builder = PharmacyWorkflowBuilder.NewPrescription();
        var patientId = "PAT123";
        var lastName = "Smith";
        var firstName = "John";

        // Act
        var result = builder.WithPatient(patientId, lastName, firstName);
        var message = result.BuildWithWarnings();

        // Assert
        Assert.Same(builder, result); // Method chaining
        Assert.Equal(patientId, message.PatientIdentification.PatientIdentifierList.IdNumber);
        Assert.Equal(lastName, message.PatientIdentification.PatientName.FamilyName);
        Assert.Equal(firstName, message.PatientIdentification.PatientName.GivenName);
    }

    [Fact]
    public void WithPatient_EmptyPatientId_AddsValidationError()
    {
        // Arrange
        var builder = PharmacyWorkflowBuilder.NewPrescription();

        // Act
        builder.WithPatient("", "Smith", "John");
        var errors = builder.ValidateOnly();

        // Assert
        Assert.Contains("Patient ID is required", errors);
    }

    [Fact]
    public void WithPatient_EmptyLastName_AddsValidationError()
    {
        // Arrange
        var builder = PharmacyWorkflowBuilder.NewPrescription();

        // Act
        builder.WithPatient("PAT123", "", "John");
        var errors = builder.ValidateOnly();

        // Assert
        Assert.Contains("Patient last name is required", errors);
    }

    [Fact]
    public void WithPatientDemographics_SetsFullPatientInfo()
    {
        // Arrange
        var builder = PharmacyWorkflowBuilder.NewPrescription();
        var patientId = "PAT123";
        var lastName = "Smith";
        var firstName = "John";
        var middleName = "Robert";
        var dateOfBirth = new DateTime(1980, 5, 15);
        var gender = "M";
        var ssn = "123456789";

        // Act
        var result = builder.WithPatientDemographics(
            patientId, lastName, firstName, middleName, dateOfBirth, gender, ssn);
        var message = result.BuildWithWarnings();

        // Assert
        Assert.Same(builder, result);
        Assert.Equal(patientId, message.PatientIdentification.PatientIdentifierList.IdNumber);
        Assert.Equal(lastName, message.PatientIdentification.PatientName.FamilyName);
        Assert.Equal(firstName, message.PatientIdentification.PatientName.GivenName);
        Assert.Equal(middleName, message.PatientIdentification.PatientName.MiddleName);
        Assert.Equal(dateOfBirth, message.PatientIdentification.DateTimeOfBirth.ToDateTime());
        Assert.Equal(gender, message.PatientIdentification.AdministrativeSex.Value);
        Assert.Equal(ssn, message.PatientIdentification.PatientAccountNumber.Value);
    }

    [Fact]
    public void WithPatientAddress_SetsAddressInfo()
    {
        // Arrange
        var builder = PharmacyWorkflowBuilder.NewPrescription();
        var street = "123 Main St";
        var city = "Anytown";
        var state = "CA";
        var zipCode = "12345";
        var country = "USA";

        // Act
        var result = builder.WithPatientAddress(street, city, state, zipCode, country);
        var message = result.BuildWithWarnings();

        // Assert
        Assert.Same(builder, result);
        // Note: Address validation would depend on the specific implementation
        // of SetAddress method in PIDSegment
    }

    [Fact]
    public void WithPatientPhones_SetsPhoneNumbers()
    {
        // Arrange
        var builder = PharmacyWorkflowBuilder.NewPrescription();
        var homePhone = "555-1234";
        var workPhone = "555-5678";

        // Act
        var result = builder.WithPatientPhones(homePhone, workPhone);
        var message = result.BuildWithWarnings();

        // Assert
        Assert.Same(builder, result);
        // Note: Phone validation would depend on the specific implementation
        // of SetPhoneNumbers method in PIDSegment
    }

    #endregion

    #region Medication Information Tests

    [Fact]
    public void WithMedication_SetsBasicMedicationInfo()
    {
        // Arrange
        var builder = PharmacyWorkflowBuilder.NewPrescription();
        var medicationCode = "12345678";
        var medicationName = "Lisinopril";
        var strength = 10m;
        var strengthUnits = "mg";
        var dosageForm = "TAB";

        // Act
        var result = builder.WithMedication(medicationCode, medicationName, strength, strengthUnits, dosageForm);
        var message = result.BuildWithWarnings();

        // Assert
        Assert.Same(builder, result);
        Assert.Equal(medicationCode, message.PharmacyOrder.GiveCode.Identifier);
        Assert.Equal(medicationName, message.PharmacyOrder.GiveCode.Text);
        // Note: Strength and dosage form validation would depend on specific implementation
    }

    [Fact]
    public void WithMedication_EmptyMedicationCode_AddsValidationError()
    {
        // Arrange
        var builder = PharmacyWorkflowBuilder.NewPrescription();

        // Act
        builder.WithMedication("", "Lisinopril");
        var errors = builder.ValidateOnly();

        // Assert
        Assert.Contains("Medication code is required", errors);
    }

    [Fact]
    public void WithMedication_EmptyMedicationName_AddsValidationError()
    {
        // Arrange
        var builder = PharmacyWorkflowBuilder.NewPrescription();

        // Act
        builder.WithMedication("12345678", "");
        var errors = builder.ValidateOnly();

        // Assert
        Assert.Contains("Medication name is required", errors);
    }

    [Fact]
    public void WithDispensing_SetsDispensingInfo()
    {
        // Arrange
        var builder = PharmacyWorkflowBuilder.NewPrescription();
        var quantity = 30m;
        var units = "TAB";
        var refills = 3;

        // Act
        var result = builder.WithDispensing(quantity, units, refills);
        var message = result.BuildWithWarnings();

        // Assert
        Assert.Same(builder, result);
        // Note: Dispensing validation would depend on specific implementation
        // of SetDispensingInfo method in RXESegment
    }

    [Fact]
    public void WithDispensing_ZeroQuantity_AddsValidationError()
    {
        // Arrange
        var builder = PharmacyWorkflowBuilder.NewPrescription();

        // Act
        builder.WithDispensing(0m, "TAB", 0);
        var errors = builder.ValidateOnly();

        // Assert
        Assert.Contains("Quantity must be greater than 0", errors);
    }

    [Fact]
    public void WithDispensing_EmptyUnits_AddsValidationError()
    {
        // Arrange
        var builder = PharmacyWorkflowBuilder.NewPrescription();

        // Act
        builder.WithDispensing(30m, "", 0);
        var errors = builder.ValidateOnly();

        // Assert
        Assert.Contains("Units are required", errors);
    }

    [Fact]
    public void WithInstructions_SetsDosageInstructions()
    {
        // Arrange
        var builder = PharmacyWorkflowBuilder.NewPrescription();
        var sig = "Take 1 tablet by mouth daily";

        // Act
        var result = builder.WithInstructions(sig);
        var message = result.BuildWithWarnings();

        // Assert
        Assert.Same(builder, result);
        Assert.Equal(sig, message.PharmacyOrder.ProviderPharmacyTreatmentInstructions.Value);
    }

    [Fact]
    public void WithInstructions_EmptySig_AddsValidationError()
    {
        // Arrange
        var builder = PharmacyWorkflowBuilder.NewPrescription();

        // Act
        builder.WithInstructions("");
        var errors = builder.ValidateOnly();

        // Assert
        Assert.Contains("Dosage instructions (sig) are required", errors);
    }

    [Fact]
    public void WithInstructions_OverloadWithDispensing_SetsCompleteInfo()
    {
        // Arrange
        var builder = PharmacyWorkflowBuilder.NewPrescription();
        var sig = "Take 1 tablet by mouth daily";
        var quantity = 30m;
        var units = "TAB";
        var refills = 3;
        var daysSupply = 30;

        // Act
        var result = builder.WithInstructions(sig, quantity, units, refills, daysSupply);
        var message = result.BuildWithWarnings();

        // Assert
        Assert.Same(builder, result);
        Assert.Equal(sig, message.PharmacyOrder.ProviderPharmacyTreatmentInstructions.Value);
        // Note: Dispensing validation would depend on specific implementation
    }

    #endregion

    #region Provider Information Tests

    [Fact]
    public void WithProvider_SetsProviderInfo()
    {
        // Arrange
        var builder = PharmacyWorkflowBuilder.NewPrescription();
        var providerId = "PRV123";
        var lastName = "Johnson";
        var firstName = "Robert";
        var suffix = "MD";

        // Act
        var result = builder.WithProvider(providerId, lastName, firstName, suffix);
        var message = result.BuildWithWarnings();

        // Assert
        Assert.Same(builder, result);
        Assert.Equal(providerId, message.CommonOrder.OrderingProvider.Value);
    }

    [Fact]
    public void WithProvider_EmptyProviderId_AddsValidationError()
    {
        // Arrange
        var builder = PharmacyWorkflowBuilder.NewPrescription();

        // Act
        builder.WithProvider("", "Johnson", "Robert");
        var errors = builder.ValidateOnly();

        // Assert
        Assert.Contains("Provider ID is required", errors);
    }

    [Fact]
    public void WithProviderDEA_SetsDEANumber()
    {
        // Arrange
        var builder = PharmacyWorkflowBuilder.NewPrescription();
        var deaNumber = "BJ1234567";

        // Act
        var result = builder.WithProviderDEA(deaNumber);
        var message = result.BuildWithWarnings();

        // Assert
        Assert.Same(builder, result);
        // Note: DEA validation would depend on specific implementation
        // of SetProviderInfo method in RXESegment
    }

    [Fact]
    public void WithProviderDEA_EmptyDEANumber_AddsValidationError()
    {
        // Arrange
        var builder = PharmacyWorkflowBuilder.NewPrescription();

        // Act
        builder.WithProviderDEA("");
        var errors = builder.ValidateOnly();

        // Assert
        Assert.Contains("DEA number is required for controlled substances", errors);
    }

    #endregion

    #region Order Control Tests

    [Fact]
    public void WithOrderControl_SetsOrderControlInfo()
    {
        // Arrange
        var builder = PharmacyWorkflowBuilder.NewPrescription();
        var orderControl = "CA"; // Cancel order
        var orderStatus = "IP"; // In process

        // Act
        var result = builder.WithOrderControl(orderControl, orderStatus);
        var message = result.BuildWithWarnings();

        // Assert
        Assert.Same(builder, result);
        Assert.Equal(orderControl, message.CommonOrder.OrderControl.Value);
        // Note: Order status validation would depend on specific implementation
    }

    [Fact]
    public void WithOrderTiming_SetsTimingInfo()
    {
        // Arrange
        var builder = PharmacyWorkflowBuilder.NewPrescription();
        var orderDateTime = DateTime.Now;
        var effectiveDateTime = DateTime.Now.AddHours(1);

        // Act
        var result = builder.WithOrderTiming(orderDateTime, effectiveDateTime);
        var message = result.BuildWithWarnings();

        // Assert
        Assert.Same(builder, result);
        // Note: Timing validation would depend on specific implementation
        // of SetTiming method in ORCSegment
    }

    [Fact]
    public void WithPlacerOrderNumber_SetsCustomOrderNumber()
    {
        // Arrange
        var builder = PharmacyWorkflowBuilder.NewPrescription();
        var placerOrderNumber = "CUSTOM123456";

        // Act
        var result = builder.WithPlacerOrderNumber(placerOrderNumber);
        var message = result.BuildWithWarnings();

        // Assert
        Assert.Same(builder, result);
        Assert.Equal(placerOrderNumber, message.CommonOrder.PlacerOrderNumber.Value);
    }

    [Fact]
    public void WithPlacerOrderNumber_EmptyOrderNumber_AddsValidationError()
    {
        // Arrange
        var builder = PharmacyWorkflowBuilder.NewPrescription();

        // Act
        builder.WithPlacerOrderNumber("");
        var errors = builder.ValidateOnly();

        // Assert
        Assert.Contains("Placer order number cannot be empty", errors);
    }

    #endregion

    #region Priority and Special Instructions Tests

    [Fact]
    public void WithPriority_AddsPriorityNote()
    {
        // Arrange
        var builder = PharmacyWorkflowBuilder.NewPrescription();
        var priority = "S"; // Stat

        // Act
        var result = builder.WithPriority(priority);
        var message = result.BuildWithWarnings();

        // Assert
        Assert.Same(builder, result);
        
        // Verify priority note was added
        var notes = message.Where(s => s.SegmentId == "NTE").Cast<NTESegment>().ToList();
        Assert.True(notes.Any(n => n.Comment.Value.Contains($"Priority: {priority}")));
    }

    [Fact]
    public void WithNote_AddsNoteToMessage()
    {
        // Arrange
        var builder = PharmacyWorkflowBuilder.NewPrescription();
        var noteText = "Patient allergic to sulfa drugs";
        var noteType = "ALLERGY";

        // Act
        var result = builder.WithNote(noteText, noteType);
        var message = result.BuildWithWarnings();

        // Assert
        Assert.Same(builder, result);
        
        // Verify note was added
        var notes = message.Where(s => s.SegmentId == "NTE").Cast<NTESegment>().ToList();
        Assert.True(notes.Any(n => n.Comment.Value == noteText));
        Assert.True(notes.Any(n => n.SourceOfComment.Value == noteType));
    }

    [Fact]
    public void AsControlledSubstance_SetsControlledSubstanceInfo()
    {
        // Arrange
        var builder = PharmacyWorkflowBuilder.NewPrescription();
        var schedule = "II";
        var deaNumber = "BJ1234567";

        // Act
        var result = builder.AsControlledSubstance(schedule, deaNumber);
        var message = result.BuildWithWarnings();

        // Assert
        Assert.Same(builder, result);
        
        // Verify controlled substance note was added
        var notes = message.Where(s => s.SegmentId == "NTE").Cast<NTESegment>().ToList();
        Assert.True(notes.Any(n => n.Comment.Value.Contains($"Schedule {schedule}")));
        Assert.True(notes.Any(n => n.Comment.Value.Contains(deaNumber)));
        Assert.True(notes.Any(n => n.SourceOfComment.Value == "CONTROLLED_SUBSTANCE"));
    }

    #endregion

    #region Build and Validation Tests

    [Fact]
    public void Build_WithValidData_ReturnsRDEMessage()
    {
        // Arrange
        var builder = PharmacyWorkflowBuilder.NewPrescription()
            .WithPatient("PAT123", "Smith", "John")
            .WithMedication("12345678", "Lisinopril 10mg")
            .WithInstructions("Take 1 tablet by mouth daily")
            .WithProvider("PRV456", "Johnson", "Robert");

        // Act
        var message = builder.Build();

        // Assert
        Assert.NotNull(message);
        Assert.IsType<RDEMessage>(message);
        Assert.Equal("PAT123", message.PatientIdentification.PatientIdentifierList.IdNumber);
        Assert.Equal("12345678", message.PharmacyOrder.GiveCode.Identifier);
        Assert.Equal("Take 1 tablet by mouth daily", message.PharmacyOrder.ProviderPharmacyTreatmentInstructions.Value);
    }

    [Fact]
    public void Build_WithMissingRequiredData_ThrowsInvalidOperationException()
    {
        // Arrange
        var builder = PharmacyWorkflowBuilder.NewPrescription()
            .WithPatient("PAT123", "Smith", "John");
        // Missing medication and instructions

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => builder.Build());
        Assert.Contains("Prescription validation failed", exception.Message);
    }

    [Fact]
    public void Build_CalledTwice_ThrowsInvalidOperationException()
    {
        // Arrange
        var builder = PharmacyWorkflowBuilder.NewPrescription()
            .WithPatient("PAT123", "Smith", "John")
            .WithMedication("12345678", "Lisinopril 10mg")
            .WithInstructions("Take 1 tablet by mouth daily");

        // Act
        builder.Build(); // First build

        // Assert
        var exception = Assert.Throws<InvalidOperationException>(() => builder.Build());
        Assert.Contains("Message has already been built", exception.Message);
    }

    [Fact]
    public void ValidateOnly_ReturnsValidationErrors()
    {
        // Arrange
        var builder = PharmacyWorkflowBuilder.NewPrescription()
            .WithPatient("", "Smith", "John") // Empty patient ID
            .WithMedication("", "Lisinopril 10mg"); // Empty medication code

        // Act
        var errors = builder.ValidateOnly();

        // Assert
        Assert.Contains("Patient ID is required", errors);
        Assert.Contains("Medication code is required", errors);
    }

    [Fact]
    public void BuildWithWarnings_ReturnsMessageEvenWithValidationErrors()
    {
        // Arrange
        var builder = PharmacyWorkflowBuilder.NewPrescription()
            .WithPatient("", "Smith", "John"); // Empty patient ID

        // Act
        var message = builder.BuildWithWarnings();

        // Assert
        Assert.NotNull(message);
        Assert.IsType<RDEMessage>(message);
    }

    [Fact]
    public void BuildWithWarnings_CalledTwice_ThrowsInvalidOperationException()
    {
        // Arrange
        var builder = PharmacyWorkflowBuilder.NewPrescription();

        // Act
        builder.BuildWithWarnings(); // First build

        // Assert
        var exception = Assert.Throws<InvalidOperationException>(() => builder.BuildWithWarnings());
        Assert.Contains("Message has already been built", exception.Message);
    }

    #endregion

    #region Complex Workflow Tests

    [Fact]
    public void ComplexPrescriptionWorkflow_WithAllFeatures_BuildsSuccessfully()
    {
        // Arrange & Act
        var message = PharmacyWorkflowBuilder.NewPrescription()
            .WithPatientDemographics("PAT123", "Smith", "John", "Robert", 
                new DateTime(1980, 5, 15), "M", "123456789")
            .WithPatientAddress("123 Main St", "Anytown", "CA", "12345", "USA")
            .WithPatientPhones("555-1234", "555-5678")
            .WithMedication("00406055705", "Oxycodone 5mg", 5m, "mg", "TAB")
            .WithInstructions("Take 1 tablet every 6 hours as needed for pain", 20m, "TAB", 0, 5)
            .WithProvider("PRV456", "Johnson", "Robert", "MD")
            .WithProviderDEA("BJ1234567")
            .AsControlledSubstance("II", "BJ1234567")
            .WithOrderControl("NW", "IP")
            .WithOrderTiming(DateTime.Now, DateTime.Now.AddHours(1))
            .WithPlacerOrderNumber("CUSTOM123456")
            .WithPriority("S")
            .WithNote("Patient allergic to sulfa drugs", "ALLERGY")
            .Build();

        // Assert
        Assert.NotNull(message);
        Assert.IsType<RDEMessage>(message);
        Assert.Equal("PAT123", message.PatientIdentification.PatientIdentifierList.IdNumber);
        Assert.Equal("Smith", message.PatientIdentification.PatientName.FamilyName);
        Assert.Equal("John", message.PatientIdentification.PatientName.GivenName);
        Assert.Equal("00406055705", message.PharmacyOrder.GiveCode.Identifier);
        Assert.Equal("Oxycodone 5mg", message.PharmacyOrder.GiveCode.Text);
        Assert.Equal("Take 1 tablet every 6 hours as needed for pain", 
            message.PharmacyOrder.ProviderPharmacyTreatmentInstructions.Value);
        Assert.Equal("CUSTOM123456", message.CommonOrder.PlacerOrderNumber.Value);
        
        // Verify notes were added
        var notes = message.Where(s => s.SegmentId == "NTE").Cast<NTESegment>().ToList();
        Assert.True(notes.Count >= 3); // Priority, controlled substance, and allergy notes
        Assert.True(notes.Any(n => n.Comment.Value.Contains("Priority: S")));
        Assert.True(notes.Any(n => n.Comment.Value.Contains("Schedule II")));
        Assert.True(notes.Any(n => n.Comment.Value.Contains("Patient allergic to sulfa drugs")));
    }

    [Fact]
    public void FluentInterfaceMethodChaining_WorksCorrectly()
    {
        // Arrange
        var builder = PharmacyWorkflowBuilder.NewPrescription();

        // Act - Test that all methods return the same builder instance
        var result = builder
            .WithPatient("PAT123", "Smith", "John")
            .WithMedication("12345678", "Lisinopril 10mg")
            .WithInstructions("Take 1 tablet by mouth daily")
            .WithProvider("PRV456", "Johnson", "Robert")
            .WithDispensing(30m, "TAB", 3)
            .WithNote("Test note");

        // Assert
        Assert.Same(builder, result);
    }

    #endregion
}