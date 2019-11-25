using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

using BombPlane_Client.Tools;
using BombPlane_Client.Assets;
using System.Diagnostics;
using BombplaneProto;
using System.Runtime.InteropServices;
using System.Configuration;

namespace BombPlane_Client.Models
{
    /// <summary>
    /// 网络底层逻辑实现
    /// 不需要对该类进行实例化
    /// </summary>
    public static class NetworkClient
    {
        public delegate void ClientCallBack(Message message);
        private static Thread callback_dealer = new Thread(_Callback) { IsBackground = true };
        private static Thread receiver = new Thread(_Receive) { IsBackground = true };
        private static Thread sender = new Thread(_Send) { IsBackground = true };
        private static Thread connect_listener = new Thread(Listen_Network_Connect) { IsBackground = true };
        //private static bool first_connected = false;
        public class CallBack
        {
            public Message message;
            public ClientCallBack ClientCallBack;
            public CallBack(Message message, ClientCallBack clientCallBack)
            {
                this.message = message;
                this.ClientCallBack = clientCallBack;
            }

            public void Execute()
            {
                ClientCallBack(message);
            }
        }


        // 线程安全的回调方法队列
        private static ConcurrentQueue<CallBack> _callBackQueue;
        // 消息类型与回调字典
        private static Dictionary<BombplaneProto.Type, ClientCallBack> _callBacks =
            new Dictionary<BombplaneProto.Type, ClientCallBack>();

        public static int send_seq = 0;
        public static int receive_seq = 0;

        private static NetworkData temp_package;
        public enum seq_num
        {
            sack,
            sseq,
            rseq
        }
        /// <summary>
        /// 线程安全的字典负责存储sack、sseq、rseq
        /// 0--sack，1--sseq，2--rseq
        /// </summary>
        public static ConcurrentDictionary<seq_num, int> safe_seqs = new ConcurrentDictionary<seq_num, int>();
        public enum ClientState
        {
            None,        //未连接
            Connected,   //连接成功
        }


        // 当前状态
        public static ConcurrentDictionary<int, ClientState> curState = new ConcurrentDictionary<int, ClientState>();

        // 是否已经建立初始连接
        private static ConcurrentDictionary<int, bool> first_connected = new ConcurrentDictionary<int, bool>();
        // 是否收到了握手回复
        private static ConcurrentDictionary<int, bool> has_received = new ConcurrentDictionary<int, bool>();
        private static ClientState _curState;
        //待发送消息队列
        private static Queue<byte[]> _messages;
        private static ConcurrentQueue<NetworkData> _messages_to_send = new ConcurrentQueue<NetworkData>();
        // 向服务器建立UDP连接并获取网络通讯流
        private static Socket _client_socket =
            new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        // 服务器为客户端建立的远程连接点
        // 连接建立后收发数据都使用该EndPoint即可
        private static EndPoint _remote_ep;
        private static IPEndPoint _remote;

        // 目标IP
        private static IPAddress _address;
        // 端口号
        private static int _port;

        public static Network network;



        // 心跳包机制
        private const float HEARTBEAT_TIME = 3;         //心跳包发送间隔时间
        private static float _timer = HEARTBEAT_TIME;   //距离上次接受心跳包的时间
        public static bool Received = true;             //收到心跳包回信

        public static void Generate_Package(Message message)
        {
            short seq = (short)safe_seqs[seq_num.sseq];
            Package package = PackageUtils.MessageToPackage(message, seq);
            //Console.WriteLine("发送的包序号为" + seq);
            byte[] byte_data = ByteUtils.getBytes(package);
            NetworkData data = new NetworkData();
            data.data = byte_data;
            data.seq = seq;
            data.type = message.Type;
            Enqueue(data);
            // 发送一个包之后sseq直接+1
            safe_seqs[seq_num.sseq]++;
            //return package;
        }
        

        #region 线程相关

