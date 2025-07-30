// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using FluentAssertions;
using Segmint.Core.Standards.HL7.v23.Types;
using Xunit;

namespace Segmint.Tests.HL7.Types;

public class TimestampFieldTests
{
    [Fact]
    public void TimestampField_DefaultConstructor_CreatesEmptyField()
    {
        // Arrange & Act
        var field = new TimestampField();

        // Assert
        field.DataType.Should().Be("TS");
        field.RawValue.Should().Be("");
        field.IsEmpty.Should().BeTrue();
        field.MaxLength.Should().Be(26);
        field.Value.Should().BeNull();
    }

    [Fact]
    public void TimestampField_WithDateTime_SetsValueCorrectly()
    {
        // Arrange
        var dateTime = new DateTime(2023, 12, 25, 10, 30, 45);

        // Act
        var field = new TimestampField(dateTime);

        // Assert
        field.Value.Should().Be(dateTime);
        field.RawValue.Should().Be("20231225103045");
    }

    [Fact]
    public void TimestampField_WithHL7String_ParsesCorrectly()
    {
        // Arrange
        var hl7String = "20231225103045";

        // Act
        var field = new TimestampField(hl7String);

        // Assert
        field.Value.Should().Be(new DateTime(2023, 12, 25, 10, 30, 45));
        field.RawValue.Should().Be(hl7String);
    }

    [Fact]
    public void TimestampField_WithHL7StringAndSeconds_ParsesCorrectly()
    {
        // Arrange
        var hl7String = "20231225103045.123";

        // Act
        var field = new TimestampField(hl7String);

        // Assert
        field.Value.Should().Be(new DateTime(2023, 12, 25, 10, 30, 45, 123));
        field.RawValue.Should().Be(hl7String);
    }

    [Fact]
    public void TimestampField_SetValue_WithDateTime_UpdatesCorrectly()
    {
        // Arrange
        var field = new TimestampField();
        var dateTime = new DateTime(2023, 12, 25, 10, 30, 45);

        // Act
        field.SetValue(dateTime);

        // Assert
        field.Value.Should().Be(dateTime);
        field.RawValue.Should().Be("20231225103045");
    }

    [Fact]
    public void TimestampField_SetValue_WithHL7String_UpdatesCorrectly()
    {
        // Arrange
        var field = new TimestampField();
        var hl7String = "20231225103045";

        // Act
        field.SetValue(hl7String);

        // Assert
        field.Value.Should().Be(new DateTime(2023, 12, 25, 10, 30, 45));
        field.RawValue.Should().Be(hl7String);
    }

    [Fact]
    public void TimestampField_WithInvalidFormat_ThrowsArgumentException()
    {
        // Arrange
        var field = new TimestampField();

        // Act & Assert
        var action = () => field.SetValue("invalid-timestamp");
        action.Should().Throw<ArgumentException>()
            .WithMessage("*Invalid timestamp format*");
    }

    [Fact]
    public void TimestampField_WithInvalidDate_ThrowsArgumentException()
    {
        // Arrange
        var field = new TimestampField();

        // Act & Assert
        var action = () => field.SetValue("20231332103045"); // Invalid day
        action.Should().Throw<ArgumentException>()
            .WithMessage("*Invalid timestamp format*");
    }

    [Fact]
    public void TimestampField_Now_CreatesCurrentTimestamp()
    {
        // Arrange
        var beforeNow = DateTime.Now;

        // Act
        var field = TimestampField.Now();

        // Assert
        var afterNow = DateTime.Now;
        field.ToDateTime().Should().BeOnOrAfter(beforeNow).And.BeOnOrBefore(afterNow);
    }

    [Fact]
    public void TimestampField_Today_CreatesDateOnlyTimestamp()
    {
        // Arrange
        var today = DateTime.Today;

        // Act
        var field = TimestampField.Today();

        // Assert
        field.Value.Should().Be(today);
        field.RawValue.Should().Be(today.ToString("yyyyMMdd"));
    }

