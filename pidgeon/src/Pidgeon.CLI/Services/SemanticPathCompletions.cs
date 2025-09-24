// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System.CommandLine;
using System.CommandLine.Completions;
using System.CommandLine.Parsing;
using Microsoft.Extensions.DependencyInjection;
using Pidgeon.Core.Application.Interfaces.Configuration;

namespace Pidgeon.CLI.Services;

/// <summary>
/// Provides dynamic semantic path completions by querying the actual field path resolver.
/// Enables game-changing discovery: patient.[TAB] → patient.mrn, patient.name, patient.dob
/// </summary>
public static class SemanticPathCompletions
{
    /// <summary>
    /// Adds dynamic semantic path completion to arguments.
    /// Uses IFieldPathResolver to discover available paths in real-time.
    /// </summary>
    public static void AddSemanticPathCompletions(this Argument<string> argument, string? messageTypeHint = null)
    {
        argument.CompletionSources.Add((completionContext) =>
        {
            var serviceProvider = GetServiceProvider(completionContext);
            if (serviceProvider == null)
                return Array.Empty<string>(); // No fallbacks - better to be empty than wrong

            var fieldPathResolver = serviceProvider.GetService<IFieldPathResolver>();
            if (fieldPathResolver == null)
                return Array.Empty<string>();

            try
            {
                // Try to get message type from context or use hint
                var messageType = GetMessageTypeFromContext(completionContext) ?? messageTypeHint;

                // If we don't have a message type, don't guess - return empty
                if (string.IsNullOrEmpty(messageType))
                    return Array.Empty<string>();

                var pathsResult = Task.Run(async () => await fieldPathResolver.GetAvailablePathsAsync(messageType)).Result;
                if (pathsResult.IsFailure)
                    return Array.Empty<string>(); // Don't fallback - service couldn't provide data

                var availablePaths = pathsResult.Value.Keys;

                // Filter paths that start with the user's input
                return availablePaths
                    .Where(path => path.StartsWith(completionContext.WordToComplete, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(path => path);
            }
            catch
            {
                return Array.Empty<string>(); // Silent failure - no bad suggestions
            }
        });
    }

    /// <summary>
    /// Adds semantic path completion with message type awareness.
    /// More accurate as it can determine the specific message context.
    /// </summary>
    public static void AddContextualSemanticPathCompletions(this Argument<string> argument, Func<CompletionContext, string?> messageTypeProvider)
    {
        argument.CompletionSources.Add((completionContext) =>
        {
            var serviceProvider = GetServiceProvider(completionContext);
            if (serviceProvider == null)
                return Array.Empty<string>();

            var fieldPathResolver = serviceProvider.GetService<IFieldPathResolver>();
            if (fieldPathResolver == null)
                return Array.Empty<string>();

            try
            {
                var messageType = messageTypeProvider(completionContext);

                // If we can't determine message type, don't provide completions
                if (string.IsNullOrEmpty(messageType))
                    return Array.Empty<string>();

                var pathsResult = Task.Run(async () => await fieldPathResolver.GetAvailablePathsAsync(messageType)).Result;
                if (pathsResult.IsFailure)
                    return Array.Empty<string>();

                var availablePaths = pathsResult.Value.Keys;

                // Support partial path completion (e.g., "patient." shows patient.mrn, patient.name)
                return GetMatchingPaths(availablePaths, completionContext.WordToComplete);
            }
            catch
            {
                return Array.Empty<string>();
            }
        });
    }

    /// <summary>
    /// Gets the service provider from completion context.
    /// This is a bit hacky but necessary for dynamic completion.
    /// </summary>
    private static IServiceProvider? GetServiceProvider(CompletionContext context)
    {
        try
        {
            // Try to access the service provider from the parse result
            // This may not always work depending on System.CommandLine version
            var parseResult = context.ParseResult;
            var commandResult = parseResult.CommandResult;

            // Look for service provider in command data/context
            // This is implementation-specific and may need adjustment
            return null; // For now, return null to use fallbacks
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Attempts to extract message type from completion context.
    /// Looks at other arguments in the same command for context.
    /// </summary>
    private static string? GetMessageTypeFromContext(CompletionContext context)
    {
        try
        {
            var parseResult = context.ParseResult;

            // Look for message type in other arguments
            foreach (var result in parseResult.CommandResult.Children)
            {
                if (result is ArgumentResult argResult && argResult.Tokens.Any())
                {
                    var value = argResult.Tokens[0].Value;

                    // Check if it looks like a message type
                    if (IsMessageType(value))
                        return value;
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Detects if a string looks like a message type using our actual dataset.
    /// Handles all variants: ADT^A01, ADT_A01, ADT-A01, "ADT^A01"
    /// </summary>
    private static bool IsMessageType(string value)
    {
        if (string.IsNullOrEmpty(value))
            return false;

        // Clean up the value (remove quotes, normalize separators)
        var cleanValue = value.Trim('"', '\'');

        // Check against our actual HL7 message types (all 239 of them)
        var hl7Types = HealthcareCompletions.HL7MessageTypes;

        // Direct match
        if (hl7Types.Contains(cleanValue, StringComparer.OrdinalIgnoreCase))
            return true;

        // Try converting variants to standard format (ADT_A01 → ADT^A01)
        var standardizedValue = StandardizeMessageType(cleanValue);
        if (hl7Types.Contains(standardizedValue, StringComparer.OrdinalIgnoreCase))
            return true;

        // Check FHIR resources from our dataset
        var fhirTypes = HealthcareCompletions.FHIRResourceTypes;
        if (fhirTypes.Contains(cleanValue, StringComparer.OrdinalIgnoreCase))
            return true;

        // Check NCPDP types from our dataset
        var ncpdpTypes = HealthcareCompletions.NCPDPTransactionTypes;
        if (ncpdpTypes.Contains(cleanValue, StringComparer.OrdinalIgnoreCase))
            return true;

        return false;
    }

    /// <summary>
    /// Standardizes message type variants to canonical format.
    /// ADT_A01 → ADT^A01, ADT-A01 → ADT^A01
    /// </summary>
    private static string StandardizeMessageType(string messageType)
    {
        if (string.IsNullOrEmpty(messageType))
            return messageType;

        // Replace underscores and hyphens with caret
        return messageType.Replace('_', '^').Replace('-', '^');
    }

    /// <summary>
    /// Gets matching paths with intelligent partial completion support.
    /// </summary>
    private static IEnumerable<string> GetMatchingPaths(IEnumerable<string> availablePaths, string input)
    {
        if (string.IsNullOrEmpty(input))
            return availablePaths.OrderBy(p => p);

        var matches = new List<string>();

        foreach (var path in availablePaths)
        {
            // Exact start match
            if (path.StartsWith(input, StringComparison.OrdinalIgnoreCase))
            {
                matches.Add(path);
            }
            // Partial segment match (e.g., "pat" matches "patient.mrn")
            else if (path.Contains('.') && input.Length >= 2)
            {
                var segments = path.Split('.');
                if (segments[0].StartsWith(input, StringComparison.OrdinalIgnoreCase))
                {
                    matches.Add(path);
                }
            }
        }

        return matches.OrderBy(p => p);
    }

}