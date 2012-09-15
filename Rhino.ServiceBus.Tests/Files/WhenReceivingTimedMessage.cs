using System;
using System.IO;
using System.Threading;
using System.Transactions;
using Castle.Windsor;
using Rhino.ServiceBus.Castle;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Files;
using Rhino.ServiceBus.Serializers;
using Xunit;

namespace Rhino.ServiceBus.Tests.Files
{
    public class WhenReceivingTimedMessage : IDisposable
    {
        private readonly FilesTransport transport;
        private readonly ManualResetEvent wait = new ManualResetEvent(false);
        private readonly XmlMessageSerializer messageSerializer;

        public WhenReceivingTimedMessage()
        {
            //if (Directory.Exists("test.esent"))
            //    Directory.Delete("test.esent", true);

            var serviceLocator = new CastleServiceLocator(new WindsorContainer());
            messageSerializer = new XmlMessageSerializer(new DefaultReflection(),
                serviceLocator);
            transport = new FilesTransport(
                new Uri("file.null://localhost:23456/q"),
                new EndpointRouter(),
                messageSerializer,
                1,
                null,
                IsolationLevel.Serializable,
                5,
                false,
                new FilesMessageBuilder(messageSerializer, serviceLocator),
                null);
            transport.Start();
        }

        [Fact]
        public void Raises_message_arrived()
        {
            transport.MessageArrived += information =>
            {
                wait.Set();
                return true;
            };

            var signaled = SendTimedMessage();
            Assert.True(signaled);
        }

        [Fact]
        private void Raises_message_processing_completed()
        {
            transport.MessageProcessingCompleted += (information, ex) => wait.Set();

            var signaled = SendTimedMessage();
            Assert.True(signaled);
        }

        [Fact]
        public void Raises_before_message_transaction_commit()
        {
            transport.BeforeMessageTransactionCommit += information => wait.Set();

            var signaled = SendTimedMessage();
            Assert.True(signaled);
        }

        public void Dispose()
        {
            transport.Dispose();
            wait.Close();
        }

        private bool SendTimedMessage()
        {
            using (var tx = new TransactionScope())
            {
                transport.Send(transport.Endpoint, DateTime.Now.AddSeconds(3), new object[] { "test" });
                tx.Complete();
            }

            return wait.WaitOne(TimeSpan.FromSeconds(5), false);
        }
    }
}