using System;
using System.Threading;
using System.Transactions;
using Common.Logging;
using Rhino.ServiceBus.Files.Storage;

namespace Rhino.ServiceBus.Files.Queues
{
    public partial class QueueManager
    {
        [ThreadStatic]
        private static TransactionEnlistment Enlistment;

        [ThreadStatic]
        private static Transaction CurrentlyEnslistedTransaction;

        #region TransactionEnlistment

        public class TransactionEnlistment : ISinglePhaseNotification
        {
            private readonly IQueueStorage queueStorage;
            private readonly Action onCompelete;
            private readonly Action assertNotDisposed;
            private readonly ILog logger = LogManager.GetLogger(typeof(TransactionEnlistment));

            public TransactionEnlistment(IQueueStorage queueStorage, Action onCompelete, Action assertNotDisposed)
            {
                this.queueStorage = queueStorage;
                this.onCompelete = onCompelete;
                this.assertNotDisposed = assertNotDisposed;
                var transaction = Transaction.Current;
                if (transaction != null) // should happen only during recovery
                    transaction.EnlistDurable(queueStorage.Id, this, EnlistmentOptions.None);
                Id = Guid.NewGuid();
                logger.DebugFormat("Enlisting in the current transaction with enlistment id: {0}", Id);
            }

            public Guid Id { get; private set; }

            public void Prepare(PreparingEnlistment enlistment)
            {
                assertNotDisposed();
                logger.DebugFormat("Preparing enlistment with id: {0}", Id);
                var information = enlistment.RecoveryInformation();
                queueStorage.Global(actions =>
                {
                    actions.RegisterRecoveryInformation(Id, information);
                    actions.Commit();
                });
                enlistment.Prepared();
                logger.DebugFormat("Prepared enlistment with id: {0}", Id);
            }

            public void Commit(Enlistment enlistment)
            {
                try { PerformActualCommit(); enlistment.Done(); }
                catch (Exception e) { logger.Warn("Failed to commit enlistment " + Id, e); }
                finally { onCompelete(); }
            }

            private void PerformActualCommit()
            {
                assertNotDisposed();
                logger.DebugFormat("Committing enlistment with id: {0}", Id);
                queueStorage.Global(actions =>
                {
                    actions.RemoveReversalsMoveCompletedMessagesAndFinishSubQueueMove(Id);
                    actions.MarkAsReadyToSend(Id);
                    actions.DeleteRecoveryInformation(Id);
                    actions.Commit();
                });
                logger.DebugFormat("Commited enlistment with id: {0}", Id);
            }

            public void Rollback(Enlistment enlistment)
            {
                try
                {
                    assertNotDisposed();
                    logger.DebugFormat("Rolling back enlistment with id: {0}", Id);
                    queueStorage.Global(actions =>
                    {
                        actions.ReverseAllFrom(Id);
                        actions.DeleteMessageToSend(Id);
                        actions.Commit();
                    });
                    enlistment.Done();
                    logger.DebugFormat("Rolledback enlistment with id: {0}", Id);
                }
                catch (Exception e) { logger.Warn("Failed to rollback enlistment " + Id, e); }
                finally { onCompelete(); }
            }

            public void InDoubt(Enlistment enlistment) { enlistment.Done(); }

            public void SinglePhaseCommit(SinglePhaseEnlistment enlistment)
            {
                try { PerformActualCommit(); enlistment.Done(); }
                catch (Exception e) { logger.Warn("Failed to commit enlistment " + Id, e); }
                finally { onCompelete(); }
            }
        }

        #endregion

        private void EnsureEnslistment()
        {
            AssertNotDisposedOrDisposing();
            if (Transaction.Current == null)
                throw new InvalidOperationException("You must use TransactionScope when using Rhino.Queues");
            if (CurrentlyEnslistedTransaction == Transaction.Current)
                return;
            // need to change the enslitment
#pragma warning disable 420
            Interlocked.Increment(ref currentlyInsideTransaction);
#pragma warning restore 420
            Enlistment = new TransactionEnlistment(queueStorage, () =>
            {
                lock (newMessageArrivedLock)
                    Monitor.PulseAll(newMessageArrivedLock);
#pragma warning disable 420
                Interlocked.Decrement(ref currentlyInsideTransaction);
#pragma warning restore 420
            }, AssertNotDisposed);
            CurrentlyEnslistedTransaction = Transaction.Current;
        }
    }
}
