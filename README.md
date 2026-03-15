<div align="center">
  <img src="https://raw.githubusercontent.com/osprivPL/PCmote/main/icon.png" alt="PCmote Logo" width="128" height="128">
  <h1>PCmote</h1>
  <p>🚀 A C# client-server application for remote Windows PC management via TCP.</p>

  <a href="https://github.com/osprivPL/PCmote/releases">
    <img src="https://img.shields.io/github/v/release/osprivPL/PCmote?style=for-the-badge&color=0078d4" alt="Release">
  </a>
  <a href="https://github.com/osprivPL/PCmote/stargazers">
    <img src="https://img.shields.io/github/stars/osprivPL/PCmote?style=for-the-badge&color=0078d4" alt="Stars">
  </a>
  <a href="https://github.com/osprivPL/PCmote/blob/main/LICENSE">
    <img src="https://img.shields.io/github/license/osprivPL/PCmote?style=for-the-badge&color=0078d4" alt="License">
  </a>
</div>

---

## 📖 About The Project

**PCmote** consists of two components: a lightweight Windows console server and a cross-platform .NET MAUI client app (`PCmotePhone`). They communicate over your local network using TCP sockets, allowing you to remotely control your computer's media, mouse, system state, and securely execute shell commands.

### ✨ Key Features
- 🖱️ **Mouse Control:** Remote cursor movement, left/right clicks, and scrolling.
- 🔊 **Volume Control:** System-wide volume up, down, and mute.
- ⏯️ **Media Management:** Play/Pause, Next, and Previous track control using Windows API broadcasts.
- 💻 **System Actions:** Remotely lock the PC, show the desktop, or close the active application.
- ⌨️ **Command Execution:** Execute standard Windows CMD commands remotely. (Includes a built-in safety filter blocking destructive commands like `del`, `format`, or `regedit`).
- 📱 **Cross-Platform Client:** The phone app is built with .NET MAUI, targeting Android, iOS, macOS, and Windows.

---

## 🛠️ Tech Stack

- **C# & .NET 10**
- **.NET MAUI** (for the mobile/desktop client)
- **TCP Sockets** (for fast, local client-server communication)
- **Windows API (`user32.dll`)** (for deep system integration on the server side)

---

## 🚀 Getting Started

### Prerequisites
- **Server:** A Windows OS PC with .NET 10 Runtime installed.
- **Client:** An Android, iOS, or Windows device to run the MAUI application.

### Setup Instructions

# OPTION 1:
1. **Clone the repository:**
   ```bash
   git clone [https://github.com/osprivPL/PCmote.git](https://github.com/osprivPL/PCmote.git)
   cd PCmote
   ```
2. **Configure the Server**:
  The server generates a settings.json file on first run. You can configure the TCP port and toggle logging.
  It also uses a commandsPreset.json file for storing pre-configured custom shell commands.

3. Run the Server:
  Open the PCmote-server project in Visual Studio or run it via the .NET CLI on your target Windows machine.
   ```
   cd PCmote
   dotnet run
   ```

4. Build the Client app:
   Open PCmotePhone.csproj in Visual Studio (with .NET MAUI workload installed) and deploy it to your preferred device (Android emulator, physical phone, or local Windows machine).

5. Connect:
  Ensure both devices are on the same local network. The server console will display your local IP address upon startup. Enter this IP and the configured port into the client app to establish a connection.
   
# OPTION 2:
  Doesn't exist (yet)



# 🤝 Contributing
  Contributions are welcome! If you have suggestions or want to add a feature:
    1. Fork the Project
    2. Create your Feature Branch (git checkout -b feature/NewFeature)
    3. Commit your Changes (git commit -m 'Add NewFeature')
    4. Push to the Branch (git push origin feature/NewFeature)
    5. Open a Pull Request

# 📄 License
  Distributed under the MIT License. See LICENSE for more information.

<div align="center">
  © 2026 Michał Ożdżyński (<a href="https://github.com/osprivPL">osprivPL</a>)
</div>
