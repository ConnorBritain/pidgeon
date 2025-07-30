// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using FluentAssertions;
using Segmint.Core.Standards.HL7.v23.Messages;
using Segmint.Core.Standards.HL7.v23.Segments;
using Xunit;

namespace Segmint.Tests.HL7.Messages;

public class ADTMessageTests
{
    [Fact]
    public void ADTMessage_DefaultConstructor_CreatesMessageWithCorrectType()
    {
        // Arrange & Act
        var message = new ADTMessage();

        // Assert
        message.MessageType.Should().Be("ADT");
        message.TriggerEvent.Should().Be("A01");
        message.MessageStructure.Should().Be("ADT_A01");
        message.SegmentCount.Should().Be(4); // MSH, EVN, PID, PV1
    }

    [Fact]
    public void ADTMessage_WithCustomTriggerEvent_CreatesCorrectMessage()
    {
        // Arrange & Act
        var message = new ADTMessage("A03");

        // Assert
        message.TriggerEvent.Should().Be("A03");
        message.MessageStructure.Should().Be("ADT_A03");
    }

    [Fact]
    public void ADTMessage_CreateAdmitPatient_CreatesA01Message()
    {
        // Arrange & Act
        var message = ADTMessage.CreateAdmitPatient();

        // Assert
        message.TriggerEvent.Should().Be("A01");
        message.GetEventTypeDescription().Should().Be("Admit Patient");
    }

    [Fact]
    public void ADTMessage_CreateDischargePatient_CreatesA03Message()
    {
        // Arrange & Act
        var message = ADTMessage.CreateDischargePatient();

        // Assert
        message.TriggerEvent.Should().Be("A03");
        message.GetEventTypeDescription().Should().Be("Discharge Patient");
    }

    [Fact]
    public void ADTMessage_CreateTransferPatient_CreatesA02Message()
    {
        // Arrange & Act
        var message = ADTMessage.CreateTransferPatient();

        // Assert
        message.TriggerEvent.Should().Be("A02");
        message.GetEventTypeDescription().Should().Be("Transfer Patient");
    }

    [Fact]
    public void ADTMessage_CreateRegisterPatient_CreatesA04Message()
    {
        // Arrange & Act
        var message = ADTMessage.CreateRegisterPatient();

        // Assert
        message.TriggerEvent.Should().Be("A04");
        message.GetEventTypeDescription().Should().Be("Register Patient");
    }

