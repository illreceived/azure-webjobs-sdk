using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Protocols;
using Microsoft.ServiceBus.Messaging;

namespace Microsoft.Azure.WebJobs.ServiceBus.Bindings
{
    internal class MessageSessionBinding : IBinding
    {
        private readonly ParameterInfo _parameter;

        public MessageSessionBinding(ParameterInfo parameter)
        {
            _parameter = parameter;
        }

        public bool FromAttribute {
            get { return false; }
        }

        public Task<IValueProvider> BindAsync(object value, ValueBindingContext context)
        {
            if (value == null || !_parameter.ParameterType.IsAssignableFrom(value.GetType()))
            {
                throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "Unable to convert value to {0}.", _parameter.ParameterType));
            }

            IValueProvider valueProvider = new ValueBinder(value, _parameter.ParameterType);
            return Task.FromResult<IValueProvider>(valueProvider);
        }

        public Task<IValueProvider> BindAsync(BindingContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            object tracer = null;
            if (_parameter.ParameterType == typeof(MessageSession))
            {
                // bind directly to the context TraceWriter
                tracer = context.Trace;
            }

            return BindAsync(tracer, context.ValueContext);
        }

        public ParameterDescriptor ToParameterDescriptor()
        {
            throw new NotImplementedException();
        }

        private sealed class ValueBinder : IValueBinder
        {
            private readonly object _tracer;
            private readonly Type _type;

            public ValueBinder(object tracer, Type type)
            {
                _tracer = tracer;
                _type = type;
            }

            public Type Type
            {
                get { return _type; }
            }

            public object GetValue()
            {
                return _tracer;
            }

            public string ToInvokeString()
            {
                return null;
            }

            public Task SetValueAsync(object value, CancellationToken cancellationToken)
            {
                TextWriterTraceAdapter traceAdapter = value as TextWriterTraceAdapter;
                if (traceAdapter != null)
                {
                    traceAdapter.Flush();
                }
                return Task.FromResult(0);
            }
        }
    }
}
