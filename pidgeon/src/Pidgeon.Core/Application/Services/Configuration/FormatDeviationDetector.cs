// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Microsoft.Extensions.Logging;
using Pidgeon.Core.Infrastructure.Standards.Abstractions;
using Pidgeon.Core.Domain.Configuration.Entities;

namespace Pidgeon.Core.Domain.Configuration.Services;

/// <summary>
/// Standard-agnostic orchestrator for detecting format deviations in healthcare messages.
/// Delegates standard-specific logic to appropriate plugins while providing 
/// unified interface for format deviation detection across all healthcare standards.
/// Single responsibility: "Orchestrate format deviation detection using standard plugins."
/// </summary>
internal class FormatDeviationDetector : IFormatDeviationDetector
{
    private readonly IStandardPluginRegistry _pluginRegistry;
    private readonly ILogger<FormatDeviationDetector> _logger;

    public FormatDeviationDetector(
        IStandardPluginRegistry pluginRegistry,
        ILogger<FormatDeviationDetector> logger)
    {
        _pluginRegistry = pluginRegistry ?? throw new ArgumentNullException(nameof(pluginRegistry));
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<FormatDeviation>>> DetectEncodingDeviationsAsync(
        IEnumerable<string> messages, 
        string standard)
    {
        try
        {
            var messageList = messages.ToList();
            if (messageList.Count == 0)
                return Result<IReadOnlyList<FormatDeviation>>.Failure("No messages provided for encoding analysis");

            _logger.LogDebug("Detecting encoding deviations in {MessageCount} {Standard} messages", 
                messageList.Count, standard);

            var plugin = _pluginRegistry.GetFormatAnalysisPlugin(standard);
            if (plugin == null)
            {
                _logger.LogWarning("No format analysis plugin found for standard: {Standard}", standard);
                return Result<IReadOnlyList<FormatDeviation>>.Failure($"No format analysis plugin available for standard: {standard}");
            }

            var result = await plugin.DetectEncodingDeviationsAsync(messageList);
            
            if (result.IsSuccess)
            {
                _logger.LogInformation("Detected {DeviationCount} encoding deviations in {MessageCount} {Standard} messages",
                    result.Value.Count, messageList.Count, standard);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting encoding deviations for {Standard}", standard);
            return Result<IReadOnlyList<FormatDeviation>>.Failure($"Encoding deviation detection failed: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<FormatDeviation>>> DetectStructuralDeviationsAsync(
        IEnumerable<string> messages,
        string standard,
        string messageType)
    {
        try
        {
            var messageList = messages.ToList();
            if (messageList.Count == 0)
                return Result<IReadOnlyList<FormatDeviation>>.Failure("No messages provided for structural analysis");

            _logger.LogDebug("Detecting structural deviations in {MessageCount} {Standard} {MessageType} messages",
                messageList.Count, standard, messageType);

            var plugin = _pluginRegistry.GetFormatAnalysisPlugin(standard);
            if (plugin == null)
            {
                _logger.LogWarning("No format analysis plugin found for standard: {Standard}", standard);
                return Result<IReadOnlyList<FormatDeviation>>.Failure($"No format analysis plugin available for standard: {standard}");
            }

            var result = await plugin.DetectStructuralDeviationsAsync(messageList, messageType);
            
            if (result.IsSuccess)
            {
                _logger.LogInformation("Detected {DeviationCount} structural deviations in {MessageCount} {Standard} messages",
                    result.Value.Count, messageList.Count, standard);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting structural deviations for {Standard}", standard);
            return Result<IReadOnlyList<FormatDeviation>>.Failure($"Structural deviation detection failed: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<FormatDeviation>>> DetectFieldFormatDeviationsAsync(
        IEnumerable<string> messages,
        string standard,
        string segmentType)
    {
        try
        {
            var messageList = messages.ToList();
            if (messageList.Count == 0)
                return Result<IReadOnlyList<FormatDeviation>>.Failure("No messages provided for field format analysis");

            _logger.LogDebug("Detecting field format deviations in {MessageCount} {Standard} {SegmentType} segments",
                messageList.Count, standard, segmentType);

            var plugin = _pluginRegistry.GetFormatAnalysisPlugin(standard);
            if (plugin == null)
            {
                _logger.LogWarning("No format analysis plugin found for standard: {Standard}", standard);
                return Result<IReadOnlyList<FormatDeviation>>.Failure($"No format analysis plugin available for standard: {standard}");
            }

            var result = await plugin.DetectFieldFormatDeviationsAsync(messageList, segmentType);
            
            if (result.IsSuccess)
            {
                _logger.LogInformation("Detected {DeviationCount} field format deviations in {MessageCount} {Standard} messages",
                    result.Value.Count, messageList.Count, standard);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting field format deviations for {Standard}", standard);
            return Result<IReadOnlyList<FormatDeviation>>.Failure($"Field format deviation detection failed: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<FormatDeviation>>> DetectAllDeviationsAsync(
        IEnumerable<string> messages,
        string standard,
        string? messageType = null)
    {
        try
        {
            var messageList = messages.ToList();
            if (messageList.Count == 0)
                return Result<IReadOnlyList<FormatDeviation>>.Failure("No messages provided for comprehensive analysis");

            _logger.LogInformation("Starting comprehensive deviation detection for {MessageCount} {Standard} messages",
                messageList.Count, standard);

            var plugin = _pluginRegistry.GetFormatAnalysisPlugin(standard);
            if (plugin == null)
            {
                _logger.LogWarning("No format analysis plugin found for standard: {Standard}", standard);
                return Result<IReadOnlyList<FormatDeviation>>.Failure($"No format analysis plugin available for standard: {standard}");
            }

            var result = await plugin.DetectAllDeviationsAsync(messageList, messageType);
            
            if (result.IsSuccess)
            {
                _logger.LogInformation("Comprehensive deviation detection completed: {TotalDeviations} total deviations found for {Standard}",
                    result.Value.Count, standard);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during comprehensive deviation detection for {Standard}", standard);
            return Result<IReadOnlyList<FormatDeviation>>.Failure($"Comprehensive deviation detection failed: {ex.Message}");
        }
    }
}