using System;

namespace Rhino.ServiceBus.Files.Protocols
{
    public struct FileInfo
    {
        public string Headers { get; set; }
        public double Timestamp { get; set; }
        public int Status { get; set; }
        public byte[] Data { get; set; }
        public Guid Id { get; set; }
    }
}
