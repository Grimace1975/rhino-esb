using System;

namespace Rhino.ServiceBus.Files.Storage
{
    public class SenderActions : AbstractActions
    {
        public bool HasMessagesToSend()
        {
            return false;
        }
    }
}
