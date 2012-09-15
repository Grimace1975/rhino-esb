using System;
using System.Collections.Generic;

namespace Rhino.ServiceBus.Files.Queues
{
    public interface IQueue
    {
        string QueueName { get; }

        IEnumerable<Message> GetAllMessages(string subqueue);
        Message Peek();
        Message Peek(TimeSpan timeout);
        Message Peek(string subqueue);
        Message Peek(string subqueue, TimeSpan timeout);
        Message Receive();
        Message Receive(TimeSpan timeout);
        Message Receive(string subqueue);
        Message Receive(string subqueue, TimeSpan timeout);
        void MoveTo(string subqueue, Message message);
        void EnqueueDirectlyTo(string subqueue, MessagePayload payload);
        Message PeekById(Guid id);
        string[] GetSubqeueues();        
    }

    public class Queue : IQueue
    {
        private readonly IQueueManager queueManager;
        private readonly string queueName;

        public Queue(IQueueManager queueManager, string queueName)
        {
            this.queueManager = queueManager;
            this.queueName = queueName;
        }

        public string QueueName
        {
            get { return queueName; }
        }

        public IEnumerable<Message> GetAllMessages(string subqueue) { return queueManager.GetAllMessages(queueName, subqueue); }
        public Message Peek() { return queueManager.Peek(queueName, null, TimeSpan.FromDays(1)); }
        public Message Peek(TimeSpan timeout) { return queueManager.Peek(queueName, null, timeout); }
        public Message Peek(string subqueue) { return queueManager.Peek(queueName, subqueue, TimeSpan.FromDays(1)); }
        public Message Peek(string subqueue, TimeSpan timeout) { return queueManager.Peek(queueName, subqueue, timeout); }
        public Message Receive() { return queueManager.Receive(queueName, null, TimeSpan.FromDays(1)); }
        public Message Receive(TimeSpan timeout) { return queueManager.Receive(queueName, null, timeout); }
        public Message Receive(string subqueue) { return queueManager.Receive(queueName, subqueue, TimeSpan.FromDays(1)); }
        public Message Receive(string subqueue, TimeSpan timeout) { return queueManager.Receive(queueName, subqueue, timeout); }
        public void MoveTo(string subqueue, Message message) { queueManager.MoveTo(subqueue, message); }
        public void EnqueueDirectlyTo(string subqueue, MessagePayload payload) { queueManager.EnqueueDirectlyTo(queueName, subqueue, payload); }
        public Message PeekById(Guid id) { return queueManager.PeekById(queueName, id); }
        public string[] GetSubqeueues() { return queueManager.GetSubqueues(queueName); }
    }
}
