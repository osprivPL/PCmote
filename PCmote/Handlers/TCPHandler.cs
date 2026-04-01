using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace PCmote_server.Handlers
{
    public class TCPHandler
    {
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
                                    if (p == null)
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