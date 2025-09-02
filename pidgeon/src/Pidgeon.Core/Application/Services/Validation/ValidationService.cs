// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Pidgeon.Core;
using Pidgeon.Core.Application.Interfaces.Validation;
using Pidgeon.Core.Application.Common;

namespace Pidgeon.Core.Application.Services.Validation;

internal class ValidationService : IValidationService
{
    public Task<Result<ValidationResult>> ValidateAsync(string messageContent, ValidationMode validationMode = ValidationMode.Strict, string? standard = null)
    {
        throw new NotImplementedException("ValidationService implementation pending");
    }
}