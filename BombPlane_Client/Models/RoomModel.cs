using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BombPlane_Client.Models
{
    public class RoomModel
    {
        public enum RoomState
        {
            Await, // 等待
            Gaming, // 对局开始
        }

        public int RoomId = 0;

    }
}
