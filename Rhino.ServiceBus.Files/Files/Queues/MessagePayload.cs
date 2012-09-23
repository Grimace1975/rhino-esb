using System;
using System.Collections.Specialized;

namespace Rhino.ServiceBus.Files.Queues
{
    public class MessagePayload
    {
        public object Data { get; set; }
        public DateTime? DeliverBy { get; set; }
        public int? MaxAttempts { get; set; }
        public NameValueCollection Headers { get; set; }
    }
}
