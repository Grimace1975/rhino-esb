using System;
using Rhino.ServiceBus.Files.Queues;

namespace Rhino.ServiceBus.Files.Storage
{
    public class GlobalActions : AbstractActions
    {
        public void CreateQueueIfDoesNotExists(string queueName)
        {
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
