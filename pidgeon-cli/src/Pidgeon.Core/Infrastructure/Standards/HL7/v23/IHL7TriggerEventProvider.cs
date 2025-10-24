// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Pidgeon.Core.Common;

namespace Pidgeon.Core.Infrastructure.Standards.HL7.v23;

/// <summary>
/// Provides access to HL7 v2.3 trigger event definitions from embedded JSON resources.
/// </summary>
public interface IHL7TriggerEventProvider
{
    /// <summary>
    /// Gets a trigger event definition by code (e.g., "ADT_A01", "ORM_O01").
    /// </summary>
    /// <param name="triggerEventCode">Trigger event code</param>
    /// <returns>Trigger event definition or error</returns>
    Task<Result<TriggerEvent>> GetTriggerEventAsync(string triggerEventCode);

    /// <summary>
    /// Gets all available trigger event codes.
    /// </summary>
    /// <returns>List of trigger event codes</returns>
    Task<Result<IReadOnlyList<string>>> GetAvailableTriggerEventsAsync();
}

/// <summary>
/// Represents an HL7 trigger event definition with segments and their rules.
/// </summary>
public record TriggerEvent(
    string Code,
    string Name,
    string Version,
    string Chapter,
    string Description,
    IReadOnlyList<TriggerEventSegment> Segments,
    int SegmentCount);

/// <summary>
/// Represents a segment definition within a trigger event.
/// </summary>
public record TriggerEventSegment(
    string SegmentCode,
    string SegmentDesc,
    string Optionality,
    string Repeatability,
    bool IsGroup,
    IReadOnlyList<string> GroupPath,
    int Level,
    int OrderIndex);