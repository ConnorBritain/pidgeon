// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Segmint.Core.Standards.Common;

/// <summary>
/// Universal configuration interface for all healthcare standards.
/// Provides consistent configuration management across HL7, FHIR, NCPDP, and other standards.
/// </summary>
public interface IStandardConfig
{
    /// <summary>
    /// Gets the healthcare standard this configuration applies to.
    /// </summary>
    string StandardName { get; }

    /// <summary>
    /// Gets the version of the healthcare standard.
    /// </summary>
    string StandardVersion { get; }

    /// <summary>
    /// Gets or sets the configuration name or identifier.
    /// </summary>
    string Name { get; set; }

    /// <summary>
    /// Gets or sets the configuration version for change tracking.
    /// </summary>
    string Version { get; set; }

    /// <summary>
    /// Gets or sets whether this configuration is locked to prevent accidental changes.
    /// </summary>
    bool IsLocked { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the configuration was created.
    /// </summary>
    DateTime CreatedDate { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the configuration was last modified.
    /// </summary>
    DateTime LastModified { get; set; }

    /// <summary>
    /// Gets the supported message types for this configuration.
    /// </summary>
    string[] SupportedMessageTypes { get; }

    /// <summary>
    /// Validates the configuration for completeness and correctness.
    /// </summary>
    /// <returns>Validation result indicating any configuration issues.</returns>
    ValidationResult Validate();

    /// <summary>
    /// Loads configuration settings from a JSON string.
    /// </summary>
    /// <param name="json">JSON configuration data.</param>
    void LoadFromJson(string json);

    /// <summary>
    /// Exports the configuration to a JSON string.
    /// </summary>
    /// <returns>JSON representation of the configuration.</returns>
    string ToJson();

    /// <summary>
    /// Creates a deep copy of the configuration.
    /// </summary>
    /// <returns>Cloned configuration instance.</returns>
    IStandardConfig Clone();

    /// <summary>
    /// Compares this configuration with another to identify differences.
    /// </summary>
    /// <param name="other">Configuration to compare against.</param>
    /// <returns>Configuration difference analysis.</returns>
    ConfigurationDiff CompareTo(IStandardConfig other);

    /// <summary>
    /// Merges changes from another configuration into this one.
    /// </summary>
    /// <param name="other">Configuration containing changes to merge.</param>
    /// <param name="conflictResolution">How to handle conflicting values.</param>
    /// <returns>Result of the merge operation.</returns>
    MergeResult MergeFrom(IStandardConfig other, ConflictResolution conflictResolution = ConflictResolution.PreferThis);

    /// <summary>
    /// Gets configuration metadata including field definitions and constraints.
    /// </summary>
    /// <returns>Configuration metadata for documentation and validation.</returns>
    ConfigurationMetadata GetMetadata();
}

/// <summary>
/// Represents the differences between two healthcare standard configurations.
/// </summary>
public class ConfigurationDiff
{
    /// <summary>
    /// Gets or sets the list of fields that were added in the comparison target.
    /// </summary>
    public List<ConfigurationChange> Added { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of fields that were removed in the comparison target.
    /// </summary>
    public List<ConfigurationChange> Removed { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of fields that were modified in the comparison target.
    /// </summary>
    public List<ConfigurationChange> Modified { get; set; } = new();

    /// <summary>
    /// Gets whether there are any differences between the configurations.
    /// </summary>
    public bool HasChanges => Added.Count > 0 || Removed.Count > 0 || Modified.Count > 0;

    /// <summary>
    /// Gets the total number of changes across all categories.
    /// </summary>
    public int TotalChanges => Added.Count + Removed.Count + Modified.Count;
}

/// <summary>
/// Represents a single change in a configuration comparison.
/// </summary>
public class ConfigurationChange
{
    /// <summary>
    /// Gets or sets the type of change (Added, Removed, Modified).
    /// </summary>
    public ChangeType Type { get; set; }

    /// <summary>
    /// Gets or sets the path to the field that changed.
    /// </summary>
    public string FieldPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the old value (for Modified changes).
    /// </summary>
    public object? OldValue { get; set; }

    /// <summary>
    /// Gets or sets the new value (for Added and Modified changes).
    /// </summary>
    public object? NewValue { get; set; }

    /// <summary>
    /// Gets or sets additional context about the change.
    /// </summary>
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Defines the types of changes that can occur in configuration comparisons.
/// </summary>
public enum ChangeType
{
    /// <summary>
    /// A new field or setting was added.
    /// </summary>
    Added,

    /// <summary>
    /// An existing field or setting was removed.
    /// </summary>
    Removed,

    /// <summary>
    /// An existing field or setting was modified.
    /// </summary>
    Modified
}

/// <summary>
/// Represents the result of merging two configurations.
/// </summary>
public class MergeResult
{
    /// <summary>
    /// Gets or sets whether the merge was successful.
    /// </summary>
    public bool IsSuccessful { get; set; }

    /// <summary>
    /// Gets or sets the list of conflicts that were encountered during the merge.
    /// </summary>
    public List<MergeConflict> Conflicts { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of changes that were applied during the merge.
    /// </summary>
    public List<ConfigurationChange> AppliedChanges { get; set; } = new();

    /// <summary>
    /// Gets or sets any error messages from the merge operation.
    /// </summary>
    public List<string> Errors { get; set; } = new();
}

/// <summary>
/// Represents a conflict encountered during configuration merging.
/// </summary>
public class MergeConflict
{
    /// <summary>
    /// Gets or sets the field path where the conflict occurred.
    /// </summary>
    public string FieldPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the value from the source configuration.
    /// </summary>
    public object? SourceValue { get; set; }

    /// <summary>
    /// Gets or sets the value from the target configuration.
    /// </summary>
    public object? TargetValue { get; set; }

    /// <summary>
    /// Gets or sets how the conflict was resolved.
    /// </summary>
    public ConflictResolution Resolution { get; set; }

    /// <summary>
    /// Gets or sets the final value that was chosen.
    /// </summary>
    public object? ResolvedValue { get; set; }
}

/// <summary>
/// Defines how conflicts should be resolved during configuration merging.
/// </summary>
public enum ConflictResolution
{
    /// <summary>
    /// Keep the value from this configuration (target).
    /// </summary>
    PreferThis,

    /// <summary>
    /// Take the value from the other configuration (source).
    /// </summary>
    PreferOther,

    /// <summary>
    /// Prompt the user to manually resolve conflicts.
    /// </summary>
    Manual,

    /// <summary>
    /// Fail the merge when conflicts are encountered.
    /// </summary>
    Fail
}

/// <summary>
/// Metadata about a healthcare standard configuration.
/// </summary>
public class ConfigurationMetadata
{
    /// <summary>
    /// Gets or sets the list of all configurable field paths.
    /// </summary>
    public List<string> FieldPaths { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of required field paths.
    /// </summary>
    public List<string> RequiredFields { get; set; } = new();

    /// <summary>
    /// Gets or sets the supported message types for this configuration.
    /// </summary>
    public List<string> MessageTypes { get; set; } = new();

    /// <summary>
    /// Gets or sets custom properties specific to the healthcare standard.
    /// </summary>
    public Dictionary<string, object> Properties { get; set; } = new();
}