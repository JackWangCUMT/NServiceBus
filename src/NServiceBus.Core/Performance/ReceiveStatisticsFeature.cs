﻿namespace NServiceBus
{
    using System.Threading.Tasks;
    using NServiceBus.Features;

    class ReceiveStatisticsFeature : Feature
    {
        public ReceiveStatisticsFeature()
        {
            EnableByDefault();
        }
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var performanceDiagnosticsBehavior = new ReceivePerformanceDiagnosticsBehavior(context.Settings.LocalAddress());

            context.Pipeline.Register("ReceivePerformanceDiagnosticsBehavior", _ => performanceDiagnosticsBehavior, "Provides various performance counters for receive statistics");
            context.Pipeline.Register<ProcessingStatisticsBehavior.Registration>();
            context.Pipeline.Register("AuditProcessingStatistics", _ => new AuditProcessingStatisticsBehavior(), "Add ProcessingStarted and ProcessingEnded headers");

            context.RegisterStartupTask(new WarmupCooldownTask(performanceDiagnosticsBehavior));
        }

        class WarmupCooldownTask : FeatureStartupTask
        {
            readonly ReceivePerformanceDiagnosticsBehavior behavior;

            public WarmupCooldownTask(ReceivePerformanceDiagnosticsBehavior behavior)
            {
                this.behavior = behavior;
            }

            protected override Task OnStart(IBusContext context)
            {
                behavior.Warmup();
                return TaskEx.Completed;
            }

            protected override Task OnStop(IBusContext context)
            {
                behavior.Cooldown();
                return TaskEx.Completed;
            }
        }
    }
}