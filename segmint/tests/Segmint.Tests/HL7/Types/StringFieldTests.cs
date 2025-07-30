// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using FluentAssertions;
using Segmint.Core.Standards.HL7.v23.Types;
using Xunit;

namespace Segmint.Tests.HL7.Types;

public class StringFieldTests
{
    [Fact]
    public void StringField_DefaultConstructor_CreatesEmptyField()
    {
        // Arrange & Act
        var field = new StringField();

        // Assert
        field.DataType.Should().Be("ST");
        field.RawValue.Should().Be("");
        field.IsEmpty.Should().BeTrue();
        field.IsRequired.Should().BeFalse();
        field.MaxLength.Should().BeNull();
    }

    [Fact]
    public void StringField_WithValue_SetsValueCorrectly()
    {
        // Arrange
        var testValue = "Test Value";

        // Act
        var field = new StringField(testValue);

        // Assert
        field.RawValue.Should().Be(testValue);
        field.IsEmpty.Should().BeFalse();
    }

    [Fact]
    public void StringField_WithMaxLength_EnforcesMaxLength()
    {
        // Arrange
        var field = new StringField(maxLength: 10);

        // Act
        var action = () => field.SetValue("This is a very long string that exceeds the maximum length");

        // Assert
        action.Should().Throw<ArgumentException>()
            .WithMessage("*exceeds maximum length*");
    }

    [Fact]
    public void StringField_WithRequiredTrue_ThrowsOnEmptyValue()
    {
        // Arrange
        var field = new StringField(isRequired: true);

        // Act
        var action = () => field.SetValue("");

        // Assert
        action.Should().Throw<ArgumentException>()
            .WithMessage("*Required field*cannot be empty*");
    }

    [Fact]
    public void StringField_WithRequiredTrue_ThrowsOnNullValue()
    {
        // Arrange
        var field = new StringField(isRequired: true);

        // Act
        var action = () => field.SetValue(null);

        // Assert
        action.Should().Throw<ArgumentException>()
            .WithMessage("*Required field*cannot be empty*");
    }

    [Fact]
    public void StringField_SetValue_UpdatesValue()
    {
        // Arrange
        var field = new StringField("Initial Value");

        // Act
        field.SetValue("Updated Value");

        // Assert
        field.RawValue.Should().Be("Updated Value");
    }

    [Fact]
    public void StringField_ToHL7String_ReturnsRawValue()
    {
        // Arrange
        var testValue = "Test Value";
        var field = new StringField(testValue);

        // Act
        var result = field.ToHL7String();

        // Assert
        result.Should().Be(testValue);
    }

    [Fact]
    public void StringField_Clone_CreatesDeepCopy()
    {
        // Arrange
        var original = new StringField("Original Value", maxLength: 50, isRequired: true);

        // Act
        var cloned = original.Clone();

        // Assert
        cloned.Should().NotBeSameAs(original);
        cloned.RawValue.Should().Be(original.RawValue);
        cloned.IsRequired.Should().Be(original.IsRequired);
        cloned.MaxLength.Should().Be(original.MaxLength);
        cloned.DataType.Should().Be(original.DataType);
    }

    [Fact]
    public void StringField_ImplicitConversion_FromString_Works()
    {
        // Arrange
        var testValue = "Test Value";

        // Act
        StringField field = testValue;

        // Assert
        field.RawValue.Should().Be(testValue);
    }

    [Fact]
    public void StringField_ImplicitConversion_ToString_Works()
    {
        // Arrange
        var testValue = "Test Value";
        var field = new StringField(testValue);

        // Act
        string result = field;

        // Assert
        result.Should().Be(testValue);
    }

    [Fact]
    public void StringField_Equals_ComparesCorrectly()
    {
        // Arrange
        var field1 = new StringField("Test", isRequired: true);
        var field2 = new StringField("Test", isRequired: true);
        var field3 = new StringField("Different", isRequired: true);
        var field4 = new StringField("Test", isRequired: false);

        // Act & Assert
        field1.Should().Be(field2);
        field1.Should().NotBe(field3);
        field1.Should().NotBe(field4);
    }

    [Fact]
    public void StringField_GetHashCode_GeneratesConsistentHash()
    {
        // Arrange
        var field1 = new StringField("Test", isRequired: true);
        var field2 = new StringField("Test", isRequired: true);

        // Act & Assert
        field1.GetHashCode().Should().Be(field2.GetHashCode());
    }

    [Fact]
    public void StringField_ToString_ReturnsFormattedString()
    {
        // Arrange
        var field = new StringField("Test Value");

        // Act
        var result = field.ToString();

        // Assert
        result.Should().Be("ST: Test Value");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void StringField_WithEmptyValue_IsEmptyReturnsTrue(string? value)
    {
        // Arrange
        var field = new StringField(value);

        // Act & Assert
        field.IsEmpty.Should().BeTrue();
    }

    [Theory]
    [InlineData("Value")]
    [InlineData("   ")]
    [InlineData("0")]
    public void StringField_WithNonEmptyValue_IsEmptyReturnsFalse(string value)
    {
        // Arrange
        var field = new StringField(value);

        // Act & Assert
        field.IsEmpty.Should().BeFalse();
    }
}