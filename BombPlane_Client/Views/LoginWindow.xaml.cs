using BombPlane_Client.Models;
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
using BombplaneProto;

using BombPlane_Client.Assets;
using BombPlane_Client.Tools;
using System.Threading;
using System.Windows.Threading;

namespace BombPlane_Client.Views
{
    /// <summary>
    /// LoginWindow.xaml 的交互逻辑
    /// </summary>
    public partial class LoginWindow : Window
    {
        public bool auto_login { get; set; }
        public bool remember_username { get; set; }


        #region 
        /// <summary>
        /// 队列计时器
        /// </summary>
        private DispatcherTimer animationTimer;
        #endregion
        public static TimingModel login_timer = new TimingModel();
        Thread login_dealer;

        public LoginWindow()
        {


            InitializeComponent();
            auto_login = false;
            remember_username = false;

            login_timer.timer.Tick += Login_Tick;

        }


        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                LoginAction();
            }
        }

        private void LoginButtonClick(object sender, RoutedEventArgs e)
        {
            LoginAction();
        }

        private void LoginAction()
        {
            string input_username = username.Text;
            string input_passwd = passwd.Password;
            if (input_username == "")
            {
                MessageBox.Show("请输入用户名");
                return;
            }
            if (input_passwd == "")
            {
                MessageBox.Show("请输入密码");
                return;
            }

            LoadGrid.Visibility = Visibility.Visible;
            login_timer.last_req_time = 0;
            login_timer.timer.Start();

            ClientInfo.username_passwd.Enqueue(input_passwd);
            ClientInfo.username_passwd.Enqueue(input_username);

            login_dealer = new Thread(_Login) { IsBackground = true };
            login_dealer.Start();


        }

        private void Login_Tick(object sender, EventArgs e)
        {
            login_timer.last_req_time++;
            if (login_timer.last_req_time > 20)
            {
                MessageBox.Show("服务器连接超时，请检查网络设置！");
                ClientInfo.login_state = ClientInfo.LoginState.not_logged_in;
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    LoadGrid.Visibility = Visibility.Collapsed;
                    login_timer.last_req_time = 0;
                    login_timer.timer.Stop();

                    login_dealer.Abort();

                }));

            }
        }


        private void _Login()
        {
            if(ClientInfo.username_passwd.Count == 2)
            {
                string username;
                string password;

                ClientInfo.username_passwd.TryDequeue(out password);
                ClientInfo.username_passwd.TryDequeue(out username);

                Login(username, password);
            }
        }

        private void Login(string input_username, string input_passwd)
        {
            Console.WriteLine("username is " + input_username + " ,password is " + input_passwd);
            Console.WriteLine("remember is " + remember_username.ToString());
            Console.WriteLine("auto is " + auto_login.ToString());
            string crypted_passwd = MD5Tools.GetMD5(input_passwd);

            int port = int.Parse(ConfigurationManager.AppSettings["network_port"]);
            string ip = ConfigurationManager.AppSettings["network_ip"];
            if (!NetworkClient.GetIsConnected())
            {
                NetworkClient.StartNetwork(ip, port);
            }
            NetworkClient.network.LoginRequest(input_username, crypted_passwd);
            while (ClientInfo.login_state != ClientInfo.LoginState.login_success)
            {
                if (ClientInfo.login_state == ClientInfo.LoginState.log_error)
                {
                    MessageBox.Show("登录错误，请检查用户名、密码是否正确！");
                    ClientInfo.login_state = ClientInfo.LoginState.not_logged_in;
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        LoadGrid.Visibility = Visibility.Collapsed;
                        login_timer.last_req_time = 0;
                        login_timer.timer.Stop();
                    }));
                    return;
                }
                Thread.Sleep(10);
            }
            ClientInfo.self_user.userState = User.user_state.free;
            ClientInfo.self_user.username = input_username;

            Dispatcher.BeginInvoke(new Action(() =>
            {
                ClientInfo.hall = new HallWindow();
                ClientInfo.hall.Show();
                login_timer.last_req_time = 0;
                login_timer.timer.Stop();

                Close();
                LoadGrid.Visibility = Visibility.Collapsed;
            }));
        }

        private void Remember_Name_Checked(object sender, RoutedEventArgs e)
        {
            remember_username = (RememberName_CheckBox.IsChecked == true);
        }

        private void Auto_Login_Checked(object sender, RoutedEventArgs e)
        {
            auto_login = (AutoLogin_CheckBox.IsChecked == true);
        }

        private void AboutUs_Button_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/TiOrg");
        }




        #region 加载界面初始化方法

        public void LoadingWait()
        {
            animationTimer = new DispatcherTimer(
                DispatcherPriority.ContextIdle, Dispatcher);
            animationTimer.Interval = new TimeSpan(0, 0, 0, 0, 90);
        }
        #endregion

        #region 执行方法

        private void Start()
        {
            animationTimer.Tick += HandleAnimationTick;
            animationTimer.Start();
        }

        private void Stop()
        {
            animationTimer.Stop();
            animationTimer.Tick -= HandleAnimationTick;
        }

        private void HandleAnimationTick(object sender, EventArgs e)
        {
            SpinnerRotate.Angle = (SpinnerRotate.Angle + 36) % 360;
        }

        private void HandleLoaded(object sender, RoutedEventArgs e)
        {
            const double offset = Math.PI;
            const double step = Math.PI * 2 / 10.0;

            SetPosition(C0, offset, 0.0, step);
            SetPosition(C1, offset, 1.0, step);
            SetPosition(C2, offset, 2.0, step);
            SetPosition(C3, offset, 3.0, step);
            SetPosition(C4, offset, 4.0, step);
            SetPosition(C5, offset, 5.0, step);
            SetPosition(C6, offset, 6.0, step);
            SetPosition(C7, offset, 7.0, step);
            SetPosition(C8, offset, 8.0, step);
        }

        private void SetPosition(Ellipse ellipse, double offset,
            double posOffSet, double step)
        {
            ellipse.SetValue(Canvas.LeftProperty, 60.0
                + Math.Sin(offset + posOffSet * step) * 60.0);

            ellipse.SetValue(Canvas.TopProperty, 60
                + Math.Cos(offset + posOffSet * step) * 60.0);
        }

        private void HandleUnloaded(object sender, RoutedEventArgs e)
        {
            Stop();
        }

        private void HandleVisibleChanged(object sender,
            DependencyPropertyChangedEventArgs e)
        {
            bool isVisible = (bool)e.NewValue;
            if (animationTimer == null)
            {
                LoadingWait();
            }
            if (isVisible)
                Start();
            else
                Stop();
        }

        #endregion
    }
}
