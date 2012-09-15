using System;

namespace Rhino.ServiceBus.Files.Protocols
{
    public class CsvProtocol : IQueueProtocol
    {
        public string Id
        {
            get { return "csv"; }
        }
    }
}
