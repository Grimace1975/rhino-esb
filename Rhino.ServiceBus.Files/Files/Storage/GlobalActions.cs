using System;
using Rhino.ServiceBus.Files.Queues;
using System.IO;
using Rhino.ServiceBus.Files.Protocols;

namespace Rhino.ServiceBus.Files.Storage
{
    public class GlobalActions : AbstractActions
    {
        public GlobalActions(IQueueProtocol protocol, string path, Guid id)
            : base(protocol, path, id) { }

        public void CreateQueueIfDoesNotExists(string queueName)
        {
            protocol
            var p = Path.Combine(path, queueName);
            if (Directory.Exists(p))
                return;
            Directory.CreateDirectory(p);
        }

        public void RegisterUpdateToReverse(Guid txId, MessageBookmark bookmark, MessageStatus statusToRestore, string subQueue)
        {
        }

        public string[] GetAllQueuesNames()
        {
            return null;
        }

        public int GetNumberOfMessages(string queueName)
        {
            return 0;
        }

        public void RegisterRecoveryInformation(Guid Id, byte[] information)
        {
        }

        public void RemoveReversalsMoveCompletedMessagesAndFinishSubQueueMove(Guid Id)
        {
        }

        public void MarkAsReadyToSend(Guid Id)
        {
        }

        public void DeleteRecoveryInformation(Guid Id)
        {
        }

        public void ReverseAllFrom(Guid Id)
        {
        }

        public void DeleteMessageToSend(Guid Id)
        {
        }
    }
}
