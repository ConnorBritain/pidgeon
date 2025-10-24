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
/// Command for generating realistic clinical scenario bundles.
/// Creates multi-resource FHIR workflows representing common healthcare patterns.
/// </summary>
public class ScenarioCommand : CommandBuilderBase
{
    private readonly IFHIRSearchHarnessService _fhirSearchHarness;

    public ScenarioCommand(
        ILogger<ScenarioCommand> logger,
        IFHIRSearchHarnessService fhirSearchHarness) 
        : base(logger)
    {
        _fhirSearchHarness = fhirSearchHarness;
    }

    public override Command CreateCommand()
    {
        var command = new Command("scenario", "Generate realistic clinical scenario bundles");

        // Arguments
        var scenarioTypeArg = new Argument<string>("scenario-type")
        {
            Description = "Clinical scenario type (admission-with-labs, diabetes-management, emergency-visit, etc.)"
        };

        // Options with short flags
        var outputOption = CreateNullableOption("--output", "-o", "Output file path (optional, defaults to console)");
        var seedOption = CreateNullableOption("--seed", "-s", "Deterministic seed for reproducible scenarios");
        var formatOption = CreateOptionalOption("--format", "-f", "Output format: json|ndjson", "json");
        var standardOption = CreateOptionalOption("--standard", "-t", "Healthcare standard: fhir|hl7|ncpdp", "fhir");

        command.Add(scenarioTypeArg);
        command.Add(outputOption);
        command.Add(seedOption);
        command.Add(formatOption);
        command.Add(standardOption);

        SetCommandAction(command, async (parseResult, cancellationToken) =>
        {
            try
            {
                var scenarioTypeString = parseResult.GetValue(scenarioTypeArg)!;
                var output = parseResult.GetValue(outputOption);
                var seedString = parseResult.GetValue(seedOption);
                var format = parseResult.GetValue(formatOption);
                var standard = parseResult.GetValue(standardOption);

                Logger.LogInformation("Generating clinical scenario: {ScenarioType} ({Standard})", scenarioTypeString, standard);

                // Validate standard (currently only FHIR supported)
                if (standard != "fhir")
                {
                    Console.WriteLine($"❌ Standard '{standard}' not yet supported for clinical scenarios.");
                    Console.WriteLine("Currently supported: fhir (default)");
                    Console.WriteLine("Coming soon: hl7 (message sequences), ncpdp (prescription workflows)");
                    return 1;
                }

                // Parse scenario type
                var scenarioType = MapToScenarioType(scenarioTypeString);
                if (scenarioType == null)
                {
                    Console.WriteLine($"❌ Unknown clinical scenario: {scenarioTypeString}");
                    Console.WriteLine();
                    Console.WriteLine("Available scenarios:");
                    Console.WriteLine("  admission-with-labs      Patient admission with lab orders and results");
                    Console.WriteLine("  diabetes-management      Diabetes patient with monitoring and medication");
                    Console.WriteLine("  emergency-visit          Emergency department visit with multiple observations");
                    Console.WriteLine("  surgical-care            Surgical procedure with pre/post-op care");
                    Console.WriteLine("  medication-refill        Outpatient medication refill workflow");
                    Console.WriteLine("  preventive-care          Preventive care visit with screenings");
                    Console.WriteLine("  chronic-care-management  Chronic disease management with care team");
                    Console.WriteLine("  pediatric-well-child     Pediatric well-child visit with development tracking");
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

                // Generate clinical scenario
                var result = await _fhirSearchHarness.GenerateClinicalScenarioAsync(scenarioType.Value, options);

                if (result.IsSuccess)
                {
                    // Format output based on requested format
                    var formattedOutput = FormatOutput(result.Value, format!);
                    
                    if (!string.IsNullOrWhiteSpace(output))
                    {
                        await File.WriteAllTextAsync(output, formattedOutput);
                        Console.WriteLine($"✅ Clinical scenario written to: {output}");
                    }
                    else
                    {
                        Console.WriteLine(formattedOutput);
                    }

                    // Display summary
                    DisplayScenarioSummary(result.Value, scenarioTypeString);
                    
                    return 0;
                }
                else
                {
                    Logger.LogError("Clinical scenario generation failed: {Error}", result.Error);
                    Console.WriteLine($"❌ Error: {result.Error}");
                    return 1;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to execute clinical scenario command");
                Console.WriteLine($"❌ Error: {ex.Message}");
                return 1;
            }
        });

        return command;
    }

    /// <summary>
    /// Maps scenario name to ClinicalScenarioType enum.
    /// </summary>
    private static ClinicalScenarioType? MapToScenarioType(string scenarioName)
    {
        return scenarioName.Replace("-", "").Replace("_", "").ToLowerInvariant() switch
        {
            "admissionwithlabs" => ClinicalScenarioType.AdmissionWithLabs,
            "diabetesmanagement" => ClinicalScenarioType.DiabetesManagement,
            "emergencyvisit" => ClinicalScenarioType.EmergencyVisit,
            "surgicalcare" => ClinicalScenarioType.SurgicalCare,
            "medicationrefill" => ClinicalScenarioType.MedicationRefill,
            "preventivecare" => ClinicalScenarioType.PreventiveCare,
            "chroniccaremanagement" => ClinicalScenarioType.ChronicCareManagement,
            "pediatricwellchild" => ClinicalScenarioType.PediatricWellChild,
            _ => null
        };
    }

    /// <summary>
    /// Formats output based on requested format.
    /// </summary>
    private static string FormatOutput(string json, string format)
    {
        return format.ToLowerInvariant() switch
        {
            "ndjson" => ConvertToNDJSON(json),
            "json" => FormatJson(json),
            _ => json
        };
    }

    /// <summary>
    /// Converts FHIR Bundle to NDJSON format (each resource on separate line).
    /// </summary>
    private static string ConvertToNDJSON(string bundleJson)
    {
        try
        {
            var bundle = JsonSerializer.Deserialize<JsonElement>(bundleJson);
            if (bundle.TryGetProperty("entry", out var entries) && entries.ValueKind == JsonValueKind.Array)
            {
                var lines = new List<string>();
                foreach (var entry in entries.EnumerateArray())
                {
                    if (entry.TryGetProperty("resource", out var resource))
                    {
                        lines.Add(JsonSerializer.Serialize(resource, new JsonSerializerOptions { WriteIndented = false }));
                    }
                }
                return string.Join("\n", lines);
            }
        }
        catch
        {
            // Fall back to original if conversion fails
        }
        
        return bundleJson;
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

    /// <summary>
    /// Displays a summary of the generated scenario.
    /// </summary>
    private static void DisplayScenarioSummary(string bundleJson, string scenarioType)
    {
        try
        {
            var bundle = JsonSerializer.Deserialize<JsonElement>(bundleJson);
            if (bundle.TryGetProperty("entry", out var entryElement) && entryElement.ValueKind == JsonValueKind.Array)
            {
                var resourceCounts = new Dictionary<string, int>();
                foreach (var entry in entryElement.EnumerateArray())
                {
                    if (entry.TryGetProperty("resource", out var resource) && 
                        resource.TryGetProperty("resourceType", out var resourceType))
                    {
                        var type = resourceType.GetString() ?? "Unknown";
                        resourceCounts[type] = resourceCounts.GetValueOrDefault(type, 0) + 1;
                    }
                }

                Console.WriteLine();
                Console.WriteLine($"📋 Scenario: {scenarioType}");
                Console.WriteLine($"📦 Resources generated: {entryElement.GetArrayLength()}");
                
                foreach (var kvp in resourceCounts.OrderBy(x => x.Key))
                {
                    Console.WriteLine($"   • {kvp.Value} {kvp.Key}{(kvp.Value > 1 ? "s" : "")}");
                }
            }
        }
        catch
        {
            // Silent fail on summary - not critical
        }
    }
}