namespace NServiceBus.Pipeline.Contexts
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NServiceBus.Persistence;
    using NServiceBus.Unicast;
    using NServiceBus.Unicast.Behaviors;
    using NServiceBus.Unicast.Messages;

    class InvokeHandlerContextImpl : IncomingContextImpl, InvokeHandlerContext
    {
        internal InvokeHandlerContextImpl(MessageHandler handler, SynchronizedStorageSession storageSession, LogicalMessageProcessingContext parentContext)
            : this(handler, parentContext.MessageId, parentContext.ReplyToAddress, parentContext.MessageHeaders, parentContext.Message.Metadata, parentContext.Message.Instance, storageSession, parentContext)
        {
        }

        public InvokeHandlerContextImpl(MessageHandler handler, string messageId, string replyToAddress, IReadOnlyDictionary<string, string> headers, MessageMetadata messageMetadata, object messageBeingHandled, SynchronizedStorageSession storageSession, BehaviorContext parentContext)
            : base(messageId, replyToAddress, headers, parentContext)
        {
            MessageHandler = handler;
            MessageBeingHandled = messageBeingHandled;
            MessageMetadata = messageMetadata;
            Set(storageSession);
        }

        public MessageHandler MessageHandler { get; }

        public SynchronizedStorageSession SynchronizedStorageSession => Get<SynchronizedStorageSession>();

        public object MessageBeingHandled { get; }

        public bool HandlerInvocationAborted { get; private set; }

        public MessageMetadata MessageMetadata { get; }

        public bool HandleCurrentMessageLaterWasCalled { get; private set; }

        public async Task HandleCurrentMessageLater()
        {
            await BusOperationsInvokeHandlerContext.HandleCurrentMessageLater(this).ConfigureAwait(false);
            HandleCurrentMessageLaterWasCalled = true;
            DoNotContinueDispatchingCurrentMessageToHandlers();
        }

        public void DoNotContinueDispatchingCurrentMessageToHandlers()
        {
            HandlerInvocationAborted = true;
        }
    }
}