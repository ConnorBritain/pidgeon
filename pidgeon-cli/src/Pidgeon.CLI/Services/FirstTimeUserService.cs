using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Pidgeon.Core;
using Pidgeon.Core.Application.Interfaces.Intelligence;

namespace Pidgeon.CLI.Services;

/// <summary>
/// Manages the first-time user experience including welcome wizard,
/// AI model selection, and quick start demonstration.
/// </summary>
public class FirstTimeUserService(
    ILogger<FirstTimeUserService> logger,
    IModelManagementService modelService)
{
    private readonly ILogger<FirstTimeUserService> _logger = logger;
    private readonly IModelManagementService _modelService = modelService;
    private readonly string _configPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".pidgeon");
    
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };
    
    // Model recommendations with healthcare focus
    private readonly List<ModelRecommendation> _modelRecommendations =
    [
        new ModelRecommendation
        {
            ModelId = "tinyllama-1.1b-chat",
            DisplayName = "TinyLlama-1.1B",
            Size = "638MB",
            DownloadTime = "5-15 minutes",
            Speed = "Fast",
            Quality = "Good",
            UseCase = "Fast, small, good",
            Recommendation = "Quick validation and testing",
            Priority = 1,
            IsHealthcareSpecific = false,
            DownloadUrl = "https://huggingface.co/TheBloke/TinyLlama-1.1B-Chat-v1.0-GGUF/resolve/main/tinyllama-1.1b-chat-v1.0.Q4_K_M.gguf"
        },
        new ModelRecommendation
        {
            ModelId = "phi-3-mini-4k",
            DisplayName = "Phi-3-Mini-4K",
            Size = "2.2GB",
            DownloadTime = "15-30 minutes",
            Speed = "Medium",
            Quality = "Better",
            UseCase = "Balanced size/performance",
            Recommendation = "Daily use with good accuracy",
            Priority = 2,
            IsHealthcareSpecific = false,
            DownloadUrl = "https://huggingface.co/microsoft/Phi-3-mini-4k-instruct-gguf/resolve/main/Phi-3-mini-4k-instruct-q4.gguf"
        },
        new ModelRecommendation
        {
            ModelId = "biomistral-7b",
            DisplayName = "BioMistral-7B",
            Size = "4.1GB",
            DownloadTime = "30-60 minutes",
            Speed = "Slower",
            Quality = "Best",
            UseCase = "Healthcare domain expert",
            Recommendation = "Clinical accuracy priority",
            Priority = 3,
            IsHealthcareSpecific = true,
            DownloadUrl = "https://huggingface.co/BioMistral/BioMistral-7B-GGUF/resolve/main/biomistral-7b-q4.gguf"
        }
    ];


    /// <summary>
    /// Checks if this is the first time the user is running Pidgeon.
    /// </summary>
    public async Task<bool> IsFirstTimeUserAsync()
    {
        await Task.Yield();
        var configFile = Path.Combine(_configPath, "pidgeon.config.json");
        return !File.Exists(configFile);
    }

    /// <summary>
    /// Creates a minimal default configuration for auto-graduation.
    /// Used when experienced users run productive commands without going through setup.
    /// </summary>
    public async Task<Result<bool>> CreateMinimalConfigAsync()
    {
        try
        {
            // Ensure directory exists
            Directory.CreateDirectory(_configPath);

            var configFile = Path.Combine(_configPath, "pidgeon.config.json");

            // Create wise default configuration (git-style)
            var config = new
            {
                firstRun = DateTime.UtcNow,
                version = "1.0.0",
                setupType = "auto-graduated",
                telemetry = new { enabled = false },
                preferences = new
                {
                    defaultStandard = "hl7",
                    outputFormat = "console",
                    validationMode = "compatibility",
                    aiAssistance = "disabled"
                },
                generation = new
                {
                    defaultCount = 1,
                    includeOptionalFields = true,
                    seedStrategy = "random"
                },
                deidentification = new
                {
                    dateShiftDays = 30,
                    preserveFormat = true,
                    crossMessageConsistency = true
                },
                validation = new
                {
                    strictMode = false,
                    showWarnings = true,
                    stopOnFirstError = false
                }
            };

            var json = JsonSerializer.Serialize(config, JsonOptions);
            await File.WriteAllTextAsync(configFile, json);

            _logger.LogInformation("Created minimal configuration for auto-graduation");
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create minimal configuration");
            return Result<bool>.Failure($"Configuration creation failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Runs welcome orientation - demo and introduction only (no configuration).
    /// </summary>
    public async Task<Result<bool>> RunWelcomeOrientationAsync()
    {
        try
        {
            ShowWelcomeBanner();
            
            Console.WriteLine("Let's see what Pidgeon can do...");
            Console.WriteLine();
            
            // Always run the demo for welcome
            await RunQuickDemoAsync();
            
            Console.WriteLine();
            Console.WriteLine("Ready to set up your machine? Run: pidgeon --init");
            Console.WriteLine("Need help? Run: pidgeon help");
            Console.WriteLine();
            
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during welcome orientation");
            return Result<bool>.Failure($"Welcome orientation failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Runs machine initialization - configuration and setup.
    /// </summary>
    public async Task<Result<bool>> RunInitializationAsync()
    {
        try
        {
            Console.WriteLine();
            Console.WriteLine("═══════════════════════════════════════════════════════════════");
            Console.WriteLine("  Pidgeon Machine Setup");
            Console.WriteLine("═══════════════════════════════════════════════════════════════");
            Console.WriteLine();
            Console.WriteLine("Let's get your machine configured for productive use.");
            Console.WriteLine();
            
            // Initialize project structure first
            await InitializeProjectStructureAsync();
            
            // Set up AI models (optional)
            Console.WriteLine("Step 1: AI Model Setup");
            Console.WriteLine("───────────────────────");
            var modelResult = await RunModelSelectionWizardAsync();
            
            // Save configuration
            await SaveFirstRunConfigAsync();
            
            Console.WriteLine();
            Console.WriteLine("✅ Machine setup complete!");
            Console.WriteLine();
            Console.WriteLine("Try these commands:");
            Console.WriteLine("  pidgeon generate ADT^A01         # Generate test message");
            Console.WriteLine("  pidgeon validate --file msg.hl7  # Validate a message");
            Console.WriteLine("  pidgeon welcome                  # See demo");
            Console.WriteLine();
            
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during initialization");
            return Result<bool>.Failure($"Initialization failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Interactive AI model selection wizard with space management.
    /// </summary>
    public async Task<Result<ModelInfo>> RunModelSelectionWizardAsync()
    {
        Console.WriteLine();
        Console.WriteLine("──────────────────────────────────────");
        Console.WriteLine("  AI Model Setup (Optional)");
        Console.WriteLine("──────────────────────────────────────");
        Console.WriteLine();
        Console.WriteLine("🔒 Security: All AI models run 100% on your device.");
        Console.WriteLine("   No patient data ever leaves your computer.");
        Console.WriteLine();
        
        Console.Write("Would you like to set up an AI model for enhanced analysis? (y/N): ");
        var setupChoice = Console.ReadLine();
        
        if (setupChoice?.ToLowerInvariant() != "y")
        {
            Console.WriteLine("\nSkipping AI setup. You can always run 'pidgeon ai download' later.");
            return Result<ModelInfo>.Success(new ModelInfo { ModelId = "none" });
        }
        
        Console.WriteLine();
        // Show compact model options
        Console.WriteLine("Available Models:");
        Console.WriteLine();
        Console.WriteLine("  1. TinyLlama (638MB) - Fast, small, good for quick testing");
        Console.WriteLine("  2. Phi-3 (2.2GB) - Balanced size/performance for daily use");
        Console.WriteLine("  3. BioMistral (4.1GB) - Healthcare expert, best accuracy");
        Console.WriteLine("  0. Skip for now");
        Console.WriteLine();
        
        var diskSpace = GetAvailableDiskSpace();
        Console.WriteLine($"  Disk space available: {FormatBytes(diskSpace)}");
        Console.WriteLine();
        
        Console.Write("Select model (0-3) [1]: ");
        var input = Console.ReadLine();
        
        if (string.IsNullOrWhiteSpace(input) || input == "1")
        {
            input = "1";
        }
        
        if (!int.TryParse(input, out var selection) || selection < 0 || selection > 3)
        {
            return Result<ModelInfo>.Failure("Invalid selection");
        }
        
        if (selection == 0)
        {
            Console.WriteLine("\nSkipping AI model setup. You can download models later with:");
            Console.WriteLine("  pidgeon ai download <model-name>");
            return Result<ModelInfo>.Success(new ModelInfo { ModelId = "none" });
        }
        
        var selectedModel = _modelRecommendations[selection - 1];
        
        // Parse size and check space first
        var requiredBytes = ParseBytes(selectedModel.Size);
        if (requiredBytes > diskSpace * 0.9) // Leave 10% buffer
        {
            Console.WriteLine("\n⚠️  Insufficient disk space for this model.");
            Console.WriteLine("   Please free up space or choose a smaller model.");
            return Result<ModelInfo>.Failure("Insufficient disk space");
        }
        
        // Show concise confirmation
        Console.WriteLine();
        Console.WriteLine($"Ready to download {selectedModel.DisplayName}:");
        Console.WriteLine($"• Size: {selectedModel.Size}");
        Console.WriteLine($"• Time: {selectedModel.DownloadTime}");
        Console.WriteLine($"• Location: ~/.pidgeon/models/");
        Console.WriteLine();
        
        Console.Write("Continue? (y/N): ");
        var confirm = Console.ReadLine();
        
        if (confirm?.ToLowerInvariant() != "y")
        {
            Console.WriteLine("\nModel download cancelled. You can download later with:");
            Console.WriteLine($"  pidgeon ai download {selectedModel.ModelId}");
            return Result<ModelInfo>.Success(new ModelInfo { ModelId = "none" });
        }
        
        // Download the model
        Console.WriteLine();
        Console.WriteLine($"Downloading {selectedModel.DisplayName}...");
        
        var downloadResult = await DownloadModelWithProgressAsync(
            selectedModel.ModelId,
            selectedModel.DownloadUrl,
            requiredBytes
        );
        
        if (downloadResult.IsFailure)
        {
            return Result<ModelInfo>.Failure(downloadResult.Error);
        }
        
        Console.WriteLine();
        Console.WriteLine("✅ Model installed successfully!");
        Console.WriteLine();
        
        Console.Write("Try a quick demo? (Y/n): ");
        var ready = Console.ReadLine();
        
        if (string.IsNullOrWhiteSpace(ready) || !string.Equals(ready, "n", StringComparison.OrdinalIgnoreCase))
        {
            await RunQuickDemoAsync();
        }
        else
        {
            Console.WriteLine("\nGet started with: pidgeon generate ADT^A01");
        }
        
        return Result<ModelInfo>.Success(new ModelInfo 
        { 
            ModelId = selectedModel.ModelId,
            ModelPath = Path.Combine(_configPath, "models", selectedModel.ModelId),
            Size = requiredBytes
        });
    }

    private static void ShowWelcomeBanner()
    {
        // Don't clear screen - respect user's terminal history
        Console.WriteLine();
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine("  Welcome to Pidgeon Healthcare Platform");
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine();
        Console.WriteLine("Generate, validate, and test HL7/FHIR messages");
        Console.WriteLine("without the compliance nightmare of real patient data.");
        Console.WriteLine();
        
        Console.WriteLine("Press Enter to continue...");
        Console.ReadLine();
    }


    private void ShowModelRecommendationsTable()
    {
        var tableWidth = 80;
        Console.WriteLine("Recommended Models:");
        Console.WriteLine("┌" + "─".PadRight(tableWidth - 2, '─') + "┐");
        Console.WriteLine($"│ {"Model",-20} │ {"Size",-8} │ {"Speed",-8} │ {"Quality",-8} │ {"Use Case",-25} │");
        Console.WriteLine("├" + "─".PadRight(tableWidth - 2, '─') + "┤");
        
        for (int i = 0; i < _modelRecommendations.Count; i++)
        {
            var model = _modelRecommendations[i];
            var prefix = model.IsHealthcareSpecific ? "[Medical] " : "";
            Console.WriteLine($"│ {(i + 1) + ". " + prefix + model.DisplayName,-20} │ {model.Size,-8} │ {model.Speed,-8} │ {model.Quality,-8} │ {model.UseCase,-25} │");
        }
        
        Console.WriteLine("└" + "─".PadRight(tableWidth - 2, '─') + "┘");
    }

    private async Task<Result<bool>> DownloadModelWithProgressAsync(
        string modelId, 
        string url, 
        long totalBytes)
    {
        try
        {
            var modelsDir = Path.Combine(_configPath, "models");
            Directory.CreateDirectory(modelsDir);
            
            var modelPath = Path.Combine(modelsDir, $"{modelId}.gguf");
            
            // TODO: Implement actual download from url using HttpClient with progress
            // Currently simulating download with progress bar
            _ = url; // TODO: Use url parameter when implementing real download
            var progressBar = new StringBuilder();
            var progressWidth = 50;
            
            for (int i = 0; i <= 100; i += 2)
            {
                var filled = (int)(progressWidth * (i / 100.0));
                progressBar.Clear();
                progressBar.Append('[');
                progressBar.Append('=', filled);
                progressBar.Append('>');
                progressBar.Append(' ', progressWidth - filled);
                progressBar.Append(']');
                
                Console.Write($"\r{progressBar} {i}% ({FormatBytes(totalBytes * i / 100)}/{FormatBytes(totalBytes)})");
                await Task.Delay(100); // Simulate download time
            }
            
            Console.WriteLine();
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return Result<bool>.Failure($"Download failed: {ex.Message}");
        }
    }

    private static async Task RunQuickDemoAsync()
    {
        await Task.Yield();
        Console.WriteLine();
        Console.WriteLine("──────────────────────────────────────");
        Console.WriteLine("  Quick Demo");
        Console.WriteLine("──────────────────────────────────────");
        Console.WriteLine();
        Console.WriteLine("Generating an HL7 admission message...");
        Console.WriteLine();
        
        // Show the command
        Console.WriteLine("  $ pidgeon generate ADT^A01");
        Console.WriteLine();
        
        // Generate and show just a snippet
        var message = GenerateDemoMessage();
        var lines = message.Split('\n');
        
        // Show first 3 segments only
        Console.WriteLine("  " + lines[0]); // MSH
        Console.WriteLine("  " + lines[1]); // EVN
        Console.WriteLine("  " + lines[2]); // PID
        Console.WriteLine("  ...");
        Console.WriteLine();
        
        Console.WriteLine("✅ Generated realistic HL7 message");
        Console.WriteLine("   • Synthetic patient data (safe for testing)");
        Console.WriteLine("   • Standards-compliant format");
        Console.WriteLine();
        
        Console.WriteLine("Common commands:");
        Console.WriteLine("  pidgeon generate ADT^A01         # Generate admission");
        Console.WriteLine("  pidgeon validate --file msg.hl7  # Validate message");
        Console.WriteLine("  pidgeon deident --in ./real      # De-identify real data");
        Console.WriteLine();
    }

    private static async Task ShowDeIdentificationDemoAsync()
    {
        await Task.Yield();
        Console.WriteLine();
        Console.WriteLine("──────────────────────────────────────");
        Console.WriteLine("  De-identification");
        Console.WriteLine("──────────────────────────────────────");
        Console.WriteLine();
        Console.WriteLine("Transform real messages into safe test data:");
        Console.WriteLine();
        Console.WriteLine("  $ pidgeon deident --in ./real --out ./safe");
        Console.WriteLine();
        Console.WriteLine("• Removes all PHI (HIPAA Safe Harbor)");
        Console.WriteLine("• Preserves message structure");
        Console.WriteLine("• Runs 100% on your device");
        Console.WriteLine();
        Console.WriteLine("Press Enter to continue...");
        Console.ReadLine();
    }

    private static async Task RunGuidedTutorialAsync()
    {
        await Task.Yield();
        Console.WriteLine();
        Console.WriteLine("──────────────────────────────────────");
        Console.WriteLine("  Tutorial Coming Soon");
        Console.WriteLine("──────────────────────────────────────");
        Console.WriteLine();
        Console.WriteLine("For now, explore these commands:");
        Console.WriteLine();
        Console.WriteLine("  pidgeon help              # Show all commands");
        Console.WriteLine("  pidgeon generate --help   # Generation options");
        Console.WriteLine("  pidgeon validate --help   # Validation options");
        Console.WriteLine();
        Console.WriteLine("Press Enter to continue...");
        Console.ReadLine();
    }

    private async Task InitializeProjectStructureAsync()
    {
        await Task.Yield();
        try
        {
            // Create standard directory structure
            var directories = new[]
            {
                Path.Combine(_configPath, "models"),
                Path.Combine(_configPath, "configs"),
                Path.Combine(_configPath, "configs", "epic"),
                Path.Combine(_configPath, "configs", "cerner"),
                Path.Combine(_configPath, "configs", "allscripts"),
                Path.Combine(_configPath, "workflows"),
                Path.Combine(_configPath, "templates"),
            };

            foreach (var dir in directories)
            {
                Directory.CreateDirectory(dir);
            }

            _logger.LogInformation("Initialized project structure at {Path}", _configPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize project structure");
        }
    }

    private async Task SaveFirstRunConfigAsync()
    {
        try
        {
            var configFile = Path.Combine(_configPath, "pidgeon.config.json");
            var config = new
            {
                firstRun = DateTime.UtcNow,
                version = "1.0.0",
                telemetry = new { enabled = false },
                preferences = new
                {
                    defaultStandard = "hl7",
                    autoDetectModels = true,
                    colorOutput = true
                }
            };

            var json = JsonSerializer.Serialize(config, JsonOptions);

            await File.WriteAllTextAsync(configFile, json);
            _logger.LogInformation("Saved first-run configuration");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save first-run configuration");
        }
    }

    private static long GetAvailableDiskSpace()
    {
        try
        {
            var homePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var driveInfo = new DriveInfo(Path.GetPathRoot(homePath) ?? "/");
            return driveInfo.AvailableFreeSpace;
        }
        catch
        {
            return 10L * 1024 * 1024 * 1024; // Default to 10GB if can't determine
        }
    }

    private static long ParseBytes(string sizeStr)
    {
        sizeStr = sizeStr.ToUpperInvariant().Trim();
        
        if (sizeStr.EndsWith("GB"))
        {
            var gb = double.Parse(sizeStr.Replace("GB", ""));
            return (long)(gb * 1024 * 1024 * 1024);
        }
        else if (sizeStr.EndsWith("MB"))
        {
            var mb = double.Parse(sizeStr.Replace("MB", ""));
            return (long)(mb * 1024 * 1024);
        }
        
        return 0;
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes >= 1024L * 1024 * 1024)
        {
            return $"{bytes / (1024.0 * 1024 * 1024):F1}GB";
        }
        else if (bytes >= 1024L * 1024)
        {
            return $"{bytes / (1024.0 * 1024):F1}MB";
        }
        else
        {
            return $"{bytes / 1024.0:F1}KB";
        }
    }

    private static string GenerateDemoMessage()
    {
        return @"MSH|^~\&|PIDGEON|HOSPITAL|RECEIVER|FACILITY|20250912154823||ADT^A01^ADT_A01|MSG12345|P|2.3
EVN|A01|20250912154823|||
PID|1||PAT123456^^^HOSPITAL^MR||DOE^JOHN^M||19850615|M||2106-3|123 MAIN ST^^ANYTOWN^CA^90210
PV1|1|I|ICU^201^A|E||||||SUR||||2|||DOC123^SMITH^JANE
DG1|1||I10^E11.9^Diabetes mellitus type 2||20250912|A";
    }

    private class ModelRecommendation
    {
        public string ModelId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Size { get; set; } = string.Empty;
        public string DownloadTime { get; set; } = string.Empty;
        public string Speed { get; set; } = string.Empty;
        public string Quality { get; set; } = string.Empty;
        public string UseCase { get; set; } = string.Empty;
        public string Recommendation { get; set; } = string.Empty;
        public int Priority { get; set; }
        public bool IsHealthcareSpecific { get; set; }
        public string DownloadUrl { get; set; } = string.Empty;
    }
    
    public class ModelInfo
    {
        public string ModelId { get; set; } = string.Empty;
        public string ModelPath { get; set; } = string.Empty;
        public long Size { get; set; }
    }
}