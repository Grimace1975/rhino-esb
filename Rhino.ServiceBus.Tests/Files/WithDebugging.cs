using Common.Logging;
using Common.Logging.Simple;

namespace Rhino.ServiceBus.Tests.Files
{
    public class WithDebugging
    {
        static WithDebugging()
        {
            LogManager.Adapter = new ConsoleOutLoggerFactoryAdapter();
        }
    }
}