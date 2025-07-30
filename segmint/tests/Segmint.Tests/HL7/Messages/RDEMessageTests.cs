// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using FluentAssertions;
using Segmint.Core.Standards.HL7.v23.Messages;
using Segmint.Core.Standards.HL7.v23.Segments;
using Xunit;

namespace Segmint.Tests.HL7.Messages;

public class RDEMessageTests
{
    [Fact]
    public void RDEMessage_DefaultConstructor_CreatesMessageWithCorrectType()
    {
        // Arrange & Act
        var message = new RDEMessage();

        // Assert
        message.MessageType.Should().Be("RDE");
        message.TriggerEvent.Should().Be("O01");
        message.MessageStructure.Should().Be("RDE_O01");
        message.SegmentCount.Should().Be(5); // MSH, EVN, PID, ORC, RXE
    }

    [Fact]
    public void RDEMessage_HasRequiredSegments_AfterConstruction()
    {
        // Arrange & Act
        var message = new RDEMessage();

        // Assert
        message.MessageHeader.Should().NotBeNull();
        message.EventType.Should().NotBeNull();
        message.PatientIdentification.Should().NotBeNull();
        message.CommonOrder.Should().NotBeNull();
        message.PharmacyOrder.Should().NotBeNull();
    }

    [Fact]
    public void RDEMessage_SetPatientDemographics_UpdatesPatientSegment()
    {
        // Arrange
        var message = new RDEMessage();

        // Act
        message.SetPatientDemographics("12345", "Doe", "John", "M", 
            new DateTime(1980, 5, 15), "M", "123-45-6789");

        // Assert
        message.PatientIdentification!.PatientIdentifierList.IdNumber.Should().Be("12345");
        message.PatientIdentification.PatientName.LastName.Should().Be("Doe");
        message.PatientIdentification.PatientName.FirstName.Should().Be("John");
        message.PatientIdentification.PatientName.MiddleName.Should().Be("M");
        message.PatientIdentification.DateTimeOfBirth.Value.Should().Be(new DateTime(1980, 5, 15));
        message.PatientIdentification.AdministrativeSex.RawValue.Should().Be("M");
        message.PatientIdentification.SocialSecurityNumber.RawValue.Should().Be("123-45-6789");
    }

    [Fact]
    public void RDEMessage_SetPatientAddress_UpdatesPatientSegment()
    {
        // Arrange
        var message = new RDEMessage();

        // Act
        message.SetPatientAddress("123 Main St", "Anytown", "ST", "12345", "USA");

        // Assert
        message.PatientIdentification!.PatientAddress.StreetAddress.Should().Be("123 Main St");
        message.PatientIdentification.PatientAddress.City.Should().Be("Anytown");
        message.PatientIdentification.PatientAddress.StateOrProvince.Should().Be("ST");
        message.PatientIdentification.PatientAddress.ZipOrPostalCode.Should().Be("12345");
        message.PatientIdentification.PatientAddress.Country.Should().Be("USA");
    }

    [Fact]
    public void RDEMessage_SetMedication_UpdatesPharmacySegment()
    {
        // Arrange
        var message = new RDEMessage();

        // Act
        message.SetMedication("123456", "Amoxicillin 500mg", "500", "mg", "PO");

        // Assert
        message.PharmacyOrder!.GiveCode.Identifier.Should().Be("123456");
        message.PharmacyOrder.GiveCode.Text.Should().Be("Amoxicillin 500mg");
        message.PharmacyOrder.GiveAmountMinimum.Quantity.Should().Be("500");
        message.PharmacyOrder.GiveAmountMinimum.Units.Should().Be("mg");
        message.PharmacyOrder.GiveUnits.Identifier.Should().Be("PO");
    }

    [Fact]
    public void RDEMessage_SetDosageInstructions_UpdatesPharmacySegment()
    {
        // Arrange
        var message = new RDEMessage();

        // Act
        message.SetDosageInstructions("Take one tablet twice daily");

        // Assert
        message.PharmacyOrder!.ProviderPharmacyTreatmentInstructions.RawValue
            .Should().Be("Take one tablet twice daily");
    }

