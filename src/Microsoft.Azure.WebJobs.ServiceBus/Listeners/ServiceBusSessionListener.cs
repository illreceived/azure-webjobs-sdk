// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Microsoft.Azure.WebJobs.ServiceBus.Triggers;
using Microsoft.ServiceBus.Messaging;

namespace Microsoft.Azure.WebJobs.ServiceBus.Listeners
{
    internal sealed class ServiceBusSessionListener : IListener
    {
        private readonly MessagingProvider _messagingProvider;
        private readonly MessagingFactory _messagingFactory;
        private readonly string _entityPath;
        private readonly ServiceBusSessionTriggerExecutor _triggerExecutor;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly MessageProcessor _messageProcessor;
        private bool _disposed;

        public ServiceBusSessionListener(MessagingFactory messagingFactory, string entityPath, ServiceBusSessionTriggerExecutor triggerExecutor, ServiceBusConfiguration config)
        {
            _messagingFactory = messagingFactory;
            _entityPath = entityPath;
            _triggerExecutor = triggerExecutor;
            _cancellationTokenSource = new CancellationTokenSource();
            _messagingProvider = config.MessagingProvider;
            _messageProcessor = config.MessagingProvider.CreateMessageProcessor(entityPath);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            return StartAsyncCore(cancellationToken);
        }

        private Task StartAsyncCore(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            IMessageSessionAsyncHandlerFactory sessionHandlerFactory = new SessionHandlerFactory(_cancellationTokenSource.Token, _messageProcessor, _triggerExecutor);
            _messagingProvider.RegisterSessionHandlerFactory(_messagingFactory, _entityPath, sessionHandlerFactory);

            return Task.FromResult(0);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            // Signal ProcessMessage to shut down gracefully
            _cancellationTokenSource.Cancel();

            return Task.FromResult(0);
        }

        public void Cancel()
        {
            ThrowIfDisposed();
            _cancellationTokenSource.Cancel();
        }

        [SuppressMessage("Microsoft.Usage", "CA2213:DisposableFieldsShouldBeDisposed", MessageId = "_cancellationTokenSource")]
        public void Dispose()
        {
            if (!_disposed)
            {
                // Running callers might still be using the cancellation token.
                // Mark it canceled but don't dispose of the source while the callers are running.
                // Otherwise, callers would receive ObjectDisposedException when calling token.Register.
                // For now, rely on finalization to clean up _cancellationTokenSource's wait handle (if allocated).
                _cancellationTokenSource.Cancel();

                _disposed = true;
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(null);
            }
        }

        internal class SessionHandler : IMessageSessionAsyncHandler
        {
            private readonly CancellationToken _cancellationToken;

            private readonly MessageProcessor _messageProcessor;

            private readonly ServiceBusSessionTriggerExecutor _triggerExecutor;

            public SessionHandler(CancellationToken token, MessageProcessor messageProcessor, ServiceBusSessionTriggerExecutor triggerExecutor)
            {
                _messageProcessor = messageProcessor;
                _cancellationToken = token;
                _triggerExecutor = triggerExecutor;
            }

            public async Task OnMessageAsync(MessageSession session, BrokeredMessage message)
            {
                if (!await _messageProcessor.BeginProcessingMessageAsync(message, _cancellationToken))
                {
                    return;
                }

                var wrappedMessage = new BrokeredMessageSession() { Message = message, Session = session };

                FunctionResult result = await _triggerExecutor.ExecuteAsync(wrappedMessage, _cancellationToken);

                await _messageProcessor.CompleteProcessingMessageAsync(wrappedMessage.Message, result, _cancellationToken);
            }

            public Task OnCloseSessionAsync(MessageSession session)
            {
                return Task.FromResult(0);
            }

            public Task OnSessionLostAsync(Exception exception)
            {
                return Task.FromResult(0);
            }
        }

        internal class SessionHandlerFactory : IMessageSessionAsyncHandlerFactory
        {
            private readonly CancellationToken _cancellationToken;

            private readonly MessageProcessor _messageProcessor;

            private readonly ServiceBusSessionTriggerExecutor _triggerExecutor;

            public SessionHandlerFactory(CancellationToken token, MessageProcessor messageProcessor, ServiceBusSessionTriggerExecutor triggerExecutor)
            {
                _messageProcessor = messageProcessor;
                _cancellationToken = token;
                _triggerExecutor = triggerExecutor;
            }

            public IMessageSessionAsyncHandler CreateInstance(MessageSession session, BrokeredMessage message)
            {
                if (session != null)
                {
                    Console.WriteLine("Created new Session Handler for session {0}",
                        session.SessionId.ToString(CultureInfo.InvariantCulture));
                }
                return new SessionHandler(_cancellationToken, _messageProcessor, _triggerExecutor);
            }

            public void DisposeInstance(IMessageSessionAsyncHandler handler)
            {
            }
        }
    }
}
