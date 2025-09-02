// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Pidgeon.Core.Application.Interfaces.Standards;
using Pidgeon.Core.Domain.Configuration.Entities;

namespace Pidgeon.Core.Application.Services.Configuration;

/// <summary>
/// Configuration validation service.
/// </summary>
public interface IConfigurationValidationService
{
    /// <summary>
    /// Validates a configuration against known patterns and best practices.
    /// </summary>
    /// <param name="configuration">The configuration to validate</param>
    /// <returns>A result containing validation results</returns>
    Task<Result<ConfigurationValidationResult>> ValidateConfigurationAsync(IStandardConfig configuration);
}