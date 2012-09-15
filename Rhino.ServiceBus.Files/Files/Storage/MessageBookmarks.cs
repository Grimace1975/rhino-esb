namespace Rhino.ServiceBus.Files.Storage
{
    public class MessageBookmark
    {
        public string QueueName;
        public byte[] Bookmark = new byte[0];
    }
}
