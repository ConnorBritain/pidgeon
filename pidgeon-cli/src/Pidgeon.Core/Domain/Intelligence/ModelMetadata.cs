// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

namespace Pidgeon.Core.Domain.Intelligence;

/// <summary>
/// Metadata about an AI model available for download or currently installed.
/// </summary>
public record ModelMetadata
{
    /// <summary>
    /// Unique model identifier
    /// </summary>
    public string Id { get; init; } = string.Empty;
    
    /// <summary>
    /// Human-readable name
    /// </summary>
    public string Name { get; init; } = string.Empty;
    
    /// <summary>
    /// Model description and use cases
    /// </summary>
    public string Description { get; init; } = string.Empty;
    
    /// <summary>
    /// Model version
    /// </summary>
    public string Version { get; init; } = string.Empty;
    
    /// <summary>
    /// Healthcare tier (Free, Pro, Enterprise)
    /// </summary>
    public string Tier { get; init; } = "Free";
    
    /// <summary>
    /// Model size in bytes
    /// </summary>
    public long SizeBytes { get; init; }
    
    /// <summary>
    /// Download URL for the model
    /// </summary>
    public string DownloadUrl { get; init; } = string.Empty;
    
    /// <summary>
    /// Checksum for integrity verification
    /// </summary>
    public string Checksum { get; init; } = string.Empty;
    
    /// <summary>
    /// Model format (GGUF, ONNX, etc.)
    /// </summary>
    public string Format { get; init; } = string.Empty;
    
    /// <summary>
    /// Healthcare specialization
    /// </summary>
    public string HealthcareSpecialty { get; init; } = "General";
    
    /// <summary>
    /// Recommended system requirements
    /// </summary>
    public SystemRequirements Requirements { get; init; } = new();
    
    /// <summary>
    /// Healthcare use cases this model is optimized for
    /// </summary>
    public List<string> UseCases { get; init; } = new();
    
    /// <summary>
    /// Supported healthcare standards
    /// </summary>
    public List<string> SupportedStandards { get; init; } = new();
}

/// <summary>
/// System requirements for running a model.
/// </summary>
public record SystemRequirements
{
    /// <summary>
    /// Minimum RAM required in MB
    /// </summary>
    public int MinRamMB { get; init; }
    
    /// <summary>
    /// Recommended RAM in MB
    /// </summary>
    public int RecommendedRamMB { get; init; }
    
    /// <summary>
    /// Minimum CPU cores
    /// </summary>
    public int MinCpuCores { get; init; } = 2;
    
    /// <summary>
    /// Whether GPU acceleration is supported
    /// </summary>
    public bool SupportsGpu { get; init; }
    
    /// <summary>
    /// Estimated tokens per second on recommended hardware
    /// </summary>
    public int EstimatedTokensPerSecond { get; init; }
}

/// <summary>
/// Model performance characteristics.
/// </summary>
public record ModelPerformance
{
    /// <summary>
    /// Average inference time in milliseconds
    /// </summary>
    public int AverageInferenceMs { get; init; }
    
    /// <summary>
    /// Tokens processed per second
    /// </summary>
    public int TokensPerSecond { get; init; }
    
    /// <summary>
    /// Memory usage in MB during inference
    /// </summary>
    public int MemoryUsageMB { get; init; }
    
    /// <summary>
    /// Model accuracy on healthcare tasks (0.0-1.0)
    /// </summary>
    public double HealthcareAccuracy { get; init; }
}