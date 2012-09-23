using System;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Transactions;
using System.Xml;
using Common.Logging;
using Rhino.ServiceBus.Files.Queues;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Internal;
using Rhino.ServiceBus.Transport;
using Rhino.ServiceBus.Util;
using Transaction = System.Transactions.Transaction;
using Rhino.ServiceBus.Files.Protocols;

namespace Rhino.ServiceBus.Files
{
    public class FilesTransport : ITransport
    {
        private readonly ILog logger = LogManager.GetLogger(typeof(FilesTransport));
        private Uri endpoint;
        private readonly IEndpointRouter endpointRouter;
        private readonly IMessageSerializer messageSerializer;
        private readonly int threadCount; private readonly Thread[] threads;
        private readonly Type type;
        private readonly IsolationLevel queueIsolationLevel;
        private readonly int numberOfRetries;
        private readonly bool enablePerformanceCounters;
        private readonly IMessageBuilder<MessagePayload> messageBuilder;
        private readonly IQueueProtocol queueProtocol;
        //
        private IQueueManager queueManager;
        private TimeoutAction timeout;
        private volatile bool shouldContinue;
        private bool haveStarted;
        private bool shouldStop;
        private IQueue queue;
        private string queueName;
        private string path;

        [ThreadStatic]
        private static FilesCurrentMessageInformation currentMessageInformation;

        public FilesTransport(Uri endpoint,
            IEndpointRouter endpointRouter,
            IMessageSerializer messageSerializer,
            int threadCount,
            Type type,
            IsolationLevel queueIsolationLevel,
            int numberOfRetries,
            bool enablePerformanceCounters,
            IMessageBuilder<MessagePayload> messageBuilder,
            IQueueProtocol queueProtocol)
        {
            this.endpoint = endpoint;
            this.endpointRouter = endpointRouter;
            this.messageSerializer = messageSerializer;
            this.threadCount = threadCount; threads = new Thread[threadCount];
            this.type = type;
            this.queueIsolationLevel = queueIsolationLevel;
            this.numberOfRetries = numberOfRetries;
            this.enablePerformanceCounters = enablePerformanceCounters;
            this.messageBuilder = messageBuilder;
            new ErrorAction(numberOfRetries).Init(this);
            this.messageBuilder.Initialize(Endpoint);
            this.queueProtocol = queueProtocol;
        }

        #region ITransport Members

        public void Start()
        {
            if (haveStarted)
                return;
            logger.DebugFormat("Starting file transport on: {0}", endpoint);
            ConfigureAndStartQueueManager();
            queue = queueManager.GetQueue(queueName);
            //
            timeout = new TimeoutAction(queue);
            BeforeStart(queue);
            shouldStop = false;
            for (var t = 0; t < ThreadCount; t++)
                (threads[t] = new Thread(PeekMessageOnBackgroundThread)
                {
                    Name = "Rhino Service Bus Worker Thread #" + t,
                    IsBackground = true
                }).Start();
            haveStarted = true;
            var started = Started;
            if (started != null)
                started();
            AfterStart(queue);
        }

        public void Dispose()
        {
            shouldStop = true;
            logger.DebugFormat("Stopping transport for {0}", endpoint);
            OnStop();
            if (timeout != null)
                timeout.Dispose();
            WaitForProcessingToEnd();
            //
            haveStarted = false;
        }

        private void WaitForProcessingToEnd()
        {
            if (!haveStarted)
                return;
            foreach (var thread in threads)
                thread.Join();
        }

        protected virtual void AfterStart(IQueue queue) { }
        protected virtual void BeforeStart(IQueue queue) { }
        protected virtual void OnStop() { }

        public Endpoint Endpoint
        {
            get { return endpointRouter.GetRoutedEndpoint(endpoint); }
        }

        public int ThreadCount
        {
            get { return threadCount; }
        }

        public CurrentMessageInformation CurrentMessageInformation
        {
            get { return currentMessageInformation; }
        }

