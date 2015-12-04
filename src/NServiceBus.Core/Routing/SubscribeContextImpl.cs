﻿namespace NServiceBus.Routing
{
    using System;
    using NServiceBus.Pipeline;

    class SubscribeContextImpl : BehaviorContextImpl, SubscribeContext
    {
        public SubscribeContextImpl(BehaviorContext parentContext, Type eventType, SubscribeOptions options)
            : base(parentContext)
        {
            Guard.AgainstNull(nameof(parentContext), parentContext);
            Guard.AgainstNull(nameof(eventType), eventType);
            Guard.AgainstNull(nameof(options), options);

            parentContext.Extensions.Merge(options.Context);

            EventType = eventType;
        }

        public Type EventType { get; }
    }
}