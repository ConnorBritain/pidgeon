// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Microsoft.Extensions.Logging;
using Pidgeon.Core.Application.Interfaces.Configuration;
using Pidgeon.Core.Domain.Configuration.Entities;
using Pidgeon.Core;
using System.Text.Json;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Pidgeon.Core.Application.Services.Configuration;

/// <summary>
/// Service for exporting and importing lock sessions to/from various formats.
/// Implements template creation and sharing functionality.
/// </summary>
public class SessionExportService : ISessionExportService
{
    private readonly ILockSessionService _lockSessionService;
    private readonly ILogger<SessionExportService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static readonly ISerializer YamlSerializer = new SerializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .Build();

    private static readonly IDeserializer YamlDeserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .Build();

    public SessionExportService(
        ILockSessionService lockSessionService,
        ILogger<SessionExportService> logger)
    {
        _lockSessionService = lockSessionService;
        _logger = logger;
    }

    public async Task<Result<string>> ExportSessionAsync(
        string sessionName,
        ExportFormat format,
        ExportOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(sessionName))
            {
                return Result<string>.Failure("Session name cannot be empty");
            }

            options ??= new ExportOptions();

            // Get the session
            var sessionResult = await _lockSessionService.GetSessionAsync(sessionName, cancellationToken);
            if (sessionResult.IsFailure)
            {
                return Result<string>.Failure($"Failed to retrieve session: {sessionResult.Error.Message}");
            }

            var session = sessionResult.Value;

            // Create exportable session object
            var exportableSession = CreateExportableSession(session, options);

