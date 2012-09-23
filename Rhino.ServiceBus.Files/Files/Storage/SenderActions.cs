using System;
using Rhino.ServiceBus.Files.Protocols;

namespace Rhino.ServiceBus.Files.Storage
{
    public class SenderActions : AbstractActions
    {
        public SenderActions(IQueueProtocol protocol, string path, Guid id)
            : base(protocol, path, id) { }

        public bool HasMessagesToSend()
        {
            return false;
        }
    }
}
