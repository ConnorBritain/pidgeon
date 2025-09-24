// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Microsoft.Extensions.Logging;
using Pidgeon.Core.Domain.Transformation.Entities;
using Pidgeon.Core.Application.Interfaces.Workflow;
using Pidgeon.Core.Common.Types;
using Pidgeon.CLI.Services;
using System.CommandLine;
using System.Text.Json;

namespace Pidgeon.CLI.Commands;

/// <summary>
/// Pro-tier workflow wizard for guided multi-step healthcare integration scenarios.
/// Orchestrates generation, validation, and configuration operations with interactive prompts.
/// </summary>
public class WorkflowCommand : CommandBuilderBase
{
    private readonly ConfigurationStorage _configStorage;
    private readonly IWorkflowExecutionService? _workflowExecutionService;
    private readonly ProTierValidationService _proTierValidation;

    // FIXME: Need proper DI container integration for CLI commands to inject IWorkflowExecutionService
    public WorkflowCommand(
        ILogger<WorkflowCommand> logger,
        ProTierValidationService proTierValidation,
        IWorkflowExecutionService? workflowExecutionService = null) : base(logger)
    {
        _configStorage = new ConfigurationStorage();
        _workflowExecutionService = workflowExecutionService;
        _proTierValidation = proTierValidation;
    }

    public override Command CreateCommand()
    {
        var command = new Command("workflow", "⚠️  Beta: Interactive workflow wizard for multi-step healthcare scenarios");

        // Add subcommands
        command.Add(CreateWizardCommand());
        command.Add(CreateListCommand());
        command.Add(CreateRunCommand());
        command.Add(CreateTemplatesCommand());
        command.Add(CreateExportCommand());

        return command;
    }

    private Command CreateWizardCommand()
    {
        var nameOption = CreateNullableOption("--name", "-n", "Name for the workflow scenario");
        var templateOption = CreateNullableOption("--template", "-t", "Start from a template");
        var skipProCheckFlag = CreateFlag("--skip-pro-check", "Skip Pro tier validation (development only)");

        var command = new Command("wizard", "⚠️  Beta: Interactive guided workflow creation")
        {
            nameOption, templateOption, skipProCheckFlag
        };

        SetCommandAction(command, async (parseResult, cancellationToken) =>
        {
            var name = parseResult.GetValue(nameOption);
            var template = parseResult.GetValue(templateOption);
            var skipProCheck = parseResult.GetValue(skipProCheckFlag);

            // Pro tier feature validation using new subscription system
            var validationResult = await _proTierValidation.ValidateFeatureAccessAsync(
                FeatureFlags.WorkflowWizard, skipProCheck, cancellationToken);

            if (validationResult.IsFailure)
            {
                Console.WriteLine(validationResult.Error.Message);
                return 1;
            }

            Console.WriteLine("🔮 Pidgeon Workflow Wizard");
            Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            Console.WriteLine();

            try
            {
                var scenario = await RunInteractiveWizardAsync(name, template, cancellationToken);
                if (scenario != null)
                {
                    await SaveWorkflowScenarioAsync(scenario);
                    Console.WriteLine();
                    Console.WriteLine($"✅ Workflow scenario '{scenario.Name}' created successfully!");
                    Console.WriteLine($"💾 Saved to: ~/.pidgeon/workflows/{scenario.Name.ToLower().Replace(" ", "_")}.json");
                    Console.WriteLine();
                    Console.WriteLine("Next steps:");
                    Console.WriteLine($"   pidgeon workflow run --name \"{scenario.Name}\"");
                    Console.WriteLine($"   pidgeon workflow export --name \"{scenario.Name}\" --out ./my_workflow.json");
                }
                return 0;
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine();
                Console.WriteLine("Workflow wizard cancelled.");
                return 1;
            }
        });

        return command;
    }

    private Command CreateListCommand()
    {
        var formatOption = CreateNullableOption("--format", "-f", "Output format (table|json, default: table)");

        var command = new Command("list", "List available workflow scenarios and templates")
        {
            formatOption
        };

        SetCommandAction(command, async (parseResult, cancellationToken) =>
        {
            var format = parseResult.GetValue(formatOption) ?? "table";

            Console.WriteLine("Available Workflow Scenarios");
            Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            Console.WriteLine();

            var scenarios = await LoadWorkflowScenariosAsync();
            
            if (!scenarios.Any())
            {
                Console.WriteLine("No workflow scenarios found.");
                Console.WriteLine();
                Console.WriteLine("Create your first workflow:");
                Console.WriteLine("   pidgeon workflow wizard");
                return 0;
            }

            DisplayWorkflowList(scenarios, format);
            
            Console.WriteLine();
            Console.WriteLine($"Found {scenarios.Count} workflow scenarios");
            
            return 0;
        });

        return command;
    }

