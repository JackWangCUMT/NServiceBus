namespace NServiceBus.Features
{
    using System.Threading.Tasks;
    using NServiceBus.DelayedDelivery.TimeoutManager;

    class TimeoutPollerRunner : FeatureStartupTask
    {
        ExpiredTimeoutsPoller poller;

        public TimeoutPollerRunner(ExpiredTimeoutsPoller poller)
        {
            this.poller = poller;
        }

        protected override Task OnStart(IBusContext context)
        {
            poller.Start();
            return TaskEx.Completed;
        }

        protected override Task OnStop(IBusContext context)
        {
            return poller.Stop();
        }
    }
}