using PCmote_Server.Models;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Timer = System.Windows.Forms.Timer;

namespace PCmote_Server
{
    class Program
    {
        //user32.dll ....
        [DllImport("user32.dll")]
        static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);
        [DllImport("user32.dll")]
        static extern IntPtr PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll")]
        static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);
        [DllImport("user32.dll")]
        static extern bool LockWorkStation();
        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        static extern int DeleteMenu(IntPtr hMenu, int nPosition, int wFlags);

        [DllImport("user32.dll")]
        private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

        [DllImport("user32.dll")]
        static extern bool IsIconic(IntPtr hWnd);
        [DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        // PilotMode Variables (Constants)
        private const int MOUSEEVENTF_MOVE = 0x0001;
        private const byte WIN_KEY = 0x5B;
        private const byte D_KEY = 0x44;
        private const byte KEYUP = 0x0002;
        private const uint WM_CLOSE = 0x0010;

        // Minimazing App Variables
        private const int SW_HIDE = 0;
        private const int SW_SHOW = 5;
        private const int SW_RESTORE = 9;
        private const int MF_BYCOMMAND = 0x00000000;
        private const int SC_CLOSE = 0xF060;
        private static bool isHiden = false;

        //Settings Variables (mainly paths)
        private static string filesDirectory = Environment.GetEnvironmentVariable("USERPROFILE") + "\\appdata\\roaming\\ospriv\\PCmoteServer\\";
        private static string commandsJsonContent;
        private static readonly string commandsJson = "commandsPreset.json";
        private static readonly string settingsJson = "settings.json";
        private static int firstRun; // 0 - first run, 1 - not first run
        private static bool autostartEnabled;
        private static bool logs;
        private static int port;
        private static List<ShellCommand> commands;
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

        public static NotifyIcon trayIcon = new NotifyIcon()
        {
            Icon = SystemIcons.Application,
            Text = "PCmote Server",
            Visible = true
        };


        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                // Disabling X button (only works with conhost, fuck windows terminal)
                DeleteMenu(GetSystemMenu(GetConsoleWindow(), false), SC_CLOSE, MF_BYCOMMAND);
                IntPtr consoleHandle = GetConsoleWindow();

                trayIcon.Click += (sender, e) =>
                {
                    if (isHiden)
                    {
                        ShowWindow(consoleHandle, SW_RESTORE);
                        SetForegroundWindow(consoleHandle);
                        isHiden = false;
                    }
                    else
                    {
                        ShowWindow(consoleHandle, SW_HIDE);
                        isHiden = true;
                    }
                }; // TrayIcon 


                Application.ApplicationExit += (sender, e) =>
                {
                    trayIcon.Visible = false;
                    trayIcon.Dispose();
                }; // Deletes tray on exit

                Timer minimizeTimer = new Timer();
                minimizeTimer.Interval = 100;

                minimizeTimer.Tick += (sender, e) =>
                {
                    if (IsIconic(consoleHandle) && !isHiden)
                    {
                        {
                            ShowWindow(consoleHandle, SW_HIDE);
                            isHiden = true;
                        }
                    }
                };   //checksIsHidden every 100ms (in case user tries to minimize with keyboard or something)
                minimizeTimer.Start();

                if (args.Contains("--autostart"))
                {
                    isHiden = true;
                }

                isFirstRun();
                isAutostartEnabled();

                if (firstRun == 0)
                {
                    createFiles();
                }
                else
                {
                    readFiles();
                }


                Console.OutputEncoding = Encoding.UTF8;

                showLogo();
                showNetworkInfo();

                TcpListener server = new TcpListener(IPAddress.Any, port);
                server.Start();
                Console.WriteLine($"\n[Server] Started. Waiting for connection on {port}...");

                // nasluchiwanie polaczenia w tle (nie blokuje cmdka)
                Task.Run(() => acceptClientsLoop(server));

                showOptions();

                Task.Run(() =>
                {
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
                            case "8":
                                toggleAutostart();
                                clearConsole();
                                Console.WriteLine(autostartEnabled ? "\n>>> AUTOSTART ENABLED <<<" : "\n>>> AUTOSTART DISABLED <<<");
                                break;
                            case "0":
                                trayIcon.Visible = false;
                                trayIcon.Dispose();
                                Environment.Exit(0);
                                break;
                            default:
                                Console.WriteLine("Unknown command. Type 1 to show menu.");
                                break;
                        }
                    }
                });

                Application.Run();
            }
            catch (Exception ex)
            {
                trayIcon.Visible = false;
                trayIcon.Dispose();
                Application.Exit();
            }
        }

        public static void isFirstRun()
        {
            if (Directory.Exists(filesDirectory))
            {
                firstRun = 1;
            }
            else
            {
                firstRun = 0;
                Directory.CreateDirectory(filesDirectory);
            }
        }

        public static void isAutostartEnabled()
        {
            string startupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            string shortcutPath = Path.Combine(startupFolder, "PCmote_Server.lnk");
            if (File.Exists(shortcutPath))
            {
                autostartEnabled = true;
            }
            else
            {
                autostartEnabled = false;
            }
        }

        public static void createFiles()
        {
            try
            {
                File.WriteAllText(filesDirectory + commandsJson, "[]");
                File.WriteAllText(filesDirectory + settingsJson, "{\n  \"logs\": false,\n  \"port\": \"5555\"\n}");
            }
            catch
            {
                Console.WriteLine("Program doesn't have permissions to write files in this directory: " + filesDirectory + "\nEvery change done at this session will be forgotten");
            }
        }

        public static void createStartupShortcut()
        {
            try
            {
                string startupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup); // shell:startup
                string shortcutPath = Path.Combine(startupFolder, "PCmote_Server.lnk");

                //Executable path and working directory
                string exePath = Process.GetCurrentProcess().MainModule.FileName;
                string workingDirectory = Path.GetDirectoryName(exePath);

                Type t = Type.GetTypeFromProgID("WScript.Shell");
                dynamic shell = Activator.CreateInstance(t);

                dynamic shortcut = shell.CreateShortcut(shortcutPath);
                shortcut.TargetPath = exePath;
                shortcut.WorkingDirectory = workingDirectory;
                shortcut.Arguments = "--autostart";
                shortcut.Description = "Run PCmote Server in Background";

                shortcut.Save();

            }
            catch (Exception ex)
            {
                if (logs) Console.WriteLine($"Failed to create startup shortcut: {ex.Message}");
            }
        }

        public static void readFiles()
        {
            try
            {
                commandsJsonContent = File.ReadAllText(filesDirectory + commandsJson);
                commands = JsonSerializer.Deserialize<List<ShellCommand>>(commandsJsonContent);

                string jsonString = File.ReadAllText(filesDirectory + settingsJson);

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
                Console.WriteLine("Something went wrong with reading settings, using default values:");
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
|               ___________________     _____   ______________________________               |
|              \______   \_   ___ \   /     \  \_____  \__    ___/\_   _____/                |
|                |     ___/    \  \/  /  \ /  \  /   |   \|    |    |    __)_                |
|                |    |   \     \____/    Y    \/    |    \    |    |        \               |
|                |____|    \______  /\____|__  /\_______  /____|   /_______  /               |
|                                 \/         \/         \/                 \/                |
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
            Console.WriteLine("8 - Toggle autostart (currently " +(autostartEnabled ? "ON" : "OFF") + ")");
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

        public static void toggleAutostart()
        {
            if (autostartEnabled)
            {
                autostartEnabled = false;
                string startupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup); // shell:startup
                string shortcutPath = Path.Combine(startupFolder, "PCmote_Server.lnk");
                if (File.Exists(shortcutPath))
                {
                    File.Delete(shortcutPath);
                }
            }
            else
            {
                autostartEnabled = true;
                createStartupShortcut();
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
                        else if (command == "PIL_LEFTMOUSEBTN")
                        {
                            leftMouseButton();
                        }
                        else if (command == "PIL_RIGHTMOUSEBTN")
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

                                    if (!string.IsNullOrWhiteSpace(output) && logs)
                                    {
                                        Console.WriteLine("[Output]:\n{output.Trim()}");
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

        private static void sendKey(byte key)
        {
            keybd_event(key, 0, 0, 0);
            keybd_event(key, 0, KEYUP, 0);
        }

        private static IntPtr calculateParam(int comm)
        {
            return (IntPtr)((comm << 16));
        }

        private static void prevTrack()
        {
            sendKey(0xB1);
        }

        private static void playPauseResume()
        {
            sendKey(0xB3);
        }

        private static void nextTrack()
        {
            sendKey(0xB0);
        }


        private static void volDown()
        {
            sendKey(0xAE);
        }

        private static void volUp()
        {
            sendKey(0xAF);
        }
        private static void volMute()
        {
            sendKey(0xAD);
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
            keybd_event(WIN_KEY, 0, 0x0001, 0);
            keybd_event(D_KEY, 0, 0, 0);

            keybd_event(D_KEY, 0, KEYUP, 0);
            keybd_event(WIN_KEY, 0, 0x0001 | KEYUP, 0);
        }

        private static void closeApp()
        {
            IntPtr activeWindow = GetForegroundWindow();
            if (activeWindow != IntPtr.Zero)
            {
                PostMessage(activeWindow, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
            }
        }

        private static void lockPC()
        {
            LockWorkStation();
        }
    }
}