    [Fact]
    public void ADTMessage_SetPatientDemographics_UpdatesPatientSegment()
    {
        // Arrange
        var message = new ADTMessage();

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
    public void ADTMessage_SetPatientAddress_UpdatesPatientSegment()
    {
        // Arrange
        var message = new ADTMessage();

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
    public void ADTMessage_SetPatientVisit_UpdatesVisitSegment()
    {
        // Arrange
        var message = new ADTMessage();

        // Act
        message.SetPatientVisit("I", "2W^201^A", "Smith John", "E", "V12345");

        // Assert
        message.PatientVisit!.PatientClass.RawValue.Should().Be("I");
        message.PatientVisit.AssignedPatientLocation.RawValue.Should().Be("2W^201^A");
        message.PatientVisit.AdmissionType.RawValue.Should().Be("E");
        message.PatientVisit.VisitNumber.IdNumber.Should().Be("V12345");
    }

    [Fact]
    public void ADTMessage_SetAdmissionDateTime_UpdatesVisitSegment()
    {
        // Arrange
        var message = new ADTMessage();
        var admissionTime = new DateTime(2023, 12, 25, 10, 30, 0);

        // Act
        message.SetAdmissionDateTime(admissionTime);

        // Assert
        message.PatientVisit!.AdmitDateTime.Value.Should().Be(admissionTime);
    }

    [Fact]
    public void ADTMessage_SetDischargeDateTime_UpdatesVisitSegment()
    {
        // Arrange
        var message = new ADTMessage();
        var dischargeTime = new DateTime(2023, 12, 28, 14, 45, 0);

        // Act
        message.SetDischargeDateTime(dischargeTime);

        // Assert
        message.PatientVisit!.DischargeDateTime.Value.Should().Be(dischargeTime);
    }

    [Fact]
    public void ADTMessage_SetSendingApplication_UpdatesMessageHeader()
    {
        // Arrange
        var message = new ADTMessage();

        // Act
        message.SetSendingApplication("Hospital System", "Main Hospital");

        // Assert
        message.MessageHeader!.SendingApplication.RawValue.Should().Be("Hospital System");
        message.MessageHeader.SendingFacility.RawValue.Should().Be("Main Hospital");
    }

    [Fact]
    public void ADTMessage_SetReceivingApplication_UpdatesMessageHeader()
    {
        // Arrange
        var message = new ADTMessage();

        // Act
        message.SetReceivingApplication("ADT System", "Central Processing");

        // Assert
        message.MessageHeader!.ReceivingApplication.RawValue.Should().Be("ADT System");
        message.MessageHeader.ReceivingFacility.RawValue.Should().Be("Central Processing");
    }

    [Fact]
    public void ADTMessage_AddNote_AddsNoteSegment()
    {
        // Arrange
        var message = new ADTMessage();

        // Act
        message.AddNote("Patient admitted through emergency department", "L");

        // Assert
        message.SegmentCount.Should().Be(5); // Added NTE segment
        var noteSegment = message.GetSegment<NTESegment>();
        noteSegment.Should().NotBeNull();
        noteSegment!.Comment.RawValue.Should().Be("Patient admitted through emergency department");
        noteSegment.SourceOfComment.RawValue.Should().Be("L");
    }

    [Fact]
    public void ADTMessage_ToHL7String_GeneratesValidHL7()
    {
        // Arrange
        var message = ADTMessage.CreateAdmitPatient();
        message.SetPatientDemographics("12345", "Doe", "John", "M");
        message.SetPatientVisit("I", "2W^201^A", "Smith John", "E");

        // Act
        var hl7String = message.ToHL7String();

        // Assert
        hl7String.Should().StartWith("MSH|^~\\&|");
        hl7String.Should().Contain("ADT^A01");
        hl7String.Should().Contain("PID|");
        hl7String.Should().Contain("EVN|");
        hl7String.Should().Contain("PV1|");
        hl7String.Should().Contain("Doe^John^M");
        hl7String.Should().Contain("2W^201^A");
        hl7String.Should().Contain("A01"); // Event type
    }

    [Fact]
    public void ADTMessage_ToDisplayString_FormatsReadably()
    {
        // Arrange
        var message = ADTMessage.CreateAdmitPatient();
        message.SetPatientDemographics("12345", "Doe", "John", "M");

        // Act
        var displayString = message.ToDisplayString();

        // Assert
        displayString.Should().Be("ADT Admit Patient - John M Doe (ID: 12345)");
    }

    [Fact]
    public void ADTMessage_GetEventTypeDescription_ReturnsCorrectDescriptions()
    {
        // Arrange & Act & Assert
        ADTMessage.CreateAdmitPatient().GetEventTypeDescription().Should().Be("Admit Patient");
        ADTMessage.CreateTransferPatient().GetEventTypeDescription().Should().Be("Transfer Patient");
        ADTMessage.CreateDischargePatient().GetEventTypeDescription().Should().Be("Discharge Patient");
        ADTMessage.CreateRegisterPatient().GetEventTypeDescription().Should().Be("Register Patient");
        ADTMessage.CreatePreAdmitPatient().GetEventTypeDescription().Should().Be("Pre-Admit Patient");
        ADTMessage.CreateUpdatePatient().GetEventTypeDescription().Should().Be("Update Patient Information");
        ADTMessage.CreateCancelAdmit().GetEventTypeDescription().Should().Be("Cancel Admit");
        ADTMessage.CreateCancelDischarge().GetEventTypeDescription().Should().Be("Cancel Discharge");
    }

    [Fact]
    public void ADTMessage_Clone_CreatesDeepCopy()
    {
        // Arrange
        var original = ADTMessage.CreateAdmitPatient();
        original.SetPatientDemographics("12345", "Doe", "John", "M");
        original.SetPatientVisit("I", "2W^201^A");

        // Act
        var cloned = (ADTMessage)original.Clone();

        // Assert
        cloned.Should().NotBeSameAs(original);
        cloned.TriggerEvent.Should().Be(original.TriggerEvent);
        cloned.PatientIdentification!.PatientIdentifierList.IdNumber.Should().Be("12345");
        cloned.PatientVisit!.PatientClass.RawValue.Should().Be("I");
    }

    [Fact]
    public void ADTMessage_ComplexAdmissionScenario_HandlesCorrectly()
    {
        // Arrange
        var message = ADTMessage.CreateAdmitPatient();
        var admissionTime = new DateTime(2023, 12, 25, 10, 30, 0);

        // Act - Set up a complex admission
        message.SetPatientDemographics("MRN123456", "Smith", "Jane", "A", 
            new DateTime(1975, 8, 22), "F", "123-45-6789");
        message.SetPatientAddress("123 Healthcare Ave", "Medical City", "HC", "12345", "USA");
        message.SetPatientVisit("I", "2W^201^A", "Johnson Michael", "E", "V78901");
        message.SetAdmissionDateTime(admissionTime);
        message.SetSendingApplication("Hospital System v3.2", "Main Hospital");
        message.SetReceivingApplication("ADT System", "Central ADT");
        message.AddNote("Patient admitted through emergency department with chest pain", "L");
        message.AddNote("Patient has allergy to penicillin", "P");

        // Assert
        var hl7String = message.ToHL7String();
        hl7String.Should().Contain("MRN123456");
        hl7String.Should().Contain("Smith^Jane^A");
        hl7String.Should().Contain("2W^201^A");
        hl7String.Should().Contain("V78901");
        hl7String.Should().Contain("20231225103000");
        hl7String.Should().Contain("chest pain");
        hl7String.Should().Contain("allergy to penicillin");
        
        message.SegmentCount.Should().Be(6); // MSH, EVN, PID, PV1, NTE, NTE
    }

    [Fact]
    public void ADTMessage_DischargeScenario_HandlesCorrectly()
    {
        // Arrange
        var message = ADTMessage.CreateDischargePatient();
        var dischargeTime = new DateTime(2023, 12, 28, 14, 45, 0);

        // Act
        message.SetPatientDemographics("MRN123456", "Smith", "Jane", "A");
        message.SetPatientVisit("I", "2W^201^A", "Johnson Michael", "E", "V78901");
        message.SetDischargeDateTime(dischargeTime);
        message.AddNote("Patient discharged home in stable condition", "L");

        // Assert
        message.TriggerEvent.Should().Be("A03");
        message.GetEventTypeDescription().Should().Be("Discharge Patient");
        message.PatientVisit!.DischargeDateTime.Value.Should().Be(dischargeTime);
        
        var hl7String = message.ToHL7String();
        hl7String.Should().Contain("ADT^A03");
        hl7String.Should().Contain("20231228144500");
        hl7String.Should().Contain("stable condition");
    }

    [Fact]
    public void ADTMessage_TransferScenario_HandlesCorrectly()
    {
        // Arrange
        var message = ADTMessage.CreateTransferPatient();

        // Act
        message.SetPatientDemographics("MRN123456", "Smith", "Jane", "A");
        message.SetPatientVisit("I", "3N^301^B", "Johnson Michael", "E", "V78901");
        message.AddNote("Patient transferred to ICU for closer monitoring", "L");

        // Assert
        message.TriggerEvent.Should().Be("A02");
        message.GetEventTypeDescription().Should().Be("Transfer Patient");
        message.PatientVisit!.AssignedPatientLocation.RawValue.Should().Be("3N^301^B");
        
        var hl7String = message.ToHL7String();
        hl7String.Should().Contain("ADT^A02");
        hl7String.Should().Contain("3N^301^B");
        hl7String.Should().Contain("closer monitoring");
    }

    [Fact]
    public void ADTMessage_HasRequiredSegments_AfterConstruction()
    {
        // Arrange & Act
        var message = new ADTMessage();

        // Assert
        message.MessageHeader.Should().NotBeNull();
        message.EventType.Should().NotBeNull();
        message.PatientIdentification.Should().NotBeNull();
        message.PatientVisit.Should().NotBeNull();
        
        // Check that segments are in the correct order
        message.GetSegment<MSHSegment>().Should().NotBeNull();
        message.GetSegment<EVNSegment>().Should().NotBeNull();
        message.GetSegment<PIDSegment>().Should().NotBeNull();
        message.GetSegment<PV1Segment>().Should().NotBeNull();
    }

    [Theory]
    [InlineData("A01", "Admit Patient")]
    [InlineData("A02", "Transfer Patient")]
    [InlineData("A03", "Discharge Patient")]
    [InlineData("A04", "Register Patient")]
    [InlineData("A05", "Pre-Admit Patient")]
    [InlineData("A08", "Update Patient Information")]
    [InlineData("A11", "Cancel Admit")]
    [InlineData("A13", "Cancel Discharge")]
    [InlineData("A99", "A99")] // Unknown event type
    public void ADTMessage_GetEventTypeDescription_HandlesAllEventTypes(string eventType, string expectedDescription)
    {
        // Arrange
        var message = new ADTMessage(eventType);

        // Act
        var description = message.GetEventTypeDescription();

        // Assert
        description.Should().Be(expectedDescription);
    }
}