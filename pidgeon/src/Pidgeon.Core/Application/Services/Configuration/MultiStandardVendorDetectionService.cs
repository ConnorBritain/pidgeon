// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Microsoft.Extensions.Logging;
using Pidgeon.Core.Application.Interfaces.Configuration;
using Pidgeon.Core.Domain.Configuration.Entities;
using Pidgeon.Core.Generation;

namespace Pidgeon.Core.Application.Services.Configuration;

/// <summary>
/// Multi-standard vendor detection service that orchestrates vendor pattern analysis
/// across HL7, FHIR, NCPDP, and other healthcare standards.
/// Provides unified vendor detection interface while delegating to standard-specific plugins.
/// </summary>
internal class MultiStandardVendorDetectionService : IVendorDetectionService
{
    private readonly IStandardVendorPluginRegistry _pluginRegistry;
    private readonly ILogger<MultiStandardVendorDetectionService> _logger;

    public MultiStandardVendorDetectionService(
        IStandardVendorPluginRegistry pluginRegistry,
        ILogger<MultiStandardVendorDetectionService> logger)
    {
        _pluginRegistry = pluginRegistry;
        _logger = logger;
    }

    /// <summary>
    /// Detects vendor patterns in a message using smart standard inference.
    /// Automatically determines the healthcare standard and routes to appropriate plugin.
    /// </summary>
    public async Task<Result<VendorMatch>> DetectVendorAsync(string messageContent)
    {
        if (string.IsNullOrWhiteSpace(messageContent))
            return Result<VendorMatch>.Failure("Message content cannot be empty");

        try
        {
            // Get plugins that can handle this message (ordered by priority)
            var compatiblePlugins = _pluginRegistry.GetPluginsForMessage(messageContent);
            
            if (!compatiblePlugins.Any())
            {
                _logger.LogWarning("No vendor plugins found for message content");
                return Result<VendorMatch>.Failure("No compatible vendor detection plugins found");
            }

            _logger.LogDebug("Found {PluginCount} compatible vendor plugins for message", compatiblePlugins.Count);

            // Try each plugin in priority order until we find a confident match
            var bestMatch = default(VendorMatch);
            var allMatches = new List<VendorMatch>();

            foreach (var plugin in compatiblePlugins)
            {
                try
                {
                    // Get baseline configurations for this standard
                    var baselineResult = await plugin.GetBaselineVendorConfigurationsAsync();
                    if (baselineResult.IsFailure)
                    {
                        _logger.LogWarning("Failed to get baseline configurations for {Standard}: {Error}", 
                            plugin.Standard, baselineResult.Error);
                        continue;
                    }

                    // Detect vendor candidates using this plugin
                    var candidatesResult = await plugin.DetectVendorCandidatesAsync(messageContent, baselineResult.Value);
                    if (candidatesResult.IsFailure)
                    {
                        _logger.LogWarning("Vendor detection failed for {Standard}: {Error}", 
                            plugin.Standard, candidatesResult.Error);
                        continue;
                    }

                    var candidates = candidatesResult.Value;
                    allMatches.AddRange(candidates);

                    // Track the best match across all plugins
                    var topCandidate = candidates.OrderByDescending(c => c.Confidence).FirstOrDefault();
                    if (topCandidate != null && (bestMatch == null || topCandidate.Confidence > bestMatch.Confidence))
                    {
                        bestMatch = topCandidate;
                    }

                    _logger.LogDebug("Plugin {Standard} found {CandidateCount} vendor candidates", 
                        plugin.Standard, candidates.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during vendor detection with plugin {Standard}", plugin.Standard);
                }
            }

            if (bestMatch == null)
            {
                return Result<VendorMatch>.Failure("No vendor patterns matched with sufficient confidence");
            }

            _logger.LogInformation("Detected vendor {Vendor} with {Confidence:P1} confidence using {Standard}", 
                bestMatch.VendorConfiguration.Address.Vendor, bestMatch.Confidence, bestMatch.Standard);

            return Result<VendorMatch>.Success(bestMatch);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during vendor detection");
            return Result<VendorMatch>.Failure($"Vendor detection failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Analyzes a collection of messages to infer vendor patterns across multiple standards.
    /// Automatically groups messages by standard and processes each group with appropriate plugin.
    /// </summary>
    public async Task<Result<VendorConfiguration>> AnalyzeVendorPatternsAsync(
        IEnumerable<string> messages, 
        InferenceOptions options)
    {
        if (messages == null)
            return Result<VendorConfiguration>.Failure("Messages collection cannot be null");

        var messageList = messages.ToList();
        if (!messageList.Any())
            return Result<VendorConfiguration>.Failure("Must provide at least one message for analysis");

        try
        {
            _logger.LogInformation("Analyzing vendor patterns across {MessageCount} messages", messageList.Count);

            // Group messages by standard using plugin detection
            var messagesByStandard = new Dictionary<string, List<string>>();
            var unclassifiedMessages = new List<string>();

            foreach (var message in messageList)
            {
                var compatiblePlugins = _pluginRegistry.GetPluginsForMessage(message);
                if (compatiblePlugins.Any())
                {
                    var primaryPlugin = compatiblePlugins.First(); // Highest priority plugin
                    var standard = primaryPlugin.Standard;
                    
                    if (!messagesByStandard.ContainsKey(standard))
                        messagesByStandard[standard] = new List<string>();
                    
                    messagesByStandard[standard].Add(message);
                }
                else
                {
                    unclassifiedMessages.Add(message);
                }
            }

            if (unclassifiedMessages.Any())
            {
                _logger.LogWarning("Found {UnclassifiedCount} messages that could not be classified by standard", 
                    unclassifiedMessages.Count);
            }

            if (!messagesByStandard.Any())
            {
                return Result<VendorConfiguration>.Failure("No messages could be classified by any supported standard");
            }

            // Analyze each standard group and find the most confident result
            VendorConfiguration? bestConfiguration = null;
            double bestConfidence = 0.0;

            foreach (var (standard, standardMessages) in messagesByStandard)
            {
                var plugin = _pluginRegistry.GetPluginForStandard(standard);
                if (plugin == null)
                {
                    _logger.LogWarning("No plugin found for standard {Standard}", standard);
                    continue;
                }

                _logger.LogDebug("Analyzing {MessageCount} {Standard} messages", standardMessages.Count, standard);

                try
                {
                    var analysisResult = await plugin.AnalyzeVendorPatternsAsync(standardMessages, options);
                    if (analysisResult.IsSuccess)
                    {
                        var config = analysisResult.Value;
                        if (config.Metadata.Confidence > bestConfidence)
                        {
                            bestConfiguration = config;
                            bestConfidence = config.Metadata.Confidence;
                        }

                        _logger.LogInformation("Standard {Standard} analysis completed: {Vendor} with {Confidence:P1} confidence", 
                            standard, config.Address.Vendor, config.Metadata.Confidence);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to analyze {Standard} messages: {Error}", 
                            standard, analysisResult.Error);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error analyzing {Standard} messages", standard);
                }
            }

            if (bestConfiguration == null)
            {
                return Result<VendorConfiguration>.Failure("No standard plugins successfully analyzed the message patterns");
            }

            _logger.LogInformation("Vendor pattern analysis completed: {Summary}", bestConfiguration.GetSummary());
            return Result<VendorConfiguration>.Success(bestConfiguration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during vendor pattern analysis");
            return Result<VendorConfiguration>.Failure($"Vendor pattern analysis failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets all vendor configurations across all supported standards.
    /// Provides unified view of vendor intelligence from all plugins.
    /// </summary>
    public async Task<Result<IReadOnlyList<VendorConfiguration>>> GetAllVendorConfigurationsAsync()
    {
        try
        {
            var allConfigurations = new List<VendorConfiguration>();
            var plugins = _pluginRegistry.GetAllPlugins();

            _logger.LogDebug("Collecting vendor configurations from {PluginCount} standard plugins", plugins.Count);

            foreach (var plugin in plugins)
            {
                try
                {
                    var baselineResult = await plugin.GetBaselineVendorConfigurationsAsync();
                    if (baselineResult.IsSuccess)
                    {
                        allConfigurations.AddRange(baselineResult.Value);
                        _logger.LogDebug("Added {ConfigCount} configurations from {Standard} plugin", 
                            baselineResult.Value.Count, plugin.Standard);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to get configurations from {Standard} plugin: {Error}", 
                            plugin.Standard, baselineResult.Error);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting configurations from {Standard} plugin", plugin.Standard);
                }
            }

            _logger.LogInformation("Collected {TotalConfigurations} vendor configurations across {StandardCount} standards", 
                allConfigurations.Count, plugins.Count);

            return Result<IReadOnlyList<VendorConfiguration>>.Success(allConfigurations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error collecting vendor configurations");
            return Result<IReadOnlyList<VendorConfiguration>>.Failure($"Failed to collect vendor configurations: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets vendor configurations for a specific standard.
    /// Delegates to the appropriate standard plugin.
    /// </summary>
    public async Task<Result<IReadOnlyList<VendorConfiguration>>> GetVendorConfigurationsForStandardAsync(string standard)
    {
        if (string.IsNullOrWhiteSpace(standard))
            return Result<IReadOnlyList<VendorConfiguration>>.Failure("Standard cannot be empty");

        try
        {
            var plugin = _pluginRegistry.GetPluginForStandard(standard);
            if (plugin == null)
            {
                // Try standard family matching (e.g., "HL7" matches "HL7v23", "HL7v25")
                var familyPlugins = _pluginRegistry.GetPluginsForStandardFamily(standard);
                if (familyPlugins.Any())
                {
                    // Collect from all plugins in the family
                    var allConfigurations = new List<VendorConfiguration>();
                    foreach (var familyPlugin in familyPlugins)
                    {
                        var familyResult = await familyPlugin.GetBaselineVendorConfigurationsAsync();
                        if (familyResult.IsSuccess)
                        {
                            allConfigurations.AddRange(familyResult.Value);
                        }
                    }
                    return Result<IReadOnlyList<VendorConfiguration>>.Success(allConfigurations);
                }
                
                return Result<IReadOnlyList<VendorConfiguration>>.Failure($"No plugin found for standard '{standard}'");
            }

            var result = await plugin.GetBaselineVendorConfigurationsAsync();
            if (result.IsFailure)
                return Result<IReadOnlyList<VendorConfiguration>>.Failure(result.Error);

            _logger.LogDebug("Retrieved {ConfigCount} vendor configurations for standard {Standard}", 
                result.Value.Count, standard);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting vendor configurations for standard {Standard}", standard);
            return Result<IReadOnlyList<VendorConfiguration>>.Failure($"Failed to get configurations: {ex.Message}");
        }
    }
}