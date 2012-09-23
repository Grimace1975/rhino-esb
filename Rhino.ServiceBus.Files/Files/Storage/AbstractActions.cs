using System;
using System.Transactions;
using System.Collections.Generic;
using System.IO;
using Rhino.ServiceBus.Files.Protocols;

namespace Rhino.ServiceBus.Files.Storage
{
    public class AbstractActions : IDisposable
    {
        protected readonly Dictionary<string, QueueActions> queuesByName = new Dictionary<string, QueueActions>();
        protected readonly IQueueProtocol protocol;
        protected readonly string path;
        protected readonly Guid id;

        protected AbstractActions(IQueueProtocol protocol, string path, Guid id)
        {
            this.protocol = protocol;
            this.path = path;
            this.id = id;
        }

        public QueueActions GetQueue(string queueName)
        {
            QueueActions actions;
            if (queuesByName.TryGetValue(queueName, out actions))
                return actions;
            var p = Path.Combine(path, queueName);
            if (!Directory.Exists(p))
                throw new QueueDoesNotExistsException(queueName);
            queuesByName[queueName] = actions = new QueueActions(protocol, path, queueName, GetSubqueues(queueName), this, i => { });
            return actions;
        }

        private string[] GetSubqueues(string queueName)
        {
            return null;
        }

        public void Commit() { }

        public void Dispose()
        {
        }
    }
}