        public void Send(Endpoint destination, object[] msgs) { SendInternal(msgs, destination, nv => { }); }
        public void Send(Endpoint endpoint, DateTime processAgainAt, object[] msgs)
        {
            if (!haveStarted)
                throw new InvalidOperationException("Cannot send a message before transport is started");
            SendInternal(msgs, endpoint,
                nv =>
                {
                    nv["time-to-send"] = processAgainAt.ToString("yyyy-MM-ddTHH:mm:ss.fffffff", CultureInfo.InvariantCulture);
                    nv["type"] = MessageType.TimeoutMessageMarker.ToString();
                });
        }

        private void SendInternal(object[] msgs, Endpoint destination, Action<NameValueCollection> customizeHeaders)
        {
            var messageId = Guid.NewGuid();
            var messageInformation = new OutgoingMessageInformation
            {
                Destination = destination,
                Messages = msgs,
                Source = Endpoint
            };
            var payload = messageBuilder.BuildFromMessageBatch(messageInformation);
            logger.DebugFormat("Sending a message with id '{0}' to '{1}'", messageId, destination.Uri);
            customizeHeaders(payload.Headers);
            var transactionOptions = GetTransactionOptions();
            using (var tx = new TransactionScope(TransactionScopeOption.Required, transactionOptions))
            {
                queueManager.Send(destination.Uri, payload);
                tx.Complete();
            }
            var messageSent = MessageSent;
            if (messageSent == null)
                return;
            messageSent(new FilesCurrentMessageInformation
            {
                AllMessages = msgs,
                Source = Endpoint.Uri,
                Destination = destination.Uri,
                MessageId = messageId,
            });
        }

        private TransactionOptions GetTransactionOptions()
        {
            return new TransactionOptions
            {
                IsolationLevel = (Transaction.Current == null ? queueIsolationLevel : Transaction.Current.IsolationLevel),
                Timeout = TransportUtil.GetTransactionTimeout(),
            };
        }

        public void Reply(params object[] messages)
        {
            if (currentMessageInformation == null)
                throw new TransactionException("There is no message to reply to, sorry.");
            logger.DebugFormat("Replying to {0}", currentMessageInformation.Source);
            Send(endpointRouter.GetRoutedEndpoint(currentMessageInformation.Source), messages);
        }

        public event Action<CurrentMessageInformation> MessageSent;
        public event Func<CurrentMessageInformation, bool> AdministrativeMessageArrived;
        public event Func<CurrentMessageInformation, bool> MessageArrived;
        public event Action<CurrentMessageInformation, Exception> MessageSerializationException;
        public event Action<CurrentMessageInformation, Exception> MessageProcessingFailure;
        public event Action<CurrentMessageInformation, Exception> MessageProcessingCompleted;
        public event Action<CurrentMessageInformation> BeforeMessageTransactionRollback;
        public event Action<CurrentMessageInformation> BeforeMessageTransactionCommit;
        public event Action<CurrentMessageInformation, Exception> AdministrativeMessageProcessingCompleted;
        public event Action Started;

        #endregion

        public IQueue Queue
        {
            get { return queue; }
        }

        private void ConfigureAndStartQueueManager()
        {
            queueName = Path.GetFileName(endpoint.LocalPath);
            path = (endpoint.Host != "." ? Path.GetDirectoryName(endpoint.LocalPath) : queueProtocol.DefaultPath);
            queueManager = new QueueManager(queueProtocol, path);
            queueManager.CreateQueues(queueName);
            if (enablePerformanceCounters)
                queueManager.EnablePerformanceCounters();
            queueManager.Start();
        }

