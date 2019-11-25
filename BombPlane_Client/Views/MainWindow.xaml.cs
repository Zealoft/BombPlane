using BombPlane_Client.Models;
using System;
using System.Collections.Generic;
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
using System.Configuration;

using BombPlane_Client.Assets;
using BombPlane_Client.Tools;
using System.Threading;

namespace BombPlane_Client.Views
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        // 定义全局变量
        private int SINGLE_AREA_SIZE;
        private bool begin_flag = false;
        private const int BLOCK_NUM = 10;
        private const int GAME_AREA_SIZE = 500;

        // me
        // 0: nothing
        // 1: plane
        private bool[][] is_selected = new bool[BLOCK_NUM][];

        // enemy
        // 0: unknown
        // 1: hit
        // 2: destoryed
        // 3: revealed
        private bool[][] enemy_block = new bool[BLOCK_NUM][];

        private int max_selected_num = 1;
        private int selected_num = 0;
        private bool is_bombing = true;
        private const string selected_uid = "selected_area";
        private const string mouse_in_uid = "current_area";
        private const string my_plane_uid = "my_plane_area";
        private List<string> selected_uids = new List<string>();
        private List<PlaneLocator> planeLocators = new List<PlaneLocator>();
        private int[][] position = new int[BLOCK_NUM][];
        private int clicked_X, clicked_Y, current_X, current_Y;
        private int clicked_X_Rival, clicked_Y_Rival, current_X_Rival, current_Y_Rival;
        private bool mouse_is_left = true;
        private ChessBoard my_chessBoard = new ChessBoard();
        private ChessBoard rival_chessBoard = new ChessBoard();
        private TimingModel timing_model = new TimingModel();
        private PlaneLocator selected_planeLocator = new PlaneLocator();
        private Coordinate selected_coordinate = new Coordinate();
        // 不确定在主线程中使用是否会产生问题，最好不用
        private ClientInfo.game_status current_status;
        private int my_board_times = 0;
        private int rival_board_times = 0;
        private Plane plane = new Plane();
        private int plane_locator_index = 0;
        public MainWindow()
        {
            InitializeComponent();
            Left = SystemParameters.PrimaryScreenWidth - Width - 10;
            Top = SystemParameters.PrimaryScreenHeight - Height - 80;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            SINGLE_AREA_SIZE = GAME_AREA_SIZE / BLOCK_NUM;
            PaintBoard(true);
            PaintBoard(false);
            InitPosition();

            Submit_Button.Opacity = 0;
            Send_Button.Opacity = 0;
            Submit_Button.IsEnabled = false;
            Send_Button.IsEnabled = false;
            selected_uids.Add("selected_area_1");
            selected_uids.Add("selected_area_2");
            selected_uids.Add("selected_area_3");
            current_status = GameUtils.GetGameStatus();
            Thread gamestatus_listener = new Thread(Listen_Gamestatus_Change) { IsBackground = true };
            gamestatus_listener.Start();
            Thread chessboard_listener = new Thread(Listen_Chessboard) { IsBackground = true };
            chessboard_listener.Start();
            //int port = int.Parse(ConfigurationManager.AppSettings["network_port"]);
            //string ip = ConfigurationManager.AppSettings["network_ip"];
            //network_model = new NetworkModel(ip, port);
        }

        private bool is_game_starting(ClientInfo.game_status status)
        {
            if (current_status == ClientInfo.game_status.waiting_drawing && status == ClientInfo.game_status.self_guessing)
            {
                return true;
            }
            else if (current_status == ClientInfo.game_status.draw_plane && status == ClientInfo.game_status.self_guessing)
            {
                return true;
            }
            else if (current_status == ClientInfo.game_status.waiting_drawing && status == ClientInfo.game_status.rival_guessing)
            {
                return true;
            }
            else if (current_status == ClientInfo.game_status.draw_plane && status == ClientInfo.game_status.rival_guessing)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public string Get_Block_Uid(int x, int y, bool is_left = true)
        {
            string str = "";
            if (is_left)
            {
                str += "self_block";
            }
            else
            {
                str += "rival_block";
            }
            str += y.ToString() + "_" + x.ToString();
            return str;
        }

        public void Draw_Point(int x, int y, ChessBoard.point_state state, bool is_left = true)
        {
            string block_uid = Get_Block_Uid(x, y, is_left);
            switch (state)
            {
                case ChessBoard.point_state.destroy:
                    Add_Block_Color(x, y, TiColors.Red, block_uid, is_left);
                    break;
                case ChessBoard.point_state.hit:
                    Add_Block_Color(x, y, TiColors.Yellow, block_uid, is_left);
                    break;
                case ChessBoard.point_state.miss:
                    Add_Block_Color(x, y, TiColors.BlueGrey, block_uid, is_left);
                    break;
                case ChessBoard.point_state.unknown:
                    Delete_Block_Color(block_uid, is_left);
                    break;
                case ChessBoard.point_state.empty:
                    Delete_Block_Color(block_uid, is_left);
                    break;
            }
        }

        /// <summary>
        /// 根据本类存储的Chessboard和共有Chessboard的区别绘制棋盘
        /// </summary>
        /// <param name="is_left"></param>
        //public void Draw_Chessboard(bool is_left = true)
        //{
        //    if (is_left)
        //    {
        //        Thread.Sleep(100);
        //        //ChessBoard public_my_board = GameUtils.GetChessBoard(ChessBoard.whose_board.my_board);
        //        for (int i = 0; i < 10; i++)
        //        {
        //            for (int j = 0; j < 10; j++)
        //            {
        //                if (public_my_board.chessboard[i][j] != my_chessBoard.chessboard[i][j])
        //                {
        //                    my_chessBoard.chessboard[i][j] = public_my_board.chessboard[i][j];
        //                    Dispatcher.BeginInvoke(new Action(() =>
        //                    {
        //                        Draw_Point(j, i, public_my_board.chessboard[i][j], is_left);
        //                    }));
        //                }
        //            }
        //        }
        //    }
        //    else
        //    {
        //        Thread.Sleep(100);
        //        ChessBoard public_rival_board = GameUtils.GetChessBoard(ChessBoard.whose_board.rival_board);
        //        for (int i = 0; i < 10; i++)
        //        {
        //            for (int j = 0; j < 10; j++)
        //            {
        //                if (public_rival_board.chessboard[i][j] != my_chessBoard.chessboard[i][j])
        //                {
        //                    rival_chessBoard.chessboard[i][j] = public_rival_board.chessboard[i][j];
        //                    Dispatcher.BeginInvoke(new Action(() =>
        //                    {
        //                        Draw_Point(j, i, public_rival_board.chessboard[i][j], is_left);
        //                    }));
        //                }
        //            }
        //        }
        //    }
        //}
        /// <summary>
        /// 在一个单独的线程中监听游戏状态是否发生改变
        /// 控制某些UI发生改变
        /// </summary>
        private void Listen_Gamestatus_Change()
        {
            Console.WriteLine("监听游戏状态的线程已经开启...");
            while (true)
            {
                ClientInfo.game_status status = GameUtils.GetGameStatus();
                if (status == current_status)
                {
                    Thread.Sleep(500);
                    continue;
                }
                else
                {
                    Console.WriteLine("当前的游戏状态：" + status.ToString());
                    if (is_game_starting(status))
                    {
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            Init_Game_Chessboard();
                        }));
                    }
                    current_status = status;

                    // 此处写状态改变后需要改变的UI
                    switch (status)
                    {
                        case ClientInfo.game_status.draw_plane:
                            Dispatcher.BeginInvoke(new Action(() =>
                            {
                                GameStatus.Text = "设定我方飞机位置";
                            }));
                            break;
                        case ClientInfo.game_status.rival_guessing:
                            Dispatcher.BeginInvoke(new Action(() =>
                            {
                                GameStatus.Text = "对方回合";
                            }));
                            break;
                        case ClientInfo.game_status.self_guessing:
                            Dispatcher.BeginInvoke(new Action(() =>
                            {
                                GameStatus.Text = "我方回合";
                            }));
                            break;
                        case ClientInfo.game_status.waiting:
                            Dispatcher.BeginInvoke(new Action(() =>
                            {
                                GameStatus.Text = "正在等待服务器响应";
                            }));
                            break;
                        case ClientInfo.game_status.waiting_drawing:
                            Dispatcher.BeginInvoke(new Action(() =>
                            {
                                GameStatus.Text = "等待对方指定飞机位置";
                            }));
                            break;
                        case ClientInfo.game_status.self_winning:
                            Dispatcher.BeginInvoke(new Action(() =>
                            {
                                GameStatus.Text = "我方获胜！";
                            }));
                            break;
                        case ClientInfo.game_status.rival_winning:
                            Dispatcher.BeginInvoke(new Action(() =>
                            {
                                GameStatus.Text = "对方获胜！";
                            }));
                            break;
                    }
                    Thread.Sleep(100);
                }
            }
        }

        private void Listen_Chessboard()
        {
            Console.WriteLine("监听游戏棋盘改变的线程已经开启");
            while (true)
            {
                if (ClientInfo.my_points.Count == 0 && ClientInfo.rival_points.Count == 0)
                {
                    Thread.Sleep(100);
                    continue;
                }
                else
                {
                    while (ClientInfo.my_points.Count > 0)
                    {
                        ChessBoard.Chessboard_Point point;
                        ClientInfo.my_points.TryDequeue(out point);
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            Draw_Point(point.x, point.y, point.state, true);
                        }));
                    }
                    while (ClientInfo.rival_points.Count > 0)
                    {
                        ChessBoard.Chessboard_Point point;
                        ClientInfo.rival_points.TryDequeue(out point);
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            Draw_Point(point.x, point.y, point.state, false);
                        }));
                    }
                }
                Thread.Sleep(100);
            }
        }
        private void PaintBoard(bool is_left = true)
        {
            if (is_left)
            {
                Play_Ground.Background = new SolidColorBrush(Color.FromRgb(255, 255, 255));
            }
            else
            {
                Play_Ground_Rival.Background = new SolidColorBrush(Color.FromRgb(255, 255, 255));
            }
            for (int i = 0; i < BLOCK_NUM; i++)
            {
                Line xLine = new Line();
                Line yLine = new Line();
                Line x = new Line();
                Line y = new Line();
                xLine.Stroke = new SolidColorBrush(Color.FromRgb(0, 0, 0));
                yLine.Stroke = new SolidColorBrush(Color.FromRgb(0, 0, 0));
                xLine.X1 = 0;
                xLine.X2 = GAME_AREA_SIZE - 2;
                yLine.Y1 = 0;
                yLine.Y2 = GAME_AREA_SIZE - 2;
                xLine.Y1 = i * SINGLE_AREA_SIZE;
                xLine.Y2 = i * SINGLE_AREA_SIZE;
                yLine.X1 = i * SINGLE_AREA_SIZE;
                yLine.X2 = i * SINGLE_AREA_SIZE;

                //x.Stroke = new SolidColorBrush(Color.FromRgb(0, 0, 0));
                //y.Stroke = new SolidColorBrush(Color.FromRgb(0, 0, 0));
                //x.X1 = 0;
                //x.X2 = GAME_AREA_SIZE - 2;
                //y.Y1 = 0;
                //y.Y2 = GAME_AREA_SIZE - 2;
                //x.Y1 = i * SINGLE_AREA_SIZE;
                //x.Y2 = i * SINGLE_AREA_SIZE;
                //y.X1 = i * SINGLE_AREA_SIZE;
                //y.X2 = i * SINGLE_AREA_SIZE;
                if (is_left)
                {
                    Play_Ground.Children.Add(xLine);
                    Play_Ground.Children.Add(yLine);
                }
                else
                {
                    Play_Ground_Rival.Children.Add(xLine);
                    Play_Ground_Rival.Children.Add(yLine);
                }
            }
        }


        private void RePaintBoard(bool is_left = true)
        {
            if (is_left)
                Play_Ground.Children.Clear();
            else
                Play_Ground_Rival.Children.Clear();
            PaintBoard(is_left);
        }

        private void InitPosition()
        {
            for (int i = 0; i < 10; i++)
            {
                position[i] = new int[BLOCK_NUM];
                is_selected[i] = new bool[BLOCK_NUM];
                enemy_block[i] = new bool[BLOCK_NUM];
                for (int j = 0; j < BLOCK_NUM; j++)
                {
                    position[i][j] = 0;
                    is_selected[i][j] = false;
                    enemy_block[i][j] = false;
                }
            }
            clicked_X = -1;
            clicked_Y = -1;
            current_X = -1;
            current_Y = -1;
        }

        private void Delete_Block_Color(string uid, bool is_left = true)
        {
            if (is_left)
            {
                foreach (UIElement ui in Play_Ground.Children)
                {
                    if (ui.Uid == uid)
                    {
                        Play_Ground.Children.Remove(ui);
                        break;
                    }
                }
            }
            else
            {
                foreach (UIElement ui in Play_Ground_Rival.Children)
                {
                    if (ui.Uid == uid)
                    {
                        Play_Ground_Rival.Children.Remove(ui);
                        break;
                    }
                }
            }
        }

        private void Add_Block_Color(int x, int y, Color color, string uid, bool is_left = true)
        {
            Rectangle current_area = new Rectangle();
            current_area.Opacity = 0.5;
            current_area.Uid = uid;
            if (x == BLOCK_NUM - 1)
                current_area.Width = SINGLE_AREA_SIZE - 4;
            else
                current_area.Width = SINGLE_AREA_SIZE - 1;
            if (y == BLOCK_NUM - 1)
                current_area.Height = SINGLE_AREA_SIZE - 4;
            else
                current_area.Height = SINGLE_AREA_SIZE - 2;

            current_area.Fill = new SolidColorBrush(color);
            Canvas.SetLeft(current_area, x * SINGLE_AREA_SIZE + 1);
            Canvas.SetTop(current_area, y * SINGLE_AREA_SIZE + 1);
            if (is_left)
                Play_Ground.Children.Add(current_area);
            else
                Play_Ground_Rival.Children.Add(current_area);
        }

        private void Init_Game_Chessboard()
        {

            //int[] plane_x = new int[9];
            //int[] plane_y = new int[9];
            //for (int i = 0; i < 3; i++)
            //{
            //    PlaneLocator planeLocator = planeLocators[i];
            //    plane_x[i * 3] = planeLocator.Pos1.X;
            //    plane_x[i * 3 + 1] = planeLocator.Pos2.X;
            //    plane_x[i * 3 + 2] = planeLocator.Pos3.X;
            //    plane_y[i * 3] = planeLocator.Pos1.Y;
            //    plane_y[i * 3 + 1] = planeLocator.Pos2.Y;
            //    plane_y[i * 3 + 2] = planeLocator.Pos3.Y;
            //}
            //for (int i = 0; i < 10; i++)
            //    for (int j = 0; j < 10; j++)
            //    {
            //        bool has_plane = false;
            //        for (int k = 0; k < 9; k++)
            //        {
            //            if (i == plane_y[k] && j == plane_x[k])
            //            {
            //                Add_Block_Color(j, i, TiColors.Pink, my_plane_uid, false);
            //                has_plane = true;
            //                break;
            //            }
            //        }
            //        if (has_plane)
            //            continue;
            //    }
            //RePaintBoard(true);
        }
        /// <summary>
        /// 返回飞机色块的uid列表
        /// </summary>
        private List<string> Show_Plane(int current_X, int current_Y, bool is_fixed = false, bool is_left = true)
        {
            List<string> uids = new List<string>();
            if (plane.Is_Outbound(BLOCK_NUM - 1) || Is_Overlapped(is_left))
            {
                for (int i = 0; i < 10; i++)
                {
                    int x = current_X + plane.plane_shape[(int)plane.Direction][i].x;
                    int y = current_Y + plane.plane_shape[(int)plane.Direction][i].y;
                    if (x > BLOCK_NUM - 1 || x < 0 || y > BLOCK_NUM - 1 || y < 0)
                        continue;
                    else
                    {
                        Add_Block_Color(x, y, Color.FromRgb(125, 125, 125), mouse_in_uid + i.ToString(), is_left);
                        uids = uids.Append(mouse_in_uid + i.ToString()).ToList<string>();
                    }

                }
            }
            else
            {
                for (int i = 0; i < 10; i++)
                {
                    int x, y;
                    if (is_left)
                    {
                        x = current_X + plane.plane_shape[(int)plane.Direction][i].x;
                        y = current_Y + plane.plane_shape[(int)plane.Direction][i].y;
                    }
                    else
                    {
                        x = current_X + plane.plane_shape[(int)plane.Direction][i].x;
                        y = current_Y + plane.plane_shape[(int)plane.Direction][i].y;
                    }
                    string block_uid = Get_Block_Uid(x, y, is_left);
                    if (is_fixed)
                    {
                        Add_Block_Color(x, y, Color.FromRgb(255, 64, 129), block_uid + "-guess", is_left);
                        uids = uids.Append(block_uid + "-guess").ToList<string>();
                    }

                    else
                    {
                        Add_Block_Color(x, y, Color.FromRgb(0, 176, 255), mouse_in_uid + i.ToString(), is_left);
                        uids = uids.Append(mouse_in_uid + i.ToString()).ToList<string>();
                    }
                }
            }
            return uids;
        }

        /// <summary>
        /// 删除鼠标移动过程中的飞机
        /// </summary>
        /// <param name="is_left"></param>
        private void Delete_Plane(bool is_left = true)
        {
            if (is_left)
            {
                for (int i = 0; i < 10; i++)
                {
                    // 删除之前的色块
                    foreach (UIElement ui in Play_Ground.Children)
                    {

                        if (ui.Uid == mouse_in_uid + i.ToString())
                        {
                            Play_Ground.Children.Remove(ui);
                            break;
                        }
                    }
                }
            }
            else
            {
                for (int i = 0; i < 10; i++)
                {
                    // 删除之前的色块
                    foreach (UIElement ui in Play_Ground_Rival.Children)
                    {

                        if (ui.Uid == mouse_in_uid + i.ToString())
                        {
                            Play_Ground_Rival.Children.Remove(ui);
                            break;
                        }
                    }
                }
            }
        }

        private void Delete_Plane(List<string> uids, bool is_left)
        {
            foreach (string uid in uids)
                Delete_Block_Color(uid, is_left);
        }

        private void Refresh_Plane(bool is_left = true)
        {
            Delete_Plane(is_left);
            Show_Plane(plane.center.x, plane.center.y, false, is_left);
        }

        private void Set_Selected(bool is_left = true)
        {
            for (int i = 0; i < 10; i++)
            {
                if (is_left)
                {
                    int x = current_X + plane.plane_shape[(int)plane.Direction][i].x;
                    int y = current_Y + plane.plane_shape[(int)plane.Direction][i].y;

                    is_selected[x][y] = true;
                }
                else
                {
                    int x = current_X_Rival + plane.plane_shape[(int)plane.Direction][i].x;
                    int y = current_Y_Rival + plane.plane_shape[(int)plane.Direction][i].y;

                    enemy_block[x][y] = true;
                }
            }
        }

        private bool Is_Overlapped(bool is_left = true)
        {
            for (int i = 0; i < 10; i++)
            {
                if (is_left)
                {
                    int x = current_X + plane.plane_shape[(int)plane.Direction][i].x;
                    int y = current_Y + plane.plane_shape[(int)plane.Direction][i].y;

                    if (is_selected[x][y])
                        return true;
                }
                else
                {
                    int x = current_X_Rival + plane.plane_shape[(int)plane.Direction][i].x;
                    int y = current_Y_Rival + plane.plane_shape[(int)plane.Direction][i].y;

                    if (enemy_block[x][y])
                        return true;
                }
            }
            return false;
        }

        private void Play_Ground_LeftMouseDown(object sender, MouseButtonEventArgs e)
        {
            Point clicked_point = e.GetPosition(Play_Ground);
            clicked_X = (int)(clicked_point.X / SINGLE_AREA_SIZE);
            clicked_Y = (int)(clicked_point.Y / SINGLE_AREA_SIZE);

            plane.Move_Plane(clicked_X, clicked_Y);
            if (plane.Is_Outbound(BLOCK_NUM - 1))
                return;

            ClientInfo.game_status current_status = GameUtils.GetGameStatus();
            if (current_status == ClientInfo.game_status.draw_plane)
            {
                max_selected_num = 3;

                if (selected_num < max_selected_num)
                {
                    if (!Is_Overlapped())
                    {
                        Show_Plane(clicked_X, clicked_Y, true);
                        Set_Selected();
                        PlaneLocator planeLocator = new PlaneLocator();
                        Coordinate co1 = new Coordinate();
                        co1.X = clicked_X + plane.plane_shape[(int)plane.Direction][0].x;
                        co1.Y = clicked_Y + plane.plane_shape[(int)plane.Direction][0].y;
                        //Console.WriteLine("第一个坐标的x：" + co1.X.ToString() + "，第一个坐标的y：" + co1.Y.ToString());
                        Coordinate co2 = new Coordinate();
                        co2.X = clicked_X + plane.plane_shape[(int)plane.Direction][1].x;
                        co2.Y = clicked_Y + plane.plane_shape[(int)plane.Direction][1].y;
                        //Console.WriteLine("第二个坐标的x：" + co2.X.ToString() + "，第二个坐标的y：" + co2.Y.ToString());
                        Coordinate co3 = new Coordinate();
                        co3.X = clicked_X + plane.plane_shape[(int)plane.Direction][2].x;
                        co3.Y = clicked_Y + plane.plane_shape[(int)plane.Direction][2].y;
                        //Console.WriteLine("第三个坐标的x：" + co3.X.ToString() + "，第三个坐标的y：" + co3.Y.ToString());
                        planeLocator.Pos1 = co1;
                        planeLocator.Pos2 = co2;
                        planeLocator.Pos3 = co3;
                        planeLocators.Add(planeLocator);
                        selected_num++;
                        if (selected_num == max_selected_num)
                        {
                            Submit_Button.Opacity = 1;
                            Submit_Button.IsEnabled = true;
                        }
                        //Add_Plane_Chessboard();
                    }
                }
                // 已经选了三个飞机
                else
                {
                    MessageBox.Show("已经选完三架飞机的位置！");
                    return;
                }
            }
            else
            {
                return;
            }
            //bool send_status = network_model.Send_SingleCoordinate(
            //    clicked_X, clicked_Y);

        }

        private void Play_Ground_RightMouseDown(object sender, MouseButtonEventArgs e)
        {
            //Point clicked_point = e.GetPosition(Play_Ground);
            //clicked_X = (int)(clicked_point.X / SINGLE_AREA_SIZE);
            //clicked_Y = (int)(clicked_point.Y / SINGLE_AREA_SIZE);
            //ClientInfo.game_status current_status = GameUtils.GetGameStatus();
            //if (current_status == ClientInfo.game_status.self_guessing)
            //{
            //    if (is_bombing)
            //    {
            //        Delete_Block_Color(mouse_in_uid);
            //        Show_Plane(clicked_X, clicked_Y);
            //    }
            //    else
            //    {
            //        Delete_Plane();
            //        Add_Block_Color(current_X, current_Y, Color.FromRgb(255, 0, 0), mouse_in_uid);
            //    }
            //    is_bombing = !is_bombing;
            //}
        }

        private void Play_Ground_MouseEnter(object sender, MouseEventArgs e)
        {
            mouse_is_left = true;
            Point reach_point = e.GetPosition(Play_Ground);
            int reached_X = (int)(reach_point.X / SINGLE_AREA_SIZE);
            int reached_Y = (int)(reach_point.Y / SINGLE_AREA_SIZE);
            current_X = reached_X;
            current_Y = reached_Y;
            ClientInfo.game_status current_status = GameUtils.GetGameStatus();
            if (current_status == ClientInfo.game_status.draw_plane)
            {
                plane.Move_Plane(current_X, current_Y);
                Show_Plane(current_X, current_Y);
            }
            else
            {
                return;
            }

        }

        private void Play_Ground_MouseLeave(object sender, MouseEventArgs e)
        {
            ClientInfo.game_status current_status = GameUtils.GetGameStatus();
            if (current_status == ClientInfo.game_status.draw_plane)
            {
                Delete_Plane();
            }
            //else if (current_status == ClientInfo.game_status.self_guessing)
            //{
            //    if (is_bombing)
            //    {
            //        Delete_Block_Color(mouse_in_uid);
            //    }
            //    else
            //    {
            //        Delete_Plane();
            //    }
            //}
            else
            {

            }
        }

        private void Play_Ground_MouseMove(object sender, MouseEventArgs e)
        {



            Point reach_point = e.GetPosition(Play_Ground);
            int reached_X = (int)(reach_point.X / SINGLE_AREA_SIZE);
            int reached_Y = (int)(reach_point.Y / SINGLE_AREA_SIZE);

            if (reached_X == current_X && reached_Y == current_Y)
            {
                // 当前鼠标区域未发生改变
                return;
            }
            else
            {
                ClientInfo.game_status current_status = GameUtils.GetGameStatus();
                current_X = reached_X;
                current_Y = reached_Y;

                if (current_status == ClientInfo.game_status.draw_plane)
                {
                    plane.Move_Plane(current_X, current_Y);
                    Refresh_Plane();

                }
                //else if (current_status == ClientInfo.game_status.self_guessing)
                //{
                //    if (is_bombing)
                //    {
                //        Delete_Block_Color(mouse_in_uid);
                //        Add_Block_Color(current_X, current_Y, Color.FromRgb(255, 0, 0), mouse_in_uid);
                //    }
                //    else
                //    {
                //        plane.Move_Plane(current_X, current_Y);
                //        Refresh_Plane();
                //    }
                //}
                else
                {

                }
            }

        }
        
        private string old_selected_bomb_uid;
        private List<string> old_selected_guess_uids = new List<string>();
        private void Play_Ground_Rival_LeftMouseDown(object sender, MouseButtonEventArgs e)
        {
            Point clicked_point = e.GetPosition(Play_Ground_Rival);
            clicked_X_Rival = (int)((clicked_point.X) / SINGLE_AREA_SIZE);
            clicked_Y_Rival = (int)((clicked_point.Y) / SINGLE_AREA_SIZE);

            plane.Move_Plane(clicked_X_Rival, clicked_Y_Rival);
            

            ClientInfo.game_status current_status = GameUtils.GetGameStatus();
            if (current_status == ClientInfo.game_status.draw_plane)
            {
                return;
            }
            else if (current_status == ClientInfo.game_status.self_guessing)
            {
                if (is_bombing)
                {
                    //Delete_Block_Color(mouse_in_uid, false);
                    Delete_Plane(old_selected_guess_uids, false);
                    Delete_Block_Color(old_selected_bomb_uid, false);
                    Add_Block_Color(current_X_Rival, current_Y_Rival, Color.FromRgb(255, 0, 0), selected_uid, false);
                    old_selected_bomb_uid = selected_uid;
                    selected_coordinate.X = current_X_Rival;
                    selected_coordinate.Y = current_Y_Rival;
                    Send_Button.Opacity = 1;
                    Send_Button.IsEnabled = true;
                }
                else
                {
                    if (plane.Is_Outbound(BLOCK_NUM - 1))
                        return;
                    //Delete_Plane(false);
                    Delete_Plane(old_selected_guess_uids, false);
                    Delete_Block_Color(old_selected_bomb_uid, false);
                    old_selected_guess_uids = Show_Plane(clicked_X_Rival, clicked_Y_Rival, true, false);
                    Coordinate co1 = new Coordinate();
                    co1.X = clicked_X_Rival + plane.plane_shape[(int)plane.Direction][0].x;
                    co1.Y = clicked_Y_Rival + plane.plane_shape[(int)plane.Direction][0].y;
                    //Console.WriteLine("第一个坐标的x：" + co1.X.ToString() + "，第一个坐标的y：" + co1.Y.ToString());
                    Coordinate co2 = new Coordinate();
                    co2.X = clicked_X_Rival + plane.plane_shape[(int)plane.Direction][1].x;
                    co2.Y = clicked_Y_Rival + plane.plane_shape[(int)plane.Direction][1].y;
                    //Console.WriteLine("第二个坐标的x：" + co2.X.ToString() + "，第二个坐标的y：" + co2.Y.ToString());
                    Coordinate co3 = new Coordinate();
                    co3.X = clicked_X_Rival + plane.plane_shape[(int)plane.Direction][2].x;
                    co3.Y = clicked_Y_Rival + plane.plane_shape[(int)plane.Direction][2].y;
                    //Console.WriteLine("第三个坐标的x：" + co3.X.ToString() + "，第三个坐标的y：" + co3.Y.ToString());
                    selected_planeLocator.Pos1 = co1;
                    selected_planeLocator.Pos2 = co2;
                    selected_planeLocator.Pos3 = co3;
                    Send_Button.Opacity = 1;
                    Send_Button.IsEnabled = true;
                }
            }
            else
            {

            }
            //bool send_status = network_model.Send_SingleCoordinate(
            //    clicked_X, clicked_Y);

        }

        private void Play_Ground_Rival_RightMouseDown(object sender, MouseButtonEventArgs e)
        {
            Point clicked_point = e.GetPosition(Play_Ground_Rival);
            clicked_X_Rival = (int)(clicked_point.X / SINGLE_AREA_SIZE);
            clicked_Y_Rival = (int)(clicked_point.Y / SINGLE_AREA_SIZE);
            plane.Move_Plane(clicked_X_Rival, clicked_Y_Rival);
            ClientInfo.game_status current_status = GameUtils.GetGameStatus();
            if (current_status == ClientInfo.game_status.self_guessing)
            {
                if (is_bombing)
                {
                    Delete_Block_Color(mouse_in_uid, false);
                    Show_Plane(clicked_X_Rival, clicked_Y_Rival, false, false);
                }
                else
                {
                    Delete_Plane(false);
                    Add_Block_Color(current_X_Rival, current_Y_Rival, Color.FromRgb(255, 0, 0), mouse_in_uid, false);
                }
                is_bombing = !is_bombing;
            }
        }

        private void Play_Ground_Rival_MouseEnter(object sender, MouseEventArgs e)
        {
            mouse_is_left = false;
            Point reach_point = e.GetPosition(Play_Ground_Rival);
            int reached_X = (int)(reach_point.X / SINGLE_AREA_SIZE);
            int reached_Y = (int)(reach_point.Y / SINGLE_AREA_SIZE);
            current_X_Rival = reached_X;
            current_Y_Rival = reached_Y;
            ClientInfo.game_status current_status = GameUtils.GetGameStatus();
            if (current_status == ClientInfo.game_status.draw_plane)
            {
                return;
            }
            else if (current_status == ClientInfo.game_status.self_guessing)
            {
                if (is_bombing)
                {
                    Add_Block_Color(current_X_Rival, current_Y_Rival, Color.FromRgb(255, 0, 0), mouse_in_uid, false);
                }
                else
                {
                    plane.Move_Plane(current_X_Rival, current_Y_Rival);
                    Show_Plane(clicked_X_Rival, clicked_Y_Rival, false, false);
                }
            }
            else
            {

            }

        }

        private void Play_Ground_Rival_MouseLeave(object sender, MouseEventArgs e)
        {
            ClientInfo.game_status current_status = GameUtils.GetGameStatus();
            if (current_status == ClientInfo.game_status.draw_plane)
            {
                return;
            }
            else if (current_status == ClientInfo.game_status.self_guessing)
            {
                if (is_bombing)
                {
                    Delete_Block_Color(mouse_in_uid, false);
                }
                else
                {
                    Delete_Plane(false);
                }
            }
            else
            {

            }
        }

        private void Play_Ground_Rival_MouseMove(object sender, MouseEventArgs e)
        {
            Point reach_point = e.GetPosition(Play_Ground_Rival);
            int reached_X = (int)(reach_point.X / SINGLE_AREA_SIZE);
            int reached_Y = (int)(reach_point.Y / SINGLE_AREA_SIZE);
            if (reached_X == current_X_Rival && reached_Y == current_Y_Rival)
            {
                // 当前鼠标区域未发生改变
                return;
            }
            else
            {
                ClientInfo.game_status current_status = GameUtils.GetGameStatus();
                current_X_Rival = reached_X;
                current_Y_Rival = reached_Y;

                if (current_status == ClientInfo.game_status.draw_plane)
                {
                    return;

                }
                else if (current_status == ClientInfo.game_status.self_guessing)
                {
                    if (is_bombing)
                    {
                        Delete_Block_Color(mouse_in_uid, false);
                        Add_Block_Color(current_X_Rival, current_Y_Rival, Color.FromRgb(255, 0, 0), mouse_in_uid, false);
                    }
                    else
                    {
                        plane.Move_Plane(current_X_Rival, current_Y_Rival);
                        Refresh_Plane(false);
                    }
                }
                else
                {

                }
            }

        }

        /// <summary>
        /// 把初始化时设置的三架飞机存入Chessboard中
        /// </summary>
        //private void Add_Plane_Chessboard()
        //{
        //    ChessBoard public_chessboard;
        //    if (ClientInfo.my_chessboard.Count == 0)
        //        public_chessboard = new ChessBoard();
        //    else
        //        ClientInfo.my_chessboard.TryDequeue(out public_chessboard);
        //    for (int i = 0; i < 10; i++)
        //    {
        //        int x, y;
        //        x = current_X + plane.plane_shape[(int)plane.Direction][i].x;
        //        y = current_Y + plane.plane_shape[(int)plane.Direction][i].y;
        //        //GameUtils.SetChessBoard(x, y, ChessBoard.whose_board.my_board, ChessBoard.point_state.has_plane, false);
        //        my_chessBoard.chessboard[y][x] = ChessBoard.point_state.has_plane;
        //        public_chessboard.chessboard[y][x] = ChessBoard.point_state.has_plane;
        //        //Add_Block_Color(x, y, Color.FromRgb(255, 64, 129), selected_uid + i.ToString());
        //    }
        //    //_ = ClientInfo.my_chessboard.TryDequeue(out _);
        //    ClientInfo.my_chessboard.Enqueue(public_chessboard);

        //    Console.WriteLine("plane count:" + ClientInfo.my_chessboard.Count());
        //}

        private void Submit_Button_Click(object sender, RoutedEventArgs e)
        {
            //ChessBoard chessBoard = new ChessBoard();
            //ClientInfo.rival_chessboard.Enqueue(chessBoard);
            //ClientInfo.my_chessboard.Enqueue(chessBoard);
            //Console.WriteLine("submit count0: " + ClientInfo.my_chessboard.Count());
            GameUtils.SetGameStatus(ClientInfo.game_status.waiting_drawing);
            NetworkClient.network.InitPosRequest(planeLocators);
            Submit_Button.Opacity = 0;
            Submit_Button.IsEnabled = false;
            //Console.WriteLine("submit count: " + ClientInfo.my_chessboard.Count());
        }

        private void Send_Button_Click(object sender, RoutedEventArgs e)
        {
            //GameUtils.SetGameStatus(ClientInfo.game_status.rival_guessing);
            //Console.WriteLine("send count: " + ClientInfo.my_chessboard.Count());
            //Delete_Plane(old_selected_guess_uids, false);
            //Delete_Block_Color(old_selected_bomb_uid, false);
            if (is_bombing)
            {
                int x = selected_coordinate.X, y = selected_coordinate.Y;
                NetworkClient.network._BombRequest(selected_coordinate);
            }
            else
            {
                NetworkClient.network._GuessRequest(selected_planeLocator);
            }
            Send_Button.Opacity = 0;
            Send_Button.IsEnabled = false;
            //GameUtils.SetGameStatus(ClientInfo.game_status.waiting);
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                // 游戏菜单
                EscPopup.IsOpen = !EscPopup.IsOpen;
            }
            if (e.Key == Key.R)
            {
                ClientInfo.game_status current_status = GameUtils.GetGameStatus();

                if (current_status == ClientInfo.game_status.draw_plane || (current_status == ClientInfo.game_status.self_guessing && !is_bombing))
                {
                    // 翻转飞机方向
                    plane.Rotate_Plane();
                    Refresh_Plane(mouse_is_left);
                }
            }
        }


        protected override void OnClosed(EventArgs e)
        {
            NetworkClient.network._ExitRoomNotification();

            ClientInfo.hall.Visibility = Visibility.Visible;
            base.OnClosed(e);
        }

    }
}
