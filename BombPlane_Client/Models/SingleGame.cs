using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BombPlane_Client.Models
{
    /// <summary>
    /// 游戏过程中各种信息的管理，在游戏开始后初始化
    /// </summary>
    public static class SingleGame
    {
        // 单局游戏所需的各种信息
        private const int BLOCK_NUM = 10;
        public enum block_status
        {
            init = 1,
            head = 2,
            body = 3,
            miss = 4,
        }
        public static block_status[][] my_blocks = new block_status[BLOCK_NUM][];
        public static block_status[][] rival_blocks = new block_status[BLOCK_NUM][];

        /// <summary>
        /// true则当前是己方回合
        /// 否则当前是对方回合
        /// </summary>
        public static bool is_my_turn;
        public static void Init()
        {
            for(int i = 0; i < BLOCK_NUM; i++)
            {
                for(int j = 0; j < BLOCK_NUM; j++)
                {
                    my_blocks[i][j] = block_status.init;
                    rival_blocks[i][j] = block_status.init;
                    is_my_turn = false;
                }
            }

        }


    }
}
