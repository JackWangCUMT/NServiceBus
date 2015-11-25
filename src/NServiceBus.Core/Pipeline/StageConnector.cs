namespace NServiceBus.Pipeline
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Connects two stages of the pipeline.
    /// </summary>
    public abstract class StageConnector<TFrom, TTo> :IStageConnector, IBehavior<TFrom, TTo> 
        where TFrom : BehaviorContext
        where TTo : BehaviorContext
    {
        /// <summary>
        /// Contains information about the pipeline this behavior is part of.
        /// </summary>
        /// <inheritdoc />
        public abstract Task Invoke(TFrom context, Func<TTo, Task> next);
    }
}