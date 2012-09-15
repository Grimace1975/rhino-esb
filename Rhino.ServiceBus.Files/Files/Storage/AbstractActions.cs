using System;
using System.Transactions;
using System.Collections.Generic;

namespace Rhino.ServiceBus.Files.Storage
{
    public class AbstractActions : IDisposable
    {
        protected readonly Dictionary<string, QueueActions> queuesByName = new Dictionary<string, QueueActions>();

        public QueueActions GetQueue(string queueName)
        {
            QueueActions actions;
            if (queuesByName.TryGetValue(queueName, out actions))
                return actions;
            if (false)
                throw new QueueDoesNotExistsException(queueName);

            //queuesByName[queueName] = actions =
            //    new QueueActions(queueName, GetSubqueues(queueName), this,
            //        i => AddToNumberOfMessagesIn(queueName, i));
            return actions;
        }

        private string[] GetSubqueues(string queueName)
        {
            var list = new List<string>();
            return list.ToArray();
        }

        public void Commit() { }

        public void Dispose()
        {
        }
    }
}
