// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using FluentAssertions;
using Segmint.Core.Standards.HL7.v23.Types;
using Xunit;

namespace Segmint.Tests.HL7.Types;

public class PersonNameFieldTests
{
    [Fact]
    public void PersonNameField_DefaultConstructor_CreatesEmptyField()
    {
        // Arrange & Act
        var field = new PersonNameField();

        // Assert
        field.DataType.Should().Be("XPN");
        field.RawValue.Should().Be("");
        field.IsEmpty.Should().BeTrue();
        field.MaxLength.Should().Be(250);
        field.LastName.Should().Be("");
        field.FirstName.Should().Be("");
        field.MiddleName.Should().Be("");
    }

    [Fact]
    public void PersonNameField_WithHL7String_ParsesCorrectly()
    {
        // Arrange
        var hl7String = "Doe^John^M^Jr^Dr";

        // Act
        var field = new PersonNameField(hl7String);

        // Assert
        field.LastName.Should().Be("Doe");
        field.FirstName.Should().Be("John");
        field.MiddleName.Should().Be("M");
        field.Suffix.Should().Be("Jr");
        field.Prefix.Should().Be("Dr");
        field.RawValue.Should().Be(hl7String);
    }

    [Fact]
    public void PersonNameField_SetName_UpdatesComponents()
    {
        // Arrange
        var field = new PersonNameField();

        // Act
        field.SetName("Smith", "Jane", "A", "MD", "Dr");

        // Assert
        field.LastName.Should().Be("Smith");
        field.FirstName.Should().Be("Jane");
        field.MiddleName.Should().Be("A");
        field.Suffix.Should().Be("MD");
        field.Prefix.Should().Be("Dr");
        field.RawValue.Should().Be("Smith^Jane^A^MD^Dr");
    }

    [Fact]
    public void PersonNameField_SetName_WithMinimalComponents_UpdatesCorrectly()
    {
        // Arrange
        var field = new PersonNameField();

        // Act
        field.SetName("Smith", "Jane");

        // Assert
        field.LastName.Should().Be("Smith");
        field.FirstName.Should().Be("Jane");
        field.MiddleName.Should().Be("");
        field.RawValue.Should().Be("Smith^Jane");
    }

    [Fact]
    public void PersonNameField_SetName_WithOnlyLastName_UpdatesCorrectly()
    {
        // Arrange
        var field = new PersonNameField();

        // Act
        field.SetName("Smith");

        // Assert
        field.LastName.Should().Be("Smith");
        field.FirstName.Should().Be("");
        field.RawValue.Should().Be("Smith");
    }

    [Fact]
    public void PersonNameField_Create_CreatesNameCorrectly()
    {
        // Arrange & Act
        var field = PersonNameField.Create("Doe", "John", "M");

        // Assert
        field.LastName.Should().Be("Doe");
        field.FirstName.Should().Be("John");
        field.MiddleName.Should().Be("M");
        field.RawValue.Should().Be("Doe^John^M");
    }

    [Fact]
    public void PersonNameField_CreateDoctor_CreatesWithDrPrefix()
    {
        // Arrange & Act
        var field = PersonNameField.CreateDoctor("Smith", "Jane", "A", "MD");

        // Assert
        field.LastName.Should().Be("Smith");
        field.FirstName.Should().Be("Jane");
        field.MiddleName.Should().Be("A");
        field.Suffix.Should().Be("MD");
        field.Prefix.Should().Be("Dr");
        field.RawValue.Should().Be("Smith^Jane^A^MD^Dr");
    }

    [Fact]
    public void PersonNameField_CreatePatient_CreatesPatientName()
    {
        // Arrange & Act
        var field = PersonNameField.CreatePatient("Johnson", "Michael", "R");

        // Assert
        field.LastName.Should().Be("Johnson");
        field.FirstName.Should().Be("Michael");
        field.MiddleName.Should().Be("R");
        field.RawValue.Should().Be("Johnson^Michael^R");
    }

    [Fact]
    public void PersonNameField_ToDisplayString_FormatsCorrectly()
    {
        // Arrange
        var field = new PersonNameField("Doe^John^M^Jr^Dr");

        // Act
        var result = field.ToDisplayString();

        // Assert
        result.Should().Be("Dr John M Doe Jr");
    }

    [Fact]
    public void PersonNameField_ToDisplayString_WithMinimalComponents_FormatsCorrectly()
    {
        // Arrange
        var field = new PersonNameField("Smith^Jane");

        // Act
        var result = field.ToDisplayString();

        // Assert
        result.Should().Be("Jane Smith");
    }

    [Fact]
    public void PersonNameField_ToDisplayString_WithOnlyLastName_FormatsCorrectly()
    {
        // Arrange
        var field = new PersonNameField("Smith");

        // Act
        var result = field.ToDisplayString();

        // Assert
        result.Should().Be("Smith");
    }

