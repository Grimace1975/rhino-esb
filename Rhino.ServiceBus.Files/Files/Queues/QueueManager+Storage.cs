using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using Rhino.ServiceBus.Files.Storage;

namespace Rhino.ServiceBus.Files.Queues
{
    public partial class QueueManager
    {
        private IQueueStorage queueStorage;
        private volatile bool waitingForAllMessagesToBeSent;

        private Message GetMessageFromQueue(string queueName, string subqueue)
        {
            AssertNotDisposedOrDisposing();
            StorageMessage message = null;
            queueStorage.Global(actions =>
            {
                message = actions.GetQueue(queueName).Dequeue(subqueue);
                if (message != null)
                    actions.RegisterUpdateToReverse(Enlistment.Id, message.Bookmark, MessageStatus.ReadyToDeliver, subqueue);
                actions.Commit();
            });
            return message;
        }

        private Message PeekMessageFromQueue(string queueName, string subqueue)
        {
            AssertNotDisposedOrDisposing();
            Message message = null;
            queueStorage.Global(actions =>
            {
                message = actions.GetQueue(queueName).Peek(subqueue);
                actions.Commit();
            });
            if (message != null)
                logger.DebugFormat("Peeked message with id '{0}' from '{1}/{2}'", message.Id, queueName, subqueue);
            return message;
        }

        public IEnumerable<Message> GetAllMessages(string queueName, string subqueue)
        {
            IEnumerable<Message> messages = null;
            queueStorage.Global(actions =>
            {
                messages = actions.GetQueue(queueName).GetAllMessages(subqueue).ToArray();
                actions.Commit();
            });
            return messages;
        }

        public void WaitForAllMessagesToBeSent()
        {
            waitingForAllMessagesToBeSent = true;
            try
            {
                var hasMessagesToSend = true;
                do
                {
                    queueStorage.Send(actions =>
                    {
                        hasMessagesToSend = actions.HasMessagesToSend();
                        actions.Commit();
                    });
                    if (hasMessagesToSend)
                        Thread.Sleep(100);
                } while (hasMessagesToSend);
            }
            finally { waitingForAllMessagesToBeSent = false; }
        }

        public void CreateQueues(params string[] queueNames)
        {
            AssertNotDisposedOrDisposing();
            queueStorage.Global(actions =>
            {
                foreach (var queueName in queueNames)
                    actions.CreateQueueIfDoesNotExists(queueName);
                actions.Commit();
            });
        }

        public string[] Queues
        {
            get
            {
                AssertNotDisposedOrDisposing();
                string[] queues = null;
                queueStorage.Global(actions =>
                {
                    queues = actions.GetAllQueuesNames();
                    actions.Commit();
                });
                return queues;
            }
        }

        public void MoveTo(string subqueue, Message message)
        {
            AssertNotDisposedOrDisposing();
            EnsureEnslistment();
            queueStorage.Global(actions =>
            {
                var queue = actions.GetQueue(message.Queue);
                var bookmark = queue.MoveTo(subqueue, (Message)message);
                actions.RegisterUpdateToReverse(Enlistment.Id, bookmark, MessageStatus.ReadyToDeliver, message.SubQueue);
                actions.Commit();
            });
            if (((StorageMessage)message).Status == MessageStatus.ReadyToDeliver)
                OnMessageReceived(new MessageEventArgs(null, message));
            var updatedMessage = new Message
            {
                Id = message.Id,
                Data = message.Data,
                Headers = message.Headers,
                Queue = message.Queue,
                SubQueue = subqueue,
                SentAt = message.SentAt
            };
            OnMessageQueuedForReceive(new MessageEventArgs(null, updatedMessage));
        }

        public void EnqueueDirectlyTo(string queueName, string subqueue, MessagePayload payload)
        {
            EnsureEnslistment();
            var message = new StorageMessage
            {
                Data = payload.Data,
                Headers = payload.Headers,
                Id = Guid.NewGuid(),
                Queue = queueName,
                SentAt = DateTime.Now,
                SubQueue = subqueue,
                Status = MessageStatus.EnqueueWait
            };
            queueStorage.Global(actions =>
            {
                var queueActions = actions.GetQueue(queueName);
                var bookmark = queueActions.Enqueue(message);
                actions.RegisterUpdateToReverse(Enlistment.Id, bookmark, MessageStatus.EnqueueWait, subqueue);
                actions.Commit();
            });
            OnMessageQueuedForReceive(new MessageEventArgs(null, message));
            lock (newMessageArrivedLock)
                Monitor.PulseAll(newMessageArrivedLock);
        }

        public Message PeekById(string queueName, Guid id)
        {
            Message message = null;
            queueStorage.Global(actions =>
            {
                var queue = actions.GetQueue(queueName);
                message = queue.PeekById(id);
                actions.Commit();
            });
            return message;
        }

        public string[] GetSubqueues(string queueName)
        {
            string[] result = null;
            queueStorage.Global(actions =>
            {
                var queue = actions.GetQueue(queueName);
                result = queue.Subqueues;
                actions.Commit();
            });
            return result;
        }

        public int GetNumberOfMessages(string queueName)
        {
            int numberOfMsgs = 0;
            queueStorage.Global(actions =>
            {
                numberOfMsgs = actions.GetNumberOfMessages(queueName);
                actions.Commit();
            });
            return numberOfMsgs;
        }
    }
}
