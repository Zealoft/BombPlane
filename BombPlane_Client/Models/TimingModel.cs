using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace BombPlane_Client.Models
{
    public class TimingModel
    {
        // 向服务器询问的时间间隔
        public int last_rsp_time;

        // 心跳包时间
        public int last_req_time;
        public bool req_sent = false;

        // 游戏开始的时间
        public int start_time;
        public DispatcherTimer timer;

        public TimingModel()
        {
            timer = new DispatcherTimer();
            start_time = 0;
            timer.Interval = TimeSpan.FromMilliseconds(1000);
        }
    }
}
