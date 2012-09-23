using System;
using Rhino.ServiceBus.Files.Queues;
using System.Web;
using System.Collections.Specialized;
using Rhino.ServiceBus.Files.Utils;
using System.IO;

namespace Rhino.ServiceBus.Files.Storage
{
    public class MessageMeta
    {
        public MessageMeta(string path) { }
        public MessageMeta(Message message)
        {
            Id = message.Id;
            var storageMessage = (message as StorageMessage);
            Status = (storageMessage != null ? storageMessage.Status : MessageStatus.InTransit);
            SentAt = message.SentAt;
            SetHeaders(message.Headers);
            SetData(message.Data);
        }

        public Guid Id { get; private set; }
        public MessageStatus Status { get; set; }
        public DateTime SentAt { get; private set; }

        public void Update() { }

        internal byte[] GetData()
        {
            return null;
        }
        internal void SetData(object d)
        {
        }

        internal NameValueCollection GetHeaders()
        {
            return HttpUtility.ParseQueryString(null);
        }

        internal void SetHeaders(NameValueCollection headers)
        {
            var headers2 = headers.ToQueryString();
        }

        internal static string GetPath(string path, string queueName, string subqueue)
        {
            return (subqueue == null ? Path.Combine(path, queueName) : Path.Combine(path, queueName, subqueue));
        }
    }
}
