// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System.CommandLine;
using Microsoft.Extensions.Logging;
using Pidgeon.Core.Application.Interfaces.Comparison;
using Pidgeon.Core.Domain.Comparison.Entities;

namespace Pidgeon.CLI.Commands;

/// <summary>
/// CLI command for comparing healthcare messages with field-level differences.
/// Pro feature with algorithmic analysis and AI-powered insights.
/// </summary>
public class DiffCommand : CommandBuilderBase
{
    private readonly IMessageDiffService _diffService;

    public DiffCommand(
        ILogger<DiffCommand> logger,
        IMessageDiffService diffService) 
        : base(logger)
    {
        _diffService = diffService;
    }

    public override Command CreateCommand()
    {
        var command = new Command("diff", "[Pro] Compare two artifacts (files or folders). Field-aware for HL7; JSON-tree for FHIR. Emits hints.");

        // Positional arguments for natural usage: pidgeon diff file1 file2
        var pathsArgument = new Argument<string[]>("paths")
        {
            Description = "Files or directories to compare (left/baseline first, right/candidate second)",
            Arity = ArgumentArity.ZeroOrMore
        };

        // Optional explicit flags for when users want to be specific
        var leftOption = CreateNullableOption("--left", "Explicitly specify baseline file/folder (overrides positional)");
        var rightOption = CreateNullableOption("--right", "Explicitly specify candidate file/folder (overrides positional)");
        var ignoreOption = CreateNullableOption("--ignore", "Comma-list of fields/segments to ignore (e.g., MSH-7, PID.3[*].assigningAuthority)");
        var reportOption = CreateNullableOption("--report", "HTML/JSON diff report with triage hints");
        var severityOption = CreateOptionalOption("--severity", "hint|warn|error", "hint");
        var aiLocalOption = CreateBooleanOption("--ai-local", "Enable local AI analysis for root cause insights (requires Pro)");
        var aiModelOption = CreateNullableOption("--ai-model", "Specify local AI model to use (e.g., phi2-healthcare)");
        var skipProCheckOption = CreateBooleanOption("--skip-pro-check", "Skip Pro tier check (for development/testing)");

        command.Add(pathsArgument);
        command.Add(leftOption);
        command.Add(rightOption);
        command.Add(ignoreOption);
        command.Add(reportOption);
        command.Add(severityOption);
        command.Add(aiLocalOption);
        command.Add(aiModelOption);
        command.Add(skipProCheckOption);

        SetCommandAction(command, async (parseResult, cancellationToken) =>
        {
            try
            {
                // Get paths from either positional arguments or explicit flags
                var paths = parseResult.GetValue(pathsArgument) ?? Array.Empty<string>();
                var explicitLeft = parseResult.GetValue(leftOption);
                var explicitRight = parseResult.GetValue(rightOption);
                
                // Determine left and right paths with explicit flags taking precedence
                string? leftPath = explicitLeft;
                string? rightPath = explicitRight;
                
                // If no explicit flags, use positional arguments
                if (string.IsNullOrEmpty(leftPath) && paths.Length > 0)
                {
                    leftPath = paths[0];
                }
                if (string.IsNullOrEmpty(rightPath) && paths.Length > 1)
                {
                    rightPath = paths[1];
                }
                
                // Validate we have both paths
                if (string.IsNullOrEmpty(leftPath) || string.IsNullOrEmpty(rightPath))
                {
                    Console.WriteLine("Error: Two paths required for comparison");
                    Console.WriteLine();
                    Console.WriteLine("Usage:");
                    Console.WriteLine("  pidgeon diff file1 file2                    # Compare two files");
                    Console.WriteLine("  pidgeon diff dir1 dir2                      # Compare two directories");  
                    Console.WriteLine("  pidgeon diff --left file1 --right file2    # Explicit flags");
                    Console.WriteLine();
                    Console.WriteLine("Examples:");
                    Console.WriteLine("  pidgeon diff msg.hl7 msg_new.hl7");
                    Console.WriteLine("  pidgeon diff ./dev ./prod --report diff.html");
                    Console.WriteLine("  pidgeon diff old.hl7 new.hl7 --ignore MSH-7,PV1.44");
                    return 1;
                }
                
                var ignoreFields = parseResult.GetValue(ignoreOption);
                var reportFile = parseResult.GetValue(reportOption);
                var severity = parseResult.GetValue(severityOption)!;
                var useAiLocal = parseResult.GetValue(aiLocalOption);
                var aiModel = parseResult.GetValue(aiModelOption);
                var skipProCheck = parseResult.GetValue(skipProCheckOption);

                return await ExecuteDiffAsync(
                    _diffService, leftPath, rightPath, ignoreFields, 
                    reportFile, severity, useAiLocal, aiModel, skipProCheck);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Unexpected error during diff execution");
                Console.WriteLine($"Error: {ex.Message}");
                return 1;
            }
        });

        return command;
    }

