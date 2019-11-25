using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BombPlane_Client.Assets
{
    public static class Time
    {
        // 最小的时间间隔
        public const float deltaTime = 1;
        public const float SendKeepAlive = 1;
        public const float KeepAliveResponse = 10;
    }
}
