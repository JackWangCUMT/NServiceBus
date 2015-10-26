namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Messaging;
    using System.Security;
    using System.Text;
    using System.Transactions;
    using NServiceBus.Features;
    using NServiceBus.Logging;
    using NServiceBus.Performance.TimeToBeReceived;
    using NServiceBus.Settings;
    using NServiceBus.Support;
    using NServiceBus.Transports;
    using NServiceBus.Transports.Msmq;
    using NServiceBus.Transports.Msmq.Config;
    using NServiceBus.Utils;
    using TransactionSettings = NServiceBus.Unicast.Transport.TransactionSettings;

    /// <summary>
    /// Transport definition for MSMQ.
    /// </summary>
    public class MsmqTransport : TransportDefinition
    {
        /// <summary>
        /// Initializes a new instance of <see cref="MsmqTransport"/>.
        /// </summary>
        public MsmqTransport()
        {
            RequireOutboxConsent = true;
        }

        /// <summary>
        /// Configures transport for receiving.
        /// </summary>
        protected internal override void ConfigureForReceiving(TransportReceivingConfigurationContext context)
        {
            new CheckMachineNameForComplianceWithDtcLimitation().Check();

            var settings = context.ConnectionString != null
                ? new MsmqConnectionStringBuilder(context.ConnectionString).RetrieveSettings()
                : new MsmqSettings();

            context.SetQueueCreatorFactory(() => new QueueCreator(settings));

            var transactionSettings = new TransactionSettings(context.Settings);
            var transactionOptions = new TransactionOptions
            {
                IsolationLevel = transactionSettings.IsolationLevel,
                Timeout = transactionSettings.TransactionTimeout
            };
            context.SetMessagePumpFactory(c => new MessagePump(c, guarantee => SelectReceiveStrategy(guarantee, transactionOptions)));
        }

        ReceiveStrategy SelectReceiveStrategy(TransactionSupport minimumConsistencyGuarantee, TransactionOptions transactionOptions)
        {
            if (minimumConsistencyGuarantee == TransactionSupport.Distributed)
            {
                return new ReceiveWithTransactionScope(transactionOptions);
            }

            if (minimumConsistencyGuarantee == TransactionSupport.None)
            {
                return new ReceiveWithNoTransaction();
            }

            return new ReceiveWithNativeTransaction();
        }

        /// <summary>
        /// Configures transport for sending.
        /// </summary>
        protected internal override void ConfigureForSending(TransportSendingConfigurationContext context)
        {
            new CheckMachineNameForComplianceWithDtcLimitation().Check();

            Func<IReadOnlyDictionary<string, string>, string> getMessageLabel;
            context.GlobalSettings.TryGet("Msmq.GetMessageLabel", out getMessageLabel);
            var settings = new MsmqConnectionStringBuilder(context.ConnectionString).RetrieveSettings();

            MsmqLabelGenerator messageLabelGenerator;
            context.ExtensionSettings.TryGet(out messageLabelGenerator);
            context.SetDispatcherFactory(() => new MsmqMessageSender(settings, messageLabelGenerator));
        }

        /// <summary>
        /// Performs start-up checks specific to a given transport (e.g. checking queue permissions).
        /// </summary>
        protected internal override void PerformStartUpChecks(TransportStartUpCheckContext context)
        {
            var boundQueueAddresses = context.QueueBindings.ReceivingAddresses.Concat(context.QueueBindings.SendingAddresses);

            foreach (var address in boundQueueAddresses)
            {
                CheckQueue(address);
            }
        }

        static void CheckQueue(string address)
        {
            var msmqAddress = MsmqAddress.Parse(address);
            var queuePath = msmqAddress.PathWithoutPrefix;

            if (MessageQueue.Exists(queuePath))
            {
                using (var messageQueue = new MessageQueue(queuePath))
                {
                    WarnIfPublicAccess(messageQueue);
                }
            }
        }

        static void WarnIfPublicAccess(MessageQueue queue)
        {
            MessageQueueAccessRights? everyoneRights, anonymousRights;
            var logger = LogManager.GetLogger<MsmqTransport>();
            try
            {
                queue.TryGetPermissions(QueueCreator.LocalAnonymousLogonName, out anonymousRights);
                queue.TryGetPermissions(QueueCreator.LocalEveryoneGroupName, out everyoneRights);
            }
            catch (SecurityException se)
            {
                logger.Warn($"Unable to read permissions for queue [{queue.QueueName}]. Make sure you have administrative access on the target machine", se);
                return;
            }

            if (anonymousRights.HasValue && everyoneRights.HasValue)
            {
                var logMessage = $"Queue [{queue.QueueName}] is running with [{QueueCreator.LocalEveryoneGroupName}] and [{QueueCreator.LocalAnonymousLogonName}] permissions. Consider setting appropriate permissions, if required by your organization. For more information, please consult the documentation.";

                if (Debugger.IsAttached)
                {
                    logger.Info(logMessage);
                }
                else
                {
                    logger.Warn(logMessage);
                }
            }
        }

        /// <summary>
        /// The list of constraints supported by the MSMQ transport.
        /// </summary>
        public override IEnumerable<Type> GetSupportedDeliveryConstraints()
        {
            return new[]
            {
                typeof(DiscardIfNotReceivedBefore)
            };
        }

        /// <summary>
        /// Gets the supported transactionallity for this transport.
        /// </summary>
        public override TransactionSupport GetTransactionSupport()
        {
            return TransactionSupport.Distributed;
        }

        /// <summary>
        /// Not used by the msmq transport.
        /// </summary>
        public override IManageSubscriptions GetSubscriptionManager()
        {
            throw new NotSupportedException("Msmq don't support native pub sub");
        }

        /// <summary>
        /// Returns the discriminator for this endpoint instance.
        /// </summary>
        public override string GetDiscriminatorForThisEndpointInstance()
        {
            return RuntimeEnvironment.MachineName;
        }
       
        /// <summary>
        /// Converts a given logical address to the transport address.
        /// </summary>
        /// <param name="logicalAddress">The logical address.</param>
        /// <returns>The transport address.</returns>
        public override string ToTransportAddress(LogicalAddress logicalAddress)
        {
            var machine = logicalAddress.EndpointInstanceName.TransportDiscriminator ?? RuntimeEnvironment.MachineName;

            var queue = new StringBuilder(logicalAddress.EndpointInstanceName.EndpointName.ToString());
            if (logicalAddress.EndpointInstanceName.UserDiscriminator != null)
            {
                queue.Append("-" + logicalAddress.EndpointInstanceName.UserDiscriminator);
            }
            if (logicalAddress.Qualifier != null)
            {
                queue.Append("." + logicalAddress.Qualifier);
            }
            return queue + "@" + machine;
        }

        /// <summary>
        /// Returns the outbound routing policy selected for the transport.
        /// </summary>
        public override OutboundRoutingPolicy GetOutboundRoutingPolicy(ReadOnlySettings settings)
        {
            return new OutboundRoutingPolicy(OutboundRoutingType.DirectSend, OutboundRoutingType.DirectSend, OutboundRoutingType.DirectSend);
        }

        /// <summary>
        /// Gets an example connection string to use when reporting lack of configured connection string to the user.
        /// </summary>
        public override string ExampleConnectionStringForErrorMessage => "cacheSendConnection=true;journal=false;deadLetter=true";

        /// <summary>
        /// Used by implementations to control if a connection string is necessary.
        /// </summary>
        public override bool RequiresConnectionString => false;
    }
}