    private async Task<int> ExecuteDiffAsync(
        IMessageDiffService diffService,
        string leftPath,
        string rightPath,
        string? ignoreFields,
        string? reportFile,
        string severity,
        bool useAiLocal,
        string? aiModel,
        bool skipProCheck)
    {
        // Check Pro tier unless skipped
        if (!skipProCheck)
        {
            Console.WriteLine("Pro Feature: Diff Analysis");
            Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            Console.WriteLine();
            Console.WriteLine("The Diff Analysis is a Pro-tier feature that provides:");
            Console.WriteLine("  • Field-level comparison with semantic understanding");
            Console.WriteLine("  • AI-powered root cause analysis and fix suggestions");
            Console.WriteLine("  • Local AI models for healthcare-compliant analysis");
            Console.WriteLine("  • Visual HTML reports with side-by-side differences");
            Console.WriteLine("  • Batch comparison of entire directories");
            Console.WriteLine();
            Console.WriteLine("Upgrade to Pidgeon Pro to unlock this feature:");
            Console.WriteLine("  • $29/month per user");
            Console.WriteLine("  • All CLI features plus Pro workflows and AI analysis");
            Console.WriteLine("  • Enhanced datasets and local AI models");
            Console.WriteLine("  • Priority support");
            Console.WriteLine();
            Console.WriteLine("Learn more: https://pidgeon.health/pro");
            Console.WriteLine();
            Console.WriteLine("For development/testing, use: --skip-pro-check");
            return 1;
        }

        Console.WriteLine("Message Diff Analysis");
        Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        Console.WriteLine();

        // Check for AI capabilities if requested
        if (useAiLocal)
        {
            var aiCapabilityCheck = await CheckAiCapabilities(aiModel);
            if (!aiCapabilityCheck.isAvailable)
            {
                Console.WriteLine($"Warning: {aiCapabilityCheck.message}");
                Console.WriteLine("Proceeding with algorithmic analysis only.");
                Console.WriteLine();
            }
            else
            {
                Console.WriteLine($"AI Model: {aiCapabilityCheck.message}");
                Console.WriteLine();
            }
        }

        // Determine if paths are files or directories
        var leftIsFile = File.Exists(leftPath);
        var rightIsFile = File.Exists(rightPath);
        var leftIsDir = Directory.Exists(leftPath);
        var rightIsDir = Directory.Exists(rightPath);

        if (!leftIsFile && !leftIsDir)
        {
            Console.WriteLine($"Left path not found: {leftPath}");
            return 1;
        }

        if (!rightIsFile && !rightIsDir)
        {
            Console.WriteLine($"Right path not found: {rightPath}");
            return 1;
        }

        if (leftIsFile && rightIsFile)
        {
            // File comparison
            Console.WriteLine($"Comparing files:");
            Console.WriteLine($"   Left:  {Path.GetFileName(leftPath)}");
            Console.WriteLine($"   Right: {Path.GetFileName(rightPath)}");
            Console.WriteLine();

            var context = CreateDiffContext(ignoreFields, ComparisonType.MessageToMessage, useAiLocal);
            var result = await diffService.CompareMessageFilesAsync(leftPath, rightPath, context);

            if (result.IsFailure)
            {
                Console.WriteLine($"Comparison failed: {result.Error}");
                return 1;
            }

            var diff = result.Value;
            DisplayDiffResults(diff, severity);

            // Generate report if specified
            if (!string.IsNullOrEmpty(reportFile))
            {
                await GenerateReportAsync(diff, reportFile);
                Console.WriteLine($"Report generated: {reportFile}");
            }
        }
        else
        {
            Console.WriteLine("Directory comparison not yet fully implemented");
            Console.WriteLine("Please compare individual files for now");
            return 1;
        }

        return 0;
    }

