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
    private readonly string _modelRegistryUrl;

    public ModelManagementService(
        ILogger<ModelManagementService> logger,
        HttpClient httpClient)
    {
        _logger = logger;
        _httpClient = httpClient;
        
        // Use ~/.pidgeon/models directory as specified in architecture
        var userHome = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        _modelsDirectory = Path.Combine(userHome, ".pidgeon", "models");
        
        // TODO: Make this configurable
        _modelRegistryUrl = "https://models.pidgeon.health/registry.json";
        
        Directory.CreateDirectory(_modelsDirectory);
    }

    public async Task<Result<IReadOnlyList<ModelMetadata>>> ListAvailableModelsAsync(CancellationToken cancellationToken = default)
    {
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
        // Healthcare-optimized models as specified in the architecture document
        return new List<ModelMetadata>
        {
            new ModelMetadata
            {
                Id = "phi2-healthcare",
                Name = "Phi-2 Healthcare",
                Description = "Microsoft's efficient 2.7B parameter model fine-tuned for healthcare terminology and patterns",
                Version = "1.0",
                Tier = "Pro",
                SizeBytes = 1610612736, // ~1.5GB quantized
                DownloadUrl = "https://models.pidgeon.health/phi2-healthcare-q4.gguf",
                Checksum = "sha256:placeholder",
                Format = "GGUF",
                HealthcareSpecialty = "Clinical",
                Requirements = new SystemRequirements
                {
                    MinRamMB = 4096,
                    RecommendedRamMB = 8192,
                    MinCpuCores = 4,
                    SupportsGpu = true,
                    EstimatedTokensPerSecond = 25
                },
                UseCases = new List<string> { "Field analysis", "Pattern matching", "Message validation", "Root cause analysis" },
                SupportedStandards = new List<string> { "HL7", "FHIR", "NCPDP" }
            },
            new ModelMetadata
            {
                Id = "tinyllama-medical",
                Name = "TinyLlama Medical",
                Description = "Ultra-lightweight 1.1B model optimized for basic healthcare diff analysis",
                Version = "1.0",
                Tier = "Pro",
                SizeBytes = 838860800, // ~800MB quantized
                DownloadUrl = "https://models.pidgeon.health/tinyllama-medical-q4.gguf",
                Checksum = "sha256:placeholder",
                Format = "GGUF",
                HealthcareSpecialty = "General",
                Requirements = new SystemRequirements
                {
                    MinRamMB = 2048,
                    RecommendedRamMB = 4096,
                    MinCpuCores = 2,
                    SupportsGpu = true,
                    EstimatedTokensPerSecond = 45
                },
                UseCases = new List<string> { "Basic diff analysis", "Suggestions", "Real-time analysis" },
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
                DownloadUrl = "https://models.pidgeon.health/biogpt-clinical.onnx",
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
            _ => false
        };
    }

    private async Task<ModelInfo?> CreateModelInfoFromFile(string filePath)
    {
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
            _ => ".bin"
        };
        
        return $"{metadata.Id}{extension}";
    }

    private async Task DownloadFileWithProgress(string url, string targetPath, IProgress<DownloadProgress>? progress, CancellationToken cancellationToken)
    {
        // TODO: Implement proper download with progress reporting
        // For now, simulate download for testing
        if (url.StartsWith("https://models.pidgeon.health/"))
        {
            // Create a placeholder file for testing
            var placeholderContent = $"# Placeholder model file for {Path.GetFileName(targetPath)}\n# This would be the actual model file in production\n";
            await File.WriteAllTextAsync(targetPath, placeholderContent, cancellationToken);
            
            progress?.Report(new DownloadProgress
            {
                BytesDownloaded = placeholderContent.Length,
                TotalBytes = placeholderContent.Length,
                Stage = "completed",
                StatusMessage = "Download completed"
            });
            
            return;
        }
        
        // Real download implementation would go here
        using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        
        var totalBytes = response.Content.Headers.ContentLength ?? 0;
        using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var fileStream = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 8192, useAsync: true);
        
        var buffer = new byte[8192];
        long bytesDownloaded = 0;
        int bytesRead;
        
        while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
        {
            await fileStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
            bytesDownloaded += bytesRead;
            
            progress?.Report(new DownloadProgress
            {
                BytesDownloaded = bytesDownloaded,
                TotalBytes = totalBytes,
                Stage = "downloading",
                StatusMessage = $"Downloaded {bytesDownloaded:N0} of {totalBytes:N0} bytes"
            });
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