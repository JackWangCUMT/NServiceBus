namespace NServiceBus.Transports
{
    using System;
    using System.Threading.Tasks;
    using Features;

    class Receiving : Feature
    {
        internal Receiving()
        {
            EnableByDefault();
            DependsOn<Transport>();
            Prerequisite(c => !c.Settings.GetOrDefault<bool>("Endpoint.SendOnly"), "Endpoint is configured as send-only");
            Defaults(s =>
            {
                var transport = s.Get<TransportAddresses>();
                var receiveAddress = transport.GetTransportAddress(s.RootLogicalAddress());
                s.SetDefault("NServiceBus.LocalAddress", receiveAddress);
            });
        }

        /// <summary>
        /// <see cref="Feature.Setup"/>.
        /// </summary>
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var inboundTransport = context.Settings.Get<InboundTransport>();

            context.Settings.Get<QueueBindings>().BindReceiving(context.Settings.LocalAddress());

            context.Container.RegisterSingleton(inboundTransport.Definition);

            var receiveConfigResult = inboundTransport.Configure(context.Settings);
            context.Container.ConfigureComponent(b => receiveConfigResult.MessagePumpFactory(b.Build<CriticalError>()), DependencyLifecycle.InstancePerCall);
            context.Container.ConfigureComponent(b => receiveConfigResult.QueueCreatorFactory(), DependencyLifecycle.SingleInstance);

            context.RegisterStartupTask(new PrepareForReceiving(receiveConfigResult.PreStartupCheck));
        }
        
        class PrepareForReceiving : FeatureStartupTask
        {
            readonly Func<Task<StartupCheckResult>> preStartupCheck;

            public PrepareForReceiving(Func<Task<StartupCheckResult>> preStartupCheck)
            {
                this.preStartupCheck = preStartupCheck;
            }

            protected override async Task OnStart(IBusContext context)
            {
                var result = await preStartupCheck().ConfigureAwait(false);
                if (!result.Succeeded)
                {
                    throw new Exception($"Pre start-up check failed: {result.ErrorMessage}");
                }
            }
        }
    }
}
