using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Mail;
using Rhino.ServiceBus.Internal;

namespace Rhino.ServiceBus.Files.Protocols
{
    public class EmlProtocol : IQueueProtocol
    {
        private string pickupDirectory;

        public string Id
        {
            get { return "eml"; }
        }

        public string SearchPattern
        {
            get { return "*.eml"; }
        }

        public FileInfo GetFileInfo(string f)
        {
            throw new NotImplementedException();
        }

        public MailMessage Read(string path)
        {
            return new MailMessage
            {
            };
        }

        public void Write(MailMessage message)
        {
            if (!Path.IsPathRooted(pickupDirectory))
                throw new Exception("SmtpNeedAbsolutePickupDirectory");
            var m = new SmtpClient
            {
                DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory,
                PickupDirectoryLocation = pickupDirectory,
            };
            m.Send(message);
        }

        public string DefaultPath
        {
            get
            {
                var client = new SmtpClient();
                if (client.DeliveryMethod != SmtpDeliveryMethod.SpecifiedPickupDirectory)
                    throw new InvalidOperationException("Please set email to use SpecifiedPickupDirectory");
                var path = client.PickupDirectoryLocation;
                if (string.IsNullOrEmpty(path))
                    throw new InvalidOperationException("PickupDirectoryLocation not set");
                return path;
            }
        }

        public object GetPayloadData(IMessageSerializer messageSerializer, object[] messages)
        {
            return null;
        }
    }
}
