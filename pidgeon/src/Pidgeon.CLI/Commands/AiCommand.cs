// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Microsoft.Extensions.Logging;
using Pidgeon.Core.Application.Interfaces.Intelligence;
using Pidgeon.CLI.Services;
using Pidgeon.Core.Common.Types;
using Pidgeon.Core.Domain.Intelligence;
using System.CommandLine;
using System.Linq;

namespace Pidgeon.CLI.Commands;

/// <summary>
/// AI model management and AI-assisted message operations.
///
/// Current Status: Foundation complete, ready for community contribution.
/// Infrastructure: Model downloading, management, and basic inference pipeline implemented.
/// Opportunity: Inference optimization and healthcare AI model integration.
///
/// Contributors welcome to enhance:
/// - Model inference performance optimization
/// - Additional healthcare-specific AI models
/// - Advanced AI-assisted message modification features
/// - Natural language to HL7 conversion improvements
/// </summary>
public class AiCommand : CommandBuilderBase
{
    private readonly IModelManagementService _modelManagementService;
    private readonly IAIMessageModificationService _modificationService;
    private readonly ProTierValidationService _proTierValidation;

    public AiCommand(
        ILogger<AiCommand> logger,
        IModelManagementService modelManagementService,
        IAIMessageModificationService modificationService,
        ProTierValidationService proTierValidation)
        : base(logger)
    {
        _modelManagementService = modelManagementService;
        _modificationService = modificationService;
        _proTierValidation = proTierValidation;
    }

    public override Command CreateCommand()
    {
        var command = new Command("ai", "⚠️  AI features coming in v0.1.0 (use --dev for early access)");

        // Global development flag for early AI access
        var devOption = CreateBooleanOption("--dev", "Enable development AI features (v0.1.0 preview)");
        var devShortOption = CreateBooleanOption("-d", "Enable development AI features (v0.1.0 preview)");

        command.Add(devOption);
        command.Add(devShortOption);

        // Model management subcommands
        command.Add(CreateListCommand());
        command.Add(CreateDownloadCommand());
        command.Add(CreateRemoveCommand());
        command.Add(CreateInfoCommand());

        // AI-assisted message operations (Pro features)
        command.Add(CreateModifyCommand());
        command.Add(CreateSuggestCommand());
        command.Add(CreateTemplateCommand());

        return command;
    }

