// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using FluentAssertions;
using Segmint.Core.Standards.HL7.v23.Messages;
using Segmint.Core.Standards.HL7.v23.Segments;
using Xunit;

namespace Segmint.Tests.HL7.Messages;

public class ACKMessageTests
{
    [Fact]
    public void ACKMessage_DefaultConstructor_CreatesMessageWithCorrectType()
    {
        // Arrange & Act
        var message = new ACKMessage();

        // Assert
        message.MessageType.Should().Be("ACK");
        message.TriggerEvent.Should().Be("R01");
        message.MessageStructure.Should().Be("ACK_R01");
        message.SegmentCount.Should().Be(2); // MSH, MSA
    }

    [Fact]
    public void ACKMessage_HasRequiredSegments_AfterConstruction()
    {
        // Arrange & Act
        var message = new ACKMessage();

        // Assert
        message.MessageHeader.Should().NotBeNull();
        message.MessageAcknowledgment.Should().NotBeNull();
    }

    [Fact]
    public void ACKMessage_CreateSuccessfulAck_CreatesAAMessage()
    {
        // Arrange
        var originalMessage = new RDEMessage();
        originalMessage.MessageHeader!.SetMessageControlId("12345");
        originalMessage.MessageHeader.SetSendingApplication("EMR");
        originalMessage.MessageHeader.SetReceivingApplication("Pharmacy");

        // Act
        var ack = ACKMessage.CreateSuccessfulAck(originalMessage, "Message processed successfully");

        // Assert
        ack.MessageAcknowledgment!.AcknowledgmentCode.RawValue.Should().Be("AA");
        ack.MessageAcknowledgment.MessageControlId.RawValue.Should().Be("12345");
        ack.MessageAcknowledgment.TextMessage.RawValue.Should().Be("Message processed successfully");
        ack.IsSuccessful().Should().BeTrue();
    }

    [Fact]
    public void ACKMessage_CreateErrorAck_CreatesAEMessage()
    {
        // Arrange
        var originalMessage = new RDEMessage();
        originalMessage.MessageHeader!.SetMessageControlId("12345");

        // Act
        var ack = ACKMessage.CreateErrorAck(originalMessage, "Invalid patient ID", "E001");

        // Assert
        ack.MessageAcknowledgment!.AcknowledgmentCode.RawValue.Should().Be("AE");
        ack.MessageAcknowledgment.MessageControlId.RawValue.Should().Be("12345");
        ack.MessageAcknowledgment.TextMessage.RawValue.Should().Be("Invalid patient ID");
        ack.MessageAcknowledgment.ErrorCondition.Identifier.Should().Be("E001");
        ack.IsSuccessful().Should().BeFalse();
    }

    [Fact]
    public void ACKMessage_CreateRejectionAck_CreatesARMessage()
    {
        // Arrange
        var originalMessage = new RDEMessage();
        originalMessage.MessageHeader!.SetMessageControlId("12345");

        // Act
        var ack = ACKMessage.CreateRejectionAck(originalMessage, "Message format invalid", "R001");

        // Assert
        ack.MessageAcknowledgment!.AcknowledgmentCode.RawValue.Should().Be("AR");
        ack.MessageAcknowledgment.MessageControlId.RawValue.Should().Be("12345");
        ack.MessageAcknowledgment.TextMessage.RawValue.Should().Be("Message format invalid");
        ack.MessageAcknowledgment.ErrorCondition.Identifier.Should().Be("R001");
        ack.IsSuccessful().Should().BeFalse();
    }

    [Fact]
    public void ACKMessage_CreateCommitAcceptAck_CreatesCAMessage()
    {
        // Arrange
        var originalMessage = new RDEMessage();
        originalMessage.MessageHeader!.SetMessageControlId("12345");

        // Act
        var ack = ACKMessage.CreateCommitAcceptAck(originalMessage, "Order committed successfully");

        // Assert
        ack.MessageAcknowledgment!.AcknowledgmentCode.RawValue.Should().Be("CA");
        ack.MessageAcknowledgment.MessageControlId.RawValue.Should().Be("12345");
        ack.MessageAcknowledgment.TextMessage.RawValue.Should().Be("Order committed successfully");
        ack.IsSuccessful().Should().BeTrue();
    }

