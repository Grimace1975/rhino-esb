using System;

namespace Rhino.ServiceBus.Files.Protocols
{
    public class EmlProtocol : IQueueProtocol
    {
        public string Id
        {
            get { return "eml"; }
        }
    }
}
