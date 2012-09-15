using Rhino.ServiceBus.Files.Queues;
using Rhino.ServiceBus.Impl;

namespace Rhino.ServiceBus.Files
{
    public class FilesCurrentMessageInformation : CurrentMessageInformation
    {
        public Message TransportMessage { get; set; }
        public IQueue Queue { get; set; }
    }
}