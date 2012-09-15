using System;
using System.Threading;
using Common.Logging;

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
        private readonly string path;

        public QueueStorage(string path)
        {
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
                using (var qa = new GlobalActions())
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
                using (var qa = new SenderActions())
                    action(qa);
            }
            finally { if (shouldTakeLock) usageLock.ExitReadLock(); }
        }

        public void Dispose()
        {
        }

        public void DisposeRudely()
        {
        }
    }
}
