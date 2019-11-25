using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using BombPlane_Client.Models;
using BombPlane_Client.Tools;
using BombplaneProto;
using BombPlane_Client.Assets;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading;
using System.Windows.Threading;

namespace BombPlane_Client.Views
{
    /// <summary>
    /// GameHallWindow.xaml 的交互逻辑
    /// </summary>
    public partial class GameHallWindow : Window
    {
        private int clicked_user;
        public List<User> users = new List<User>();
        public bool should_close = false;
        public ClientInfo.message_type current_message = ClientInfo.message_type.no_message;

        public GameHallWindow()
        {
            InitializeComponent();

            Init_Online_Users();

            Thread queue_dealer = new Thread(Listen_Invite_Queue) { IsBackground = true };
            queue_dealer.Start();
            Thread message_listener = new Thread(Listen_Messages) { IsBackground = true };
            message_listener.Start();
            ClientInfo.client_clock.timer.Tick += Askalive_tick;
            ClientInfo.client_clock.timer.Tick += Recvalive_tick;
            ClientInfo.client_clock.timer.Start();

            ImageBrush ib = new ImageBrush();
            ib.ImageSource = new BitmapImage(new Uri("../../Assets/Images/background.png", UriKind.Relative));
            this.Background = ib;
        }

        public void Listen_Messages()
        {
            Console.WriteLine("监听GameHall窗体消息的线程已经开启");
            while (true)
            {
                if (ClientInfo.messages.Count == 0)
                {
                    Thread.Sleep(100);
                    continue;
                }
                else
                {
                    //Console.WriteLine("踢我");
                    ClientInfo.message_type message_;
                    ClientInfo.messages.TryDequeue(out message_);
                    //ClientInfo.message_type message = ClientInfo.messages.First();
                    switch (message_)
                    {
                        case ClientInfo.message_type.kick:
                            Dispatcher.BeginInvoke(new Action(() =>
                            {
                                current_message = ClientInfo.message_type.kick;
                                Pop_Message_Window("已经被服务器踢出");
                            }));
                            
                            break;
                        default:
                            break;
                    }
                }
            }
        }
        public void Listen_Invite_Queue()
        {
            while (true)
            {
                if (ClientInfo.send_invite_response_user.Count != 0)
                {
                    User user;
                    ClientInfo.send_invite_response_user.TryDequeue(out user);
                    if (user.invite == User.invite_state.accept)
                    {
                        Start_Mainwindow();
                    }
                    else
                    {
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            Pop_InviteDeny_Window(user.username);
                        }));
                        continue;
                    }
                    //break;
                }
                if (ClientInfo.send_invite_user.Count == 0)
                {
                    Thread.Sleep(500);
                    continue;
                }
                else
                {
                    User user;
                    ClientInfo.send_invite_user.TryDequeue(out user);
                    Pop_Invite_Window(user.username);
                }
                Thread.Sleep(100);
            }
        }
        private void GameHallWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                InvitePopup.IsOpen = false;
                // 游戏菜单
                EscPopup.IsOpen = !EscPopup.IsOpen;
                InviteNotification.IsOpen = false;
            }
        }


        /// <summary>
        /// 计时器的事件处理方法
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Askalive_tick(object sender, EventArgs e)
        {
            ClientInfo.client_clock.last_req_time++;
            Console.WriteLine("last_request_time is " + ClientInfo.client_clock.last_req_time);
            if (ClientInfo.client_clock.last_req_time >= Time.SendKeepAlive)
            {
                // send keepalive

                NetworkClient.network.KeepaliveRequest();
                ClientInfo.client_clock.last_req_time = 0;
                ClientInfo.client_clock.req_sent = true;

            }
        }

        private void Recvalive_tick(object sender, EventArgs e)
        {
            if (ClientInfo.client_clock.req_sent)
            {
                ClientInfo.client_clock.last_rsp_time++;
                Console.WriteLine("last_response_time is " + ClientInfo.client_clock.last_rsp_time);
                if (ClientInfo.client_clock.last_rsp_time >= Time.KeepAliveResponse)
                {
                    // send keepalive
                    MessageBox.Show("已与服务器断开连接，请检查网络设置");
                    Application.Current.Shutdown();

                }
            }
        }



        //private StackPanel Add_Single_User(string username, int id)
        //{
        //    StackPanel stackPanel = new StackPanel();
        //    stackPanel.Orientation = Orientation.Horizontal;
        //    Thickness thickness = new Thickness();
        //    thickness.Left = 10;
        //    thickness.Right = 10;
        //    thickness.Top = 10;
        //    stackPanel.Margin = thickness;
        //    TextBlock id_block = new TextBlock();
        //    Thickness thickness_child = new Thickness();
        //    thickness_child.Right = 10;
        //    thickness_child.Top = 10;
        //    id_block.FontSize = 16;
        //    id_block.Text = id.ToString();
        //    id_block.Margin = thickness_child;
        //    stackPanel.Children.Add(id_block);

        //    TextBlock name_block = new TextBlock();
        //    name_block.Text = username.ToString();
        //    name_block.Margin = thickness_child;
        //    name_block.FontSize = 16;
        //    stackPanel.Children.Add(name_block);
        //    return stackPanel;
        //}
        //private StackPanel Add_Single_User_Avatar(string username, int userid, int panel_id)
        //{
        //    Thickness thickness = new Thickness();
        //    thickness.Left = 10;
        //    thickness.Right = 10;
        //    thickness.Top = 20;
        //    StackPanel stackPanel = new StackPanel();
        //    stackPanel.Name = "avatar_panel_" + panel_id.ToString();
        //    stackPanel.Margin = thickness;
        //    stackPanel.MouseRightButtonDown += Invite_Menu_Action;
        //    Image avatar = new Image();
        //    avatar.Height = 40;
        //    avatar.Width = 40;
        //    BitmapImage bi = new BitmapImage();
        //    bi.BeginInit();
        //    bi.UriSource = new Uri("../Assets/Images/avatar.png", UriKind.Relative);
        //    bi.EndInit();
        //    //avatar.Stretch = Stretch.Fill;
        //    avatar.Source = bi;
        //    stackPanel.Children.Add(avatar);
        //    TextBlock textBlock = new TextBlock();
        //    textBlock.Text = username;

        //    textBlock.Margin = thickness;
        //    textBlock.FontSize = 16;
        //    stackPanel.Children.Add(textBlock);
        //    return stackPanel;
        //}

        private void Invite_Menu_Action(object sender, MouseButtonEventArgs e)
        {
            InvitePopup.IsOpen = false;
            StackPanel stackPanel = sender as StackPanel;
            TextBlock textBlock = stackPanel.Children[2] as TextBlock;
            Console.WriteLine(textBlock.Text);
            clicked_user = int.Parse(textBlock.Text);
            //Console.WriteLine(clicked_user.ToString());
            //for (int i = 0; i < users.Count; i++)
            //{
            //    if (users[i].userID == int.Parse(textBlock.Text))
            //    {
            //        clicked_user = i;
            //    }
            //}
            Console.WriteLine("clicked user: " + clicked_user.ToString());
            //InvitePopup.Placement = System.Windows.Controls.Primitives.PlacementMode.Mouse;
            InvitePopup.IsOpen = true;

            //InvitePopup.IsOpen = false;
            //StackPanel stackPanel = sender as StackPanel;
            //clicked_user = int.Parse(stackPanel.Name.Substring(13));

            //Console.WriteLine(clicked_user.ToString());
            //InvitePopup.PlacementTarget = stackPanel;
            //InvitePopup.IsOpen = true;
        }

        private void Init_Online_Users()
        {
            int i = 0;
            foreach (KeyValuePair<int, User> pair in ClientInfo.online_users)
            {
                users.Add(pair.Value);
                i++;
            }
            avatar_items.ItemsSource = ClientInfo.online_user_obser;
            users_grid.DataContext = ClientInfo.online_user_obser;
            //Console.WriteLine("Bag num: " + ClientInfo.online_user_bag.Count.ToString());
        }

        private void Quit_Button_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Invite_Button_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("dict count: " + ClientInfo.online_users.Count.ToString());
            foreach (KeyValuePair<int, User> pair in ClientInfo.online_users)
            {
                Console.WriteLine("id:" + pair.Key.ToString());
            }
            User user = ClientInfo.online_users[clicked_user];
            Console.WriteLine(user.userID);
            NetworkClient.network._InviteRequest(user.userID);
            InvitePopup.IsOpen = false;
        }

        private void Accept_Invite_Button_Click(object sender, RoutedEventArgs e)
        {
            NetworkClient.network._InviteResponse(true, ClientInfo.rival_user.userID);
            _start_Mainwindow();
        }
        private void Deny_Invite_Button_Click(object sender, RoutedEventArgs e)
        {
            NetworkClient.network._InviteResponse(false, ClientInfo.rival_user.userID);
            InviteNotification.IsOpen = false;
        }
        public void Start_Mainwindow()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                _start_Mainwindow();
            }));
        }
        public void _start_Mainwindow()
        {
            // 将游戏状态变为放置飞机坐标
            GameUtils.SetGameStatus(ClientInfo.game_status.draw_plane);
            InviteNotification.IsOpen = false;
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
            //Close();
        }

        

        private void _pop_invite_window(string name)
        {
            //InviteNotification.Placement = System.Windows.Controls.Primitives.PlacementMode.Mouse;
            InviteNoti_Text.Text = "收到了来自用户" + name + "的邀请";
            InviteNotification.IsOpen = true;
        }
        public void Pop_InviteDeny_Window(string name)
        {
            Message_Text.Text = "用户" + name + "拒绝了您的邀请！";
            MessageNotification.IsOpen = true;
        }
        public void Pop_Invite_Window(string name)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                _pop_invite_window(name);
            }));
        }
        protected override void OnClosed(EventArgs e)
        {
            NetworkClient.network._QuitResponse();
            base.OnClosed(e);
        }
        
        public void Pop_Message_Window(string str)
        {
            Message_Text.Text = str;
            MessageNotification.IsOpen = true;
        }

        private void IKnow_Button_Click(object sender, RoutedEventArgs e)
        {
            MessageNotification.IsOpen = false;
            if (current_message == ClientInfo.message_type.kick)
            {
                var cur = Application.Current;
                cur.Shutdown();
            }
        }


    }
}