    [Fact]
    public void PersonNameField_ToFormalString_FormatsCorrectly()
    {
        // Arrange
        var field = new PersonNameField("Doe^John^M^Jr^Dr");

        // Act
        var result = field.ToFormalString();

        // Assert
        result.Should().Be("Doe, John M, Jr, Dr");
    }

    [Fact]
    public void PersonNameField_ToFormalString_WithMinimalComponents_FormatsCorrectly()
    {
        // Arrange
        var field = new PersonNameField("Smith^Jane");

        // Act
        var result = field.ToFormalString();

        // Assert
        result.Should().Be("Smith, Jane");
    }

    [Fact]
    public void PersonNameField_Clone_CreatesDeepCopy()
    {
        // Arrange
        var original = new PersonNameField("Doe^John^M^Jr^Dr", true);

        // Act
        var cloned = (PersonNameField)original.Clone();

        // Assert
        cloned.Should().NotBeSameAs(original);
        cloned.RawValue.Should().Be(original.RawValue);
        cloned.IsRequired.Should().Be(original.IsRequired);
        cloned.LastName.Should().Be(original.LastName);
        cloned.FirstName.Should().Be(original.FirstName);
        cloned.MiddleName.Should().Be(original.MiddleName);
    }

    [Fact]
    public void PersonNameField_ImplicitConversion_Works()
    {
        // Arrange
        var hl7String = "Doe^John^M";

        // Act
        PersonNameField field = hl7String;
        string result = field;

        // Assert
        field.LastName.Should().Be("Doe");
        field.FirstName.Should().Be("John");
        field.MiddleName.Should().Be("M");
        result.Should().Be(hl7String);
    }

    [Fact]
    public void PersonNameField_ValidateValue_AllowsValidCharacters()
    {
        // Arrange
        var field = new PersonNameField();

        // Act & Assert
        var action = () => field.SetValue("O'Connor^Mary-Jane^A");
        action.Should().NotThrow();
    }

    [Fact]
    public void PersonNameField_ValidateValue_RejectsControlCharacters()
    {
        // Arrange
        var field = new PersonNameField();

        // Act & Assert
        var action = () => field.SetValue("Doe|John");
        action.Should().Throw<ArgumentException>()
            .WithMessage("*cannot contain HL7 control characters*");
    }

    [Fact]
    public void PersonNameField_WithComplexName_HandlesCorrectly()
    {
        // Arrange
        var field = new PersonNameField();

        // Act
        field.SetName("van der Berg", "Johannes", "Willem", "III", "Prof");

        // Assert
        field.LastName.Should().Be("van der Berg");
        field.FirstName.Should().Be("Johannes");
        field.MiddleName.Should().Be("Willem");
        field.Suffix.Should().Be("III");
        field.Prefix.Should().Be("Prof");
        field.ToDisplayString().Should().Be("Prof Johannes Willem van der Berg III");
    }

    [Fact]
    public void PersonNameField_WithEmptyValue_HandlesGracefully()
    {
        // Arrange
        var field = new PersonNameField();

        // Act
        field.SetValue("");

        // Assert
        field.IsEmpty.Should().BeTrue();
        field.LastName.Should().Be("");
        field.FirstName.Should().Be("");
        field.ToDisplayString().Should().Be("");
    }

    [Fact]
    public void PersonNameField_WithPartialComponents_HandlesCorrectly()
    {
        // Arrange
        var field = new PersonNameField();

        // Act
        field.SetValue("Doe^^M");

        // Assert
        field.LastName.Should().Be("Doe");
        field.FirstName.Should().Be("");
        field.MiddleName.Should().Be("M");
        field.ToDisplayString().Should().Be("M Doe");
    }

    [Theory]
    [InlineData("Smith", "Smith")]
    [InlineData("Smith^John", "John Smith")]
    [InlineData("Smith^John^M", "John M Smith")]
    [InlineData("Smith^John^M^Jr", "John M Smith Jr")]
    [InlineData("Smith^John^M^Jr^Dr", "Dr John M Smith Jr")]
    public void PersonNameField_ToDisplayString_Various_Formats(string hl7String, string expectedDisplay)
    {
        // Arrange
        var field = new PersonNameField(hl7String);

        // Act
        var result = field.ToDisplayString();

        // Assert
        result.Should().Be(expectedDisplay);
    }

    [Fact]
    public void PersonNameField_GetInitials_ReturnsCorrectInitials()
    {
        // Arrange
        var field = new PersonNameField("Smith^John^Michael");

        // Act
        var result = field.GetInitials();

        // Assert
        result.Should().Be("J.M.S.");
    }

    [Fact]
    public void PersonNameField_GetInitials_WithEmptyComponents_HandlesCorrectly()
    {
        // Arrange
        var field = new PersonNameField("Smith^John");

        // Act
        var result = field.GetInitials();

        // Assert
        result.Should().Be("J.S.");
    }
}