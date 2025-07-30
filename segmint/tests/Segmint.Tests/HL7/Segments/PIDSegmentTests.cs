// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using FluentAssertions;
using Segmint.Core.Standards.HL7.v23.Segments;
using Segmint.Core.Standards.HL7.v23.Types;
using Xunit;

namespace Segmint.Tests.HL7.Segments;

public class PIDSegmentTests
{
    [Fact]
    public void PIDSegment_DefaultConstructor_CreatesSegmentWithCorrectId()
    {
        // Arrange & Act
        var segment = new PIDSegment();

        // Assert
        segment.SegmentId.Should().Be("PID");
        segment.FieldCount.Should().Be(39); // PID has 39 fields
    }

    [Fact]
    public void PIDSegment_SetPatientId_UpdatesField()
    {
        // Arrange
        var segment = new PIDSegment();
        var patientId = "12345";

        // Act
        segment.SetPatientId(patientId);

        // Assert
        segment.PatientIdentifierList.IdNumber.Should().Be(patientId);
        segment.PatientIdentifierList.IdentifierTypeCode.Should().Be("MR");
    }

    [Fact]
    public void PIDSegment_SetPatientName_UpdatesField()
    {
        // Arrange
        var segment = new PIDSegment();

        // Act
        segment.SetPatientName("Doe", "John", "M");

        // Assert
        segment.PatientName.LastName.Should().Be("Doe");
        segment.PatientName.FirstName.Should().Be("John");
        segment.PatientName.MiddleName.Should().Be("M");
    }

    [Fact]
    public void PIDSegment_SetDateOfBirth_UpdatesField()
    {
        // Arrange
        var segment = new PIDSegment();
        var dateOfBirth = new DateTime(1980, 5, 15);

        // Act
        segment.SetDateOfBirth(dateOfBirth);

        // Assert
        segment.DateTimeOfBirth.Value.Should().Be(dateOfBirth);
    }

    [Fact]
    public void PIDSegment_SetGender_UpdatesField()
    {
        // Arrange
        var segment = new PIDSegment();

        // Act
        segment.SetGender("M");

        // Assert
        segment.AdministrativeSex.RawValue.Should().Be("M");
    }

    [Fact]
    public void PIDSegment_SetPatientAddress_UpdatesField()
    {
        // Arrange
        var segment = new PIDSegment();

        // Act
        segment.SetPatientAddress("123 Main St", "Anytown", "ST", "12345", "USA");

        // Assert
        segment.PatientAddress.StreetAddress.Should().Be("123 Main St");
        segment.PatientAddress.City.Should().Be("Anytown");
        segment.PatientAddress.StateOrProvince.Should().Be("ST");
        segment.PatientAddress.ZipOrPostalCode.Should().Be("12345");
        segment.PatientAddress.Country.Should().Be("USA");
    }

    [Fact]
    public void PIDSegment_SetPhoneNumber_UpdatesField()
    {
        // Arrange
        var segment = new PIDSegment();

        // Act
        segment.SetPhoneNumber("555-1234");

        // Assert
        segment.PhoneNumberHome.Value.Should().Be("555-1234");
    }

    [Fact]
    public void PIDSegment_SetSocialSecurityNumber_UpdatesField()
    {
        // Arrange
        var segment = new PIDSegment();

        // Act
        segment.SetSocialSecurityNumber("123-45-6789");

        // Assert
        segment.SocialSecurityNumber.RawValue.Should().Be("123-45-6789");
    }

    [Fact]
    public void PIDSegment_SetMaritalStatus_UpdatesField()
    {
        // Arrange
        var segment = new PIDSegment();

        // Act
        segment.SetMaritalStatus("M");

        // Assert
        segment.MaritalStatus.RawValue.Should().Be("M");
    }

    [Fact]
    public void PIDSegment_SetReligion_UpdatesField()
    {
        // Arrange
        var segment = new PIDSegment();

        // Act
        segment.SetReligion("CHR", "Christian");

        // Assert
        segment.Religion.Identifier.Should().Be("CHR");
        segment.Religion.Text.Should().Be("Christian");
    }

    [Fact]
    public void PIDSegment_SetRace_UpdatesField()
    {
        // Arrange
        var segment = new PIDSegment();

        // Act
        segment.SetRace("W", "White");

        // Assert
        segment.Race.Identifier.Should().Be("W");
        segment.Race.Text.Should().Be("White");
    }

    [Fact]
    public void PIDSegment_SetEthnicGroup_UpdatesField()
    {
        // Arrange
        var segment = new PIDSegment();

        // Act
        segment.SetEthnicGroup("H", "Hispanic");

        // Assert
        segment.EthnicGroup.Identifier.Should().Be("H");
        segment.EthnicGroup.Text.Should().Be("Hispanic");
    }

    [Fact]
    public void PIDSegment_SetPrimaryLanguage_UpdatesField()
    {
        // Arrange
        var segment = new PIDSegment();

        // Act
        segment.SetPrimaryLanguage("EN", "English");

        // Assert
        segment.PrimaryLanguage.Identifier.Should().Be("EN");
        segment.PrimaryLanguage.Text.Should().Be("English");
    }

    [Fact]
    public void PIDSegment_SetAccountNumber_UpdatesField()
    {
        // Arrange
        var segment = new PIDSegment();

        // Act
        segment.SetAccountNumber("ACC123456");

        // Assert
        segment.PatientAccountNumber.Value.Should().Be("ACC123456^^^AN");
    }

