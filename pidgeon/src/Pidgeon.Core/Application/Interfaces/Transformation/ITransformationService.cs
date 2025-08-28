// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Pidgeon.Core.Domain.Configuration.Entities;
using Pidgeon.Core.Domain.Transformation.Entities;

namespace Pidgeon.Core.Services;

/// <summary>
/// Message transformation service for converting between standards.
/// </summary>
public interface ITransformationService
{
    /// <summary>
    /// Transforms a message from one standard to another.
    /// </summary>
    /// <param name="sourceMessage">The source message</param>
    /// <param name="sourceStandard">The source standard</param>
    /// <param name="targetStandard">The target standard</param>
    /// <param name="options">Transformation options</param>
    /// <returns>A result containing the transformed message or an error</returns>
    Task<Result<string>> TransformAsync(string sourceMessage, string sourceStandard, string targetStandard, TransformationOptions? options = null);
}