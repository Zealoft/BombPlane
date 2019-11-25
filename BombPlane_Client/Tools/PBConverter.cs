using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf;

namespace BombPlane_Client.Tools
{
    public static class PBConverter
    {
        public static byte[] Serialize<T>(T obj) where T : IMessage
        {
            byte[] data = obj.ToByteArray();
            return data;
        }

        public static T Deserialize<T>(byte[] data) where T : class, IMessage, new()
        {
            T obj = new T();
            IMessage message;
            try
            {
                message = obj.Descriptor.Parser.ParseFrom(data);
                
            }
            catch(Exception e)
            {
                Console.WriteLine(e.ToString());
                return null as T;
            }
            return message as T;
        }
    }
}
