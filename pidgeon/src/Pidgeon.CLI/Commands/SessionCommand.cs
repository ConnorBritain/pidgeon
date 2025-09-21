// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Microsoft.Extensions.Logging;
using Pidgeon.Core.Application.Interfaces.Configuration;
using Pidgeon.Core.Domain.Configuration.Entities;
using Pidgeon.CLI.Services;
using System.CommandLine;
using System.Text.Json;

namespace Pidgeon.CLI.Commands;

/// <summary>
/// CLI command for importing and exporting lock sessions as templates.
/// Enables template marketplace functionality and workflow sharing.
/// </summary>
public class SessionCommand : CommandBuilderBase
{
    private readonly ISessionExportService _sessionExportService;
    private readonly ILockSessionService _lockSessionService;
    private readonly TemplateConfigHelper _templateConfigHelper;

    public SessionCommand(
        ILogger<SessionCommand> logger,
        ISessionExportService sessionExportService,
        ILockSessionService lockSessionService,
        TemplateConfigHelper templateConfigHelper)
        : base(logger)
    {
        _sessionExportService = sessionExportService;
        _lockSessionService = lockSessionService;
        _templateConfigHelper = templateConfigHelper;
    }

    public override Command CreateCommand()
    {
        var command = new Command("session", "Import and export lock sessions as templates for workflow sharing");

        // Subcommands
        command.Add(CreateExportCommand());
        command.Add(CreateImportCommand());
        command.Add(CreateValidateCommand());

        return command;
    }

    private Command CreateExportCommand()
    {
        var command = new Command("export", "Export a lock session as a template file");

        var nameArg = new Argument<string>("name")
        {
            Description = "Name of the lock session to export"
        };

        var outputOption = CreateNullableOption("--output", "Output file path (default: <session-name>.yaml)");
        var formatOption = CreateOptionalOption("--format", "Export format: yaml|json", "yaml");
        var templateOption = CreateBooleanOption("--template", "Include template metadata for marketplace sharing");
        var includeTimestampsOption = CreateBooleanOption("--include-timestamps", "Include creation and lock timestamps");
        var prettyOption = CreateBooleanOption("--pretty", "Format output for readability", true);

        // Template metadata options
        var authorOption = CreateNullableOption("--author", "Template author name");
        var descriptionOption = CreateNullableOption("--description", "Template description");
        var categoryOption = CreateNullableOption("--category", "Template category (emergency, surgical, lab, etc.)");
        var versionOption = CreateOptionalOption("--version", "Template version", "1.0.0");

        command.Add(nameArg);
        command.Add(outputOption);
        command.Add(formatOption);
        command.Add(templateOption);
        command.Add(includeTimestampsOption);
        command.Add(prettyOption);
        command.Add(authorOption);
        command.Add(descriptionOption);
        command.Add(categoryOption);
        command.Add(versionOption);

        SetCommandAction(command, async (parseResult, cancellationToken) =>
        {
            try
            {
                var sessionName = parseResult.GetValue(nameArg)!;
                var output = parseResult.GetValue(outputOption);
                var formatStr = parseResult.GetValue(formatOption)!;
                var isTemplate = parseResult.GetValue(templateOption);
                var includeTimestamps = parseResult.GetValue(includeTimestampsOption);
                var pretty = parseResult.GetValue(prettyOption);
                var authorFlag = parseResult.GetValue(authorOption);
                var description = parseResult.GetValue(descriptionOption);
                var categoryFlag = parseResult.GetValue(categoryOption);
                var version = parseResult.GetValue(versionOption)!;

                // Get smart defaults using config helper
                var format = await _templateConfigHelper.GetExportFormatAsync(formatStr);
                var author = await _templateConfigHelper.GetTemplateAuthorAsync(authorFlag, isTemplate);
                var category = await _templateConfigHelper.GetTemplateCategoryAsync(categoryFlag, isTemplate);

                // Build export options
                var options = new ExportOptions
                {
                    IncludeMetadata = isTemplate,
                    IncludeTimestamps = includeTimestamps,
                    PrettyFormat = pretty,
                    TemplateMetadata = isTemplate ? new TemplateMetadata
                    {
                        Name = sessionName,
                        Description = description ?? "",
                        Author = author ?? "",
                        Version = version,
                        Category = category ?? "",
                        Tags = [],
                        Verified = false
                    } : null
                };

                // Export session
                var result = await _sessionExportService.ExportSessionAsync(sessionName, format, options, cancellationToken);

                if (result.IsFailure)
                {
                    Console.WriteLine($"❌ Failed to export session: {result.Error.Message}");
                    return 1;
                }

                // Determine output file
                var outputFile = output ?? $"{sessionName}.{(format == ExportFormat.Json ? "json" : "yaml")}";

                // Write to file
                await File.WriteAllTextAsync(outputFile, result.Value, cancellationToken);

                Console.WriteLine($"✅ Exported session '{sessionName}' to {outputFile}");
                Console.WriteLine($"   Format: {format}");
                Console.WriteLine($"   Size: {result.Value.Length:N0} characters");

                if (isTemplate)
                {
                    Console.WriteLine($"   Template: Ready for marketplace sharing");
                    Console.WriteLine();
                    Console.WriteLine("💡 Share your template:");
                    Console.WriteLine($"   pidgeon template publish {outputFile}");
                }

                return 0;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Unexpected error exporting session");
                Console.WriteLine($"❌ Error: {ex.Message}");
                return 1;
            }
        });

        return command;
    }

