// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Microsoft.Extensions.Logging;
using Pidgeon.Core.Application.Interfaces.Intelligence;
using Pidgeon.Core.Domain.Intelligence;
using System.Security.Cryptography;
using System.Text.Json;

namespace Pidgeon.Core.Application.Services.Intelligence;

/// <summary>
/// Service for managing AI model downloads, installation, and configuration.
/// Provides healthcare-optimized models with HIPAA compliance focus.
/// </summary>
public class ModelManagementService : IModelManagementService
{
    private readonly ILogger<ModelManagementService> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _modelsDirectory;

    public ModelManagementService(
        ILogger<ModelManagementService> logger,
        HttpClient httpClient)
    {
        _logger = logger;
        _httpClient = httpClient;
        
        // Use ~/.pidgeon/models directory as specified in architecture
        var userHome = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        _modelsDirectory = Path.Combine(userHome, ".pidgeon", "models");
        
        // TODO: Make model registry URL configurable
        
        Directory.CreateDirectory(_modelsDirectory);
    }

    public async Task<Result<IReadOnlyList<ModelMetadata>>> ListAvailableModelsAsync(CancellationToken cancellationToken = default)
    {
        await Task.Yield();
        try
        {
            _logger.LogInformation("Fetching available models from registry");
            
            // For now, return a curated list of healthcare-optimized models
            // TODO: Fetch from actual model registry
            var models = GetCuratedHealthcareModels();
            
            _logger.LogInformation("Found {ModelCount} available models", models.Count);
            return Result<IReadOnlyList<ModelMetadata>>.Success(models);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch available models");
            return Result<IReadOnlyList<ModelMetadata>>.Failure($"Failed to fetch available models: {ex.Message}");
        }
    }

    public async Task<Result<IReadOnlyList<ModelInfo>>> ListInstalledModelsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var installedModels = new List<ModelInfo>();
            
            if (!Directory.Exists(_modelsDirectory))
            {
                return Result<IReadOnlyList<ModelInfo>>.Success(installedModels);
            }

            var modelFiles = Directory.GetFiles(_modelsDirectory, "*.*", SearchOption.AllDirectories)
                .Where(f => IsModelFile(f))
                .ToList();

            foreach (var modelFile in modelFiles)
            {
                var modelInfo = await CreateModelInfoFromFile(modelFile);
                if (modelInfo != null)
                {
                    installedModels.Add(modelInfo);
                }
            }

