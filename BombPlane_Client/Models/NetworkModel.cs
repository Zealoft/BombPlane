using BombPlane_Client.Tools;
using BombplaneProto;
using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BombPlane_Client.Models
{
    
    public class NetworkModel
    {
        private const int BUF_SIZE = 2048;
        private int interval_pointer = 0;
        // 查询是否有消息发送来的时间间隔，动态变化
        private int[] TickInvervals = { 1, 2, 5, 10, 20 };
        // 在这里实现动态时间间隔
        private int tick_time = 0;

        private Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        private IPAddress ip_address;

        UdpClient send_udp_client;
        UdpClient receive_udp_client;

        UdpClient _client;
        IPEndPoint ep;
        IPEndPoint remote;

        
        // 缓冲区队列，表示等待进行处理的消息
        public Queue<Message> queue_message = new Queue<Message>();

        private Thread receiveThread;   // 接收数据的线程
        public delegate void receiveDelegate(byte[] receive_data);  // 处理接收数据事件的方法类型
        public event receiveDelegate receiveEvent;      // 接收数据的事件


        



        public NetworkModel(string ip, int remote_port)
        {
            ip_address = IPAddress.Parse(ip);
            
            // 远程节点
            ep = new IPEndPoint(ip_address, remote_port);
            remote = new IPEndPoint(IPAddress.Any, 0);
            //client.Connect(ep);

            send_udp_client = new UdpClient();
            receive_udp_client = new UdpClient();
            send_udp_client.Connect(ep);
            receive_udp_client.Connect(ep);

            receiveThread = new Thread(Receiver);
            // 开启一个新的线程监听收到的数据
            receiveThread.Start();
        }

        /// <summary>
        /// 初始化网络客户端
        /// </summary>
        

        public void Connect()
        {
            _client = new UdpClient();

            
        }
        // 接收数据对应的线程（接收到数据之后触发事件）
        private void Receiver()
        {
            Console.WriteLine("正在开启接收端线程");
            IPEndPoint remoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0);
            // 不断接收数据
            while (true)
            {
                byte[] receiveBytes = receive_udp_client.Receive(ref remoteIpEndPoint);
                receiveEvent(receiveBytes);
            }
        }

        public void Client_ReceiveEvent(byte[] receive_data)
        {

        }
        public bool DealQueue()
        {
            // 处理当前的消息队列
            if (queue_message.Count == 0)
                return false;
            while (queue_message.Count > 0)
            {
                Message message = queue_message.Dequeue();
                if (message.Type == BombplaneProto.Type.UpdateonlineBroadcast)
                {
                    // 有用户上线/下线，通知所有用户更改在线列表
                }
                else if (message.Type == BombplaneProto.Type.BombResponse)
                {
                    // 炸点的结果回应
                }
                else if (message.Type == BombplaneProto.Type.InviteResponse)
                {
                    // 用户邀请其他玩家，玩家已经给出回应
                }
                else if(message.Type == BombplaneProto.Type.GamestartResponse)
                {
                    // 双方布置飞机完成，服务器通知玩家可以开始游戏
                }
                else if (message.Type == BombplaneProto.Type.GamecrushNotification)
                {
                    // 游戏崩溃
                }
                else if (message.Type == BombplaneProto.Type.GameoverNotification)
                {
                    // 服务器通知游戏结束，发来游戏结果
                }
                else if (message.Type == BombplaneProto.Type.UpdateroomBroadcast)
                {
                    // 更新房间的请求？等待进一步思考
                }
                else
                {
                    // 预料之外的消息，抛弃并报错
                }
            }
            return true;
        }
        
        public void NetworkTick(object sender, EventArgs e)
        {
            tick_time++;
            if (tick_time < TickInvervals[interval_pointer])
            {
                return;
            }
            tick_time = 0;
            // 进行相关操作
            // 处理消息队列
            if (DealQueue() == false)
            {
                interval_pointer = (interval_pointer == 4) ? 4 : (interval_pointer + 1);
                return;
            }
            // 向服务器发心跳包
            interval_pointer = (interval_pointer == 0) ? 0 : (interval_pointer - 1);
            Send_KeepAliveRequest();
        }

        public LoginState LoginAction(string username, string password)
        {
            // 登录时需要进行的操作
            
            byte[] recv_data = new byte[BUF_SIZE];
            LoginRequest login_request = new LoginRequest
            {
                Username = ByteString.CopyFrom(username, Encoding.UTF8),
                Password = ByteString.CopyFrom(password, Encoding.UTF8)
            };
            Message message = new Message
            {
                Type = BombplaneProto.Type.LoginRequest,
                Loginrequest = login_request
            };
            byte[] data = PBConverter.Serialize(message);

            Send_Data_ToServer(data);
            //System.Threading.Thread.Sleep(3000);
            LoginState state;
            Recv_Data_FromServer();
            // 
            //socket.ReceiveFrom(recv_data, ref remote);
            //recv_data = this.state.buffer;

            Message recv_message = PBConverter.Deserialize<Message>(recv_data);
            if (recv_message.Type == BombplaneProto.Type.LoginResponse)
            {
                LoginResponse response = recv_message.Loginresponse;
                state = response.State;
                return state;
            }
            else
            {
                // 收到的不是所需的消息类型，需要放到缓冲区
                queue_message.Append(recv_message);
            }
            state = LoginState.ServerError;
            Console.WriteLine("Login Action finished.");
            return state;
        }

        public bool Send_SingleCoordinate(int x, int y)
        {
            // 单坐标模式 将选择坐标发送到服务器
            Coordinate c = new Coordinate
            {
                X = x,
                Y = y
            };
            //Message message = new Message();
            //message.Type = BombplaneProto.Type.C
            // 使用PB序列化后得到的数据，准备发送到服务器
            byte[] data = PBConverter.Serialize<Coordinate>(c);
            //Coordinate temp = converter.Deserialize<Coordinate>(data);
            //Console.WriteLine("data from PB is " + temp.ToString());
            if (Send_Data_ToServer(data) == false)
                return false;
            return true;
        }

        public bool Send_KeepAliveRequest()
        {
            // 发送客户端的心跳包
            Message message = new Message
            {
                Type = BombplaneProto.Type.KeepaliveRequest
            };
            byte[] data = PBConverter.Serialize(message);
            return Send_Data_ToServer(data);
        }

        private bool Send_Data_ToServer(byte[] data)
        {
            // @TODO 增加消息头的长度信息
            int length = data.Length;


            try
            {
                send_udp_client.Send(data, data.Length, ep);
            }
            catch (ArgumentNullException ane)
            {
                Console.WriteLine("ArgumentNullException : {0}", ane.ToString());
                return false;
            }
            catch (SocketException se)
            {
                Console.WriteLine("SocketException : {0}", se.ToString());
                return false;
            }
            catch (Exception e)
            {
                Console.WriteLine("Unexpected exception : {0}", e.ToString());
                return false;
            }
            return true;
        }

        private void Recv_Data_FromServer()
        {
            IPEndPoint remoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0);
            while (true)
            {
                try
                {
                    byte[] receiveBytes = receive_udp_client.Receive(ref remoteIpEndPoint);
                    Message recv_message = PBConverter.Deserialize<Message>(receiveBytes);
                    
                    //string message = Encoding.Unicode.GetString(receiveBytes);

                    // 显示消息内容
                    //ShowMessageforView(receive_udp_client, string.Format("{0}[{1}]", remoteIpEndPoint, message));
                }
                catch(Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }
        }
    }
}
