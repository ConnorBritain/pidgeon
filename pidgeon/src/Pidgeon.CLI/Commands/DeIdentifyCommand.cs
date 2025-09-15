// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Microsoft.Extensions.Logging;
using Pidgeon.Core.Application.Services.DeIdentification;
using Pidgeon.Core.Domain.DeIdentification;
using System.CommandLine;

namespace Pidgeon.CLI.Commands;

/// <summary>
/// Command for de-identifying healthcare messages to create safe test data.
/// Removes PHI while preserving referential integrity and clinical relationships.
/// </summary>
public class DeIdentifyCommand : CommandBuilderBase
{
    private readonly IDeIdentificationEngine _deIdentificationEngine;

    public DeIdentifyCommand(
        ILogger<DeIdentifyCommand> logger,
        IDeIdentificationEngine deIdentificationEngine) 
        : base(logger)
    {
        _deIdentificationEngine = deIdentificationEngine;
    }

    public override Command CreateCommand()
    {
        var command = new Command("deident", "De-identify real messages/resources (on-device), preserving referential integrity");

        // Options matching CLI_REFERENCE.md specification
        var inputOption = CreateRequiredOption("--in", "File or folder of source data");
        var outputOption = CreateRequiredOption("--out", "Output file/folder (created if missing)");
        var dateShiftOption = CreateNullableOption("--date-shift", "Shift dates by +/-N days (e.g., 30d, -14d)");
        var keepIdsOption = CreateNullableOption("--keep-ids", "Comma-list of identifiers to keep unhashed (e.g., visitId)");
        var saltOption = CreateNullableOption("--salt", "Salt for deterministic hashing");
        var previewOption = CreateBooleanOption("--preview", "Show sample before/after rows without writing files");

        command.Add(inputOption);
        command.Add(outputOption);
        command.Add(dateShiftOption);
        command.Add(keepIdsOption);
        command.Add(saltOption);
        command.Add(previewOption);

        SetCommandAction(command, async (parseResult, cancellationToken) =>
        {
            try
            {
                var inputPath = parseResult.GetValue(inputOption);
                var outputPath = parseResult.GetValue(outputOption);
                var dateShiftStr = parseResult.GetValue(dateShiftOption);
                var keepIds = parseResult.GetValue(keepIdsOption);
                var salt = parseResult.GetValue(saltOption);
                var preview = parseResult.GetValue(previewOption);

                // Parse date shift option
                TimeSpan? dateShift = null;
                if (!string.IsNullOrEmpty(dateShiftStr))
                {
                    dateShift = ParseDateShift(dateShiftStr);
                    if (!dateShift.HasValue)
                    {
                        Logger.LogError("Invalid date-shift format. Use format like '30d' or '-14d'");
                        Console.WriteLine("❌ Invalid date-shift format. Use format like '30d' or '-14d'");
                        return 1;
                    }
                }

                // Parse keep-ids option
                var idsToKeep = new HashSet<string>();
                if (!string.IsNullOrEmpty(keepIds))
                {
                    idsToKeep = new HashSet<string>(keepIds.Split(',', StringSplitOptions.RemoveEmptyEntries));
                }

                // Create de-identification options
                var options = new DeIdentificationOptions
                {
                    Salt = salt,
                    DateShift = dateShift,
                    PreserveRelationships = true,
                    GenerateReport = !preview,
                    Method = DeIdentificationMethod.SafeHarborPlus,
                    PreviewMode = preview,
                    PreserveDateTimes = true,
                    CustomFieldMappings = new Dictionary<string, IdentifierType>()
                    // TODO: Support custom field mappings from config file
                };

                // Handle preview mode
                if (preview)
                {
                    Logger.LogInformation("Generating de-identification preview for {Input}", inputPath);
                    Console.WriteLine($"🔍 Previewing de-identification changes for: {inputPath}");
                    
                    var previewResult = await _deIdentificationEngine.PreviewChangesAsync(inputPath!, options);
                    if (previewResult.IsFailure)
                    {
                        Logger.LogError("Preview generation failed: {Error}", previewResult.Error.Message);
                        Console.WriteLine($"❌ Preview generation failed: {previewResult.Error.Message}");
                        return 1;
                    }

                    DisplayPreview(previewResult.Value);
                    return 0;
                }

                // Determine if input is file or directory
                bool isDirectory = Directory.Exists(inputPath);
                bool isFile = File.Exists(inputPath);

                if (!isDirectory && !isFile)
                {
                    Logger.LogError("Input path does not exist: {Path}", inputPath);
                    Console.WriteLine($"❌ Input path does not exist: {inputPath}");
                    return 1;
                }

                // Process based on input type
                if (isFile)
                {
                    Logger.LogInformation("De-identifying file: {Input} -> {Output}", inputPath, outputPath);
                    Console.WriteLine($"🔒 De-identifying file: {inputPath}");
                    
                    var result = await _deIdentificationEngine.ProcessFileAsync(inputPath!, outputPath!, options);
                    if (result.IsFailure)
                    {
                        Logger.LogError("De-identification failed: {Error}", result.Error.Message);
                        Console.WriteLine($"❌ De-identification failed: {result.Error.Message}");
                        return 1;
                    }

                    DisplaySummary(result.Value);
                    Console.WriteLine($"✅ De-identified file saved to: {outputPath}");
                }
                else // isDirectory
                {
                    Logger.LogInformation("De-identifying directory: {Input} -> {Output}", inputPath, outputPath);
                    Console.WriteLine($"🔒 De-identifying directory: {inputPath}");
                    
                    var result = await _deIdentificationEngine.ProcessDirectoryAsync(inputPath!, outputPath!, options);
                    if (result.IsFailure)
                    {
                        Logger.LogError("De-identification failed: {Error}", result.Error.Message);
                        Console.WriteLine($"❌ De-identification failed: {result.Error.Message}");
                        return 1;
                    }

                    DisplayBatchSummary(result.Value);
                    Console.WriteLine($"✅ De-identified files saved to: {outputPath}");
                }

                return 0;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Unexpected error during de-identification");
                Console.WriteLine($"❌ Unexpected error: {ex.Message}");
                return 1;
            }
        });

        return command;
    }

