using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Specialized;

namespace Rhino.ServiceBus.Files.Queues
{
    public class Message
    {
        public byte[] Data { get; set; }
        public NameValueCollection Headers { get; set; }
        public Guid Id { get; set; }
        public string Queue { get; set; }
        public DateTime SentAt { get; set; }
        public string SubQueue { get; set; }
    }
}
