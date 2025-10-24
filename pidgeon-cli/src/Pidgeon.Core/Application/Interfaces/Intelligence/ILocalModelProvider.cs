// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Pidgeon.Core.Domain.Intelligence;

namespace Pidgeon.Core.Application.Interfaces.Intelligence;

/// <summary>
/// Interface for local AI model providers that run models on-premises.
/// Ensures HIPAA compliance by keeping all PHI processing local.
/// </summary>
public interface ILocalModelProvider : IAIAnalysisProvider
{
    /// <summary>
    /// Load a specific model from the given path
    /// </summary>
    Task<Result> LoadModelAsync(string modelPath, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get information about the currently loaded model
    /// </summary>
    Task<Result<ModelInfo>> GetModelInfoAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get the size in bytes of the currently loaded model
    /// </summary>
    long GetModelSizeBytes();
    
    /// <summary>
    /// Unload the current model to free memory
    /// </summary>
    Task<Result> UnloadModelAsync(CancellationToken cancellationToken = default);
}