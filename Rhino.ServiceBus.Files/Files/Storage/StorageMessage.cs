using Rhino.ServiceBus.Files.Queues;

namespace Rhino.ServiceBus.Files.Storage
{
    public class StorageMessage : Message
    {
        public MessageBookmark Bookmark { get; set; }
        public MessageStatus Status { get; set; }
    }
}
