using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Net;
using System.Threading.Tasks;



using BombPlane_Client.Assets;
using BombPlane_Client.Models;
using BombPlane_Client.Tools;
using BombPlane_Client.Views;
using BombplaneProto;

using Google.Protobuf;
using System.Windows.Threading;
using System.Threading;
using System.Windows;

namespace BombPlane_Client.Models
{
    public class Network
    {

        public Network()
        {
            NetworkClient.Register(BombplaneProto.Type.LoginResponse, _LoginResponse);
            NetworkClient.Register(BombplaneProto.Type.KeepaliveResponse, _KeepaliveResponse);
            NetworkClient.Register(BombplaneProto.Type.UpdateroomBroadcast, _UpdateRoomBroadcast);
            NetworkClient.Register(BombplaneProto.Type.UpdateonlineBroadcast, _UpdateOnlineBroadcast);
            NetworkClient.Register(BombplaneProto.Type.GameoverNotification, _GameOverNotification);
            NetworkClient.Register(BombplaneProto.Type.GamecrushNotification, _GameCrushNotification);
            NetworkClient.Register(BombplaneProto.Type.OnlinelistNotification, _OnlinelistNotification);
            NetworkClient.Register(BombplaneProto.Type.InviteRequest, _InviteRequestNotification);
            NetworkClient.Register(BombplaneProto.Type.KickNotification, _KickNotification);
            NetworkClient.Register(BombplaneProto.Type.InviteResponse, _InviteResponseNotification);
            NetworkClient.Register(BombplaneProto.Type.GamestartNotification, _GameStartNotification);
            NetworkClient.Register(BombplaneProto.Type.BombResponse, _BombResponse);
            NetworkClient.Register(BombplaneProto.Type.GuessResponse, _GuessResponse);
            NetworkClient.Register(BombplaneProto.Type.ErrorNotification, _ErrorNotification);
        }

        public void _QuitResponse()
        {
            Message message = new Message();
            message.Type = BombplaneProto.Type.QuitNotification;
            Console.WriteLine("发送了退出请求");

            NetworkClient.Generate_Package(message);
        }

        public void _InviteResponse(bool accept, int dstuserid)
        {
            InviteResponse inviteResponse = new InviteResponse();
            inviteResponse.Accept = accept;
            inviteResponse.Srcuserid = dstuserid;

            Message message = new Message();
            message.Type = BombplaneProto.Type.InviteResponse;
            message.Inviteresponse = inviteResponse;
            NetworkClient.Generate_Package(message);
            //Console.WriteLine("hello");
        }
        

        public void _InviteRequest(int userid)
        {
            InviteRequest invite = new InviteRequest();
            invite.Dstuserid = userid;
            invite.Srcuserid = ClientInfo.self_user.userID;

            Message message = new Message();
            message.Type = BombplaneProto.Type.InviteRequest;
            message.Inviterequest = invite;
            Console.WriteLine("dstuserid:" + message.Inviterequest.Dstuserid.ToString());
            NetworkClient.Generate_Package(message);
        }

        

        public void LoginRequest(string username, string password)
        {
            LoginRequest login = new LoginRequest();
            login.Username = ByteString.CopyFromUtf8(username);
            
            login.Password = ByteString.CopyFromUtf8(password);

            Message message = new Message();
            message.Type = BombplaneProto.Type.LoginRequest;
            message.Loginrequest = login;
            Console.WriteLine("login: " + login.ToString());
            Console.WriteLine("message: " + message.ToString());

            NetworkClient.Generate_Package(message);
        }

        public void _BombRequest(Coordinate coordinate)
        {
            BombRequest bombRequest = new BombRequest();
            bombRequest.Pos = coordinate;
            Message message = new Message();
            message.Type = BombplaneProto.Type.BombRequest;
            message.Bombrequest = bombRequest;

            NetworkClient.Generate_Package(message);
        }

        public void _GuessRequest(PlaneLocator planeLocator)
        {
            GuessRequest guessRequest = new GuessRequest();
            guessRequest.Loc = planeLocator;
            Message message = new Message();
            message.Type = BombplaneProto.Type.GuessRequest;
            message.Guessrequest = guessRequest;

            NetworkClient.Generate_Package(message);
        }

