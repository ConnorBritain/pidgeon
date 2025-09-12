// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Microsoft.Extensions.Logging;
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
        IMessageValidationService validationService) 
        : base(logger)
    {
        _validationService = validationService;
    }

    public override Command CreateCommand()
    {
        var command = new Command("validate", "Validate healthcare messages against standards");

        var fileOption = CreateRequiredOption("--file", "Path to the message file to validate");
        var modeOption = new Option<ValidationMode>("--mode")
        {
            Description = "Validation mode (Strict, Compatibility, Lenient)",
            DefaultValueFactory = _ => ValidationMode.Strict
        };
        var standardOption = CreateNullableOption("--standard", "Specific standard to validate against (auto-detect if not specified)");

        command.Add(fileOption);
        command.Add(modeOption);
        command.Add(standardOption);

        SetCommandAction(command, async (parseResult, cancellationToken) =>
        {
            var file = parseResult.GetValue(fileOption);
            var mode = parseResult.GetValue(modeOption);
            var standard = parseResult.GetValue(standardOption);

            var fileCheck = ValidateFileExists(file!);
            if (fileCheck != 0) return fileCheck;

            Console.WriteLine($"Validating {file} with {mode} mode...");
            
            var content = await File.ReadAllTextAsync(file!, cancellationToken);
            var result = await _validationService.ValidateAsync(content, standard, mode);
            
            if (result.IsSuccess)
            {
                var validation = result.Value;
                if (validation.IsValid)
                {
                    Console.WriteLine("Validation passed!");
                    var warnings = validation.Issues.Where(i => i.Severity == ValidationSeverity.Warning).ToList();
                    if (warnings.Any())
                    {
                        Console.WriteLine($"Warnings: {warnings.Count}");
                        foreach (var warning in warnings)
                        {
                            Console.WriteLine($"{warning.RuleId}: {warning.Message}");
                        }
                    }
                }
                else
                {
                    var errors = validation.Issues.Where(i => i.Severity == ValidationSeverity.Error).ToList();
                    Console.Error.WriteLine($"Validation failed with {errors.Count} error(s):");
                    foreach (var error in errors)
                    {
                        Console.Error.WriteLine($"  {error.RuleId}: {error.Message}");
                        if (!string.IsNullOrEmpty(error.Location))
                            Console.WriteLine($"    Location: {error.Location}");
                    }
                    return 1;
                }
            }
            
            return HandleResult(result);
        });

        return command;
    }
}