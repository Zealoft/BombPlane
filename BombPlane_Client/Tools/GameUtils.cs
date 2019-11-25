using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BombPlane_Client.Models;
using BombPlane_Client.Assets;
using System.Threading;
using System.Windows.Threading;
using System.Windows;

using BombplaneProto;

namespace BombPlane_Client.Tools
{
    public static class GameUtils
    {
        public static Plane.plane_direction GetDirection(PlaneLocator planeLocator)
        {
            // 上
            if (planeLocator.Pos1.Y < planeLocator.Pos2.Y && planeLocator.Pos2.Y == planeLocator.Pos3.Y)
            {
                return Plane.plane_direction.up;
            }
            // 下
            else if (planeLocator.Pos1.Y > planeLocator.Pos2.Y && planeLocator.Pos2.Y == planeLocator.Pos3.Y)
            {
                return Plane.plane_direction.down;
            }
            else if (planeLocator.Pos1.X < planeLocator.Pos2.X && planeLocator.Pos2.X == planeLocator.Pos3.X)
            {
                return Plane.plane_direction.left;
            }
            else
            {
                return Plane.plane_direction.right;
            }
        }
        //public static void ClearChessBoard()
        //{

        //    ChessBoard clear_chessboard = new ChessBoard();
        //    ThreadPool.QueueUserWorkItem(delegate
        //    {
        //        SynchronizationContext.SetSynchronizationContext(new
        //          DispatcherSynchronizationContext(Application.Current.Dispatcher));
        //        SynchronizationContext.Current.Post(pl =>
        //        {
        //            //里面写真正的业务内容
        //            while (ClientInfo.my_chessboard.Count > 0)
        //                _ = ClientInfo.my_chessboard.TryDequeue(out _);
        //            while (ClientInfo.rival_chessboard.Count > 0)
        //                _ = ClientInfo.rival_chessboard.TryDequeue(out _);
        //            ClientInfo.my_chessboard.Enqueue(clear_chessboard);
        //            ClientInfo.rival_chessboard.Enqueue(clear_chessboard);
        //            //ClientInfo.my_chessboard.Clear();
        //            //ClientInfo.rival_chessboard.Clear();
        //            //ClientInfo.my_chessboard.Add(clear_chessboard);
        //            //ClientInfo.rival_chessboard.Add(clear_chessboard);
        //        }, null);
        //    });

        //}

        //public static void SetChessBoard(int x, int y, ChessBoard.whose_board whose, ChessBoard.point_state state, bool is_out = true)
        //{
        //    object thislock = new object();
        //    lock (thislock)
        //    {
        //        Console.WriteLine(x.ToString() + ":::" + y.ToString());
        //        if (whose == ChessBoard.whose_board.my_board)
        //        {
        //            ChessBoard chessBoard;
        //            if (ClientInfo.my_chessboard.Count == 0)
        //            {
        //                chessBoard = new ChessBoard();
        //            }
        //            else
        //                ClientInfo.my_chessboard.TryDequeue(out chessBoard);
        //            chessBoard.chessboard[y][x] = state;
        //            if (is_out)
        //                chessBoard.set_times++;
        //            ClientInfo.my_chessboard.Enqueue(chessBoard);
        //            Console.WriteLine("times: " + chessBoard.set_times.ToString());
        //        }
        //        else
        //        {
        //            ChessBoard chessBoard;
        //            if (ClientInfo.rival_chessboard.Count == 0)
        //                chessBoard = new ChessBoard();
        //            else
        //                ClientInfo.rival_chessboard.TryDequeue(out chessBoard);
        //            chessBoard.chessboard[y][x] = state;
        //            chessBoard.set_times++;
        //            ClientInfo.rival_chessboard.Enqueue(chessBoard);
        //            Console.WriteLine("times: " + chessBoard.set_times.ToString());
        //        }
        //    }
            
        //}

        //public static ChessBoard GetChessBoard(ChessBoard.whose_board whose)
        //{
        //    object thislock = new object();
        //    lock (thislock)
        //    {
        //        ChessBoard chessBoard;
        //        if (whose == ChessBoard.whose_board.my_board)
        //        {
        //            //while (ClientInfo.my_chessboard.Count == 0)
        //            //    Thread.Sleep(100);
        //            chessBoard = ClientInfo.my_chessboard.First();
        //            //ClientInfo.my_chessboard.TryDequeue(out chessBoard);
        //        }
        //        else
        //        {
        //            //while (ClientInfo.rival_chessboard.Count == 0)
        //            //    Thread.Sleep(100);
        //            chessBoard = ClientInfo.rival_chessboard.First();
        //        }
        //        return chessBoard;
        //    }
            
        //}

        public static void SetGameStatus(ClientInfo.game_status status)
        {
            _ = ClientInfo.current_gamestatus.TryDequeue(out _);
            ClientInfo.current_gamestatus.Enqueue(status);
        }

        public static ClientInfo.game_status GetGameStatus()
        {
            ClientInfo.game_status status;
            status = ClientInfo.current_gamestatus.First();
            return status;
        }

        public static List<ChessBoard.Chessboard_Point> PlaneLocator_To_Points(PlaneLocator planeLocator)
        {
            List<ChessBoard.Chessboard_Point> chessboard_Points = new List<ChessBoard.Chessboard_Point>();
            int center_x = 0, center_y = 0;
            Plane plane = new Plane();
            Plane.plane_direction direction = GetDirection(planeLocator);
            plane.Direction = direction;
            switch (direction)
            {
                case Plane.plane_direction.down:
                    center_x = planeLocator.Pos1.X;
                    center_y = planeLocator.Pos1.Y - 1;
                    break;
                case Plane.plane_direction.left:
                    center_x = planeLocator.Pos1.X + 1;
                    center_y = planeLocator.Pos1.Y;
                    break;
                case Plane.plane_direction.up:
                    center_x = planeLocator.Pos1.X;
                    center_y = planeLocator.Pos1.Y + 1;
                    break;
                case Plane.plane_direction.right:
                    center_x = planeLocator.Pos1.X - 1;
                    center_y = planeLocator.Pos1.Y;
                    break;
            }
            for(int i = 0; i < 10; i++)
            {
                ChessBoard.Chessboard_Point point = new ChessBoard.Chessboard_Point();
                point.x = center_x + plane.plane_shape[(int)plane.Direction][i].x;
                point.y = center_y + plane.plane_shape[(int)plane.Direction][i].y;
                chessboard_Points.Add(point);
            }
            return chessboard_Points;
        }


    }
}