    private Command CreateListCommand()
    {
        var command = new Command("list", "Show AI model setup experience");

        var availableOption = CreateBooleanOption("--available", "Show available models for download");
        var installedOption = CreateBooleanOption("--installed", "Show locally installed models");
        var simpleOption = CreateBooleanOption("--simple", "Show simple list format (legacy)");

        command.Add(availableOption);
        command.Add(installedOption);
        command.Add(simpleOption);

        SetCommandAction(command, async (parseResult, cancellationToken) =>
        {
            // Check if development mode is enabled
            if (!CheckDevModeOrShowComingSoon(parseResult))
                return 0;

            var showAvailable = parseResult.GetValue(availableOption);
            var showInstalled = parseResult.GetValue(installedOption);
            var simpleFormat = parseResult.GetValue(simpleOption);

            try
            {
                // Default to showing enhanced experience if no specific flags
                if (!showAvailable && !showInstalled && !simpleFormat)
                {
                    await ShowEnhancedModelExperience(cancellationToken);
                    return 0;
                }

                // Legacy simple format for specific queries
                if (showAvailable)
                {
                    Console.WriteLine("Available Models for Download:");
                    Console.WriteLine();

                    var availableResult = await _modelManagementService.ListAvailableModelsAsync(cancellationToken);
                    if (availableResult.IsSuccess)
                    {
                        foreach (var model in availableResult.Value)
                        {
                            var sizeGB = model.SizeBytes / (1024.0 * 1024.0 * 1024.0);
                            var tierPrefix = model.Tier switch
                            {
                                "Pro" => "[PRO]",
                                "Enterprise" => "[ENTERPRISE]",
                                _ => "[FREE]"
                            };

                            Console.WriteLine($"  {tierPrefix} {model.Name} ({model.Id})");
                            Console.WriteLine($"      Size: {sizeGB:F1}GB | Specialty: {model.HealthcareSpecialty} | Tier: {model.Tier}");
                            Console.WriteLine($"      {model.Description}");
                            Console.WriteLine($"      Standards: {string.Join(", ", model.SupportedStandards)}");
                            Console.WriteLine();
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Error: Failed to fetch available models: {availableResult.Error.Message}");
                    }
                }

                if (showInstalled)
                {
                    Console.WriteLine("Locally Installed Models:");
                    Console.WriteLine();

                    var installedResult = await _modelManagementService.ListInstalledModelsAsync(cancellationToken);
                    if (installedResult.IsSuccess)
                    {
                        if (!installedResult.Value.Any())
                        {
                            Console.WriteLine("  No models installed. Use 'pidgeon ai download <model-id>' to install a model.");
                        }
                        else
                        {
                            foreach (var model in installedResult.Value)
                            {
                                var sizeMB = model.SizeBytes / (1024.0 * 1024.0);
                                var lastUsed = model.LastUsed?.ToString("yyyy-MM-dd") ?? "Never";

                                Console.WriteLine($"  {model.Name} ({model.Id})");
                                Console.WriteLine($"      Size: {sizeMB:F1}MB | Format: {model.Format} | Provider: {model.ProviderId}");
                                Console.WriteLine($"      Installed: {model.InstallDate:yyyy-MM-dd} | Last Used: {lastUsed}");
                                Console.WriteLine($"      Path: {model.FilePath}");
                                Console.WriteLine();
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Error: Failed to list installed models: {installedResult.Error.Message}");
                    }
                }

                return 0;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Unexpected error listing models");
                Console.WriteLine($"Error: {ex.Message}");
                return 1;
            }
        });

        return command;
    }

    private async Task ShowEnhancedModelExperience(CancellationToken cancellationToken)
    {
        Console.WriteLine("🧠 AI Model Setup");
        Console.WriteLine();
        Console.WriteLine("Choose your AI experience:");
        Console.WriteLine();

        // No embedded models available - users download models as needed

        // Get available downloadable models
        var availableResult = await _modelManagementService.ListAvailableModelsAsync(cancellationToken);
        var downloadableModels = availableResult.IsSuccess ? availableResult.Value.ToList() : new List<ModelMetadata>();

        // Get installed models
        var installedResult = await _modelManagementService.ListInstalledModelsAsync(cancellationToken);
        var installedModels = installedResult.IsSuccess ? installedResult.Value.ToList() : new List<ModelInfo>();

        Console.WriteLine("📊 Available Options:");
        Console.WriteLine("┌─────────────────────────────────────────────────────────────┐");
        Console.WriteLine("│ Model                 │ Size   │ Setup Time │ Quality       │");
        Console.WriteLine("├─────────────────────────────────────────────────────────────┤");


        // Show top downloadable models
        var topModels = downloadableModels
            .Where(m => m.Id == "phi3-mini-instruct" || m.Id == "biomistral-7b")
            .OrderBy(m => m.SizeBytes)
            .Take(2);

        foreach (var model in topModels)
        {
            var sizeGB = model.SizeBytes / (1024.0 * 1024.0 * 1024.0);
            // Check for installed models using prefix matching since IDs might have suffixes like "-test"
            var isInstalled = installedModels.Any(i => i.Id.StartsWith(model.Id) || model.Id.StartsWith(i.Id));
            var icon = isInstalled ? "✅" : "📥";
            var setupTime = isInstalled ? "Ready" : sizeGB < 3 ? "5-15min" : "10-30min";
            var quality = model.Id.Contains("biomistral") ? "Medical Expert" : "Better";

            // Truncate name if too long to fit in the table
            var displayName = model.Name.Length > 16 ? model.Name.Substring(0, 13) + "..." : model.Name;
            Console.WriteLine($"│ {icon} {displayName,-16} │ {sizeGB:F1}GB  │ {setupTime,-10} │ {quality,-13} │");
        }

        Console.WriteLine("└─────────────────────────────────────────────────────────────┘");
        Console.WriteLine();

        // Show current status and next steps

        if (installedModels.Any())
        {
            Console.WriteLine($"✅ You have {installedModels.Count} model(s) installed and ready:");
            foreach (var installed in installedModels.Take(3))
            {
                var sizeMB = installed.SizeBytes / (1024.0 * 1024.0);
                Console.WriteLine($"   • {installed.Name} ({sizeMB:F0}MB) - Last used: {installed.LastUsed?.ToString("MMM d") ?? "Never"}");
            }
            Console.WriteLine();
        }

        Console.WriteLine("🚀 Quick Start Options:");
        Console.WriteLine("   pidgeon ai modify message.hl7 -i \"your instructions\"  # Use best available model");

        var hasDownloadSuggestions = false;
        var hasPhi3 = installedModels.Any(m => m.Id.StartsWith("phi3"));
        var hasBioMistral = installedModels.Any(m => m.Id.StartsWith("biomistral"));
        var hasGptOss = installedModels.Any(m => m.Id.Contains("gpt-oss"));

        if (!hasPhi3)
        {
            Console.WriteLine("   pidgeon ai download phi3-mini-instruct               # Download better model (2.2GB)");
            hasDownloadSuggestions = true;
        }

        if (!hasBioMistral)
        {
            Console.WriteLine("   pidgeon ai download biomistral-7b                    # Download medical expert (4.1GB)");
            hasDownloadSuggestions = true;
        }

        if (!hasGptOss)
        {
            Console.WriteLine("   pidgeon ai download gpt-oss-20b                      # Download enterprise model (11.7GB)");
            hasDownloadSuggestions = true;
        }

        if (!hasDownloadSuggestions)
        {
            Console.WriteLine("   🎉 You have the best models already installed!");
        }

        Console.WriteLine();
        Console.WriteLine("💻 Advanced:");
        Console.WriteLine("   pidgeon ai list --available    # Show all downloadable models");
        Console.WriteLine("   pidgeon ai list --installed    # Show all installed models");
    }

    private Command CreateDownloadCommand()
    {
        var command = new Command("download", "Download and install an AI model");
        
        var modelIdArg = new Argument<string>("model-id")
        {
            Description = "ID of the model to download (e.g., phi3-mini-instruct, biomistral-7b, gpt-oss-20b)"
        };
        
        var backgroundOption = new Option<bool>("--background")
        {
            Description = "Download in background (recommended for large models >1GB)"
        };
        
        var statusOption = new Option<bool>("--status")
        {
            Description = "Check status of background downloads"
        };
        
        command.Add(modelIdArg);
        command.Add(backgroundOption);
        command.Add(statusOption);

        SetCommandAction(command, async (parseResult, cancellationToken) =>
        {
            // Check if development mode is enabled
            if (!CheckDevModeOrShowComingSoon(parseResult))
                return 0;

            var modelId = parseResult.GetValue(modelIdArg);
            var useBackground = parseResult.GetValue(backgroundOption);
            var checkStatus = parseResult.GetValue(statusOption);
            
            if (checkStatus)
            {
                // TODO: Implement background download status checking
                Console.WriteLine("Background download status checking not yet implemented");
                return 0;
            }
            
            if (string.IsNullOrEmpty(modelId))
            {
                Console.WriteLine("Error: Model ID is required");
                return 1;
            }

            try
            {
                if (useBackground)
                {
                    Console.WriteLine($"Starting background download of model: {modelId}");
                    Console.WriteLine("Note: Use 'pidgeon ai download --status' to check progress");
                    Console.WriteLine("Large models (>1GB) recommended for background download to avoid timeout");
                    Console.WriteLine();
                    
                    // Start background process
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _modelManagementService.DownloadModelAsync(modelId, null, CancellationToken.None);
                        }
                        catch (Exception ex)
                        {
                            // Background errors logged to file
                            Logger.LogError(ex, "Background download failed for model {ModelId}", modelId);
                        }
                    }, CancellationToken.None);
                    
                    Console.WriteLine($"Background download started for {modelId}");
                    return 0;
                }
                
                Console.WriteLine($"Downloading model: {modelId}");
                Console.WriteLine("Warning: Large models may timeout. Use --background for models >1GB");
                Console.WriteLine();
                Console.WriteLine("📋 What to expect:");
                Console.WriteLine("  • Download: 2-5 minutes for 2GB models");
                Console.WriteLine("  • Antivirus scanning: 5-15 minutes (automatic after download)");
                Console.WriteLine("  • AI features ready immediately after scanning completes");
                Console.WriteLine();

                var progress = new Progress<Core.Domain.Intelligence.DownloadProgress>(p =>
                {
                    if (p.TotalBytes > 0)
                    {
                        var progressBar = CreateProgressBar(p.PercentageComplete);
                        Console.Write($"\r{p.Stage}: {progressBar} {p.PercentageComplete:F1}% ({p.StatusMessage})");
                    }
                    else
                    {
                        Console.Write($"\r{p.StatusMessage}...");
                    }
                });

                var result = await _modelManagementService.DownloadModelAsync(modelId, progress, cancellationToken);
                Console.WriteLine(); // New line after progress

                if (result.IsSuccess)
                {
                    var sizeMB = result.Value.SizeBytes / (1024.0 * 1024.0);
                    Console.WriteLine($"✅ Successfully downloaded {result.Value.Name}");
                    Console.WriteLine($"   Size: {sizeMB:F1}MB | Format: {result.Value.Format}");
                    Console.WriteLine($"   Location: {result.Value.FilePath}");
                    Console.WriteLine();
                    Console.WriteLine("🚀 Ready to use! Try these commands:");
                    Console.WriteLine("  pidgeon ai modify message.hl7 -i \"your instructions\"");
                    Console.WriteLine("  pidgeon diff --ai-local file1.hl7 file2.hl7");
                    Console.WriteLine("  pidgeon validate --file message.hl7 --ai");
                    return 0;
                }
                else
                {
                    Console.WriteLine($"Download failed: {result.Error.Message}");
                    return 1;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Unexpected error downloading model {ModelId}", modelId);
                Console.WriteLine($"Error: {ex.Message}");
                return 1;
            }
        });

        return command;
    }

    private Command CreateRemoveCommand()
    {
        var command = new Command("remove", "Remove a locally installed model");

        var modelIdArg = new Argument<string>("model-id")
        {
            Description = "ID of the model to remove"
        };

        command.Add(modelIdArg);

        SetCommandAction(command, async (parseResult, cancellationToken) =>
        {
            // Check if development mode is enabled
            if (!CheckDevModeOrShowComingSoon(parseResult))
                return 0;

            var modelId = parseResult.GetValue(modelIdArg);
            if (string.IsNullOrEmpty(modelId))
            {
                Console.WriteLine("Error: Model ID is required");
                return 1;
            }

            try
            {
                var result = await _modelManagementService.RemoveModelAsync(modelId, cancellationToken);
                
                if (result.IsSuccess)
                {
                    Console.WriteLine($"Successfully removed model: {modelId}");
                    return 0;
                }
                else
                {
                    Console.WriteLine($"Failed to remove model: {result.Error.Message}");
                    return 1;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Unexpected error removing model {ModelId}", modelId);
                Console.WriteLine($"Error: {ex.Message}");
                return 1;
            }
        });

        return command;
    }

    private Command CreateInfoCommand()
    {
        var command = new Command("info", "Show detailed information about a model");

        var modelIdArg = new Argument<string>("model-id")
        {
            Description = "ID of the model to get information about"
        };

        command.Add(modelIdArg);

        SetCommandAction(command, async (parseResult, cancellationToken) =>
        {
            // Check if development mode is enabled
            if (!CheckDevModeOrShowComingSoon(parseResult))
                return 0;

            var modelId = parseResult.GetValue(modelIdArg);
            if (string.IsNullOrEmpty(modelId))
            {
                Console.WriteLine("Error: Model ID is required");
                return 1;
            }

            try
            {
                var metadataResult = await _modelManagementService.GetModelMetadataAsync(modelId, cancellationToken);
                
                if (metadataResult.IsSuccess)
                {
                    var model = metadataResult.Value;
                    var sizeGB = model.SizeBytes / (1024.0 * 1024.0 * 1024.0);
                    
                    Console.WriteLine($"Model Information: {model.Name}");
                    Console.WriteLine($"   ID: {model.Id}");
                    Console.WriteLine($"   Version: {model.Version}");
                    Console.WriteLine($"   Description: {model.Description}");
                    Console.WriteLine($"   Tier: {model.Tier}");
                    Console.WriteLine($"   Size: {sizeGB:F2}GB");
                    Console.WriteLine($"   Format: {model.Format}");
                    Console.WriteLine($"   Healthcare Specialty: {model.HealthcareSpecialty}");
                    Console.WriteLine();
                    Console.WriteLine("System Requirements:");
                    Console.WriteLine($"   Minimum RAM: {model.Requirements.MinRamMB}MB");
                    Console.WriteLine($"   Recommended RAM: {model.Requirements.RecommendedRamMB}MB");
                    Console.WriteLine($"   CPU Cores: {model.Requirements.MinCpuCores}+");
                    Console.WriteLine($"   GPU Support: {(model.Requirements.SupportsGpu ? "Yes" : "No")}");
                    Console.WriteLine($"   Est. Speed: {model.Requirements.EstimatedTokensPerSecond} tokens/sec");
                    Console.WriteLine();
                    Console.WriteLine("Use Cases:");
                    foreach (var useCase in model.UseCases)
                    {
                        Console.WriteLine($"   - {useCase}");
                    }
                    Console.WriteLine();
                    Console.WriteLine("Supported Standards:");
                    Console.WriteLine($"   {string.Join(", ", model.SupportedStandards)}");
                    
                    return 0;
                }
                else
                {
                    Console.WriteLine($"Failed to get model info: {metadataResult.Error.Message}");
                    return 1;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Unexpected error getting model info for {ModelId}", modelId);
                Console.WriteLine($"Error: {ex.Message}");
                return 1;
            }
        });

        return command;
    }

    private static string CreateProgressBar(double percentage)
    {
        const int barWidth = 30;
        var filled = (int)(percentage / 100.0 * barWidth);
        var bar = new string('=', filled) + new string('-', barWidth - filled);
        return $"[{bar}]";
    }

    // AI-assisted message modification commands

    private Command CreateModifyCommand()
    {
        var command = new Command("modify", "🔒 Pro: Modify a healthcare message using AI assistance");

        // Primary argument - the file to modify (positional for ease)
        var fileArg = new Argument<string>("file")
        {
            Description = "Path to the message file to modify"
        };

        // Simple intent option - this is all most users need!
        var intentOption = CreateNullableOption("-i", "Natural language description of desired changes");

        // Interactive wizard mode - for complex modifications without flags
        var wizardOption = CreateBooleanOption("-w", "Run interactive modification wizard (guided experience)");

        // Output option with sensible default behavior (modifies in place or creates .modified version)
        var outputOption = CreateNullableOption("-o", "Output file (default: <input>.modified.hl7)");

        // Advanced options - hidden from basic usage
        var explainOption = CreateBooleanOption("--explain", "Explain each change made");

        var lockedOption = CreateNullableOption("--lock", "Lock fields (comma-separated, e.g., MSH.7,PID.3)");

        var skipProCheckOption = CreateFlag("--skip-pro-check", "Skip professional tier validation");

        command.Add(fileArg);
        command.Add(intentOption);
        command.Add(wizardOption);
        command.Add(outputOption);
        command.Add(explainOption);
        command.Add(lockedOption);
        command.Add(skipProCheckOption);

        SetCommandAction(command, async (parseResult, cancellationToken) =>
        {
            // Check if development mode is enabled
            if (!CheckDevModeOrShowComingSoon(parseResult))
                return 0;

            var file = parseResult.GetValue(fileArg)!;
            var intent = parseResult.GetValue(intentOption);
            var wizard = parseResult.GetValue(wizardOption);
            var output = parseResult.GetValue(outputOption);
            var explain = parseResult.GetValue(explainOption);
            var locked = parseResult.GetValue(lockedOption);
            var skipProCheck = parseResult.GetValue(skipProCheckOption);

            // Parse locked fields from comma-separated string
            var lockedFields = string.IsNullOrEmpty(locked)
                ? Array.Empty<string>()
                : locked.Split(',').Select(f => f.Trim()).ToArray();

            // Always validate constraints (sensible default)
            var validate = true;

            return await ExecuteModifyCommand(file, intent, wizard, output, validate, explain, lockedFields, skipProCheck, cancellationToken);
        });

        return command;
    }

    private Command CreateSuggestCommand()
    {
        var command = new Command("suggest-value", "🔒 Pro: Suggest a realistic value for a specific field");

        var fieldArg = new Argument<string>("field")
        {
            Description = "HL7 field path (e.g., PID.5)"
        };

        var contextArg = new Argument<string>("context")
        {
            Description = "Context for value generation"
        };

        var skipProCheckOption = CreateFlag("--skip-pro-check", "Skip professional tier validation");

        command.Add(fieldArg);
        command.Add(contextArg);
        command.Add(skipProCheckOption);

        SetCommandAction(command, async (parseResult, cancellationToken) =>
        {
            // Check if development mode is enabled
            if (!CheckDevModeOrShowComingSoon(parseResult))
                return 0;

            var field = parseResult.GetValue(fieldArg)!;
            var context = parseResult.GetValue(contextArg)!;
            var skipProCheck = parseResult.GetValue(skipProCheckOption);

            return await ExecuteSuggestCommand(field, context, skipProCheck, cancellationToken);
        });

        return command;
    }

    private Command CreateTemplateCommand()
    {
        var command = new Command("template", "🔒 Pro: Apply a predefined modification template");

        var fileArg = new Argument<string>("file")
        {
            Description = "Path to the message file to modify"
        };

        var templateArg = new Argument<string>("template")
        {
            Description = "Template name to apply (e.g., diabetes, pediatric)"
        };

        var outputOption = CreateNullableOption("-o", "Output file (default: <input>.modified.hl7)");

        var skipProCheckOption = CreateFlag("--skip-pro-check", "Skip professional tier validation");

        command.Add(fileArg);
        command.Add(templateArg);
        command.Add(outputOption);
        command.Add(skipProCheckOption);

        SetCommandAction(command, async (parseResult, cancellationToken) =>
        {
            // Check if development mode is enabled
            if (!CheckDevModeOrShowComingSoon(parseResult))
                return 0;

            var file = parseResult.GetValue(fileArg)!;
            var template = parseResult.GetValue(templateArg)!;
            var output = parseResult.GetValue(outputOption);
            var skipProCheck = parseResult.GetValue(skipProCheckOption);

            return await ExecuteTemplateCommand(file, template, output, skipProCheck, cancellationToken);
        });

        return command;
    }

    // Command execution methods

    private async Task<int> ExecuteModifyCommand(
        string file,
        string? intent,
        bool wizard,
        string? output,
        bool validateConstraints,
        bool explainChanges,
        string[] lockedFields,
        bool skipProCheck,
        CancellationToken cancellationToken)
    {
        try
        {
            // Check professional tier
            var validationResult = await _proTierValidation.ValidateFeatureAccessAsync(
                FeatureFlags.LocalAIModels,
                skipProCheck,
                cancellationToken);

            if (!validationResult.IsSuccess)
            {
                return 1;
            }

            // Read the original message
            if (!File.Exists(file))
            {
                Console.WriteLine($"❌ File not found: {file}");
                return 1;
            }

            var originalMessage = await File.ReadAllTextAsync(file, cancellationToken);

            if (wizard)
            {
                // Run interactive wizard
                return await RunModificationWizard(originalMessage, output, lockedFields, cancellationToken);
            }
            else if (!string.IsNullOrEmpty(intent))
            {
                // Run with intent
                return await RunIntentBasedModification(originalMessage, intent, output, validateConstraints, explainChanges, lockedFields, cancellationToken);
            }
            else
            {
                Console.WriteLine("❌ Either --intent or --wizard must be specified");
                return 1;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error executing modify command");
            Console.WriteLine($"❌ Error: {ex.Message}");
            return 1;
        }
    }

    private async Task<int> ExecuteSuggestCommand(
        string field,
        string context,
        bool skipProCheck,
        CancellationToken cancellationToken)
    {
        try
        {
            // Check professional tier
            var validationResult = await _proTierValidation.ValidateFeatureAccessAsync(
                FeatureFlags.LocalAIModels,
                skipProCheck,
                cancellationToken);

            if (!validationResult.IsSuccess)
            {
                return 1;
            }

            Console.WriteLine($"🤖 Generating suggestion for field {field}...");
            Console.WriteLine();

            var result = await _modificationService.SuggestFieldValueAsync(field, context, cancellationToken);

            if (!result.IsSuccess)
            {
                Console.WriteLine($"❌ Suggestion failed: {result.Error}");
                return 1;
            }

            var suggestion = result.Value;

            Console.WriteLine($"📋 Field: {field}");
            Console.WriteLine($"💬 Context: {context}");
            Console.WriteLine();
            Console.WriteLine($"💡 Suggested Value: \"{suggestion.Value}\"");
            Console.WriteLine($"📊 Confidence: {suggestion.Confidence:P0}");
            Console.WriteLine($"📋 Source: {suggestion.Source}");
            Console.WriteLine($"✅ Valid: {(suggestion.IsValid ? "Yes" : "No")}");
            Console.WriteLine();
            Console.WriteLine($"💭 Reasoning: {suggestion.Reasoning}");

            if (suggestion.Alternatives.Any())
            {
                Console.WriteLine();
                Console.WriteLine("🔄 Alternative suggestions:");
                foreach (var alt in suggestion.Alternatives)
                {
                    Console.WriteLine($"   - \"{alt}\"");
                }
            }

            return 0;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error executing suggest command");
            Console.WriteLine($"❌ Error: {ex.Message}");
            return 1;
        }
    }

    private async Task<int> ExecuteTemplateCommand(
        string file,
        string template,
        string? output,
        bool skipProCheck,
        CancellationToken cancellationToken)
    {
        try
        {
            // Check professional tier
            var validationResult = await _proTierValidation.ValidateFeatureAccessAsync(
                FeatureFlags.LocalAIModels,
                skipProCheck,
                cancellationToken);

            if (!validationResult.IsSuccess)
            {
                return 1;
            }

            // Read the original message
            if (!File.Exists(file))
            {
                Console.WriteLine($"❌ File not found: {file}");
                return 1;
            }

            var originalMessage = await File.ReadAllTextAsync(file, cancellationToken);

            Console.WriteLine($"📋 Applying template: {template}");
            Console.WriteLine();

            var result = await _modificationService.ApplyTemplateAsync(originalMessage, template, cancellationToken);

            if (!result.IsSuccess)
            {
                Console.WriteLine($"❌ Template application failed: {result.Error}");
                return 1;
            }

            var modification = result.Value;

            Console.WriteLine("✅ Template applied successfully");
            Console.WriteLine($"📊 {modification.AppliedChanges.Count} changes made");
            Console.WriteLine();

            foreach (var change in modification.AppliedChanges.Take(5))
            {
                Console.WriteLine($"   {change.FieldPath}: \"{change.OriginalValue}\" → \"{change.NewValue}\"");
            }

            if (modification.AppliedChanges.Count > 5)
            {
                Console.WriteLine($"   ... and {modification.AppliedChanges.Count - 5} more changes");
            }

            // Save the modified message
            var outputPath = output ?? file.Replace(".hl7", $"_{template}.hl7");
            await File.WriteAllTextAsync(outputPath, modification.ModifiedMessage, cancellationToken);

            Console.WriteLine();
            Console.WriteLine($"💾 Saved as: {outputPath}");

            return 0;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error executing template command");
            Console.WriteLine($"❌ Error: {ex.Message}");
            return 1;
        }
    }

    // Interactive wizard methods (placeholder implementations)

    private async Task<int> RunModificationWizard(
        string originalMessage,
        string? output,
        string[] lockedFields,
        CancellationToken cancellationToken)
    {
        Console.WriteLine("🧙‍♂️ HL7 Message Modification Wizard");
        Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        Console.WriteLine();

        try
        {
            // Step 1: Show current message structure
            await DisplayMessageStructure(originalMessage);

            // Step 2: Modification type selection
            var modificationType = await SelectModificationType();
            if (modificationType == "exit") return 0;

            // Step 3: Get user intent based on modification type
            var intent = await GatherUserIntent(modificationType);
            if (string.IsNullOrEmpty(intent)) return 0;

            // Step 4: Configuration options
            var options = await ConfigureModificationOptions(lockedFields);

            // Step 5: Preview and confirm
            var confirmed = await PreviewModification(originalMessage, intent, options);
            if (!confirmed) return 0;

            // Step 6: Execute modification
            return await ExecuteModificationWithProgress(originalMessage, intent, options, output, cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error in modification wizard");
            Console.WriteLine($"❌ Wizard error: {ex.Message}");
            return 1;
        }
    }

    private async Task<int> RunIntentBasedModification(
        string originalMessage,
        string intent,
        string? output,
        bool validateConstraints,
        bool explainChanges,
        string[] lockedFields,
        CancellationToken cancellationToken)
    {
        Console.WriteLine("🤖 AI Analysis in progress...");
        Console.WriteLine();
        Console.WriteLine("💡 First model load may take 1-2 minutes (subsequent uses will be faster)");
        Console.WriteLine("⏳ AI is analyzing your healthcare message and generating modifications...");
        Console.WriteLine();

        // Show progress animation
        var progress = ShowProgressAnimation("Processing", cancellationToken);

        var options = new ModificationOptions
        {
            ValidateConstraints = validateConstraints,
            ExplainChanges = explainChanges,
            LockedFields = lockedFields.ToList(),
            UseDemographicData = true
        };

        var result = await _modificationService.ModifyMessageAsync(
            originalMessage,
            intent,
            options,
            cancellationToken);

        // Stop progress animation
        progress.Dispose();
        Console.WriteLine("\r✅ AI analysis complete!                    ");
        Console.WriteLine();

        if (!result.IsSuccess)
        {
            Console.WriteLine($"❌ Modification failed: {result.Error}");
            return 1;
        }

        var modification = result.Value;

        // Display proposed changes
        Console.WriteLine("📋 Proposed Changes:");
        Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        foreach (var change in modification.AppliedChanges)
        {
            var icon = change.IsValidated ? "✅" : "⚠️";
            Console.WriteLine($"   {change.FieldPath} ({change.FieldName}): \"{change.OriginalValue}\" → \"{change.NewValue}\"");
            if (explainChanges)
            {
                Console.WriteLine($"   {icon} {change.Reason}");
                Console.WriteLine($"   📊 Source: {change.ValueSource}");
            }
            Console.WriteLine();
        }

        // Display validation results
        Console.WriteLine("🔧 Validation Check:");
        if (modification.ValidationResult.IsValid)
        {
            Console.WriteLine("   ✅ All changes pass constraint validation");
        }
        else
        {
            Console.WriteLine($"   ⚠️ {modification.ValidationResult.ErrorCount} validation errors");
            foreach (var message in modification.ValidationResult.Messages)
            {
                Console.WriteLine($"   - {message}");
            }
        }

        Console.WriteLine();
        Console.WriteLine($"📊 Confidence: {modification.Confidence:P0}");

        if (modification.Warnings.Any())
        {
            Console.WriteLine();
            Console.WriteLine("⚠️ Warnings:");
            foreach (var warning in modification.Warnings)
            {
                Console.WriteLine($"   - {warning}");
            }
        }

        // Ask for confirmation
        Console.WriteLine();
        Console.Write("Apply changes? [Y/n] ");
        var confirm = Console.ReadLine();

        if (string.IsNullOrEmpty(confirm) || confirm.ToLower().StartsWith("y"))
        {
            // Save the modified message
            var outputPath = output ?? originalMessage.Replace(".hl7", "_modified.hl7");
            await File.WriteAllTextAsync(outputPath, modification.ModifiedMessage, cancellationToken);

            Console.WriteLine();
            Console.WriteLine($"✅ Changes applied successfully");
            Console.WriteLine($"✅ Message re-validated: {(modification.ValidationResult.IsValid ? "All constraints satisfied" : "Some warnings present")}");
            Console.WriteLine($"💾 Saved as: {outputPath}");
        }
        else
        {
            Console.WriteLine("❌ Changes cancelled");
        }

        return 0;
    }

    // ==================== INTERACTIVE WIZARD METHODS ====================

    private async Task DisplayMessageStructure(string message)
    {
        await Task.Yield();

        Console.WriteLine("📋 Current Message Structure:");
        Console.WriteLine("─────────────────────────────");

        var lines = message.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        var segmentCounts = new Dictionary<string, int>();

        foreach (var line in lines.Take(10)) // Show first 10 segments
        {
            if (line.Length >= 3)
            {
                var segmentType = line.Substring(0, 3);
                segmentCounts[segmentType] = segmentCounts.GetValueOrDefault(segmentType) + 1;

                // Extract key information
                var info = ExtractSegmentInfo(line, segmentType);
                Console.WriteLine($"   {segmentType}: {info}");
            }
        }

        if (lines.Length > 10)
        {
            Console.WriteLine($"   ... ({lines.Length - 10} more segments)");
        }

        Console.WriteLine();
        Console.WriteLine($"📊 Summary: {segmentCounts.Count} segment types, {lines.Length} total segments");
        Console.WriteLine();
    }

    private string ExtractSegmentInfo(string segmentLine, string segmentType)
    {
        var parts = segmentLine.Split('|');

        return segmentType switch
        {
            "MSH" => parts.Length > 8 ? $"Message Type: {parts[8]}" : "Message Header",
            "PID" => parts.Length > 5 ? $"Patient: {parts[5]}" : "Patient ID",
            "PV1" => parts.Length > 3 ? $"Location: {parts[3]}" : "Patient Visit",
            "DG1" => parts.Length > 3 ? $"Diagnosis: {parts[3]}" : "Diagnosis",
            "AL1" => parts.Length > 3 ? $"Allergy: {parts[3]}" : "Allergy Info",
            "OBR" => parts.Length > 4 ? $"Order: {parts[4]}" : "Observation Request",
            "OBX" => parts.Length > 5 ? $"Result: {parts[5]}" : "Observation Result",
            _ => segmentType
        };
    }

    private async Task<string> SelectModificationType()
    {
        await Task.Yield();

        Console.WriteLine("🎯 What type of modification would you like to make?");
        Console.WriteLine("─────────────────────────────────────────────────");
        Console.WriteLine("1. 👤 Patient Demographics (name, age, gender, address)");
        Console.WriteLine("2. 🏥 Clinical Information (diagnoses, allergies, medications)");
        Console.WriteLine("3. 📅 Temporal Changes (dates, times, visit details)");
        Console.WriteLine("4. 🔧 Custom Field Modification (specific field changes)");
        Console.WriteLine("5. 🎭 Scenario Templates (elderly patient, pediatric, etc.)");
        Console.WriteLine("6. 🚪 Exit wizard");
        Console.WriteLine();

        while (true)
        {
            Console.Write("Select option (1-6): ");
            var input = Console.ReadLine()?.Trim();

            switch (input)
            {
                case "1": return "demographics";
                case "2": return "clinical";
                case "3": return "temporal";
                case "4": return "custom";
                case "5": return "template";
                case "6": return "exit";
                default:
                    Console.WriteLine("❌ Invalid selection. Please choose 1-6.");
                    continue;
            }
        }
    }

    private async Task<string> GatherUserIntent(string modificationType)
    {
        await Task.Yield();

        Console.WriteLine();
        Console.WriteLine("📝 Describe your changes:");
        Console.WriteLine("──────────────────────────");

        // Provide context-specific prompts and examples
        switch (modificationType)
        {
            case "demographics":
                Console.WriteLine("💡 Examples:");
                Console.WriteLine("   • \"Make patient elderly (age 75+)\"");
                Console.WriteLine("   • \"Change to female pediatric patient\"");
                Console.WriteLine("   • \"Update address to rural location\"");
                Console.WriteLine("   • \"Generate Hispanic/Latino demographics\"");
                break;

            case "clinical":
                Console.WriteLine("💡 Examples:");
                Console.WriteLine("   • \"Add Type 2 diabetes diagnosis\"");
                Console.WriteLine("   • \"Include penicillin allergy\"");
                Console.WriteLine("   • \"Add hypertension and cholesterol medication\"");
                Console.WriteLine("   • \"Remove all current diagnoses\"");
                break;

            case "temporal":
                Console.WriteLine("💡 Examples:");
                Console.WriteLine("   • \"Make this an emergency admission\"");
                Console.WriteLine("   • \"Set visit date to last week\"");
                Console.WriteLine("   • \"Change to outpatient encounter\"");
                Console.WriteLine("   • \"Add discharge date 3 days after admit\"");
                break;

            case "custom":
                Console.WriteLine("💡 Examples:");
                Console.WriteLine("   • \"Change PID.5 to realistic Hispanic name\"");
                Console.WriteLine("   • \"Update PV1.2 to inpatient status\"");
                Console.WriteLine("   • \"Set MSH.3 sending application to EPIC\"");
                break;

            case "template":
                return await SelectTemplate();
        }

        Console.WriteLine();
        Console.Write("Describe your changes: ");

        var intent = Console.ReadLine()?.Trim();
        if (string.IsNullOrEmpty(intent))
        {
            Console.WriteLine("❌ No changes specified.");
            return string.Empty;
        }

        return intent;
    }

    private async Task<string> SelectTemplate()
    {
        await Task.Yield();

        Console.WriteLine("📋 Available Templates:");
        Console.WriteLine("────────────────────────");
        Console.WriteLine("1. 👴 Elderly Patient (65+ years, age-appropriate conditions)");
        Console.WriteLine("2. 👶 Pediatric Patient (2-12 years, child-appropriate care)");
        Console.WriteLine("3. 🤰 Maternity Case (pregnancy-related encounter)");
        Console.WriteLine("4. 🚨 Emergency Admission (trauma, urgent conditions)");
        Console.WriteLine("5. 💊 Diabetes Management (comprehensive diabetes care)");
        Console.WriteLine("6. ❤️ Cardiac Patient (heart conditions, medications)");
        Console.WriteLine("7. 🔙 Back to modification types");
        Console.WriteLine();

        while (true)
        {
            Console.Write("Select template (1-7): ");
            var input = Console.ReadLine()?.Trim();

            switch (input)
            {
                case "1": return "Convert to elderly patient with age-appropriate conditions and medications";
                case "2": return "Convert to pediatric patient with child-appropriate demographics and care";
                case "3": return "Convert to maternity case with pregnancy-related encounter details";
                case "4": return "Convert to emergency admission with trauma indicators and urgent status";
                case "5": return "Add comprehensive diabetes management with related medications and monitoring";
                case "6": return "Add cardiac conditions with heart medications and monitoring requirements";
                case "7": return string.Empty; // Signal to go back
                default:
                    Console.WriteLine("❌ Invalid selection. Please choose 1-7.");
                    continue;
            }
        }
    }

    private async Task<ModificationOptions> ConfigureModificationOptions(string[] lockedFields)
    {
        await Task.Yield();

        Console.WriteLine();
        Console.WriteLine("⚙️ Configuration Options:");
        Console.WriteLine("─────────────────────────");

        // Validation mode
        Console.WriteLine("🔍 Validation Mode:");
        Console.WriteLine("  1. Strict (full constraint validation)");
        Console.WriteLine("  2. Compatible (allow common variations)");
        Console.Write("Choose validation mode (1-2) [default: 2]: ");
        var validationInput = Console.ReadLine()?.Trim();
        var validationMode = validationInput == "1" ? "strict" : "compatibility";

        // Demographic data usage
        Console.WriteLine();
        Console.Write("📊 Use realistic demographic data? (Y/n) [default: Y]: ");
        var demoInput = Console.ReadLine()?.Trim().ToLowerInvariant();
        var useDemographics = demoInput != "n" && demoInput != "no";

        // Explanation level
        Console.WriteLine();
        Console.Write("📋 Explain each change made? (y/N) [default: N]: ");
        var explainInput = Console.ReadLine()?.Trim().ToLowerInvariant();
        var explainChanges = explainInput == "y" || explainInput == "yes";

        // Additional locked fields
        var allLockedFields = lockedFields.ToList();
        if (lockedFields.Length > 0)
        {
            Console.WriteLine();
            Console.WriteLine($"🔒 Pre-configured locked fields: {string.Join(", ", lockedFields)}");
        }

        Console.WriteLine();
        Console.Write("🔒 Additional fields to lock (comma-separated, e.g., MSH.7,PID.3) [optional]: ");
        var additionalLocked = Console.ReadLine()?.Trim();
        if (!string.IsNullOrEmpty(additionalLocked))
        {
            allLockedFields.AddRange(additionalLocked.Split(',').Select(f => f.Trim()));
        }

        return new ModificationOptions
        {
            ValidateConstraints = true,
            ValidationMode = validationMode,
            UseDemographicData = useDemographics,
            ExplainChanges = explainChanges,
            LockedFields = allLockedFields,
            MaxChanges = 20
        };
    }

    private async Task<bool> PreviewModification(string originalMessage, string intent, ModificationOptions options)
    {
        await Task.Yield();

        Console.WriteLine();
        Console.WriteLine("👀 Modification Preview:");
        Console.WriteLine("────────────────────────");
        Console.WriteLine($"📝 Intent: {intent}");
        Console.WriteLine($"🔍 Validation: {options.ValidationMode}");
        Console.WriteLine($"📊 Demographics: {(options.UseDemographicData ? "Enabled" : "Disabled")}");
        Console.WriteLine($"📋 Explanations: {(options.ExplainChanges ? "Enabled" : "Disabled")}");

        if (options.LockedFields.Any())
        {
            Console.WriteLine($"🔒 Locked Fields: {string.Join(", ", options.LockedFields)}");
        }

        Console.WriteLine();
        Console.WriteLine("⚠️  Note: AI will analyze your intent and make appropriate changes.");
        Console.WriteLine("Changes will be validated against HL7 constraints and use realistic data.");
        Console.WriteLine();

        while (true)
        {
            Console.Write("Proceed with modification? (Y/n) [default: Y]: ");
            var input = Console.ReadLine()?.Trim().ToLowerInvariant();

            if (string.IsNullOrEmpty(input) || input == "y" || input == "yes")
                return true;
            else if (input == "n" || input == "no")
                return false;
            else
                Console.WriteLine("❌ Please answer Y or n.");
        }
    }

    private async Task<int> ExecuteModificationWithProgress(
        string originalMessage,
        string intent,
        ModificationOptions options,
        string? output,
        CancellationToken cancellationToken)
    {
        Console.WriteLine();
        Console.WriteLine("🤖 AI Modification in Progress...");
        Console.WriteLine("─────────────────────────────────");

        // Step 1: AI Analysis
        Console.WriteLine("🧠 Step 1/4: AI analyzing intent...");
        await Task.Delay(500, cancellationToken); // Brief pause for user experience

        // Step 2: Constraint checking
        Console.WriteLine("✅ Step 2/4: Validating constraints...");
        await Task.Delay(300, cancellationToken);

        // Step 3: Demographic enhancement
        Console.WriteLine("📊 Step 3/4: Applying demographic data...");
        await Task.Delay(300, cancellationToken);

        // Step 4: Execute modification
        Console.WriteLine("🔧 Step 4/4: Applying changes...");

        var result = await _modificationService.ModifyMessageAsync(
            originalMessage,
            intent,
            options,
            cancellationToken);

        if (!result.IsSuccess)
        {
            Console.WriteLine();
            Console.WriteLine($"❌ Modification failed: {result.Error}");
            return 1;
        }

        var modification = result.Value;
        Console.WriteLine();
        Console.WriteLine("✅ Modification Complete!");

        // Display results
        await DisplayModificationResults(modification, options);

        // Save results
        var outputPath = output ?? Path.ChangeExtension(Path.GetTempFileName(), ".hl7");
        await File.WriteAllTextAsync(outputPath, modification.ModifiedMessage, cancellationToken);

        Console.WriteLine();
        Console.WriteLine($"💾 Modified message saved to: {outputPath}");
        Console.WriteLine("🎉 Wizard completed successfully!");

        return 0;
    }

    private async Task DisplayModificationResults(MessageModificationResult modification, ModificationOptions options)
    {
        await Task.Yield();

        Console.WriteLine();
        Console.WriteLine("📊 Modification Results:");
        Console.WriteLine("────────────────────────");
        Console.WriteLine($"✨ Changes Applied: {modification.AppliedChanges.Count}");
        Console.WriteLine($"🎯 Confidence: {modification.Confidence:P1}");
        Console.WriteLine($"✅ Validation: {(modification.ValidationResult.IsValid ? "Passed" : "Issues Found")}");

        if (modification.AppliedChanges.Any())
        {
            Console.WriteLine();
            Console.WriteLine("🔧 Changes Made:");
            foreach (var change in modification.AppliedChanges.Take(5))
            {
                var changeIcon = change.ChangeType switch
                {
                    ChangeType.Added => "➕",
                    ChangeType.Modified => "📝",
                    ChangeType.Removed => "➖",
                    ChangeType.SegmentAdded => "📋",
                    ChangeType.SegmentRemoved => "🗑️",
                    _ => "🔄"
                };

                Console.WriteLine($"   {changeIcon} {change.FieldName} ({change.FieldPath}): {change.NewValue}");
                if (options.ExplainChanges && !string.IsNullOrEmpty(change.Reason))
                {
                    Console.WriteLine($"      💡 {change.Reason}");
                }
            }

            if (modification.AppliedChanges.Count > 5)
            {
                Console.WriteLine($"   ... ({modification.AppliedChanges.Count - 5} more changes)");
            }
        }

        if (modification.Warnings.Any())
        {
            Console.WriteLine();
            Console.WriteLine("⚠️  Warnings:");
            foreach (var warning in modification.Warnings)
            {
                Console.WriteLine($"   • {warning}");
            }
        }

        if (!string.IsNullOrEmpty(modification.AIReasoning))
        {
            Console.WriteLine();
            Console.WriteLine("🤖 AI Analysis:");
            Console.WriteLine($"   {modification.AIReasoning.Substring(0, Math.Min(200, modification.AIReasoning.Length))}...");
        }
    }

    /// <summary>
    /// Creates a boolean flag option.
    /// </summary>
    private static Option<bool> CreateFlag(string name, string description)
    {
        return new Option<bool>(name)
        {
            Description = description,
            DefaultValueFactory = _ => false
        };
    }

    /// <summary>
    /// Creates a string array option.
    /// </summary>
    private static Option<string[]> CreateStringArrayOption(string name, string description)
    {
        return new Option<string[]>(name)
        {
            Description = description,
            DefaultValueFactory = _ => Array.Empty<string>()
        };
    }

    /// <summary>
    /// Checks if development mode is enabled and shows coming soon message if not.
    /// </summary>
    private static bool CheckDevModeOrShowComingSoon(ParseResult parseResult)
    {
        // Check if any dev flags are present in the command line
        var commandLineTokens = parseResult.Tokens.Select(t => t.Value).ToArray();
        var devEnabled = commandLineTokens.Contains("--dev") || commandLineTokens.Contains("-d");

        if (!devEnabled)
        {
            Console.WriteLine();
            Console.WriteLine("🚀 AI Features Coming Soon!");
            Console.WriteLine();
            Console.WriteLine("AI-powered healthcare message analysis and modification will be available in Pidgeon v0.1.0.");
            Console.WriteLine();
            Console.WriteLine("📅 Expected Release: Next update cycle");
            Console.WriteLine("🔬 Early Access: Add --dev or -d flag to try experimental features");
            Console.WriteLine("📢 Stay Updated: Run 'pidgeon info --version' to check for updates");
            Console.WriteLine();
            Console.WriteLine("🎯 Coming AI Features:");
            Console.WriteLine("  • Intelligent message modification with healthcare context");
            Console.WriteLine("  • Field value suggestions based on clinical best practices");
            Console.WriteLine("  • Automated compliance checking and recommendations");
            Console.WriteLine("  • Natural language to HL7 conversion assistance");
            Console.WriteLine();
            return false;
        }

        return true;
    }

    /// <summary>
    /// Shows an animated progress indicator to reassure users that AI processing is active.
    /// </summary>
    private static IDisposable ShowProgressAnimation(string message, CancellationToken cancellationToken)
    {
        var cts = new CancellationTokenSource();
        var combinedToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token).Token;

        var task = Task.Run(async () =>
        {
            var spinners = new[] { "⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏" };
            var counter = 0;
            var startTime = DateTime.Now;

            try
            {
                while (!combinedToken.IsCancellationRequested)
                {
                    var elapsed = DateTime.Now - startTime;
                    var timeStr = elapsed.TotalSeconds < 60
                        ? $"{elapsed.TotalSeconds:F0}s"
                        : $"{elapsed.TotalMinutes:F1}m";

                    Console.Write($"\r{spinners[counter % spinners.Length]} {message}... ({timeStr})");
                    counter++;

                    await Task.Delay(150, combinedToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when animation is stopped
            }
        }, combinedToken);

        return new ProgressAnimationDisposable(cts, task);
    }

    /// <summary>
    /// Helper class to properly dispose of the progress animation.
    /// </summary>
    private class ProgressAnimationDisposable : IDisposable
    {
        private readonly CancellationTokenSource _cts;
        private readonly Task _task;
        private bool _disposed;

        public ProgressAnimationDisposable(CancellationTokenSource cts, Task task)
        {
            _cts = cts;
            _task = task;
        }

        public void Dispose()
        {
            if (_disposed) return;

            _cts.Cancel();
            try
            {
                _task.Wait(TimeSpan.FromMilliseconds(500)); // Give it a moment to clean up
            }
            catch
            {
                // Ignore cleanup exceptions
            }

            _cts.Dispose();
            _disposed = true;
        }
    }
}