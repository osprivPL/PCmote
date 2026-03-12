using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace PCmotePhone
{
    public static class GlobalThings
    {
        public static TcpClient AppClient { get;  set; }
        public static NetworkStream AppStream { get;  set; }
    }
}
