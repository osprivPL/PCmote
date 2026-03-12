namespace PCmote_Server.Models
{
    public class ShellCommand
    {
        public string header { get; set; } = string.Empty;
        public string command { get; set; } = string.Empty;

        public ShellCommand() { }

        public ShellCommand(string _header, string _command)
        {
            header = _header;
            command = _command;
        }
    }
}