    [Fact]
    public void TimestampField_FromDate_CreatesFromDateOnly()
    {
        // Arrange
        var date = new DateTime(2023, 12, 25);

        // Act
        var field = TimestampField.FromDate(date);

        // Assert
        field.Value.Should().Be(date);
        field.RawValue.Should().Be("20231225");
    }

    [Fact]
    public void TimestampField_Clone_CreatesDeepCopy()
    {
        // Arrange
        var original = new TimestampField(new DateTime(2023, 12, 25, 10, 30, 45), true);

        // Act
        var cloned = (TimestampField)original.Clone();

        // Assert
        cloned.Should().NotBeSameAs(original);
        cloned.RawValue.Should().Be(original.RawValue);
        cloned.Value.Should().Be(original.Value);
        cloned.IsRequired.Should().Be(original.IsRequired);
    }

    [Fact]
    public void TimestampField_ImplicitConversion_Works()
    {
        // Arrange
        var dateTime = new DateTime(2023, 12, 25, 10, 30, 45);
        var hl7String = "20231225103045";

        // Act
        TimestampField field1 = dateTime;
        TimestampField field2 = hl7String;
        string result = field1;

        // Assert
        field1.Value.Should().Be(dateTime);
        field2.Value.Should().Be(dateTime);
        result.Should().Be(hl7String);
    }

    [Theory]
    [InlineData("20231225")]
    [InlineData("202312251030")]
    [InlineData("20231225103045")]
    [InlineData("20231225103045.123")]
    [InlineData("20231225103045+0500")]
    [InlineData("20231225103045.123+0500")]
    public void TimestampField_ValidFormats_DoesNotThrow(string timestamp)
    {
        // Arrange
        var field = new TimestampField();

        // Act & Assert
        var action = () => field.SetValue(timestamp);
        action.Should().NotThrow();
    }

    [Theory]
    [InlineData("2023-12-25")]
    [InlineData("12/25/2023")]
    [InlineData("20231225 10:30:45")]
    [InlineData("20231225T103045")]
    [InlineData("not-a-date")]
    public void TimestampField_InvalidFormats_ThrowsException(string timestamp)
    {
        // Arrange
        var field = new TimestampField();

        // Act & Assert
        var action = () => field.SetValue(timestamp);
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void TimestampField_WithTimezone_ParsesCorrectly()
    {
        // Arrange
        var hl7String = "20231225103045+0500";

        // Act
        var field = new TimestampField(hl7String);

        // Assert
        field.RawValue.Should().Be(hl7String);
        field.Value.Should().NotBeNull();
        field.Timezone.Should().Be("+0500");
    }

    [Fact]
    public void TimestampField_ToDisplayString_FormatsReadably()
    {
        // Arrange
        var field = new TimestampField(new DateTime(2023, 12, 25, 10, 30, 45));

        // Act
        var result = field.ToDisplayString();

        // Assert
        result.Should().Be("12/25/2023 10:30:45 AM");
    }

    [Fact]
    public void TimestampField_ToDisplayString_WithEmptyValue_ReturnsEmpty()
    {
        // Arrange
        var field = new TimestampField();

        // Act
        var result = field.ToDisplayString();

        // Assert
        result.Should().Be("");
    }

    [Fact]
    public void TimestampField_WithMilliseconds_ParsesCorrectly()
    {
        // Arrange
        var hl7String = "20231225103045.123";

        // Act
        var field = new TimestampField(hl7String);

        // Assert
        field.Value.Should().Be(new DateTime(2023, 12, 25, 10, 30, 45, 123));
        field.RawValue.Should().Be(hl7String);
    }

    [Fact]
    public void TimestampField_WithNullValue_ClearsField()
    {
        // Arrange
        var field = new TimestampField(new DateTime(2023, 12, 25));

        // Act
        field.SetValue("");

        // Assert
        field.Value.Should().BeNull();
        field.RawValue.Should().Be("");
        field.IsEmpty.Should().BeTrue();
    }
}