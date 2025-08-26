// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Pidgeon.Core.Types;

namespace Pidgeon.Core.Services.Configuration;

internal class ConfigurationInferenceService : IConfigurationInferenceService
{
    public Task<Result<VendorConfiguration>> InferConfigurationAsync(
        IEnumerable<string> sampleMessages, 
        ConfigurationAddress address, 
        InferenceOptions? options = null)
    {
        throw new NotImplementedException("ConfigurationInferenceService implementation pending - will be implemented with plugin architecture");
    }
}