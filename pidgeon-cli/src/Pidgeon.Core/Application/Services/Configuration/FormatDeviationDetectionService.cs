// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Microsoft.Extensions.Logging;
using Pidgeon.Core.Application.Interfaces.Standards;
using Pidgeon.Core.Application.Interfaces.Configuration;
using Pidgeon.Core.Domain.Configuration.Entities;

namespace Pidgeon.Core.Application.Services.Configuration;

/// <summary>
/// Standard-agnostic orchestrator for detecting format deviations in healthcare messages.
/// Delegates standard-specific logic to appropriate plugins while providing 
/// unified interface for format deviation detection across all healthcare standards.
/// Single responsibility: "Orchestrate format deviation detection using standard plugins."
/// </summary>
internal class FormatDeviationDetectionService : PluginAccessorBase<FormatDeviationDetectionService, IStandardFormatAnalysisPlugin>, IFormatDeviationDetectionService
{
    public FormatDeviationDetectionService(
        IStandardPluginRegistry pluginRegistry,
        ILogger<FormatDeviationDetectionService> logger)
        : base(pluginRegistry, logger)
    {
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<FormatDeviation>>> DetectEncodingDeviationsAsync(
        IEnumerable<string> messages, 
        string standard)
    {
        var messageList = messages.ToList();
        if (messageList.Count == 0)
            return Result<IReadOnlyList<FormatDeviation>>.Failure("No messages provided for encoding analysis");

        _logger.LogDebug("Detecting encoding deviations in {MessageCount} {Standard} messages", 
            messageList.Count, standard);

        return await ExecutePluginOperationAsync(
            standard,
            registry => registry.GetFormatAnalysisPlugin(standard),
            async plugin =>
            {
                var result = await plugin.DetectEncodingDeviationsAsync(messageList);
                
                if (result.IsSuccess)
                {
                    _logger.LogInformation("Detected {DeviationCount} encoding deviations in {MessageCount} {Standard} messages",
                        result.Value.Count, messageList.Count, standard);
                }

                return result;
            },
            "encoding deviation detection");
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<FormatDeviation>>> DetectStructuralDeviationsAsync(
        IEnumerable<string> messages,
        string standard,
        string messageType)
    {
        var messageList = messages.ToList();
        if (messageList.Count == 0)
            return Result<IReadOnlyList<FormatDeviation>>.Failure("No messages provided for structural analysis");

        _logger.LogDebug("Detecting structural deviations in {MessageCount} {Standard} {MessageType} messages",
            messageList.Count, standard, messageType);

        return await ExecutePluginOperationAsync(
            standard,
            registry => registry.GetFormatAnalysisPlugin(standard),
            async plugin =>
            {
                var result = await plugin.DetectStructuralDeviationsAsync(messageList, messageType);
                
                if (result.IsSuccess)
                {
                    _logger.LogInformation("Detected {DeviationCount} structural deviations in {MessageCount} {Standard} messages",
                        result.Value.Count, messageList.Count, standard);
                }

                return result;
            },
            "structural deviation detection");
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<FormatDeviation>>> DetectFieldFormatDeviationsAsync(
        IEnumerable<string> messages,
        string standard,
        string segmentType)
    {
        var messageList = messages.ToList();
        if (messageList.Count == 0)
            return Result<IReadOnlyList<FormatDeviation>>.Failure("No messages provided for field format analysis");

        _logger.LogDebug("Detecting field format deviations in {MessageCount} {Standard} {SegmentType} segments",
            messageList.Count, standard, segmentType);

        return await ExecutePluginOperationAsync(
            standard,
            registry => registry.GetFormatAnalysisPlugin(standard),
            async plugin =>
            {
                var result = await plugin.DetectFieldFormatDeviationsAsync(messageList, segmentType);
                
                if (result.IsSuccess)
                {
                    _logger.LogInformation("Detected {DeviationCount} field format deviations in {MessageCount} {Standard} messages",
                        result.Value.Count, messageList.Count, standard);
                }

                return result;
            },
            "field format deviation detection");
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<FormatDeviation>>> DetectAllDeviationsAsync(
        IEnumerable<string> messages,
        string standard,
        string? messageType = null)
    {
        var messageList = messages.ToList();
        if (messageList.Count == 0)
            return Result<IReadOnlyList<FormatDeviation>>.Failure("No messages provided for comprehensive analysis");

        _logger.LogInformation("Starting comprehensive deviation detection for {MessageCount} {Standard} messages",
            messageList.Count, standard);

        return await ExecutePluginOperationAsync(
            standard,
            registry => registry.GetFormatAnalysisPlugin(standard),
            async plugin =>
            {
                var result = await plugin.DetectAllDeviationsAsync(messageList, messageType);
                
                if (result.IsSuccess)
                {
                    _logger.LogInformation("Comprehensive deviation detection completed: {TotalDeviations} total deviations found for {Standard}",
                        result.Value.Count, standard);
                }

                return result;
            },
            "comprehensive deviation detection");
    }
}