            // Serialize to requested format
            string serializedData = format switch
            {
                ExportFormat.Json => JsonSerializer.Serialize(exportableSession, options.PrettyFormat ? JsonOptions : new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }),
                ExportFormat.Yaml => YamlSerializer.Serialize(exportableSession),
                _ => throw new ArgumentException($"Unsupported export format: {format}")
            };

            _logger.LogInformation("Successfully exported session {SessionName} to {Format} format", sessionName, format);
            return Result<string>.Success(serializedData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export session {SessionName}", sessionName);
            return Result<string>.Failure($"Export failed: {ex.Message}");
        }
    }

    public async Task<Result<LockSession>> ImportSessionAsync(
        string sessionData,
        ImportOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(sessionData))
            {
                return Result<LockSession>.Failure("Session data cannot be empty");
            }

            options ??= new ImportOptions();

            // Validate session data if requested
            if (options.ValidateBeforeImport)
            {
                var validationResult = await ValidateSessionDataAsync(sessionData);
                if (validationResult.IsFailure)
                {
                    return Result<LockSession>.Failure($"Validation failed: {validationResult.Error.Message}");
                }

                if (!validationResult.Value.IsValid)
                {
                    var errors = string.Join(", ", validationResult.Value.Errors);
                    return Result<LockSession>.Failure($"Session data validation failed: {errors}");
                }
            }

            // Deserialize session data
            var deserializeResult = DeserializeSessionData(sessionData);
            if (deserializeResult.IsFailure)
            {
                return Result<LockSession>.Failure($"Failed to deserialize session data: {deserializeResult.Error.Message}");
            }

            var importableSession = deserializeResult.Value;

            // Create lock session from importable data
            var session = CreateLockSessionFromImport(importableSession, options);

            // Check if session exists and handle overwrite logic
            var sessionName = options.SessionName ?? session.Name;
            var existingSessionResult = await _lockSessionService.GetSessionAsync(sessionName, cancellationToken);

            if (existingSessionResult.IsSuccess && !options.OverwriteExisting)
            {
                return Result<LockSession>.Failure($"Session '{sessionName}' already exists. Use OverwriteExisting option to replace it.");
            }

            // Create or update the session
            if (existingSessionResult.IsSuccess && options.OverwriteExisting)
            {
                // Remove existing session first
                await _lockSessionService.RemoveSessionAsync(sessionName, cancellationToken);
            }

            // Create new session
            var createResult = await _lockSessionService.CreateSessionAsync(
                sessionName,
                session.Scope,
                session.Description,
                cancellationToken);

            if (createResult.IsFailure)
            {
                return Result<LockSession>.Failure($"Failed to create session: {createResult.Error.Message}");
            }

            // Set locked values
            foreach (var lockedValue in session.LockedValues)
            {
                var setValueResult = await _lockSessionService.SetValueAsync(
                    sessionName,
                    lockedValue.FieldPath,
                    lockedValue.Value,
                    lockedValue.Reason,
                    cancellationToken);

                if (setValueResult.IsFailure)
                {
                    _logger.LogWarning("Failed to set value {FieldPath} during import: {Error}",
                        lockedValue.FieldPath, setValueResult.Error.Message);
                }
            }

            // Get the final created session
            var finalSessionResult = await _lockSessionService.GetSessionAsync(sessionName, cancellationToken);
            if (finalSessionResult.IsFailure)
            {
                return Result<LockSession>.Failure($"Failed to retrieve imported session: {finalSessionResult.Error.Message}");
            }

            _logger.LogInformation("Successfully imported session {SessionName} with {ValueCount} locked values",
                sessionName, session.LockedValues.Count);

            return Result<LockSession>.Success(finalSessionResult.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import session");
            return Result<LockSession>.Failure($"Import failed: {ex.Message}");
        }
    }

    public async Task<Result<LockSession>> ImportSessionFromFileAsync(
        string filePath,
        ImportOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return Result<LockSession>.Failure("File path cannot be empty");
            }

            if (!File.Exists(filePath))
            {
                return Result<LockSession>.Failure($"File not found: {filePath}");
            }

            var sessionData = await File.ReadAllTextAsync(filePath, cancellationToken);
            return await ImportSessionAsync(sessionData, options, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import session from file {FilePath}", filePath);
            return Result<LockSession>.Failure($"File import failed: {ex.Message}");
        }
    }

    public async Task<Result<SessionValidationResult>> ValidateSessionDataAsync(
        string sessionData,
        ExportFormat? format = null)
    {
        await Task.Yield();

        try
        {
            if (string.IsNullOrWhiteSpace(sessionData))
            {
                return Result<SessionValidationResult>.Success(new SessionValidationResult
                {
                    IsValid = false,
                    Errors = ["Session data cannot be empty"]
                });
            }

            var errors = new List<string>();
            var warnings = new List<string>();
            ExportFormat? detectedFormat = null;

            // Try to detect format and validate
            var deserializeResult = DeserializeSessionData(sessionData);
            if (deserializeResult.IsFailure)
            {
                errors.Add($"Failed to parse session data: {deserializeResult.Error.Message}");
            }
            else
            {
                var session = deserializeResult.Value;
                detectedFormat = DetectFormat(sessionData);

                // Validate required fields
                if (string.IsNullOrWhiteSpace(session.Name))
                {
                    errors.Add("Session name is required");
                }

                if (session.LockedValues == null)
                {
                    warnings.Add("No locked values found in session");
                }
                else if (session.LockedValues.Count == 0)
                {
                    warnings.Add("Session contains no locked values");
                }

                // Validate locked values structure
                foreach (var lockedValue in session.LockedValues ?? [])
                {
                    if (string.IsNullOrWhiteSpace(lockedValue.FieldPath))
                    {
                        errors.Add("Locked value missing field path");
                    }

                    if (string.IsNullOrWhiteSpace(lockedValue.Value))
                    {
                        warnings.Add($"Locked value for '{lockedValue.FieldPath}' has empty value");
                    }
                }
            }

            return Result<SessionValidationResult>.Success(new SessionValidationResult
            {
                IsValid = errors.Count == 0,
                Errors = errors,
                Warnings = warnings,
                DetectedFormat = detectedFormat
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate session data");
            return Result<SessionValidationResult>.Failure($"Validation failed: {ex.Message}");
        }
    }

    public IReadOnlyList<ExportFormat> GetSupportedFormats()
    {
        return [ExportFormat.Json, ExportFormat.Yaml];
    }

    private static ExportableSession CreateExportableSession(LockSession session, ExportOptions options)
    {
        return new ExportableSession
        {
            Name = session.Name,
            Description = session.Description,
            Scope = session.Scope.ToString(),
            LockedValues = session.LockedValues.Select(lv => new ExportableLockValue
            {
                FieldPath = lv.FieldPath,
                Value = lv.Value,
                Reason = lv.Reason,
                DataType = lv.DataType,
                LockedAt = options.IncludeTimestamps ? lv.LockedAt : null
            }).ToList(),
            Metadata = options.IncludeMetadata ? new ExportableMetadata
            {
                CreatedAt = options.IncludeTimestamps ? session.CreatedAt : null,
                Tags = session.Tags.ToList(),
                TemplateMetadata = options.TemplateMetadata
            } : null
        };
    }

    private static LockSession CreateLockSessionFromImport(ExportableSession importableSession, ImportOptions options)
    {
        var sessionName = options.SessionName ?? importableSession.Name;
        var now = DateTime.UtcNow;

        if (!Enum.TryParse<LockScope>(importableSession.Scope, true, out var scope))
        {
            scope = LockScope.Patient; // Default fallback
        }

        var lockedValues = (importableSession.LockedValues ?? []).Select(lv => new LockValue
        {
            FieldPath = lv.FieldPath ?? "",
            Value = lv.Value ?? "",
            Reason = lv.Reason,
            DataType = lv.DataType,
            LockedAt = options.PreserveTimestamps && lv.LockedAt.HasValue ? lv.LockedAt.Value : now
        }).ToList();

        return new LockSession
        {
            SessionId = Guid.NewGuid().ToString(),
            Name = sessionName,
            Description = importableSession.Description,
            CreatedAt = options.PreserveTimestamps && importableSession.Metadata?.CreatedAt.HasValue == true
                ? importableSession.Metadata.CreatedAt.Value
                : now,
            LastAccessedAt = now,
            Scope = scope,
            LockedValues = lockedValues,
            Tags = importableSession.Metadata?.Tags ?? [],
            IsActive = true
        };
    }

    private static Result<ExportableSession> DeserializeSessionData(string sessionData)
    {
        try
        {
            // Try JSON first
            try
            {
                var jsonSession = JsonSerializer.Deserialize<ExportableSession>(sessionData, JsonOptions);
                if (jsonSession != null)
                {
                    return Result<ExportableSession>.Success(jsonSession);
                }
            }
            catch
            {
                // Fall through to YAML
            }

            // Try YAML
            var yamlSession = YamlDeserializer.Deserialize<ExportableSession>(sessionData);
            if (yamlSession != null)
            {
                return Result<ExportableSession>.Success(yamlSession);
            }

            return Result<ExportableSession>.Failure("Could not deserialize session data as JSON or YAML");
        }
        catch (Exception ex)
        {
            return Result<ExportableSession>.Failure($"Deserialization failed: {ex.Message}");
        }
    }

    private static ExportFormat? DetectFormat(string sessionData)
    {
        var trimmedData = sessionData.TrimStart();

        if (trimmedData.StartsWith('{') || trimmedData.StartsWith('['))
        {
            return ExportFormat.Json;
        }

        if (trimmedData.Contains(':') && !trimmedData.StartsWith('{'))
        {
            return ExportFormat.Yaml;
        }

        return null;
    }

    /// <summary>
    /// Exportable session structure for serialization.
    /// </summary>
    private record ExportableSession
    {
        public string Name { get; init; } = "";
        public string? Description { get; init; }
        public string Scope { get; init; } = "";
        public List<ExportableLockValue> LockedValues { get; init; } = [];
        public ExportableMetadata? Metadata { get; init; }
    }

    /// <summary>
    /// Exportable lock value structure.
    /// </summary>
    private record ExportableLockValue
    {
        public string? FieldPath { get; init; }
        public string? Value { get; init; }
        public string? Reason { get; init; }
        public string? DataType { get; init; }
        public DateTime? LockedAt { get; init; }
    }

    /// <summary>
    /// Exportable metadata structure.
    /// </summary>
    private record ExportableMetadata
    {
        public DateTime? CreatedAt { get; init; }
        public List<string> Tags { get; init; } = [];
        public TemplateMetadata? TemplateMetadata { get; init; }
    }
}