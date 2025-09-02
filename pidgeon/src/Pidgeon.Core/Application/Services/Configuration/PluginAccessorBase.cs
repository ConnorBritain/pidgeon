// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Microsoft.Extensions.Logging;
using Pidgeon.Core.Application.Interfaces.Standards;

namespace Pidgeon.Core.Application.Services.Configuration;

/// <summary>
/// Base class providing common plugin access patterns for all Configuration services.
/// Consolidates duplicate plugin retrieval, null checking, and error handling logic.
/// </summary>
/// <typeparam name="TService">The derived service type for logging context</typeparam>
/// <typeparam name="TPlugin">The specific plugin type to retrieve</typeparam>
internal abstract class PluginAccessorBase<TService, TPlugin> 
    where TService : class 
    where TPlugin : class
{
    protected readonly IStandardPluginRegistry _pluginRegistry;
    protected readonly ILogger<TService> _logger;

    protected PluginAccessorBase(
        IStandardPluginRegistry pluginRegistry,
        ILogger<TService> logger)
    {
        _pluginRegistry = pluginRegistry ?? throw new ArgumentNullException(nameof(pluginRegistry));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Template method for executing plugin operations with consistent error handling.
    /// </summary>
    /// <typeparam name="TResult">The result type returned by the plugin operation</typeparam>
    /// <param name="standard">The healthcare standard identifier</param>
    /// <param name="pluginSelector">Function to select the appropriate plugin from registry</param>
    /// <param name="pluginOperation">Async operation to perform with the selected plugin</param>
    /// <param name="operationName">Name of the operation for logging and error messages</param>
    /// <returns>Result containing the plugin operation outcome</returns>
    protected async Task<Result<TResult>> ExecutePluginOperationAsync<TResult>(
        string standard,
        Func<IStandardPluginRegistry, TPlugin?> pluginSelector,
        Func<TPlugin, Task<Result<TResult>>> pluginOperation,
        string operationName)
    {
        try
        {
            var plugin = pluginSelector(_pluginRegistry);
            if (plugin == null)
            {
                _logger.LogWarning("No {PluginType} plugin found for standard: {Standard}", 
                    typeof(TPlugin).Name, standard);
                return Result<TResult>.Failure($"No {typeof(TPlugin).Name.ToLowerInvariant()} plugin available for standard: {standard}");
            }

            return await pluginOperation(plugin);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during {OperationName} for {Standard}", operationName, standard);
            return Result<TResult>.Failure($"{operationName} failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Synchronous version for plugin operations that don't require async processing.
    /// </summary>
    /// <typeparam name="TResult">The result type returned by the plugin operation</typeparam>
    /// <param name="standard">The healthcare standard identifier</param>
    /// <param name="pluginSelector">Function to select the appropriate plugin from registry</param>
    /// <param name="pluginOperation">Operation to perform with the selected plugin</param>
    /// <param name="operationName">Name of the operation for logging and error messages</param>
    /// <returns>Result containing the plugin operation outcome</returns>
    protected Result<TResult> ExecutePluginOperation<TResult>(
        string standard,
        Func<IStandardPluginRegistry, TPlugin?> pluginSelector,
        Func<TPlugin, Result<TResult>> pluginOperation,
        string operationName)
    {
        try
        {
            var plugin = pluginSelector(_pluginRegistry);
            if (plugin == null)
            {
                _logger.LogWarning("No {PluginType} plugin found for standard: {Standard}", 
                    typeof(TPlugin).Name, standard);
                return Result<TResult>.Failure($"No {typeof(TPlugin).Name.ToLowerInvariant()} plugin available for standard: {standard}");
            }

            return pluginOperation(plugin);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during {OperationName} for {Standard}", operationName, standard);
            return Result<TResult>.Failure($"{operationName} failed: {ex.Message}");
        }
    }
}