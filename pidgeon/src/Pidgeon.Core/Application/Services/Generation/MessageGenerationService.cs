// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Pidgeon.Core.Application.Interfaces.Generation;
using Pidgeon.Core.Generation;

namespace Pidgeon.Core.Application.Services.Generation;

/// <summary>
/// Main message generation service using plugin-segregated architecture.
/// Each healthcare standard plugin owns its message type universe and generation logic.
/// </summary>
internal class MessageGenerationService : IMessageGenerationService
{
    private readonly IEnumerable<IMessageGenerationPlugin> _messageGenerationPlugins;
    
    public MessageGenerationService(
        IEnumerable<IMessageGenerationPlugin> messageGenerationPlugins)
    {
        _messageGenerationPlugins = messageGenerationPlugins;
    }
    
    /// <summary>
    /// Generates synthetic healthcare messages by delegating to appropriate standard plugin.
    /// </summary>
    /// <param name="standard">Healthcare standard (hl7, fhir, ncpdp)</param>
    /// <param name="messageType">Standard-specific message type</param>
    /// <param name="count">Number of messages to generate</param>
    /// <param name="options">Generation options</param>
    /// <returns>Generated message strings or error with intelligent suggestions</returns>
    public async Task<Result<IReadOnlyList<string>>> GenerateSyntheticDataAsync(
        string standard, 
        string messageType, 
        int count = 1, 
        GenerationOptions? options = null)
    {
        try
        {
            // Find the appropriate plugin for the specified standard
            var plugin = _messageGenerationPlugins.FirstOrDefault(p => 
                p.StandardName.Equals(standard, StringComparison.OrdinalIgnoreCase));
                
            if (plugin == null)
            {
                var availableStandards = _messageGenerationPlugins.Select(p => p.StandardName).ToList();
                return Result<IReadOnlyList<string>>.Failure(
                    $"No plugin found for standard '{standard}'. Available standards: {string.Join(", ", availableStandards)}");
            }

            // Check if the plugin can handle the message type
            if (!plugin.CanHandleMessageType(messageType))
            {
                return Result<IReadOnlyList<string>>.Failure(plugin.GetUnsupportedMessageTypeError(messageType));
            }

            // Delegate generation to the standard-specific plugin
            return await plugin.GenerateMessagesAsync(messageType, count, options);
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<string>>.Failure($"Message generation failed: {ex.Message}");
        }
    }
}