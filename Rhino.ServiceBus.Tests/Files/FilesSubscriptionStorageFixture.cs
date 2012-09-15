using System;
using System.IO;
using Castle.MicroKernel;
using Castle.Windsor;
using Rhino.ServiceBus.Castle;
using Rhino.ServiceBus.Impl;
using Rhino.ServiceBus.Messages;
using Rhino.ServiceBus.Files;
using Rhino.ServiceBus.Serializers;
using Xunit;
using System.Linq;

namespace Rhino.ServiceBus.Tests.RhinoQueues
{
    public class FilesSubscriptionStorageFixture : IDisposable, OccasionalConsumerOf<string>
    {
        private readonly FilesSubscriptionStorage storage;

        public FilesSubscriptionStorageFixture()
        {
            //if (Directory.Exists("path"))
            //    Directory.Delete("path", true);

            storage = new FilesSubscriptionStorage("path",
            new XmlMessageSerializer(new DefaultReflection(),
                new CastleServiceLocator(new WindsorContainer())),
                new DefaultReflection());
            storage.Initialize();
        }

        [Fact]
        public void WillDetectDuplicateSubscription()
        {
            Assert.True(storage.ConsumeAddSubscription(new AddSubscription
            {
                Endpoint = new Endpoint
                {
                    Uri = new Uri("file.null://foo/bar")
                },
                Type = "System.String"
            }));
            Assert.False(storage.ConsumeAddSubscription(new AddSubscription
            {
                Endpoint = new Endpoint
                {
                    Uri = new Uri("file.null://foo/bar")
                },
                Type = "System.String"
            }));
        }

        [Fact]
        public void CanQueryForExistingSubscriptions()
        {
            storage.ConsumeAddSubscription(new AddSubscription
            {
                Endpoint = new Endpoint
                {
                    Uri = new Uri("file.null://foo/bar")
                },
                Type = "System.String"
            });
            storage.ConsumeAddSubscription(new AddSubscription
            {
                Endpoint = new Endpoint
                {
                    Uri = new Uri("file.null://foo/baz")
                },
                Type = "System.String"
            });

            Assert.Equal(
                new []
                {
                    new Uri("file.null://foo/bar"),
                    new Uri("file.null://foo/baz"),
                },
                storage.GetSubscriptionsFor(typeof(string)).ToArray()
                );
        }

        [Fact]
        public void CanRemoveSubscription()
        {
            storage.ConsumeAddSubscription(new AddSubscription
            {
                Endpoint = new Endpoint
                {
                    Uri = new Uri("file.null://foo/bar" )
                },
                Type = "System.String"
            });

            storage.ConsumeRemoveSubscription(new RemoveSubscription()
            {
                Endpoint = new Endpoint
                {
                    Uri = new Uri("file.null://foo/bar")
                },
                Type = "System.String"
            });

            Assert.Equal(
                new Uri[] { },
                storage.GetSubscriptionsFor(typeof(string)).ToArray()
                );
        }

        [Fact]
        public void CanAddLocalSubscripton()
        {
            storage.AddLocalInstanceSubscription(this);
            
            Assert.Equal(
                this,
                storage.GetInstanceSubscriptions(typeof(string))[0]
                );
        }

        [Fact]
        public void CanRemoveLocalSubscripton()
        {
            storage.AddLocalInstanceSubscription(this);

            Assert.Equal(
                this,
                storage.GetInstanceSubscriptions(typeof(string))[0]
                );

            storage.RemoveLocalInstanceSubscription(this);

            Assert.Empty(
                storage.GetInstanceSubscriptions(typeof(string))
                );
        }

        [Fact]
        public void CanAddRemoteInstanceSubscription()
        {
            storage.ConsumeAddInstanceSubscription(
                new AddInstanceSubscription
                {
                    Endpoint = new Uri("file.null://localhost/foobar").ToString(),
                    InstanceSubscriptionKey = Guid.NewGuid(),
                    Type = typeof(string).ToString()
                });

            Assert.Equal(
                new Uri("file.null://localhost/foobar"),
                storage.GetSubscriptionsFor(typeof(string)).Single()
                );
        }

        [Fact]
        public void CanRemoveRemoteInstanceSubscription()
        {
            var guid = Guid.NewGuid();
            storage.ConsumeAddInstanceSubscription(
                new AddInstanceSubscription
                {
                    Endpoint = new Uri("file.null://localhost/foobar").ToString(),
                    InstanceSubscriptionKey = guid,
                    Type = typeof(string).ToString()
                });

            storage.ConsumeRemoveInstanceSubscription(
                new RemoveInstanceSubscription()
                {
                    Endpoint = new Uri("file.null://localhost/foobar").ToString(),
                    InstanceSubscriptionKey = guid,
                    Type = typeof(string).ToString()
                });

            Assert.Empty(
                storage.GetSubscriptionsFor(typeof(string))
                );
        }

        public void Dispose()
        {
            //storage.Dispose();
        }

        public void Consume(string message)
        {
        }
    }
}