    [Fact]
    public void RDEMessage_SetQuantityTiming_UpdatesPharmacySegment()
    {
        // Arrange
        var message = new RDEMessage();

        // Act
        message.SetQuantityTiming("1", "D2", "BID");

        // Assert
        message.PharmacyOrder!.QuantityTiming.Quantity.Should().Be("1");
        message.PharmacyOrder.QuantityTiming.Interval.Should().Be("D2");
        message.PharmacyOrder.QuantityTiming.Text.Should().Be("BID");
    }

    [Fact]
    public void RDEMessage_SetOrderControl_UpdatesCommonOrderSegment()
    {
        // Arrange
        var message = new RDEMessage();

        // Act
        message.SetOrderControl("NW");

        // Assert
        message.CommonOrder!.OrderControl.RawValue.Should().Be("NW");
    }

    [Fact]
    public void RDEMessage_SetOrderingProvider_UpdatesCommonOrderSegment()
    {
        // Arrange
        var message = new RDEMessage();

        // Act
        message.SetOrderingProvider("Smith", "John", "MD");

        // Assert
        message.CommonOrder!.OrderingProvider.Value.Should().Be("Smith^John^MD");
    }

    [Fact]
    public void RDEMessage_SetSendingApplication_UpdatesMessageHeader()
    {
        // Arrange
        var message = new RDEMessage();

        // Act
        message.SetSendingApplication("EMR System", "Main Hospital");

        // Assert
        message.MessageHeader!.SendingApplication.RawValue.Should().Be("EMR System");
        message.MessageHeader.SendingFacility.RawValue.Should().Be("Main Hospital");
    }

    [Fact]
    public void RDEMessage_SetReceivingApplication_UpdatesMessageHeader()
    {
        // Arrange
        var message = new RDEMessage();

        // Act
        message.SetReceivingApplication("Pharmacy System", "Hospital Pharmacy");

        // Assert
        message.MessageHeader!.ReceivingApplication.RawValue.Should().Be("Pharmacy System");
        message.MessageHeader.ReceivingFacility.RawValue.Should().Be("Hospital Pharmacy");
    }

    [Fact]
    public void RDEMessage_AddNote_AddsNoteSegment()
    {
        // Arrange
        var message = new RDEMessage();

        // Act
        message.AddNote("Patient has allergies to penicillin", "L");

        // Assert
        message.SegmentCount.Should().Be(6); // Added NTE segment
        var noteSegment = message.GetSegment<NTESegment>();
        noteSegment.Should().NotBeNull();
        noteSegment!.Comment.RawValue.Should().Be("Patient has allergies to penicillin");
        noteSegment.SourceOfComment.RawValue.Should().Be("L");
    }

    [Fact]
    public void RDEMessage_AddRoute_AddsRouteSegment()
    {
        // Arrange
        var message = new RDEMessage();

        // Act
        message.AddRoute("PO", "Oral", "Mouth");

        // Assert
        message.SegmentCount.Should().Be(6); // Added RXR segment
        var routeSegment = message.GetSegment<RXRSegment>();
        routeSegment.Should().NotBeNull();
        routeSegment!.Route.Identifier.Should().Be("PO");
        routeSegment.Route.Text.Should().Be("Oral");
        routeSegment.AdministrationSite.RawValue.Should().Be("Mouth");
    }

    [Fact]
    public void RDEMessage_ToHL7String_GeneratesValidHL7()
    {
        // Arrange
        var message = new RDEMessage();
        message.SetPatientDemographics("12345", "Doe", "John", "M");
        message.SetMedication("123456", "Amoxicillin 500mg", "500", "mg", "PO");
        message.SetOrderControl("NW");

        // Act
        var hl7String = message.ToHL7String();

        // Assert
        hl7String.Should().StartWith("MSH|^~\\&|");
        hl7String.Should().Contain("RDE^O01");
        hl7String.Should().Contain("PID|");
        hl7String.Should().Contain("EVN|");
        hl7String.Should().Contain("ORC|");
        hl7String.Should().Contain("RXE|");
        hl7String.Should().Contain("Doe^John^M");
        hl7String.Should().Contain("Amoxicillin 500mg");
        hl7String.Should().Contain("NW");
    }

