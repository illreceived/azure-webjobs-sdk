// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.Host.Converters;
using Microsoft.Azure.WebJobs.Host.Triggers;
using Microsoft.ServiceBus.Messaging;

namespace Microsoft.Azure.WebJobs.ServiceBus.Triggers
{
    internal class ServiceBusSessionTriggerAttributeBindingProvider : ITriggerBindingProvider
    {
        private static readonly ISessionQueueTriggerArgumentBindingProvider InnerProvider =
            new SessionCompositeArgumentBindingProvider(
                new SessionConverterArgumentBindingProvider<BrokeredMessage>(
                    new AsyncConverter<BrokeredMessage, BrokeredMessage>(new IdentityConverter<BrokeredMessage>())),
                    new SessionConverterArgumentBindingProvider<string>(new BrokeredMessageToStringConverter()),
                new SessionConverterArgumentBindingProvider<byte[]>(new BrokeredMessageToByteArrayConverter()),
                new SessionUserTypeArgumentBindingProvider()); // Must come last, because it will attempt to bind all types.

        private readonly INameResolver _nameResolver;
        private readonly ServiceBusConfiguration _config;

        public ServiceBusSessionTriggerAttributeBindingProvider(INameResolver nameResolver, ServiceBusConfiguration config)
        {
            if (nameResolver == null)
            {
                throw new ArgumentNullException("nameResolver");
            }
            if (config == null)
            {
                throw new ArgumentNullException("config");
            }

            _nameResolver = nameResolver;
            _config = config;
        }

        public Task<ITriggerBinding> TryCreateAsync(TriggerBindingProviderContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            ParameterInfo parameter = context.Parameter;
            var attribute = TypeUtility.GetResolvedAttribute<ServiceBusSessionTriggerAttribute>(parameter);

            if (attribute == null)
            {
                return Task.FromResult<ITriggerBinding>(null);
            }

            string queueName = null;
            string topicName = null;
            string subscriptionName = null;
            string entityPath = null;

            if (attribute.QueueName != null)
            {
                queueName = Resolve(attribute.QueueName);
                entityPath = queueName;
            }
            else
            {
                topicName = Resolve(attribute.TopicName);
                subscriptionName = Resolve(attribute.SubscriptionName);
                entityPath = SubscriptionClient.FormatSubscriptionPath(topicName, subscriptionName);
            }

            ITriggerDataArgumentBinding<BrokeredMessageSession> argumentBinding = InnerProvider.TryCreate(parameter);
            if (argumentBinding == null)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, "Can't bind ServiceBusTrigger to type '{0}'.", parameter.ParameterType));
            }

            ServiceBusAccount account = new ServiceBusAccount
            {
                MessagingFactory = _config.MessagingProvider.CreateMessagingFactory(entityPath, attribute.Connection),
                NamespaceManager = _config.MessagingProvider.CreateNamespaceManager(attribute.Connection)
            };

            ITriggerBinding binding;
            if (queueName != null)
            {
                binding = new ServiceBusSessionTriggerBinding(parameter.Name, parameter.ParameterType, argumentBinding, account, queueName, attribute.Access, _config);
            }
            else
            {
                binding = new ServiceBusSessionTriggerBinding(parameter.Name, argumentBinding, account, topicName, subscriptionName, attribute.Access, _config);
            }

            return Task.FromResult<ITriggerBinding>(binding);
        }

        private string Resolve(string queueName)
        {
            if (_nameResolver == null)
            {
                return queueName;
            }

            return _nameResolver.ResolveWholeString(queueName);
        }
    }
}
