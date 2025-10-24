// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System.Text.Json.Serialization;

namespace Pidgeon.Core.Domain.Configuration.Entities;

/// <summary>
/// Represents field cardinality constraints in healthcare message patterns.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Cardinality
{
    /// <summary>
    /// Field is optional (0..1 or 0..n).
    /// </summary>
    Optional,

    /// <summary>
    /// Field is required (1..1 or 1..n).
    /// </summary>
    Required,

    /// <summary>
    /// Field may repeat (0..n or 1..n).
    /// </summary>
    Repeating,

    /// <summary>
    /// Field is conditionally required based on other field values.
    /// </summary>
    Conditional
}