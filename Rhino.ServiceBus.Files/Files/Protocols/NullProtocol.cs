using System;

namespace Rhino.ServiceBus.Files.Protocols
{
    public class NullProtocol : IQueueProtocol
    {
        public string Id
        {
            get { return "null"; }
        }
    }
}
