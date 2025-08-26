// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Pidgeon.Core.Types;

namespace Pidgeon.Core.Services;

internal class TransformationService : ITransformationService
{
    public Task<Result<string>> TransformAsync(string sourceMessage, string sourceStandard, string targetStandard, TransformationOptions? options = null)
    {
        throw new NotImplementedException("TransformationService implementation pending");
    }
}