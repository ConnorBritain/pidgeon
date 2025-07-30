// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System;
using FluentAssertions;
using Segmint.Core.Standards.HL7.v23.Segments;
using Segmint.Core.Standards.HL7.v23.Types;
using Xunit;

namespace Segmint.Tests.HL7.Segments;

/// <summary>
/// Tests for IN1Segment (Insurance) functionality.
/// </summary>
public class IN1SegmentTests
{
    [Fact]
    public void Constructor_InitializesSegmentCorrectly()
    {
        // Arrange & Act
        var segment = new IN1Segment();

        // Assert
        segment.SegmentId.Should().Be("IN1");
        segment.SetId.Value.Should().Be("1"); // Default from InitializeFields
    }

    [Fact]
    public void SetBasicInsurance_SetsRequiredFields()
    {
        // Arrange
        var segment = new IN1Segment();

        // Act
        segment.SetBasicInsurance(
            setId: 1,
            planId: "BCBS001",
            planName: "Blue Cross Blue Shield",
            companyId: "BCBS",
            companyName: "Blue Cross Blue Shield Corp",
            memberId: "123456789",
            groupNumber: "EMP001");

        // Assert
        segment.SetId.Value.Should().Be("1");
        segment.InsurancePlanId.Identifier.Should().Be("BCBS001");
        segment.InsurancePlanId.Text.Should().Be("Blue Cross Blue Shield");
        segment.InsuranceCompanyId.IdNumber.Should().Be("BCBS");
        segment.InsuranceCompanyName.IdNumber.Should().Be("Blue Cross Blue Shield Corp");
        segment.InsuredsIdNumber.IdNumber.Should().Be("123456789");
        segment.GroupNumber.Value.Should().Be("EMP001");
    }

    [Fact]
    public void SetInsuredPersonInfo_SetsPersonFields()
    {
        // Arrange
        var segment = new IN1Segment();
        var insuredName = new PersonNameField("Doe^John^M");

        // Act
        segment.SetInsuredPersonInfo(
            insuredName: insuredName,
            relationshipToPatient: "SEL",
            insuredDateOfBirth: new DateTime(1980, 5, 15),
            insuredGender: "M");

        // Assert
        segment.NameOfInsured.FamilyName.Should().Be("Doe");
        segment.NameOfInsured.GivenName.Should().Be("John");
        segment.InsuredsRelationshipToPatient.Identifier.Should().Be("SEL");
        segment.InsuredsRelationshipToPatient.Text.Should().Be("Self");
        segment.InsuredsDateOfBirth.Value.Should().Be(new DateTime(1980, 5, 15));
        segment.InsuredsAdministrativeSex.Identifier.Should().Be("M");
        segment.InsuredsAdministrativeSex.Text.Should().Be("Male");
    }

    [Fact]
    public void SetCoverageDetails_SetsFinancialFields()
    {
        // Arrange
        var segment = new IN1Segment();

        // Act
        segment.SetCoverageDetails(
            planType: "HMO",
            deductibleAmount: 500.00m,
            policyLimitAmount: 1000000.00m,
            policyLimitDays: 365);

        // Assert
        segment.PlanType.Identifier.Should().Be("HMO");
        segment.PlanType.Text.Should().Be("Health Maintenance Organization");
        segment.PolicyDeductible.Quantity.Should().Be("500.00");
        segment.PolicyDeductible.Units.Should().Be("USD");
        segment.PolicyLimitAmount.Quantity.Should().Be("1000000.00");
        segment.PolicyLimitAmount.Units.Should().Be("USD");
        segment.PolicyLimitDays.ToInt().Should().Be(365);
    }

    [Fact]
    public void IsPrimary_WithSetId1_ReturnsTrue()
    {
        // Arrange
        var segment = new IN1Segment();
        segment.SetId.SetValue("1");

        // Act & Assert
        segment.IsPrimary().Should().BeTrue();
    }

    [Fact]
    public void IsPrimary_WithSetId2_ReturnsFalse()
    {
        // Arrange
        var segment = new IN1Segment();
        segment.SetId.SetValue("2");

        // Act & Assert
        segment.IsPrimary().Should().BeFalse();
    }