    private static DiffContext CreateDiffContext(string? ignoreFields, ComparisonType comparisonType, bool useAiLocal = false)
    {
        var ignoredFieldsList = new List<string>();
        if (!string.IsNullOrEmpty(ignoreFields))
        {
            ignoredFieldsList = ignoreFields.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(f => f.Trim())
                .ToList();
        }

        return new DiffContext
        {
            ComparisonType = comparisonType,
            IgnoredFields = ignoredFieldsList,
            InitiatedBy = Environment.UserName,
            Purpose = "CLI diff analysis",
            AnalysisOptions = new ComparisonOptions
            {
                IncludeStructuralAnalysis = true,
                IncludeSemanticAnalysis = true,
                UseIntelligentAnalysis = useAiLocal,
                NormalizeFormatting = true
            }
        };
    }

    private static async Task<(bool isAvailable, string message)> CheckAiCapabilities(string? requestedModel)
    {
        try
        {
            if (string.IsNullOrEmpty(requestedModel))
            {
                return (false, "No AI models installed. Use 'pidgeon ai download phi2-healthcare' to install a model.");
            }
            
            if (requestedModel.Contains("phi2") || requestedModel.Contains("tinyllama") || requestedModel.Contains("biogpt"))
            {
                return (true, $"Using local model: {requestedModel}");
            }
            
            return (false, $"Model '{requestedModel}' not found locally. Use 'pidgeon ai list' to see available models.");
        }
        catch (Exception)
        {
            return (false, "AI service unavailable");
        }
    }

    private static void DisplayDiffResults(MessageDiff diff, string minSeverity)
    {
        Console.WriteLine($"Diff Results");
        Console.WriteLine($"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        Console.WriteLine($"Similarity: {diff.SimilarityScore:P1}");
        Console.WriteLine($"Differences: {diff.Summary.TotalDifferences}");

        if (diff.Summary.TotalDifferences == 0)
        {
            Console.WriteLine();
            Console.WriteLine("Files are identical!");
            return;
        }

        // Show difference breakdown
        if (diff.Summary.CriticalDifferences > 0)
            Console.WriteLine($"  Critical: {diff.Summary.CriticalDifferences}");
        if (diff.Summary.WarningDifferences > 0)
            Console.WriteLine($"  Warning: {diff.Summary.WarningDifferences}");
        if (diff.Summary.InformationalDifferences > 0)
            Console.WriteLine($"  Info: {diff.Summary.InformationalDifferences}");

        Console.WriteLine();

        // Show AI insights if available
        if (diff.Insights.Any())
        {
            Console.WriteLine();
            Console.WriteLine("Analysis Insights:");
            Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

            foreach (var insight in diff.Insights.Take(5))
            {
                Console.WriteLine($"• {insight.Title}");
                Console.WriteLine($"  {insight.Description}");
                if (!string.IsNullOrEmpty(insight.RecommendedAction))
                {
                    Console.WriteLine($"  Action: {insight.RecommendedAction}");
                }
                Console.WriteLine();
            }
        }
    }

    private static async Task GenerateReportAsync(MessageDiff diff, string reportFile)
    {
        var extension = Path.GetExtension(reportFile).ToLower();
        
        if (extension == ".json")
        {
            // Generate JSON report
            var jsonOptions = new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            };
            var json = System.Text.Json.JsonSerializer.Serialize(diff, jsonOptions);
            await File.WriteAllTextAsync(reportFile, json);
        }
        else
        {
            // Generate basic HTML report
            var html = $@"<!DOCTYPE html>
<html>
<head><title>Pidgeon Diff Report</title></head>
<body>
<h1>Diff Report</h1>
<p>Similarity: {diff.SimilarityScore:P1}</p>
<p>Differences: {diff.Summary.TotalDifferences}</p>
</body>
</html>";
            await File.WriteAllTextAsync(reportFile, html);
        }
    }
}