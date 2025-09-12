// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

namespace Pidgeon.Core.Domain.Intelligence;

/// <summary>
/// Progress information for model download operations.
/// </summary>
public record DownloadProgress
{
    /// <summary>
    /// Bytes downloaded so far
    /// </summary>
    public long BytesDownloaded { get; init; }
    
    /// <summary>
    /// Total bytes to download
    /// </summary>
    public long TotalBytes { get; init; }
    
    /// <summary>
    /// Percentage complete (0-100)
    /// </summary>
    public double PercentageComplete => TotalBytes > 0 ? (double)BytesDownloaded / TotalBytes * 100 : 0;
    
    /// <summary>
    /// Download speed in bytes per second
    /// </summary>
    public long BytesPerSecond { get; init; }
    
    /// <summary>
    /// Estimated time remaining
    /// </summary>
    public TimeSpan? EstimatedTimeRemaining { get; init; }
    
    /// <summary>
    /// Current download stage (downloading, extracting, verifying, etc.)
    /// </summary>
    public string Stage { get; init; } = "downloading";
    
    /// <summary>
    /// Human-readable status message
    /// </summary>
    public string StatusMessage { get; init; } = string.Empty;
}

/// <summary>
/// Result of model validation operation.
/// </summary>
public record ModelValidationResult
{
    /// <summary>
    /// Whether the model passed validation
    /// </summary>
    public bool IsValid { get; init; }
    
    /// <summary>
    /// Validation error messages if any
    /// </summary>
    public List<string> Errors { get; init; } = new();
    
    /// <summary>
    /// Validation warnings if any
    /// </summary>
    public List<string> Warnings { get; init; } = new();
    
    /// <summary>
    /// Checksum verification result
    /// </summary>
    public bool ChecksumValid { get; init; }
    
    /// <summary>
    /// Security scan result
    /// </summary>
    public bool SecurityScanPassed { get; init; }
    
    /// <summary>
    /// Model format validation result
    /// </summary>
    public bool FormatValid { get; init; }
}