    private Command CreateRunCommand()
    {
        var nameOption = CreateRequiredOption("--name", "Name of workflow scenario to run");
        var skipProCheckFlag = CreateFlag("--skip-pro-check", "Skip Pro tier validation (development only)");

        var command = new Command("run", "⚠️  Beta: Execute a workflow scenario")
        {
            nameOption, skipProCheckFlag
        };

        SetCommandAction(command, async (parseResult, cancellationToken) =>
        {
            var name = parseResult.GetValue(nameOption)!;
            var skipProCheck = parseResult.GetValue(skipProCheckFlag);

            // Pro tier feature validation using new subscription system
            var validationResult = await _proTierValidation.ValidateFeatureAccessAsync(
                FeatureFlags.WorkflowWizard, skipProCheck, cancellationToken);

            if (validationResult.IsFailure)
            {
                Console.WriteLine(validationResult.Error.Message);
                return 1;
            }

            Console.WriteLine($"🚀 Executing workflow: {name}");
            Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            Console.WriteLine();

            var scenarios = await LoadWorkflowScenariosAsync();
            var scenario = scenarios.FirstOrDefault(s => 
                s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            if (scenario == null)
            {
                Console.WriteLine($"Error: Workflow scenario not found: {name}");
                Console.WriteLine();
                Console.WriteLine("Available workflows:");
                var availableScenarios = await LoadWorkflowScenariosAsync();
                DisplayWorkflowList(availableScenarios, "table");
                return 1;
            }

            if (_workflowExecutionService == null)
            {
                // FIXME: Implement proper DI integration so WorkflowExecutionService is available
                Console.WriteLine("Workflow execution service not available - using simulation mode");
                Console.WriteLine($"Simulating execution of {scenario.Steps.Count} workflow steps...");
                Console.WriteLine();

                foreach (var step in scenario.Steps.OrderBy(s => s.Order))
                {
                    Console.WriteLine($"Step {step.Order}: {step.Name} ({step.StepType})");
                    await Task.Delay(500, cancellationToken);
                }

                Console.WriteLine();
                Console.WriteLine($"Workflow '{scenario.Name}' simulation completed");
                return 0;
            }

            // Use actual WorkflowExecutionService
            Console.WriteLine($"Executing workflow '{scenario.Name}' with {scenario.Steps.Count} steps...");
            Console.WriteLine();

            var executionResult = await _workflowExecutionService.ExecuteWorkflowAsync(scenario, cancellationToken);
            
            if (executionResult.IsFailure)
            {
                Console.WriteLine($"Workflow execution failed: {executionResult.Error.Message}");
                return 1;
            }

            var execution = executionResult.Value;
            Console.WriteLine($"Workflow execution completed with status: {execution.Status}");
            Console.WriteLine($"Steps completed: {execution.StepResults.Count(r => r.Status == WorkflowStepStatus.Success)}/{execution.StepResults.Count}");
            
            if (execution.StepResults.Any(r => r.Status == WorkflowStepStatus.Failed))
            {
                Console.WriteLine();
                Console.WriteLine("Failed steps:");
                foreach (var failedStep in execution.StepResults.Where(r => r.Status == WorkflowStepStatus.Failed))
                {
                    Console.WriteLine($"  - {failedStep.StepName}: {failedStep.ErrorMessage}");
                }
            }

            return execution.Status == WorkflowExecutionStatus.Completed ? 0 : 1;
        });

        return command;
    }

    private Command CreateTemplatesCommand()
    {
        var command = new Command("templates", "List available workflow templates");

        SetCommandAction(command, async (parseResult, cancellationToken) =>
        {
            Console.WriteLine("Built-in Workflow Templates");
            Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            Console.WriteLine();

            var templates = GetBuiltInTemplates();
            
            foreach (var template in templates)
            {
                var difficultyIcon = template.DifficultyLevel switch
                {
                    WorkflowDifficultyLevel.Beginner => "🟢",
                    WorkflowDifficultyLevel.Intermediate => "🟡",
                    WorkflowDifficultyLevel.Advanced => "🟠",
                    WorkflowDifficultyLevel.Expert => "🔴",
                    _ => "⚪"
                };

                Console.WriteLine($"{difficultyIcon} {template.Name}");
                Console.WriteLine($"   {template.Description}");
                Console.WriteLine($"   Standards: {string.Join(", ", template.SupportedStandards)}");
                Console.WriteLine($"   Duration: ~{template.EstimatedDuration.TotalMinutes:F0} minutes");
                Console.WriteLine();
            }

            Console.WriteLine("Use a template:");
            Console.WriteLine("   pidgeon workflow wizard --template \"Integration Testing\"");

            await Task.CompletedTask;
            return 0;
        });

        return command;
    }

    private Command CreateExportCommand()
    {
        var nameOption = CreateRequiredOption("--name", "Name of workflow scenario to export");
        var outOption = CreateRequiredOption("--out", "Output file path");

        var command = new Command("export", "Export workflow scenario as JSON")
        {
            nameOption, outOption
        };

        SetCommandAction(command, async (parseResult, cancellationToken) =>
        {
            var name = parseResult.GetValue(nameOption)!;
            var outPath = parseResult.GetValue(outOption)!;

            var scenarios = await LoadWorkflowScenariosAsync();
            var scenario = scenarios.FirstOrDefault(s => 
                s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            if (scenario == null)
            {
                Console.WriteLine($"Error: Workflow scenario not found: {name}");
                return 1;
            }

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var json = JsonSerializer.Serialize(scenario, options);
            await File.WriteAllTextAsync(outPath, json, cancellationToken);

            Console.WriteLine($"✅ Workflow scenario exported to: {outPath}");
            Console.WriteLine("This file can be imported into the Pidgeon GUI or shared with your team.");

            return 0;
        });

        return command;
    }

    private async Task<WorkflowScenario?> RunInteractiveWizardAsync(string? name, string? templateName, CancellationToken cancellationToken)
    {
        // Step 1: Get scenario name
        name ??= PromptForString("Enter workflow name", "My Healthcare Workflow");
        var description = PromptForString("Enter description", "");
        
        // Step 2: Select standards
        var standards = await PromptForStandardsAsync();
        
        // Step 3: Select vendor configurations
        var vendorConfigs = await PromptForVendorConfigurationsAsync();
        
        // Step 4: Configure workflow steps
        var steps = await PromptForWorkflowStepsAsync();
        
        // Step 5: Add tags and metadata
        var tags = PromptForTags();

        var scenario = new WorkflowScenario
        {
            Name = name,
            Description = description,
            Standards = standards,
            VendorConfigurations = vendorConfigs,
            Steps = steps,
            Tags = tags,
            EstimatedDuration = CalculateEstimatedDuration(steps),
            CreatedDate = DateTime.UtcNow,
            Version = "1.0"
        };

        return scenario;
    }

    private async Task<List<string>> PromptForStandardsAsync()
    {
        await Task.Yield();
        Console.WriteLine();
        Console.WriteLine("🏥 Healthcare Standards Selection");
        Console.WriteLine("Select the standards your workflow will use:");
        Console.WriteLine();
        Console.WriteLine("1. HL7 v2.3 (Hospital messaging)");
        Console.WriteLine("2. FHIR R4 (Modern API-based)");
        Console.WriteLine("3. NCPDP SCRIPT (Pharmacy)");
        Console.WriteLine("4. All standards");
        Console.WriteLine();

        var choice = PromptForChoice("Select option (1-4)", "1");
        
        return choice switch
        {
            "1" => new List<string> { "HL7v2" },
            "2" => new List<string> { "FHIR" },
            "3" => new List<string> { "NCPDP" },
            "4" => new List<string> { "HL7v2", "FHIR", "NCPDP" },
            _ => new List<string> { "HL7v2" }
        };
    }

    private async Task<List<Core.Domain.Configuration.Entities.ConfigurationAddress>> PromptForVendorConfigurationsAsync()
    {
        await Task.Yield();
        Console.WriteLine();
        Console.WriteLine("🏢 Vendor Configuration Selection");
        Console.WriteLine("Available vendor configurations:");
        Console.WriteLine();

        // List available configurations using our storage
        var configFiles = _configStorage.ListConfigFiles();
        var vendorConfigs = new List<Core.Domain.Configuration.Entities.ConfigurationAddress>();

        if (!configFiles.Any())
        {
            Console.WriteLine("No vendor configurations found.");
            Console.WriteLine("You can create vendor configurations with:");
            Console.WriteLine("   pidgeon config analyze --samples ./messages --save vendor_config.json");
            Console.WriteLine();
            return vendorConfigs;
        }

        Console.WriteLine("Available configurations:");
        for (int i = 0; i < Math.Min(configFiles.Length, 10); i++)
        {
            var fileName = Path.GetFileNameWithoutExtension(configFiles[i]);
            Console.WriteLine($"{i + 1}. {fileName}");
        }
        Console.WriteLine($"{configFiles.Length + 1}. Skip vendor configurations");
        Console.WriteLine();

        var choice = PromptForChoice($"Select configuration (1-{configFiles.Length + 1})", $"{configFiles.Length + 1}");
        
        if (int.TryParse(choice, out var index) && index > 0 && index <= configFiles.Length)
        {
            // TODO: Parse actual configuration file to get ConfigurationAddress
            // For now, create a placeholder
            var fileName = Path.GetFileNameWithoutExtension(configFiles[index - 1]);
            vendorConfigs.Add(new Core.Domain.Configuration.Entities.ConfigurationAddress(
                "Unknown", "HL7v2", "Unknown") { });
        }

        return vendorConfigs;
    }

    private async Task<List<WorkflowStep>> PromptForWorkflowStepsAsync()
    {
        await Task.Yield();
        Console.WriteLine();
        Console.WriteLine("🔧 Workflow Steps Configuration");
        Console.WriteLine("Build your workflow step by step:");
        Console.WriteLine();

        var steps = new List<WorkflowStep>();
        var stepOrder = 1;

        while (true)
        {
            Console.WriteLine($"Step {stepOrder}:");
            Console.WriteLine("1. Generate messages");
            Console.WriteLine("2. Validate messages");
            Console.WriteLine("3. De-identify messages");
            Console.WriteLine("4. Analyze vendor patterns");
            Console.WriteLine("5. Compare messages/configs");
            Console.WriteLine("6. Finish workflow");
            Console.WriteLine();

            var choice = PromptForChoice("Select step type (1-6)", "6");
            
            if (choice == "6") break;

            var stepType = choice switch
            {
                "1" => WorkflowStepType.Generate,
                "2" => WorkflowStepType.Validate,
                "3" => WorkflowStepType.DeIdentify,
                "4" => WorkflowStepType.ConfigureVendor,
                "5" => WorkflowStepType.Compare,
                _ => WorkflowStepType.Generate
            };

            var stepName = PromptForString($"Enter name for {stepType} step", $"{stepType} Step {stepOrder}");
            var stepDescription = PromptForString("Enter step description", "");

            var step = new WorkflowStep
            {
                Name = stepName,
                Description = stepDescription,
                StepType = stepType,
                Order = stepOrder,
                EstimatedDuration = TimeSpan.FromMinutes(5), // Default
                InputDependencies = stepOrder > 1 && steps.Any() 
                    ? new List<string> { steps.Last().Id } 
                    : new List<string>()
            };

            steps.Add(step);
            stepOrder++;

            Console.WriteLine($"✅ Added: {step.Name}");
            Console.WriteLine();
        }

        return steps;
    }

    private List<string> PromptForTags()
    {
        Console.WriteLine();
        Console.WriteLine("🏷️  Tags (optional)");
        var tagsInput = PromptForString("Enter tags (comma-separated)", "");
        
        return string.IsNullOrWhiteSpace(tagsInput) 
            ? new List<string>() 
            : tagsInput.Split(',').Select(t => t.Trim()).Where(t => !string.IsNullOrEmpty(t)).ToList();
    }

    private TimeSpan CalculateEstimatedDuration(List<WorkflowStep> steps)
    {
        return TimeSpan.FromMinutes(steps.Count * 5); // Simple calculation
    }

    private async Task SaveWorkflowScenarioAsync(WorkflowScenario scenario)
    {
        var workflowDir = Path.Combine(_configStorage.ConfigDirectory, "workflows");
        Directory.CreateDirectory(workflowDir);

        var fileName = $"{scenario.Name.ToLower().Replace(" ", "_")}.json";
        var filePath = Path.Combine(workflowDir, fileName);

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var json = JsonSerializer.Serialize(scenario, options);
        await File.WriteAllTextAsync(filePath, json);
    }

    private async Task<List<WorkflowScenario>> LoadWorkflowScenariosAsync()
    {
        var workflowDir = Path.Combine(_configStorage.ConfigDirectory, "workflows");
        if (!Directory.Exists(workflowDir))
        {
            return new List<WorkflowScenario>();
        }

        var scenarios = new List<WorkflowScenario>();
        var files = Directory.GetFiles(workflowDir, "*.json");

        foreach (var file in files)
        {
            try
            {
                var json = await File.ReadAllTextAsync(file);
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var scenario = JsonSerializer.Deserialize<WorkflowScenario>(json, options);
                if (scenario != null)
                {
                    scenarios.Add(scenario);
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to load workflow scenario from {File}", file);
            }
        }

        return scenarios.OrderBy(s => s.Name).ToList();
    }

    private void DisplayWorkflowList(List<WorkflowScenario> scenarios, string format)
    {
        switch (format.ToLowerInvariant())
        {
            case "json":
                var jsonOptions = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                var json = JsonSerializer.Serialize(scenarios, jsonOptions);
                Console.WriteLine(json);
                break;

            case "table":
            default:
                Console.WriteLine($"{"Name",-25} {"Standards",-15} {"Steps",-8} {"Duration",-10} {"Tags",-20}");
                Console.WriteLine(new string('─', 80));
                
                foreach (var scenario in scenarios)
                {
                    var standards = string.Join(",", scenario.Standards);
                    var duration = $"{scenario.EstimatedDuration.TotalMinutes:F0}m";
                    var tags = string.Join(",", scenario.Tags.Take(3));
                    
                    Console.WriteLine($"{scenario.Name,-25} {standards,-15} {scenario.Steps.Count,-8} {duration,-10} {tags,-20}");
                }
                break;
        }
    }

    private List<WorkflowTemplate> GetBuiltInTemplates()
    {
        return new List<WorkflowTemplate>
        {
            new WorkflowTemplate
            {
                Name = "Integration Testing",
                Description = "Basic integration testing workflow with message generation and validation",
                Category = WorkflowTemplateCategory.Integration,
                SupportedStandards = new List<string> { "HL7v2", "FHIR" },
                DifficultyLevel = WorkflowDifficultyLevel.Beginner,
                EstimatedDuration = TimeSpan.FromMinutes(15)
            },
            new WorkflowTemplate
            {
                Name = "Vendor Migration",
                Description = "Compare configurations between different vendor implementations",
                Category = WorkflowTemplateCategory.VendorSpecific,
                SupportedStandards = new List<string> { "HL7v2" },
                DifficultyLevel = WorkflowDifficultyLevel.Intermediate,
                EstimatedDuration = TimeSpan.FromMinutes(30)
            },
            new WorkflowTemplate
            {
                Name = "De-identification Pipeline",
                Description = "Complete data de-identification workflow for safe testing",
                Category = WorkflowTemplateCategory.DeIdentification,
                SupportedStandards = new List<string> { "HL7v2", "FHIR", "NCPDP" },
                DifficultyLevel = WorkflowDifficultyLevel.Advanced,
                EstimatedDuration = TimeSpan.FromMinutes(45)
            }
        };
    }


    private string PromptForString(string prompt, string defaultValue)
    {
        var defaultText = string.IsNullOrEmpty(defaultValue) ? "" : $" [{defaultValue}]";
        Console.Write($"{prompt}{defaultText}: ");
        
        var input = Console.ReadLine()?.Trim();
        return string.IsNullOrEmpty(input) ? defaultValue : input;
    }

    private string PromptForChoice(string prompt, string defaultValue)
    {
        var defaultText = string.IsNullOrEmpty(defaultValue) ? "" : $" [{defaultValue}]";
        Console.Write($"{prompt}{defaultText}: ");
        
        var input = Console.ReadLine()?.Trim();
        return string.IsNullOrEmpty(input) ? defaultValue : input;
    }

    private static Option<bool> CreateFlag(string name, string description)
    {
        return new Option<bool>(name)
        {
            Description = description,
            DefaultValueFactory = _ => false
        };
    }
}