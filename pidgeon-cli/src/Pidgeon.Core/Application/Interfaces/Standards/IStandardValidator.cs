// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Pidgeon.Core;
using Pidgeon.Core.Application.Common;

namespace Pidgeon.Core.Application.Interfaces.Standards;

/// <summary>
/// Defines the contract for validators that can validate messages
/// according to healthcare standard specifications.
/// </summary>
public interface IStandardValidator
{
    /// <summary>
    /// Gets the standard this validator supports.
    /// </summary>
    string StandardName { get; }

    /// <summary>
    /// Gets the version of the standard supported.
    /// </summary>
    Version StandardVersion { get; }

    /// <summary>
    /// Validates a message string according to the standard.
    /// </summary>
    /// <param name="messageContent">The message content to validate</param>
    /// <param name="validationMode">The validation mode to use</param>
    /// <returns>A result containing validation results</returns>
    Result<ValidationResult> ValidateMessage(string messageContent, ValidationMode validationMode = ValidationMode.Strict);

    /// <summary>
    /// Validates a structured message object.
    /// </summary>
    /// <param name="message">The message object to validate</param>
    /// <param name="validationMode">The validation mode to use</param>
    /// <returns>A result containing validation results</returns>
    Result<ValidationResult> ValidateMessage(IStandardMessage message, ValidationMode validationMode = ValidationMode.Strict);

    /// <summary>
    /// Gets available validation rules for this standard.
    /// </summary>
    /// <returns>A list of validation rule information</returns>
    IReadOnlyList<ValidationRuleInfo> GetValidationRules();
}