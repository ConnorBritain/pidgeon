using FluentAssertions;
using Xunit;

namespace Pidgeon.Core.Tests;

/// <summary>
/// Minimal smoke tests for Core assembly.
/// Ensures basic project structure and dependencies work.
/// </summary>
public class BasicCoreTests
{
    [Fact]
    public void CoreAssembly_CanBeLoaded()
    {
        // Arrange & Act
        var assembly = typeof(Pidgeon.Core.Result<>).Assembly;

        // Assert
        assembly.Should().NotBeNull();
        assembly.FullName.Should().StartWith("Pidgeon.Core");
    }

    [Theory]
    [InlineData("Success")]
    [InlineData("")]
    public void Result_Success_CanBeCreated(string value)
    {
        // Arrange & Act
        var result = Pidgeon.Core.Result<string>.Success(value);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(value);
    }

    [Fact]
    public void Result_Failure_CanBeCreated()
    {
        // Arrange & Act
        var result = Pidgeon.Core.Result<string>.Failure("Test error");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
    }
}