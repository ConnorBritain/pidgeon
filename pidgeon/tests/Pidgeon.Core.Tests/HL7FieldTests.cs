// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using FluentAssertions;
using Pidgeon.Core.Domain.Messaging.HL7v2.Common;
using Xunit;

namespace Pidgeon.Core.Tests;

/// <summary>
/// Tests for HL7Field constructor patterns and consolidated functionality.
/// Verifies that constructor trinity pattern consolidation preserves behavior.
/// </summary>
public class HL7FieldTests
{
    /// <summary>
    /// Verifies that StringField constructor patterns work correctly.
    /// </summary>
    [Fact(DisplayName = "StringField constructors should preserve trinity pattern behavior")]
    public void Should_Preserve_StringField_Constructor_Patterns()
    {
        // Empty constructor
        var emptyField = new StringField();
        emptyField.Value.Should().BeNull();
        emptyField.IsRequired.Should().BeFalse();
        emptyField.MaxLength.Should().BeNull();

        // Value constructor
        var valueField = new StringField("test");
        valueField.Value.Should().Be("test");
        valueField.RawValue.Should().Be("test");

        // Value + constraints constructor
        var constrainedField = new StringField("test", 10, true);
        constrainedField.Value.Should().Be("test");
        constrainedField.MaxLength.Should().Be(10);
        constrainedField.IsRequired.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that NumericField constructor patterns work correctly.
    /// </summary>
    [Fact(DisplayName = "NumericField constructors should preserve trinity pattern behavior")]
    public void Should_Preserve_NumericField_Constructor_Patterns()
    {
        // Empty constructor
        var emptyField = new NumericField();
        emptyField.Value.Should().BeNull();
        emptyField.IsRequired.Should().BeFalse();
        emptyField.MaxLength.Should().Be(16); // HL7 v2.3 standard

        // Value constructor  
        var valueField = new NumericField(123.45m);
        valueField.Value.Should().Be(123.45m);
        valueField.RawValue.Should().Be("123.45");

        // Value + constraints constructor
        var constrainedField = new NumericField(456.78m, true);
        constrainedField.Value.Should().Be(456.78m);
        constrainedField.IsRequired.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that DateField constructor patterns work correctly.
    /// </summary>
    [Fact(DisplayName = "DateField constructors should preserve trinity pattern behavior")]
    public void Should_Preserve_DateField_Constructor_Patterns()
    {
        var testDate = new DateTime(2025, 9, 2);

        // Empty constructor
        var emptyField = new DateField();
        emptyField.Value.Should().BeNull();
        emptyField.IsRequired.Should().BeFalse();

        // Value constructor
        var valueField = new DateField(testDate);
        valueField.Value.Should().Be(testDate);
        valueField.RawValue.Should().Be("20250902");

        // Value + constraints constructor
        var constrainedField = new DateField(testDate, true);
        constrainedField.Value.Should().Be(testDate);
        constrainedField.IsRequired.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that TimestampField constructor patterns work correctly.
    /// </summary>
    [Fact(DisplayName = "TimestampField constructors should preserve trinity pattern behavior")]
    public void Should_Preserve_TimestampField_Constructor_Patterns()
    {
        var testTime = new DateTime(2025, 9, 2, 14, 30, 0);

        // Empty constructor
        var emptyField = new TimestampField();
        emptyField.Value.Should().BeNull();
        emptyField.IsRequired.Should().BeFalse();

        // Value constructor
        var valueField = new TimestampField(testTime);
        valueField.Value.Should().Be(testTime);
        valueField.RawValue.Should().Be("20250902143000");

        // Value + constraints constructor
        var constrainedField = new TimestampField(testTime, true);
        constrainedField.Value.Should().Be(testTime);
        constrainedField.IsRequired.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that consolidated validation patterns work correctly.
    /// </summary>
    [Fact(DisplayName = "Base class validation should work consistently across field types")]
    public void Should_Provide_Consistent_Validation_Across_Field_Types()
    {
        // Required field validation
        var requiredString = new StringField(null, null, true);
        var validationResult = requiredString.Validate();
        validationResult.IsFailure.Should().BeTrue();
        validationResult.Error.Code.Should().Be("VALIDATION_ERROR");

        // Max length validation for StringField
        var longString = new StringField("ThisIsAVeryLongString", 5, false);
        var lengthResult = longString.Validate();
        lengthResult.IsFailure.Should().BeTrue();

        // Valid field passes validation
        var validString = new StringField("Valid", 10, false);
        var validResult = validString.Validate();
        validResult.IsSuccess.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that Result<T> patterns work correctly in field parsing.
    /// </summary>
    [Fact(DisplayName = "Field parsing should use Result<T> pattern consistently")]
    public void Should_Use_Result_Pattern_For_Field_Parsing()
    {
        // Valid date parsing
        var dateField = new DateField();
        var validDateResult = dateField.SetValue("20250902");
        validDateResult.IsSuccess.Should().BeTrue();
        dateField.Value.Should().Be(new DateTime(2025, 9, 2));

        // Invalid date parsing
        var invalidDateResult = dateField.SetValue("invalid");
        invalidDateResult.IsFailure.Should().BeTrue();

        // Valid numeric parsing
        var numericField = new NumericField();
        var validNumericResult = numericField.SetValue("123.45");
        validNumericResult.IsSuccess.Should().BeTrue();
        numericField.Value.Should().Be(123.45m);

        // Invalid numeric parsing
        var invalidNumericResult = numericField.SetValue("not-a-number");
        invalidNumericResult.IsFailure.Should().BeTrue();
    }
}