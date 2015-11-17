﻿namespace NServiceBus
{
    using Extensibility;
    using NServiceBus.Pipeline.OutgoingPipeline;
    using OutgoingPipeline;

    /// <summary>
    /// Extensions to the outgoing pipeline.
    /// </summary>
    public static class MessageIdExtensions
    {
        /// <summary>
        /// Returns the id for this message.
        /// </summary>
        /// <param name="context">Context being extended.</param>
        /// <returns>The message id.</returns>
        public static string GetMessageId(this OutgoingPhysicalMessageContext context)
        {
            return context.GetOrCreate<OutgoingPhysicalToRoutingConnector.State>().MessageId;
        }
        /// <summary>
        /// Returns the id for this message.
        /// </summary>
        /// <param name="context">Context being extended.</param>
        /// <returns>The message id.</returns>
        public static string GetMessageId(this OutgoingLogicalMessageContext context)
        {
            return context.GetOrCreate<OutgoingPhysicalToRoutingConnector.State>().MessageId;
        }

        /// <summary>
        /// Allows the user to set the message id.
        /// </summary>
        /// <param name="context">Context to extend.</param>
        /// <param name="messageId">The message id to use.</param>
        public static void SetMessageId(this ExtendableOptions context, string messageId)
        {
            Guard.AgainstNullAndEmpty(messageId, messageId);

            context.Context.GetOrCreate<OutgoingPhysicalToRoutingConnector.State>()
                .MessageId = messageId;
        }

    }
}