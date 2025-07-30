// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Segmint.Core.DataGeneration;
using Segmint.Core.DataGeneration.Clinical;
using Segmint.CLI.Serialization;

namespace Segmint.CLI.Commands;

/// <summary>
/// CLI commands for synthetic data generation.
/// </summary>
public static class DataGenerationCommands
{
    /// <summary>
    /// JSON serializer options configured for trimmed builds.
    /// </summary>
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        TypeInfoResolver = SegmintJsonContext.Default
    };

    /// <summary>
    /// Creates the main data generation command with all subcommands.
    /// </summary>
    /// <param name="dataService">The synthetic data service.</param>
    /// <param name="logger">Logger instance.</param>
    /// <returns>The configured data generation command.</returns>
    public static Command CreateDataCommand(
        SyntheticDataService dataService,
        ILogger logger)
    {
        var dataCommand = new Command("data", "Generate synthetic test data");

        // Add subcommands
        dataCommand.AddCommand(CreateGenerateCommand(dataService, logger));
        dataCommand.AddCommand(CreateScenarioCommand(dataService, logger));
        dataCommand.AddCommand(CreateSuiteCommand(dataService, logger));
        dataCommand.AddCommand(CreateDemographicsCommand(dataService, logger));
        dataCommand.AddCommand(CreateMedicationCommand(dataService, logger));

        return dataCommand;
    }

    /// <summary>
    /// Creates the data generate command for basic message generation.
    /// </summary>
    private static Command CreateGenerateCommand(
        SyntheticDataService dataService,
        ILogger logger)
    {
        var generateCommand = new Command("generate", "Generate synthetic HL7 messages");

        var typeOption = new Option<string>(
            aliases: ["--type", "-t"],
            description: "Message type to generate (RDE, ADT)")
        {
            IsRequired = true
        };

        var countOption = new Option<int>(
            aliases: ["--count", "-c"],
            description: "Number of messages to generate")
        {
            ArgumentHelpName = "COUNT"
        };
        countOption.SetDefaultValue(10);

        var outputOption = new Option<DirectoryInfo>(
            aliases: ["--output", "-o"],
            description: "Output directory for generated messages")
        {
            ArgumentHelpName = "DIRECTORY"
        };
        outputOption.SetDefaultValue(new DirectoryInfo("."));

        var formatOption = new Option<string>(
            aliases: ["--format", "-f"],
            description: "Output format (hl7, json, xml)")
        {
            ArgumentHelpName = "FORMAT"
        };
        formatOption.SetDefaultValue("hl7");

        var seedOption = new Option<int?>(
            aliases: ["--seed", "-s"],
            description: "Random seed for reproducible generation")
        {
            ArgumentHelpName = "SEED"
        };

        var qualityOption = new Option<string>(
            aliases: ["--quality", "-q"],
            description: "Data quality level (basic, realistic, comprehensive)")
        {
            ArgumentHelpName = "QUALITY"
        };
        qualityOption.SetDefaultValue("realistic");

        generateCommand.AddOption(typeOption);
        generateCommand.AddOption(countOption);
        generateCommand.AddOption(outputOption);
        generateCommand.AddOption(formatOption);
        generateCommand.AddOption(seedOption);
        generateCommand.AddOption(qualityOption);

        generateCommand.SetHandler(async (string type, int count, DirectoryInfo output, string format, int? seed, string quality) =>
        {
            await HandleGenerateCommand(dataService, logger, type, count, output, format, seed, quality, CancellationToken.None);
        }, typeOption, countOption, outputOption, formatOption, seedOption, qualityOption);

        return generateCommand;
    }

    /// <summary>
    /// Creates the data scenario command for scenario-based generation.
    /// </summary>
    private static Command CreateScenarioCommand(
        SyntheticDataService dataService,
        ILogger logger)
    {
        var scenarioCommand = new Command("scenario", "Generate scenario-based test data");

        var typeOption = new Option<string>(
            aliases: ["--type", "-t"],
            description: "Scenario type (pharmacy-order, emergency-admission, elective-surgery, outpatient-visit, complete-stay)")
        {
            IsRequired = true
        };

        var countOption = new Option<int>(
            aliases: ["--count", "-c"],
            description: "Number of scenarios to generate")
        {
            ArgumentHelpName = "COUNT"
        };
        countOption.SetDefaultValue(1);

        var outputOption = new Option<DirectoryInfo>(
            aliases: ["--output", "-o"],
            description: "Output directory for generated scenarios")
        {
            ArgumentHelpName = "DIRECTORY"
        };
        outputOption.SetDefaultValue(new DirectoryInfo("."));

        var formatOption = new Option<string>(
            aliases: ["--format", "-f"],
            description: "Output format (hl7, json, xml)")
        {
            ArgumentHelpName = "FORMAT"
        };
        formatOption.SetDefaultValue("hl7");

        var seedOption = new Option<int?>(
            aliases: ["--seed", "-s"],
            description: "Random seed for reproducible generation")
        {
            ArgumentHelpName = "SEED"
        };

        scenarioCommand.AddOption(typeOption);
        scenarioCommand.AddOption(countOption);
        scenarioCommand.AddOption(outputOption);
        scenarioCommand.AddOption(formatOption);
        scenarioCommand.AddOption(seedOption);

        scenarioCommand.SetHandler(async (string type, int count, DirectoryInfo output, string format, int? seed) =>
        {
            await HandleScenarioCommand(dataService, logger, type, count, output, format, seed, CancellationToken.None);
        }, typeOption, countOption, outputOption, formatOption, seedOption);

        return scenarioCommand;
    }

    /// <summary>
    /// Creates the data suite command for comprehensive test suites.
    /// </summary>
    private static Command CreateSuiteCommand(
        SyntheticDataService dataService,
        ILogger logger)
    {
        var suiteCommand = new Command("suite", "Generate comprehensive test suites");

        var nameOption = new Option<string>(
            aliases: ["--name", "-n"],
            description: "Test suite name")
        {
            ArgumentHelpName = "NAME"
        };
        nameOption.SetDefaultValue("TestSuite");

        var outputOption = new Option<DirectoryInfo>(
            aliases: ["--output", "-o"],
            description: "Output directory for test suite")
        {
            ArgumentHelpName = "DIRECTORY"
        };
        outputOption.SetDefaultValue(new DirectoryInfo("."));

        var formatOption = new Option<string>(
            aliases: ["--format", "-f"],
            description: "Output format (hl7, json, xml)")
        {
            ArgumentHelpName = "FORMAT"
        };
        formatOption.SetDefaultValue("hl7");

        var seedOption = new Option<int?>(
            aliases: ["--seed", "-s"],
            description: "Random seed for reproducible generation")
        {
            ArgumentHelpName = "SEED"
        };

        var mixOption = new Option<string>(
            aliases: ["--mix", "-m"],
            description: "Scenario mix (typical, pharmacy-focused, inpatient-focused, outpatient-focused)")
        {
            ArgumentHelpName = "MIX"
        };
        mixOption.SetDefaultValue("typical");

        suiteCommand.AddOption(nameOption);
        suiteCommand.AddOption(outputOption);
        suiteCommand.AddOption(formatOption);
        suiteCommand.AddOption(seedOption);
        suiteCommand.AddOption(mixOption);

        suiteCommand.SetHandler(async (string name, DirectoryInfo output, string format, int? seed, string mix) =>
        {
            await HandleSuiteCommand(dataService, logger, name, output, format, seed, mix, CancellationToken.None);
        }, nameOption, outputOption, formatOption, seedOption, mixOption);

        return suiteCommand;
    }

    /// <summary>
    /// Creates the demographics command for patient data generation.
    /// </summary>
    private static Command CreateDemographicsCommand(
        SyntheticDataService dataService,
        ILogger logger)
    {
        var demographicsCommand = new Command("demographics", "Generate patient demographic data");

        var countOption = new Option<int>(
            aliases: ["--count", "-c"],
            description: "Number of patients to generate")
        {
            ArgumentHelpName = "COUNT"
        };
        countOption.SetDefaultValue(10);

        var outputOption = new Option<FileInfo>(
            aliases: ["--output", "-o"],
            description: "Output file for demographic data")
        {
            ArgumentHelpName = "FILE"
        };

        var formatOption = new Option<string>(
            aliases: ["--format", "-f"],
            description: "Output format (json, csv, xml)")
        {
            ArgumentHelpName = "FORMAT"
        };
        formatOption.SetDefaultValue("json");

        var seedOption = new Option<int?>(
            aliases: ["--seed", "-s"],
            description: "Random seed for reproducible generation")
        {
            ArgumentHelpName = "SEED"
        };

        var includeSSNOption = new Option<bool>(
            aliases: ["--include-ssn"],
            description: "Include social security numbers in output");

        demographicsCommand.AddOption(countOption);
        demographicsCommand.AddOption(outputOption);
        demographicsCommand.AddOption(formatOption);
        demographicsCommand.AddOption(seedOption);
        demographicsCommand.AddOption(includeSSNOption);

        demographicsCommand.SetHandler(async (int count, FileInfo? output, string format, int? seed, bool includeSSN) =>
        {
            await HandleDemographicsCommand(dataService, logger, count, output, format, seed, includeSSN, CancellationToken.None);
        }, countOption, outputOption, formatOption, seedOption, includeSSNOption);

        return demographicsCommand;
    }

    /// <summary>
    /// Creates the medication command for pharmacy data generation.
    /// </summary>
    private static Command CreateMedicationCommand(
        SyntheticDataService dataService,
        ILogger logger)
    {
        var medicationCommand = new Command("medication", "Generate medication and prescription data");

        var countOption = new Option<int>(
            aliases: ["--count", "-c"],
            description: "Number of prescriptions to generate")
        {
            ArgumentHelpName = "COUNT"
        };
        countOption.SetDefaultValue(10);

        var outputOption = new Option<FileInfo>(
            aliases: ["--output", "-o"],
            description: "Output file for medication data")
        {
            ArgumentHelpName = "FILE"
        };

        var formatOption = new Option<string>(
            aliases: ["--format", "-f"],
            description: "Output format (json, csv, xml)")
        {
            ArgumentHelpName = "FORMAT"
        };
        formatOption.SetDefaultValue("json");

        var seedOption = new Option<int?>(
            aliases: ["--seed", "-s"],
            description: "Random seed for reproducible generation")
        {
            ArgumentHelpName = "SEED"
        };

        var categoryOption = new Option<string?>(
            aliases: ["--category"],
            description: "Medication category filter (cardiovascular, diabetes, pain, antibiotic, mental-health)")
        {
            ArgumentHelpName = "CATEGORY"
        };

        medicationCommand.AddOption(countOption);
        medicationCommand.AddOption(outputOption);
        medicationCommand.AddOption(formatOption);
        medicationCommand.AddOption(seedOption);
        medicationCommand.AddOption(categoryOption);

        medicationCommand.SetHandler(async (int count, FileInfo? output, string format, int? seed, string? category) =>
        {
            await HandleMedicationCommand(dataService, logger, count, output, format, seed, category, CancellationToken.None);
        }, countOption, outputOption, formatOption, seedOption, categoryOption);

        return medicationCommand;
    }

    // Command handlers

    private static async Task HandleGenerateCommand(
        SyntheticDataService dataService,
        ILogger logger,
        string type,
        int count,
        DirectoryInfo output,
        string format,
        int? seed,
        string quality,
        CancellationToken cancellationToken)
    {
        try
        {
            var constraints = new DataGenerationConstraints
            {
                Seed = seed,
                QualityLevel = quality
            };

            logger.LogInformation("Generating {Count} {Type} messages with {Quality} quality", count, type, quality);

            if (!output.Exists)
            {
                output.Create();
                logger.LogInformation("Created output directory: {Directory}", output.FullName);
            }

            if (type.ToUpperInvariant() == "RDE")
            {
                var messages = dataService.GenerateRDEMessages(count, constraints);
                await SaveMessages(messages, output, format, "RDE", logger, cancellationToken);
            }
            else if (type.ToUpperInvariant() == "ADT")
            {
                var messages = dataService.GenerateADTMessages(count, constraints);
                await SaveMessages(messages, output, format, "ADT", logger, cancellationToken);
            }
            else
            {
                logger.LogError("Unknown message type: {Type}. Supported types: RDE, ADT", type);
                return;
            }

            logger.LogInformation("✅ Successfully generated {Count} {Type} messages in {Directory}", count, type, output.FullName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating {Type} messages", type);
        }
    }

    private static async Task HandleScenarioCommand(
        SyntheticDataService dataService,
        ILogger logger,
        string type,
        int count,
        DirectoryInfo output,
        string format,
        int? seed,
        CancellationToken cancellationToken)
    {
        try
        {
            var constraints = new DataGenerationConstraints { Seed = seed };
            var scenarioType = ParseScenarioType(type);

            logger.LogInformation("Generating {Count} {Type} scenarios", count, type);

            if (!output.Exists)
            {
                output.Create();
                logger.LogInformation("Created output directory: {Directory}", output.FullName);
            }

            for (int i = 0; i < count; i++)
            {
                var scenario = new TestScenario
                {
                    Type = scenarioType,
                    Name = $"{type}_{i + 1:D3}",
                    Description = $"Generated {type} scenario"
                };

                var scenarioData = dataService.GenerateScenario(scenario, constraints);
                await SaveScenario(scenarioData, output, format, logger, cancellationToken);
            }

            logger.LogInformation("✅ Successfully generated {Count} {Type} scenarios in {Directory}", count, type, output.FullName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating {Type} scenarios", type);
        }
    }

    private static async Task HandleSuiteCommand(
        SyntheticDataService dataService,
        ILogger logger,
        string name,
        DirectoryInfo output,
        string format,
        int? seed,
        string mix,
        CancellationToken cancellationToken)
    {
        try
        {
            var constraints = new DataGenerationConstraints { Seed = seed };
            var scenarioMix = CreateScenarioMix(mix);

            logger.LogInformation("Generating test suite '{Name}' with {ScenarioCount} scenarios", name, scenarioMix.TotalScenarios);

            var suiteDirectory = new DirectoryInfo(Path.Combine(output.FullName, name));
            if (!suiteDirectory.Exists)
            {
                suiteDirectory.Create();
                logger.LogInformation("Created test suite directory: {Directory}", suiteDirectory.FullName);
            }

            var scenarios = dataService.GenerateTestSuite(scenarioMix, constraints);
            int scenarioIndex = 0;

            foreach (var scenarioData in scenarios)
            {
                scenarioIndex++;
                var scenarioDir = new DirectoryInfo(Path.Combine(suiteDirectory.FullName, $"Scenario_{scenarioIndex:D3}_{scenarioData.Scenario.Type}"));
                scenarioDir.Create();

                await SaveScenario(scenarioData, scenarioDir, format, logger, cancellationToken);
                
                if (scenarioIndex % 10 == 0)
                {
                    logger.LogInformation("Generated {Count} scenarios...", scenarioIndex);
                }
            }

            // Create suite summary
            await CreateSuiteSummary(scenarioMix, suiteDirectory, logger, cancellationToken);

            logger.LogInformation("✅ Successfully generated test suite '{Name}' with {ScenarioCount} scenarios in {Directory}", 
                name, scenarioMix.TotalScenarios, suiteDirectory.FullName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating test suite '{Name}'", name);
        }
    }

    private static Task HandleDemographicsCommand(
        SyntheticDataService dataService,
        ILogger logger,
        int count,
        FileInfo? output,
        string format,
        int? seed,
        bool includeSSN,
        CancellationToken cancellationToken)
    {
        // Implementation for demographics-only generation
        logger.LogInformation("Demographics generation not yet implemented");
        return Task.CompletedTask;
    }

    private static Task HandleMedicationCommand(
        SyntheticDataService dataService,
        ILogger logger,
        int count,
        FileInfo? output,
        string format,
        int? seed,
        string? category,
        CancellationToken cancellationToken)
    {
        // Implementation for medication-only generation
        logger.LogInformation("Medication generation not yet implemented");
        return Task.CompletedTask;
    }

    // Helper methods

    private static ScenarioType ParseScenarioType(string type)
    {
        return type.ToLowerInvariant().Replace("-", "") switch
        {
            "pharmacyorder" => ScenarioType.PharmacyOrder,
            "emergencyadmission" => ScenarioType.EmergencyAdmission,
            "electivesurgery" => ScenarioType.ElectiveSurgery,
            "outpatientvisit" => ScenarioType.OutpatientVisit,
            "completestay" => ScenarioType.CompletePatientStay,
            _ => throw new ArgumentException($"Unknown scenario type: {type}")
        };
    }

    private static ScenarioMix CreateScenarioMix(string mix)
    {
        return mix.ToLowerInvariant() switch
        {
            "typical" => ScenarioMix.CreateTypicalMix(),
            "pharmacy-focused" => new ScenarioMix
            {
                Scenarios = new()
                {
                    new() { Type = ScenarioType.PharmacyOrder, Count = 20, Description = "Pharmacy orders" },
                    new() { Type = ScenarioType.OutpatientVisit, Count = 5, Description = "Outpatient visits" }
                }
            },
            "inpatient-focused" => new ScenarioMix
            {
                Scenarios = new()
                {
                    new() { Type = ScenarioType.EmergencyAdmission, Count = 10, Description = "Emergency admissions" },
                    new() { Type = ScenarioType.ElectiveSurgery, Count = 8, Description = "Elective surgeries" },
                    new() { Type = ScenarioType.CompletePatientStay, Count = 5, Description = "Complete stays" }
                }
            },
            "outpatient-focused" => new ScenarioMix
            {
                Scenarios = new()
                {
                    new() { Type = ScenarioType.OutpatientVisit, Count = 15, Description = "Outpatient visits" },
                    new() { Type = ScenarioType.PharmacyOrder, Count = 10, Description = "Pharmacy orders" }
                }
            },
            _ => throw new ArgumentException($"Unknown scenario mix: {mix}")
        };
    }

    private static async Task SaveMessages<T>(IEnumerable<T> messages, DirectoryInfo output, string format, string prefix, ILogger logger, CancellationToken cancellationToken) where T : class
    {
        int index = 0;
        foreach (var message in messages)
        {
            index++;
            var fileName = $"{prefix}_{index:D4}.{GetFileExtension(format)}";
            var filePath = Path.Combine(output.FullName, fileName);
            
            var content = format.ToLowerInvariant() switch
            {
                "hl7" => message.ToString(),
                "json" => JsonSerializer.Serialize(message, JsonOptions),
                "xml" => SerializeToXml(message),
                _ => message.ToString()
            };

            await File.WriteAllTextAsync(filePath, content, cancellationToken);
        }
    }

    private static async Task SaveScenario(ScenarioData scenarioData, DirectoryInfo output, string format, ILogger logger, CancellationToken cancellationToken)
    {
        // Save individual messages
        await SaveMessages(scenarioData.Messages, output, format, scenarioData.Scenario.Type.ToString(), logger, cancellationToken);
        
        // Save scenario metadata
        var metadataPath = Path.Combine(output.FullName, "scenario_metadata.json");
        var metadata = new ScenarioMetadataDto
        {
            Scenario = scenarioData.Scenario?.ToString() ?? "",
            Patient = scenarioData.Patient?.ToString() ?? "",
            MessageCount = scenarioData.Messages.Count,
            GeneratedAt = scenarioData.GeneratedAt
        };
        
        var metadataJson = JsonSerializer.Serialize(metadata, JsonOptions);
        await File.WriteAllTextAsync(metadataPath, metadataJson, cancellationToken);
    }

    private static async Task CreateSuiteSummary(ScenarioMix scenarioMix, DirectoryInfo suiteDirectory, ILogger logger, CancellationToken cancellationToken)
    {
        var summary = new SuiteSummaryDto
        {
            GeneratedAt = DateTime.Now,
            TotalScenarios = scenarioMix.TotalScenarios,
            ScenarioBreakdown = scenarioMix.Scenarios.Select(s => new ScenarioBreakdownDto 
            { 
                Type = s.Type.ToString(), 
                Count = s.Count, 
                Description = s.Description 
            }).ToList()
        };

        var summaryPath = Path.Combine(suiteDirectory.FullName, "suite_summary.json");
        var summaryJson = JsonSerializer.Serialize(summary, JsonOptions);
        await File.WriteAllTextAsync(summaryPath, summaryJson, cancellationToken);
    }

    private static string GetFileExtension(string format)
    {
        return format.ToLowerInvariant() switch
        {
            "hl7" => "hl7",
            "json" => "json",
            "xml" => "xml",
            _ => "txt"
        };
    }

    private static string SerializeToXml<T>(T obj)
    {
        // Simple XML serialization - could be enhanced
        return $"<{typeof(T).Name}>{obj}</{typeof(T).Name}>";
    }
}