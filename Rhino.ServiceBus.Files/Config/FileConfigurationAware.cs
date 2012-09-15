using System;
using System.Configuration;
using Rhino.ServiceBus.Files;
using Rhino.ServiceBus.Files.Queues;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Internal;

namespace Rhino.ServiceBus.Config
{
    public class FileConfigurationAware : IBusConfigurationAware
    {
        public void Configure(AbstractRhinoServiceBusConfiguration config, IBusContainerBuilder builder, IServiceLocator locator)
        {
            var busConfig = config as RhinoServiceBusConfiguration;
            if (busConfig == null)
                return;

            IQueueProtocol protocol;
            if (!config.Endpoint.Scheme.StartsWith("file.", StringComparison.InvariantCultureIgnoreCase) || (protocol = ProtocolManager.FindProtocolById(config.Endpoint.Scheme.Substring(5))) == null)
                return;

            var busConfigSection = config.ConfigurationSection.Bus;
            if (!string.IsNullOrEmpty(busConfigSection.Name) && Type.GetType(busConfigSection.Name, false) == null)
                throw new ConfigurationErrorsException(
                    "Could not find Type for attribute 'name' in node 'bus' in configuration");

            RegisterFileTransport(config, builder, locator, protocol);
        }

        private void RegisterFileTransport(AbstractRhinoServiceBusConfiguration c, IBusContainerBuilder b, IServiceLocator l, IQueueProtocol protocol)
        {
            var busConfig = c.ConfigurationSection.Bus;

            b.RegisterSingleton<ISubscriptionStorage>(() => (ISubscriptionStorage)new FilesSubscriptionStorage(
                busConfig.SubscriptionPath,
                l.Resolve<IMessageSerializer>(),
                l.Resolve<IReflection>()));

            b.RegisterSingleton<IMessageBuilder<MessagePayload>>(() => (IMessageBuilder<MessagePayload>)new FilesMessageBuilder(
                l.Resolve<IMessageSerializer>(),
                l.Resolve<IServiceLocator>()));

            b.RegisterSingleton<ITransport>(() => (ITransport)new FilesTransport(
                c.Endpoint,
                l.Resolve<IEndpointRouter>(),
                l.Resolve<IMessageSerializer>(),
                c.ThreadCount,
                (!string.IsNullOrEmpty(busConfig.QueuePath) ? Type.GetType(busConfig.QueuePath) : null),
                c.IsolationLevel,
                c.NumberOfRetries,
                busConfig.EnablePerformanceCounters,
                l.Resolve<IMessageBuilder<MessagePayload>>(),
                protocol));
        }
    }
}