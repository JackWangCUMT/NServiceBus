namespace NServiceBus.Features
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.Logging;
    using NServiceBus.Settings;
    using NServiceBus.Transports;
    using NServiceBus.Unicast.Messages;

    class UnicastBus : Feature
    {
        internal UnicastBus()
        {
            EnableByDefault();
            Defaults(s =>
            {
                var endpointInstanceName = GetEndpointInstanceName(s);
                var rootLogicalAddress = new LogicalAddress(endpointInstanceName);
                s.SetDefault<EndpointInstance>(endpointInstanceName);
                s.SetDefault<LogicalAddress>(rootLogicalAddress);
            });
        }

        static EndpointInstance GetEndpointInstanceName(ReadOnlySettings settings)
        {
            var userDiscriminator = settings.GetOrDefault<string>("EndpointInstanceDiscriminator");
            var scaleOut = settings.GetOrDefault<bool>("IndividualizeEndpointAddress");
            var transportDiscriminator = settings.Get<TransportDefinition>().GetDiscriminatorForThisEndpointInstance(settings);
            if (scaleOut && userDiscriminator == null && transportDiscriminator == null)
            {
                throw new Exception("No endpoint instance discriminator found. This value is usually provided by your transport so please make sure you're on the lastest version of your specific transport or set the discriminator using 'configuration.ScaleOut().UniqueQueuePerEndpointInstance(myDiscriminator)'");
            }
            return new EndpointInstance(settings.EndpointName(), userDiscriminator, transportDiscriminator);
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent<BusNotifications>(DependencyLifecycle.SingleInstance);

            var knownMessages = context.Settings.GetAvailableTypes()
                .Where(context.Settings.Get<Conventions>().IsMessageType)
                .ToList();

            ConfigureMessageRegistry(context, knownMessages);
        }

        static void ConfigureMessageRegistry(FeatureConfigurationContext context, IEnumerable<Type> knownMessages)
        {
            var messageRegistry = new MessageMetadataRegistry(context.Settings.Get<Conventions>());

            foreach (var msg in knownMessages)
            {
                messageRegistry.RegisterMessageType(msg);
            }

            context.Container.RegisterSingleton(messageRegistry);
            context.Container.ConfigureComponent<LogicalMessageFactory>(DependencyLifecycle.SingleInstance);

            if (!Logger.IsInfoEnabled)
            {
                return;
            }

            var messageDefinitions = messageRegistry.GetAllMessages().ToList();

            Logger.DebugFormat("Number of messages found: {0}", messageDefinitions.Count());

            if (!Logger.IsDebugEnabled)
            {
                return;
            }

            Logger.DebugFormat("Message definitions: \n {0}",
                string.Concat(messageDefinitions.Select(md => md.ToString() + "\n")));
        }

        static ILog Logger = LogManager.GetLogger<UnicastBus>();
    }
}