using PCmote_Server.Models;
using System.Diagnostics;
using System.Text.Json;

namespace PCmote_server.Handlers
{
    public class FileHandler
    {
        public static string filesDirectory = Path.Combine(Environment.GetEnvironmentVariable("USERPROFILE"),  "\\appdata\\roaming\\ospriv\\PCmoteServer\\");
        public static string commandsJsonContent;
        public static readonly string commandsJson = "commandsPreset.json";
        public static readonly string settingsJson = "settings.json";

        public static void createFiles()
        {
            try
            {
                File.WriteAllText(Path.Combine(filesDirectory, commandsJson), "[]");
                File.WriteAllText(Path.Combine(filesDirectory, settingsJson), "{\n  \"logs\": false,\n  \"port\": \"5555\"\n}");
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
                if (GlobalVariables.logs) Console.WriteLine($"Failed to create startup shortcut: {ex.Message}");
            }
        }

        public static void readFiles()
        {
            try
            {
                commandsJsonContent = File.ReadAllText(Path.Combine(filesDirectory, commandsJson));
                GlobalVariables.commands = JsonSerializer.Deserialize<List<ShellCommand>>(commandsJsonContent);

                string jsonString = File.ReadAllText(Path.Combine(filesDirectory, settingsJson));

                using (JsonDocument doc = JsonDocument.Parse(jsonString))
                {
                    JsonElement root = doc.RootElement;
                    GlobalVariables.logs = root.GetProperty("logs").GetBoolean();
                    GlobalVariables.port = int.Parse(root.GetProperty("port").GetString());
                }
            }
            catch (Exception ex)
            {
                GlobalVariables.port = 5555;
                GlobalVariables.logs = false;
                Console.WriteLine("Something went wrong with reading settings, using default values:");
                Console.WriteLine($"port: {GlobalVariables.port}");
                Console.WriteLine($"logs: {(GlobalVariables.logs ? "on" : "off")}");
            }
        }

        public static string readCommandsFile()
        {
            try
            {
                return File.ReadAllText(Path.Combine(filesDirectory, commandsJson));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to read commands file: {ex.Message}");
                return "[]";
            }
        }
    }
}
