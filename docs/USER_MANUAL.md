# Otelnet Mono - User Manual

**Complete guide to using otelnet_mono**

**Version**: 1.0.0-mono
**Last Updated**: 2025-10-25

---

## Table of Contents

1. [Introduction](#introduction)
2. [Installation](#installation)
3. [Command-Line Interface](#command-line-interface)
4. [Connecting to Servers](#connecting-to-servers)
5. [Console Mode](#console-mode)
6. [Session Logging](#session-logging)
7. [Statistics](#statistics)
8. [Terminal Control](#terminal-control)
9. [Advanced Features](#advanced-features)
10. [Troubleshooting](#troubleshooting)

---

## Introduction

### What is Otelnet Mono?

Otelnet Mono is a full-featured telnet client implemented in C# for the Mono runtime. It provides complete support for the telnet protocol (RFCs 854, 855, and related) with additional features like console mode, session logging, and statistics tracking.

### Key Features

- ✅ **Full Telnet Protocol Support** - RFCs 854, 855, 856, 858, 1073, 1079, 1091, 1184, 1572
- ✅ **Interactive Console Mode** - Access commands with Ctrl+]
- ✅ **Session Logging** - Hex dump format for debugging
- ✅ **Connection Statistics** - Track bytes sent/received and duration
- ✅ **Terminal Control** - Raw mode, signal handling, window resize
- ✅ **Multiple Terminal Types** - XTERM, VT100, ANSI
- ✅ **Error Handling** - Clear error messages
- ✅ **File Management** - ls, pwd, cd commands in console mode

### System Requirements

- **Operating System**: Linux or Unix-like system
- **Runtime**: Mono 6.8.0 or later
- **Terminal**: Any terminal emulator (xterm, gnome-terminal, etc.)
- **Disk Space**: < 1 MB
- **Memory**: < 10 MB during operation

---

## Installation

### Installing Mono

**Ubuntu/Debian**:
```bash
sudo apt-get update
sudo apt-get install mono-complete
```

**Fedora/RHEL**:
```bash
sudo dnf install mono-complete
```

**Arch Linux**:
```bash
sudo pacman -S mono
```

**macOS** (via Homebrew):
```bash
brew install mono
```

### Building Otelnet Mono

1. **Navigate to project directory**:
   ```bash
   cd /path/to/otelnet_mono
   ```

2. **Build using Make**:
   ```bash
   make build
   ```

   Output:
   ```
   Compilation succeeded - 3 warning(s)
   Build complete: bin/otelnet.exe
   ```

3. **Verify installation**:
   ```bash
   mono bin/otelnet.exe --version
   ```

   Output:
   ```
   otelnet version 1.0.0-mono
   ```

### Creating an Alias (Optional)

For easier access, add to `~/.bashrc` or `~/.zshrc`:

```bash
alias otelnet='mono /full/path/to/otelnet_mono/bin/otelnet.exe'
```

Then reload:
```bash
source ~/.bashrc
```

Now you can use:
```bash
otelnet hostname port
```

---

## Command-Line Interface

### Basic Syntax

```bash
mono bin/otelnet.exe <host> <port> [options]
```

### Arguments

| Argument | Required | Description | Example |
|----------|----------|-------------|---------|
| `host` | Yes | Remote hostname or IP | `localhost`, `192.168.1.100` |
| `port` | Yes | Remote port number (1-65535) | `23`, `8080` |

### Options

| Option | Description |
|--------|-------------|
| `-h`, `--help` | Display help message and exit |
| `-v`, `--version` | Display version and exit |
| `-c <config>` | Load configuration file *(not yet implemented)* |

### Examples

**Display help**:
```bash
mono bin/otelnet.exe --help
```

**Display version**:
```bash
mono bin/otelnet.exe --version
```

**Connect to server**:
```bash
mono bin/otelnet.exe example.com 23
```

**Connect to IP address**:
```bash
mono bin/otelnet.exe 192.168.1.100 8080
```

---

## Connecting to Servers

### Connection Process

When you connect to a server:

1. **TCP connection** is established
2. **Option negotiation** occurs (automatic)
3. **Terminal type** is sent (XTERM by default)
4. **Window size** is detected and sent
5. **Welcome message** from server is displayed

### Connection Messages

```
Otelnet Mono Version 1.0.0-mono

[INFO] Terminal size: 80x24
Connected to example.com:23
Press Ctrl+] for console mode
Press Ctrl+C to disconnect

[Server welcome message appears here...]
```

### Supported Servers

Otelnet Mono works with:
- Standard telnet servers (telnetd)
- BBS systems
- MUD game servers
- Router/switch management interfaces
- Any RFC 854-compliant telnet server

### Option Negotiation

The client automatically negotiates these options:

| Option | RFC | Description |
|--------|-----|-------------|
| BINARY | 856 | Binary transmission mode |
| SGA | 858 | Suppress Go Ahead |
| ECHO | 857 | Server echo control |
| TTYPE | 1091 | Terminal type (XTERM/VT100/ANSI) |
| NAWS | 1073 | Window size negotiation |
| TSPEED | 1079 | Terminal speed |
| ENVIRON | 1572 | Environment variables |
| LINEMODE | 1184 | Line mode operation |

### Connection Modes

The client automatically detects and adapts to:

**Line Mode**:
- Input sent line-by-line
- Local editing enabled
- Server uses LINEMODE option

**Character Mode**:
- Input sent character-by-character
- Server echoes input
- Server uses ECHO + SGA options

**Binary Mode**:
- All bytes transmitted (0-255)
- IAC byte (255) properly escaped
- Server uses BINARY option

---

## Console Mode

### What is Console Mode?

Console mode allows you to execute local commands while remaining connected to the server. Think of it as a command prompt that runs alongside your telnet session.

### Activating Console Mode

**While connected to a server, press: `Ctrl+]`**

You'll see:
```
[Console Mode - Enter empty line to return, 'quit' to exit]
otelnet>
```

### Returning to Client Mode

**Press Enter with no command** (empty line):
```
otelnet>
[Back to client mode]
```

### Console Commands

#### help / ?
Display all available commands.

```
otelnet> help
```

Output:
```
=== Console Commands ===
  [empty]       - Return to client mode
  quit, exit    - Disconnect and exit program
  help, ?       - Show this help message
  stats         - Show connection statistics

=== File Management ===
  ls [dir]      - List files in directory
  pwd           - Print working directory
  cd <dir>      - Change directory

=== Examples ===
  ls /tmp       - List /tmp directory
========================
```

#### stats
Display connection statistics.

```
otelnet> stats
```

Output:
```
=== Connection Statistics ===
Bytes sent:     1,234
Bytes received: 5,678
Duration:       123 seconds
```

#### ls [directory]
List files in a directory.

```
otelnet> ls
  file1.txt (1024 bytes)
  dir1/

[Total: 2 entries]
```

```
otelnet> ls /tmp
  test.log (512 bytes)
  data/

[Total: 2 entries]
```

#### pwd
Print the current working directory.

```
otelnet> pwd
/home/user/projects
```

#### cd <directory>
Change the current working directory.

```
otelnet> cd /tmp
[Changed to: /tmp]

otelnet> pwd
/tmp
```

**Supports tilde (~) expansion**:
```
otelnet> cd ~
[Changed to: /home/user]
```

#### quit / exit
Disconnect and exit the program.

```
otelnet> quit
[Exiting...]

=== Connection Statistics ===
Bytes sent:     1,234
Bytes received: 5,678
Duration:       123 seconds

Disconnected
```

---

## Session Logging

### What is Session Logging?

Session logging records all data sent and received during your telnet session in a hex dump format, perfect for debugging protocol issues.

### Enabling Logging

**Method 1: Edit Source Code** (Permanent)

Edit `src/Program.cs`, line 98:

```csharp
// Uncomment this line:
logger.Start("otelnet_session.log");
```

Rebuild:
```bash
make build
```

**Method 2: Code Modification** (Custom location)

```csharp
logger.Start("/path/to/your/session.log");
```

### Log File Format

The log file contains:
- Session start/end markers
- Timestamps for each data transfer
- Direction indicators (SENT/RECV)
- Hex dump of data
- ASCII representation

**Example**:
```
[2025-10-25 12:34:56] === Session started ===
[2025-10-25 12:34:56][SENT] ff fb 01 ff fb 03  | ......
[2025-10-25 12:34:57][RECV] ff fd 01 ff fd 03  | ......
[2025-10-25 12:34:57][RECV] 57 65 6c 63 6f 6d 65 0d 0a  | Welcome..
[2025-10-25 12:35:00] === Session ended ===
```

### Viewing Log Files

```bash
# View entire log
cat otelnet_session.log

# View with less
less otelnet_session.log

# View last 50 lines
tail -50 otelnet_session.log

# Search for specific data
grep "SENT" otelnet_session.log
```

---

## Statistics

### Connection Statistics

Statistics are automatically tracked for every connection:

| Metric | Description |
|--------|-------------|
| **Bytes sent** | Total bytes transmitted to server |
| **Bytes received** | Total bytes received from server |
| **Duration** | Connection time in seconds |

### Viewing Statistics

**Method 1: Console Mode** (while connected)
```
Ctrl+] → stats → Enter
```

**Method 2: Automatic** (on disconnect)
Statistics are displayed automatically when you disconnect.

### Example Output

```
=== Connection Statistics ===
Bytes sent:     2,456
Bytes received: 15,789
Duration:       342 seconds
```

---

## Terminal Control

### Raw Mode

The client automatically enables raw terminal mode for character-at-a-time input when needed. This allows:
- Immediate character transmission
- No local echo
- Full control character support

### Window Size Detection

The client automatically:
1. Detects your terminal window size
2. Sends size to server (NAWS option)
3. Updates size when you resize the window

**Window size change example**:
```
[INFO] Window size changed: 80x24 -> 120x40
[DEBUG] Sent: IAC SB NAWS 0 120 0 40 IAC SE
```

### Signal Handling

The client handles these signals:

| Signal | Trigger | Action |
|--------|---------|--------|
| **SIGINT** | Ctrl+C | Graceful disconnect, show stats |
| **SIGTERM** | kill command | Clean shutdown |
| **SIGWINCH** | Window resize | Update server with new size |

---

## Advanced Features

### Terminal Type Cycling

The client supports multiple terminal types and cycles through them if requested by the server:

1. **XTERM** (default)
2. **VT100**
3. **ANSI**

**Cycling example**:
```
[INFO] Sending TERMINAL-TYPE IS XTERM (cycle 0)
[INFO] Sending TERMINAL-TYPE IS VT100 (cycle 1)
[INFO] Sending TERMINAL-TYPE IS ANSI (cycle 2)
```

### Environment Variables

The client sends these environment variables when requested:

| Variable | Value | Description |
|----------|-------|-------------|
| `USER` | Current username | From $USER or $LOGNAME |
| `DISPLAY` | X display | From $DISPLAY (if set) |

### Binary Mode

In binary mode:
- All 256 byte values can be transmitted
- IAC byte (255) is automatically escaped
- No CR/LF translation occurs

### Mode Detection

The client automatically detects the server's preferred mode:

**Priority**:
1. LINEMODE (if server supports it)
2. Character mode (if server does ECHO + SGA)
3. Line mode (default)

**Mode indicators**:
```
[Mode: LINE MODE]    - Line-at-a-time editing
[Mode: CHAR MODE]    - Character-at-a-time
[Mode: BINARY MODE]  - Binary transmission
```

---

## Troubleshooting

### Common Issues

#### "Error: Invalid port number"
**Cause**: Port is not a number
**Solution**: Use numeric port (e.g., `23`, not `telnet`)

#### "Error: Port must be between 1 and 65535"
**Cause**: Port number out of range
**Solution**: Use port between 1-65535

#### "Connection refused"
**Cause**: Server not running or firewall blocking
**Solution**:
- Check if server is running
- Verify firewall rules
- Try different port

#### "[ERROR] Failed to get terminal attributes"
**Cause**: Not running in a proper TTY
**Solution**: Run from a terminal, not via pipes or scripts

#### "[WARNING] Failed to get window size"
**Cause**: Terminal doesn't support TIOCGWINSZ
**Solution**: Normal - client continues without NAWS

### Debug Output

Enable debug output by observing `[DEBUG]` messages:

```
[DEBUG] Sent: IAC WILL BINARY
[DEBUG] Received: IAC DO BINARY
[DEBUG] Sent: IAC WILL SGA
```

These show protocol negotiation in real-time.

### Getting Help

1. Check this manual
2. Review [Quick Start Guide](QUICK_START.md)
3. See [Troubleshooting Guide](TROUBLESHOOTING.md)
4. Check [README](../README.md)
5. Review test output: `bash scripts/run_integration_tests.sh`

---

## Appendix A: Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| `Ctrl+]` | Enter console mode |
| `Ctrl+C` | Disconnect and exit |
| `Ctrl+D` | Send EOF (if in line mode) |
| `Backspace` | Delete previous character (console mode) |
| `Enter` | Send line or execute command |

---

## Appendix B: Supported RFCs

| RFC | Title | Support |
|-----|-------|---------|
| 854 | Telnet Protocol Specification | 100% |
| 855 | Telnet Option Specification | 100% |
| 856 | Binary Transmission | 100% |
| 857 | Echo Option | 100% |
| 858 | Suppress Go Ahead | 100% |
| 1073 | Window Size (NAWS) | 100% |
| 1079 | Terminal Speed | 100% |
| 1091 | Terminal Type | 95% |
| 1184 | Linemode Option | 70% |
| 1572 | Environment Option | 80% |

---

## Appendix C: File Locations

```
otelnet_mono/
├── bin/
│   └── otelnet.exe              - Main executable
├── src/
│   ├── Program.cs               - Entry point (line 98: logging)
│   ├── Telnet/                  - Protocol implementation
│   ├── Terminal/                - Terminal control
│   ├── Logging/                 - Session logging
│   └── Interactive/             - Console mode
├── docs/
│   ├── QUICK_START.md          - This guide's companion
│   ├── USER_MANUAL.md          - This file
│   ├── TROUBLESHOOTING.md      - Problem solutions
│   └── *.md                     - Other documentation
└── scripts/
    └── run_integration_tests.sh - Test suite
```

---

## Appendix D: Exit Codes

| Code | Meaning |
|------|---------|
| 0 | Normal exit |
| 1 | Error (connection failed, invalid arguments, etc.) |

---

**End of User Manual**

*For quick reference, see [QUICK_START.md](QUICK_START.md)*
*For troubleshooting, see [TROUBLESHOOTING.md](TROUBLESHOOTING.md)*
*For examples, see [USAGE_EXAMPLES.md](USAGE_EXAMPLES.md)*

**Version**: 1.0.0-mono
**Last Updated**: 2025-10-25
