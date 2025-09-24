// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Microsoft.Extensions.Logging;
using Pidgeon.CLI.Services;
using Pidgeon.Core;
using Pidgeon.Core.Application.Services.Generation;
using Pidgeon.Core.Application.Interfaces.Generation;
using Pidgeon.Core.Generation;
using System.CommandLine;

namespace Pidgeon.CLI.Commands;

/// <summary>
/// Command for generating healthcare messages and synthetic test data.
/// </summary>
public class GenerateCommand : CommandBuilderBase
{
    private readonly IMessageGenerationService _messageGenerationService;
    private readonly SessionHelper _sessionHelper;

    public GenerateCommand(
        ILogger<GenerateCommand> logger,
        IMessageGenerationService messageGenerationService,
        SessionHelper sessionHelper,
        FirstTimeUserService firstTimeUserService)
        : base(logger, firstTimeUserService)
    {
        _messageGenerationService = messageGenerationService;
        _sessionHelper = sessionHelper;
    }

    public override Command CreateCommand()
    {
        var command = new Command("generate", "Generate synthetic messages/resources with smart standard inference");

        // Positional arguments for smart inference: pidgeon generate <message-type> or pidgeon generate <standard> <message-type>
        var messageTypeArg = new Argument<string[]>("message-types")
        {
            Description = "Message type (e.g., ADT^A01, Patient, NewRx) - supports 239+ HL7/FHIR/NCPDP types",
            Arity = ArgumentArity.OneOrMore
        };

        // Add smart completions that work during tab completion but don't affect help display
        HealthcareCompletions.AddSmartCompletionsToArgument(messageTypeArg);
        
        // Options
        var countOption = CreateIntegerOption("--count", "Number of messages to generate", 1);
        var outputOption = CreateNullableOption("--output", "Output file path (optional, defaults to console)");
        var formatOption = CreateOptionalOption("--format", "Output format: auto|hl7|json|ndjson", "auto");
        HealthcareCompletions.AddFormatCompletions(formatOption);

        var modeOption = CreateOptionalOption("--mode", "Generation mode: procedural|local-ai|api-ai", "procedural");
        HealthcareCompletions.AddGenerationModeCompletions(modeOption);
        var seedOption = CreateNullableOption("--seed", "Deterministic seed for reproducible data");
        var vendorOption = CreateNullableOption("--vendor", "Apply vendor pattern (e.g., epic, cerner, meditech)");
        HealthcareCompletions.AddVendorCompletions(vendorOption);
        var sessionOption = CreateNullableOption("--session", "Use specific session (overrides current session)");
        var noSessionOption = CreateBooleanOption("--no-session", "Force pure random generation (ignore current session)");

        command.Add(messageTypeArg);
        command.Add(countOption);
        command.Add(outputOption);
        command.Add(formatOption);
        command.Add(modeOption);
        command.Add(seedOption);
        command.Add(vendorOption);
        command.Add(sessionOption);
        command.Add(noSessionOption);

        SetCommandAction(command, async (parseResult, cancellationToken) =>
        {
            try
            {
                // Parse smart inference arguments
                var args = parseResult.GetValue(messageTypeArg) ?? Array.Empty<string>();
                if (args.Length == 0)
                {
                    Logger.LogError("Message type is required. Usage: pidgeon generate <message-type> or pidgeon generate <standard> <message-type>");
                    Console.WriteLine("Examples:");
                    Console.WriteLine("  pidgeon generate \"ADT^A01\"");
                    Console.WriteLine("  pidgeon generate Patient --count 10");
                    Console.WriteLine("  pidgeon generate hl7 \"ADT^A01\"");
                    return 1;
                }

                var parsingResult = SmartCommandParser.Parse(args);
                if (parsingResult.IsFailure)
                {
                    Logger.LogError(parsingResult.Error.Message);
                    Console.WriteLine($"❌ {parsingResult.Error.Message}");
                    Console.WriteLine("Examples:");
                    Console.WriteLine("  pidgeon generate \"ADT^A01\"");
                    Console.WriteLine("  pidgeon generate Patient --count 10");
                    Console.WriteLine("  pidgeon generate hl7 \"ADT^A01\"");
                    return 1;
                }
                
                var request = parsingResult.Value;

                var count = parseResult.GetValue(countOption);
                var output = parseResult.GetValue(outputOption);
                var format = parseResult.GetValue(formatOption);
                var mode = parseResult.GetValue(modeOption);
                var seedStr = parseResult.GetValue(seedOption);
                int? seed = null;
                if (!string.IsNullOrEmpty(seedStr) && int.TryParse(seedStr, out var parsedSeed))
                {
                    seed = parsedSeed;
                }
                var vendor = parseResult.GetValue(vendorOption);
                var sessionOverride = parseResult.GetValue(sessionOption);
                var noSession = parseResult.GetValue(noSessionOption);

                // Determine which session to use (smart session management)
                string? sessionToUse = await DetermineSessionAsync(sessionOverride, noSession, cancellationToken);

                // Validate Pro features if needed
                if ((mode == "local-ai" || mode == "api-ai") && !IsProFeatureAvailable())
                {
                    Console.WriteLine("AI generation modes require Pidgeon Pro. Using procedural mode.");
                    Console.WriteLine("Upgrade at: pidgeon login --pro");
                    mode = "procedural";
                }

                Console.WriteLine($"Generating {count} {request.Standard.ToUpperInvariant()} {request.MessageType} message(s)...");
                if (request.ExplicitStandardProvided)
                {
                    Console.WriteLine($"Standard: {request.Standard} (explicit)");
                }
                else
                {
                    Console.WriteLine($"Standard: {request.Standard} (inferred from message type)");
                }

                if (!string.IsNullOrEmpty(sessionToUse))
                {
                    Console.WriteLine($"🔒 Using session: {sessionToUse}");
                }
            
                var options = new GenerationOptions
                {
                    UseAI = mode != "procedural",
                    Seed = seed,
                    LockSessionName = sessionToUse,
                    // TODO: Map vendor string to VendorProfile enum
                    VendorProfile = null
                };
                
                var result = await _messageGenerationService.GenerateSyntheticDataAsync(request.Standard, request.MessageType, count, options);
            
                if (result.IsSuccess)
                {
                    if (!string.IsNullOrEmpty(output))
                    {
                        // Write to file
                        await WriteToFileAsync(output, result.Value, format!);
                        Console.WriteLine($"✅ Generated {count} {request.MessageType} message(s) and saved to {output}");
                    }
                    else
                    {
                        // Write to console  
                        WriteToConsole(result.Value, format!);
                        Console.WriteLine($"✅ Generated {count} {request.MessageType} message(s)");
                    }
                    
                    return 0;
                }
                
                return HandleResult(result);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Unexpected error during message generation");
                Console.WriteLine($"❌ Error: {ex.Message}");
                return 1;
            }
        });

        return command;
    }

    private async Task WriteToFileAsync(string filePath, IReadOnlyList<string> messages, string format)
    {
        var content = string.Join(Environment.NewLine + Environment.NewLine, messages);
        await File.WriteAllTextAsync(filePath, content);
        
        Logger.LogInformation("Generated messages written to {FilePath}", filePath);
    }

    private void WriteToConsole(IReadOnlyList<string> messages, string format)
    {
        foreach (var message in messages)
        {
            if (format != "auto")
            {
                // Add some formatting for readability
                Console.WriteLine(new string('-', 50));
            }
            
            Console.WriteLine(message);
            
            if (format != "auto" && messages.Count > 1)
            {
                Console.WriteLine(new string('-', 50));
                Console.WriteLine();
            }
        }
    }

    private async Task<string?> DetermineSessionAsync(string? sessionOverride, bool noSession, CancellationToken cancellationToken)
    {
        if (noSession)
            return null;

        if (!string.IsNullOrEmpty(sessionOverride))
            return sessionOverride;

        return await _sessionHelper.GetCurrentSessionAsync(cancellationToken);
    }

    private static bool IsProFeatureAvailable()
    {
        // TODO: Replace with actual license validation
        return false;
    }
}