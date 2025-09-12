// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Pidgeon.Core.Domain.Intelligence;

namespace Pidgeon.Core.Application.Interfaces.Intelligence;

/// <summary>
/// Service for managing AI model downloads, installation, and configuration.
/// Supports healthcare-optimized models with size and performance considerations.
/// </summary>
public interface IModelManagementService
{
    /// <summary>
    /// List all available models for download from the model registry
    /// </summary>
    Task<Result<IReadOnlyList<ModelMetadata>>> ListAvailableModelsAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// List all locally installed models
    /// </summary>
    Task<Result<IReadOnlyList<ModelInfo>>> ListInstalledModelsAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Download and install a model by ID (e.g., "phi2-healthcare", "tinyllama-medical")
    /// </summary>
    Task<Result<ModelInfo>> DownloadModelAsync(string modelId, IProgress<DownloadProgress>? progress = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Remove a locally installed model
    /// </summary>
    Task<Result> RemoveModelAsync(string modelId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get detailed information about a specific model
    /// </summary>
    Task<Result<ModelMetadata>> GetModelMetadataAsync(string modelId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Validate model integrity and security
    /// </summary>
    Task<Result<ModelValidationResult>> ValidateModelAsync(string modelPath, CancellationToken cancellationToken = default);
}