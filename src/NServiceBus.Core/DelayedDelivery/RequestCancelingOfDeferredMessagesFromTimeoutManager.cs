﻿namespace NServiceBus
{
    using System.Threading.Tasks;
    using NServiceBus.Pipeline;
    using NServiceBus.Routing;
    using NServiceBus.TransportDispatch;
    using NServiceBus.Transports;
    using NServiceBus.Unicast.Transport;

    class RequestCancelingOfDeferredMessagesFromTimeoutManager : ICancelDeferredMessages
    {

        public RequestCancelingOfDeferredMessagesFromTimeoutManager(string timeoutManagerAddress, IPipelineBase<IRoutingContext> dispatchPipeline)
        {
            this.timeoutManagerAddress = timeoutManagerAddress;
            this.dispatchPipeline = dispatchPipeline;
        }

        public Task CancelDeferredMessages(string messageKey, IBehaviorContext context)
        {
            var controlMessage = ControlMessageFactory.Create(MessageIntentEnum.Send);

            controlMessage.Headers[Headers.SagaId] = messageKey;
            controlMessage.Headers[TimeoutManagerHeaders.ClearTimeouts] = bool.TrueString;

            var dispatchContext = new RoutingContext(controlMessage, new UnicastRoutingStrategy(timeoutManagerAddress), context);
            
            return dispatchPipeline.Invoke(dispatchContext);
        }

        string timeoutManagerAddress;
        IPipelineBase<IRoutingContext> dispatchPipeline;
    }
}