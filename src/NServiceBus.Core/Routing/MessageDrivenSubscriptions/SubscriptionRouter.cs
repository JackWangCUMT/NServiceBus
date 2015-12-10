﻿namespace NServiceBus.Routing.MessageDrivenSubscriptions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.Transports;

    class SubscriptionRouter
    {
        public SubscriptionRouter(Publishers publishers, EndpointInstances endpointInstances, TransportAddresses physicalAddresses)
        {
            this.publishers = publishers;
            this.endpointInstances = endpointInstances;
            this.physicalAddresses = physicalAddresses;
        }

        public IEnumerable<string> GetAddressesForEventType(Type messageType)
        {
            return publishers
                .GetPublisherFor(messageType).SelectMany(p => p
                    .Resolve(e => endpointInstances.FindInstances(e), i => physicalAddresses.GetTransportAddress(new LogicalAddress(i))));
        }

        Publishers publishers;
        EndpointInstances endpointInstances;
        TransportAddresses physicalAddresses;
    }
}