    private Command CreateImportCommand()
    {
        var command = new Command("import", "Import a lock session from a template file");

        var fileArg = new Argument<string>("file")
        {
            Description = "Path to the template file to import"
        };

        var nameOption = CreateNullableOption("--name", "Name for the imported session (default: use name from file)");
        var overwriteOption = CreateBooleanOption("--overwrite", "Overwrite existing session with same name");
        var validateOption = CreateBooleanOption("--validate", "Validate template before import", true);
        var preserveTimestampsOption = CreateBooleanOption("--preserve-timestamps", "Preserve original timestamps from template");
        var dryRunOption = CreateBooleanOption("--dry-run", "Show what would be imported without creating session");

        command.Add(fileArg);
        command.Add(nameOption);
        command.Add(overwriteOption);
        command.Add(validateOption);
        command.Add(preserveTimestampsOption);
        command.Add(dryRunOption);

        SetCommandAction(command, async (parseResult, cancellationToken) =>
        {
            try
            {
                var filePath = parseResult.GetValue(fileArg)!;
                var name = parseResult.GetValue(nameOption);
                var overwrite = parseResult.GetValue(overwriteOption);
                var validate = parseResult.GetValue(validateOption);
                var preserveTimestamps = parseResult.GetValue(preserveTimestampsOption);
                var dryRun = parseResult.GetValue(dryRunOption);

                // Validate file exists
                if (!File.Exists(filePath))
                {
                    Console.WriteLine($"❌ File not found: {filePath}");
                    return 1;
                }

                // Build import options
                var options = new ImportOptions
                {
                    SessionName = name,
                    OverwriteExisting = overwrite,
                    ValidateBeforeImport = validate,
                    PreserveTimestamps = preserveTimestamps
                };

                if (dryRun)
                {
                    // Validate only, don't import
                    var sessionData = await File.ReadAllTextAsync(filePath, cancellationToken);
                    var validationResult = await _sessionExportService.ValidateSessionDataAsync(sessionData);

                    if (validationResult.IsFailure)
                    {
                        Console.WriteLine($"❌ Validation failed: {validationResult.Error.Message}");
                        return 1;
                    }

                    var validation = validationResult.Value;
                    Console.WriteLine($"📋 Dry Run - Template Analysis");
                    Console.WriteLine($"   File: {filePath}");
                    Console.WriteLine($"   Format: {validation.DetectedFormat}");
                    Console.WriteLine($"   Valid: {(validation.IsValid ? "✅ Yes" : "❌ No")}");

                    if (validation.Errors.Any())
                    {
                        Console.WriteLine("   Errors:");
                        foreach (var error in validation.Errors)
                        {
                            Console.WriteLine($"     • {error}");
                        }
                    }

                    if (validation.Warnings.Any())
                    {
                        Console.WriteLine("   Warnings:");
                        foreach (var warning in validation.Warnings)
                        {
                            Console.WriteLine($"     • {warning}");
                        }
                    }

                    return validation.IsValid ? 0 : 1;
                }

                // Import session
                var result = await _sessionExportService.ImportSessionFromFileAsync(filePath, options, cancellationToken);

                if (result.IsFailure)
                {
                    Console.WriteLine($"❌ Failed to import session: {result.Error.Message}");
                    return 1;
                }

                var session = result.Value;
                Console.WriteLine($"✅ Imported session: {session.Name}");
                Console.WriteLine($"   Session ID: {session.SessionId}");
                Console.WriteLine($"   Scope: {session.Scope}");
                Console.WriteLine($"   Locked Values: {session.LockedValues.Count}");
                Console.WriteLine($"   File: {filePath}");

                if (!string.IsNullOrEmpty(session.Description))
                {
                    Console.WriteLine($"   Description: {session.Description}");
                }

                Console.WriteLine();
                Console.WriteLine("💡 Next steps:");
                Console.WriteLine($"   pidgeon lock show {session.Name}");
                Console.WriteLine($"   pidgeon generate \"ADT^A01\" --use-lock {session.Name}");

                return 0;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Unexpected error importing session");
                Console.WriteLine($"❌ Error: {ex.Message}");
                return 1;
            }
        });

        return command;
    }

