namespace NServiceBus.Routing.MessageDrivenSubscriptions
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NServiceBus.Transports;

    class SubscriptionRouter
    {
        public SubscriptionRouter(Publishers publishers, EndpointInstances endpointInstances, TransportAddresses physicalAddresses)
        {
            this.publishers = publishers;
            this.endpointInstances = endpointInstances;
            this.physicalAddresses = physicalAddresses;
        }

        public async Task<IEnumerable<string>> GetAddressesForEventType(Type messageType)
        {
            var results = new List<string>();
            foreach (var publisherAddress in publishers.GetPublisherFor(messageType))
            {
                results.AddRange(await publisherAddress.Resolve(
                    async e => await endpointInstances.FindInstances(e).ConfigureAwait(false), 
                    i => physicalAddresses.GetTransportAddress(i)));
            }
            return results;
        }

        Publishers publishers;
        EndpointInstances endpointInstances;
        TransportAddresses physicalAddresses;
    }
}