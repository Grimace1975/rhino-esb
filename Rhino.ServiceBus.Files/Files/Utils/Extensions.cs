using System;
using System.Collections.Specialized;
using System.IO;
using Rhino.ServiceBus.Internal;

namespace Rhino.ServiceBus.Files.Utils
{
    public static class Extensions
    {
        public static string ToQueryString(this NameValueCollection qs)
        {
            return string.Join("&", Array.ConvertAll(qs.AllKeys, key => string.Format("{0}={1}", MonoHttpUtility.UrlEncode(key), MonoHttpUtility.UrlEncode(qs[key]))));
        }

        public static byte[] Deserialize(IMessageSerializer messageSerializer, object[] messages)
        {
            using (var memoryStream = new MemoryStream())
            {
                messageSerializer.Serialize(messages, memoryStream);
                return memoryStream.ToArray();
            }
        }
    }
}