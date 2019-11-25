using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BombPlane_Client.Tools
{
    public class MessagePacker
    {
        private List<byte> bytes = new List<byte>();

        public byte[] Package
        {
            get { return bytes.ToArray(); }
        }

        public MessagePacker Add(byte[] data)
        {
            bytes.AddRange(data);
            return this;
        }

        public MessagePacker Add(ushort value)
        {
            byte[] data = BitConverter.GetBytes(value);
            bytes.AddRange(data);
            return this;
        }

        public MessagePacker Add(uint value)
        {
            byte[] data = BitConverter.GetBytes(value);
            bytes.AddRange(data);
            return this;
        }

        public MessagePacker Add(ulong value)
        {
            byte[] data = BitConverter.GetBytes(value);
            bytes.AddRange(data);
            return this;
        }
    }
}
