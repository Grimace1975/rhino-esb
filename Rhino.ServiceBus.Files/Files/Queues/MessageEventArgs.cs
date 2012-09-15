using System;

namespace Rhino.ServiceBus.Files.Queues
{
    public class MessageEventArgs : EventArgs
    {
        public MessageEventArgs(Uri endpoint, Message message)
        {
            this.Endpoint = endpoint;
            this.Message = message;
        }

        public Uri Endpoint { get; private set; }
        public Message Message { get; private set; }
    }
}
