// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Pidgeon.Core.Standards.Common;

namespace Pidgeon.Core.Services;

/// <summary>
/// Message validation service.
/// </summary>
public interface IValidationService
{
    /// <summary>
    /// Validates a message according to its standard.
    /// </summary>
    /// <param name="messageContent">The message to validate</param>
    /// <param name="validationMode">The validation mode</param>
    /// <param name="standard">The specific standard to validate against (optional)</param>
    /// <returns>A result containing validation results</returns>
    Task<Result<ValidationResult>> ValidateAsync(string messageContent, ValidationMode validationMode = ValidationMode.Strict, string? standard = null);
}