        public void InitPosRequest(List<PlaneLocator> planeLocators)
        {
            InitposNotification initposNotification = new InitposNotification();
            //InitposRequest initposRequest = new InitposRequest();
            initposNotification.Loc1 = planeLocators[0];
            initposNotification.Loc2 = planeLocators[1];
            initposNotification.Loc3 = planeLocators[2];

            Message message = new Message();
            message.Type = BombplaneProto.Type.InitposNotification;
            message.Initposnotification = initposNotification;

            NetworkClient.Generate_Package(message);
        }
        /// <summary>
        ///  发送心跳包
        /// </summary>
        public void KeepaliveRequest()
        {

            Message message = new Message();
            message.Type = BombplaneProto.Type.KeepaliveRequest;
            NetworkClient.Generate_Package(message);
            //Console.WriteLine("发送心跳包");

        }

        public void _ExitRoomNotification()
        {
            Message message = new Message();
            message.Type = BombplaneProto.Type.ExitgameNotification;
            NetworkClient.Generate_Package(message);
        }
        #region 发送消息回调事件

        public void _LoginResponse(Message message)
        {
            LoginResponse loginResponse = message.Loginresponse;
            // 登录成功
            if (loginResponse.State == LoginResponse.Types.LoginState.Success || loginResponse.State == LoginResponse.Types.LoginState.SuccessKick)
            {
                ClientInfo.login_state = ClientInfo.LoginState.login_success;
                Console.WriteLine("用户已经登录成功！");
                ClientInfo.self_user.userID = loginResponse.Userid;
                if(loginResponse.State== LoginResponse.Types.LoginState.SuccessKick)
                {
                    MessageBox.Show("已经踢出了其他同账户用户");
                }
            }
            else
            {
                ClientInfo.login_state = ClientInfo.LoginState.log_error;
                Console.WriteLine("用户登录失败！");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        private void _KeepaliveResponse(Message message)
        {
            NetworkClient.Received = true;
            Console.WriteLine("收到心跳包回应");
            ClientInfo.client_clock.last_rsp_time = 0;
            ClientInfo.client_clock.req_sent = false;
        }


        private void _ErrorNotification(Message message)
        {
            NetworkClient.curState[0] = NetworkClient.ClientState.None;
            Console.WriteLine("收到Error，网络连接出错！");
        }


        /// <summary>
        /// 服务器对所有用户发送“AB进入或退出房间”的消息
        /// </summary>
        private void _UpdateRoomBroadcast(Message message)
        {
            UpdateroomBroadcast updateroomBroadcast = message.Updateroombroadcast;
            // 用户进入房间
            int id1 = updateroomBroadcast.Userid1;
            int id2 = updateroomBroadcast.Userid2;

            List<int> keys = ClientInfo.online_users.Keys.ToList();
            int index1 = keys.IndexOf(id1);
            int index2 = keys.IndexOf(id2);

            if (updateroomBroadcast.Inout == true)
            {
                ClientInfo.online_users[id1].userState = User.user_state.playing;
                ClientInfo.online_users[id2].userState = User.user_state.playing;

                ClientInfo.online_user_obser[index1].userState = User.user_state.playing;
                ClientInfo.online_user_obser[index2].userState = User.user_state.playing;
            }
            // 用户退出房间
            else if (updateroomBroadcast.Inout == false)
            {
                ClientInfo.online_users[id1].userState = User.user_state.free;
                ClientInfo.online_users[id2].userState = User.user_state.free;

                ClientInfo.online_user_obser[index1].userState = User.user_state.free;
                ClientInfo.online_user_obser[index2].userState = User.user_state.free;
            }
        }
        /// <summary>
        /// S->allC: 单个用户的上线或下线需要对所有用户发送
        /// 每个已经在线的用户更新在线列表
        /// </summary>
        private void _UpdateOnlineBroadcast(Message message)
        {
            UpdateonlineBroadcast updateonlineBroadcast = message.Updateonlinebroadcast;
            // 有新用户上线
            if (updateonlineBroadcast.Online == true) 
            {
                User user_new = new User();
                user_new.userID = updateonlineBroadcast.Userid;
                user_new.username = updateonlineBroadcast.Username.ToStringUtf8();
                user_new.userState = User.user_state.free;
                //ClientInfo.online_user_bag.Add(user_new);
                ClientInfo.online_users.TryAdd(user_new.userID, user_new);
                //ClientInfo.online_users[user_new.userID] = user_new;
                ThreadPool.QueueUserWorkItem(delegate
                {
                    SynchronizationContext.SetSynchronizationContext(new
                      DispatcherSynchronizationContext(Application.Current.Dispatcher));
                    SynchronizationContext.Current.Post(pl =>
                    {
                        //里面写真正的业务内容
                        ClientInfo.online_user_obser.Add(user_new);
                    }, null);
                });
                
                // 在这里通知控件进行更新
                Console.WriteLine("有新用户上线，用户名为" + user_new.username);
                //GameHallWindow gameHallWindow = new GameHallWindow();
                
            }
            else
            {
                _ = ClientInfo.online_users.TryRemove(updateonlineBroadcast.Userid, out _);
                ThreadPool.QueueUserWorkItem(delegate
                {
                    SynchronizationContext.SetSynchronizationContext(new
                      DispatcherSynchronizationContext(Application.Current.Dispatcher));
                    SynchronizationContext.Current.Post(pl =>
                    {
                        //里面写真正的业务内容
                        foreach (User user in ClientInfo.online_user_obser)
                        {
                            if (user.userID == updateonlineBroadcast.Userid)
                            {
                                ClientInfo.online_user_obser.Remove(user);
                                break;
                            }
                        }
                    }, null);
                });
                
            }
        }

        /// <summary>
        /// 服务器发送游戏结束的通知
        /// </summary>
        /// <param name="data"></param>
        private void _GameOverNotification(Message message)
        {
            GameoverNotification gameoverNotification = message.Gameovernotification;
            int winner_id = gameoverNotification.Winnerid;
            if (winner_id == ClientInfo.self_user.userID)
            {
                // 己方获胜
                GameUtils.SetGameStatus(ClientInfo.game_status.self_winning);
            }
            else
            {
                // 对方获胜
                GameUtils.SetGameStatus(ClientInfo.game_status.rival_winning);
            }
        }
        /// <summary>
        /// 向双方同时发送轰炸结果
        /// </summary>
        /// <param name="data"></param>
        private void _BombResponse(Message message)
        {
            BombResponse bombResponse = message.Bombresponse;
            BombResponse.Types.BOMB_RESULT result = bombResponse.Res;
            int x = bombResponse.Pos.X;
            int y = bombResponse.Pos.Y;
            Console.WriteLine("result: " + result.ToString());
            ChessBoard.Chessboard_Point point = new ChessBoard.Chessboard_Point();
            point.x = x;
            point.y = y;
            if (GameUtils.GetGameStatus() == ClientInfo.game_status.rival_guessing)
            {
                //Console.WriteLine("network_count0: " + ClientInfo.my_chessboard.Count());
                switch (result)
                {
                    case BombResponse.Types.BOMB_RESULT.Destoryed:
                        point.state = ChessBoard.point_state.destroy;
                        ClientInfo.my_points.Enqueue(point);
                        //GameUtils.SetChessBoard(x, y, ChessBoard.whose_board.my_board, ChessBoard.point_state.destroy);
                        break;
                    case BombResponse.Types.BOMB_RESULT.Hit:
                        point.state = ChessBoard.point_state.hit;
                        ClientInfo.my_points.Enqueue(point);
                        //GameUtils.SetChessBoard(x, y, ChessBoard.whose_board.my_board, ChessBoard.point_state.hit);
                        break;
                    case BombResponse.Types.BOMB_RESULT.Miss:
                        point.state = ChessBoard.point_state.miss;
                        ClientInfo.my_points.Enqueue(point);
                        //GameUtils.SetChessBoard(x, y, ChessBoard.whose_board.my_board, ChessBoard.point_state.miss);
                        break;
                }
                GameUtils.SetGameStatus(ClientInfo.game_status.self_guessing);
            }
            else if(GameUtils.GetGameStatus() == ClientInfo.game_status.self_guessing)
            {
                //Console.WriteLine("network_count0: " + ClientInfo.my_chessboard.Count());
                switch (result)
                {
                    case BombResponse.Types.BOMB_RESULT.Destoryed:
                        //GameUtils.SetChessBoard(x, y, ChessBoard.whose_board.rival_board, ChessBoard.point_state.destroy);
                        point.state = ChessBoard.point_state.destroy;
                        ClientInfo.rival_points.Enqueue(point);
                        break;
                    case BombResponse.Types.BOMB_RESULT.Hit:
                        point.state = ChessBoard.point_state.hit;
                        ClientInfo.rival_points.Enqueue(point);
                        //GameUtils.SetChessBoard(x, y, ChessBoard.whose_board.rival_board, ChessBoard.point_state.hit);
                        break;
                    case BombResponse.Types.BOMB_RESULT.Miss:
                        point.state = ChessBoard.point_state.miss;
                        ClientInfo.rival_points.Enqueue(point);
                        //GameUtils.SetChessBoard(x, y, ChessBoard.whose_board.rival_board, ChessBoard.point_state.miss);
                        break;
                }
                GameUtils.SetGameStatus(ClientInfo.game_status.rival_guessing);
            }
            //Thread.Sleep(1000);
            //Console.WriteLine("network_count: " + ClientInfo.my_chessboard.Count());
        }

        private void _KickNotification(Message message)
        {
            
            ClientInfo.message_type kick_message = ClientInfo.message_type.kick;
            ClientInfo.messages.Enqueue(kick_message);
            //try
            //{
            //    ThreadPool.QueueUserWorkItem(delegate
            //    {
            //        try
            //        {
            //            var cur = Application.Current;
            //            SynchronizationContext.SetSynchronizationContext(new
            //          DispatcherSynchronizationContext(cur.Dispatcher));
            //            SynchronizationContext.Current.Post(pl =>
            //            {
            //                //里面写真正的业务内容
                            
            //                cur.Shutdown();

            //            }, null);
            //        }
            //        catch(Exception e)
            //        {
            //            Console.WriteLine(e.ToString());
            //        }
                    
            //    });
            //}
            //catch (Exception e)
            //{
            //    Console.WriteLine(e.ToString());
            //}
        }


        /// <summary>
        /// 向双方同时发送猜测结果
        /// </summary>
        /// <param name="data"></param>
        private void _GuessResponse(Message message)
        {
            GuessResponse guessResponse = message.Guessresponse;
            bool destroyed = guessResponse.Destroyed;
            PlaneLocator planeLocator = guessResponse.Loc;
            List<ChessBoard.Chessboard_Point> points = GameUtils.PlaneLocator_To_Points(planeLocator);
            if (GameUtils.GetGameStatus() == ClientInfo.game_status.rival_guessing)
            {
                foreach (ChessBoard.Chessboard_Point point in points)
                {
                    if (destroyed)
                    {
                        point.state = ChessBoard.point_state.destroy;
                    }
                    else
                        point.state = ChessBoard.point_state.miss;
                    ClientInfo.my_points.Enqueue(point);
                }
                GameUtils.SetGameStatus(ClientInfo.game_status.self_guessing);
            }
            else if (GameUtils.GetGameStatus() == ClientInfo.game_status.self_guessing)
            {
                foreach (ChessBoard.Chessboard_Point point in points)
                {
                    if (destroyed)
                    {
                        point.state = ChessBoard.point_state.destroy;
                    }
                    else
                        point.state = ChessBoard.point_state.miss;
                    ClientInfo.rival_points.Enqueue(point);
                }
                GameUtils.SetGameStatus(ClientInfo.game_status.rival_guessing);
            }
                
        }

        /// <summary>
        /// 对游戏崩溃通知信息的处理
        /// </summary>
        /// <param name="data"></param>
        private void _GameCrushNotification(Message message)
        {
            GamecrushNotification gamecrushNotification = message.Gamecrushnotification;
            GamecrushNotification.Types.CrushReason reason = gamecrushNotification.Reason;

            if (reason == GamecrushNotification.Types.CrushReason.OpponentOff)
            {
                ClientInfo.message_type crash_message = ClientInfo.message_type.game_crush_peer;
                ClientInfo.messages.Enqueue(crash_message);
            }
            else
            {
                ClientInfo.message_type crash_message = ClientInfo.message_type.game_crush_server;
                ClientInfo.messages.Enqueue(crash_message);
            }

        }

        /// <summary>
        /// 服务器向新上线的用户发送在线用户列表
        /// </summary>
        /// <param name="data"></param>
        private void _OnlinelistNotification(Message message)
        {
            OnlinelistNotification onlinelistNotification = message.Onlinelistnotification;
            foreach(OnlineUser user in onlinelistNotification.Onlinelist)
            {
                User user_new = new User();
                user_new.userID = user.Userid;
                user_new.username = user.Username.ToStringUtf8();
                if (user.Inroom == true)
                {
                    user_new.userState = User.user_state.playing;
                }
                else
                {
                    user_new.userState = User.user_state.free;
                }
                //ClientInfo.online_user_bag.Add(user_new);
                ClientInfo.online_users.TryAdd(user_new.userID, user_new);
                ThreadPool.QueueUserWorkItem(delegate
                {
                    SynchronizationContext.SetSynchronizationContext(new
                      DispatcherSynchronizationContext(Application.Current.Dispatcher));
                    SynchronizationContext.Current.Post(pl =>
                    {
                        //里面写真正的业务内容
                        ClientInfo.online_user_obser.Add(user_new);
                    }, null);
                });
                //ClientInfo.online_users.Add(user_new);
            }
            Console.WriteLine("Online Users nums:" + ClientInfo.online_users.Count.ToString());
            
        }

        private void _InviteRequestNotification(Message message)
        {
            InviteRequest inviteRequest = message.Inviterequest;
            Console.WriteLine("收到了来自用户的邀请：" + inviteRequest.Srcuserid.ToString());
            ClientInfo.rival_user.userID = inviteRequest.Srcuserid;
            int userid = inviteRequest.Srcuserid;
            User src_user;
            ClientInfo.online_users.TryGetValue(userid, out src_user);
            string name = src_user.username;

            bool has_user = false;
            foreach (KeyValuePair<int, User> pair in ClientInfo.online_users)
            {
                if (pair.Key == ClientInfo.rival_user.userID)
                {
                    ClientInfo.rival_user.username = pair.Value.username;
                    has_user = true;
                }
            }
            if (!has_user)
            {
                Console.WriteLine("该用户不在线！");
                return;
            }
            ClientInfo.send_invite_user.Enqueue(ClientInfo.rival_user);
        }

        public void _InviteResponseNotification(Message message)
        {
            InviteResponse inviteResponse = message.Inviteresponse;
            
            if (inviteResponse.Accept)
            {
                ClientInfo.rival_user.invite = User.invite_state.accept;
                ClientInfo.send_invite_response_user.Enqueue(ClientInfo.rival_user);
            }
            else
            {
                ClientInfo.rival_user.invite = User.invite_state.decline;
                ClientInfo.send_invite_response_user.Enqueue(ClientInfo.rival_user);
            }
            
        }

        public void _GameStartNotification(Message message)
        {
            GamestartNotification gamestartNotification = message.Gamestartnotification;
            while (ClientInfo.my_points.Count > 0)
            {
                _ = ClientInfo.my_points.TryDequeue(out _);
            }
            while (ClientInfo.rival_points.Count > 0)
            {
                _ = ClientInfo.rival_points.TryDequeue(out _);
            }
            //Thread.Sleep(100);
            if (gamestartNotification.Userid == ClientInfo.self_user.userID)
            {
                GameUtils.SetGameStatus(ClientInfo.game_status.self_guessing);
            }
            else
            {
                GameUtils.SetGameStatus(ClientInfo.game_status.rival_guessing);
            }
        }

        #endregion
    }
}
