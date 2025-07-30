using FluentAssertions;
using Segmint.Core.Standards.HL7.v23.Types;
using System;
using Xunit;

namespace Segmint.Tests.HL7.Types
{
    public class DateFieldTests
    {
        [Fact]
        public void DateField_ValidFullDate_ShouldNotThrow()
        {
            // Arrange
            var date = "20000412"; // April 12, 2000
            
            // Act & Assert
            var dateField = new DateField(date);
            var dateTime = dateField.ToDateTime();
            
            dateTime.Should().NotBeNull();
            dateTime.Value.Year.Should().Be(2000);
            dateTime.Value.Month.Should().Be(4);
            dateTime.Value.Day.Should().Be(12);
        }

        [Fact]
        public void DateField_ValidYearOnly_ShouldNotThrow()
        {
            // Arrange
            var date = "2000";
            
            // Act & Assert
            var dateField = new DateField(date);
            var dateTime = dateField.ToDateTime();
            
            dateTime.Should().NotBeNull();
            dateTime.Value.Year.Should().Be(2000);
            dateTime.Value.Month.Should().Be(1);
            dateTime.Value.Day.Should().Be(1);
        }

        [Fact]
        public void DateField_ValidYearMonth_ShouldNotThrow()
        {
            // Arrange
            var date = "200004"; // April 2000
            
            // Act & Assert
            var dateField = new DateField(date);
            var dateTime = dateField.ToDateTime();
            
            dateTime.Should().NotBeNull();
            dateTime.Value.Year.Should().Be(2000);
            dateTime.Value.Month.Should().Be(4);
            dateTime.Value.Day.Should().Be(1);
        }

        [Fact]
        public void DateField_InvalidMonth_ShouldReturnNull()
        {
            // Arrange
            var date = "20001300"; // Month 13 doesn't exist
            
            // Act & Assert
            var dateField = new DateField(date);
            var dateTime = dateField.ToDateTime();
            
            dateTime.Should().BeNull();
        }

        [Fact]
        public void DateField_InvalidDay_ShouldReturnNull()
        {
            // Arrange
            var date = "20000232"; // February 32 doesn't exist
            
            // Act & Assert
            var dateField = new DateField(date);
            var dateTime = dateField.ToDateTime();
            
            dateTime.Should().BeNull();
        }

        [Fact]
        public void DateField_InvalidFormat_ShouldThrow()
        {
            // Arrange
            var date = "12345"; // Invalid length
            
            // Act & Assert
            var action = () => new DateField(date);
            action.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void DateField_EmptyValue_ShouldNotThrow()
        {
            // Arrange
            var date = "";
            
            // Act & Assert
            var dateField = new DateField(date);
            var dateTime = dateField.ToDateTime();
            
            dateTime.Should().BeNull();
        }

        [Fact]
        public void DateField_LeapYear_ShouldWork()
        {
            // Arrange
            var date = "20000229"; // February 29, 2000 (leap year)
            
            // Act & Assert
            var dateField = new DateField(date);
            var dateTime = dateField.ToDateTime();
            
            dateTime.Should().NotBeNull();
            dateTime.Value.Year.Should().Be(2000);
            dateTime.Value.Month.Should().Be(2);
            dateTime.Value.Day.Should().Be(29);
        }

        [Fact]
        public void DateField_NonLeapYear_ShouldFail()
        {
            // Arrange
            var date = "19990229"; // February 29, 1999 (not a leap year)
            
            // Act & Assert
            var dateField = new DateField(date);
            var dateTime = dateField.ToDateTime();
            
            dateTime.Should().BeNull();
        }
    }
}