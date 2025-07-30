// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System;
using FluentAssertions;
using Segmint.Core.Standards.HL7.v23.Types;
using Xunit;

namespace Segmint.Tests.HL7.Types;

/// <summary>
/// Tests for CompositeQuantityField functionality.
/// </summary>
public class CompositeQuantityFieldTests
{
    [Fact]
    public void Constructor_WithValue_SetsValueCorrectly()
    {
        // Arrange & Act
        var field = new CompositeQuantityField("10^MG");

        // Assert
        field.RawValue.Should().Be("10^MG");
        field.Quantity.Should().Be("10");
        field.Units.Should().Be("MG");
    }

    [Fact]
    public void Constructor_WithQuantityAndUnits_FormatsCorrectly()
    {
        // Arrange & Act
        var field = new CompositeQuantityField(25.5m, "ML");

        // Assert
        field.RawValue.Should().Be("25.5^ML");
        field.Quantity.Should().Be("25.5");
        field.Units.Should().Be("ML");
    }

    [Fact]
    public void GetQuantityAsDecimal_WithValidQuantity_ReturnsDecimal()
    {
        // Arrange
        var field = new CompositeQuantityField("100.25^MG");

        // Act
        var quantity = field.GetQuantityAsDecimal();

        // Assert
        quantity.Should().Be(100.25m);
    }

    [Fact]
    public void GetQuantityAsDecimal_WithInvalidQuantity_ReturnsNull()
    {
        // Arrange
        var field = new CompositeQuantityField("ABC^MG");

        // Act
        var quantity = field.GetQuantityAsDecimal();

        // Assert
        quantity.Should().BeNull();
    }

    [Fact]
    public void SetComponents_WithDecimalAndUnits_FormatsCorrectly()
    {
        // Arrange
        var field = new CompositeQuantityField();

        // Act
        field.SetComponents(15.75m, "GM");

        // Assert
        field.RawValue.Should().Be("15.75^GM");
        field.Quantity.Should().Be("15.75");
        field.Units.Should().Be("GM");
    }

    [Fact]
    public void SetComponents_WithStringAndUnits_FormatsCorrectly()
    {
        // Arrange
        var field = new CompositeQuantityField();

        // Act
        field.SetComponents("50", "KG");

        // Assert
        field.RawValue.Should().Be("50^KG");
        field.Quantity.Should().Be("50");
        field.Units.Should().Be("KG");
    }

    [Fact]
    public void SetComponents_WithEmptyUnits_TrimsEmptyComponents()
    {
        // Arrange
        var field = new CompositeQuantityField();

        // Act
        field.SetComponents("10", "");

        // Assert
        field.RawValue.Should().Be("10");
        field.Quantity.Should().Be("10");
        field.Units.Should().Be("");
    }

    [Fact]
    public void GetComponent_WithValidIndex_ReturnsComponent()
    {
        // Arrange
        var field = new CompositeQuantityField("75^LB");

        // Act & Assert
        field.GetComponent(1).Should().Be("75");
        field.GetComponent(2).Should().Be("LB");
    }

    [Fact]
    public void GetComponent_WithInvalidIndex_ThrowsException()
    {
        // Arrange
        var field = new CompositeQuantityField("75^LB");

        // Act & Assert
        field.Invoking(f => f.GetComponent(0)).Should().Throw<ArgumentOutOfRangeException>();
        field.Invoking(f => f.GetComponent(3)).Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void SetComponent_WithValidIndex_SetsComponent()
    {
        // Arrange
        var field = new CompositeQuantityField("75^LB");

        // Act
        field.SetComponent(1, "100");
        field.SetComponent(2, "KG");

        // Assert
        field.RawValue.Should().Be("100^KG");
        field.Quantity.Should().Be("100");
        field.Units.Should().Be("KG");
    }

    [Fact]
    public void ValidateValue_WithValidQuantity_DoesNotThrow()
    {
        // Arrange & Act & Assert
        var field = new CompositeQuantityField();
        field.Invoking(f => f.SetValue("25.5^MG")).Should().NotThrow();
    }

    [Fact]
    public void ValidateValue_WithInvalidQuantity_ThrowsException()
    {
        // Arrange
        var field = new CompositeQuantityField();

        // Act & Assert
        field.Invoking(f => f.SetValue("ABC^MG")).Should().Throw<ArgumentException>()
            .WithMessage("*Quantity component must be numeric*");
    }

    [Fact]
    public void ValidateValue_WithTooManyComponents_ThrowsException()
    {
        // Arrange
        var field = new CompositeQuantityField();

        // Act & Assert
        field.Invoking(f => f.SetValue("10^MG^EXTRA")).Should().Throw<ArgumentException>()
            .WithMessage("*cannot have more than 2 components*");
    }

    [Fact]
    public void ValidateValue_WithControlCharacters_ThrowsException()
    {
        // Arrange
        var field = new CompositeQuantityField();

        // Act & Assert
        field.Invoking(f => f.SetValue("10|BAD^MG")).Should().Throw<ArgumentException>()
            .WithMessage("*cannot contain HL7 control characters*");
    }

    [Fact]
    public void Clone_CreatesIndependentCopy()
    {
        // Arrange
        var original = new CompositeQuantityField("50^ML", isRequired: true);

        // Act
        var clone = (CompositeQuantityField)original.Clone();

        // Assert
        clone.Should().NotBeSameAs(original);
        clone.RawValue.Should().Be(original.RawValue);
        clone.IsRequired.Should().Be(original.IsRequired);
        clone.Quantity.Should().Be("50");
        clone.Units.Should().Be("ML");
    }

    [Fact]
    public void Create_StaticMethod_CreatesCorrectField()
    {
        // Arrange & Act
        var field = CompositeQuantityField.Create(100m, "MG", isRequired: true);

        // Assert
        field.RawValue.Should().Be("100^MG");
        field.IsRequired.Should().BeTrue();
        field.Quantity.Should().Be("100");
        field.Units.Should().Be("MG");
    }

    [Fact]
    public void ImplicitConversion_FromString_CreatesField()
    {
        // Arrange & Act
        CompositeQuantityField field = "75^KG";

        // Assert
        field.RawValue.Should().Be("75^KG");
        field.Quantity.Should().Be("75");
        field.Units.Should().Be("KG");
    }

    [Fact]
    public void ImplicitConversion_ToString_ReturnsRawValue()
    {
        // Arrange
        var field = new CompositeQuantityField("200^LB");

        // Act
        string value = field;

        // Assert
        value.Should().Be("200^LB");
    }

    [Fact]
    public void DataType_ReturnsCorrectValue()
    {
        // Arrange
        var field = new CompositeQuantityField();

        // Act & Assert
        field.DataType.Should().Be("CQ");
    }

    [Fact]
    public void MaxLength_ReturnsCorrectValue()
    {
        // Arrange
        var field = new CompositeQuantityField();

        // Act & Assert
        field.MaxLength.Should().Be(60);
    }

    [Theory]
    [InlineData("", true)]
    [InlineData("10", false)]
    [InlineData("10^MG", false)]
    [InlineData("0^GM", false)]
    public void IsEmpty_ReturnsCorrectValue(string value, bool expectedEmpty)
    {
        // Arrange
        var field = new CompositeQuantityField(value);

        // Act & Assert
        field.IsEmpty.Should().Be(expectedEmpty);
    }
}