    [Fact]
    public void ACKMessage_CreateCommitErrorAck_CreatesCEMessage()
    {
        // Arrange
        var originalMessage = new RDEMessage();
        originalMessage.MessageHeader!.SetMessageControlId("12345");

        // Act
        var ack = ACKMessage.CreateCommitErrorAck(originalMessage, "Commit failed", "CE001");

        // Assert
        ack.MessageAcknowledgment!.AcknowledgmentCode.RawValue.Should().Be("CE");
        ack.MessageAcknowledgment.MessageControlId.RawValue.Should().Be("12345");
        ack.MessageAcknowledgment.TextMessage.RawValue.Should().Be("Commit failed");
        ack.MessageAcknowledgment.ErrorCondition.Identifier.Should().Be("CE001");
        ack.IsSuccessful().Should().BeFalse();
    }

    [Fact]
    public void ACKMessage_CreateCommitRejectAck_CreatesCRMessage()
    {
        // Arrange
        var originalMessage = new RDEMessage();
        originalMessage.MessageHeader!.SetMessageControlId("12345");

        // Act
        var ack = ACKMessage.CreateCommitRejectAck(originalMessage, "Order rejected", "CR001");

        // Assert
        ack.MessageAcknowledgment!.AcknowledgmentCode.RawValue.Should().Be("CR");
        ack.MessageAcknowledgment.MessageControlId.RawValue.Should().Be("12345");
        ack.MessageAcknowledgment.TextMessage.RawValue.Should().Be("Order rejected");
        ack.MessageAcknowledgment.ErrorCondition.Identifier.Should().Be("CR001");
        ack.IsSuccessful().Should().BeFalse();
    }

    [Fact]
    public void ACKMessage_WithOriginalMessage_SwapsApplications()
    {
        // Arrange
        var originalMessage = new RDEMessage();
        originalMessage.MessageHeader!.SetSendingApplication("EMR System");
        originalMessage.MessageHeader.SetSendingFacility("Hospital");
        originalMessage.MessageHeader.SetReceivingApplication("Pharmacy System");
        originalMessage.MessageHeader.SetReceivingFacility("Pharmacy");
        originalMessage.MessageHeader.SetMessageControlId("12345");

        // Act
        var ack = ACKMessage.CreateSuccessfulAck(originalMessage);

        // Assert
        ack.MessageHeader!.SendingApplication.RawValue.Should().Be("Pharmacy System");
        ack.MessageHeader.SendingFacility.RawValue.Should().Be("Pharmacy");
        ack.MessageHeader.ReceivingApplication.RawValue.Should().Be("EMR System");
        ack.MessageHeader.ReceivingFacility.RawValue.Should().Be("Hospital");
    }

    [Fact]
    public void ACKMessage_SetAcknowledgmentCode_UpdatesField()
    {
        // Arrange
        var message = new ACKMessage();

        // Act
        message.SetAcknowledgmentCode("AE");

        // Assert
        message.MessageAcknowledgment!.AcknowledgmentCode.RawValue.Should().Be("AE");
    }

    [Fact]
    public void ACKMessage_SetTextMessage_UpdatesField()
    {
        // Arrange
        var message = new ACKMessage();

        // Act
        message.SetTextMessage("Custom message");

        // Assert
        message.MessageAcknowledgment!.TextMessage.RawValue.Should().Be("Custom message");
    }

