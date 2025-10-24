// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Microsoft.Extensions.Logging;
using Pidgeon.Core.Application.Interfaces.Configuration;
using System.CommandLine;
using System.Text.Json;

namespace Pidgeon.CLI.Commands;

/// <summary>
/// CLI command for semantic path discovery and exploration.
/// Enables users to discover, understand, and validate cross-standard field paths.
/// </summary>
public class PathCommand : CommandBuilderBase
{
    private readonly IFieldPathResolver _fieldPathResolver;
    private readonly IConfigurationService _configurationService;

    public PathCommand(
        ILogger<PathCommand> logger,
        IFieldPathResolver fieldPathResolver,
        IConfigurationService configurationService)
        : base(logger)
    {
        _fieldPathResolver = fieldPathResolver;
        _configurationService = configurationService;
    }

    public override Command CreateCommand()
    {
        var command = new Command("path", "Discover and explore semantic field paths across healthcare standards");

        // Subcommands
        command.Add(CreateListCommand());
        command.Add(CreateResolveCommand());
        command.Add(CreateValidateCommand());
        command.Add(CreateSearchCommand());

        return command;
    }

    private Command CreateListCommand()
    {
        var command = new Command("list", "Discover available semantic paths for a message type");

        var messageTypeArg = new Argument<string?>("message-type")
        {
            Description = "HL7 message (ADT^A01), FHIR resource (Patient), or NCPDP type (NewRx). If omitted, shows universal paths.",
            Arity = ArgumentArity.ZeroOrOne
        };

        var standardOption = CreateNullableOption("--standard", "Show paths for specific standard (hl7v23|fhirv4|ncpdp)");
        var categoryOption = CreateNullableOption("--category", "Filter by category: patient|encounter|provider|medication|all");
        var formatOption = CreateOptionalOption("--format", "Output format: table|json|csv", "table");
        var descriptionsOption = CreateBooleanOption("--descriptions", "Include field descriptions (verbose)");
        var examplesOption = CreateBooleanOption("--examples", "Show example values for each path");
        var outputOption = CreateNullableOption("--output", "Write results to file (default: stdout)");

        command.Add(messageTypeArg);
        command.Add(standardOption);
        command.Add(categoryOption);
        command.Add(formatOption);
        command.Add(descriptionsOption);
        command.Add(examplesOption);
        command.Add(outputOption);

        SetCommandAction(command, async (parseResult, cancellationToken) =>
        {
            try
            {
                var messageType = parseResult.GetValue(messageTypeArg);
                var standard = parseResult.GetValue(standardOption);
                var category = parseResult.GetValue(categoryOption);
                var format = parseResult.GetValue(formatOption)!;
                var descriptions = parseResult.GetValue(descriptionsOption);
                var examples = parseResult.GetValue(examplesOption);
                var output = parseResult.GetValue(outputOption);

                if (string.IsNullOrWhiteSpace(messageType))
                {
                    return await ShowUniversalPathsAsync(format, category, descriptions, examples, output, cancellationToken);
                }

                return await ShowMessageTypePathsAsync(messageType, standard, category, format, descriptions, examples, output, cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Unexpected error listing paths");
                Console.WriteLine($"❌ Error: {ex.Message}");
                return 1;
            }
        });

        return command;
    }

    private Command CreateResolveCommand()
    {
        var command = new Command("resolve", "Show how a semantic path maps to standard-specific field locations");

        var semanticPathArg = new Argument<string>("semantic-path")
        {
            Description = "Semantic path to resolve (e.g., patient.mrn, encounter.location)"
        };

        var messageTypeArg = new Argument<string>("message-type")
        {
            Description = "Target message type (ADT^A01, Patient, NewRx)"
        };

        var standardOption = CreateNullableOption("--standard", "Show mapping for specific standard only");
        var allStandardsOption = CreateBooleanOption("--all-standards", "Show mappings across all supported standards");
        var formatOption = CreateOptionalOption("--format", "Output format: table|json", "table");
        var detailedOption = CreateBooleanOption("--detailed", "Include field type, validation rules, and examples");
        var pathOnlyOption = CreateBooleanOption("--path-only", "Output only the resolved path (useful for scripting)");

        command.Add(semanticPathArg);
        command.Add(messageTypeArg);
        command.Add(standardOption);
        command.Add(allStandardsOption);
        command.Add(formatOption);
        command.Add(detailedOption);
        command.Add(pathOnlyOption);

        SetCommandAction(command, async (parseResult, cancellationToken) =>
        {
            try
            {
                var semanticPath = parseResult.GetValue(semanticPathArg)!;
                var messageType = parseResult.GetValue(messageTypeArg)!;
                var standard = parseResult.GetValue(standardOption);
                var allStandards = parseResult.GetValue(allStandardsOption);
                var format = parseResult.GetValue(formatOption)!;
                var detailed = parseResult.GetValue(detailedOption);
                var pathOnly = parseResult.GetValue(pathOnlyOption);

                return await ResolveSemanticPathAsync(semanticPath, messageType, standard, allStandards, format, detailed, pathOnly, cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Unexpected error resolving path");
                Console.WriteLine($"❌ Error: {ex.Message}");
                return 1;
            }
        });

        return command;
    }

    private Command CreateValidateCommand()
    {
        var command = new Command("validate", "Check if a semantic path is valid for a given message type");

        var semanticPathArg = new Argument<string>("semantic-path")
        {
            Description = "Path to validate (e.g., medication.dosage)"
        };

        var messageTypeArg = new Argument<string>("message-type")
        {
            Description = "Target message type (ADT^A01, Patient, etc.)"
        };

        var standardOption = CreateNullableOption("--standard", "Validate against specific standard");
        var suggestionsOption = CreateBooleanOption("--suggestions", "Show related/alternative paths when invalid", true);
        var formatOption = CreateOptionalOption("--format", "Output format: text|json", "text");

        command.Add(semanticPathArg);
        command.Add(messageTypeArg);
        command.Add(standardOption);
        command.Add(suggestionsOption);
        command.Add(formatOption);

        SetCommandAction(command, async (parseResult, cancellationToken) =>
        {
            try
            {
                var semanticPath = parseResult.GetValue(semanticPathArg)!;
                var messageType = parseResult.GetValue(messageTypeArg)!;
                var standard = parseResult.GetValue(standardOption);
                var suggestions = parseResult.GetValue(suggestionsOption);
                var format = parseResult.GetValue(formatOption)!;

                return await ValidateSemanticPathAsync(semanticPath, messageType, standard, suggestions, format, cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Unexpected error validating path");
                Console.WriteLine($"❌ Error: {ex.Message}");
                return 1;
            }
        });

        return command;
    }

    private Command CreateSearchCommand()
    {
        var command = new Command("search", "Find semantic paths by keyword or description");

        var queryArg = new Argument<string>("query")
        {
            Description = "Search term (e.g., \"phone\", \"date\", \"medical record\")"
        };

        var messageTypeOption = CreateNullableOption("--message-type", "Limit search to specific message type");
        var standardOption = CreateNullableOption("--standard", "Limit search to specific standard");
        var categoryOption = CreateNullableOption("--category", "Limit search to category (patient|encounter|provider|medication)");
        var exactOption = CreateBooleanOption("--exact", "Exact match only (no fuzzy search)");
        var formatOption = CreateOptionalOption("--format", "Output format: table|json", "table");

        command.Add(queryArg);
        command.Add(messageTypeOption);
        command.Add(standardOption);
        command.Add(categoryOption);
        command.Add(exactOption);
        command.Add(formatOption);

        SetCommandAction(command, async (parseResult, cancellationToken) =>
        {
            try
            {
                var query = parseResult.GetValue(queryArg)!;
                var messageType = parseResult.GetValue(messageTypeOption);
                var standard = parseResult.GetValue(standardOption);
                var category = parseResult.GetValue(categoryOption);
                var exact = parseResult.GetValue(exactOption);
                var format = parseResult.GetValue(formatOption)!;

                return await SearchSemanticPathsAsync(query, messageType, standard, category, exact, format, cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Unexpected error searching paths");
                Console.WriteLine($"❌ Error: {ex.Message}");
                return 1;
            }
        });

        return command;
    }

    private Task<int> ShowUniversalPathsAsync(
        string format,
        string? category,
        bool descriptions,
        bool examples,
        string? output,
        CancellationToken cancellationToken)
    {
        Console.WriteLine("🌐 Universal Semantic Paths (All Standards)");
        Console.WriteLine();
        Console.WriteLine("📋 Common semantic paths that work across HL7, FHIR, and NCPDP:");
        Console.WriteLine();

        // TODO: Implement universal path listing
        // For now, show a sample of common paths
        var universalPaths = new[]
        {
            ("patient.mrn", "Medical Record Number"),
            ("patient.lastName", "Patient Last Name"),
            ("patient.firstName", "Patient First Name"),
            ("patient.dateOfBirth", "Date of Birth"),
            ("patient.sex", "Administrative Sex"),
            ("patient.phoneNumber", "Primary Phone Number"),
            ("message.sendingApplication", "Sending Application"),
            ("message.sendingFacility", "Sending Facility"),
            ("message.timestamp", "Message Date/Time")
        };

        Console.WriteLine("PATIENT DEMOGRAPHICS");
        foreach (var (path, desc) in universalPaths.Where(p => p.Item1.StartsWith("patient.")))
        {
            if (descriptions)
                Console.WriteLine($"  {path,-25} {desc}");
            else
                Console.WriteLine($"  {path}");
        }

        Console.WriteLine();
        Console.WriteLine("MESSAGE CONTROL");
        foreach (var (path, desc) in universalPaths.Where(p => p.Item1.StartsWith("message.")))
        {
            if (descriptions)
                Console.WriteLine($"  {path,-25} {desc}");
            else
                Console.WriteLine($"  {path}");
        }

        Console.WriteLine();
        Console.WriteLine("💡 Use 'pidgeon path list <message-type>' to see message-specific paths");
        Console.WriteLine("💡 Use 'pidgeon path resolve <path> <message-type>' to see standard mappings");

        return Task.FromResult(0);
    }

    private async Task<int> ShowMessageTypePathsAsync(
        string messageType,
        string? standard,
        string? category,
        string format,
        bool descriptions,
        bool examples,
        string? output,
        CancellationToken cancellationToken)
    {
        // Get available paths for the message type
        var result = await _fieldPathResolver.GetAvailablePathsAsync(messageType, standard);
        if (result.IsFailure)
        {
            Console.WriteLine($"❌ Failed to get paths for {messageType}: {result.Error.Message}");
            return 1;
        }

        var paths = result.Value;
        if (!paths.Any())
        {
            Console.WriteLine($"📭 No semantic paths found for message type: {messageType}");
            Console.WriteLine();
            Console.WriteLine("💡 Try:");
            Console.WriteLine("   pidgeon path list                    # Show universal paths");
            Console.WriteLine("   pidgeon path search <keyword>        # Search for specific paths");
            return 0;
        }

        // Determine effective standard for display
        var effectiveStandard = standard;
        if (string.IsNullOrWhiteSpace(effectiveStandard))
        {
            var standardResult = await _configurationService.GetEffectiveStandardAsync(messageType, standard);
            effectiveStandard = standardResult.IsSuccess ? standardResult.Value : "Unknown";
        }

        Console.WriteLine($"📋 Available Semantic Paths: {messageType} ({effectiveStandard})");
        Console.WriteLine();

        // Group paths by category
        var groupedPaths = GroupPathsByCategory(paths, category);

        foreach (var group in groupedPaths.OrderBy(g => g.Key))
        {
            Console.WriteLine(group.Key.ToUpperInvariant());
            foreach (var (path, description) in group.Value.OrderBy(p => p.Key))
            {
                if (descriptions)
                {
                    Console.WriteLine($"  {path,-25} {description}");
                    if (examples)
                    {
                        // TODO: Add example values based on path type
                        Console.WriteLine($"  {"",-25} Example: {GetExampleValue(path)}");
                    }
                }
                else
                {
                    Console.WriteLine($"  {path}");
                }
            }
            Console.WriteLine();
        }

        Console.WriteLine("💡 Use 'pidgeon path resolve <path> <message-type>' to see standard-specific mappings");
        Console.WriteLine("💡 Use 'pidgeon path search <keyword>' to find paths by description");

        return 0;
    }

    private async Task<int> ResolveSemanticPathAsync(
        string semanticPath,
        string messageType,
        string? standard,
        bool allStandards,
        string format,
        bool detailed,
        bool pathOnly,
        CancellationToken cancellationToken)
    {
        if (pathOnly)
        {
            // Script-friendly mode - just output the resolved path
            var result = await _fieldPathResolver.ResolvePathAsync(semanticPath, messageType, standard);
            if (result.IsFailure)
            {
                Console.Error.WriteLine($"Error: {result.Error.Message}");
                return 1;
            }
            Console.WriteLine(result.Value);
            return 0;
        }

        Console.WriteLine($"🔍 Path Resolution: {semanticPath} → {GetPathDescription(semanticPath)}");
        Console.WriteLine();
        Console.WriteLine($"MESSAGE TYPE: {messageType}");
        Console.WriteLine();

        if (allStandards)
        {
            // TODO: Implement cross-standard resolution
            Console.WriteLine("Cross-standard resolution not yet implemented");
            return 1;
        }
        else
        {
            var result = await _fieldPathResolver.ResolvePathAsync(semanticPath, messageType, standard);
            if (result.IsFailure)
            {
                Console.WriteLine($"❌ Resolution failed: {result.Error.Message}");
                return 1;
            }

            // Determine effective standard
            var effectiveStandard = standard;
            if (string.IsNullOrWhiteSpace(effectiveStandard))
            {
                var standardResult = await _configurationService.GetEffectiveStandardAsync(messageType, standard);
                effectiveStandard = standardResult.IsSuccess ? standardResult.Value : "Unknown";
            }

            Console.WriteLine($"{effectiveStandard}:    {result.Value}");
            if (detailed)
            {
                Console.WriteLine($"           │ Field Type: {GetFieldType(semanticPath)}");
                Console.WriteLine($"           │ Description: {GetPathDescription(semanticPath)}");
                Console.WriteLine($"           │ Example: \"{GetExampleValue(semanticPath)}\"");
            }
            Console.WriteLine();

            Console.WriteLine("✅ Validation: Path is valid for this message type");
            Console.WriteLine($"💡 Use 'pidgeon set my-session {semanticPath} \"{GetExampleValue(semanticPath)}\"' to lock this value");
        }

        return 0;
    }

    private async Task<int> ValidateSemanticPathAsync(
        string semanticPath,
        string messageType,
        string? standard,
        bool suggestions,
        string format,
        CancellationToken cancellationToken)
    {
        var result = await _fieldPathResolver.ValidatePathAsync(semanticPath, messageType, standard);
        if (result.IsFailure)
        {
            Console.WriteLine($"❌ Validation failed: {result.Error.Message}");
            return 1;
        }

        if (result.Value)
        {
            Console.WriteLine($"✅ Valid Path: {semanticPath} for {messageType}");
            Console.WriteLine();
            Console.WriteLine($"💡 Use 'pidgeon path resolve {semanticPath} \"{messageType}\"' to see standard mappings");
            Console.WriteLine($"💡 Use 'pidgeon set my-session {semanticPath} \"<value>\"' to lock this field");
            return 0;
        }
        else
        {
            Console.WriteLine($"❌ Invalid Path: {semanticPath} for {messageType}");
            Console.WriteLine();

            if (suggestions)
            {
                Console.WriteLine("REASON: Path is not applicable to this message type");
                Console.WriteLine();
                Console.WriteLine("💡 SUGGESTIONS:");

                // TODO: Implement intelligent suggestions based on path analysis
                var availableResult = await _fieldPathResolver.GetAvailablePathsAsync(messageType, standard);
                if (availableResult.IsSuccess && availableResult.Value.Any())
                {
                    Console.WriteLine($"  For {messageType} messages, consider:");
                    var suggestions_list = availableResult.Value.Take(5);
                    foreach (var suggestion in suggestions_list)
                    {
                        Console.WriteLine($"  • {suggestion.Key,-25} ({suggestion.Value})");
                    }
                }
                Console.WriteLine();
                Console.WriteLine($"🔍 Use 'pidgeon path list \"{messageType}\"' to see all available paths");
            }

            return 1;
        }
    }

    private Task<int> SearchSemanticPathsAsync(
        string query,
        string? messageType,
        string? standard,
        string? category,
        bool exact,
        string format,
        CancellationToken cancellationToken)
    {
        // TODO: Implement actual search functionality
        // For now, provide a placeholder implementation

        Console.WriteLine($"🔍 Search Results: \"{query}\" (placeholder implementation)");
        Console.WriteLine();

        // Sample search results based on query
        var sampleResults = query.ToLowerInvariant() switch
        {
            "phone" => new[]
            {
                ("patient.phoneNumber", "Primary Phone Number (Home)"),
                ("patient.phoneWork", "Work Phone Number"),
                ("patient.phoneMobile", "Mobile Phone Number"),
                ("provider.phoneNumber", "Provider Phone Number")
            },
            "mrn" or "medical record" => new[]
            {
                ("patient.mrn", "Medical Record Number"),
                ("patient.accountNumber", "Patient Account Number")
            },
            "date" => new[]
            {
                ("patient.dateOfBirth", "Date of Birth"),
                ("encounter.admissionDate", "Admit Date/Time"),
                ("encounter.dischargeDate", "Discharge Date/Time"),
                ("message.timestamp", "Message Date/Time")
            },
            _ => new[]
            {
                ("patient.mrn", "Medical Record Number"),
                ("patient.lastName", "Patient Last Name")
            }
        };

        if (sampleResults.Any())
        {
            var groups = sampleResults.GroupBy(r => r.Item1.Split('.')[0])
                                   .ToDictionary(g => g.Key, g => g.ToArray());

            foreach (var group in groups)
            {
                Console.WriteLine(group.Key.ToUpperInvariant() + " FIELDS");
                foreach (var (path, description) in group.Value)
                {
                    Console.WriteLine($"  {path,-25} {description}");
                }
                Console.WriteLine();
            }
        }
        else
        {
            Console.WriteLine($"No paths found matching \"{query}\"");
            Console.WriteLine();
        }

        Console.WriteLine("💡 Use 'pidgeon path resolve <path> <message-type>' for standard-specific mappings");
        Console.WriteLine("💡 Use 'pidgeon path list' to see all available paths");

        return Task.FromResult(0);
    }

    private static Dictionary<string, Dictionary<string, string>> GroupPathsByCategory(
        IReadOnlyDictionary<string, string> paths,
        string? categoryFilter)
    {
        var groups = new Dictionary<string, Dictionary<string, string>>();

        foreach (var (path, description) in paths)
        {
            var category = GetPathCategory(path);

            if (!string.IsNullOrWhiteSpace(categoryFilter) &&
                !string.Equals(category, categoryFilter, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!groups.ContainsKey(category))
            {
                groups[category] = new Dictionary<string, string>();
            }
            groups[category][path] = description;
        }

        return groups;
    }

    private static string GetPathCategory(string path)
    {
        var parts = path.Split('.');
        if (parts.Length == 0) return "Other";

        return parts[0].ToLowerInvariant() switch
        {
            "patient" => "Patient Demographics",
            "encounter" => "Encounter Information",
            "provider" => "Provider Information",
            "medication" => "Medication Information",
            "message" => "Message Control",
            "facility" => "Facility Information",
            _ => "Other Fields"
        };
    }

    private static string GetPathDescription(string path)
    {
        // TODO: Get actual descriptions from field path plugins
        return path switch
        {
            "patient.mrn" => "Medical Record Number",
            "patient.lastName" => "Patient Last Name",
            "patient.firstName" => "Patient First Name",
            "patient.dateOfBirth" => "Date of Birth",
            "patient.sex" => "Administrative Sex",
            "patient.phoneNumber" => "Primary Phone Number",
            "encounter.location" => "Patient Location",
            "encounter.patientClass" => "Patient Class",
            "message.sendingApplication" => "Sending Application",
            "message.sendingFacility" => "Sending Facility",
            _ => "Healthcare field"
        };
    }

    private static string GetFieldType(string path)
    {
        // TODO: Get actual field types from field path plugins
        return path switch
        {
            "patient.dateOfBirth" => "TS (Timestamp)",
            "patient.sex" => "IS (Coded Value)",
            "patient.phoneNumber" => "XTN (Telephone)",
            _ => "ST (String)"
        };
    }

    private static string GetExampleValue(string path)
    {
        // TODO: Get realistic example values from field path plugins
        return path switch
        {
            "patient.mrn" => "MR123456",
            "patient.lastName" => "Smith",
            "patient.firstName" => "John",
            "patient.dateOfBirth" => "19850315",
            "patient.sex" => "M",
            "patient.phoneNumber" => "(555) 123-4567",
            "encounter.location" => "ER-1",
            "encounter.patientClass" => "I",
            "message.sendingApplication" => "EPIC",
            "message.sendingFacility" => "General Hospital",
            _ => "example_value"
        };
    }
}