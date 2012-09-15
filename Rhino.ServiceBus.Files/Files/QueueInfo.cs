using System;
using System.ComponentModel;
using Rhino.ServiceBus.Transport;
using System.IO;

namespace Rhino.ServiceBus.File
{
    public class QueueInfo
    {
        private string queuePath;
        public string QueuePath
        {
            get { return queuePath; }
            set
            {
                if (value.Contains(";"))
                {
                    var parts = value.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                    queuePath = parts[0];
                    SubQueue = (SubQueue)Enum.Parse(typeof(SubQueue), parts[1], true);
                }
                else
                    queuePath = value;
            }
        }

        public SubQueue? SubQueue { get; set; }
        public Uri QueueUri { get; set; }
        public bool? Transactional { get; set; }
        public bool IsLocal { get; set; }

        public bool Exists
        {
            get
            {
                if (IsLocal)
                    return Directory.Exists(QueuePath);
                return true; // we assume that remote queues exists
            }
        }

        public string QueuePathWithSubQueue
        {
            get
            {
                if (SubQueue == null)
                    return QueuePath;
                return QueuePath + ";" + SubQueue;
            }
        }

        public OpenedQueue Open()
        {
            var openedQueue = new OpenedQueue(this, QueueUri.ToString(), Transactional)
            {
                //Formatter = formatter
            };
            if (SubQueue != null)
                return openedQueue.OpenSubQueue(SubQueue.Value);
            return openedQueue;
        }

        public void Delete()
        {
            if (Exists && IsLocal)
                Directory.Delete(QueuePath);
        }

        //public MessageQueue Create()
        //{
        //    if (!IsLocal || Exists)
        //        return new MessageQueue(queuePath);
        //    try { return FileUtil.CreateQueue(queuePath, Transactional ?? true); }
        //    catch (Exception e) { throw new InvalidAsynchronousStateException("Could not create queue: " + QueueUri, e); }
        //}

        public override string ToString() { return this.QueueUri.ToString(); }
    }
}
