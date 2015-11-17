namespace NServiceBus.Routing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.Extensibility;
    using NServiceBus.Transports;
    using NServiceBus.Unicast.Messages;

    class UnicastRouter : IUnicastRouter
    {
        EndpointInstances endpointInstances;
        TransportAddresses physicalAddresses;
        MessageMetadataRegistry messageMetadataRegistry;
        UnicastRoutingTable unicastRoutingTable;

        public UnicastRouter(MessageMetadataRegistry messageMetadataRegistry, UnicastRoutingTable unicastRoutingTable, EndpointInstances endpointInstances, TransportAddresses physicalAddresses)
        {
            this.messageMetadataRegistry = messageMetadataRegistry;
            this.unicastRoutingTable = unicastRoutingTable;
            this.endpointInstances = endpointInstances;
            this.physicalAddresses = physicalAddresses;
        }

        public IEnumerable<UnicastRoutingStrategy> Route(Type messageType, DistributionStrategy distributionStrategy, ContextBag contextBag)
        {
            var typesToRoute = messageMetadataRegistry.GetMessageMetadata(messageType)
                .MessageHierarchy
                .Distinct()
                .ToList();

            var routes = typesToRoute.SelectMany(t => unicastRoutingTable.GetDestinationsFor(t, contextBag)).Distinct().ToList();

            var destinations = routes.SelectMany(d => d.Resolve(e => endpointInstances.FindInstances(e))).Distinct();

            var destinationsByEndpoint = destinations.GroupBy(d => d.EndpointName, d => d);

            var selectedDestinations = SelectDestinationsForEachEndpoint(distributionStrategy, destinationsByEndpoint);

            return selectedDestinations.Select(destination => new UnicastRoutingStrategy(destination.Resolve(physicalAddresses.GetTransportAddress)));
        }

        static IEnumerable<UnicastRoutingTarget> SelectDestinationsForEachEndpoint(DistributionStrategy distributionStrategy, IEnumerable<IGrouping<EndpointName, UnicastRoutingTarget>> destinationsByEndpoint)
        {
            foreach (var group in destinationsByEndpoint)
            {
                Func<IEnumerable<UnicastRoutingTarget>, IEnumerable<UnicastRoutingTarget>> selector;
                if (@group.Key == null)
                {
                    selector = x => x;
                }
                else
                {
                    selector = distributionStrategy.SelectDestination;
                }
                foreach (var destination in selector(@group))
                {
                    yield return destination;
                }
            }
        }
    }
}