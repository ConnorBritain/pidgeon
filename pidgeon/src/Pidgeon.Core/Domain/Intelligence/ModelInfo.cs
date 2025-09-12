// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

namespace Pidgeon.Core.Domain.Intelligence;

/// <summary>
/// Information about a locally installed AI model.
/// </summary>
public record ModelInfo
{
    /// <summary>
    /// Unique model identifier (e.g., "phi2-healthcare", "tinyllama-medical")
    /// </summary>
    public string Id { get; init; } = string.Empty;
    
    /// <summary>
    /// Human-readable name of the model
    /// </summary>
    public string Name { get; init; } = string.Empty;
    
    /// <summary>
    /// Model version
    /// </summary>
    public string Version { get; init; } = string.Empty;
    
    /// <summary>
    /// Local file path to the model
    /// </summary>
    public string FilePath { get; init; } = string.Empty;
    
    /// <summary>
    /// Size of the model file in bytes
    /// </summary>
    public long SizeBytes { get; init; }
    
    /// <summary>
    /// Model format (e.g., "GGUF", "ONNX", "PyTorch")
    /// </summary>
    public string Format { get; init; } = string.Empty;
    
    /// <summary>
    /// Healthcare specialization (e.g., "General", "Clinical", "Pharmacy")
    /// </summary>
    public string HealthcareSpecialty { get; init; } = "General";
    
    /// <summary>
    /// Recommended minimum RAM in MB
    /// </summary>
    public int MinimumRamMB { get; init; }
    
    /// <summary>
    /// Provider that can load this model
    /// </summary>
    public string ProviderId { get; init; } = string.Empty;
    
    /// <summary>
    /// When the model was installed
    /// </summary>
    public DateTime InstallDate { get; init; }
    
    /// <summary>
    /// Last time the model was used
    /// </summary>
    public DateTime? LastUsed { get; init; }
    
    /// <summary>
    /// Model performance characteristics
    /// </summary>
    public ModelPerformance Performance { get; init; } = new();
}