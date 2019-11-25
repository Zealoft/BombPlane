using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BombplaneProto;

namespace BombPlane_Client.Models
{
    public class NetworkData
    {
        public int seq;
        public byte[] data;
        public BombplaneProto.Type type;
    }
}
