using PCmote_Server.Models;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;

namespace PCmote_server.Handlers
{
    public class ConsoleHandler
    {
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
            Console.WriteLine("7 - Toggle logging (currently " + (GlobalVariables.logs ? "ON" : "OFF") + ")");
            Console.WriteLine("8 - Toggle autostart (currently " + (GlobalVariables.autostartEnabled ? "ON" : "OFF") + ")");
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
            for (int i = 0; i < GlobalVariables.commands.Count(); i++)
            {
                Console.WriteLine($"Header: {GlobalVariables.commands[i].header}, command: {GlobalVariables.commands[i].command}");
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

            GlobalVariables.commands.Add(new ShellCommand(newHeader, newCommand));

            FileHandler.commandsJsonContent = JsonSerializer.Serialize(GlobalVariables.commands, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(Path.Combine(FileHandler.filesDirectory, FileHandler.commandsJson), FileHandler.commandsJsonContent);

            clearConsole();
            Console.WriteLine("\nCommand created successfully!");
        }

        public static void editCommands()
        {
            clearConsole();

            for (int i = 0; i < GlobalVariables.commands.Count; i++)
            {
                Console.WriteLine($"[{i}] Header: {GlobalVariables.commands[i].header}, command: {GlobalVariables.commands[i].command}");
            }

            Console.Write("\nChoose command to edit (index): ");

            try
            {
                int choose = int.Parse(Console.ReadLine());

                if (choose >= 0 && choose < GlobalVariables.commands.Count)
                {
                    Console.Write($"New header ({GlobalVariables.commands[choose].header}): ");
                    string newHeader = Console.ReadLine();
                    if (!string.IsNullOrWhiteSpace(newHeader)) GlobalVariables.commands[choose].header = newHeader;

                    Console.Write($"New command ({GlobalVariables.commands[choose].command}): ");
                    string newCommand = Console.ReadLine();
                    if (!string.IsNullOrWhiteSpace(newCommand)) GlobalVariables.commands[choose].command = newCommand;

                    FileHandler.commandsJsonContent = JsonSerializer.Serialize(GlobalVariables.commands, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(Path.Combine(FileHandler.filesDirectory, FileHandler.commandsJson), FileHandler.commandsJsonContent);

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
            if (GlobalVariables.autostartEnabled)
            {
                GlobalVariables.autostartEnabled = false;
                string startupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup); // shell:startup
                string shortcutPath = Path.Combine(startupFolder, "PCmote_Server.lnk");
                if (File.Exists(shortcutPath))
                {
                    File.Delete(shortcutPath);
                }
            }
            else
            {
                GlobalVariables.autostartEnabled = true;
                FileHandler.createStartupShortcut();
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
    }
}
