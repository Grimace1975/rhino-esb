using System;
using System.Collections.Generic;
using System.Linq;

namespace Rhino.ServiceBus.Files.Protocols
{
    public class NullProtocol : IQueueProtocol
    {
        public string Id
        {
            get { return "null"; }
        }

        public string SearchPattern
        {
            get { return "*"; }
        }

        public FileInfo GetFileInfo(string f)
        {
            throw new NotImplementedException();
        }

        public string DefaultPath
        {
            get { return null; }
        }


        public object GetPayloadData(Internal.IMessageSerializer messageSerializer, object[] messages)
        {
            throw new NotImplementedException();
        }
    }
}
