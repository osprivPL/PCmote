using System.Runtime.InteropServices;

namespace PCmote_server.Handlers
{
    public class PilotCommandHandler
    {

        private const byte KEYUP = 0x0002;
        private const byte WIN_KEY = 0x5B;
        private const byte D_KEY = 0x44;
        private const uint WM_CLOSE = 0x0010;
        private const byte VK_SHIFT = 0x10;
        private const byte VK_CONTROL = 0x11;
        private const byte VK_MENU = 0x12;


        private static void sendKey(byte key)
        {
            WindowsInterop.keybd_event(key, 0, 0, 0);
            WindowsInterop.keybd_event(key, 0, KEYUP, 0);
        }

        public static void keyboardKey(string key)
        {
            if (string.IsNullOrEmpty(key)) return;

            char znak = key[0];

            if (znak == '\b')
            {
                backspaceKey();
                return;
            }

            short vkCodeWithState = WindowsInterop.VkKeyScan(znak);
            byte keyToPress = (byte)(vkCodeWithState & 0xFF);
            byte shiftState = (byte)((vkCodeWithState >> 8) & 0xFF);
            bool needShift = (shiftState & 1) != 0;
            bool needCtrl = (shiftState & 2) != 0;
            bool needAlt = (shiftState & 4) != 0;

            if (needShift) WindowsInterop.keybd_event(VK_SHIFT, 0, 0, 0);
            if (needCtrl) WindowsInterop.keybd_event(VK_CONTROL, 0, 0, 0);
            if (needAlt) WindowsInterop.keybd_event(VK_MENU, 0, 0, 0);

            sendKey(keyToPress);

            if (needShift) WindowsInterop.keybd_event(VK_SHIFT, 0, KEYUP, 0);
            if (needCtrl) WindowsInterop.keybd_event(VK_CONTROL, 0, KEYUP, 0);
            if (needAlt) WindowsInterop.keybd_event(VK_MENU, 0, KEYUP, 0);
        }

        public static void backspaceKey()
        {
            sendKey(0x08);

        }

        public static void prevTrack()
        {
            sendKey(0xB1);
        }

        public static void playPauseResume()
        {
            sendKey(0xB3);
        }

        public static void nextTrack()
        {
            sendKey(0xB0);
        }


        public static void volDown()
        {
            sendKey(0xAE);
        }

        public static void volUp()
        {
            sendKey(0xAF);
        }
        public static void volMute()
        {
            sendKey(0xAD);
        }

        public static void leftMouseButtonPressed()
        {
            WindowsInterop.mouse_event(0x02, 0, 0, 0, 0); // pressed
        }

        public static void leftMouseButtonReleased()
        {
            WindowsInterop.mouse_event(0x04, 0, 0, 0, 0); // released
        }

        public static void rightMouseButtonPressed()
        {
            WindowsInterop.mouse_event(0x08, 0, 0, 0, 0); // pressed
        }
        public static void rightMouseButtonReleased()
        {
            WindowsInterop.mouse_event(0x10, 0, 0, 0, 0); // released
        }

        public static void scrollUp()
        {
            WindowsInterop.mouse_event(0x0800, 0, 0, 120, 0);
        }
        public static void scrollDown()
        {
            WindowsInterop.mouse_event(0x0800, 0, 0, -120, 0);
        }

        public static void showDesktop()
        {
            WindowsInterop.keybd_event(WIN_KEY, 0, 0x0001, 0);
            WindowsInterop.keybd_event(D_KEY, 0, 0, 0);

            WindowsInterop.keybd_event(D_KEY, 0, KEYUP, 0);
            WindowsInterop.keybd_event(WIN_KEY, 0, 0x0001 | KEYUP, 0);
        }

        public static void closeApp()
        {
            IntPtr activeWindow = WindowsInterop.GetForegroundWindow();
            if (activeWindow != IntPtr.Zero)
            {
                WindowsInterop.PostMessage(activeWindow, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
            }
        }

        public static void lockPC()
        {
            WindowsInterop.LockWorkStation();
        }
    }
}
