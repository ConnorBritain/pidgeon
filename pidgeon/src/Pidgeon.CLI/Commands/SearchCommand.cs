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
/// Command for simulating FHIR search functionality with realistic server responses.
/// Provides searchset Bundles with proper pagination, _include parameters, and reference resolution.
/// </summary>
public class SearchCommand : CommandBuilderBase
{
    private readonly IFHIRSearchHarnessService _fhirSearchHarness;

    public SearchCommand(
        ILogger<SearchCommand> logger,
        IFHIRSearchHarnessService fhirSearchHarness) 
        : base(logger)
    {
        _fhirSearchHarness = fhirSearchHarness;
    }

    public override Command CreateCommand()
    {
        var command = new Command("search", "Simulate FHIR search queries with realistic server responses");

        // Arguments
        var resourceTypeArg = new Argument<string>("resource-type")
        {
            Description = "FHIR resource type to search (Patient, Observation, Encounter, Practitioner, etc.)"
        };

        // Options
        var paramsOption = CreateNullableOption("--params", "Search parameters as query string (e.g., \"name=Smith&birthdate=gt1990-01-01\")");
        var includeOption = CreateNullableOption("--include", "Include related resources (doctors, encounters, observations, everything)");
        var revIncludeOption = CreateNullableOption("--revinclude", "Reverse include parameters (e.g., \"Observation:patient\")");
        var countOption = CreateIntegerOption("--count", "Maximum number of results", 10);
        var offsetOption = CreateIntegerOption("--offset", "Offset for pagination", 0);
        var outputOption = CreateNullableOption("--output", "Output file path (optional, defaults to console)");
        var seedOption = CreateNullableOption("--seed", "Deterministic seed for reproducible results");
        var formatOption = CreateOptionalOption("--format", "Output format: json|ndjson", "json");

        command.Add(resourceTypeArg);
        command.Add(paramsOption);
        command.Add(includeOption);
        command.Add(revIncludeOption);
        command.Add(countOption);
        command.Add(offsetOption);
        command.Add(outputOption);
        command.Add(seedOption);
        command.Add(formatOption);

        SetCommandAction(command, async (parseResult, cancellationToken) =>
        {
            try
            {
                var resourceType = parseResult.GetValue(resourceTypeArg)!;
                var parametersString = parseResult.GetValue(paramsOption);
                var includeString = parseResult.GetValue(includeOption);
                var revIncludeString = parseResult.GetValue(revIncludeOption);
                var count = parseResult.GetValue(countOption);
                var offset = parseResult.GetValue(offsetOption);
                var output = parseResult.GetValue(outputOption);
                var seedString = parseResult.GetValue(seedOption);
                var format = parseResult.GetValue(formatOption);

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
                    // Format output based on requested format
                    var formattedOutput = FormatOutput(result.Value, format);
                    
                    if (!string.IsNullOrWhiteSpace(output))
                    {
                        await File.WriteAllTextAsync(output, formattedOutput);
                        Console.WriteLine($"✅ FHIR search results written to: {output}");
                    }
                    else
                    {
                        Console.WriteLine(formattedOutput);
                    }

                    // Display summary
                    DisplaySearchSummary(result.Value, resourceType, searchQuery);
                    
                    return 0;
                }
                else
                {
                    Logger.LogError("FHIR search failed: {Error}", result.Error);
                    Console.WriteLine($"❌ Error: {result.Error}");
                    return 1;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to execute FHIR search command");
                Console.WriteLine($"❌ Error: {ex.Message}");
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
    /// Parses include parameters from comma-separated format with smart aliases.
    /// </summary>
    private static List<string> ParseIncludeParameters(string? includeString)
    {
        if (string.IsNullOrWhiteSpace(includeString))
            return new List<string>();

        // Smart aliases for common includes
        var aliases = new Dictionary<string, string>
        {
            { "doctors", "Patient:general-practitioner" },
            { "practitioners", "Patient:general-practitioner" }, 
            { "encounters", "Patient:encounter" },
            { "observations", "Patient:observation" },
            { "everything", "Patient:general-practitioner,Patient:encounter" }
        };

        var includes = new List<string>();
        foreach (var item in includeString.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = item.Trim().ToLowerInvariant();
            if (aliases.ContainsKey(trimmed))
            {
                includes.AddRange(aliases[trimmed].Split(','));
            }
            else
            {
                includes.Add(item.Trim());
            }
        }

        return includes;
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
    /// Converts FHIR searchset Bundle to NDJSON format (each resource on separate line).
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
    /// Displays a summary of the search results.
    /// </summary>
    private static void DisplaySearchSummary(string bundleJson, string resourceType, FHIRSearchQuery searchQuery)
    {
        try
        {
            var bundle = JsonSerializer.Deserialize<JsonElement>(bundleJson);
            
            Console.WriteLine();
            Console.WriteLine($"🔍 Search: {resourceType}");
            
            if (bundle.TryGetProperty("total", out var totalElement))
            {
                Console.WriteLine($"📊 Total results: {totalElement.GetInt32()}");
            }

            if (searchQuery.Include.Any())
            {
                Console.WriteLine($"🔗 Included: {string.Join(", ", searchQuery.Include)}");
            }

            if (searchQuery.RevInclude.Any())
            {
                Console.WriteLine($"🔙 Reverse included: {string.Join(", ", searchQuery.RevInclude)}");
            }

            if (searchQuery.Parameters.Any())
            {
                Console.WriteLine($"📋 Parameters: {string.Join(", ", searchQuery.Parameters.Select(p => $"{p.Key}={p.Value}"))}");
            }

            // Resource breakdown
            if (bundle.TryGetProperty("entry", out var entryElement) && entryElement.ValueKind == JsonValueKind.Array)
            {
                var resourceCounts = new Dictionary<string, int>();
                foreach (var entry in entryElement.EnumerateArray())
                {
                    if (entry.TryGetProperty("resource", out var resource) && 
                        resource.TryGetProperty("resourceType", out var resourceTypeElement))
                    {
                        var type = resourceTypeElement.GetString() ?? "Unknown";
                        resourceCounts[type] = resourceCounts.GetValueOrDefault(type, 0) + 1;
                    }
                }

                if (resourceCounts.Count > 1)
                {
                    Console.WriteLine("📦 Resources returned:");
                    foreach (var kvp in resourceCounts.OrderBy(x => x.Key))
                    {
                        Console.WriteLine($"   • {kvp.Value} {kvp.Key}{(kvp.Value > 1 ? "s" : "")}");
                    }
                }
            }
        }
        catch
        {
            // Silent fail on summary - not critical
        }
    }
}