    [Fact]
    public void GetPriority_ReturnsCorrectPriority()
    {
        // Arrange
        var segment = new IN1Segment();

        // Act & Assert
        segment.SetId.SetValue("1");
        segment.GetPriority().Should().Be(InsurancePriority.Primary);

        segment.SetId.SetValue("2");
        segment.GetPriority().Should().Be(InsurancePriority.Secondary);

        segment.SetId.SetValue("3");
        segment.GetPriority().Should().Be(InsurancePriority.Tertiary);

        segment.SetId.SetValue("4");
        segment.GetPriority().Should().Be(InsurancePriority.Other);
    }

    [Fact]
    public void GetDisplayValue_ReturnsFormattedString()
    {
        // Arrange
        var segment = new IN1Segment();
        segment.SetBasicInsurance(1, "BCBS001", "Blue Cross", "BCBS", "Blue Cross Corp", "123456789");

        // Act
        var displayValue = segment.GetDisplayValue();

        // Assert
        displayValue.Should().Be("Blue Cross Corp - Blue Cross (Member: 123456789)");
    }

    [Fact]
    public void CreateBasicInsurance_StaticMethod_CreatesConfiguredSegment()
    {
        // Arrange & Act
        var segment = IN1Segment.CreateBasicInsurance(
            setId: 1,
            planName: "Aetna HMO",
            companyName: "Aetna Inc",
            memberId: "987654321",
            groupNumber: "GRP001");

        // Assert
        segment.SetId.Value.Should().Be("1");
        segment.InsurancePlanId.Text.Should().Be("Aetna HMO");
        segment.InsuranceCompanyName.IdNumber.Should().Be("Aetna Inc");
        segment.InsuredsIdNumber.IdNumber.Should().Be("987654321");
        segment.GroupNumber.Value.Should().Be("GRP001");
    }

    [Fact]
    public void Validate_WithRequiredFields_ReturnsNoErrors()
    {
        // Arrange
        var segment = new IN1Segment();
        segment.SetBasicInsurance(1, "PLAN001", "Test Plan", "COMP001", "Test Company", "MEM001");

        // Act
        var errors = segment.Validate();

        // Assert
        errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithMissingRequiredFields_ReturnsErrors()
    {
        // Arrange
        var segment = new IN1Segment();
        // Don't set any required fields

        // Act
        var errors = segment.Validate();

        // Assert
        errors.Should().Contain("Insurance Plan ID (IN1.2) is required");
        errors.Should().Contain("Insurance Company ID (IN1.3) is required");
    }

    [Fact]
    public void Validate_WithInvalidDateRange_ReturnsError()
    {
        // Arrange
        var segment = new IN1Segment();
        segment.SetBasicInsurance(1, "PLAN001", "Test Plan", "COMP001", "Test Company", "MEM001");
        segment.PlanEffectiveDate.SetValue("20250601");
        segment.PlanExpirationDate.SetValue("20250101"); // Before effective date

        // Act
        var errors = segment.Validate();

        // Assert
        errors.Should().Contain("Plan expiration date cannot be before effective date");
    }

    [Fact]
    public void Clone_CreatesIndependentCopy()
    {
        // Arrange
        var original = new IN1Segment();
        original.SetBasicInsurance(1, "PLAN001", "Test Plan", "COMP001", "Test Company", "MEM001");

        // Act
        var clone = (IN1Segment)original.Clone();

        // Assert
        clone.Should().NotBeSameAs(original);
        clone.SetId.Value.Should().Be(original.SetId.Value);
        clone.InsurancePlanId.Identifier.Should().Be(original.InsurancePlanId.Identifier);
        clone.InsuranceCompanyId.IdNumber.Should().Be(original.InsuranceCompanyId.IdNumber);
    }

    [Fact]
    public void ToHL7String_GeneratesCorrectFormat()
    {
        // Arrange
        var segment = new IN1Segment();
        segment.SetBasicInsurance(1, "BCBS001", "Blue Cross", "BCBS", "Blue Cross Corp", "123456789");

        // Act
        var hl7String = segment.ToHL7String();

        // Assert
        hl7String.Should().StartWith("IN1|1|BCBS001^Blue Cross|BCBS^Blue Cross Corp|BCBS^Blue Cross Corp|");
        hl7String.Should().Contain("123456789^^MB");
    }
}