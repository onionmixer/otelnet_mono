# Otelnet Mono - Quick Start Guide

**Get started with otelnet_mono in 5 minutes!**

---

## What is Otelnet Mono?

Otelnet Mono is a modern, feature-rich telnet client written in C# for the Mono runtime. It provides:
- Full telnet protocol support
- Interactive console mode
- Session logging
- Connection statistics
- And much more!

---

## Installation

### Requirements

- **Mono Runtime** (version 6.8.0 or later)
- **Linux/Unix system** (tested on Ubuntu, Debian, Fedora)

### Quick Install

1. **Install Mono** (if not already installed):
   ```bash
   # Ubuntu/Debian
   sudo apt-get install mono-complete

   # Fedora
   sudo dnf install mono-complete

   # Arch Linux
   sudo pacman -S mono
   ```

2. **Download otelnet_mono**:
   ```bash
   cd /path/to/otelnet_mono
   ```

3. **Build**:
   ```bash
   make build
   ```

4. **Verify installation**:
   ```bash
   mono bin/otelnet.exe --version
   # Output: otelnet version 1.0.0-mono
   ```

✅ **You're ready to go!**

---

## Basic Usage

### Connecting to a Server

```bash
mono bin/otelnet.exe <hostname> <port>
```

**Examples**:
```bash
# Connect to localhost telnet server
mono bin/otelnet.exe localhost 23

# Connect to a BBS
mono bin/otelnet.exe bbs.example.com 23

# Connect to a MUD
mono bin/otelnet.exe mud.example.org 4000
```

### Your First Connection

Let's connect to a telnet server:

```bash
mono bin/otelnet.exe localhost 23
```

You'll see:
```
Otelnet Mono Version 1.0.0-mono

[INFO] Terminal size: 80x24
Connected to localhost:23
Press Ctrl+] for console mode
Press Ctrl+C to disconnect

[Connected to server...]
```

**Type normally** - your keystrokes are sent to the server!

### Disconnecting

**Two ways to disconnect**:

1. **Ctrl+C** - Quick disconnect
   - Shows statistics
   - Clean exit

2. **Console mode → quit**
   - Press `Ctrl+]`
   - Type `quit` and press Enter

---

## Console Mode

Console mode gives you access to special commands while connected.

### Entering Console Mode

While connected, press **Ctrl+]**

You'll see:
```
[Console Mode - Enter empty line to return, 'quit' to exit]
otelnet>
```

### Available Commands

| Command | Description | Example |
|---------|-------------|---------|
| `help` | Show all commands | `otelnet> help` |
| `stats` | Show connection statistics | `otelnet> stats` |
| `ls` | List files | `otelnet> ls /tmp` |
| `pwd` | Print working directory | `otelnet> pwd` |
| `cd` | Change directory | `otelnet> cd /home` |
| `quit` | Disconnect and exit | `otelnet> quit` |
| *(empty)* | Return to client mode | `otelnet> ` *(press Enter)* |

### Example Console Session

```
[Connected to server]
login: _

[Press Ctrl+]]

[Console Mode - Enter empty line to return, 'quit' to exit]
otelnet> pwd
/home/user

otelnet> ls
  documents/
  downloads/
  projects/

otelnet> stats

=== Connection Statistics ===
Bytes sent:     245
Bytes received: 1,024
Duration:       42 seconds

otelnet>
[Press Enter - back to client mode]

login: _
```

---

## Common Tasks

### View Help

```bash
mono bin/otelnet.exe --help
```

### Check Version

```bash
mono bin/otelnet.exe --version
```

### View Statistics

While connected:
1. Press `Ctrl+]`
2. Type `stats`
3. Press Enter

Or wait until you disconnect - statistics are shown automatically.

### Navigate Local Filesystem

While in console mode:
```
otelnet> pwd
/home/user

otelnet> cd /tmp
[Changed to: /tmp]

otelnet> ls
  file1.txt (1024 bytes)
  file2.log (512 bytes)
```

---

## Tips & Tricks

### 💡 Tip 1: Create an Alias

Add to your `~/.bashrc`:
```bash
alias otelnet='mono /path/to/otelnet_mono/bin/otelnet.exe'
```

Then just type:
```bash
otelnet localhost 23
```

### 💡 Tip 2: Quick Stats

Want to see stats without disconnecting?
```
Ctrl+] → stats → Enter → Enter (back to client)
```

### 💡 Tip 3: File Navigation

Use console mode to check files before upload/download:
```
Ctrl+] → ls → pwd → cd /path → Enter (back)
```

### 💡 Tip 4: Session Logging

Edit `src/Program.cs` line 98 to enable logging:
```csharp
// Uncomment this line:
logger.Start("otelnet_session.log");
```

Rebuild:
```bash
make build
```

Now all sessions are logged to `otelnet_session.log`!

---

## Next Steps

**You're ready to use otelnet!** 🎉

**Want to learn more?**
- 📖 [User Manual](USER_MANUAL.md) - Complete feature guide
- 🔧 [Troubleshooting](TROUBLESHOOTING.md) - Fix common issues
- 📝 [Usage Examples](USAGE_EXAMPLES.md) - Real-world scenarios

**Need help?**
- Check the [Troubleshooting Guide](TROUBLESHOOTING.md)
- Read the full [User Manual](USER_MANUAL.md)
- Review the [README](../README.md)

---

## Quick Reference Card

```
┌─────────────────────────────────────────────────────────┐
│ OTELNET MONO - QUICK REFERENCE                          │
├─────────────────────────────────────────────────────────┤
│ CONNECT:     mono bin/otelnet.exe <host> <port>        │
│ CONSOLE:     Ctrl+]                                      │
│ DISCONNECT:  Ctrl+C or quit command                     │
│                                                          │
│ CONSOLE COMMANDS:                                        │
│   help      - Show help                                  │
│   stats     - Show statistics                            │
│   ls [dir]  - List files                                 │
│   pwd       - Current directory                          │
│   cd <dir>  - Change directory                           │
│   quit      - Exit program                               │
│   (empty)   - Back to client mode                        │
└─────────────────────────────────────────────────────────┘
```

---

**Happy Telneting! 🚀**

*Last Updated: 2025-10-25*
