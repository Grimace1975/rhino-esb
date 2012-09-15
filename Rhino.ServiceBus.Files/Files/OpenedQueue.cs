using System;
using System.Transactions;
using Rhino.ServiceBus.Exceptions;
using Rhino.ServiceBus.Transport;
using Common.Logging;

namespace Rhino.ServiceBus.File
{
    public class OpenedQueue : IDisposable
    {
        private readonly QueueInfo info;
        private readonly OpenedQueue parent;
        private readonly string queueUrl;
        private readonly bool transactional;
        private readonly ILog logger = LogManager.GetLogger(typeof(OpenedQueue));

        public OpenedQueue(QueueInfo info, string url, bool? transactional)
        {
            if (!info.Exists)
                throw new TransportException("The queue " + info.QueueUri + " does not exists");
            this.info = info;
            queueUrl = url;
        }
        public OpenedQueue(OpenedQueue parent, string url)
            : this(parent.info, url, parent.transactional)
        {
            this.parent = parent;
        }

        public string QueueUrl
        {
            get { return queueUrl; }
        }

        public Uri RootUri
        {
            get { return info.QueueUri; }
        }

        public bool IsTransactional
        {
            get
            {
                if (parent != null)
                    return parent.IsTransactional;
                return false;
            }
        }

        public IMessageFormatter Formatter { get; set; }

        public void Dispose() { }

        public void Send(Message msg)
        {
            var responsePath = "no response queue";
            if (msg.ResponseQueue != null)
                responsePath = msg.ResponseQueue.Path;
            logger.DebugFormat("Sending message {0} to {1}, reply: {2}", msg.Label, queue.Path, responsePath);
            queue.Send(msg, GetTransactionType());
        }

        public MessageQueueTransactionType GetTransactionType()
        {
            try
            {
                if (transactional)
                    return Transaction.Current == null ? MessageQueueTransactionType.Single : MessageQueueTransactionType.Automatic;
                return MessageQueueTransactionType.None;
            }
            catch (Exception e) { logger.Error("Could not access the ambient transaction", e); throw; }
        }

        public MessageQueueTransactionType GetSingleTransactionType()
        {
            if (parent != null)
                return parent.GetSingleTransactionType();
            return (transactional ? MessageQueueTransactionType.Single : MessageQueueTransactionType.None);
        }

        public void SendInSingleTransaction(Message message) { queue.Send(message, GetSingleTransactionType()); }

        public Message TryGetMessageFromQueue(string messageId)
        {
            try { return queue.ReceiveById(messageId, GetTransactionType()); }
            catch (InvalidOperationException) { return null; } // message was read before we could read it
        }

        public OpenedQueue OpenSubQueue(SubQueue subQueue)
        {
            var messageQueue = new MessageQueue(info.QueuePath + ";" + subQueue);
            if (Formatter != null)
                messageQueue.Formatter = Formatter;
            return new OpenedQueue(this, messageQueue, queueUrl + ";" + subQueue);
        }

        public Message ReceiveById(string id) { return queue.ReceiveById(id, GetTransactionType()); }
        public MessageEnumerator GetMessageEnumerator2() { return queue.GetMessageEnumerator2(); }
        public void MoveToSubQueue(SubQueue subQueue, Message message) { queue.MoveToSubQueue(subQueue.ToString(), message); }
        public OpenedQueue OpenSiblngQueue(SubQueue subQueue, QueueAccessMode accessMode) { return new OpenedQueue(info, new MessageQueue(info.QueuePath + "#" + subQueue), queueUrl + "#" + subQueue, transactional); }

        public Message[] GetAllMessagesWithStringFormatter()
        {
            try { queue.Formatter = new XmlMessageFormatter(new[] { typeof(string) }); return queue.GetAllMessages(); }
            catch (Exception e) { throw new InvalidOperationException("Could not read messages from: " + queue.Path, e); }
        }

        public Message Peek(TimeSpan timeout) { return queue.Peek(timeout); }
        public MessageQueue ToResponseQueue() { return queue; }
        public void ConsumeMessage(string id) { TryGetMessageFromQueue(id); }
        public int GetMessageCount() { return queue.GetCount(); }
        public Message Receive() { return queue.Receive(GetTransactionType()); }
    }
}