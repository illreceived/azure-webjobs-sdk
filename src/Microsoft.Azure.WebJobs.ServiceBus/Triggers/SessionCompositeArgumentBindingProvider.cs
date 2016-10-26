// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Triggers;

namespace Microsoft.Azure.WebJobs.ServiceBus.Triggers
{
    internal class SessionCompositeArgumentBindingProvider : ISessionQueueTriggerArgumentBindingProvider
    {
        private readonly IEnumerable<ISessionQueueTriggerArgumentBindingProvider> _providers;

        public SessionCompositeArgumentBindingProvider(params ISessionQueueTriggerArgumentBindingProvider[] providers)
        {
            _providers = providers;
        }

        public ITriggerDataArgumentBinding<BrokeredMessageSession> TryCreate(ParameterInfo parameter)
        {
            foreach (ISessionQueueTriggerArgumentBindingProvider provider in _providers)
            {
                ITriggerDataArgumentBinding<BrokeredMessageSession> binding = provider.TryCreate(parameter);

                if (binding != null)
                {
                    return binding;
                }
            }

            return null;
        }
    }
}
