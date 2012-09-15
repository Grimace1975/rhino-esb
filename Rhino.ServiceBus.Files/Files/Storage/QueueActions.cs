using System;
using Rhino.ServiceBus.Files.Queues;
using System.Collections.Generic;

namespace Rhino.ServiceBus.Files.Storage
{
    public class QueueActions : IDisposable
    {
        public void Dispose()
        {
        }

        public MessageBookmark Enqueue(Message message)
        {
            return null;
        }

        public StorageMessage Dequeue(string subqueue)
        {
            return null;
        }

        public Message Peek(string subqueue)
        {
            return null;
        }

        public IEnumerable<StorageMessage> GetAllMessages(string subqueue)
        {
            return null;
        }

        public MessageBookmark MoveTo(string subqueue, Message message)
        {
            return null;
        }

        public StorageMessage PeekById(Guid id)
        {
            return null;
        }

        public string[] Subqueues
        {
            get { return null; }
        }
    }
}