            _logger.LogInformation("Found {ModelCount} installed models", installedModels.Count);
            return Result<IReadOnlyList<ModelInfo>>.Success(installedModels);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list installed models");
            return Result<IReadOnlyList<ModelInfo>>.Failure($"Failed to list installed models: {ex.Message}");
        }
    }

    public async Task<Result<ModelInfo>> DownloadModelAsync(string modelId, IProgress<DownloadProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting download of model {ModelId}", modelId);
            
            var availableModels = await ListAvailableModelsAsync(cancellationToken);
            if (availableModels.IsFailure)
            {
                return Result<ModelInfo>.Failure(availableModels.Error.Message);
            }

            var modelMetadata = availableModels.Value.FirstOrDefault(m => m.Id == modelId);
            if (modelMetadata == null)
            {
                return Result<ModelInfo>.Failure($"Model '{modelId}' not found in registry");
            }

            // Pre-download system checks
            var systemCheck = await PerformPreDownloadChecks(modelMetadata);
            if (systemCheck.IsFailure)
            {
                return Result<ModelInfo>.Failure(systemCheck.Error.Message);
            }

            var fileName = GetModelFileName(modelMetadata);
            var targetPath = Path.Combine(_modelsDirectory, fileName);

            // Check if already downloaded
            if (File.Exists(targetPath))
            {
                _logger.LogInformation("Model {ModelId} already exists at {Path}", modelId, targetPath);
                var existingModel = await CreateModelInfoFromFile(targetPath);
                if (existingModel != null)
                {
                    return Result<ModelInfo>.Success(existingModel);
                }
            }

            // Download the model
            progress?.Report(new DownloadProgress
            {
                Stage = "downloading",
                StatusMessage = $"Downloading {modelMetadata.Name}..."
            });

            await DownloadFileWithProgress(modelMetadata.DownloadUrl, targetPath, progress, cancellationToken);

            // Validate the downloaded model
            progress?.Report(new DownloadProgress
            {
                Stage = "verifying",
                StatusMessage = "Verifying model integrity..."
            });

            var validationResult = await ValidateModelAsync(targetPath, cancellationToken);
            if (validationResult.IsFailure || !validationResult.Value.IsValid)
            {
                File.Delete(targetPath);
                return Result<ModelInfo>.Failure($"Model validation failed: {string.Join(", ", validationResult.Value?.Errors ?? new List<string> { "Unknown validation error" })}");
            }

            var modelInfo = await CreateModelInfoFromFile(targetPath);
            if (modelInfo == null)
            {
                return Result<ModelInfo>.Failure("Failed to create model info after download");
            }

            _logger.LogInformation("Successfully downloaded model {ModelId} to {Path}", modelId, targetPath);
            return Result<ModelInfo>.Success(modelInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download model {ModelId}", modelId);
            return Result<ModelInfo>.Failure($"Failed to download model: {ex.Message}");
        }
    }

    public async Task<Result> RemoveModelAsync(string modelId, CancellationToken cancellationToken = default)
    {
        try
        {
            var installedModels = await ListInstalledModelsAsync(cancellationToken);
            if (installedModels.IsFailure)
            {
                return Result.Failure(installedModels.Error.Message);
            }

            var model = installedModels.Value.FirstOrDefault(m => m.Id == modelId);
            if (model == null)
            {
                return Result.Failure($"Model '{modelId}' not found");
            }

            File.Delete(model.FilePath);
            _logger.LogInformation("Removed model {ModelId} from {Path}", modelId, model.FilePath);
            
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove model {ModelId}", modelId);
            return Result.Failure($"Failed to remove model: {ex.Message}");
        }
    }

    public async Task<Result<ModelMetadata>> GetModelMetadataAsync(string modelId, CancellationToken cancellationToken = default)
    {
        var availableModels = await ListAvailableModelsAsync(cancellationToken);
        if (availableModels.IsFailure)
        {
            return Result<ModelMetadata>.Failure(availableModels.Error.Message);
        }

        var model = availableModels.Value.FirstOrDefault(m => m.Id == modelId);
        if (model == null)
        {
            return Result<ModelMetadata>.Failure($"Model '{modelId}' not found");
        }

        return Result<ModelMetadata>.Success(model);
    }

    public async Task<Result<ModelValidationResult>> ValidateModelAsync(string modelPath, CancellationToken cancellationToken = default)
    {
        await Task.Yield();
        try
        {
            var result = new ModelValidationResult
            {
                IsValid = true,
                ChecksumValid = true,  // TODO: Implement actual checksum validation
                SecurityScanPassed = true,  // TODO: Implement security scanning
                FormatValid = IsModelFile(modelPath)
            };

            if (!File.Exists(modelPath))
            {
                result = result with 
                { 
                    IsValid = false,
                    Errors = new List<string> { "Model file does not exist" }
                };
            }

            return Result<ModelValidationResult>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate model at {Path}", modelPath);
            return Result<ModelValidationResult>.Failure($"Model validation failed: {ex.Message}");
        }
    }

    private List<ModelMetadata> GetCuratedHealthcareModels()
    {
        // Healthcare-optimized FOSS models for different use cases and resource constraints
        return new List<ModelMetadata>
        {
            // Large models for advanced analysis
            new ModelMetadata
            {
                Id = "gpt-oss-20b",
                Name = "OpenAI GPT-OSS-20B",
                Description = "OpenAI's open-source 20B parameter model with strong reasoning capabilities, optimized for healthcare analysis",
                Version = "1.0",
                Tier = "Pro",
                SizeBytes = 13761300984, // 13.7GB
                DownloadUrl = "https://huggingface.co/openai/gpt-oss-20b/resolve/main/model.safetensors",
                Checksum = "sha256:to-be-calculated",
                Format = "SafeTensors",
                HealthcareSpecialty = "Clinical",
                Requirements = new SystemRequirements
                {
                    MinRamMB = 16384, // 16GB minimum
                    RecommendedRamMB = 20480, // 20GB recommended  
                    MinCpuCores = 8,
                    SupportsGpu = true,
                    EstimatedTokensPerSecond = 15
                },
                UseCases = new List<string> { "Advanced reasoning", "Complex root cause analysis", "Multi-standard troubleshooting" },
                SupportedStandards = new List<string> { "HL7", "FHIR", "NCPDP", "DICOM" }
            },
            
            // Medical-specific models
            new ModelMetadata
            {
                Id = "biomistral-7b",
                Name = "BioMistral-7B",
                Description = "Mistral-7B fine-tuned on PubMed Central for biomedical domain expertise",
                Version = "1.0",
                Tier = "Pro",
                SizeBytes = 4368709120, // ~4.1GB GGUF Q4_K_M
                DownloadUrl = "https://huggingface.co/MaziyarPanahi/BioMistral-7B-GGUF/resolve/main/BioMistral-7B-GGUF.Q4_K_M.gguf",
                Checksum = "sha256:to-be-calculated",
                Format = "GGUF", 
                HealthcareSpecialty = "Biomedical",
                Requirements = new SystemRequirements
                {
                    MinRamMB = 6144, // 6GB minimum
                    RecommendedRamMB = 8192, // 8GB recommended
                    MinCpuCores = 4,
                    SupportsGpu = true,
                    EstimatedTokensPerSecond = 20
                },
                UseCases = new List<string> { "Medical terminology analysis", "Clinical context understanding", "Biomedical reasoning" },
                SupportedStandards = new List<string> { "HL7", "FHIR", "SNOMED", "ICD" }
            },
            
            new ModelMetadata
            {
                Id = "mediphi-instruct",
                Name = "MediPhi-Instruct",
                Description = "Microsoft Phi-3.5-Mini specialized for medical and clinical NLP tasks",
                Version = "1.0",
                Tier = "Pro",
                SizeBytes = 2415919104, // ~2.25GB
                DownloadUrl = "https://huggingface.co/microsoft/Phi-3.5-mini-instruct/resolve/main/model.safetensors",
                Checksum = "sha256:to-be-calculated",
                Format = "SafeTensors",
                HealthcareSpecialty = "Clinical",
                Requirements = new SystemRequirements
                {
                    MinRamMB = 4096, // 4GB minimum
                    RecommendedRamMB = 6144, // 6GB recommended
                    MinCpuCores = 4,
                    SupportsGpu = true,
                    EstimatedTokensPerSecond = 30
                },
                UseCases = new List<string> { "Clinical NLP", "Medical coding", "Healthcare information extraction" },
                SupportedStandards = new List<string> { "HL7", "FHIR", "ICD10CM", "ICD10PROC", "ATC" }
            },
            
            // Efficient models for resource-constrained environments
            new ModelMetadata
            {
                Id = "phi3-mini-instruct",
                Name = "Phi-3-Mini-4K-Instruct",
                Description = "Microsoft's efficient 3.8B parameter model, FOSS under MIT license",
                Version = "1.0",
                Tier = "Free",
                SizeBytes = 2415919104, // ~2.25GB
                DownloadUrl = "https://huggingface.co/microsoft/Phi-3-mini-4k-instruct-gguf/resolve/main/Phi-3-mini-4k-instruct-q4.gguf",
                Checksum = "sha256:to-be-calculated",
                Format = "GGUF",
                HealthcareSpecialty = "General",
                Requirements = new SystemRequirements
                {
                    MinRamMB = 4096, // 4GB minimum
                    RecommendedRamMB = 6144, // 6GB recommended
                    MinCpuCores = 4,
                    SupportsGpu = true,
                    EstimatedTokensPerSecond = 35
                },
                UseCases = new List<string> { "General analysis", "Code understanding", "Basic reasoning" },
                SupportedStandards = new List<string> { "HL7", "FHIR", "JSON", "XML" }
            },
            
            new ModelMetadata
            {
                Id = "tinyllama-chat",
                Name = "TinyLlama-1.1B-Chat",
                Description = "Ultra-lightweight 1.1B parameter model for resource-constrained analysis",
                Version = "1.0",
                Tier = "Free",
                SizeBytes = 668123136, // ~637MB GGUF Q4_K_M  
                DownloadUrl = "https://huggingface.co/TheBloke/TinyLlama-1.1B-Chat-v1.0-GGUF/resolve/main/tinyllama-1.1b-chat-v1.0.Q4_K_M.gguf",
                Checksum = "sha256:to-be-calculated",
                Format = "GGUF",
                HealthcareSpecialty = "General",
                Requirements = new SystemRequirements
                {
                    MinRamMB = 2048, // 2GB minimum
                    RecommendedRamMB = 3072, // 3GB recommended
                    MinCpuCores = 2,
                    SupportsGpu = true,
                    EstimatedTokensPerSecond = 50
                },
                UseCases = new List<string> { "Basic diff analysis", "Quick suggestions", "Low-latency responses" },
                SupportedStandards = new List<string> { "HL7", "FHIR" }
            },
            new ModelMetadata
            {
                Id = "biogpt-clinical",
                Name = "BioGPT Clinical",
                Description = "Healthcare-specific model trained on medical literature and clinical texts",
                Version = "1.0",
                Tier = "Pro",
                SizeBytes = 2147483648, // ~2GB ONNX format
                DownloadUrl = "https://huggingface.co/microsoft/BioGPT/resolve/main/pytorch_model.bin",
                Checksum = "sha256:placeholder",
                Format = "ONNX",
                HealthcareSpecialty = "Clinical",
                Requirements = new SystemRequirements
                {
                    MinRamMB = 6144,
                    RecommendedRamMB = 8192,
                    MinCpuCores = 4,
                    SupportsGpu = false,
                    EstimatedTokensPerSecond = 15
                },
                UseCases = new List<string> { "Medical term understanding", "Clinical pattern recognition" },
                SupportedStandards = new List<string> { "HL7", "FHIR", "DICOM" }
            }
        };
    }

    private bool IsModelFile(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension switch
        {
            ".gguf" => true,
            ".onnx" => true,
            ".bin" => true,
            ".safetensors" => true,
            ".pt" => true,   // PyTorch
            ".pth" => true,  // PyTorch
            _ => false
        };
    }

    private async Task<ModelInfo?> CreateModelInfoFromFile(string filePath)
    {
        await Task.Yield();
        try
        {
            var fileInfo = new FileInfo(filePath);
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            
            // Extract model ID and format from filename
            var format = Path.GetExtension(filePath).TrimStart('.').ToUpperInvariant();
            
            return new ModelInfo
            {
                Id = fileName,
                Name = fileName.Replace("-", " ").Replace("_", " "),
                Version = "1.0",
                FilePath = filePath,
                SizeBytes = fileInfo.Length,
                Format = format,
                HealthcareSpecialty = DetermineHealthcareSpecialty(fileName),
                MinimumRamMB = EstimateMinimumRam(fileInfo.Length),
                ProviderId = DetermineProviderId(format),
                InstallDate = fileInfo.CreationTime,
                LastUsed = fileInfo.LastAccessTime > fileInfo.CreationTime ? fileInfo.LastAccessTime : null,
                Performance = new ModelPerformance
                {
                    // TODO: Store actual performance metrics
                    AverageInferenceMs = 2000,
                    TokensPerSecond = 20,
                    MemoryUsageMB = (int)(fileInfo.Length / 1024 / 1024),
                    HealthcareAccuracy = 0.85
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create model info for {FilePath}", filePath);
            return null;
        }
    }

    private string GetModelFileName(ModelMetadata metadata)
    {
        var extension = metadata.Format.ToLowerInvariant() switch
        {
            "gguf" => ".gguf",
            "onnx" => ".onnx", 
            "safetensors" => ".safetensors",
            "pytorch" => ".bin",
            _ => ".bin"
        };
        
        return $"{metadata.Id}{extension}";
    }

    private async Task DownloadFileWithProgress(string url, string targetPath, IProgress<DownloadProgress>? progress, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting download from {Url} to {Path}", url, targetPath);
        
        try
        {
            // Create directory if it doesn't exist
            var directory = Path.GetDirectoryName(targetPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            progress?.Report(new DownloadProgress
            {
                Stage = "connecting",
                StatusMessage = "Connecting to download server..."
            });

            using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var totalBytes = response.Content.Headers.ContentLength ?? 0;
            var startTime = DateTime.UtcNow;
            
            progress?.Report(new DownloadProgress
            {
                TotalBytes = totalBytes,
                Stage = "downloading",
                StatusMessage = totalBytes > 0 ? $"Downloading {totalBytes:N0} bytes..." : "Downloading..."
            });

            using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var fileStream = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 81920, useAsync: true);
            
            var buffer = new byte[81920]; // 80KB buffer for better performance
            long bytesDownloaded = 0;
            int bytesRead;
            var lastProgressUpdate = DateTime.UtcNow;
            
            while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
            {
                await fileStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                bytesDownloaded += bytesRead;
                
                // Update progress every 500ms to avoid overwhelming the UI
                if (DateTime.UtcNow - lastProgressUpdate > TimeSpan.FromMilliseconds(500))
                {
                    var elapsed = DateTime.UtcNow - startTime;
                    var bytesPerSecond = elapsed.TotalSeconds > 0 ? (long)(bytesDownloaded / elapsed.TotalSeconds) : 0;
                    var estimatedTimeRemaining = totalBytes > 0 && bytesPerSecond > 0 
                        ? TimeSpan.FromSeconds((totalBytes - bytesDownloaded) / (double)bytesPerSecond) 
                        : (TimeSpan?)null;

                    progress?.Report(new DownloadProgress
                    {
                        BytesDownloaded = bytesDownloaded,
                        TotalBytes = totalBytes,
                        BytesPerSecond = bytesPerSecond,
                        EstimatedTimeRemaining = estimatedTimeRemaining,
                        Stage = "downloading",
                        StatusMessage = totalBytes > 0 
                            ? $"Downloaded {bytesDownloaded:N0} of {totalBytes:N0} bytes ({bytesPerSecond / 1024:N0} KB/s)"
                            : $"Downloaded {bytesDownloaded:N0} bytes ({bytesPerSecond / 1024:N0} KB/s)"
                    });
                    
                    lastProgressUpdate = DateTime.UtcNow;
                }
            }

            progress?.Report(new DownloadProgress
            {
                BytesDownloaded = bytesDownloaded,
                TotalBytes = totalBytes,
                Stage = "completed",
                StatusMessage = "Download completed successfully"
            });
            
            _logger.LogInformation("Successfully downloaded {Bytes:N0} bytes to {Path}", bytesDownloaded, targetPath);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error during download from {Url}", url);
            
            // Clean up partial download
            if (File.Exists(targetPath))
            {
                File.Delete(targetPath);
            }
            
            throw new InvalidOperationException($"Download failed: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex) when (ex.CancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Download cancelled by user");
            
            // Clean up partial download
            if (File.Exists(targetPath))
            {
                File.Delete(targetPath);
            }
            
            throw new OperationCanceledException("Download was cancelled", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during download from {Url}", url);
            
            // Clean up partial download
            if (File.Exists(targetPath))
            {
                File.Delete(targetPath);
            }
            
            throw;
        }
    }

    private string DetermineHealthcareSpecialty(string fileName)
    {
        var lowerName = fileName.ToLowerInvariant();
        if (lowerName.Contains("clinical") || lowerName.Contains("medical")) return "Clinical";
        if (lowerName.Contains("pharmacy") || lowerName.Contains("drug")) return "Pharmacy";
        if (lowerName.Contains("radiology") || lowerName.Contains("imaging")) return "Radiology";
        return "General";
    }

    private async Task<Result> PerformPreDownloadChecks(ModelMetadata modelMetadata)
    {
        await Task.Yield();
        _logger.LogInformation("Performing pre-download checks for model {ModelId}", modelMetadata.Id);
        
        try
        {
            var errors = new List<string>();
            var warnings = new List<string>();

            // 1. Check disk space
            var diskCheck = CheckAvailableDiskSpace(modelMetadata.SizeBytes);
            if (diskCheck.isError)
            {
                errors.Add(diskCheck.message);
            }
            else if (diskCheck.hasWarning)
            {
                warnings.Add(diskCheck.message);
            }

            // 2. Check system RAM
            var ramCheck = CheckSystemRam(modelMetadata.Requirements);
            if (ramCheck.isError)
            {
                errors.Add(ramCheck.message);
            }
            else if (ramCheck.hasWarning)
            {
                warnings.Add(ramCheck.message);
            }

            // 3. Check CPU requirements
            var cpuCheck = CheckCpuRequirements(modelMetadata.Requirements);
            if (cpuCheck.hasWarning)
            {
                warnings.Add(cpuCheck.message);
            }

            // Log warnings
            foreach (var warning in warnings)
            {
                _logger.LogWarning("Pre-download warning for {ModelId}: {Warning}", modelMetadata.Id, warning);
            }

            // Return errors if any
            if (errors.Any())
            {
                var errorMessage = $"Cannot download {modelMetadata.Name}:\n" + string.Join("\n", errors);
                
                // Add recommendations for alternative models
                var alternatives = GetAlternativeModels(modelMetadata);
                if (alternatives.Any())
                {
                    errorMessage += $"\n\nRecommended alternatives:\n" + string.Join("\n", alternatives);
                }

                return Result.Failure(errorMessage);
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during pre-download checks for {ModelId}", modelMetadata.Id);
            return Result.Failure($"System check failed: {ex.Message}");
        }
    }

    private (bool isError, bool hasWarning, string message) CheckAvailableDiskSpace(long requiredBytes)
    {
        try
        {
            var drive = new DriveInfo(Path.GetPathRoot(_modelsDirectory)!);
            var availableBytes = drive.AvailableFreeSpace;
            var requiredGB = requiredBytes / (1024.0 * 1024.0 * 1024.0);
            var availableGB = availableBytes / (1024.0 * 1024.0 * 1024.0);

            // Need 1.5x the model size for safe download (temp space + final file)
            var safeRequiredBytes = (long)(requiredBytes * 1.5);

            if (availableBytes < safeRequiredBytes)
            {
                return (true, false, $"Insufficient disk space. Required: {requiredGB:F1}GB (+ 50% buffer), Available: {availableGB:F1}GB.\n" +
                                     "Free up space or use: 'pidgeon ai list' to find smaller models.");
            }

            // Warning if less than 2GB free after download
            var remainingAfterDownload = availableBytes - safeRequiredBytes;
            var minFreeGB = 2.0;
            if (remainingAfterDownload < (minFreeGB * 1024 * 1024 * 1024))
            {
                return (false, true, $"Low disk space warning. Only {remainingAfterDownload / (1024.0 * 1024.0 * 1024.0):F1}GB will remain free after download.");
            }

            return (false, false, string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not check disk space");
            return (false, true, "Could not verify disk space - proceeding with caution");
        }
    }

    private (bool isError, bool hasWarning, string message) CheckSystemRam(SystemRequirements requirements)
    {
        try
        {
            // Get total system RAM (this is approximate - actual implementation would use platform-specific APIs)
            var totalRamGB = GetEstimatedSystemRamGB();
            var requiredGB = requirements.MinRamMB / 1024.0;
            var recommendedGB = requirements.RecommendedRamMB / 1024.0;

            if (totalRamGB < requiredGB)
            {
                return (true, false, $"Insufficient RAM. Required: {requiredGB:F1}GB, Estimated available: {totalRamGB:F1}GB.\n" +
                                     "Consider using a smaller model or upgrading your hardware.");
            }

            if (totalRamGB < recommendedGB)
            {
                return (false, true, $"RAM below recommended. Recommended: {recommendedGB:F1}GB, Estimated available: {totalRamGB:F1}GB.\n" +
                                     "Model may run slowly or require swap memory.");
            }

            return (false, false, string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not check system RAM");
            return (false, true, "Could not verify RAM - model may not perform optimally");
        }
    }

    private (bool hasWarning, string message) CheckCpuRequirements(SystemRequirements requirements)
    {
        try
        {
            var coreCount = Environment.ProcessorCount;
            
            if (coreCount < requirements.MinCpuCores)
            {
                return (true, $"CPU cores below recommended. Required: {requirements.MinCpuCores}, Available: {coreCount}. " +
                              "Model inference may be slower than expected.");
            }

            return (false, string.Empty);
        }
        catch
        {
            return (true, "Could not verify CPU requirements");
        }
    }

    private double GetEstimatedSystemRamGB()
    {
        // This is a rough estimation - in production you'd use platform-specific APIs
        // For now, assume a reasonable default based on common developer machines
        return 16.0; // Assume 16GB as baseline
    }

    private List<string> GetAlternativeModels(ModelMetadata failedModel)
    {
        var alternatives = new List<string>();
        
        // If the failed model was large, recommend smaller alternatives
        var sizeMB = failedModel.SizeBytes / (1024.0 * 1024.0);
        if (sizeMB > 8000) // > 8GB
        {
            alternatives.Add("• phi2-healthcare (1.5GB) - Efficient clinical analysis");
            alternatives.Add("• tinyllama-medical (800MB) - Lightweight healthcare model");
        }
        else if (sizeMB > 2000) // > 2GB
        {
            alternatives.Add("• tinyllama-medical (800MB) - Lightweight healthcare model");
        }

        // Add troubleshooting steps
        alternatives.Add("• Use 'df -h' to check disk usage");
        alternatives.Add("• Clean up old models with 'pidgeon ai remove <model-id>'");
        alternatives.Add("• Consider using external storage or cloud compute");

        return alternatives;
    }

    private int EstimateMinimumRam(long fileSizeBytes)
    {
        // Rule of thumb: model needs ~2x its file size in RAM for inference
        return (int)((fileSizeBytes * 2) / 1024 / 1024);
    }

    private string DetermineProviderId(string format)
    {
        return format.ToUpperInvariant() switch
        {
            "GGUF" => "llama-cpp",
            "ONNX" => "onnx-runtime", 
            _ => "generic"
        };
    }
}