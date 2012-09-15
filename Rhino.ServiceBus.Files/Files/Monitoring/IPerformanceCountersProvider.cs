namespace Rhino.ServiceBus.Files.Monitoring
{
    public interface IPerformanceCountersProvider
    {
        IOutboundPerfomanceCounters GetOutboundCounters(string instanceName);
        IInboundPerfomanceCounters GetInboundCounters(string instanceName);
    }
}