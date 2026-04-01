using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace PCmote_server.Handlers
{
    public class MouseHandler
    {
        private const int MOUSEEVENTF_MOVE = 0x0001;
        public static void moveMouse(string command)
        {
            try
            {
                string[] parts = command.Split(':');
                if (parts.Length == 3)
                {
                    double dx = double.Parse(parts[1], CultureInfo.InvariantCulture);
                    double dy = double.Parse(parts[2], CultureInfo.InvariantCulture);

                    int moveX = (int)Math.Round(dx);
                    int moveY = (int)Math.Round(dy);

                    WindowsInterop.mouse_event(MOUSEEVENTF_MOVE, moveX, moveY, 0, 0);
                }
            }
            catch
            {
            }
        }
    }
}