    [Fact]
    public void PIDSegment_ToHL7String_GeneratesCorrectFormat()
    {
        // Arrange
        var segment = new PIDSegment();
        segment.SetPatientId("12345");
        segment.SetPatientName("Doe", "John", "M");
        segment.SetDateOfBirth(new DateTime(1980, 5, 15));
        segment.SetGender("M");

        // Act
        var result = segment.ToHL7String();

        // Assert
        result.Should().StartWith("PID|");
        result.Should().Contain("12345^^MR");
        result.Should().Contain("Doe^John^M");
        result.Should().Contain("19800515");
        result.Should().Contain("M");
    }

    [Fact]
    public void PIDSegment_ToDisplayString_FormatsReadably()
    {
        // Arrange
        var segment = new PIDSegment();
        segment.SetPatientId("12345");
        segment.SetPatientName("Doe", "John", "M");
        segment.SetDateOfBirth(new DateTime(1980, 5, 15));
        segment.SetGender("M");

        // Act
        var result = segment.ToDisplayString();

        // Assert
        result.Should().Be("John M Doe (ID: 12345) - Male, DOB: 5/15/1980");
    }

    [Fact]
    public void PIDSegment_ToDisplayString_WithMinimalData_HandlesGracefully()
    {
        // Arrange
        var segment = new PIDSegment();
        segment.SetPatientId("12345");
        segment.SetPatientName("Doe", "John");

        // Act
        var result = segment.ToDisplayString();

        // Assert
        result.Should().Be("John Doe (ID: 12345)");
    }

    [Fact]
    public void PIDSegment_Clone_CreatesDeepCopy()
    {
        // Arrange
        var original = new PIDSegment();
        original.SetPatientId("12345");
        original.SetPatientName("Doe", "John", "M");
        original.SetDateOfBirth(new DateTime(1980, 5, 15));

        // Act
        var cloned = (PIDSegment)original.Clone();

        // Assert
        cloned.Should().NotBeSameAs(original);
        cloned.PatientIdentifierList.IdNumber.Should().Be(original.PatientIdentifierList.IdNumber);
        cloned.PatientName.LastName.Should().Be(original.PatientName.LastName);
        cloned.DateTimeOfBirth.Value.Should().Be(original.DateTimeOfBirth.Value);
    }

    [Fact]
    public void PIDSegment_FieldAccess_ByIndex_Works()
    {
        // Arrange
        var segment = new PIDSegment();
        segment.SetPatientId("12345");

        // Act
        var field3 = segment[3]; // Patient ID list is field 3

        // Assert
        field3.Should().BeOfType<ExtendedCompositeIdField>();
        ((ExtendedCompositeIdField)field3).IdNumber.Should().Be("12345");
    }

    [Fact]
    public void PIDSegment_RequiredFields_AreMarkedCorrectly()
    {
        // Arrange
        var segment = new PIDSegment();

        // Act & Assert
        segment.PatientIdentifierList.IsRequired.Should().BeTrue();
        segment.PatientName.IsRequired.Should().BeTrue();
        segment.DateTimeOfBirth.IsRequired.Should().BeFalse();
        segment.AdministrativeSex.IsRequired.Should().BeFalse();
    }

    [Fact]
    public void PIDSegment_SetPatientAddress_WithPartialAddress_HandlesCorrectly()
    {
        // Arrange
        var segment = new PIDSegment();

        // Act
        segment.SetPatientAddress("123 Main St", "Anytown", "ST", "12345");

        // Assert
        segment.PatientAddress.StreetAddress.Should().Be("123 Main St");
        segment.PatientAddress.City.Should().Be("Anytown");
        segment.PatientAddress.StateOrProvince.Should().Be("ST");
        segment.PatientAddress.ZipOrPostalCode.Should().Be("12345");
        segment.PatientAddress.Country.Should().Be("");
    }

    [Fact]
    public void PIDSegment_MultipleFieldUpdates_MaintainIndependence()
    {
        // Arrange
        var segment = new PIDSegment();

        // Act
        segment.SetPatientId("12345");
        segment.SetPatientName("Doe", "John");
        segment.SetGender("M");
        segment.SetMaritalStatus("S");

        // Assert
        segment.PatientIdentifierList.IdNumber.Should().Be("12345");
        segment.PatientName.LastName.Should().Be("Doe");
        segment.AdministrativeSex.RawValue.Should().Be("M");
        segment.MaritalStatus.RawValue.Should().Be("S");
    }

    [Fact]
    public void PIDSegment_WithComplexPatientData_HandlesCorrectly()
    {
        // Arrange
        var segment = new PIDSegment();

        // Act
        segment.SetPatientId("MRN123456");
        segment.SetPatientName("van der Berg", "Johannes", "Willem");
        segment.SetDateOfBirth(new DateTime(1975, 12, 31));
        segment.SetGender("M");
        segment.SetPatientAddress("123 Healthcare Ave", "Medical City", "HC", "12345", "USA");
        segment.SetPhoneNumber("555-0123");
        segment.SetSocialSecurityNumber("123-45-6789");
        segment.SetMaritalStatus("M");

        // Act
        var hl7String = segment.ToHL7String();

        // Assert
        hl7String.Should().Contain("MRN123456");
        hl7String.Should().Contain("van der Berg^Johannes^Willem");
        hl7String.Should().Contain("19751231");
        hl7String.Should().Contain("M");
        hl7String.Should().Contain("123 Healthcare Ave");
        hl7String.Should().Contain("555-0123");
        hl7String.Should().Contain("123-45-6789");
    }
}