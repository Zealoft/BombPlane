using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;


namespace BombPlane_Client.Models
{
    public class ChessBoard
    {
        public enum point_state
        {
            empty,      // 空位置
            hit,        // 被击中
            destroy,    // 被击毁
            guessing,   // 正在猜测的位置
            bombing,    // 正在炸的位置
            has_plane,  // 本方放置飞机的位置
            unknown,    // 未知位置
            miss,       // 猜测错误的位置
        }

        public class Chessboard_Point
        {
            public int x;
            public int y;
            public point_state state;
        }

        public enum whose_board
        {
            my_board,
            rival_board,
        }

        public point_state[][] chessboard = new point_state[10][];

        /// <summary>
        /// 棋盘被改变的次数，每次set后自动+1，以判断是否被改变
        /// </summary>
        public int set_times;
        public ChessBoard()
        {
            for (int i = 0; i < 10; i++)
            {
                chessboard[i] = new point_state[10];
                for(int j = 0; j < 10; j++)
                {
                    chessboard[i][j] = point_state.empty;
                }
            }
            set_times = 0;
        }
    }
}
