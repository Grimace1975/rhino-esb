using System.Collections.Generic;
using Rhino.ServiceBus.Internal;

namespace Rhino.ServiceBus.Files.Protocols
{
    public interface IQueueProtocol
    {
        string Id { get; }
        string SearchPattern { get; }
        FileInfo GetFileInfo(string f);
        string DefaultPath { get; }
        object GetPayloadData(IMessageSerializer messageSerializer, object[] messages);
    }
}
