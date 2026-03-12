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

        
        private const int MOUSEEVENTF_MOVE = 0x0001;

        private static readonly string jsonName = "commandsPreset.json";
        private static string jsonContent;
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

        public static bool logs = false;

        static void Main(string[] args)
        {
            if (!File.Exists(jsonName))
            {
                File.WriteAllText(jsonName, "[]");
            }

            jsonContent = File.ReadAllText(jsonName);
            commands = JsonSerializer.Deserialize<List<ShellCommand>>(jsonContent);

            Console.OutputEncoding = Encoding.UTF8;

            showLogo();
            showNetworkInfo();

            Console.WriteLine("\nSpecify the port to listen to (default: 5555):");
            string portInput = Console.ReadLine();
            int port = string.IsNullOrWhiteSpace(portInput) ? 5555 : int.Parse(portInput);

            TcpListener server = new TcpListener(IPAddress.Any, port);
            server.Start();
            Console.WriteLine($"\n[Server] Started. Waiting for connection on {port}...");

            // nasluchiwanie polaczenia w tle (nie blokuje cmdka)
            Task.Run(() => AcceptClientsLoop(server));

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
                        logs = !logs;
                        clearConsole();
                        Console.WriteLine(logs ? "\n>>> LOGGING ENABLED <<<" : "\n>>> LOGGING DISABLED <<<");
                        break;
                    case "3":
                        clearConsole();
                        break;
                    case "4":
                        showNetworkInfo();
                        break;
                    case "5":
                        showPreparedCommands();
                        break;
                    case "6":
                        addCommand();
                        break;
                    case "7":
                        editCommands();
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
        static void AcceptClientsLoop(TcpListener server)
        {
            while (true)
            {
                TcpClient client = server.AcceptTcpClient();
                string clientIp = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();

                if (logs) Console.WriteLine($"\n[Server] Connected with client: {clientIp}");


                Task.Run(() => HandleClient(client, clientIp));
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
            Console.WriteLine("2 - " + (logs ? "Stop" : "Start") + " logging");
            Console.WriteLine("3 - Clear Console");
            Console.WriteLine("4 - Show network info");
            Console.WriteLine("5 - Show prepared commands");
            Console.WriteLine("6 - Add prepared command");
            Console.WriteLine("7 - Edit prepared commands");
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
            for (int i = 0; i< commands.Count(); i++)
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

            jsonContent = JsonSerializer.Serialize(commands, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(jsonName, jsonContent);

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

                    jsonContent = JsonSerializer.Serialize(commands, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(jsonName, jsonContent);

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

        static void HandleClient(TcpClient client, string clientIp)
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
                            byte[] jsonBytes = Encoding.UTF8.GetBytes(jsonContent);
                            stream.WriteAsync(jsonBytes, 0, jsonBytes.Length);
                            if (logs) Console.WriteLine($"[Server] Sent JSON to client ({jsonBytes.Length} bytes)");
                            continue;
                        }

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
    }
}