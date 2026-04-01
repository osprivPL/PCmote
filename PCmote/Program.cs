using PCmote_server;
using PCmote_server.Handlers;
using PCmote_Server.Models;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Timer = System.Windows.Forms.Timer;

namespace PCmote_Server
{
    class Program
    {
        // Minimazing App Variables
        private const int SW_HIDE = 0;
        private const int SW_SHOW = 5;
        private const int SW_RESTORE = 9;
        private const int MF_BYCOMMAND = 0x00000000;
        private const int SC_CLOSE = 0xF060;
        private static bool isHiden = false;

        //Settings Variables (mainly paths)
        

        private static Dictionary<string, Action<NetworkStream>> commandActions = new Dictionary<string, Action<NetworkStream>>
                    {
                        { "GET_JSON", (NetworkStream stream) => {
                            byte[] jsonBytes = Encoding.UTF8.GetBytes(FileHandler.readCommandsFile());
                            stream.WriteAsync(jsonBytes, 0, jsonBytes.Length);
                            if (GlobalVariables.logs) Console.WriteLine($"[Server] Sent JSON to client ({jsonBytes.Length} bytes)");
                        }},
                        { "PIL_PREVTRACK", _ => PilotCommandHandler.prevTrack() },
                        { "PIL_PLAYPAUSERESUME",_ => PilotCommandHandler.playPauseResume() },
                        { "PIL_NEXTTRACK",_ => PilotCommandHandler.nextTrack() },
                        { "PIL_VOLDOWN",_ => PilotCommandHandler.volDown() },
                        { "PIL_VOLUP",_ => PilotCommandHandler.volUp() },
                        { "PIL_VOLMUTE",_ => PilotCommandHandler.volMute() },
                        { "PIL_LEFTMOUSEBTN",_ => PilotCommandHandler.leftMouseButton() },
                        { "PIL_RIGHTMOUSEBTN",_ => PilotCommandHandler.rightMouseButton() },
                        { "PIL_SCROLLUP",_ => PilotCommandHandler.scrollUp() },
                        { "PIL_SCROLLDOWN",_ => PilotCommandHandler.scrollDown() },
                        { "PIL_SHOWDESKTOP",_ => PilotCommandHandler.showDesktop() },
                        { "PIL_CLOSEAPP",_ => PilotCommandHandler.closeApp() },
                        { "PIL_LOCKPC",_ => PilotCommandHandler.lockPC() }
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
                WindowsInterop.DeleteMenu(WindowsInterop.GetSystemMenu(WindowsInterop.GetConsoleWindow(), false), SC_CLOSE, MF_BYCOMMAND);
                IntPtr consoleHandle = WindowsInterop.GetConsoleWindow();

                trayIcon.Click += (sender, e) =>
                {
                    if (isHiden)
                    {
                        WindowsInterop.ShowWindow(consoleHandle, SW_RESTORE);
                        WindowsInterop.SetForegroundWindow(consoleHandle);
                        isHiden = false;
                    }
                    else
                    {
                        WindowsInterop.ShowWindow(consoleHandle, SW_HIDE);
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
                    if (WindowsInterop.IsIconic(consoleHandle) && !isHiden)
                    {
                        {
                            WindowsInterop.ShowWindow(consoleHandle, SW_HIDE);
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

                if (GlobalVariables.firstRun)
                {
                    FileHandler.createFiles();
                }
                else
                {
                    FileHandler.readFiles();
                }


                Console.OutputEncoding = Encoding.UTF8;

                ConsoleHandler.showLogo();
                ConsoleHandler.showNetworkInfo();

                TcpListener server = new TcpListener(IPAddress.Any, GlobalVariables.port);
                server.Start();
                Console.WriteLine($"\n[Server] Started. Waiting for connection on {GlobalVariables.port}...");

                // nasluchiwanie polaczenia w tle (nie blokuje cmdka)
                Task.Run(() => acceptClientsLoop(server));

                ConsoleHandler.showOptions();

                Task.Run(() =>
                {
                    while (true)
                    {
                        string input = Console.ReadLine();

                        switch (input)
                        {
                            case "1":
                                ConsoleHandler.showOptions();
                                break;
                            case "2":
                                ConsoleHandler.clearConsole();
                                break;
                            case "3":
                                ConsoleHandler.showNetworkInfo();
                                break;
                            case "4":
                                ConsoleHandler.showPreparedCommands();
                                break;
                            case "5":
                                ConsoleHandler.addCommand();
                                break;
                            case "6":
                                ConsoleHandler.editCommands();
                                break;
                            case "7":
                                GlobalVariables.logs = !GlobalVariables.logs;
                                ConsoleHandler.clearConsole();
                                Console.WriteLine(GlobalVariables.logs ? "\n>>> LOGGING ENABLED <<<" : "\n>>> LOGGING DISABLED <<<");
                                break;
                            case "8":
                                ConsoleHandler.toggleAutostart();
                                ConsoleHandler.clearConsole();
                                Console.WriteLine(GlobalVariables.autostartEnabled ? "\n>>> AUTOSTART ENABLED <<<" : "\n>>> AUTOSTART DISABLED <<<");
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
            if (Directory.Exists(FileHandler.filesDirectory))
            {
                GlobalVariables.firstRun = false;
            }
            else
            {
                GlobalVariables.firstRun = true;
                Directory.CreateDirectory(FileHandler.filesDirectory);
            }
        }

        public static void isAutostartEnabled()
        {
            string startupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            string shortcutPath = Path.Combine(startupFolder, "PCmote_Server.lnk");
            if (File.Exists(shortcutPath))
            {
                GlobalVariables.autostartEnabled = true;
            }
            else
            {
                GlobalVariables.autostartEnabled = false;
            }
        }

        public static void acceptClientsLoop(TcpListener server)
        {
            while (true)
            {
                TcpClient client = server.AcceptTcpClient();
                string clientIp = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();

                if (GlobalVariables.logs) Console.WriteLine($"\n[Server] Connected with client: {clientIp}");

                Task.Run(() => handleClient(client, clientIp));
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
                            MouseHandler.moveMouse(command);
                            continue;
                        }

                        if (GlobalVariables.logs) Console.WriteLine($"\n[Server] Received command: {command}");


                        if (commandActions.TryGetValue(command, out Action<NetworkStream> action))
                        {
                            action(stream);
                            continue;
                        }
                        else
                        {
                            try
                            {
                                if (GlobalVariables.DangerousCommands.Any(dc => command.StartsWith(dc, StringComparison.OrdinalIgnoreCase)))
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
                                    if (p ==null)
                                    {
                                        throw new Exception("Failed to start process.");
                                    }
                                    string output = p.StandardOutput.ReadToEnd();
                                    string error = p.StandardError.ReadToEnd();

                                    p.WaitForExit();

                                    if (!string.IsNullOrWhiteSpace(output) && GlobalVariables.logs)
                                    {
                                        Console.WriteLine("[Output]:\n{output.Trim()}");
                                    }

                                    if (!string.IsNullOrWhiteSpace(error) && GlobalVariables.logs)
                                    {
                                        Console.WriteLine($"[OutputError]:\n{error.Trim()}");
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                if (GlobalVariables.logs) Console.WriteLine($"[Warning] {ex.Message}");
                                continue;
                            }
                        }

                    }
                }
            }
            catch (Exception)
            {
                if (GlobalVariables.logs) Console.WriteLine($"\n[Error] Connection abruptly lost with: {clientIp}");
            }
            finally
            {
                if (GlobalVariables.logs) Console.WriteLine($"\n[Server] Disconnected from client: {clientIp}");
                client.Close();
            }
        }
    }
}