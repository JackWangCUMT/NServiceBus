﻿namespace NServiceBus.OutgoingPipeline
{
    using System.Collections.Generic;
    using NServiceBus.Pipeline;

    class OutgoingPublishContextImpl : OutgoingContextImpl, OutgoingPublishContext
    {
        public OutgoingPublishContextImpl(OutgoingLogicalMessage message, PublishOptions options, BehaviorContext parentContext)
            : base(options.MessageId, new Dictionary<string, string>(options.OutgoingHeaders), parentContext)
        {
            Message = message;
            Guard.AgainstNull(nameof(parentContext), parentContext);
            Guard.AgainstNull(nameof(message), message);
            Guard.AgainstNull(nameof(options), options);

            parentContext.Extensions.Merge(options.Context);
        }

        public OutgoingLogicalMessage Message { get; }
    }
}