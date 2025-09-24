// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Microsoft.Extensions.Logging;
using Pidgeon.CLI.Services;
using Pidgeon.Core.Application.Interfaces;
using Pidgeon.Core.Application.Interfaces.Standards;
using Pidgeon.Core.Domain.Validation;
using System.CommandLine;

namespace Pidgeon.CLI.Commands;

/// <summary>
/// Command for validating healthcare messages.
/// </summary>
public class ValidateCommand : CommandBuilderBase
{
    private readonly IMessageValidationService _validationService;

    public ValidateCommand(
        ILogger<ValidateCommand> logger,
        IMessageValidationService validationService,
        FirstTimeUserService firstTimeUserService)
        : base(logger, firstTimeUserService)
    {
        _validationService = validationService;
    }

    public override Command CreateCommand()
    {
        var command = new Command("validate", "Validate healthcare messages against standards");

        // Positional arguments for files (supports multiple files)
        var filesArg = new Argument<string[]>("files")
        {
            Description = "Path(s) to message file(s) to validate (supports wildcards)",
            Arity = ArgumentArity.OneOrMore
        };

        // Options
        var modeOption = new Option<ValidationMode>("--mode", "-m")
        {
            Description = "Validation mode (Strict, Compatibility, Lenient)",
            DefaultValueFactory = _ => ValidationMode.Strict
        };
        var standardOption = CreateNullableOption("--standard", "-s", "Specific standard to validate against (auto-detect if not specified)");

        // Redundant option for backward compatibility and script usage
        var fileOption = CreateNullableOption("--file", "-f", "Path to file (redundant - use positional args instead)");

        command.Add(filesArg);
        command.Add(modeOption);
        command.Add(standardOption);
        command.Add(fileOption);

        SetCommandAction(command, async (parseResult, cancellationToken) =>
        {
            // Get files from positional args or fallback to --file option
            var files = parseResult.GetValue(filesArg);
            var fallbackFile = parseResult.GetValue(fileOption);

            // Support both patterns: positional args OR --file option
            if (files == null || files.Length == 0)
            {
                if (!string.IsNullOrEmpty(fallbackFile))
                {
                    files = new[] { fallbackFile };
                }
                else
                {
                    Console.WriteLine("Error: No files specified. Usage:");
                    Console.WriteLine("  pidgeon validate <file1> [file2] [...]");
                    Console.WriteLine("  pidgeon validate --file <file>");
                    return 1;
                }
            }

            var mode = parseResult.GetValue(modeOption);
            var standard = parseResult.GetValue(standardOption);

            // Validate all files
            int overallResult = 0;
            for (int i = 0; i < files.Length; i++)
            {
                var file = files[i];

                var fileCheck = ValidateFileExists(file);
                if (fileCheck != 0)
                {
                    overallResult = fileCheck;
                    continue;
                }

                if (files.Length > 1)
                {
                    Console.WriteLine($"\n[{i + 1}/{files.Length}] Validating: {file}");
                    Console.WriteLine(new string('-', 50));
                }

                Console.WriteLine($"Validating {file} with {mode} mode...");

                var content = await File.ReadAllTextAsync(file, cancellationToken);
                var result = await _validationService.ValidateAsync(content, standard, mode);

                if (result.IsSuccess)
                {
                    var validation = result.Value;
                    if (validation.IsValid)
                    {
                        Console.WriteLine("✅ Validation passed!");
                        var warnings = validation.Issues.Where(i => i.Severity == ValidationSeverity.Warning).ToList();
                        if (warnings.Any())
                        {
                            Console.WriteLine($"⚠️ Warnings: {warnings.Count}");
                            foreach (var warning in warnings)
                            {
                                Console.WriteLine($"  {warning.RuleId}: {warning.Message}");
                            }
                        }
                    }
                    else
                    {
                        var errors = validation.Issues.Where(i => i.Severity == ValidationSeverity.Error).ToList();
                        Console.Error.WriteLine($"❌ Validation failed with {errors.Count} error(s):");
                        foreach (var error in errors)
                        {
                            Console.Error.WriteLine($"  {error.RuleId}: {error.Message}");
                            if (!string.IsNullOrEmpty(error.Location))
                                Console.WriteLine($"    Location: {error.Location}");
                        }
                        overallResult = 1;
                    }
                }
                else
                {
                    Console.Error.WriteLine($"❌ Error validating {file}: {result.Error.Message}");
                    overallResult = 1;
                }
            }

            // Summary for multiple files
            if (files.Length > 1)
            {
                Console.WriteLine($"\n📋 Validation complete: {files.Length} file(s) processed");
                Console.WriteLine(overallResult == 0 ? "✅ All files passed validation" : "❌ Some files failed validation");
            }

            return overallResult;
        });

        return command;
    }
}