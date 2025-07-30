// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Segmint.Core.Standards.HL7.v23.Messages;
using Segmint.CLI.Serialization;

namespace Segmint.CLI.Services;

/// <summary>
/// Implementation of output service for handling various output operations.
/// </summary>
public class OutputService : IOutputService
{
    private readonly ILogger<OutputService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="OutputService"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    public OutputService(ILogger<OutputService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            TypeInfoResolver = SegmintJsonContext.Default
        };
    }

    /// <inheritdoc />
    public async Task SaveMessagesAsync(
        IEnumerable<HL7Message> messages,
        string outputPath,
        string? format = null,
        bool batchMode = false,
        CancellationToken cancellationToken = default)
    {
        format = format?.ToLowerInvariant() ?? "hl7";
        
        _logger.LogInformation("Saving {MessageCount} messages to {OutputPath} in {Format} format", 
            messages.Count(), outputPath, format);

        // Ensure output directory exists
        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        switch (format)
        {
            case "hl7":
                await SaveAsHL7Async(messages, outputPath, batchMode, cancellationToken);
                break;
            case "json":
                await SaveAsJsonAsync(messages, outputPath, batchMode, cancellationToken);
                break;
            case "xml":
                await SaveAsXmlAsync(messages, outputPath, batchMode, cancellationToken);
                break;
            default:
                throw new ArgumentException($"Unsupported output format: {format}");
        }

        _logger.LogInformation("Successfully saved {MessageCount} messages to {OutputPath}", 
            messages.Count(), outputPath);
    }

    /// <inheritdoc />
    public async Task SaveValidationReportAsync(
        ValidationSummary validationSummary,
        string reportPath,
        string format = "text",
        CancellationToken cancellationToken = default)
    {
        format = format.ToLowerInvariant();
        
        _logger.LogInformation("Saving validation report to {ReportPath} in {Format} format", reportPath, format);

        // Ensure output directory exists
        var directory = Path.GetDirectoryName(reportPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string content = format switch
        {
            "text" => GenerateTextValidationReport(validationSummary),
            "json" => JsonSerializer.Serialize(validationSummary, _jsonOptions),
            "xml" => GenerateXmlValidationReport(validationSummary),
            "html" => GenerateHtmlValidationReport(validationSummary),
            _ => throw new ArgumentException($"Unsupported report format: {format}")
        };

        await File.WriteAllTextAsync(reportPath, content, cancellationToken);
        
        _logger.LogInformation("Successfully saved validation report to {ReportPath}", reportPath);
    }

    /// <inheritdoc />
    public async Task SaveComparisonReportAsync(
        ConfigurationComparisonResult comparisonResult,
        string reportPath,
        string format = "text",
        CancellationToken cancellationToken = default)
    {
        format = format.ToLowerInvariant();
        
        _logger.LogInformation("Saving comparison report to {ReportPath} in {Format} format", reportPath, format);

        // Ensure output directory exists
        var directory = Path.GetDirectoryName(reportPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string content = format switch
        {
            "text" => GenerateTextComparisonReport(comparisonResult),
            "json" => JsonSerializer.Serialize(comparisonResult, _jsonOptions),
            "xml" => GenerateXmlComparisonReport(comparisonResult),
            "html" => GenerateHtmlComparisonReport(comparisonResult),
            _ => throw new ArgumentException($"Unsupported report format: {format}")
        };

        await File.WriteAllTextAsync(reportPath, content, cancellationToken);
        
        _logger.LogInformation("Successfully saved comparison report to {ReportPath}", reportPath);
    }

    /// <inheritdoc />
    public async Task DisplayValidationSummaryAsync(
        ValidationSummary validationSummary,
        bool summaryOnly = false)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine("                     VALIDATION SUMMARY");
        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine();
        
        sb.AppendLine($"Total Messages:    {validationSummary.TotalMessages}");
        sb.AppendLine($"Valid Messages:    {validationSummary.ValidMessages} ({GetPercentage(validationSummary.ValidMessages, validationSummary.TotalMessages):F1}%)");
        sb.AppendLine($"Invalid Messages:  {validationSummary.InvalidMessages} ({GetPercentage(validationSummary.InvalidMessages, validationSummary.TotalMessages):F1}%)");
        sb.AppendLine($"Processing Time:   {validationSummary.ElapsedTime.TotalMilliseconds:F0}ms");
        sb.AppendLine();

        if (!summaryOnly && validationSummary.Results.Any())
        {
            sb.AppendLine("DETAILED RESULTS:");
            sb.AppendLine("───────────────────────────────────────────────────────────────");
            
            foreach (var result in validationSummary.Results.Take(10)) // Show first 10 results
            {
                var status = result.ValidationResult.IsValid ? "✓ VALID" : "✗ INVALID";
                sb.AppendLine($"{status,-10} {result.Source} (Message {result.MessageIndex + 1})");
                
                if (!result.ValidationResult.IsValid)
                {
                    foreach (var error in result.ValidationResult.Errors.Take(3)) // Show first 3 errors
                    {
                        sb.AppendLine($"           Error: {error.Message}");
                    }
                }
            }
            
            if (validationSummary.Results.Count > 10)
            {
                sb.AppendLine($"... and {validationSummary.Results.Count - 10} more results");
            }
        }

        await Console.Out.WriteAsync(sb.ToString());
    }

    /// <inheritdoc />
    public async Task DisplayComparisonResultAsync(
        ConfigurationComparisonResult comparisonResult,
        bool summaryOnly = false)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine("                  CONFIGURATION COMPARISON");
        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine();
        
        sb.AppendLine($"Result: {(comparisonResult.AreIdentical ? "IDENTICAL" : "DIFFERENT")}");
        sb.AppendLine($"Differences Found: {comparisonResult.Differences.Count}");
        sb.AppendLine();
        sb.AppendLine($"Summary: {comparisonResult.Summary}");
        sb.AppendLine();

        if (!summaryOnly && comparisonResult.Differences.Any())
        {
            sb.AppendLine("DIFFERENCES:");
            sb.AppendLine("───────────────────────────────────────────────────────────────");
            
            foreach (var diff in comparisonResult.Differences.Take(10)) // Show first 10 differences
            {
                sb.AppendLine($"{diff.Type,-10} {diff.Path}");
                sb.AppendLine($"           {diff.Description}");
                sb.AppendLine();
            }
            
            if (comparisonResult.Differences.Count > 10)
            {
                sb.AppendLine($"... and {comparisonResult.Differences.Count - 10} more differences");
            }
        }

        await Console.Out.WriteAsync(sb.ToString());
    }

    /// <inheritdoc />
    public async Task DisplayTemplatesAsync(
        IEnumerable<ConfigurationTemplate> templates,
        bool detailed = false)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine("                 CONFIGURATION TEMPLATES");
        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine();

        foreach (var template in templates)
        {
            sb.AppendLine($"Name: {template.Name}");
            if (detailed)
            {
                sb.AppendLine($"  Description: {template.Description}");
                sb.AppendLine($"  Category: {template.Category}");
                sb.AppendLine($"  Version: {template.Version}");
                sb.AppendLine($"  Message Types: {string.Join(", ", template.MessageTypes)}");
            }
            sb.AppendLine();
        }

        await Console.Out.WriteAsync(sb.ToString());
    }

    /// <inheritdoc />
    public IEnumerable<string> GetSupportedOutputFormats()
    {
        return new[] { "hl7", "json", "xml" };
    }

    /// <inheritdoc />
    public IEnumerable<string> GetSupportedReportFormats()
    {
        return new[] { "text", "json", "xml", "html" };
    }

    /// <summary>
    /// Saves messages as HL7 format.
    /// </summary>
    private async Task SaveAsHL7Async(
        IEnumerable<HL7Message> messages,
        string outputPath,
        bool batchMode,
        CancellationToken cancellationToken)
    {
        if (batchMode || messages.Count() > 1)
        {
            // Multiple messages - save each to separate file or batch file
            if (Directory.Exists(outputPath) || outputPath.EndsWith(Path.DirectorySeparatorChar))
            {
                // Output directory - save each message as separate file
                if (!Directory.Exists(outputPath))
                {
                    Directory.CreateDirectory(outputPath);
                }

                int index = 1;
                foreach (var message in messages)
                {
                    var fileName = $"message_{index:D4}.hl7";
                    var filePath = Path.Combine(outputPath, fileName);
                    await File.WriteAllTextAsync(filePath, message.ToHL7String(), cancellationToken);
                    index++;
                }
            }
            else
            {
                // Single file - concatenate all messages
                var allMessages = string.Join("\n\n", messages.Select(m => m.ToHL7String()));
                await File.WriteAllTextAsync(outputPath, allMessages, cancellationToken);
            }
        }
        else
        {
            // Single message
            var message = messages.First();
            await File.WriteAllTextAsync(outputPath, message.ToHL7String(), cancellationToken);
        }
    }

    /// <summary>
    /// Saves messages as JSON format.
    /// </summary>
    private async Task SaveAsJsonAsync(
        IEnumerable<HL7Message> messages,
        string outputPath,
        bool batchMode,
        CancellationToken cancellationToken)
    {
        var messageData = messages.Select(m => new MessageDataDto
        {
            MessageType = m.GetType().Name,
            Segments = m.Select(s => new SegmentDataDto
            {
                SegmentType = s.SegmentId,
                HL7Content = s.ToHL7String()
            }).ToList(),
            HL7String = m.ToHL7String()
        }).ToList();

        string json = JsonSerializer.Serialize(messageData, _jsonOptions);
        await File.WriteAllTextAsync(outputPath, json, cancellationToken);
    }

    /// <summary>
    /// Saves messages as XML format.
    /// </summary>
    private async Task SaveAsXmlAsync(
        IEnumerable<HL7Message> messages,
        string outputPath,
        bool batchMode,
        CancellationToken cancellationToken)
    {
        var root = new XElement("HL7Messages");
        
        foreach (var message in messages)
        {
            var messageElement = new XElement("Message",
                new XAttribute("Type", message.GetType().Name));
            
            foreach (var segment in message)
            {
                var segmentElement = new XElement("Segment",
                    new XAttribute("Type", segment.SegmentId),
                    new XCData(segment.ToHL7String()));
                messageElement.Add(segmentElement);
            }
            
            root.Add(messageElement);
        }

        var document = new XDocument(root);
        await File.WriteAllTextAsync(outputPath, document.ToString(), cancellationToken);
    }

    /// <summary>
    /// Generates text format validation report.
    /// </summary>
    private string GenerateTextValidationReport(ValidationSummary validationSummary)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("HL7 MESSAGE VALIDATION REPORT");
        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine();
        
        sb.AppendLine("SUMMARY:");
        sb.AppendLine($"  Total Messages: {validationSummary.TotalMessages}");
        sb.AppendLine($"  Valid Messages: {validationSummary.ValidMessages}");
        sb.AppendLine($"  Invalid Messages: {validationSummary.InvalidMessages}");
        sb.AppendLine($"  Processing Time: {validationSummary.ElapsedTime.TotalMilliseconds:F0}ms");
        sb.AppendLine();
        
        sb.AppendLine("DETAILED RESULTS:");
        sb.AppendLine("───────────────────────────────────────────────────────────────");
        
        foreach (var result in validationSummary.Results)
        {
            sb.AppendLine($"Source: {result.Source}");
            sb.AppendLine($"Message: {result.MessageIndex + 1}");
            sb.AppendLine($"Status: {(result.ValidationResult.IsValid ? "VALID" : "INVALID")}");
            sb.AppendLine($"Processing Time: {result.ProcessingTime.TotalMilliseconds:F0}ms");
            
            if (!result.ValidationResult.IsValid)
            {
                sb.AppendLine("Errors:");
                foreach (var error in result.ValidationResult.Errors)
                {
                    sb.AppendLine($"  - {error.Level}: {error.Message}");
                }
            }
            
            sb.AppendLine();
        }
        
        return sb.ToString();
    }

    /// <summary>
    /// Generates text format comparison report.
    /// </summary>
    private string GenerateTextComparisonReport(ConfigurationComparisonResult comparisonResult)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("CONFIGURATION COMPARISON REPORT");
        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine();
        
        sb.AppendLine($"Result: {(comparisonResult.AreIdentical ? "IDENTICAL" : "DIFFERENT")}");
        sb.AppendLine($"Total Differences: {comparisonResult.Differences.Count}");
        sb.AppendLine($"Summary: {comparisonResult.Summary}");
        sb.AppendLine();
        
        if (comparisonResult.Differences.Any())
        {
            sb.AppendLine("DIFFERENCES:");
            sb.AppendLine("───────────────────────────────────────────────────────────────");
            
            foreach (var diff in comparisonResult.Differences)
            {
                sb.AppendLine($"Path: {diff.Path}");
                sb.AppendLine($"Type: {diff.Type}");
                sb.AppendLine($"Description: {diff.Description}");
                if (!string.IsNullOrEmpty(diff.Value1))
                    sb.AppendLine($"Value 1: {diff.Value1}");
                if (!string.IsNullOrEmpty(diff.Value2))
                    sb.AppendLine($"Value 2: {diff.Value2}");
                sb.AppendLine();
            }
        }
        
        return sb.ToString();
    }

    /// <summary>
    /// Generates XML format validation report.
    /// </summary>
    private string GenerateXmlValidationReport(ValidationSummary validationSummary)
    {
        var root = new XElement("ValidationReport",
            new XAttribute("GeneratedUtc", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")),
            new XElement("Summary",
                new XElement("TotalMessages", validationSummary.TotalMessages),
                new XElement("ValidMessages", validationSummary.ValidMessages),
                new XElement("InvalidMessages", validationSummary.InvalidMessages),
                new XElement("ProcessingTimeMs", validationSummary.ElapsedTime.TotalMilliseconds)
            )
        );

        var resultsElement = new XElement("Results");
        foreach (var result in validationSummary.Results)
        {
            var resultElement = new XElement("Result",
                new XAttribute("Source", result.Source),
                new XAttribute("MessageIndex", result.MessageIndex),
                new XAttribute("IsValid", result.ValidationResult.IsValid),
                new XAttribute("ProcessingTimeMs", result.ProcessingTime.TotalMilliseconds)
            );

            if (!result.ValidationResult.IsValid)
            {
                var errorsElement = new XElement("Errors");
                foreach (var error in result.ValidationResult.Errors)
                {
                    errorsElement.Add(new XElement("Error",
                        new XAttribute("Level", error.Level),
                        new XAttribute("Code", error.Code ?? ""),
                        error.Message
                    ));
                }
                resultElement.Add(errorsElement);
            }

            resultsElement.Add(resultElement);
        }

        root.Add(resultsElement);
        return new XDocument(root).ToString();
    }

    /// <summary>
    /// Generates XML format comparison report.
    /// </summary>
    private string GenerateXmlComparisonReport(ConfigurationComparisonResult comparisonResult)
    {
        var root = new XElement("ComparisonReport",
            new XAttribute("GeneratedUtc", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")),
            new XAttribute("AreIdentical", comparisonResult.AreIdentical),
            new XElement("Summary", comparisonResult.Summary)
        );

        var differencesElement = new XElement("Differences");
        foreach (var diff in comparisonResult.Differences)
        {
            differencesElement.Add(new XElement("Difference",
                new XAttribute("Path", diff.Path),
                new XAttribute("Type", diff.Type),
                new XElement("Description", diff.Description),
                new XElement("Value1", diff.Value1 ?? ""),
                new XElement("Value2", diff.Value2 ?? "")
            ));
        }

        root.Add(differencesElement);
        return new XDocument(root).ToString();
    }

    /// <summary>
    /// Generates HTML format validation report.
    /// </summary>
    private string GenerateHtmlValidationReport(ValidationSummary validationSummary)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html><head><title>HL7 Validation Report</title>");
        sb.AppendLine("<style>");
        sb.AppendLine("body { font-family: Arial, sans-serif; margin: 20px; }");
        sb.AppendLine(".header { background: #f0f0f0; padding: 10px; border-radius: 5px; }");
        sb.AppendLine(".summary { margin: 20px 0; }");
        sb.AppendLine(".valid { color: green; }");
        sb.AppendLine(".invalid { color: red; }");
        sb.AppendLine("table { border-collapse: collapse; width: 100%; }");
        sb.AppendLine("th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }");
        sb.AppendLine("th { background-color: #f2f2f2; }");
        sb.AppendLine("</style></head><body>");
        
        sb.AppendLine("<div class='header'>");
        sb.AppendLine("<h1>HL7 Message Validation Report</h1>");
        sb.AppendLine($"<p>Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>");
        sb.AppendLine("</div>");
        
        sb.AppendLine("<div class='summary'>");
        sb.AppendLine("<h2>Summary</h2>");
        sb.AppendLine($"<p>Total Messages: {validationSummary.TotalMessages}</p>");
        sb.AppendLine($"<p class='valid'>Valid Messages: {validationSummary.ValidMessages}</p>");
        sb.AppendLine($"<p class='invalid'>Invalid Messages: {validationSummary.InvalidMessages}</p>");
        sb.AppendLine($"<p>Processing Time: {validationSummary.ElapsedTime.TotalMilliseconds:F0}ms</p>");
        sb.AppendLine("</div>");
        
        sb.AppendLine("<h2>Detailed Results</h2>");
        sb.AppendLine("<table>");
        sb.AppendLine("<tr><th>Source</th><th>Message</th><th>Status</th><th>Processing Time</th><th>Errors</th></tr>");
        
        foreach (var result in validationSummary.Results)
        {
            var statusClass = result.ValidationResult.IsValid ? "valid" : "invalid";
            var status = result.ValidationResult.IsValid ? "VALID" : "INVALID";
            var errors = result.ValidationResult.IsValid ? "" : 
                string.Join("<br>", result.ValidationResult.Errors.Select(e => $"{e.Level}: {e.Message}"));
            
            sb.AppendLine($"<tr>");
            sb.AppendLine($"<td>{result.Source}</td>");
            sb.AppendLine($"<td>{result.MessageIndex + 1}</td>");
            sb.AppendLine($"<td class='{statusClass}'>{status}</td>");
            sb.AppendLine($"<td>{result.ProcessingTime.TotalMilliseconds:F0}ms</td>");
            sb.AppendLine($"<td>{errors}</td>");
            sb.AppendLine($"</tr>");
        }
        
        sb.AppendLine("</table>");
        sb.AppendLine("</body></html>");
        
        return sb.ToString();
    }

    /// <summary>
    /// Generates HTML format comparison report.
    /// </summary>
    private string GenerateHtmlComparisonReport(ConfigurationComparisonResult comparisonResult)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html><head><title>Configuration Comparison Report</title>");
        sb.AppendLine("<style>");
        sb.AppendLine("body { font-family: Arial, sans-serif; margin: 20px; }");
        sb.AppendLine(".header { background: #f0f0f0; padding: 10px; border-radius: 5px; }");
        sb.AppendLine(".identical { color: green; }");
        sb.AppendLine(".different { color: red; }");
        sb.AppendLine("table { border-collapse: collapse; width: 100%; }");
        sb.AppendLine("th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }");
        sb.AppendLine("th { background-color: #f2f2f2; }");
        sb.AppendLine("</style></head><body>");
        
        sb.AppendLine("<div class='header'>");
        sb.AppendLine("<h1>Configuration Comparison Report</h1>");
        sb.AppendLine($"<p>Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>");
        sb.AppendLine("</div>");
        
        var resultClass = comparisonResult.AreIdentical ? "identical" : "different";
        var resultText = comparisonResult.AreIdentical ? "IDENTICAL" : "DIFFERENT";
        
        sb.AppendLine($"<h2 class='{resultClass}'>Result: {resultText}</h2>");
        sb.AppendLine($"<p>Total Differences: {comparisonResult.Differences.Count}</p>");
        sb.AppendLine($"<p>{comparisonResult.Summary}</p>");
        
        if (comparisonResult.Differences.Any())
        {
            sb.AppendLine("<h2>Differences</h2>");
            sb.AppendLine("<table>");
            sb.AppendLine("<tr><th>Path</th><th>Type</th><th>Description</th><th>Value 1</th><th>Value 2</th></tr>");
            
            foreach (var diff in comparisonResult.Differences)
            {
                sb.AppendLine($"<tr>");
                sb.AppendLine($"<td>{diff.Path}</td>");
                sb.AppendLine($"<td>{diff.Type}</td>");
                sb.AppendLine($"<td>{diff.Description}</td>");
                sb.AppendLine($"<td>{diff.Value1 ?? ""}</td>");
                sb.AppendLine($"<td>{diff.Value2 ?? ""}</td>");
                sb.AppendLine($"</tr>");
            }
            
            sb.AppendLine("</table>");
        }
        
        sb.AppendLine("</body></html>");
        
        return sb.ToString();
    }

    /// <summary>
    /// Calculates percentage.
    /// </summary>
    private static double GetPercentage(int value, int total)
    {
        return total == 0 ? 0 : (double)value / total * 100;
    }
}