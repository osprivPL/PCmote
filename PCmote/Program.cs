using PCmote_server;
using PCmote_server.Handlers;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Timer = System.Windows.Forms.Timer;

namespace PCmote_Server
{
    class Program
    {
        // Minimazing App Variables
        private const int SW_HIDE = 0;
        private const int SW_RESTORE = 9;
        private const int MF_BYCOMMAND = 0x00000000;
        private const int SC_CLOSE = 0xF060;
        private static bool isHiden = false;

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

                ConsoleHandler.isFirstRun();
                ConsoleHandler.isAutostartEnabled();

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
                Task.Run(() => TCPHandler.acceptClientsLoop(server));

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


    }
}