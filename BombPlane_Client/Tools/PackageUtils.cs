using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Net;
using BombplaneProto;

namespace BombPlane_Client.Tools
{
    public class Head
    {
        public short len;
        public short seq;
    }

    /// <summary>
    /// 最终与服务器交互的数据包类型
    /// </summary>
    public class Package
    {
        public Head head = new Head();
        public byte[] content;
    }
    public static class PackageUtils
    {
        public static Package MessageToPackage(Message message, short seq)
        {
            Package package = new Package();
            package.content = PBConverter.Serialize(message);
            package.head.seq = IPAddress.HostToNetworkOrder(seq);
            package.head.len = IPAddress.HostToNetworkOrder(
                (short)(package.content.Length + 2 * sizeof(short)));
            return package;
        } 

        public static Message PackageToMessage(Package package)
        {
            Message message = PBConverter.Deserialize<Message>(package.content);
            return message;
        }
    }
}
