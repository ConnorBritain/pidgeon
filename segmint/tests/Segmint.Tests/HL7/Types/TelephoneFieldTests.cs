// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using FluentAssertions;
using Segmint.Core.Standards.HL7.v23.Types;
using Xunit;

namespace Segmint.Tests.HL7.Types;

public class TelephoneFieldTests
{
    [Fact]
    public void TelephoneField_DefaultConstructor_CreatesEmptyField()
    {
        // Arrange & Act
        var field = new TelephoneField();

        // Assert
        field.DataType.Should().Be("XTN");
        field.RawValue.Should().Be("");
        field.IsEmpty.Should().BeTrue();
        field.MaxLength.Should().Be(250);
    }

    [Fact]
    public void TelephoneField_CreatePhone_CreatesPhoneNumber()
    {
        // Arrange
        var phoneNumber = "555-1234";
        var areaCode = "555";
        var extension = "123";

        // Act
        var field = TelephoneField.CreatePhone(phoneNumber, areaCode, extension);

        // Assert
        field.UseCode.Should().Be("PRN");
        field.EquipmentType.Should().Be("PH");
        field.PhoneNumber.Should().Be(phoneNumber);
        field.AreaCode.Should().Be(areaCode);
        field.Extension.Should().Be(extension);
        field.RawValue.Should().Be("PRN^PH^^555^555-1234^123");
    }

    [Fact]
    public void TelephoneField_CreateEmail_CreatesEmailAddress()
    {
        // Arrange
        var emailAddress = "test@example.com";

        // Act
        var field = TelephoneField.CreateEmail(emailAddress);

        // Assert
        field.UseCode.Should().Be("NET");
        field.EquipmentType.Should().Be("Internet");
        field.EmailAddress.Should().Be(emailAddress);
        field.RawValue.Should().Be("NET^Internet^test@example.com");
    }

    [Fact]
    public void TelephoneField_CreateFax_CreatesFaxNumber()
    {
        // Arrange
        var faxNumber = "555-5678";
        var areaCode = "555";

        // Act
        var field = TelephoneField.CreateFax(faxNumber, areaCode);

        // Assert
        field.UseCode.Should().Be("WPN");
        field.EquipmentType.Should().Be("FX");
        field.PhoneNumber.Should().Be(faxNumber);
        field.AreaCode.Should().Be(areaCode);
        field.RawValue.Should().Be("WPN^FX^^555^555-5678");
    }

    [Fact]
    public void TelephoneField_WithHL7String_ParsesCorrectly()
    {
        // Arrange
        var hl7String = "PRN^PH^^555^555-1234^123^Test Phone";

        // Act
        var field = new TelephoneField(hl7String);

        // Assert
        field.UseCode.Should().Be("PRN");
        field.EquipmentType.Should().Be("PH");
        field.EmailAddress.Should().Be("");
        field.CountryCode.Should().Be("555");
        field.AreaCode.Should().Be("555");
        field.PhoneNumber.Should().Be("555-1234");
        field.Extension.Should().Be("123");
        field.Text.Should().Be("Test Phone");
    }

    [Fact]
    public void TelephoneField_ValidateValue_ValidatesEmailFormat()
    {
        // Arrange
        var field = new TelephoneField();

        // Act & Assert
        var action = () => field.SetValue("NET^Internet^invalid-email");
        action.Should().Throw<ArgumentException>()
            .WithMessage("*Invalid email address format*");
    }

    [Fact]
    public void TelephoneField_ValidateValue_ValidatesPhoneFormat()
    {
        // Arrange
        var field = new TelephoneField();

        // Act & Assert
        var action = () => field.SetValue("PRN^PH^^^^^invalid-phone-with-letters");
        action.Should().Throw<ArgumentException>()
            .WithMessage("*Invalid phone number format*");
    }

    [Fact]
    public void TelephoneField_ValidateValue_ValidatesAreaCodeFormat()
    {
        // Arrange
        var field = new TelephoneField();

        // Act & Assert
        var action = () => field.SetValue("PRN^PH^^^invalid-area-code");
        action.Should().Throw<ArgumentException>()
            .WithMessage("*Invalid area code format*");
    }

    [Fact]
    public void TelephoneField_ToDisplayString_FormatsPhoneNumber()
    {
        // Arrange
        var field = TelephoneField.CreatePhone("555-1234", "555", "123");

        // Act
        var result = field.ToDisplayString();

        // Assert
        result.Should().Be("(555) 555-1234 ext. 123");
    }

    [Fact]
    public void TelephoneField_ToDisplayString_FormatsEmailAddress()
    {
        // Arrange
        var field = TelephoneField.CreateEmail("test@example.com");

        // Act
        var result = field.ToDisplayString();

        // Assert
        result.Should().Be("test@example.com");
    }

    [Fact]
    public void TelephoneField_ToDisplayString_FormatsPhoneWithoutAreaCode()
    {
        // Arrange
        var field = TelephoneField.CreatePhone("555-1234");

        // Act
        var result = field.ToDisplayString();

        // Assert
        result.Should().Be("555-1234");
    }

    [Fact]
    public void TelephoneField_Clone_CreatesDeepCopy()
    {
        // Arrange
        var original = TelephoneField.CreatePhone("555-1234", "555", "123", true);

        // Act
        var cloned = (TelephoneField)original.Clone();

        // Assert
        cloned.Should().NotBeSameAs(original);
        cloned.RawValue.Should().Be(original.RawValue);
        cloned.IsRequired.Should().Be(original.IsRequired);
        cloned.UseCode.Should().Be(original.UseCode);
        cloned.PhoneNumber.Should().Be(original.PhoneNumber);
    }

    [Fact]
    public void TelephoneField_ImplicitConversion_Works()
    {
        // Arrange
        var hl7String = "PRN^PH^^555^555-1234";

        // Act
        TelephoneField field = hl7String;
        string result = field;

        // Assert
        result.Should().Be(hl7String);
    }

    [Theory]
    [InlineData("test@example.com")]
    [InlineData("user.name@domain.co.uk")]
    [InlineData("test+tag@example.org")]
    public void TelephoneField_ValidEmail_DoesNotThrow(string email)
    {
        // Arrange
        var field = new TelephoneField();

        // Act & Assert
        var action = () => field.SetValue($"NET^Internet^{email}");
        action.Should().NotThrow();
    }

    [Theory]
    [InlineData("555-1234")]
    [InlineData("(555) 123-4567")]
    [InlineData("555.123.4567")]
    [InlineData("+1-555-123-4567")]
    public void TelephoneField_ValidPhoneNumber_DoesNotThrow(string phone)
    {
        // Arrange
        var field = new TelephoneField();

        // Act & Assert
        var action = () => field.SetValue($"PRN^PH^^^^^{phone}");
        action.Should().NotThrow();
    }

    [Fact]
    public void TelephoneField_WithMinimalComponents_CreatesCorrectHL7String()
    {
        // Arrange
        var field = new TelephoneField();

        // Act
        field.SetValue("PRN");

        // Assert
        field.UseCode.Should().Be("PRN");
        field.RawValue.Should().Be("PRN");
    }

    [Fact]
    public void TelephoneField_WithEmptyValue_DoesNotThrow()
    {
        // Arrange
        var field = new TelephoneField();

        // Act & Assert
        var action = () => field.SetValue("");
        action.Should().NotThrow();
    }
}