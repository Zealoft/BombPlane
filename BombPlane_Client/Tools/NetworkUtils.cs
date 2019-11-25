using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using BombPlane_Client.Tools;
using BombplaneProto;

namespace BombPlane_Client.Tools
{
    public static class NetworkUtils
    {
        /// <summary>
        /// 将数据封装成长度+原始数据的类型
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="data"></param>
        /// <returns> 返回封装后的字节数组 </returns>
        //public static byte[] _Pack(BombplaneProto.Type type, byte[] data = null)
        //{
        //    List<byte> list = new List<byte>();
        //    if (data != null)
        //    {
        //        list.AddRange(BitConverter.GetBytes((ushort)(4 + data.Length)));//消息长度2字节
        //        list.AddRange(data);                                            //消息内容n字节
        //    }
        //    else
        //    {
        //        list.AddRange(BitConverter.GetBytes((ushort)4));                         //消息长度2字节
        //    }
        //    return list.ToArray();
        //}

        /// <summary>
        /// 获取本机IPv4,获取失败则返回null
        /// </summary>
        public static string GetLocalIPv4()
        {
            string hostName = Dns.GetHostName(); //得到主机名
            IPHostEntry iPEntry = Dns.GetHostEntry(hostName);
            for (int i = 0; i < iPEntry.AddressList.Length; i++)
            {
                //从IP地址列表中筛选出IPv4类型的IP地址
                if (iPEntry.AddressList[i].AddressFamily == AddressFamily.InterNetwork)
                    return iPEntry.AddressList[i].ToString();
            }
            return null;
        }

        /// <summary>
        /// 比特数组 -> 字符串
        /// </summary>
        public static string Byte2String(byte[] bytes)
        {
            return Encoding.UTF8.GetString(bytes);
        }

        /// <summary>
        /// 字符串 -> 比特数组
        /// </summary>
        public static byte[] String2Byte(string str)
        {
            return Encoding.UTF8.GetBytes(str);
        }
    }
}
