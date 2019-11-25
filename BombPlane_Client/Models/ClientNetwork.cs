using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using BombPlane_Client.Tools;
using BombPlane_Client.Assets;

using BombplaneProto;

namespace BombPlane_Client.Models
{
    /// <summary>
    /// 网络底层逻辑实现
    /// 不需要对该类进行实例化
    /// </summary>
    public static class ClientNetwork
    {
        public delegate void CallBack(byte[] data);

        private enum ClientState
        {
            None,        //未连接
            Connected,   //连接成功
        }


        // 当前状态
        private static ClientState _curState;
        //待发送消息队列
        private static Queue<byte[]> _messages;
        // 消息类型与回调字典
        private static Dictionary<BombplaneProto.Type, CallBack> _callBacks =
            new Dictionary<BombplaneProto.Type, CallBack>();
        // 向服务器建立UDP连接并获取网络通讯流
        private static UdpClient _client;
        // 在网络通讯流中读写数据
        private static NetworkStream _stream;

        // 目标IP
        private static IPAddress _address;
        // 端口号
        private static int port;


        // 心跳包机制
        private const float HEARTBEAT_TIME = 3;         //心跳包发送间隔时间
        private static float _timer = HEARTBEAT_TIME;   //距离上次接受心跳包的时间
        public static bool Received = true;             //收到心跳包回信

        private static IEnumerator _Connect()
        {
            _client = new UdpClient();

            // 从这里建立UDP握手

        }


        
        
        
        public static void Register(BombplaneProto.Type type, CallBack method)
        {
            if (!_callBacks.ContainsKey(type))
                _callBacks.Add(type, method);
            else
                Console.WriteLine("注册了相同的回调事件");
        }
        /// <summary>
        /// 将消息加入到消息队列中等待发送
        /// </summary>
        public static void Enqueue(Message message)
        {
            byte[] raw_bytes = PBConverter.Serialize<Message>(message);
            byte[] bytes = _Pack(message.Type, raw_bytes);

            if(_curState == ClientState.Connected)
            {
                _messages.Enqueue(bytes);
            }
        }


        /// <summary>
        /// 发送消息的例程
        /// </summary>
        /// <returns></returns>
        private static IEnumerator _Send()
        {
            while (_curState == ClientState.Connected)
            {
                _timer += Time.deltaTime;
                // 有待发送消息
                if (_messages.Count > 0)
                {
                    byte[] data = _messages.Dequeue();
                    yield return _Write(data);
                }

                //心跳包机制(每隔一段时间向服务器发送心跳包)
                if (_timer >= HEARTBEAT_TIME)
                {
                    //如果没有收到上一次发心跳包的回复
                    if (!Received)
                    {
                        _curState = ClientState.None;
                        Console.WriteLine("心跳包接受失败,断开连接");
                        yield break;
                    }
                    _timer = 0;
                    // 封装心跳包消息
                    byte[] keepalive_data = _Pack(BombplaneProto.Type.KeepaliveRequest);
                    // 发送消息
                    yield return _Write(keepalive_data);
                    Console.WriteLine("心跳包已经发送完成");
                }
                yield return null;  // 防止死循环
            }
        }


        /// <summary>
        /// 接收消息的例程，在一个单独的线程中运行
        /// </summary>
        /// <returns></returns>
        private static IEnumerator _Receive()
        {
            // 持续接收消息
            while (_curState == ClientState.Connected)
            {
                // 解析数据包
                byte[] data = new byte[4];

                int length; // 消息长度
                BombplaneProto.Type type;
                Message message = new Message();    // 消息正文
                int receive = 0;        // 接收长度

                //异步读取
                IAsyncResult async = _stream.BeginRead(data, 0, data.Length, null, null);
                while (!async.IsCompleted)
                {
                    yield return null;
                }
                //异常处理
                try
                {
                    receive = _stream.EndRead(async);
                }
                catch (Exception ex)
                {
                    _curState = ClientState.None;
                    Console.WriteLine("消息包头接收失败:" + ex.Message);
                    yield break;
                }

                if (receive < data.Length)
                {
                    _curState = ClientState.None;
                    Console.WriteLine("消息包头接收失败:");
                    yield break;
                }
                using (MemoryStream stream = new MemoryStream(data))
                {
                    BinaryReader binary = new BinaryReader(stream, Encoding.UTF8); //UTF-8格式解析
                    try
                    {
                        length = binary.ReadUInt16();
                    }
                    catch (Exception)
                    {
                        _curState = ClientState.None;
                        Console.WriteLine("消息包头接收失败:");
                        yield break;
                    }
                }

                // 如果有包体
                if (length - 4 > 0)
                {
                    data = new byte[length - 4];
                    // 异步读取
                    async = _stream.BeginRead(data, 0, data.Length, null, null);
                    while (!async.IsCompleted)
                    {
                        yield return null;
                    }
                    //异常处理
                    try
                    {
                        receive = _stream.EndRead(async);

                    }
                    catch (Exception ex)
                    {
                        _curState = ClientState.None;
                        Console.WriteLine("消息包头接收失败:" + ex.Message);
                        yield break;
                    }
                    if (receive < data.Length)
                    {
                        _curState = ClientState.None;
                        Console.WriteLine("消息包头接收失败:");
                        yield break;
                    }
                }
                // 没有包体
                else
                {
                    data = new byte[0];
                    receive = 0;
                }
                if (_callBacks.ContainsKey(type))
                {
                    // 执行回调事件
                    CallBack method = _callBacks[type];
                    method(data);
                }
                else
                {
                    Console.WriteLine("未注册该类型的回调事件");
                }
            }
        }

        public static void Init(string address = null, int port = 8848)
        {
            //连接上后不能重复连接
            if (_curState == ClientState.Connected)
                return;
            //如果为空则默认连接本机ip的服务器
            if (address == null)
                address = NetworkUtils.GetLocalIPv4();

            //类型获取失败则取消连接
            if (!IPAddress.TryParse(address, out _address))
                return;
        }


        /// <summary>
        /// 发送消息方法
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private static IEnumerator _Write(byte[] data)
        {
            if (_curState != ClientState.Connected || _stream == null)
            {
                Console.WriteLine("连接失败，无法发送消息");
                yield break;
            }

            // 异步发送消息
            IAsyncResult async = _stream.BeginWrite(data, 0, data.Length, null, null);
            while (!async.IsCompleted)
            {
                yield return null;
            }
            //异常处理
            try
            {
                _stream.EndWrite(async);
            }
            catch (Exception ex)
            {
                _curState = ClientState.None;
                Console.WriteLine("发送消息失败：",ex.ToString());
            }
        }

        private static byte[] _Pack(BombplaneProto.Type type, byte[] data = null)
        {
            MessagePacker packer = new MessagePacker();
            if (data != null)
            {
                packer.Add((ushort)(4 + data.Length)); //消息长度
                packer.Add((ushort)type);              //消息类型
                packer.Add(data);                      //消息内容
            }
            else
            {
                packer.Add(4);                         //消息长度
                packer.Add((ushort)type);              //消息类型
            }
            return packer.Package;
        }
    }

    
}
