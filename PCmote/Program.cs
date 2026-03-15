using PCmote_Server.Models;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace PCmote_Server
{
    class Program
    {
        [DllImport("user32.dll")]
        static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);
        static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
        static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);
        static extern bool lockWorkStation();

        private const int MOUSEEVENTF_MOVE = 0x0001;
        private const uint WM_APPCOMMAND = 0x0319;
        private static readonly IntPtr HWND_BROADCAST = (IntPtr)0xffff;
        private const byte WIN_KEY = 0x58;
        private const byte D_KEY = 0x44;
        private const byte ALT_KEY = 0x12;
        private const byte F4_KEY = 0x73;
        private const byte KEYUP = 0x0002;


        private static readonly string commandsJson = "commandsPreset.json";
        private static readonly string settingsJson = "settings.json";
        private static string commandsJsonContent;




        public static readonly List<string> DangerousCommands = new List<string>
        {
            // 1. Operacje na plikach i dyskach (Usuwanie, formatowanie)
            "del", "erase", "rmdir", "rd", "format", "diskpart", "fsutil", "cipher",

            // 2. Modyfikacja systemu i rejestru
            "reg", "regedit", "bcdedit", "vssadmin", "wbadmin", "wevtutil",

            // 3. Uprawnienia i konta użytkowników
            "net", "netsh", "takeown", "icacls", "cacls", "attrib", "syskey",

            // 4. Inne powłoki i skrypty (Mogą posłużyć do ominięcia zabezpieczeń)
            "powershell", "pwsh", "wscript", "cscript", "ftp", "tftp", "wsl", "bash",

            // 5. Ubijanie krytycznych procesów
            "taskkill", "tskill",

            // 6. Typowe komendy Linuxowe (jeśli ktoś używałby WSL)
            "sudo", "rm", "mv", "chown", "chmod", "mkfs", "dd"
        };

        private static bool logs;
        private static int port;
        private static List<ShellCommand> commands;

        static void Main(string[] args)
        {

            readSettings();
            readCommands();

            Console.OutputEncoding = Encoding.UTF8;

            showLogo();
            showNetworkInfo();

            TcpListener server = new TcpListener(IPAddress.Any, port);
            server.Start();
            Console.WriteLine($"\n[Server] Started. Waiting for connection on {port}...");

            // nasluchiwanie polaczenia w tle (nie blokuje cmdka)
            Task.Run(() => acceptClientsLoop(server));

            showOptions();

            while (true)
            {
                string input = Console.ReadLine();

                switch (input)
                {
                    case "1":
                        showOptions();
                        break;
                    case "2":
                        clearConsole();
                        break;
                    case "3":
                        showNetworkInfo();
                        break;
                    case "4":
                        showPreparedCommands();
                        break;
                    case "5":
                        addCommand();
                        break;
                    case "6":
                        editCommands();
                        break;
                    case "7":
                        logs = !logs;
                        clearConsole();
                        Console.WriteLine(logs ? "\n>>> LOGGING ENABLED <<<" : "\n>>> LOGGING DISABLED <<<");
                        break;
                    case "0":
                        Environment.Exit(0);
                        break;
                    default:
                        Console.WriteLine("Unknown command. Type 1 to show menu.");
                        break;
                }
            }
        }

        public static void readCommands()
        {
            if (!File.Exists(commandsJson))
            {
                File.WriteAllText(commandsJson, "[]");
            }

            commandsJsonContent = File.ReadAllText(commandsJson);
            commands = JsonSerializer.Deserialize<List<ShellCommand>>(commandsJsonContent);
        }
        public static void readSettings()
        {
            try
            {
                string jsonString = File.ReadAllText(settingsJson);

                using (JsonDocument doc = JsonDocument.Parse(jsonString))
                {
                    JsonElement root = doc.RootElement;
                    logs = root.GetProperty("logs").GetBoolean();
                    port = int.Parse(root.GetProperty("port").GetString());
                }
            }
            catch (Exception ex)
            {
                port = 5555;
                logs = false;
                Console.WriteLine("Something went wrong with reading \"settings.json\", using default values:");
                Console.WriteLine($"port: {port}");
                Console.WriteLine($"logs: {(logs ? "on" : "off")}");
            }

        }
        public static void acceptClientsLoop(TcpListener server)
        {
            while (true)
            {
                TcpClient client = server.AcceptTcpClient();
                string clientIp = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();

                if (logs) Console.WriteLine($"\n[Server] Connected with client: {clientIp}");


                Task.Run(() => handleClient(client, clientIp));
            }
        }

        public static void showLogo()
        {
            Console.WriteLine(@"
=============================================================================================|
|               __________.___.____       _____   ______________________________             |
|               \______   \   |    |     /     \  \_____  \__    ___/\_   _____/             |
|                |       _/   |    |    /  \ /  \  /   |   \|    |    |    __)_              |
|                |    |   \   |    |   /    Y    \/    |    \    |    |        \             |
|                |____|   |___|_______ \____|__  /\_______  /____|   /_______  /             |
|                                     \/       \/         \/                 \/              |
|                                 © 2026 Michał Ożdżyński                                    |
=============================================================================================|
");
        }

        public static void clearConsole()
        {
            Console.Clear();
            showLogo();
            showOptions();
        }

        public static void showOptions()
        {
            Console.WriteLine("\n--- MENU ---");
            Console.WriteLine("1 - Show this menu");
            Console.WriteLine("2 - Clear Console");
            Console.WriteLine("3 - Show network info");
            Console.WriteLine("4 - Show prepared commands");
            Console.WriteLine("5 - Add prepared command");
            Console.WriteLine("6 - Edit prepared commands");
            Console.WriteLine("7 - Toggle logging (currently " + (logs ? "ON" : "OFF") + ")");
            Console.WriteLine("\n0 - Exit");
            Console.WriteLine("------------");
        }

        public static void showNetworkInfo()
        {
            Console.WriteLine("Your local IP Adress:");
            foreach (var ip in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    Console.WriteLine($" -> {ip}");
                }
            }
        }

        public static void showPreparedCommands()
        {
            for (int i = 0; i < commands.Count(); i++)
            {
                Console.WriteLine($"Header: {commands[i].header}, command: {commands[i].command}");
            }
        }

        public static void addCommand()
        {
            clearConsole();

            string newHeader = "";
            string newCommand = "";

            while (string.IsNullOrWhiteSpace(newHeader))
            {
                Console.Write("New header: ");
                newHeader = Console.ReadLine();
            }

            while (string.IsNullOrWhiteSpace(newCommand))
            {
                Console.Write("New command:");
                newCommand = Console.ReadLine();
            }

            commands.Add(new ShellCommand(newHeader, newCommand));

            commandsJsonContent = JsonSerializer.Serialize(commands, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(commandsJson, commandsJsonContent);

            clearConsole();
            Console.WriteLine("\nCommand created successfully!");
        }

        public static void editCommands()
        {
            clearConsole();

            for (int i = 0; i < commands.Count; i++)
            {
                Console.WriteLine($"[{i}] Header: {commands[i].header}, command: {commands[i].command}");
            }

            Console.Write("\nChoose command to edit (index): ");

            try
            {
                int choose = int.Parse(Console.ReadLine());

                if (choose >= 0 && choose < commands.Count)
                {
                    Console.Write($"New header ({commands[choose].header}): ");
                    string newHeader = Console.ReadLine();
                    if (!string.IsNullOrWhiteSpace(newHeader)) commands[choose].header = newHeader;

                    Console.Write($"New command ({commands[choose].command}): ");
                    string newCommand = Console.ReadLine();
                    if (!string.IsNullOrWhiteSpace(newCommand)) commands[choose].command = newCommand;

                    commandsJsonContent = JsonSerializer.Serialize(commands, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(commandsJson, commandsJsonContent);

                    clearConsole();

                    Console.WriteLine("\nCommand updated successfully!");
                }
                else
                {
                    clearConsole();
                    Console.WriteLine("\nInvalid command index.");
                }
            }
            catch (Exception ex)
            {
                clearConsole();
                Console.WriteLine($"\nError: {ex.Message}");
            }
        }

        public static void handleClient(TcpClient client, string clientIp)
        {
            try
            {
                NetworkStream stream = client.GetStream();
                byte[] buffer = new byte[1024];
                int bytesRead;

                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    string rawData = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    string[] commandsList = rawData.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (string cmd in commandsList)
                    {
                        string command = cmd.Trim();
                        if (string.IsNullOrWhiteSpace(command)) continue;

                        if (command.StartsWith("MOUSE:"))
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

                                    mouse_event(MOUSEEVENTF_MOVE, moveX, moveY, 0, 0);
                                }
                            }
                            catch
                            {
                            }
                            continue;
                        }

                        if (logs) Console.WriteLine($"\n[Server] Received command: {command}");

                        if (command == "GET_JSON")
                        {
                            byte[] jsonBytes = Encoding.UTF8.GetBytes(commandsJsonContent);
                            stream.WriteAsync(jsonBytes, 0, jsonBytes.Length);
                            if (logs) Console.WriteLine($"[Server] Sent JSON to client ({jsonBytes.Length} bytes)");
                        }
                        else if (command == "PIL_PREVTRACK")
                        {
                            prevTrack();
                        }
                        else if (command == "PIL_PAUSERESUME")
                        {
                            playPauseResume();
                        }
                        else if (command == "PIL_NEXTTRACK")
                        {
                            nextTrack();
                        }
                        else if (command == "PIL_VOLDOWN")
                        {
                            volDown();
                        }
                        else if (command == "PIL_VOLUP")
                        {
                            volUp();
                        }
                        else if (command == "PIL_VOLMUTE")
                        {
                            volMute();
                        }
                        else if (command == "PIL_LEFTMOUSEBUTTON")
                        {
                            leftMouseButton();
                        }
                        else if (command == "PIL_RIGHTMOUSEBUTTON")
                        {
                            rightMouseButton();
                        }
                        else if (command == "PIL_SCROLLUP")
                        {
                            scrollUp();
                        }
                        else if (command == "PIL_SCROLLDOWN")
                        {
                            scrollDown();
                        }
                        else if (command == "PIL_SHOWDESKTOP")
                        {
                            showDesktop();
                        }
                        else if (command == "PIL_CLOSEAPP")
                        {
                            closeApp();
                        }
                        else if (command == "PIL_LOCKPC")
                        {
                            lockPC();
                        }
                        else
                        {

                            try
                            {
                                if (DangerousCommands.Any(dc => command.StartsWith(dc, StringComparison.OrdinalIgnoreCase)))
                                {
                                    throw new Exception("Command is considered dangerous and will not be executed.");
                                }

                                ProcessStartInfo psi = new ProcessStartInfo("cmd.exe", $"/c \"{command}\"")
                                {
                                    RedirectStandardInput = true,
                                    RedirectStandardError = true,
                                    RedirectStandardOutput = true,
                                    CreateNoWindow = true,
                                    UseShellExecute = false
                                };

                                using (Process p = Process.Start(psi))
                                {
                                    string output = p.StandardOutput.ReadToEnd();
                                    string error = p.StandardError.ReadToEnd();

                                    p.WaitForExit();

                                    if (!string.IsNullOrWhiteSpace(output))
                                    {
                                        Console.WriteLine($"[Output]:\n{output.Trim()}");
                                    }

                                    if (!string.IsNullOrWhiteSpace(error) && logs)
                                    {
                                        Console.WriteLine($"[OutputError]:\n{error.Trim()}");
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                if (logs) Console.WriteLine($"[Warning] {ex.Message}");
                                continue;
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                if (logs) Console.WriteLine($"\n[Error] Connection abruptly lost with: {clientIp}");
            }
            finally
            {
                if (logs) Console.WriteLine($"\n[Server] Disconnected from client: {clientIp}");
                client.Close();
            }
        }

        private static IntPtr calculateParam(int comm)
        {
            return (IntPtr)((comm << 16));
        }

        private static void prevTrack()
        {
            SendMessage(HWND_BROADCAST, WM_APPCOMMAND, IntPtr.Zero, calculateParam(12));
        }

        private static void playPauseResume()
        {
            SendMessage(HWND_BROADCAST, WM_APPCOMMAND, IntPtr.Zero, calculateParam(14));
        }

        private static void nextTrack()
        {
            SendMessage(HWND_BROADCAST, WM_APPCOMMAND, IntPtr.Zero, calculateParam(11));
        }


        private static void volDown()
        {
            SendMessage(HWND_BROADCAST, WM_APPCOMMAND, IntPtr.Zero, calculateParam(9));
        }

        private static void volUp()
        {
            SendMessage(HWND_BROADCAST, WM_APPCOMMAND, IntPtr.Zero, calculateParam(10));
        }
        private static void volMute()
        {
            SendMessage(HWND_BROADCAST, WM_APPCOMMAND, IntPtr.Zero, calculateParam(8));
        }

        private static void leftMouseButton()
        {
            mouse_event(0x02, 0, 0, 0, 0); // w dol
            mouse_event(0x04, 0, 0, 0, 0); // w gore
        }

        private static void rightMouseButton()
        {
            mouse_event(0x08, 0, 0, 0, 0); // w dol
            mouse_event(0x10, 0, 0, 0, 0); // w gore
        }

        private static void scrollUp()
        {
            mouse_event(0x0800, 0, 0, 120, 0);
        }
        private static void scrollDown()
        {
            mouse_event(0x0800, 0, 0, -120, 0);
        }

        private static void showDesktop()
        {
            keybd_event(WIN_KEY, 0, 0, 0);
            keybd_event(D_KEY, 0, 0, 0);
            keybd_event(WIN_KEY, 0, KEYUP, 0);
            keybd_event(D_KEY, 0, KEYUP, 0);
        }

        private static void closeApp()
        {
            keybd_event(ALT_KEY, 0, 0, 0);
            keybd_event(F4_KEY, 0, 0, 0);
            keybd_event(ALT_KEY, 0, KEYUP, 0);
            keybd_event(F4_KEY, 0, KEYUP, 0);
        }

        private static void lockPC()
        {
            lockWorkStation();
        }
    }
}