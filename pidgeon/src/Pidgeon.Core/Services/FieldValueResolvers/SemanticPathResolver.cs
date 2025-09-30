// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Microsoft.Extensions.Logging;
using Pidgeon.Core.Application.Interfaces.Configuration;

namespace Pidgeon.Core.Services.FieldValueResolvers;

/// <summary>
/// Resolves field values from user-set semantic path values in active session.
/// Bridges semantic paths (patient.mrn) with actual field positions (PID.3).
/// Priority: 100 (highest - user-set values override all other generation)
/// </summary>
public class SemanticPathResolver : IFieldValueResolver
{
    private readonly ILogger<SemanticPathResolver> _logger;
    private readonly ILockSessionService _lockSessionService;
    private readonly IFieldPathResolver _fieldPathResolver;

    public int Priority => 100;

    public SemanticPathResolver(
        ILogger<SemanticPathResolver> logger,
        ILockSessionService lockSessionService,
        IFieldPathResolver fieldPathResolver)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _lockSessionService = lockSessionService ?? throw new ArgumentNullException(nameof(lockSessionService));
        _fieldPathResolver = fieldPathResolver ?? throw new ArgumentNullException(nameof(fieldPathResolver));
    }

    public async Task<string?> ResolveAsync(FieldResolutionContext context)
    {
        try
        {
            // Check if session is configured in options
            var sessionName = context.Options.LockSessionName;
            if (string.IsNullOrEmpty(sessionName))
            {
                _logger.LogDebug("No session configured in generation options, skipping session resolution");
                return null; // No session configured
            }

            // Get current field path for this segment/position
            var currentFieldPath = $"{context.SegmentCode}.{context.FieldPosition}";

            _logger.LogDebug("Checking session values for field path: {FieldPath} in session: {SessionName}",
                currentFieldPath, sessionName);

            // Get all locked values from specified session
            var sessionValuesResult = await GetSessionValuesAsync(sessionName);
            if (sessionValuesResult.IsFailure || !sessionValuesResult.Value.Any())
            {
                _logger.LogDebug("No active session or session values found");
                return null; // No session or no values set
            }

            var sessionValues = sessionValuesResult.Value;

            // Check for direct field path matches first (e.g., "PID.3" set directly)
            if (sessionValues.TryGetValue(currentFieldPath, out var directValue))
            {
                _logger.LogDebug("Found direct field path value for {FieldPath}: '{Value}'",
                    currentFieldPath, directValue);
                return directValue;
            }

            // Check for semantic path matches (e.g., "patient.mrn" → "PID.3")
            foreach (var (semanticPath, value) in sessionValues)
            {
                // Skip if this looks like a direct field path (contains dots and numbers)
                if (IsDirectFieldPath(semanticPath))
                    continue;

                // Try to resolve this semantic path to a field path
                var resolvedPathResult = await _fieldPathResolver.ResolvePathAsync(
                    semanticPath,
                    context.GenerationContext.MessageType,
                    "HL7v23"); // TODO: Get standard from context

                if (resolvedPathResult.IsSuccess &&
                    resolvedPathResult.Value.Equals(currentFieldPath, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogDebug("Found semantic path match: '{SemanticPath}' → '{FieldPath}' = '{Value}'",
                        semanticPath, currentFieldPath, value);
                    return value;
                }
            }

            _logger.LogDebug("No session value found for field path: {FieldPath}", currentFieldPath);
            return null; // No matching value found
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving semantic path value for field {SegmentCode}.{FieldPosition}",
                context.SegmentCode, context.FieldPosition);
            return null; // Continue to other resolvers on error
        }
    }

    /// <summary>
    /// Get locked values from the specified session.
    /// </summary>
    private async Task<Result<IReadOnlyDictionary<string, string>>> GetSessionValuesAsync(string sessionName)
    {
        try
        {
            var sessionValuesResult = await _lockSessionService.GetLockedValuesAsync(sessionName);

            if (sessionValuesResult.IsFailure)
            {
                _logger.LogDebug("No values found in session {SessionName}: {Error}", sessionName, sessionValuesResult.Error);
                return Result<IReadOnlyDictionary<string, string>>.Success(
                    new Dictionary<string, string>().AsReadOnly());
            }

            return sessionValuesResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active session values");
            return Result<IReadOnlyDictionary<string, string>>.Failure("Failed to retrieve session values");
        }
    }

    /// <summary>
    /// Determine if a path looks like a direct field path (PID.3) vs semantic path (patient.mrn).
    /// </summary>
    private static bool IsDirectFieldPath(string path)
    {
        // Direct field paths typically look like: "PID.3", "MSH.10", "OBX.5.1"
        // Semantic paths look like: "patient.mrn", "patient.name", "encounter.location"

        if (string.IsNullOrEmpty(path))
            return false;

        var parts = path.Split('.');
        if (parts.Length < 2)
            return false;

        // First part should be a segment code (2-3 uppercase letters)
        var firstPart = parts[0];
        if (firstPart.Length < 2 || firstPart.Length > 3 || !firstPart.All(char.IsUpper))
            return false;

        // Second part should be numeric (field position)
        var secondPart = parts[1];
        if (!int.TryParse(secondPart, out _))
            return false;

        return true;
    }
}