using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Common.Logging;
using Rhino.ServiceBus.Files.Monitoring;
using Rhino.ServiceBus.Files.Storage;

namespace Rhino.ServiceBus.Files.Queues
{
    public interface IQueueManager : IDisposable
    {
        #region Events
        event Action<object, MessageEventArgs> MessageQueuedForReceive;
        event Action<object, MessageEventArgs> MessageQueuedForSend;
        event Action<object, MessageEventArgs> MessageReceived;
        event Action<object, MessageEventArgs> MessageSent;
        void OnMessageSent(MessageEventArgs messageEventArgs);
        #endregion
        string[] Queues { get; }
        Guid Send(Uri uri, MessagePayload payload);
        Message Peek(string queueName);
        Message Peek(string queueName, TimeSpan timeout);
        Message Peek(string queueName, string subqueue);
        Message Peek(string queueName, string subqueue, TimeSpan timeout);
        Message Receive(string queueName);
        Message Receive(string queueName, TimeSpan timeout);
        Message Receive(string queueName, string subqueue);
        Message Receive(string queueName, string subqueue, TimeSpan timeout);
        void EnablePerformanceCounters();
        void Start();
        IEnumerable<Message> GetAllMessages(string queueName, string subqueue);
        void MoveTo(string subqueue, Message message);
        void EnqueueDirectlyTo(string queueName, string subqueue, MessagePayload payload);
        Message PeekById(string queueName, Guid id);
        string[] GetSubqueues(string queueName);
        IQueue GetQueue(string queueName);
        void CreateQueues(params string[] queueName);
        int GetNumberOfMessages(string queueName);
    }

    public partial class QueueManager : IQueueManager
    {
        private readonly ILog logger = LogManager.GetLogger(typeof(QueueManager));
        private volatile bool wasStarted;
        private readonly object newMessageArrivedLock = new object();
        private volatile int currentlyInCriticalReceiveStatus;
        private volatile int currentlyInsideTransaction;
        private PerformanceMonitor monitor;
        private readonly string path;

        public QueueManager(string path)
        {
            this.path = path;
            queueStorage = new QueueStorage(path);
            queueStorage.Initialize();
            queueStorage.Global(actions =>
            {
                //receivedMsgs.Add(actions.GetAlreadyReceivedMessageIds());
                actions.Commit();
            });
        }

        #region Disposable

        private bool disposing;
        private volatile bool wasDisposed;

        public void Dispose()
        {
            if (wasDisposed)
                return;
            DisposeResourcesWhoseDisposalCannotFail();
            if (monitor != null)
                monitor.Dispose();
            queueStorage.Dispose();
            // only after we finish incoming recieves, and finish processing active transactions can we mark it as disposed
            wasDisposed = true;
        }

        public void DisposeRudely()
        {
            if (wasDisposed)
                return;
            DisposeResourcesWhoseDisposalCannotFail();
            queueStorage.DisposeRudely();
            // only after we finish incoming recieves, and finish processing active transactions can we mark it as disposed
            wasDisposed = true;
        }

        private void DisposeResourcesWhoseDisposalCannotFail()
        {
            disposing = true;
            lock (newMessageArrivedLock)
                Monitor.PulseAll(newMessageArrivedLock);
            //if (wasStarted)
            //{
            //    purgeOldDataTimer.Dispose();
            //    queuedMessagesSender.Stop();
            //    sendingThread.Join();
            //    receiver.Dispose();
            //}
            while (currentlyInCriticalReceiveStatus > 0)
            {
                logger.WarnFormat("Waiting for {0} messages that are currently in critical receive status", currentlyInCriticalReceiveStatus);
                Thread.Sleep(TimeSpan.FromSeconds(1));
            }
            while (currentlyInsideTransaction > 0)
            {
                logger.WarnFormat("Waiting for {0} transactions currently running", currentlyInsideTransaction);
                Thread.Sleep(TimeSpan.FromSeconds(1));
            }
        }

        private void AssertNotDisposed() { if (wasDisposed) throw new ObjectDisposedException("QueueManager"); }
        private void AssertNotDisposedOrDisposing() { if (disposing || wasDisposed) throw new ObjectDisposedException("QueueManager"); }

        #endregion

        #region Events

        public event Action<Endpoint> FailedToSendMessagesTo;
        public event Action<object, MessageEventArgs> MessageQueuedForReceive;
        public event Action<object, MessageEventArgs> MessageQueuedForSend;
        public event Action<object, MessageEventArgs> MessageReceived;
        public event Action<object, MessageEventArgs> MessageSent;

        public void FailedToSendTo(Endpoint endpointThatWeFailedToSendTo)
        {
            var action = FailedToSendMessagesTo;
            if (action != null)
                action(endpointThatWeFailedToSendTo);
        }