        private static void _Callback()
        {
            Console.WriteLine("消息回调队列处理线程已经开启...");
            while (true)
            {
                if (_callBackQueue.Count > 0)
                {
                    if (_callBackQueue.TryDequeue(out CallBack callBack))
                    {
                        // 执行回调
                        callBack.Execute();
                    }
                }
                // 让出线程
                try
                {
                    Thread.Sleep(10);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }
        }

        public static void _Send()
        {
            Console.WriteLine("消息发送处理线程已经开启...");
            while (true)
            {
                if (_messages_to_send.Count > 0)
                {
                    NetworkData pack;
                    _messages_to_send.TryPeek(out pack);
                    Console.WriteLine("pack seq:" + pack.seq);
                    Console.WriteLine("sack:" + safe_seqs[seq_num.sack]);
                    if (pack.seq > safe_seqs[seq_num.sack] && pack.data.Length != 4)
                    {
                        Console.WriteLine("pack seq:" + pack.seq);
                        Console.WriteLine("sack:" + safe_seqs[seq_num.sack]);
                        Console.WriteLine("数据类型：" + pack.type.ToString());
                        try
                        {
                            Thread.Sleep(2000);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.ToString());
                        }
                        if (pack.seq > safe_seqs[seq_num.sack] && pack.data.Length != 4)
                        {
                            Console.WriteLine("已经超时，重新发送上一个包，包的类型为" + temp_package.type.ToString() + 
                                "包的序号为" + temp_package.seq.ToString());
                            _client_socket.SendTo(temp_package.data, _remote_ep);
                            continue;
                        }
                    }
                    _ = _messages_to_send.TryDequeue(out _);
                    Console.WriteLine("发送包的序号为" + pack.seq + "，类别为" + pack.type.ToString());
                    try
                    {
                        _client_socket.SendTo(pack.data, _remote_ep);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                    if (pack.data.Length != 4)
                        temp_package = pack;
                }
                //if (_messages.Count > 0)
                //{
                //    byte[] data = _messages.Dequeue();
                //    try
                //    {
                //        _client_socket.SendTo(data, _remote_ep);
                //    }
                //    catch(Exception e)
                //    {
                //        Console.WriteLine(e.Message);
                //    }
                //    //Console.WriteLine("已经发送一条消息。");
                //}
                try
                {
                    Thread.Sleep(10);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }
        }

        public static void _Receive()
        {
            Console.WriteLine("消息接收线程已经开启...");
            // 持续接受消息

            while (true)
            {

                byte[] data = new byte[2048];
                Package package = new Package();
                BombplaneProto.Type type = BombplaneProto.Type.Unknown;
                // 接收消息长度
                int receive = 0;

                try
                {
                    receive = _client_socket.Receive(data);
                }
                catch (Exception e)
                {
                    Console.WriteLine("接收消息错误: " + e.ToString());
                    continue;
                }
                // 解析消息过程
                try
                {
                    package.head.len = IPAddress.NetworkToHostOrder(BitConverter.ToInt16(data, 0));
                    package.head.seq = IPAddress.NetworkToHostOrder(BitConverter.ToInt16(data, 2));

                }
                catch (Exception e)
                {
                    Console.WriteLine("解析消息错误: " + e.ToString());
                    continue;
                }

                Console.WriteLine("收到的消息长度为" + package.head.len.ToString());
                Console.WriteLine("收到的消息序列号为" + package.head.seq.ToString());
                Console.WriteLine("此时期待的消息序列号为" + safe_seqs[seq_num.rseq]);
                if (package.head.len == 4)
                {
                    Console.WriteLine("收到一个ACK包，序号为" + package.head.seq.ToString());
                    if (package.head.seq == safe_seqs[seq_num.sack])
                        safe_seqs[seq_num.sack]++;
                    continue;
                }
                if (package.head.len == 11 && package.head.seq == 0)
                {
                    SendAck(package.head.seq);
                    safe_seqs[seq_num.rseq]++;
                    continue;
                }
                if (package.head.seq == safe_seqs[seq_num.rseq])
                {
                    if (package.head.len > 4)
                    {
                        SendAck(package.head.seq);
                        safe_seqs[seq_num.rseq]++;
                    }
                }
                else if (package.head.seq < safe_seqs[seq_num.rseq])
                {
                    if (package.head.len > 4)
                        SendAck(package.head.seq);
                    continue;
                }
                if (package.head.len > 4)
                {
                    //Console.WriteLine("收到一个数据包！");
                    byte[] content = new byte[package.head.len - 4];
                    Array.Copy(data, 4, content, 0, package.head.len - 4);
                    package.content = content;
                    //content.CopyTo(package.content, 0);
                    Message message = PackageUtils.PackageToMessage(package);
                    Console.WriteLine("接收到消息，类型为" +  message.Type.ToString() + "，序号为" + package.head.seq.ToString());
                    type = message.Type;
                    // 执行回调事件
                    if (_callBacks.ContainsKey(type))
                    {
                        CallBack callBack = new CallBack(message, _callBacks[type]);
                        // 放入回调执行线程
                        _callBackQueue.Enqueue(callBack);
                    }
                }
                // 收到的包是ack包
                else
                {
                    Console.WriteLine("收到一个ACK包，序号为" + package.head.seq.ToString());
                    if (package.head.seq == safe_seqs[seq_num.sack])
                        safe_seqs[seq_num.sack]++;
                    //receive_seq++;
                }
                
            }
        }
        #endregion


        public static void Register(BombplaneProto.Type type, ClientCallBack method)
        {
            if (!_callBacks.ContainsKey(type))
                _callBacks.Add(type, method);
            else
                Console.WriteLine("注册了相同的回调事件");
        }
        /// <summary>
        /// 将消息加入到消息队列中等待发送
        /// </summary>
        //public static void Enqueue(byte[] data)
        //{
        //    // 发第一个ack的时候连接未建立且状态没有
        //    if (curState[0] == ClientState.Connected)
        //    {
        //        _messages.Enqueue(data);
        //    }
        //} 

        public static void Enqueue(NetworkData data)
        {
            if (curState.Count == 0)
                return;
            if (curState[0] == ClientState.Connected)
            {
                _messages_to_send.Enqueue(data);
            }
        }

        public static void SendAck(int seq)
        {
            Console.WriteLine("发送的ack包序号为" + seq.ToString());
            //Console.WriteLine("receive_seq is " + receive_seq.ToString());
            
            //else
            //    return;
            Head head = new Head();

            head.len = IPAddress.HostToNetworkOrder((short)(2 * sizeof(short)));
            head.seq = IPAddress.HostToNetworkOrder((short)(seq));
            byte[] data = ByteUtils.getBytes(head);
            _client_socket.SendTo(data, _remote_ep);
            //Enqueue(data);
            //NetworkData n_data = new NetworkData();

            //n_data.data = data;
            //n_data.seq = seq;
            //Enqueue(n_data);
            //Console.WriteLine("已经发出一个ack包，序号为" + receive_seq.ToString());
            //if (seq == receive_seq)
            //    receive_seq++;

        }

        public static void Listen_Received_Hello()
        {
            byte[] buffer = new byte[2048];
            _client_socket.ReceiveFrom(buffer, ref _remote_ep);
            has_received[0] = true;
        }

        public static void Listen_Network_Connect()
        {
            while (true)
            {
                // 每秒监控一次网络状态改变
                // 如果当前没有连接并且已经建立了初始的连接，则使用重新握手机制
                if (!GetIsConnected() && first_connected[0])
                {
                    // 断连后做的操作
                    Console.WriteLine("网络连接已经断开！");
                    int port = int.Parse(ConfigurationManager.AppSettings["network_port"]);
                    string ip = ConfigurationManager.AppSettings["network_ip"];
                    callback_dealer.Abort();
                    receiver.Abort();
                    sender.Abort();
                    connect_listener.Abort();


                    Connect(ip, port);


                    callback_dealer.Start();
                    receiver.Start();
                    sender.Start();
                    connect_listener.Start();

                    
                }
                Thread.Sleep(1000);
            }
        }

        public static void Connect(string address = null, int port = 21965)
        {
            // 连接上后不能重复连接
            //ClientState state;
            //curState.TryGetValue(0, out state);
            if (GetIsConnected())
                return;
            safe_seqs[seq_num.sack] = 0;
            safe_seqs[seq_num.sseq] = 0;
            safe_seqs[seq_num.rseq] = 0;
            has_received[0] = false;
            //if (_curState == ClientState.Connected)
            //    return;
            // 如果为空则默认连接本机ip的服务器
            if (address == null)
                address = NetworkUtils.GetLocalIPv4();

            // 类型获取失败则取消连接
            if (!IPAddress.TryParse(address, out _address))
            {
                Console.WriteLine("IP地址错误，请重新尝试");
                return;
            }
            _port = port;
            IPEndPoint ep = new IPEndPoint(_address, _port);
            _remote_ep = new IPEndPoint(IPAddress.Any, 0);
            try
            {
                _client_socket.Bind(_remote_ep);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }


            byte[] hello_data = Encoding.UTF8.GetBytes("hello!");
            Thread hello_receiver = new Thread(Listen_Received_Hello) { IsBackground = true };
            hello_receiver.Start();
            while (!has_received[0])
            {
                _client_socket.SendTo(hello_data, ep);
                Thread.Sleep(1000);
            }
            // 强行发送ack
            if (true)
            {
                Console.WriteLine("发送的ack包序号为0");
                //Console.WriteLine("receive_seq is " + receive_seq.ToString());

                //else
                //    return;
                Head head = new Head();

                head.len = IPAddress.HostToNetworkOrder((short)(2 * sizeof(short)));
                head.seq = IPAddress.HostToNetworkOrder((short)(0));
                byte[] data = ByteUtils.getBytes(head);
                _client_socket.SendTo(data, _remote_ep);
            }
            //SendAck(0);
            safe_seqs[seq_num.rseq]++;
            hello_receiver.Abort();
            //_client_socket.ReceiveFrom(buffer, ref _remote_ep);
            Console.WriteLine("已经建立连接：" + _remote_ep.ToString());
            _remote = (IPEndPoint)_remote_ep;
            curState[0] = ClientState.Connected;
            if (first_connected.Count==0 || !first_connected[0])
                first_connected[0] = true;
            //_curState = ClientState.Connected;

            //SendAck(0);
            //safe_seqs[seq_num.rseq]++;
            //_client.Connect(_remote);

        }

        public static bool GetIsConnected()
        {
            if (curState.Count == 0 || curState[0] == ClientState.None)
                return false;
            return true;
        }

        public static void SetIsConnected(bool is_connected)
        {
            if (is_connected)
                curState[0] = ClientState.Connected;
            else
                curState[0] = ClientState.None;
        }

        /// <summary>
        /// 开启网络服务
        /// </summary>
        /// <param name="address"></param>
        /// <param name="port"></param>
        public static void StartNetwork(string address = null, int port = 21965)
        {
            if (!GetIsConnected())
            {
                safe_seqs.TryAdd(seq_num.sack, 0);
                safe_seqs.TryAdd(seq_num.sseq, 0);
                safe_seqs.TryAdd(seq_num.rseq, 0);
                //safe_seqs[seq_num.rseq]++;
                //safe_seqs.TryUpdate(seq_num.rseq, 1, 0);
                //safe_seqs[2] = 1;
                //Console.WriteLine(safe_seqs.TryAdd(1, 1).ToString());

                network = new Network();
                // 事件处理
                _callBackQueue = new ConcurrentQueue<CallBack>();
                _messages = new Queue<byte[]>();
                // 开启udp连接
                Connect(address, port);
                // 开启处理回调消息队列线程
                callback_dealer.Start();
                // 开启接收消息线程
                receiver.Start();
                // 开启发送消息线程
                sender.Start();
                // 监控网络状态的线程
                connect_listener.Start();

                SetIsConnected(true);
            }
            
        }
    }
}
