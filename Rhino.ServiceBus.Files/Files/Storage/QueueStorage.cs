using System;
using System.Threading;
using Common.Logging;
using Rhino.ServiceBus.Files.Protocols;

namespace Rhino.ServiceBus.Files.Storage
{
    public interface IQueueStorage : IDisposable
    {
        Guid Id { get; }
        void Initialize();
        void Global(Action<GlobalActions> action);
        void Send(Action<SenderActions> action);
        void DisposeRudely();
    }

    public partial class QueueStorage : IQueueStorage
    {
        private readonly ILog log = LogManager.GetLogger(typeof(QueueStorage));
        private readonly ReaderWriterLockSlim usageLock = new ReaderWriterLockSlim();
        private readonly IQueueProtocol protocol;
        private readonly string path;

        public QueueStorage(IQueueProtocol protocol, string path)
        {
            this.protocol = protocol;
            this.path = path;
            Id = Guid.NewGuid();
        }

        public Guid Id { get; private set; }

        public void Initialize()
        {
        }

        public void Global(Action<GlobalActions> action)
        {
            var shouldTakeLock = !usageLock.IsReadLockHeld;
            try
            {
                if (shouldTakeLock)
                    usageLock.EnterReadLock();
                using (var qa = new GlobalActions(protocol, path, Id))
                    action(qa);
            }
            finally { if (shouldTakeLock) usageLock.ExitReadLock(); }
        }

        public void Send(Action<SenderActions> action)
        {
            var shouldTakeLock = !usageLock.IsReadLockHeld;
            try
            {
                if (shouldTakeLock)
                    usageLock.EnterReadLock();
                using (var qa = new SenderActions(protocol, path, Id))
                    action(qa);
            }
            finally { if (shouldTakeLock) usageLock.ExitReadLock(); }
        }

        #region IDisposable

        public void Dispose()
        {
        }

        public void DisposeRudely()
        {
        }

        #endregion
    }
}
