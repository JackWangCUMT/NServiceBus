﻿namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using DeliveryConstraints;
    using NServiceBus.Logging;
    using NServiceBus.Routing;
    using Pipeline;
    using TransportDispatch;
    using Transports;

    class RoutingToDispatchConnector : StageConnector<IRoutingContext, IDispatchContext>
    {
        public async override Task Invoke(IRoutingContext context, Func<IDispatchContext, Task> next)
        {
            var state = context.Extensions.GetOrCreate<State>();
            var dispatchConsistency = state.ImmediateDispatch ? DispatchConsistency.Isolated : DispatchConsistency.Default;

            var operations = context.RoutingStrategies
                .Select(rs =>
                {
                    var headers = new Dictionary<string, string>(context.Message.Headers);
                    var addressLabel = rs.Apply(headers);
                    var message = new OutgoingMessage(context.Message.MessageId, context.Message.Headers, context.Message.Body);
                    return new TransportOperation(message, new DispatchOptions(addressLabel, dispatchConsistency, context.GetDeliveryConstraints()));
                });

            if (log.IsDebugEnabled)
            {
                var sb = new StringBuilder();
                foreach (var operation in operations)
                {
                    var unicastAddressTag = operation.DispatchOptions.AddressTag as UnicastAddressTag;
                    if (unicastAddressTag != null)
                    {
                        sb.AppendFormat("Destination: {0}\n", unicastAddressTag.Destination);
                    }

                    sb.AppendFormat("Message headers:\n{0}", string.Join(", ", operation.Message.Headers.Select(h => h.Key + ":" + h.Value).ToArray()));
                    log.Debug(sb.ToString());
                }
            }

            PendingTransportOperations pendingOperations;

            if (!state.ImmediateDispatch && context.Extensions.TryGet(out pendingOperations))
            {
                pendingOperations.AddRange(operations);
                return;
            }

            await next(new DispatchContext(operations.ToArray(), context)).ConfigureAwait(false);
        }

        public class State
        {
            public bool ImmediateDispatch { get; set; }
        }

        static ILog log = LogManager.GetLogger<RoutingToDispatchConnector>();
    }
}