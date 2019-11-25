using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BombplaneProto;

namespace BombPlane_Client.Models
{
    public class User : INotifyPropertyChanged
    {
        private string name;
        private int id;
        public int userID
        {
            get { return id; }
            set
            {
                id = value;
                OnPropertyChanged("userID");
            }
        }
        public string username
        {
            get { return name; }
            set
            {
                name = value;
                OnPropertyChanged("username");
            }
        }
        public enum user_state
        {
            free,   // 空闲状态
            playing,// 游戏中
            offline,// 离线状态
        }

        private user_state state;
        public user_state userState
        {
            get { return state; }
            set
            {
                state = value;

                if (value == user_state.free)
                {
                    user_state_string = "用户空闲中";
                    user_state_color = "#66bb6a"; // green
                }
                else if (value == user_state.playing)
                {
                    user_state_string = "正在游戏中";
                    user_state_color = "#ed5350"; // red
                }
                else
                {
                    user_state_string = "用户已离线";
                    user_state_color = "#000000";
                }
                OnPropertyChanged("userState");
            }
        }

        private string state_string;
        public string user_state_string
        {
            get
            {
                return state_string;
            }
            set
            {
                state_string = value;
                OnPropertyChanged("user_state_string");
            }
        }


        private string state_color;
        public string user_state_color
        {
            get
            {
                return state_color;
            }
            set
            {
                state_color = value;
                OnPropertyChanged("user_state_color");
            }
        }


        public enum invite_state
        {
            accept,
            decline,
        }

        public invite_state invite;

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }
    }
}
