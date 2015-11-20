namespace NServiceBus.Features
{
    using NServiceBus.Faults;
    using NServiceBus.Hosting;
    using NServiceBus.Pipeline;
    using NServiceBus.TransportDispatch;
    using NServiceBus.Transports;

    class StoreFaultsInErrorQueue : Feature
    {
        internal StoreFaultsInErrorQueue()
        {
            EnableByDefault();
            Prerequisite(context =>
            {
                var b = !context.Settings.GetOrDefault<bool>("Endpoint.SendOnly");
                return b;
            }, "Send only endpoints can't be used to forward received messages to the error queue as the endpoint requires receive capabilities");
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {

            var errorQueue = ErrorQueueSettings.GetConfiguredErrorQueue(context.Settings);

            context.Container.ConfigureComponent(b =>
            {
                var routingPipeline = b.Build<IPipeInlet<RoutingContext>>();

                return new MoveFaultsToErrorQueueBehavior(
                    b.Build<CriticalError>(),
                    routingPipeline,
                    b.Build<HostInformation>(),
                    b.Build<BusNotifications>(),
                    errorQueue);
            }, DependencyLifecycle.InstancePerCall);

            context.Settings.Get<QueueBindings>().BindSending(errorQueue);

            context.Pipeline.Register<MoveFaultsToErrorQueueBehavior.Registration>();
        }


    }
}