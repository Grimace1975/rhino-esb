using System;
using System.Collections.Generic;
using System.Linq;
using Rhino.ServiceBus.Internal;
using Rhino.ServiceBus.Messages;

namespace Rhino.ServiceBus.Files
{
    public class FilesSubscriptionStorage : ISubscriptionStorage
    {
        public FilesSubscriptionStorage(string subscriptionPath, IMessageSerializer messageSerializer, IReflection reflection)
        {
        }

        public void Initialize()
        {
        }

        public IEnumerable<Uri> GetSubscriptionsFor(Type type)
        {
            return Enumerable.Empty<Uri>();
        }

        public void AddLocalInstanceSubscription(IMessageConsumer consumer)
        {
        }

        public void RemoveLocalInstanceSubscription(IMessageConsumer consumer)
        {
        }

        public object[] GetInstanceSubscriptions(Type type)
        {
            return new object[0];
        }

        public bool AddSubscription(string type, string endpoint)
        {
            return true;
        }

        public void RemoveSubscription(string type, string endpoint)
        {
        }

        public event Action SubscriptionChanged;



        public bool ConsumeAddSubscription(AddSubscription addSubscription)
        {
            return false;
        }

        public void ConsumeRemoveSubscription(RemoveSubscription removeSubscription)
        {
        }

        public bool ConsumeAddInstanceSubscription(AddInstanceSubscription addInstanceSubscription)
        {
            return false;
        }

        public void ConsumeRemoveInstanceSubscription(RemoveInstanceSubscription removeInstanceSubscription)
        {
        }
    }
}
