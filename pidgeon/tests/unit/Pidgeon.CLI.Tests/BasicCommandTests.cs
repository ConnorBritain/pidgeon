using FluentAssertions;
using System.Reflection;
using Xunit;

namespace Pidgeon.CLI.Tests;

/// <summary>
/// Minimal smoke tests for CLI assembly.
/// Ensures basic project structure works.
/// </summary>
public class BasicCliTests
{
    [Fact]
    public void CliAssembly_CanBeLoaded()
    {
        // Arrange & Act
        var assembly = Assembly.GetAssembly(typeof(BasicCliTests));

        // Assert
        assembly.Should().NotBeNull();
        assembly.FullName.Should().StartWith("Pidgeon.CLI.Tests");
    }

    [Fact]
    public void CliTestAssembly_CanReferenceCore()
    {
        // Arrange & Act
        var result = Pidgeon.Core.Result<string>.Success("test");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("test");
    }
}