namespace NServiceBus.Routing.Legacy
{
    internal static class DistributorHeaders
    {
        public const string WorkerCapacityAvailable = "NServiceBus.Distributor.WorkerCapacityAvailable";
        public const string WorkerStarting = "NServiceBus.Distributor.WorkerStarting";
        public const string UnregisterWorker = "NServiceBus.Distributor.UnregisterWorker";
        public const string WorkerSessionId = "NServiceBus.Distributor.WorkerSessionId";
    }
}