using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Common.Logging;
using Rhino.ServiceBus.Files.Protocols;
using Rhino.ServiceBus.Files.Queues;

namespace Rhino.ServiceBus.Files.Storage
{
    public class QueueActions : IDisposable
    {
        private readonly ILog logger = LogManager.GetLogger(typeof(QueueActions));
        private readonly IQueueProtocol protocol;
        private readonly string path;
        private readonly string queueName;
        private string[] subqueues;
        private readonly AbstractActions actions;
        private readonly Action<int> changeNumberOfMessages;

        public QueueActions(IQueueProtocol protocol, string path, string queueName, string[] subqueues, AbstractActions actions, Action<int> changeNumberOfMessages)
        {
            this.protocol = protocol;
            this.path = path;
            this.queueName = queueName;
            this.subqueues = subqueues;
            this.actions = actions;
            this.changeNumberOfMessages = changeNumberOfMessages;
        }

        public void Dispose()
        {
        }

        public string[] Subqueues
        {
            get { return subqueues; }
        }

        public MessageBookmark Enqueue(Message message)
        {
            var p = MessageMeta.GetPath(path, queueName, message.SubQueue);
            if (!Directory.Exists(p))
                Directory.CreateDirectory(p);
            var bm = new MessageBookmark { QueueName = queueName };
            var m = new MessageMeta(message);
            if (!string.IsNullOrEmpty(message.SubQueue) && !Subqueues.Contains(message.SubQueue))
            {
                //actions.AddSubqueueTo(queueName, message.SubQueue);
                subqueues = subqueues.Union(new[] { message.SubQueue }).ToArray();
            }
            logger.DebugFormat("Enqueuing msg to '{0}' with subqueue: '{1}'. Id: {2}", queueName, message.SubQueue, message.Id);
            changeNumberOfMessages(1);
            return bm;
        }

        public StorageMessage Dequeue(string subqueue)
        {
            var p = MessageMeta.GetPath(path, queueName, subqueue);
            if (!Directory.Exists(p))
                return null;
            foreach (var f in Directory.EnumerateFiles(p, protocol.SearchPattern))
            {
                var m = new MessageMeta(f);
                logger.DebugFormat("Scanning incoming message {2} on '{0}/{1}' with status {3}", queueName, subqueue, m.Id, m.Status);
                if (m.Status != MessageStatus.ReadyToDeliver)
                    continue;
                m.Status = MessageStatus.Processing;
                m.Update();
                changeNumberOfMessages(-1);
                return new StorageMessage
                {
                    Bookmark = new MessageBookmark { QueueName = queueName },
                    Headers = m.GetHeaders(),
                    Queue = queueName,
                    SentAt = m.SentAt,
                    Data = m.GetData(),
                    Id = m.Id,
                    SubQueue = subqueue,
                    Status = MessageStatus.Processing
                };
            }
            return null;
        }

        public Message Peek(string subqueue)
        {
            var p = MessageMeta.GetPath(path, queueName, subqueue);
            if (!Directory.Exists(p))
                return null;
            foreach (var f in Directory.EnumerateFiles(p, protocol.SearchPattern))
            {
                var m = new MessageMeta(f);
                logger.DebugFormat("Scanning incoming message {2} on '{0}/{1}' with status {3}", queueName, subqueue, m.Id, m.Status);
                if (m.Status != MessageStatus.ReadyToDeliver)
                    continue;
                return new StorageMessage
                {
                    Bookmark = new MessageBookmark { QueueName = queueName },
                    Headers = m.GetHeaders(),
                    Queue = queueName,
                    SentAt = m.SentAt,
                    Data = m.GetData(),
                    Id = m.Id,
                    SubQueue = subqueue,
                    Status = m.Status
                };
            }
            return null;
        }

        public IEnumerable<StorageMessage> GetAllMessages(string subqueue)
        {
            var p = MessageMeta.GetPath(path, queueName, subqueue);
            if (!Directory.Exists(p))
                yield break;
            foreach (var f in Directory.EnumerateFiles(p, protocol.SearchPattern))
            {
                var m = new MessageMeta(f);
                yield return new StorageMessage
                {
                    Bookmark = new MessageBookmark { QueueName = queueName },
                    Headers = m.GetHeaders(),
                    Queue = queueName,
                    Status = m.Status,
                    SentAt = m.SentAt,
                    Data = m.GetData(),
                    SubQueue = subqueue,
                    Id = m.Id
                };
            }
        }

        public MessageBookmark MoveTo(string subqueue, Message message)
        {
            //var id = msgsColumns["msg_id"];
            //msgsColumns["status"] = (int)MessageStatus.SubqueueChanged;
            //msgsColumns["subqueue"] =  subQueue);
            var bookmark = new MessageBookmark { QueueName = queueName };
            //update.Save(bookmark.Bookmark);
            //logger.DebugFormat("Moving message {0} to subqueue {1}", id, queueName);
            return bookmark;
        }

        public StorageMessage PeekById(Guid id)
        {
            return null;
        }
    }
}
