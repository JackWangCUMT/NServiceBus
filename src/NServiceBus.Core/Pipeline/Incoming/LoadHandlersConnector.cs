﻿namespace NServiceBus
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.Outbox;
    using NServiceBus.Persistence;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Transports;
    using NServiceBus.Unicast;

    class LoadHandlersConnector : StageConnector<IncomingLogicalMessageContext, InvokeHandlerContext>
    {
        public LoadHandlersConnector(MessageHandlerRegistry messageHandlerRegistry, ISynchronizedStorage synchronizedStorage, ISynchronizedStorageAdapter adapter)
        {
            this.messageHandlerRegistry = messageHandlerRegistry;
            this.synchronizedStorage = synchronizedStorage;
            this.adapter = adapter;
        }

        public override async Task Invoke(IncomingLogicalMessageContext context, Func<InvokeHandlerContext, Task> next)
        {
            var outboxTransaction = context.Extensions.Get<OutboxTransaction>();
            var transportTransaction = context.Extensions.Get<TransportTransaction>();
            using (var storageSession = await AdaptOrOpenNewSynchronizedStorageSession(transportTransaction, outboxTransaction, context.Extensions))
            {
                var handlersToInvoke = messageHandlerRegistry.GetHandlersFor(context.Message.MessageType).ToList();

                if (!context.MessageHandled && !handlersToInvoke.Any())
                {
                    var error = $"No handlers could be found for message type: {context.Message.MessageType}";
                    throw new InvalidOperationException(error);
                }

                foreach (var messageHandler in handlersToInvoke)
                {
                    messageHandler.Instance = context.Builder.Build(messageHandler.HandlerType);

                    var handlingContext = new InvokeHandlerContextImpl(messageHandler, storageSession, context);
                    await next(handlingContext).ConfigureAwait(false);

                    if (handlingContext.HandlerInvocationAborted)
                    {
                        //if the chain was aborted skip the other handlers
                        break;
                    }
                }
                context.MessageHandled = true;
                await storageSession.CompleteAsync().ConfigureAwait(false);
            }
        }

        async Task<CompletableSynchronizedStorageSession> AdaptOrOpenNewSynchronizedStorageSession(TransportTransaction transportTransaction, OutboxTransaction outboxTransaction, ContextBag contextBag)
        {
            CompletableSynchronizedStorageSession session;

            return await adapter.TryAdapt(outboxTransaction, contextBag, out session) || await adapter.TryAdapt(transportTransaction, contextBag, out session)
                ? session
                : await synchronizedStorage.OpenSession(contextBag);
        }


        MessageHandlerRegistry messageHandlerRegistry;
        readonly ISynchronizedStorage synchronizedStorage;
        readonly ISynchronizedStorageAdapter adapter;
    }
}