    [Fact]
    public void ACKMessage_SetErrorCondition_UpdatesField()
    {
        // Arrange
        var message = new ACKMessage();

        // Act
        message.SetErrorCondition("E001", "Invalid data", "HL7");

        // Assert
        message.MessageAcknowledgment!.ErrorCondition.Identifier.Should().Be("E001");
        message.MessageAcknowledgment.ErrorCondition.Text.Should().Be("Invalid data");
        message.MessageAcknowledgment.ErrorCondition.CodingSystem.Should().Be("HL7");
    }

    [Fact]
    public void ACKMessage_GetAcknowledgmentCodeDescription_ReturnsCorrectDescription()
    {
        // Arrange
        var message = new ACKMessage();
        message.SetAcknowledgmentCode("AE");

        // Act
        var description = message.GetAcknowledgmentCodeDescription();

        // Assert
        description.Should().Be("Application Error");
    }

    [Fact]
    public void ACKMessage_GetAcknowledgedMessageControlId_ReturnsCorrectId()
    {
        // Arrange
        var message = new ACKMessage();
        message.MessageAcknowledgment!.SetMessageControlId("12345");

        // Act
        var controlId = message.GetAcknowledgedMessageControlId();

        // Assert
        controlId.Should().Be("12345");
    }

    [Fact]
    public void ACKMessage_GetTextMessage_ReturnsCorrectMessage()
    {
        // Arrange
        var message = new ACKMessage();
        message.SetTextMessage("Test message");

        // Act
        var textMessage = message.GetTextMessage();

        // Assert
        textMessage.Should().Be("Test message");
    }

    [Fact]
    public void ACKMessage_IsSuccessful_ReturnsCorrectValue()
    {
        // Arrange
        var successMessage = new ACKMessage(acknowledgmentCode: "AA");
        var errorMessage = new ACKMessage(acknowledgmentCode: "AE");
        var commitMessage = new ACKMessage(acknowledgmentCode: "CA");

        // Act & Assert
        successMessage.IsSuccessful().Should().BeTrue();
        errorMessage.IsSuccessful().Should().BeFalse();
        commitMessage.IsSuccessful().Should().BeTrue();
    }

    [Fact]
    public void ACKMessage_ToHL7String_GeneratesValidHL7()
    {
        // Arrange
        var originalMessage = new RDEMessage();
        originalMessage.MessageHeader!.SetMessageControlId("12345");
        var ack = ACKMessage.CreateSuccessfulAck(originalMessage, "Order processed");

        // Act
        var hl7String = ack.ToHL7String();

        // Assert
        hl7String.Should().StartWith("MSH|^~\\&|");
        hl7String.Should().Contain("ACK^");
        hl7String.Should().Contain("MSA|");
        hl7String.Should().Contain("AA|12345|Order processed");
    }

    [Fact]
    public void ACKMessage_ToDisplayString_FormatsReadably()
    {
        // Arrange
        var originalMessage = new RDEMessage();
        originalMessage.MessageHeader!.SetMessageControlId("12345");
        var ack = ACKMessage.CreateSuccessfulAck(originalMessage, "Order processed");

        // Act
        var displayString = ack.ToDisplayString();

        // Assert
        displayString.Should().Be("ACK Application Accept - Control ID: 12345 - Order processed");
    }

    [Fact]
    public void ACKMessage_ToDisplayString_WithoutTextMessage_HandlesCorrectly()
    {
        // Arrange
        var originalMessage = new RDEMessage();
        originalMessage.MessageHeader!.SetMessageControlId("12345");
        var ack = ACKMessage.CreateSuccessfulAck(originalMessage);

        // Act
        var displayString = ack.ToDisplayString();

        // Assert
        displayString.Should().Be("ACK Application Accept - Control ID: 12345");
    }

