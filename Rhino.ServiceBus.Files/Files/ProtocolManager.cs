using System;
using System.Collections.Generic;
using Rhino.ServiceBus.Files.Protocols;

namespace Rhino.ServiceBus.Files
{
    public class ProtocolManager
    {
        static ProtocolManager()
        {
            AddProtocol(new NullProtocol());
            AddProtocol(new EmlProtocol());
            AddProtocol(new CsvProtocol());
        }

        private static Dictionary<string, IQueueProtocol> protocols = new Dictionary<string, IQueueProtocol>();

        public static void AddProtocol(IQueueProtocol protocol)
        {
            if (protocol == null)
                throw new ArgumentNullException("protocol");
            protocols.Add(protocol.Id.ToLowerInvariant(), protocol);
        }

        public static IQueueProtocol FindProtocolById(string id)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentNullException("id");
            IQueueProtocol protocol;
            return (protocols.TryGetValue(id.ToLowerInvariant(), out protocol) ? protocol : null);
        }
    }
}