        private void PeekMessageOnBackgroundThread(object context)
        {
            while (!shouldStop)
            {
                try { queueManager.Peek(queueName, null, TimeSpan.FromDays(1)); }
                catch (TimeoutException) { logger.DebugFormat("Could not find a message on {0} during the timeout period", endpoint); continue; }

                if (!shouldContinue)
                    return;

                var transactionOptions = GetTransactionOptions();
                using (var tx = new TransactionScope(TransactionScopeOption.Required, transactionOptions))
                {
                    Message message;
                    try { message = queueManager.Receive(queueName, null, TimeSpan.FromSeconds(1)); }
                    catch (TimeoutException) { logger.DebugFormat("Could not find a message on {0} during the timeout period", endpoint); continue; }
                    catch (Exception e) { logger.Error("An error occured while recieving a message, shutting down message processing thread", e); return; }

                    try
                    {
                        var msgType = (MessageType)Enum.Parse(typeof(MessageType), message.Headers["type"]);
                        logger.DebugFormat("Starting to handle message {0} of type {1} on {2}", message.Id, msgType, endpoint);
                        switch (msgType)
                        {
                            case MessageType.AdministrativeMessageMarker:
                                ProcessMessage(message, tx, AdministrativeMessageArrived, AdministrativeMessageProcessingCompleted, null, null);
                                break;
                            case MessageType.ShutDownMessageMarker: //ignoring this one
                                tx.Complete();
                                break;
                            case MessageType.TimeoutMessageMarker:
                                var timeToSend = XmlConvert.ToDateTime(message.Headers["time-to-send"], XmlDateTimeSerializationMode.Utc);
                                if (timeToSend > DateTime.Now)
                                {
                                    timeout.Register(message);
                                    queue.MoveTo(SubQueue.Timeout.ToString(), message);
                                    tx.Complete();
                                }
                                else
                                    ProcessMessage(message, tx, MessageArrived, MessageProcessingCompleted, BeforeMessageTransactionCommit, BeforeMessageTransactionRollback);
                                break;
                            default:
                                ProcessMessage(message, tx, MessageArrived, MessageProcessingCompleted, BeforeMessageTransactionCommit, BeforeMessageTransactionRollback);
                                break;
                        }
                    }
                    catch (Exception exception) { logger.Debug("Could not process message", exception); }
                }
            }
        }

        private void ProcessMessage(Message message, TransactionScope tx, Func<CurrentMessageInformation, bool> messageRecieved, Action<CurrentMessageInformation, Exception> messageCompleted, Action<CurrentMessageInformation> beforeTransactionCommit, Action<CurrentMessageInformation> beforeTransactionRollback)
        {
            Exception ex = null;
            try
            {
                var messages = DeserializeMessages(message);
                try
                {
                    var messageId = new Guid(message.Headers["id"]);
                    var source = new Uri(message.Headers["source"]);
                    foreach (var msg in messages)
                    {
                        currentMessageInformation = new FilesCurrentMessageInformation
                        {
                            AllMessages = messages,
                            Message = msg,
                            Destination = endpoint,
                            MessageId = messageId,
                            Source = source,
                            TransportMessageId = message.Id.ToString(),
                            Queue = queue,
                            TransportMessage = message
                        };
                        if (!TransportUtil.ProcessSingleMessage(currentMessageInformation, messageRecieved))
                            Discard(currentMessageInformation.Message);
                    }
                }
                catch (Exception e) { ex = e; logger.Error("Failed to process message", e); }
            }
            catch (Exception e) { ex = e; logger.Error("Failed to deserialize message", e); }
            finally
            {
                var messageHandlingCompletion = new MessageHandlingCompletion(tx, null, ex, messageCompleted, beforeTransactionCommit, beforeTransactionRollback, logger, MessageProcessingFailure, currentMessageInformation);
                messageHandlingCompletion.HandleMessageCompletion();
                currentMessageInformation = null;
            }
        }

        private void Discard(object message)
        {
            logger.DebugFormat("Discarding message {0} ({1}) because there are no consumers for it.", message, currentMessageInformation.TransportMessageId);
            Send(new Endpoint { Uri = endpoint.AddSubQueue(SubQueue.Discarded) }, new[] { message });
        }

        private object[] DeserializeMessages(Message message)
        {
            try { return messageSerializer.Deserialize(new MemoryStream(message.Data)); }
            catch (Exception e)
            {
                try
                {
                    logger.Error("Error when serializing message", e);
                    var serializationError = MessageSerializationException;
                    if (serializationError != null)
                    {
                        currentMessageInformation = new FilesCurrentMessageInformation
                        {
                            Message = message,
                            Source = new Uri(message.Headers["source"]),
                            MessageId = new Guid(message.Headers["id"]),
                            TransportMessageId = message.Id.ToString(),
                            TransportMessage = message,
                            Queue = queue,
                        };
                        serializationError(currentMessageInformation, e);
                    }
                }
                catch (Exception moduleEx) { logger.Error("Error when notifying about serialization exception", moduleEx); }
                throw;
            }
        }
    }
}
