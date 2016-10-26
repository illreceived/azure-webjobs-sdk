// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.ServiceBus.Messaging;

namespace Microsoft.Azure.WebJobs.ServiceBus.Triggers
{
    internal class BrokeredMessageSession
    {
        public BrokeredMessage Message { get; set; }

        public MessageSession Session { get; set; }
    }
}