    private Command CreateValidateCommand()
    {
        var command = new Command("validate", "Validate a template file without importing");

        var fileArg = new Argument<string>("file")
        {
            Description = "Path to the template file to validate"
        };

        var formatOption = CreateNullableOption("--format", "Expected format: yaml|json (auto-detect if not specified)");
        var detailedOption = CreateBooleanOption("--detailed", "Show detailed validation information");

        command.Add(fileArg);
        command.Add(formatOption);
        command.Add(detailedOption);

        SetCommandAction(command, async (parseResult, cancellationToken) =>
        {
            try
            {
                var filePath = parseResult.GetValue(fileArg)!;
                var formatStr = parseResult.GetValue(formatOption);
                var detailed = parseResult.GetValue(detailedOption);

                // Validate file exists
                if (!File.Exists(filePath))
                {
                    Console.WriteLine($"❌ File not found: {filePath}");
                    return 1;
                }

                ExportFormat? expectedFormat = null;
                if (!string.IsNullOrEmpty(formatStr))
                {
                    if (!Enum.TryParse<ExportFormat>(formatStr, true, out var format))
                    {
                        Console.WriteLine($"❌ Invalid format: {formatStr}");
                        Console.WriteLine("Valid formats: yaml, json");
                        return 1;
                    }
                    expectedFormat = format;
                }

                // Read and validate file
                var sessionData = await File.ReadAllTextAsync(filePath, cancellationToken);
                var result = await _sessionExportService.ValidateSessionDataAsync(sessionData, expectedFormat);

                if (result.IsFailure)
                {
                    Console.WriteLine($"❌ Validation failed: {result.Error.Message}");
                    return 1;
                }

                var validation = result.Value;

                Console.WriteLine($"📋 Template Validation: {filePath}");
                Console.WriteLine($"   Format: {validation.DetectedFormat?.ToString() ?? "Unknown"}");
                Console.WriteLine($"   Valid: {(validation.IsValid ? "✅ Yes" : "❌ No")}");
                Console.WriteLine($"   Size: {sessionData.Length:N0} characters");

                if (validation.Errors.Any())
                {
                    Console.WriteLine();
                    Console.WriteLine($"❌ Errors ({validation.Errors.Count}):");
                    foreach (var error in validation.Errors)
                    {
                        Console.WriteLine($"   • {error}");
                    }
                }

                if (validation.Warnings.Any())
                {
                    Console.WriteLine();
                    Console.WriteLine($"⚠️  Warnings ({validation.Warnings.Count}):");
                    foreach (var warning in validation.Warnings)
                    {
                        Console.WriteLine($"   • {warning}");
                    }
                }

                if (validation.IsValid)
                {
                    Console.WriteLine();
                    Console.WriteLine("✅ Template is ready for import");
                    Console.WriteLine($"   pidgeon session import {filePath}");
                }

                return validation.IsValid ? 0 : 1;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Unexpected error validating template");
                Console.WriteLine($"❌ Error: {ex.Message}");
                return 1;
            }
        });

        return command;
    }
}