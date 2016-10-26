// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Converters;
using Microsoft.Azure.WebJobs.Host.Triggers;
using Microsoft.ServiceBus.Messaging;

namespace Microsoft.Azure.WebJobs.ServiceBus.Triggers
{
    internal class SessionConverterArgumentBindingProvider<T> : ISessionQueueTriggerArgumentBindingProvider
    {
        private readonly IAsyncConverter<BrokeredMessage, T> _converter;

        public SessionConverterArgumentBindingProvider(IAsyncConverter<BrokeredMessage, T> converter)
        {
            _converter = converter;
        }

        public ITriggerDataArgumentBinding<BrokeredMessageSession> TryCreate(ParameterInfo parameter)
        {
            if (parameter.ParameterType != typeof(T))
            {
                return null;
            }

            return new ConverterArgumentBinding(_converter);
        }

        internal class ConverterArgumentBinding : ITriggerDataArgumentBinding<BrokeredMessageSession>
        {
            private readonly IAsyncConverter<BrokeredMessage, T> _converter;

            private readonly ReadOnlyDictionary<string, Type> _contract = new ReadOnlyDictionary<string, Type>(new Dictionary<string, Type>() { { "session", typeof(MessageSession) } });

            public ConverterArgumentBinding(IAsyncConverter<BrokeredMessage, T> converter)
            {
                _converter = converter;
            }

            public Type ValueType
            {
                get { return typeof(T); }
            }

            public IReadOnlyDictionary<string, Type> BindingDataContract
            {
                get { return _contract; } 
            }

            public async Task<ITriggerData> BindAsync(BrokeredMessageSession value, ValueBindingContext context)
            {
                BrokeredMessage clone = value.Message.Clone();
                object converted = await _converter.ConvertAsync(value.Message, context.CancellationToken);
                IValueProvider provider = await BrokeredMessageValueProvider.CreateAsync(clone, converted, typeof(T),
                    context.CancellationToken);
                Dictionary<string, object> data = new Dictionary<string, object>();
                data.Add("session", value.Session);
                return new TriggerData(provider, data);
            }
        }
    }
}
