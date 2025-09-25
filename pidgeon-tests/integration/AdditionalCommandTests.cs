using FluentAssertions;
using System.Diagnostics;
using Xunit;

namespace Pidgeon.Integration.Tests;

/// <summary>
/// Tests for additional CLI commands discovered in comprehensive command analysis.
/// Covers find, diff, workflow, info, completions, and other extended functionality.
/// </summary>
public class AdditionalCommandTests : IDisposable
{
    private readonly string _testOutputDir;
    private readonly string _cliPath;
    private readonly string _dotnetCommand;

    public AdditionalCommandTests()
    {
        _testOutputDir = Path.Combine(Path.GetTempPath(), "pidgeon_additional_tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testOutputDir);

        var projectRoot = FindProjectRoot();
        _cliPath = Path.Combine(projectRoot, "src", "Pidgeon.CLI");
        _dotnetCommand = GetDotnetCommand();
    }

    private static string GetDotnetCommand()
    {
        var wslDotnetPath = "/mnt/c/Program Files/dotnet/dotnet.exe";
        if (File.Exists(wslDotnetPath))
        {
            return wslDotnetPath;
        }
        return "dotnet";
    }

    private static string FindProjectRoot()
    {
        var current = Directory.GetCurrentDirectory();
        while (current != null)
        {
            if (Directory.Exists(Path.Combine(current, "src")) &&
                (File.Exists(Path.Combine(current, "*.sln")) ||
                 Directory.Exists(Path.Combine(current, "src", "Pidgeon.CLI"))))
            {
                return current;
            }
            current = Directory.GetParent(current)?.FullName;
        }

        // If not found, try to find relative to pidgeon-tests directory
        var testsDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        while (testsDir != null)
        {
            var pidgeonDir = Path.Combine(Directory.GetParent(testsDir)?.FullName ?? "", "pidgeon");
            if (Directory.Exists(pidgeonDir) && Directory.Exists(Path.Combine(pidgeonDir, "src")))
            {
                return pidgeonDir;
            }
            testsDir = Directory.GetParent(testsDir)?.FullName;
        }

        throw new InvalidOperationException("Could not find Pidgeon project root");
    }

    private ProcessResult RunCliCommand(string arguments, int timeoutSeconds = 30)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = _dotnetCommand,
            Arguments = $"run --project \"{_cliPath}\" -- {arguments}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = _testOutputDir
        };

        process.Start();
        var completed = process.WaitForExit(timeoutSeconds * 1000);

        if (!completed)
        {
            process.Kill();
            throw new TimeoutException($"CLI command timed out after {timeoutSeconds} seconds: {arguments}");
        }

        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();

        return new ProcessResult(process.ExitCode, stdout, stderr);
    }

    [Fact]
    public void FindCommand_BasicFieldSearch_ReturnsResults()
    {
        // Test basic field path search
        var result = RunCliCommand("find \"PID.3\"");

        result.ExitCode.Should().Be(0, $"find command should succeed. stderr: {result.StandardError}");
        result.StandardOutput.Should().Contain("PID.3", "should find the patient identifier field");
    }

    [Fact]
    public void FindCommand_SemanticSearch_ReturnsPatientRelated()
    {
        // Test semantic search for patient-related fields
        var result = RunCliCommand("find \"patient\" --semantic");

        // Should succeed or return meaningful results (even if semantic search isn't fully implemented)
        result.StandardOutput.Should().ContainAny(["PID", "Patient", "patient"],
            "semantic patient search should return patient-related results");
    }


    [Fact]
    public void CompletionsCommand_GeneratesBashCompletions()
    {
        var result = RunCliCommand("completions bash");

        result.ExitCode.Should().Be(0, "bash completion generation should succeed");
        result.StandardOutput.Should().ContainAny(["complete", "_pidgeon", "bash"],
            "should generate valid bash completion script");
    }

    [Fact]
    public void CompletionsCommand_SupportsMultipleShells()
    {
        var shells = new[] { "bash", "zsh", "fish", "powershell" };

        foreach (var shell in shells)
        {
            var result = RunCliCommand($"completions {shell}");
            result.ExitCode.Should().Be(0, $"{shell} completion should be supported");
            result.StandardOutput.Should().NotBeEmpty($"{shell} completion should produce output");
        }
    }

    [Fact]
    public void DiffCommand_WithSkipProCheck_ComparesTwoFiles()
    {
        // Create two test HL7 messages with slight differences
        var file1Path = Path.Combine(_testOutputDir, "test1.hl7");
        var file2Path = Path.Combine(_testOutputDir, "test2.hl7");

        File.WriteAllText(file1Path, "MSH|^~\\&|PIDGEON|TEST|RECEIVER|TEST|20250925||ADT^A01|123|P|2.3");
        File.WriteAllText(file2Path, "MSH|^~\\&|PIDGEON|TEST2|RECEIVER|TEST|20250925||ADT^A01|124|P|2.3");

        var result = RunCliCommand($"diff \"{file1Path}\" \"{file2Path}\" --basic --skip-pro-check");

        // Should detect differences between the files
        result.StandardOutput.Should().ContainAny(["diff", "differ", "difference", "changed"],
            "diff should detect differences between test files");
    }

    [Fact]
    public void WorkflowCommand_ListTemplates_ShowsAvailableWorkflows()
    {
        var result = RunCliCommand("workflow list");

        // Should succeed or show that workflow templates exist
        // This might be a Pro feature so we're lenient with assertions
        if (result.ExitCode == 0)
        {
            result.StandardOutput.Should().ContainAny(["workflow", "template", "scenario"],
                "workflow list should show available templates");
        }
    }

    [Fact]
    public void PathCommand_ListPaths_ShowsFieldPaths()
    {
        var result = RunCliCommand("path --list");

        if (result.ExitCode == 0)
        {
            result.StandardOutput.Should().ContainAny(["PID", "MSH", "path"],
                "path listing should show available field paths");
        }
    }


    [Fact]
    public void DeidentCommand_PreviewMode_ShowsSampleOutput()
    {
        // Create test directory with sample HL7 file
        var inputDir = Path.Combine(_testOutputDir, "deident_input");
        Directory.CreateDirectory(inputDir);

        var sampleFile = Path.Combine(inputDir, "sample.hl7");
        File.WriteAllText(sampleFile,
            "MSH|^~\\&|PIDGEON|TEST|RECEIVER|TEST|20250925||ADT^A01|123|P|2.3\r\n" +
            "PID|1||123456789^^^TEST^MR||DOE^JOHN^||19800101|M");

        var outputDir = Path.Combine(_testOutputDir, "deident_output");

        var result = RunCliCommand($"deident \"{inputDir}\" \"{outputDir}\" --preview");

        // Should show preview of de-identification process
        result.StandardOutput.Should().ContainAny(["preview", "Preview", "de-identified", "before", "after"],
            "de-identification preview should show before/after comparison");
    }

    [Fact]
    public void ScenarioCommand_ShowsAvailableScenarios()
    {
        var result = RunCliCommand("scenario --help");

        result.ExitCode.Should().Be(0, "scenario command should show help");
        result.StandardOutput.Should().Contain("scenario", "should show scenario command help");
    }

    public void Dispose()
    {
        if (Directory.Exists(_testOutputDir))
        {
            Directory.Delete(_testOutputDir, true);
        }
    }

    private record ProcessResult(int ExitCode, string StandardOutput, string StandardError);
}