using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Runtime.InteropServices;
using BombPlane_Client.Models;

namespace BombPlane_Client.Tools
{
    public static class ByteUtils
    {
        public static byte[] getBytes(Package package)
        {
            int len = IPAddress.NetworkToHostOrder(package.head.len);
            byte[] arr = new byte[len];
            byte[] len_bytes = BitConverter.GetBytes(package.head.len);
            len_bytes.CopyTo(arr, 0);
            byte[] seq_bytes = BitConverter.GetBytes(package.head.seq);
            seq_bytes.CopyTo(arr, sizeof(short));
            package.content.CopyTo(arr, 2 * sizeof(short));
            
            return arr;
        }

        public static byte[] getBytes(Head head)
        {
            int len = IPAddress.NetworkToHostOrder(head.len);
            byte[] arr = new byte[4];
            byte[] len_bytes = BitConverter.GetBytes(head.len);
            byte[] seq_bytes = BitConverter.GetBytes(head.seq);
            len_bytes.CopyTo(arr, 0);
            seq_bytes.CopyTo(arr, 2);

            return arr;
        }
    }
}
