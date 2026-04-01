using PCmote_Server.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace PCmote_server
{
    public static class GlobalVariables
    {
        public static bool logs { get; set; }
        public static int port { get; set; }
        public static List<ShellCommand> commands { get; set; }
        public static bool firstRun; // true - first run, false - not first run
        public static bool autostartEnabled;
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
    }
}