    [Fact]
    public void RDEMessage_ToNetworkString_GeneratesNetworkFormat()
    {
        // Arrange
        var message = new RDEMessage();
        message.SetPatientDemographics("12345", "Doe", "John");
        message.SetMedication("123456", "Amoxicillin 500mg");

        // Act
        var networkString = message.ToNetworkString();

        // Assert
        networkString.Should().StartWith("\x0B"); // VT (start of message)
        networkString.Should().EndWith("\x1C\r"); // FS + CR (end of message)
        networkString.Should().Contain("MSH|^~\\&|");
        networkString.Should().Contain("PID|");
        networkString.Should().Contain("RXE|");
    }

    [Fact]
    public void RDEMessage_ToDisplayString_FormatsReadably()
    {
        // Arrange
        var message = new RDEMessage();
        message.SetPatientDemographics("12345", "Doe", "John", "M");
        message.SetMedication("123456", "Amoxicillin 500mg");

        // Act
        var displayString = message.ToDisplayString();

        // Assert
        displayString.Should().Be("RDE Pharmacy Order - John M Doe (ID: 12345) - Amoxicillin 500mg");
    }

    [Fact]
    public void RDEMessage_Clone_CreatesDeepCopy()
    {
        // Arrange
        var original = new RDEMessage();
        original.SetPatientDemographics("12345", "Doe", "John", "M");
        original.SetMedication("123456", "Amoxicillin 500mg");
        original.SetOrderControl("NW");

        // Act
        var cloned = (RDEMessage)original.Clone();

        // Assert
        cloned.Should().NotBeSameAs(original);
        cloned.PatientIdentification!.PatientIdentifierList.IdNumber.Should().Be("12345");
        cloned.PharmacyOrder!.GiveCode.Text.Should().Be("Amoxicillin 500mg");
        cloned.CommonOrder!.OrderControl.RawValue.Should().Be("NW");
    }

    [Fact]
    public void RDEMessage_Validate_ReturnsValidationResults()
    {
        // Arrange
        var message = new RDEMessage();
        message.SetPatientDemographics("12345", "Doe", "John", "M");
        message.SetMedication("123456", "Amoxicillin 500mg");
        message.SetOrderControl("NW");

        // Act
        var validationResult = message.Validate();

        // Assert
        validationResult.IsValid.Should().BeTrue();
        validationResult.Errors.Should().BeEmpty();
    }

    [Fact]
    public void RDEMessage_Validate_WithMissingRequiredFields_ReturnsErrors()
    {
        // Arrange
        var message = new RDEMessage();
        // Don't set required fields

        // Act
        var validationResult = message.Validate();

        // Assert
        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public void RDEMessage_ComplexScenario_HandlesCorrectly()
    {
        // Arrange
        var message = new RDEMessage();

        // Act - Set up a complex pharmacy order
        message.SetPatientDemographics("MRN123456", "Smith", "Jane", "A", 
            new DateTime(1975, 8, 22), "F", "123-45-6789");
        message.SetPatientAddress("123 Healthcare Ave", "Medical City", "HC", "12345", "USA");
        message.SetMedication("NDC123456", "Amoxicillin 500mg Capsules", "500", "mg", "PO");
        message.SetDosageInstructions("Take one capsule by mouth twice daily with food");
        message.SetQuantityTiming("1", "D2", "BID");
        message.SetOrderControl("NW");
        message.SetOrderingProvider("Johnson", "Michael", "MD");
        message.SetSendingApplication("EMR System v2.1", "Main Hospital");
        message.SetReceivingApplication("Pharmacy System", "Hospital Pharmacy");
        message.AddNote("Patient has no known allergies", "L");
        message.AddRoute("PO", "Oral", "Mouth");

        // Assert
        var hl7String = message.ToHL7String();
        hl7String.Should().Contain("MRN123456");
        hl7String.Should().Contain("Smith^Jane^A");
        hl7String.Should().Contain("Amoxicillin 500mg");
        hl7String.Should().Contain("Take one capsule by mouth twice daily");
        hl7String.Should().Contain("Johnson^Michael^^MD");
        hl7String.Should().Contain("Patient has no known allergies");
        
        message.SegmentCount.Should().Be(7); // MSH, EVN, PID, ORC, RXE, NTE, RXR
    }
}