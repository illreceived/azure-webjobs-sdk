// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Azure.WebJobs.ServiceBus.Triggers;
using Microsoft.ServiceBus.Messaging;

namespace Microsoft.Azure.WebJobs.ServiceBus.Listeners
{
    internal class ServiceBusSessionTriggerExecutor
    {
        private readonly ITriggeredFunctionExecutor _innerExecutor;

        public ServiceBusSessionTriggerExecutor(ITriggeredFunctionExecutor innerExecutor)
        {
            _innerExecutor = innerExecutor;
        }

        public async Task<FunctionResult> ExecuteAsync(BrokeredMessageSession value, CancellationToken cancellationToken)
        {
            Guid? parentId = ServiceBusCausalityHelper.GetOwner(value.Message);
            TriggeredFunctionData input = new TriggeredFunctionData
            {
                ParentId = parentId,
                TriggerValue = value
            };
            return await _innerExecutor.TryExecuteAsync(input, cancellationToken);
        }
    }
}
