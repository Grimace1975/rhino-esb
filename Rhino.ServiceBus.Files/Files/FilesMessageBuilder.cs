using System;
using System.IO;
using System.Linq;
using Rhino.ServiceBus.Files.Queues;
using Rhino.ServiceBus.Internal;
using Rhino.ServiceBus.Messages;
using Rhino.ServiceBus.Transport;
using Rhino.ServiceBus.Files.Protocols;

namespace Rhino.ServiceBus.Files
{
    public class FilesMessageBuilder : IMessageBuilder<MessagePayload>
    {
        private readonly IQueueProtocol queueProtocol;
        private readonly IMessageSerializer messageSerializer;
        private readonly ICustomizeOutgoingMessages[] customizeHeaders;
        private Endpoint endpoint;

        public FilesMessageBuilder(IQueueProtocol queueProtocol, IMessageSerializer messageSerializer, IServiceLocator serviceLocator)
        {
            this.queueProtocol = queueProtocol;
            this.messageSerializer = messageSerializer;
            customizeHeaders = serviceLocator.ResolveAll<ICustomizeOutgoingMessages>().ToArray();
        }

        public event Action<MessagePayload> MessageBuilt;

        public MessagePayload BuildFromMessageBatch(OutgoingMessageInformation messageInformation)
        {
            if (endpoint == null)
                throw new InvalidOperationException("A source endpoint is required for Rhino Queues transport, did you Initialize me? try providing a null Uri.");

            var messageId = Guid.NewGuid();
            var payload = new MessagePayload
            {
                Data = queueProtocol.GetPayloadData(messageSerializer, messageInformation.Messages),
                Headers =
                        {
                            {"id", messageId.ToString()},
                            {"type", GetAppSpecificMarker(messageInformation.Messages).ToString()},
                            {"source", endpoint.Uri.ToString()},
                        }
            };

            messageInformation.Headers = payload.Headers;
            foreach (var customizeHeader in customizeHeaders)
                customizeHeader.Customize(messageInformation);

            payload.DeliverBy = messageInformation.DeliverBy;
            payload.MaxAttempts = messageInformation.MaxAttempts;

            var messageBuilt = MessageBuilt;
            if (messageBuilt != null)
                messageBuilt(payload);

            return payload;
        }

        public void Initialize(Endpoint source)
        {
            endpoint = source;
        }

        private static MessageType GetAppSpecificMarker(object[] msgs)
        {
            var msg = msgs[0];
            if (msg is AdministrativeMessage)
                return MessageType.AdministrativeMessageMarker;
            if (msg is LoadBalancerMessage)
                return MessageType.LoadBalancerMessageMarker;
            return 0;
        }
    }
}