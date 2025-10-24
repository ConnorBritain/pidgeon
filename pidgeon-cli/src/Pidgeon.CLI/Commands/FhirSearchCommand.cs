// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Microsoft.Extensions.Logging;
using Pidgeon.Core.Application.Interfaces.Standards;
using Pidgeon.Core.Generation;
using System.CommandLine;
using System.Text.Json;

namespace Pidgeon.CLI.Commands;

/// <summary>
/// Command for testing FHIR search functionality using the search test harness.
/// Simulates FHIR server search queries with realistic results and clinical scenarios.
/// </summary>
public class FhirSearchCommand : CommandBuilderBase
{
    private readonly IFHIRSearchHarnessService _fhirSearchHarness;

    public FhirSearchCommand(
        ILogger<FhirSearchCommand> logger,
        IFHIRSearchHarnessService fhirSearchHarness) 
        : base(logger)
    {
        _fhirSearchHarness = fhirSearchHarness;
    }

    public override Command CreateCommand()
    {
        var command = new Command("fhir-search", "Test FHIR search functionality with realistic server simulation");

        // Add subcommands
        command.Add(CreateSearchCommand());
        command.Add(CreateScenarioCommand());

        return command;
    }

    /// <summary>
    /// Creates the 'search' subcommand for FHIR search queries.
    /// </summary>
    private Command CreateSearchCommand()
    {
        var command = new Command("search", "Execute FHIR search queries with _include parameters");

        // Arguments
        var resourceTypeArg = new Argument<string>("resource-type")
        {
            Description = "FHIR resource type to search (e.g., Patient, Observation, Encounter)"
        };

        // Options
        var parametersOption = CreateNullableOption("--params", "Search parameters as query string (e.g., \"name=Smith&birthdate=gt1990-01-01\")");
        var includeOption = CreateNullableOption("--include", "Include parameters (e.g., \"Patient:general-practitioner,Observation:patient\")");
        var revIncludeOption = CreateNullableOption("--revinclude", "Reverse include parameters (e.g., \"Observation:patient\")");
        var countOption = CreateIntegerOption("--count", "Maximum number of results", 10);
        var offsetOption = CreateIntegerOption("--offset", "Offset for pagination", 0);
        var outputOption = CreateNullableOption("--output", "Output file path (optional, defaults to console)");
        var seedOption = CreateNullableOption("--seed", "Deterministic seed for reproducible results");

        command.Add(resourceTypeArg);
        command.Add(parametersOption);
        command.Add(includeOption);
        command.Add(revIncludeOption);
        command.Add(countOption);
        command.Add(offsetOption);
        command.Add(outputOption);
        command.Add(seedOption);

        SetCommandAction(command, async (parseResult, cancellationToken) =>
        {
            try
            {
                var resourceType = parseResult.GetValue(resourceTypeArg)!;
                var parametersString = parseResult.GetValue(parametersOption);
                var includeString = parseResult.GetValue(includeOption);
                var revIncludeString = parseResult.GetValue(revIncludeOption);
                var count = parseResult.GetValue(countOption);
                var offset = parseResult.GetValue(offsetOption);
                var output = parseResult.GetValue(outputOption);
                var seedString = parseResult.GetValue(seedOption);

                Logger.LogInformation("Executing FHIR search for {ResourceType}", resourceType);

                // Parse search parameters
                var parameters = ParseSearchParameters(parametersString);
                var includes = ParseIncludeParameters(includeString);
                var revIncludes = ParseIncludeParameters(revIncludeString);

                // Parse seed if provided
                int? seed = null;
                if (!string.IsNullOrWhiteSpace(seedString) && int.TryParse(seedString, out var parsedSeed))
                {
                    seed = parsedSeed;
                }

                // Create search query
                var searchQuery = new FHIRSearchQuery
                {
                    Parameters = parameters,
                    Include = includes,
                    RevInclude = revIncludes,
                    Count = count,
                    Offset = offset
                };

                // Create generation options
                var options = new GenerationOptions { Seed = seed };

                // Execute search
                var result = await _fhirSearchHarness.ExecuteSearchAsync(resourceType, searchQuery, options);

                if (result.IsSuccess)
                {
                    // Format and output results
                    var formattedJson = FormatJson(result.Value);
                    
                    if (!string.IsNullOrWhiteSpace(output))
                    {
                        await File.WriteAllTextAsync(output, formattedJson);
                        Console.WriteLine($"FHIR search results written to: {output}");
                    }
                    else
                    {
                        Console.WriteLine(formattedJson);
                    }

                    // Display summary
                    var bundle = JsonSerializer.Deserialize<JsonElement>(result.Value);
                    if (bundle.TryGetProperty("total", out var totalElement))
                    {
                        Console.WriteLine($"\nSearch completed successfully. Total results: {totalElement.GetInt32()}");
                    }
                    
                    return 0;
                }
                else
                {
                    Logger.LogError("FHIR search failed: {Error}", result.Error);
                    Console.WriteLine($"Error: {result.Error}");
                    return 1;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to execute FHIR search command");
                Console.WriteLine($"Error: {ex.Message}");
                return 1;
            }
        });

        return command;
    }

    /// <summary>
    /// Creates the 'scenario' subcommand for clinical scenario generation.
    /// </summary>
    private Command CreateScenarioCommand()
    {
        var command = new Command("scenario", "Generate realistic clinical scenario bundles");

        // Arguments
        var scenarioTypeArg = new Argument<string>("scenario-type")
        {
            Description = "Clinical scenario type (AdmissionWithLabs, DiabetesManagement, EmergencyVisit, etc.)"
        };

        // Options  
        var outputOption = CreateNullableOption("--output", "Output file path (optional, defaults to console)");
        var seedOption = CreateNullableOption("--seed", "Deterministic seed for reproducible scenarios");

        command.Add(scenarioTypeArg);
        command.Add(outputOption);
        command.Add(seedOption);

        SetCommandAction(command, async (parseResult, cancellationToken) =>
        {
            try
            {
                var scenarioTypeString = parseResult.GetValue(scenarioTypeArg)!;
                var output = parseResult.GetValue(outputOption);
                var seedString = parseResult.GetValue(seedOption);

                Logger.LogInformation("Generating clinical scenario: {ScenarioType}", scenarioTypeString);

                // Parse scenario type
                if (!Enum.TryParse<ClinicalScenarioType>(scenarioTypeString, true, out var scenarioType))
                {
                    Console.WriteLine($"Invalid scenario type: {scenarioTypeString}");
                    Console.WriteLine("Valid options: AdmissionWithLabs, DiabetesManagement, EmergencyVisit, SurgicalCare, MedicationRefill, PreventiveCare, ChronicCareManagement, PediatricWellChild");
                    return 1;
                }

                // Parse seed if provided
                int? seed = null;
                if (!string.IsNullOrWhiteSpace(seedString) && int.TryParse(seedString, out var parsedSeed))
                {
                    seed = parsedSeed;
                }

                // Create generation options
                var options = new GenerationOptions { Seed = seed };

                // Generate scenario
                var result = await _fhirSearchHarness.GenerateClinicalScenarioAsync(scenarioType, options);

                if (result.IsSuccess)
                {
                    // Format and output results
                    var formattedJson = FormatJson(result.Value);
                    
                    if (!string.IsNullOrWhiteSpace(output))
                    {
                        await File.WriteAllTextAsync(output, formattedJson);
                        Console.WriteLine($"Clinical scenario written to: {output}");
                    }
                    else
                    {
                        Console.WriteLine(formattedJson);
                    }

                    // Display summary
                    var bundle = JsonSerializer.Deserialize<JsonElement>(result.Value);
                    if (bundle.TryGetProperty("entry", out var entryElement) && entryElement.ValueKind == JsonValueKind.Array)
                    {
                        Console.WriteLine($"\nScenario generated successfully. Resources included: {entryElement.GetArrayLength()}");
                    }
                    
                    return 0;
                }
                else
                {
                    Logger.LogError("Clinical scenario generation failed: {Error}", result.Error);
                    Console.WriteLine($"Error: {result.Error}");
                    return 1;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to execute clinical scenario command");
                Console.WriteLine($"Error: {ex.Message}");
                return 1;
            }
        });

        return command;
    }

    /// <summary>
    /// Parses search parameters from query string format.
    /// </summary>
    private static Dictionary<string, string> ParseSearchParameters(string? parametersString)
    {
        var parameters = new Dictionary<string, string>();
        
        if (string.IsNullOrWhiteSpace(parametersString))
            return parameters;

        foreach (var param in parametersString.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = param.Split('=', 2);
            if (parts.Length == 2)
            {
                parameters[parts[0]] = parts[1];
            }
        }

        return parameters;
    }

    /// <summary>
    /// Parses include parameters from comma-separated format.
    /// </summary>
    private static List<string> ParseIncludeParameters(string? includeString)
    {
        if (string.IsNullOrWhiteSpace(includeString))
            return new List<string>();

        return includeString.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .ToList();
    }

    /// <summary>
    /// Formats JSON for readable output.
    /// </summary>
    private static string FormatJson(string json)
    {
        try
        {
            var document = JsonDocument.Parse(json);
            return JsonSerializer.Serialize(document, new JsonSerializerOptions 
            { 
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }
        catch
        {
            return json; // Return original if formatting fails
        }
    }
}