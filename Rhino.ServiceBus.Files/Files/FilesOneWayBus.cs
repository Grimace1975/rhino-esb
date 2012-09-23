using System;
using System.Transactions;
using Rhino.ServiceBus.Files.Queues;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Internal;
using Rhino.ServiceBus.Files.Protocols;

namespace Rhino.ServiceBus.Files
{
    public class FilesOneWayBus : FilesTransport, IOnewayBus
    {
        public static readonly Uri NullEndpoint = new Uri("file.null://nowhere:0/middle");
        private MessageOwnersSelector messageOwners;

        public FilesOneWayBus(MessageOwner[] messageOwners, IMessageSerializer messageSerializer, Type type, bool enablePerformanceCounters, IMessageBuilder<MessagePayload> messageBuilder, IQueueProtocol queueProtocol)
            : base(NullEndpoint, new EndpointRouter(), messageSerializer, 1, type, IsolationLevel.ReadCommitted, 5, enablePerformanceCounters, messageBuilder, queueProtocol)
        {
            this.messageOwners = new MessageOwnersSelector(messageOwners, new EndpointRouter());
            Start();
        }

        public void Send(params object[] msgs) { base.Send(messageOwners.GetEndpointForMessageBatch(msgs), msgs); }
    }
}