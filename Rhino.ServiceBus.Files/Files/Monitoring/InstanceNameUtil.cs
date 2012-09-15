using Rhino.ServiceBus.Files.Queues;
using System;

namespace Rhino.ServiceBus.Files.Monitoring
{
    internal static class InstanceNameUtil
    {
        public static string InboundInstanceName(this IQueueManager queueManager, Message message) { return queueManager.InboundInstanceName(message.Queue, message.SubQueue); }
        public static string InboundInstanceName(this IQueueManager queueManager, string queue, string subQueue) { return string.Format("{0}/{1}", queue, subQueue).TrimEnd('/'); }
        public static string OutboundInstanceName(this Uri endpoint, Message message) { return string.Format("{0}/{1}", message.Queue, message.SubQueue).TrimEnd('/'); }
    }
}