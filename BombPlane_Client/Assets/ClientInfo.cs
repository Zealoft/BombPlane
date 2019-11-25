using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using BombPlane_Client.Models;
using BombplaneProto;
using System.Collections.ObjectModel;
using BombPlane_Client.Views;

namespace BombPlane_Client.Assets
{
    /// <summary>
    /// 存储用户登录后所需的各种相关信息
    /// 
    /// </summary>
    public static class ClientInfo
    {
        public static HallWindow hall;

        public enum game_status
        {
            not_start,      // 游戏没有开始
            draw_plane,     // 我方画飞机
            self_guessing,  // 我方回合
            rival_guessing, // 对方回合
            waiting,        // 需要等待的其他情况
            waiting_drawing,// 等待对方画飞机
            self_winning,   // 我方获胜
            rival_winning,  // 对方获胜
        }
        public enum whose_turn
        {
            my_turn,
            rival_turn,
        }
        /// <summary>
        /// 传递到各个窗体的消息类型
        /// </summary>
        public enum message_type
        {
            kick,       
            invite_deny,
            game_over,
            game_crush_peer,
            game_crush_server,
            no_message,
        }

        /// <summary>
        /// 每次改变用户状态时从队列中取出旧的状态再放入新的状态
        /// </summary>
        public static ConcurrentQueue<game_status> current_gamestatus = new ConcurrentQueue<game_status>();
        // 在线用户列表
        //public static ConcurrentBag<User> online_user_bag = new ConcurrentBag<User>();
        // id--User的键值对
        public static ConcurrentDictionary<int, User> online_users = new ConcurrentDictionary<int, User>();
        public static ObservableCollection<User> online_user_obser = new ObservableCollection<User>();
        public static ConcurrentQueue<User> send_invite_user = new ConcurrentQueue<User>();
        public static ConcurrentQueue<User> send_invite_response_user = new ConcurrentQueue<User>();
        public static ConcurrentQueue<ChessBoard.Chessboard_Point> my_points = new ConcurrentQueue<ChessBoard.Chessboard_Point>();
        public static ConcurrentQueue<ChessBoard.Chessboard_Point> rival_points = new ConcurrentQueue<ChessBoard.Chessboard_Point>();
        public static ConcurrentQueue<message_type> messages = new ConcurrentQueue<message_type>();
        //public static ObservableCollection<ChessBoard> my_chessboard = new ObservableCollection<ChessBoard>();
        //public static ObservableCollection<ChessBoard> rival_chessboard = new ObservableCollection<ChessBoard>();
        public static ConcurrentQueue<string> username_passwd = new ConcurrentQueue<string>();

        // 用户自己
        public static User self_user = new User();

        // 对方用户
        public static User rival_user = new User();

        // 登录情况
        public enum LoginState
        {
            not_logged_in,
            log_error,
            login_success,
            login_timeout
        }

        public static LoginState login_state = LoginState.not_logged_in;

        public static TimingModel client_clock = new TimingModel();

    }

}
