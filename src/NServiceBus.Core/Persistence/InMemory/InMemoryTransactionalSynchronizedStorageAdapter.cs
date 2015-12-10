namespace NServiceBus
{
    using System.Threading.Tasks;
    using System.Transactions;
    using NServiceBus.Extensibility;
    using NServiceBus.InMemory.Outbox;
    using NServiceBus.Outbox;
    using NServiceBus.Persistence;
    using NServiceBus.Transports;

    class InMemoryTransactionalSynchronizedStorageAdapter : ISynchronizedStorageAdapter
    {
        public Task<CompletableSynchronizedStorageSession> TryAdapt(OutboxTransaction transaction, ContextBag context)
        {
            var inMemOutboxTransaction = transaction as InMemoryOutboxTransaction;
            if (inMemOutboxTransaction != null)
            {
                CompletableSynchronizedStorageSession session = new InMemorySynchronizedStorageSession(inMemOutboxTransaction.Transaction);
                return Task.FromResult(session);
            }
            return Task.FromResult<CompletableSynchronizedStorageSession>(null);
        }

        public Task<CompletableSynchronizedStorageSession> TryAdapt(TransportTransaction transportTransaction, ContextBag context)
        {
            Transaction ambientTransaction;
            
            if (transportTransaction.TryGet(out ambientTransaction))
            {
                var transaction = new InMemoryTransaction();
                CompletableSynchronizedStorageSession session = new InMemorySynchronizedStorageSession(transaction);
                ambientTransaction.EnlistVolatile(new EnlistmentNotification(transaction), EnlistmentOptions.None);
                return Task.FromResult(session);
            }
            return Task.FromResult<CompletableSynchronizedStorageSession>(null);
        }

        private class EnlistmentNotification : IEnlistmentNotification
        {
            InMemoryTransaction transaction;

            public EnlistmentNotification(InMemoryTransaction transaction)
            {
                this.transaction = transaction;
            }

            public void Prepare(PreparingEnlistment preparingEnlistment)
            {
                transaction.Commit();
                preparingEnlistment.Prepared();
            }

            public void Commit(Enlistment enlistment)
            {
                enlistment.Done();
            }

            public void Rollback(Enlistment enlistment)
            {
                enlistment.Done();
            }

            public void InDoubt(Enlistment enlistment)
            {
                enlistment.Done();
            }
        }
    }
}