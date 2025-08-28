// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Pidgeon.Core.Domain.Configuration.Entities;

namespace Pidgeon.Core.Services;

// Placeholder service implementation (will be implemented in subsequent tasks)
internal class MessageService : IMessageService
{
    public Task<Result<ProcessedMessage>> ProcessMessageAsync(string messageContent, MessageProcessingOptions? options = null)
    {
        throw new NotImplementedException("MessageService implementation pending");
    }

    public Task<Result<string>> GenerateMessageAsync(object domainObject, string standard, string messageType, Generation.GenerationOptions? options = null)
    {
        throw new NotImplementedException("MessageService implementation pending");
    }
}