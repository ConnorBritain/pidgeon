// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Pidgeon.Core.Application.Interfaces.Comparison;
using Pidgeon.Core.Domain.Comparison.Entities;
using Pidgeon.CLI.Common.Extensions;

namespace Pidgeon.CLI.Commands;

/// <summary>
/// CLI command for comparing healthcare messages with field-level differences.
/// Pro feature with algorithmic analysis and AI-powered insights.
/// </summary>
public static class DiffCommand
{
    public static Command Create()
    {
        // Positional arguments for natural usage: pidgeon diff file1 file2
        var pathsArgument = new Argument<string[]>(
            name: "paths",
            description: "Files or directories to compare (left/baseline first, right/candidate second)")
        {
            Arity = ArgumentArity.ZeroOrMore
        };

        // Optional explicit flags for when users want to be specific
        var leftOption = new Option<string?>(
            aliases: ["--left"],
            description: "Explicitly specify baseline file/folder (overrides positional)")
        {
            IsRequired = false
        };

        var rightOption = new Option<string?>(
            aliases: ["--right"],
            description: "Explicitly specify candidate file/folder (overrides positional)")
        {
            IsRequired = false
        };

        var ignoreOption = new Option<string?>(
            aliases: ["--ignore"],
            description: "Comma-list of fields/segments to ignore (e.g., MSH-7, PID.3[*].assigningAuthority)")
        {
            IsRequired = false
        };

        var reportOption = new Option<string?>(
            aliases: ["--report"],
            description: "HTML/JSON diff report with triage hints")
        {
            IsRequired = false
        };

        var severityOption = new Option<string>(
            aliases: ["--severity"],
            description: "hint|warn|error",
            getDefaultValue: () => "hint")
        {
            IsRequired = false
        };

        var skipProCheckOption = new Option<bool>(
            aliases: ["--skip-pro-check"],
            description: "Skip Pro tier check (for development/testing)",
            getDefaultValue: () => false)
        {
            IsHidden = true // Hide from help text
        };

        var command = new Command("diff", "🔒 [Pro] Compare two artifacts (files or folders). Field-aware for HL7; JSON-tree for FHIR. Emits hints.")
        {
            pathsArgument,
            leftOption,
            rightOption,
            ignoreOption,
            reportOption,
            severityOption,
            skipProCheckOption
        };

        command.SetHandler(async (context) =>
        {
            // Get paths from either positional arguments or explicit flags
            var paths = context.ParseResult.GetValueForArgument(pathsArgument) ?? Array.Empty<string>();
            var explicitLeft = context.ParseResult.GetValueForOption(leftOption);
            var explicitRight = context.ParseResult.GetValueForOption(rightOption);
            
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
                Console.WriteLine("❌ Error: Two paths required for comparison");
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
                context.ExitCode = 1;
                return;
            }
            
            var ignoreFields = context.ParseResult.GetValueForOption(ignoreOption);
            var reportFile = context.ParseResult.GetValueForOption(reportOption);
            var severity = context.ParseResult.GetValueForOption(severityOption)!;
            var skipProCheck = context.ParseResult.GetValueForOption(skipProCheckOption);

            var serviceProvider = context.GetServiceProvider();
            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
            var diffService = serviceProvider.GetRequiredService<IMessageDiffService>();

            await ExecuteDiffAsync(
                diffService, logger, leftPath, rightPath, ignoreFields, 
                reportFile, severity, skipProCheck);
        });

