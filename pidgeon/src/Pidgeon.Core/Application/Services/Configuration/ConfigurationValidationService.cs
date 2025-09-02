// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Pidgeon.Core.Application.Interfaces.Standards;
using Pidgeon.Core.Domain.Configuration.Entities;

namespace Pidgeon.Core.Domain.Configuration.Services;

internal class ConfigurationValidationService : IConfigurationValidationService
{
    public Task<Result<ConfigurationValidationResult>> ValidateConfigurationAsync(IStandardConfig configuration)
    {
        throw new NotImplementedException("ConfigurationValidationService implementation pending");
    }
}