        public void OnMessageQueuedForSend(MessageEventArgs messageEventArgs)
        {
            var action = MessageQueuedForSend;
            if (action != null)
                action(this, messageEventArgs);
        }

        public void OnMessageSent(MessageEventArgs messageEventArgs)
        {
            var action = MessageSent;
            if (action != null)
                action(this, messageEventArgs);
        }

        public void OnMessageQueuedForReceive(MessageEventArgs messageEventArgs)
        {
            var action = MessageQueuedForReceive;
            if (action != null)
                action(this, messageEventArgs);
        }

        public void OnMessageReceived(MessageEventArgs messageEventArgs)
        {
            var action = MessageReceived;
            if (action != null)
                action(this, messageEventArgs);
        }

        #endregion

        public Guid Send(Uri uri, MessagePayload payload)
        {
            if (waitingForAllMessagesToBeSent)
                throw new InvalidOperationException("Currently waiting for all messages to be sent, so we cannot send. You probably have a race condition in your application.");
            EnsureEnslistment();
            var parts = uri.AbsolutePath.Substring(1).Split('/');
            var queue = parts[0];
            string subqueue = null;
            if (parts.Length > 1)
                subqueue = string.Join("/", parts.Skip(1).ToArray());

            Guid msgId = Guid.Empty;
            //var destination = new Endpoint(uri);
            queueStorage.Global(actions =>
            {
                //msgId = actions.RegisterToSend(destination, queue, subqueue, payload, Enlistment.Id);
                actions.Commit();
            });
            var messageId = Guid.NewGuid();
            var message = new Message
            {
                Id = messageId,
                Data = payload.Data,
                Headers = payload.Headers,
                Queue = queue,
                SubQueue = subqueue
            };
            OnMessageQueuedForSend(new MessageEventArgs(uri, message));
            return messageId;
        }

        private static TimeSpan Max(TimeSpan x, TimeSpan y) { return (x >= y ? x : y); }

        public Message Peek(string queueName) { return Peek(queueName, null, TimeSpan.FromDays(1)); }
        public Message Peek(string queueName, TimeSpan timeout) { return Peek(queueName, null, timeout); }
        public Message Peek(string queueName, string subqueue) { return Peek(queueName, subqueue, TimeSpan.FromDays(1)); }
        public Message Peek(string queueName, string subqueue, TimeSpan timeout)
        {
            var remaining = timeout;
            while (true)
            {
                var message = PeekMessageFromQueue(queueName, subqueue);
                if (message != null)
                    return message;
                lock (newMessageArrivedLock)
                {
                    message = PeekMessageFromQueue(queueName, subqueue);
                    if (message != null)
                        return message;
                    var sp = Stopwatch.StartNew();
                    if (!Monitor.Wait(newMessageArrivedLock, remaining))
                        throw new TimeoutException("No message arrived in the specified timeframe " + timeout);
                    remaining = Max(remaining - sp.Elapsed, TimeSpan.Zero);
                }
            }
        }

        public Message Receive(string queueName) { return Receive(queueName, null, TimeSpan.FromDays(1)); }
        public Message Receive(string queueName, TimeSpan timeout) { return Receive(queueName, null, timeout); }
        public Message Receive(string queueName, string subqueue) { return Receive(queueName, subqueue, TimeSpan.FromDays(1)); }
        public Message Receive(string queueName, string subqueue, TimeSpan timeout)
        {
            EnsureEnslistment();
            var remaining = timeout;
            while (true)
            {
                var message = GetMessageFromQueue(queueName, subqueue);
                if (message != null)
                {
                    OnMessageReceived(new MessageEventArgs(null, message));
                    return message;
                }
                lock (newMessageArrivedLock)
                {
                    message = GetMessageFromQueue(queueName, subqueue);
                    if (message != null)
                    {
                        OnMessageReceived(new MessageEventArgs(null, message));
                        return message;
                    }
                    var sp = Stopwatch.StartNew();
                    if (!Monitor.Wait(newMessageArrivedLock, remaining))
                        throw new TimeoutException("No message arrived in the specified timeframe " + timeout);
                    var newRemaining = remaining - sp.Elapsed;
                    remaining = Max(remaining - sp.Elapsed, TimeSpan.Zero);
                }
            }
        }

        public void EnablePerformanceCounters()
        {
            if (wasStarted)
                throw new InvalidOperationException("Performance counters cannot be enabled after the queue has been started.");
            monitor = new PerformanceMonitor(this);
        }

        public void Start()
        {
            if (wasStarted)
                throw new InvalidOperationException("The Start method may not be invoked more than once.");
            wasStarted = true;
        }

        public IQueue GetQueue(string queueName) { return new Queue(this, queueName); }
    }
}