        return command;
    }

    private static async Task ExecuteDiffAsync(
        IMessageDiffService diffService,
        ILogger logger,
        string leftPath,
        string rightPath,
        string? ignoreFields,
        string? reportFile,
        string severity,
        bool skipProCheck)
    {
        try
        {
            // Check Pro tier unless skipped
            if (!skipProCheck)
            {
                Console.WriteLine("🔒 Pro Feature: Diff Analysis");
                Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                Console.WriteLine();
                Console.WriteLine("The Diff Analysis is a Pro-tier feature that provides:");
                Console.WriteLine("  • Field-level comparison with semantic understanding");
                Console.WriteLine("  • AI-powered root cause analysis and fix suggestions");
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
                return;
            }

            Console.WriteLine("📊 Message Diff Analysis");
            Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            Console.WriteLine();

            // Determine if paths are files or directories
            var leftIsFile = File.Exists(leftPath);
            var rightIsFile = File.Exists(rightPath);
            var leftIsDir = Directory.Exists(leftPath);
            var rightIsDir = Directory.Exists(rightPath);

            if (!leftIsFile && !leftIsDir)
            {
                Console.WriteLine($"❌ Left path not found: {leftPath}");
                return;
            }

            if (!rightIsFile && !rightIsDir)
            {
                Console.WriteLine($"❌ Right path not found: {rightPath}");
                return;
            }

            if (leftIsDir && rightIsDir)
            {
                // Directory comparison
                Console.WriteLine($"📁 Comparing directories:");
                Console.WriteLine($"   Left:  {leftPath}");
                Console.WriteLine($"   Right: {rightPath}");
                Console.WriteLine();
                
                var context = CreateDiffContext(ignoreFields, ComparisonType.DirectoryToDirectory);
                var result = await diffService.CompareDirectoriesAsync(leftPath, rightPath, context);
                
                if (result.IsFailure)
                {
                    Console.WriteLine($"❌ Directory comparison not yet fully implemented");
                    Console.WriteLine($"   Please compare individual files for now");
                    return;
                }

                // TODO: Display directory comparison results
            }
            else if (leftIsFile && rightIsFile)
            {
                // File comparison
                Console.WriteLine($"📄 Comparing files:");
                Console.WriteLine($"   Left:  {Path.GetFileName(leftPath)}");
                Console.WriteLine($"   Right: {Path.GetFileName(rightPath)}");
                Console.WriteLine();

                var context = CreateDiffContext(ignoreFields, ComparisonType.MessageToMessage);
                var result = await diffService.CompareMessageFilesAsync(leftPath, rightPath, context);

                if (result.IsFailure)
                {
                    Console.WriteLine($"❌ Comparison failed: {result.Error}");
                    return;
                }

                var diff = result.Value;

                // Display results based on severity filter
                await DisplayDiffResultsAsync(diff, severity);

                // Generate report if specified
                if (!string.IsNullOrEmpty(reportFile))
                {
                    await GenerateReportAsync(diff, reportFile);
                    Console.WriteLine($"📄 Report generated: {reportFile}");
                }
            }
            else
            {
                Console.WriteLine("❌ Cannot compare file to directory. Both paths must be files or both must be directories.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during diff execution");
            Console.WriteLine($"❌ Error: {ex.Message}");
        }
    }

    private static DiffContext CreateDiffContext(string? ignoreFields, ComparisonType comparisonType)
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
                UseIntelligentAnalysis = false, // TODO: Enable for Pro users with local AI
                NormalizeFormatting = true
            }
        };
    }

    private static async Task DisplayDiffResultsAsync(MessageDiff diff, string minSeverity)
    {
        await Task.Yield();

        // Summary
        Console.WriteLine($"🔍 Diff Results");
        Console.WriteLine($"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        Console.WriteLine($"Similarity: {diff.SimilarityScore:P1}");
        Console.WriteLine($"Differences: {diff.Summary.TotalDifferences}");

        if (diff.Summary.TotalDifferences == 0)
        {
            Console.WriteLine();
            Console.WriteLine("✅ Files are identical!");
            return;
        }

        if (diff.Summary.CriticalDifferences > 0)
        {
            Console.WriteLine($"  🔴 Critical: {diff.Summary.CriticalDifferences}");
        }
        if (diff.Summary.WarningDifferences > 0)
        {
            Console.WriteLine($"  🟡 Warning: {diff.Summary.WarningDifferences}");
        }
        if (diff.Summary.InformationalDifferences > 0)
        {
            Console.WriteLine($"  ℹ️  Info: {diff.Summary.InformationalDifferences}");
        }

        Console.WriteLine();

        // Determine minimum severity to display
        var minSeverityLevel = minSeverity.ToLower() switch
        {
            "error" => DifferenceSeverity.Error,
            "warn" => DifferenceSeverity.Warning,
            _ => DifferenceSeverity.Info
        };

        // Field differences filtered by severity
        var filteredDifferences = diff.FieldDifferences
            .Where(d => d.Severity >= minSeverityLevel)
            .ToList();

        if (filteredDifferences.Any())
        {
            Console.WriteLine($"📋 Field Differences (showing {minSeverity} and above):");
            Console.WriteLine($"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

            foreach (var fieldDiff in filteredDifferences.Take(20))
            {
                var severityIcon = fieldDiff.Severity switch
                {
                    DifferenceSeverity.Critical or DifferenceSeverity.Error => "❌",
                    DifferenceSeverity.Warning => "⚠️",
                    _ => "ℹ️"
                };

                Console.WriteLine($"{severityIcon} {fieldDiff.FieldPath}");
                
                if (!string.IsNullOrEmpty(fieldDiff.FieldDescription))
                {
                    Console.WriteLine($"   Field: {fieldDiff.FieldDescription}");
                }
                
                Console.WriteLine($"   Left:  {TruncateValue(fieldDiff.LeftValue)}");
                Console.WriteLine($"   Right: {TruncateValue(fieldDiff.RightValue)}");

                if (!string.IsNullOrEmpty(fieldDiff.SuggestedFix))
                {
                    Console.WriteLine($"   💡 Hint: {fieldDiff.SuggestedFix}");
                }

                Console.WriteLine();
            }

            if (filteredDifferences.Count > 20)
            {
                Console.WriteLine($"... and {filteredDifferences.Count - 20} more differences");
                Console.WriteLine("Generate a report with --report for complete details");
            }
        }

        // Insights
        if (diff.Insights.Any())
        {
            Console.WriteLine();
            Console.WriteLine("🤖 Analysis Insights:");
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

    private static string TruncateValue(string? value)
    {
        if (value == null)
            return "(missing)";
        
        if (value.Length > 50)
            return value.Substring(0, 47) + "...";
        
        return value;
    }

    private static async Task GenerateReportAsync(MessageDiff diff, string reportFile)
    {
        await Task.Yield();

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
            // Generate HTML report (default)
            var html = GenerateHtmlReport(diff);
            await File.WriteAllTextAsync(reportFile, html);
        }
    }

    private static string GenerateHtmlReport(MessageDiff diff)
    {
        return $@"<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Pidgeon Diff Report</title>
    <style>
        body {{ 
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
            margin: 0;
            padding: 20px;
            background: #f5f5f5;
        }}
        .container {{
            max-width: 1200px;
            margin: 0 auto;
            background: white;
            border-radius: 8px;
            box-shadow: 0 2px 4px rgba(0,0,0,0.1);
            padding: 30px;
        }}
        h1 {{
            color: #333;
            border-bottom: 3px solid #007acc;
            padding-bottom: 10px;
        }}
        .summary {{
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
            gap: 20px;
            margin: 30px 0;
        }}
        .stat-card {{
            background: #f8f9fa;
            padding: 15px;
            border-radius: 5px;
            border-left: 4px solid #007acc;
        }}
        .stat-value {{
            font-size: 24px;
            font-weight: bold;
            color: #333;
        }}
        .stat-label {{
            color: #666;
            font-size: 14px;
            margin-top: 5px;
        }}
        .diff-item {{
            margin: 20px 0;
            padding: 15px;
            background: #fafafa;
            border-radius: 5px;
            border-left: 4px solid #ddd;
        }}
        .diff-item.critical {{ border-left-color: #dc3545; }}
        .diff-item.warning {{ border-left-color: #ffc107; }}
        .diff-item.info {{ border-left-color: #17a2b8; }}
        .field-path {{
            font-weight: bold;
            color: #007acc;
            font-family: 'Courier New', monospace;
        }}
        .diff-values {{
            display: grid;
            grid-template-columns: 1fr 1fr;
            gap: 20px;
            margin-top: 10px;
        }}
        .value-box {{
            padding: 10px;
            background: white;
            border: 1px solid #ddd;
            border-radius: 3px;
            font-family: 'Courier New', monospace;
            font-size: 13px;
            word-break: break-all;
        }}
        .value-label {{
            font-weight: bold;
            margin-bottom: 5px;
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
        }}
        .missing {{ color: #999; font-style: italic; }}
        .hint {{
            margin-top: 10px;
            padding: 10px;
            background: #fff3cd;
            border: 1px solid #ffeeba;
            border-radius: 3px;
            color: #856404;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <h1>🔍 Pidgeon Diff Report</h1>
        <p>Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>
        
        <div class='summary'>
            <div class='stat-card'>
                <div class='stat-value'>{diff.SimilarityScore:P0}</div>
                <div class='stat-label'>Similarity Score</div>
            </div>
            <div class='stat-card'>
                <div class='stat-value'>{diff.Summary.TotalDifferences}</div>
                <div class='stat-label'>Total Differences</div>
            </div>
            <div class='stat-card'>
                <div class='stat-value'>{diff.Summary.CriticalDifferences}</div>
                <div class='stat-label'>Critical Issues</div>
            </div>
            <div class='stat-card'>
                <div class='stat-value'>{diff.Summary.WarningDifferences}</div>
                <div class='stat-label'>Warnings</div>
            </div>
        </div>
        
        <h2>Field Differences</h2>
        {string.Join("", diff.FieldDifferences.Select(fd => $@"
        <div class='diff-item {fd.Severity.ToString().ToLower()}'>
            <div class='field-path'>{fd.FieldPath}</div>
            {(string.IsNullOrEmpty(fd.FieldDescription) ? "" : $"<div>{fd.FieldDescription}</div>")}
            <div class='diff-values'>
                <div class='value-box'>
                    <div class='value-label'>Left:</div>
                    {(fd.LeftValue == null ? "<span class='missing'>(missing)</span>" : System.Web.HttpUtility.HtmlEncode(fd.LeftValue))}
                </div>
                <div class='value-box'>
                    <div class='value-label'>Right:</div>
                    {(fd.RightValue == null ? "<span class='missing'>(missing)</span>" : System.Web.HttpUtility.HtmlEncode(fd.RightValue))}
                </div>
            </div>
            {(string.IsNullOrEmpty(fd.SuggestedFix) ? "" : $"<div class='hint'>💡 {fd.SuggestedFix}</div>")}
        </div>"))}
        
        {(diff.Insights.Any() ? $@"
        <h2>Analysis Insights</h2>
        {string.Join("", diff.Insights.Select(i => $@"
        <div class='diff-item'>
            <h3>{i.Title}</h3>
            <p>{i.Description}</p>
            {(string.IsNullOrEmpty(i.RecommendedAction) ? "" : $"<p><strong>Recommended Action:</strong> {i.RecommendedAction}</p>")}
            <p><small>Confidence: {i.Confidence:P0} | Source: {i.AnalysisSource.EngineName}</small></p>
        </div>"))}" : "")}
    </div>
</body>
</html>";
    }
}