    [Fact]
    public void ACKMessage_Clone_CreatesDeepCopy()
    {
        // Arrange
        var originalMessage = new RDEMessage();
        originalMessage.MessageHeader!.SetMessageControlId("12345");
        var original = ACKMessage.CreateSuccessfulAck(originalMessage, "Test message");

        // Act
        var cloned = (ACKMessage)original.Clone();

        // Assert
        cloned.Should().NotBeSameAs(original);
        cloned.MessageAcknowledgment!.AcknowledgmentCode.RawValue.Should().Be(original.MessageAcknowledgment.AcknowledgmentCode.RawValue);
        cloned.MessageAcknowledgment.MessageControlId.RawValue.Should().Be(original.MessageAcknowledgment.MessageControlId.RawValue);
        cloned.MessageAcknowledgment.TextMessage.RawValue.Should().Be(original.MessageAcknowledgment.TextMessage.RawValue);
    }

    [Fact]
    public void ACKMessage_WorkflowScenario_HandlesCorrectly()
    {
        // Arrange - Create an original RDE message
        var originalMessage = new RDEMessage();
        originalMessage.MessageHeader!.SetMessageControlId("RDE123456");
        originalMessage.MessageHeader.SetSendingApplication("EMR System");
        originalMessage.MessageHeader.SetReceivingApplication("Pharmacy System");
        originalMessage.SetPatientDemographics("12345", "Doe", "John");
        originalMessage.SetMedication("NDC123", "Amoxicillin 500mg");

        // Act - Create acknowledgment
        var ack = ACKMessage.CreateSuccessfulAck(originalMessage, "Pharmacy order processed successfully");

        // Assert - Verify complete acknowledgment
        ack.MessageType.Should().Be("ACK");
        ack.TriggerEvent.Should().Be("O01"); // Should match original message trigger
        ack.MessageAcknowledgment!.AcknowledgmentCode.RawValue.Should().Be("AA");
        ack.MessageAcknowledgment.MessageControlId.RawValue.Should().Be("RDE123456");
        ack.MessageAcknowledgment.TextMessage.RawValue.Should().Be("Pharmacy order processed successfully");
        ack.IsSuccessful().Should().BeTrue();
        
        // Verify application swap
        ack.MessageHeader!.SendingApplication.RawValue.Should().Be("Pharmacy System");
        ack.MessageHeader.ReceivingApplication.RawValue.Should().Be("EMR System");
        
        // Verify HL7 string
        var hl7String = ack.ToHL7String();
        hl7String.Should().Contain("ACK^O01");
        hl7String.Should().Contain("AA|RDE123456|Pharmacy order processed successfully");
    }

    [Fact]
    public void ACKMessage_ErrorWorkflowScenario_HandlesCorrectly()
    {
        // Arrange - Create an original ADT message
        var originalMessage = ADTMessage.CreateAdmitPatient();
        originalMessage.MessageHeader!.SetMessageControlId("ADT789012");
        originalMessage.MessageHeader.SetSendingApplication("Registration System");
        originalMessage.MessageHeader.SetReceivingApplication("ADT System");

        // Act - Create error acknowledgment
        var ack = ACKMessage.CreateErrorAck(originalMessage, "Patient ID not found in system", "E404");

        // Assert - Verify error acknowledgment
        ack.MessageType.Should().Be("ACK");
        ack.TriggerEvent.Should().Be("A01"); // Should match original message trigger
        ack.MessageAcknowledgment!.AcknowledgmentCode.RawValue.Should().Be("AE");
        ack.MessageAcknowledgment.MessageControlId.RawValue.Should().Be("ADT789012");
        ack.MessageAcknowledgment.TextMessage.RawValue.Should().Be("Patient ID not found in system");
        ack.MessageAcknowledgment.ErrorCondition.Identifier.Should().Be("E404");
        ack.IsSuccessful().Should().BeFalse();
        
        // Verify application swap
        ack.MessageHeader!.SendingApplication.RawValue.Should().Be("ADT System");
        ack.MessageHeader.ReceivingApplication.RawValue.Should().Be("Registration System");
        
        // Verify HL7 string
        var hl7String = ack.ToHL7String();
        hl7String.Should().Contain("ACK^A01");
        hl7String.Should().Contain("AE|ADT789012|Patient ID not found in system");
    }
}