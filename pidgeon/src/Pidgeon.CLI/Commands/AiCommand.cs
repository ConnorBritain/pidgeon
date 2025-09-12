// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Microsoft.Extensions.Logging;
using Pidgeon.Core.Application.Interfaces.Intelligence;
using System.CommandLine;

namespace Pidgeon.CLI.Commands;

/// <summary>
/// Command for AI model management and configuration.
/// Enables downloading, installing, and managing local healthcare AI models.
/// </summary>
public class AiCommand : CommandBuilderBase
{
    private readonly IModelManagementService _modelManagementService;

    public AiCommand(
        ILogger<AiCommand> logger,
        IModelManagementService modelManagementService)
        : base(logger)
    {
        _modelManagementService = modelManagementService;
    }

    public override Command CreateCommand()
    {
        var command = new Command("ai", "Manage local AI models for healthcare analysis");

        // Add subcommands
        command.Add(CreateListCommand());
        command.Add(CreateDownloadCommand());
        command.Add(CreateRemoveCommand());
        command.Add(CreateInfoCommand());

        return command;
    }

    private Command CreateListCommand()
    {
        var command = new Command("list", "List available and installed AI models");
        
        var availableOption = CreateBooleanOption("--available", "Show available models for download");
        var installedOption = CreateBooleanOption("--installed", "Show locally installed models");
        
        command.Add(availableOption);
        command.Add(installedOption);

        SetCommandAction(command, async (parseResult, cancellationToken) =>
        {
            var showAvailable = parseResult.GetValue(availableOption);
            var showInstalled = parseResult.GetValue(installedOption);
            
            // Default to showing both if no flags specified
            if (!showAvailable && !showInstalled)
            {
                showAvailable = showInstalled = true;
            }

            try
            {
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

    private Command CreateDownloadCommand()
    {
        var command = new Command("download", "Download and install an AI model");
        
        var modelIdArg = new Argument<string>("model-id")
        {
            Description = "ID of the model to download (e.g., phi2-healthcare, tinyllama-medical)"
        };
        
        command.Add(modelIdArg);

        SetCommandAction(command, async (parseResult, cancellationToken) =>
        {
            var modelId = parseResult.GetValue(modelIdArg);
            if (string.IsNullOrEmpty(modelId))
            {
                Console.WriteLine("Error: Model ID is required");
                return 1;
            }

            try
            {
                Console.WriteLine($"Downloading model: {modelId}");
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
                    Console.WriteLine($"Successfully downloaded {result.Value.Name}");
                    Console.WriteLine($"   Size: {sizeMB:F1}MB | Format: {result.Value.Format}");
                    Console.WriteLine($"   Location: {result.Value.FilePath}");
                    Console.WriteLine();
                    Console.WriteLine("Use 'pidgeon diff --ai-local' to use local AI analysis");
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
}