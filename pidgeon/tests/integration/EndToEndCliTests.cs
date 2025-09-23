using FluentAssertions;
using System.Diagnostics;
using Xunit;

namespace Pidgeon.Integration.Tests;

/// <summary>
/// End-to-end CLI tests that validate complete user workflows.
/// These tests execute the actual CLI and verify real output.
/// </summary>
public class EndToEndCliTests : IDisposable
{
    private readonly string _testOutputDir;
    private readonly string _cliPath;
    private readonly string _dotnetCommand;

    public EndToEndCliTests()
    {
        _testOutputDir = Path.Combine(Path.GetTempPath(), "pidgeon_e2e_tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testOutputDir);

        // Get project root (need to go up from test execution directory)
        var projectRoot = FindProjectRoot();
        _cliPath = Path.Combine(projectRoot, "src", "Pidgeon.CLI");

        // Detect dotnet command based on environment
        _dotnetCommand = GetDotnetCommand();
    }

    private static string GetDotnetCommand()
    {
        // Check if we're in WSL by looking for the Windows dotnet installation
        var wslDotnetPath = "/mnt/c/Program Files/dotnet/dotnet.exe";
        if (File.Exists(wslDotnetPath))
        {
            return wslDotnetPath;
        }

        // Default to system dotnet for Mac/Windows/Linux
        return "dotnet";
    }

    private static string FindProjectRoot()
    {
        // Start from current directory and walk up until we find the project root
        var current = Directory.GetCurrentDirectory();
        while (current != null)
        {
            // Look for indicators of project root (src folder + solution file)
            if (Directory.Exists(Path.Combine(current, "src")) &&
                (File.Exists(Path.Combine(current, "pidgeon.sln")) ||
                 Directory.Exists(Path.Combine(current, "src", "Pidgeon.CLI"))))
            {
                return current;
            }

            var parent = Directory.GetParent(current);
            current = parent?.FullName;
        }

        throw new InvalidOperationException("Could not find project root containing src/Pidgeon.CLI");
    }

    [Fact]
    public async Task Generate_BasicAdtMessage_ProducesValidHL7()
    {
        // Arrange
        var outputFile = Path.Combine(_testOutputDir, "test_adt.hl7");

        // Act
        var result = await RunCliCommand($"generate \"ADT^A01\" --output \"{outputFile}\" --count 1");

        // Assert
        result.ExitCode.Should().Be(0, "CLI should succeed");
        File.Exists(outputFile).Should().BeTrue("Output file should be created");

        var content = await File.ReadAllTextAsync(outputFile);
        content.Should().StartWith("MSH|", "Should generate valid HL7 message");
        content.Should().Contain("ADT^A01", "Should contain correct message type");
        content.Should().Contain("PID|", "Should contain patient identification segment");
    }

    [Fact]
    public async Task Validate_GeneratedMessage_PassesValidation()
    {
        // Arrange
        var messageFile = Path.Combine(_testOutputDir, "validate_test.hl7");
        await RunCliCommand($"generate \"ADT^A01\" --output \"{messageFile}\" --count 1");

        // Act
        var result = await RunCliCommand($"validate --file \"{messageFile}\"");

        // Assert
        result.ExitCode.Should().Be(0, "Validation should pass for generated message");
        result.Output.Should().Contain("Validation passed", "Should report successful validation");
    }

    [Fact]
    public async Task Path_ListCommand_ShowsAvailablePaths()
    {
        // Act
        var result = await RunCliCommand("path list");

        // Assert
        result.ExitCode.Should().Be(0, "Path list should succeed");
        result.Output.Should().Contain("patient.mrn", "Should show semantic paths");
        result.Output.Should().Contain("Universal Semantic Paths", "Should show header");
    }

    [Fact]
    public async Task Path_ResolveCommand_ShowsFieldMapping()
    {
        // Act
        var result = await RunCliCommand("path resolve patient.mrn \"ADT^A01\"");

        // Assert
        result.ExitCode.Should().Be(0, "Path resolve should succeed");
        result.Output.Should().Contain("PID.3", "Should resolve to correct HL7 field");
    }

    [Fact]
    public async Task Session_WorkflowIntegration_MaintainsPatientContext()
    {
        // Arrange
        var sessionName = $"test_session_{Guid.NewGuid():N}";
        var adtFile = Path.Combine(_testOutputDir, "session_adt.hl7");
        var oruFile = Path.Combine(_testOutputDir, "session_oru.hl7");

        try
        {
            // Act - Create session and set patient MRN
            var createResult = await RunCliCommand($"session create {sessionName}");
            createResult.ExitCode.Should().Be(0, "Session create should succeed");

            var setResult = await RunCliCommand($"set patient.mrn \"TEST123456\"");
            setResult.ExitCode.Should().Be(0, "Set command should succeed");

            // Generate ADT message using session context
            var adtResult = await RunCliCommand($"generate \"ADT^A01\" --output \"{adtFile}\"");
            adtResult.ExitCode.Should().Be(0, "ADT generation should succeed");

            // Generate ORU message using same session context
            var oruResult = await RunCliCommand($"generate \"ORU^R01\" --output \"{oruFile}\"");
            oruResult.ExitCode.Should().Be(0, "ORU generation should succeed");

            // Assert - Both messages should have same patient MRN
            var adtContent = await File.ReadAllTextAsync(adtFile);
            var oruContent = await File.ReadAllTextAsync(oruFile);

            adtContent.Should().Contain("TEST123456", "ADT should contain set MRN");
            oruContent.Should().Contain("TEST123456", "ORU should contain same MRN");
        }
        finally
        {
            // Cleanup session
            await RunCliCommand($"session remove {sessionName}");
        }
    }

    [Fact]
    public async Task DeIdentification_BasicMessage_RemovesPhiSafely()
    {
        // Arrange
        var originalFile = Path.Combine(_testOutputDir, "original_phi.hl7");
        var deidentFile = Path.Combine(_testOutputDir, "deident_phi.hl7");

        await RunCliCommand($"generate \"ADT^A01\" --output \"{originalFile}\" --count 1");

        // Act
        var result = await RunCliCommand($"deident --in \"{originalFile}\" --out \"{deidentFile}\"");

        // Assert
        result.ExitCode.Should().Be(0, "De-identification should succeed");
        File.Exists(deidentFile).Should().BeTrue("De-identified file should be created");

        var originalContent = await File.ReadAllTextAsync(originalFile);
        var deidentContent = await File.ReadAllTextAsync(deidentFile);

        deidentContent.Should().NotBeEquivalentTo(originalContent, "Content should be modified");
        deidentContent.Should().StartWith("MSH|", "Should still be valid HL7");
    }

    [Fact]
    public async Task Professional_Features_ShowUpgradePrompt()
    {
        // Act
        var result = await RunCliCommand("workflow wizard");

        // Assert
        result.ExitCode.Should().Be(1, "Should fail without Pro subscription");
        result.Output.Should().Contain("Professional", "Should mention Professional tier");
        result.Output.Should().Contain("upgrade", "Should suggest upgrade");
    }

    [Fact]
    public async Task Help_System_IsComprehensive()
    {
        // Act
        var helpResult = await RunCliCommand("--help");
        var generateHelpResult = await RunCliCommand("generate --help");

        // Assert
        helpResult.ExitCode.Should().Be(0, "Help should succeed");
        helpResult.Output.Should().Contain("generate", "Should list generate command");
        helpResult.Output.Should().Contain("validate", "Should list validate command");
        helpResult.Output.Should().Contain("deident", "Should list deident command");

        generateHelpResult.ExitCode.Should().Be(0, "Generate help should succeed");
        generateHelpResult.Output.Should().Contain("--output", "Should show output option");
        generateHelpResult.Output.Should().Contain("--count", "Should show count option");
    }

    private async Task<CliResult> RunCliCommand(string arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = _dotnetCommand,
            Arguments = $"run --project \"{_cliPath}\" -- {arguments}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = FindProjectRoot()  // Run from pidgeon root
        };

        using var process = new Process { StartInfo = startInfo };
        process.Start();

        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        var output = await outputTask;
        var error = await errorTask;

        return new CliResult
        {
            ExitCode = process.ExitCode,
            Output = output,
            Error = error
        };
    }

    public void Dispose()
    {
        if (Directory.Exists(_testOutputDir))
        {
            Directory.Delete(_testOutputDir, recursive: true);
        }
    }

    private record CliResult
    {
        public int ExitCode { get; init; }
        public string Output { get; init; } = string.Empty;
        public string Error { get; init; } = string.Empty;
    }
}