    /// <summary>
    /// Parses date shift string format (e.g., "30d", "-14d").
    /// </summary>
    private static TimeSpan? ParseDateShift(string dateShiftStr)
    {
        if (string.IsNullOrWhiteSpace(dateShiftStr))
            return null;

        dateShiftStr = dateShiftStr.Trim();
        
        // Check for 'd' suffix for days
        if (dateShiftStr.EndsWith("d", StringComparison.OrdinalIgnoreCase))
        {
            var daysStr = dateShiftStr[..^1];
            if (int.TryParse(daysStr, out var days))
            {
                return TimeSpan.FromDays(days);
            }
        }
        
        // Try parsing as plain number of days
        if (int.TryParse(dateShiftStr, out var plainDays))
        {
            return TimeSpan.FromDays(plainDays);
        }

        return null;
    }

    /// <summary>
    /// Displays a preview of de-identification changes.
    /// </summary>
    private static void DisplayPreview(DeIdentificationPreview preview)
    {
        Console.WriteLine("\n📊 De-identification Preview:");
        Console.WriteLine($"   Files to process: {preview.FilesToProcess.Count}");
        
        if (preview.EstimatedStatistics != null)
        {
            Console.WriteLine($"   Estimated fields to modify: {preview.EstimatedStatistics.FieldsModified}");
            Console.WriteLine($"   Estimated dates to shift: {preview.EstimatedStatistics.DatesShifted}");
        }

        if (preview.SampleChanges.Any())
        {
            Console.WriteLine("\n   Sample changes:");
            foreach (var change in preview.SampleChanges.Take(5))
            {
                Console.WriteLine($"     {change.Location}: \"{change.OriginalValue}\" → \"{change.ReplacementValue}\"");
            }
        }

        if (preview.ComplianceAssessment != null)
        {
            Console.WriteLine($"\n   HIPAA Safe Harbor compliance: {(preview.ComplianceAssessment.MeetsSafeHarbor ? "✓" : "✗")}");
        }

        Console.WriteLine($"\n   Estimated processing time: {preview.ResourceEstimate.EstimatedTime.TotalSeconds:F1}s");
    }

    /// <summary>
    /// Displays a summary of the de-identification results.
    /// </summary>
    private static void DisplaySummary(DeIdentificationResult result)
    {
        Console.WriteLine("\n📊 De-identification Summary:");
        Console.WriteLine($"   Messages processed: {result.Statistics.TotalMessages}");
        Console.WriteLine($"   Fields modified: {result.Statistics.FieldsModified}");
        Console.WriteLine($"   Dates shifted: {result.Statistics.DatesShifted}");
        Console.WriteLine($"   Processing time: {result.Statistics.TotalProcessingTime.TotalMilliseconds:F0}ms");
        
        if (result.Compliance != null)
        {
            Console.WriteLine($"   HIPAA Safe Harbor compliant: {(result.Compliance.MeetsSafeHarbor ? "✓" : "✗")}");
        }
    }

    /// <summary>
    /// Displays a summary of batch de-identification results.
    /// </summary>
    private static void DisplayBatchSummary(BatchDeIdentificationResult result)
    {
        Console.WriteLine("\n📊 Batch De-identification Summary:");
        Console.WriteLine($"   Total files: {result.Metadata.TotalFiles}");
        Console.WriteLine($"   Successful: {result.Metadata.SuccessfulFiles}");
        Console.WriteLine($"   Failed: {result.Metadata.FailedFiles}");
        
        if (result.CombinedStatistics != null)
        {
            Console.WriteLine($"   Total messages: {result.CombinedStatistics.TotalMessages}");
            Console.WriteLine($"   Fields modified: {result.CombinedStatistics.FieldsModified}");
            Console.WriteLine($"   Dates shifted: {result.CombinedStatistics.DatesShifted}");
            Console.WriteLine($"   Unique subjects: {result.CombinedStatistics.UniqueSubjects}");
        }
        
        Console.WriteLine($"   Total processing time: {result.Metadata.TotalProcessingTime.TotalSeconds:F1}s");
        
        if (result.BatchCompliance != null)
        {
            Console.WriteLine($"   HIPAA Safe Harbor compliant: {(result.BatchCompliance.MeetsSafeHarbor ? "✓" : "✗")}");
        }

        if (result.FileResults.Any(r => !r.Success))
        {
            Console.WriteLine("\n   ⚠️  Failed files:");
            foreach (var failure in result.FileResults.Where(r => !r.Success).Take(5))
            {
                Console.WriteLine($"     - {failure.InputPath}: {failure.ErrorMessage}");
            }
        }
    }
}