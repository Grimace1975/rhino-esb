using System;
using System.Collections.Generic;
using System.Configuration;
using Rhino.ServiceBus.Files;
using Rhino.ServiceBus.Files.Queues;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Internal;

namespace Rhino.ServiceBus.Config
{
    public class FileOneWayBusConfigurationAware : IBusConfigurationAware
    {
        public void Configure(AbstractRhinoServiceBusConfiguration config, IBusContainerBuilder builder, IServiceLocator locator)
        {
            var oneWayConfig = config as OnewayRhinoServiceBusConfiguration;
            if (oneWayConfig == null)
                return;

            var messageOwners = new List<MessageOwner>();
            var messageOwnersReader = new MessageOwnersConfigReader(config.ConfigurationSection, messageOwners);
            messageOwnersReader.ReadMessageOwners();

            IQueueProtocol protocol;
            if (!messageOwnersReader.EndpointScheme.StartsWith("file.", StringComparison.InvariantCultureIgnoreCase) || (protocol = ProtocolManager.FindProtocolById(messageOwnersReader.EndpointScheme.Substring(5))) == null)
                return;

            var busConfigSection = config.ConfigurationSection.Bus;
            if (!string.IsNullOrEmpty(busConfigSection.Name) && Type.GetType(busConfigSection.Name, false) == null)
                throw new ConfigurationErrorsException(
                    "Could not find Type for attribute 'name' in node 'bus' in configuration");

            oneWayConfig.MessageOwners = messageOwners.ToArray();
            RegisterRhinoQueuesOneWay(config, builder, locator, protocol);
        }

        private void RegisterRhinoQueuesOneWay(AbstractRhinoServiceBusConfiguration c, IBusContainerBuilder b, IServiceLocator l, IQueueProtocol protocol)
        {
            var oneWayConfig = (OnewayRhinoServiceBusConfiguration)c;
            var busConfig = c.ConfigurationSection.Bus;

            b.RegisterSingleton<IMessageBuilder<MessagePayload>>(() => (IMessageBuilder<MessagePayload>)new FilesMessageBuilder(
                l.Resolve<IMessageSerializer>(),
                l.Resolve<IServiceLocator>()));

            b.RegisterSingleton<IOnewayBus>(() => (IOnewayBus)new FilesOneWayBus(
                oneWayConfig.MessageOwners,
                l.Resolve<IMessageSerializer>(),
                (!string.IsNullOrEmpty(busConfig.QueuePath) ? Type.GetType(busConfig.QueuePath) : null),
                busConfig.EnablePerformanceCounters,
                l.Resolve<IMessageBuilder<MessagePayload>>(),
                protocol));
        }
    }
}