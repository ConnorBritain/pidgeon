// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Pidgeon.Core.Domain.Configuration.Entities;
using Pidgeon.Core;

namespace Pidgeon.Core.Application.Interfaces.Configuration;

/// <summary>
/// Service for exporting and importing lock sessions to/from various formats.
/// Enables template creation and sharing for workflow automation.
/// </summary>
public interface ISessionExportService
{
    /// <summary>
    /// Exports a lock session to the specified format.
    /// </summary>
    /// <param name="sessionName">Name of the session to export</param>
    /// <param name="format">Export format (JSON, YAML)</param>
    /// <param name="options">Export options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the exported session data as string</returns>
    Task<Result<string>> ExportSessionAsync(
        string sessionName,
        ExportFormat format,
        ExportOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Imports a lock session from the specified data.
    /// </summary>
    /// <param name="sessionData">Session data in supported format</param>
    /// <param name="options">Import options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the imported session</returns>
    Task<Result<LockSession>> ImportSessionAsync(
        string sessionData,
        ImportOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Imports a lock session from a file path.
    /// </summary>
    /// <param name="filePath">Path to the session file</param>
    /// <param name="options">Import options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the imported session</returns>
    Task<Result<LockSession>> ImportSessionFromFileAsync(
        string filePath,
        ImportOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates session data format before import.
    /// </summary>
    /// <param name="sessionData">Session data to validate</param>
    /// <param name="format">Expected format</param>
    /// <returns>Result indicating validation success or specific errors</returns>
    Task<Result<SessionValidationResult>> ValidateSessionDataAsync(
        string sessionData,
        ExportFormat? format = null);

    /// <summary>
    /// Gets supported export formats.
    /// </summary>
    /// <returns>List of supported export formats</returns>
    IReadOnlyList<ExportFormat> GetSupportedFormats();
}

/// <summary>
/// Supported export/import formats for sessions.
/// </summary>
public enum ExportFormat
{
    /// <summary>
    /// JSON format with human-readable structure.
    /// </summary>
    Json,

    /// <summary>
    /// YAML format for better readability and comments.
    /// </summary>
    Yaml
}

/// <summary>
/// Options for exporting sessions.
/// </summary>
public record ExportOptions
{
    /// <summary>
    /// Whether to include metadata in the export.
    /// </summary>
    public bool IncludeMetadata { get; init; } = true;

    /// <summary>
    /// Whether to include timestamps in the export.
    /// </summary>
    public bool IncludeTimestamps { get; init; } = false;

    /// <summary>
    /// Whether to format the output for readability.
    /// </summary>
    public bool PrettyFormat { get; init; } = true;

    /// <summary>
    /// Optional template metadata to include.
    /// </summary>
    public TemplateMetadata? TemplateMetadata { get; init; }
}

/// <summary>
/// Options for importing sessions.
/// </summary>
public record ImportOptions
{
    /// <summary>
    /// Name for the imported session. If null, uses name from import data.
    /// </summary>
    public string? SessionName { get; init; }

    /// <summary>
    /// Whether to overwrite existing session with same name.
    /// </summary>
    public bool OverwriteExisting { get; init; } = false;

    /// <summary>
    /// Whether to preserve original timestamps.
    /// </summary>
    public bool PreserveTimestamps { get; init; } = false;

    /// <summary>
    /// Whether to validate the session data before import.
    /// </summary>
    public bool ValidateBeforeImport { get; init; } = true;
}

/// <summary>
/// Template metadata for marketplace functionality.
/// </summary>
public record TemplateMetadata
{
    /// <summary>
    /// Template name for marketplace.
    /// </summary>
    public string Name { get; init; } = "";

    /// <summary>
    /// Template description.
    /// </summary>
    public string Description { get; init; } = "";

    /// <summary>
    /// Template author.
    /// </summary>
    public string Author { get; init; } = "";

    /// <summary>
    /// Template version.
    /// </summary>
    public string Version { get; init; } = "1.0.0";

    /// <summary>
    /// Template category for organization.
    /// </summary>
    public string Category { get; init; } = "";

    /// <summary>
    /// Tags for searchability.
    /// </summary>
    public List<string> Tags { get; init; } = [];

    /// <summary>
    /// Whether template is verified/trusted.
    /// </summary>
    public bool Verified { get; init; } = false;
}

/// <summary>
/// Result of session data validation.
/// </summary>
public record SessionValidationResult
{
    /// <summary>
    /// Whether the session data is valid.
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// Validation error messages.
    /// </summary>
    public List<string> Errors { get; init; } = [];

    /// <summary>
    /// Validation warnings.
    /// </summary>
    public List<string> Warnings { get; init; } = [];

    /// <summary>
    /// Detected format of the session data.
    /// </summary>
    public ExportFormat? DetectedFormat { get; init; }
}