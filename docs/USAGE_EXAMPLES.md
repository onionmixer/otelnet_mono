# Otelnet Mono - Usage Examples

**Real-world scenarios and practical examples**

---

## Table of Contents

1. [Basic Examples](#basic-examples)
2. [BBS Systems](#bbs-systems)
3. [MUD Games](#mud-games)
4. [Network Equipment](#network-equipment)
5. [Development and Testing](#development-and-testing)
6. [Automation Scripts](#automation-scripts)
7. [Advanced Scenarios](#advanced-scenarios)

---

## Basic Examples

### Example 1: Quick Test Connection

**Scenario**: Test if a telnet server is accessible.

```bash
# Connect to server
mono bin/otelnet.exe example.com 23

# If you see "Connected to example.com:23", it worked!
# Press Ctrl+C to disconnect
```

**Expected**:
```
Otelnet Mono Version 1.0.0-mono

[INFO] Terminal size: 80x24
Connected to example.com:23
Press Ctrl+] for console mode
Press Ctrl+C to disconnect

[Server welcome message...]
```

---

### Example 2: Check Connection Statistics

**Scenario**: Monitor how much data is transferred.

```bash
# Connect
mono bin/otelnet.exe server.example.com 23

# Do some work...
# login, read mail, etc.

# Check stats without disconnecting
Ctrl+]
otelnet> stats

=== Connection Statistics ===
Bytes sent:     1,234
Bytes received: 5,678
Duration:       120 seconds

otelnet> [press Enter]
# Back to session

# When done
Ctrl+]
otelnet> quit
```

---

### Example 3: Navigate Local Filesystem

**Scenario**: Check local files while connected to remote server.

```bash
# Connect to server
mono bin/otelnet.exe remote.example.com 23

# While connected, check local files
Ctrl+]
otelnet> pwd
/home/user

otelnet> ls ~/downloads
  file1.txt (1024 bytes)
  file2.pdf (2048 bytes)

otelnet> cd ~/downloads
[Changed to: /home/user/downloads]

otelnet> [press Enter]
# Back to remote session
```

---

## BBS Systems

### Example 4: Connecting to a BBS

**Scenario**: Connect to a classic BBS system.

```bash
# Connect to a BBS
mono bin/otelnet.exe bbs.example.com 23
```

**Session**:
```
Connected to bbs.example.com:23

┌─────────────────────────────────────┐
│  Welcome to Example BBS System      │
│                                     │
│  [N]ew User or [L]ogin?            │
└─────────────────────────────────────┘

L [pressed L for Login]

Username: myuser
Password: ****

[Main Menu]
(M)essages  (F)iles  (G)ames  (C)hat  (Q)uit

M [check messages]
...
```

**Tips for BBS**:
- Use ANSI terminal type (client sends XTERM which works)
- Colors should display correctly
- Arrow keys work in most systems
- Use Ctrl+] for console mode if needed

---

### Example 5: BBS File Navigation

**Scenario**: Download files from BBS (preparation).

```bash
# Before connecting, ensure you're in the right directory
cd ~/downloads

# Connect to BBS
mono bin/otelnet.exe bbs.example.com 23

# Navigate to files section
# Select file to download
# BBS may trigger ZMODEM/YMODEM

# (Note: File transfer not yet implemented in otelnet_mono)
# For now, use external tools
```

---

## MUD Games

### Example 6: Playing a MUD

**Scenario**: Connect to a Multi-User Dungeon game.

```bash
# Connect to MUD
mono bin/otelnet.exe mud.example.org 4000
```

**Session**:
```
Connected to mud.example.org:4000

Welcome to Example MUD!

By what name do you wish to be known? warrior

Password: ****

[The Town Square]
You are standing in the center of a bustling town square.
Exits: north, south, east, west

> look
[detailed description...]

> north
You walk north...
[New Room]

> inventory
You are carrying:
- rusty sword
- leather armor
- 50 gold coins

> [Continue playing...]
```

**Tips for MUDs**:
- Character mode works great for real-time updates
- Stats command useful to track data usage
- Use console mode to check local time without disconnecting

---

### Example 7: MUD with Statistics Monitoring

**Scenario**: Monitor network usage during gameplay.

```bash
# Connect
mono bin/otelnet.exe mud.example.org 4000

# Play for a while...

# Check how much data you're using
Ctrl+]
otelnet> stats

=== Connection Statistics ===
Bytes sent:     15,234  (your commands)
Bytes received: 125,678 (server responses)
Duration:       1,800 seconds (30 minutes)

otelnet> [press Enter to continue playing]
```

---

## Network Equipment

### Example 8: Router Configuration

**Scenario**: Configure a Cisco router via telnet.

```bash
# Connect to router
mono bin/otelnet.exe 192.168.1.1 23
```

**Session**:
```
Connected to 192.168.1.1:23

User Access Verification

Username: admin
Password: ****

Router> enable
Password: ****
Router# show running-config
...

Router# configure terminal
Router(config)# interface GigabitEthernet0/1
Router(config-if)# ip address 192.168.2.1 255.255.255.0
Router(config-if)# no shutdown
Router(config-if)# end
Router# write memory
Router# exit

[Connection closed by remote host]

=== Connection Statistics ===
Bytes sent:     456
Bytes received: 3,210
Duration:       180 seconds
```

---

### Example 9: Switch Management

**Scenario**: Manage network switch.

```bash
# Connect to switch
mono bin/otelnet.exe switch.local 23

# Example session:
# enable
# configure terminal
# vlan 100
# name Engineering
# exit
# write memory
# exit
```

**Console Mode Use**:
```
# While in switch config, check local VLAN documentation
Ctrl+]
otelnet> ls ~/docs/network
  vlan-plan.txt
  switch-config.txt

otelnet> [press Enter]
# Back to switch config
```

---

## Development and Testing

### Example 10: Testing Telnet Server

**Scenario**: Test your own telnet server implementation.

**Terminal 1** (Server):
```bash
# Start your telnet server
python3 my_telnet_server.py --port 8023
```

**Terminal 2** (Client):
```bash
# Connect with otelnet
mono bin/otelnet.exe localhost 8023

# Test commands...
# Watch debug output:
[DEBUG] Sent: IAC WILL BINARY
[DEBUG] Received: IAC DO BINARY
[DEBUG] Sent: IAC WILL SGA
...
```

**Benefits**:
- See exact protocol negotiation
- Verify options are handled correctly
- Debug timing issues

---

### Example 11: Protocol Debugging

**Scenario**: Debug telnet protocol issues.

**Enable logging** (edit src/Program.cs:98):
```csharp
logger.Start("debug_session.log");
```

**Rebuild and test**:
```bash
make build
mono bin/otelnet.exe testserver.example.com 23

# Do your test...

# Exit and check log
cat debug_session.log
```

**Log shows**:
```
[2025-10-25 12:34:56] === Session started ===
[2025-10-25 12:34:56][SENT] ff fb 01 ff fb 03  | ......
[2025-10-25 12:34:57][RECV] ff fd 01 ff fd 03  | ......
...
```

---

### Example 12: Automated Testing

**Scenario**: Test server with automated input.

```bash
# Create test input
cat > test_input.txt << 'EOF'
login: testuser
password: testpass
help
quit
EOF

# Run test (note: may not work perfectly without TTY)
cat test_input.txt | timeout 10 mono bin/otelnet.exe localhost 23

# Better: Use expect script
# (Not shown - requires expect tool)
```

---

## Automation Scripts

### Example 13: Connection Script

**Scenario**: Create reusable connection script.

**File: `connect-bbs.sh`**:
```bash
#!/bin/bash
# Connect to favorite BBS

cd ~/downloads  # Where files should go
mono /path/to/otelnet_mono/bin/otelnet.exe bbs.example.com 23
```

**Make executable**:
```bash
chmod +x connect-bbs.sh
```

**Use**:
```bash
./connect-bbs.sh
```

---

### Example 14: Multi-Server Script

**Scenario**: Menu to connect to different servers.

**File: `telnet-menu.sh`**:
```bash
#!/bin/bash

OTELNET="mono /path/to/otelnet_mono/bin/otelnet.exe"

echo "=== Telnet Connection Menu ==="
echo "1) Local test server"
echo "2) Production server"
echo "3) Router (192.168.1.1)"
echo "4) Switch (192.168.1.2)"
echo "0) Exit"
echo ""
read -p "Choice: " choice

case $choice in
    1) $OTELNET localhost 23 ;;
    2) $OTELNET prod.example.com 23 ;;
    3) $OTELNET 192.168.1.1 23 ;;
    4) $OTELNET 192.168.1.2 23 ;;
    0) exit 0 ;;
    *) echo "Invalid choice" ;;
esac
```

---

### Example 15: Logging Script

**Scenario**: Auto-enable logging for all sessions.

**Setup**:
1. Edit `src/Program.cs:98`, uncomment:
   ```csharp
   logger.Start("otelnet_session.log");
   ```

2. Rebuild:
   ```bash
   make build
   ```

3. Create wrapper script `telnet-with-log.sh`:
   ```bash
   #!/bin/bash
   TIMESTAMP=$(date +%Y%m%d_%H%M%S)
   LOGDIR=~/telnet_logs
   mkdir -p $LOGDIR

   cd $LOGDIR
   mono /path/to/otelnet_mono/bin/otelnet.exe "$@"

   # Rename log with timestamp
   if [ -f otelnet_session.log ]; then
       mv otelnet_session.log session_${TIMESTAMP}.log
       echo "Log saved: $LOGDIR/session_${TIMESTAMP}.log"
   fi
   ```

**Use**:
```bash
./telnet-with-log.sh example.com 23
# Log automatically saved with timestamp
```

---

## Advanced Scenarios

### Example 16: Window Resize Handling

**Scenario**: Test NAWS (window size) updates.

```bash
# Connect to server
mono bin/otelnet.exe localhost 23

# Resize your terminal window
# (Drag window corners to make bigger/smaller)

# Watch for debug message:
[INFO] Window size changed: 80x24 -> 120x40
[DEBUG] Sent: IAC SB NAWS 0 120 0 40 IAC SE

# Server receives new window size automatically
```

**Use case**: Full-screen applications that need window size.

---

### Example 17: Multiple Terminal Types

**Scenario**: Server requests different terminal types.

```bash
# Connect
mono bin/otelnet.exe advanced-server.example.com 23

# Watch terminal type negotiation:
[INFO] Sending TERMINAL-TYPE IS XTERM (cycle 0)
[INFO] Sending TERMINAL-TYPE IS VT100 (cycle 1)
[INFO] Sending TERMINAL-TYPE IS ANSI (cycle 2)

# Server picks the one it prefers
```

**Supported types**:
1. XTERM (default, most compatible)
2. VT100 (classic, widely supported)
3. ANSI (basic ANSI escape codes)

---

### Example 18: Binary Mode Transfer

**Scenario**: Transfer binary data over telnet.

```bash
# Connect to binary-capable server
mono bin/otelnet.exe binary-server.example.com 23

# Watch binary negotiation:
[DEBUG] Received: IAC WILL BINARY
[DEBUG] Sent: IAC DO BINARY
[Mode: BINARY MODE]

# Now all 256 byte values can be transmitted
# IAC (255) is automatically escaped to IAC IAC
```

---

### Example 19: Environment Variables

**Scenario**: Server requests environment variables.

```bash
# Set environment before connecting
export USER=myusername
export DISPLAY=:0

# Connect
mono bin/otelnet.exe env-aware-server.example.com 23

# Client automatically sends:
# - USER variable
# - DISPLAY variable (if set)
```

---

### Example 20: Long-Running Sessions

**Scenario**: Maintain connection for extended period.

```bash
# Start connection
mono bin/otelnet.exe server.example.com 23

# Periodically check stats (without disconnecting)
# Every 30 minutes:
Ctrl+]
otelnet> stats

=== Connection Statistics ===
Bytes sent:     45,678
Bytes received: 234,567
Duration:       1,800 seconds (30 minutes)

otelnet> [press Enter to continue]

# Continue session...
# Check again later:
Ctrl+]
otelnet> stats

=== Connection Statistics ===
Bytes sent:     89,123
Bytes received: 456,789
Duration:       3,600 seconds (60 minutes)

otelnet> [press Enter]
```

**Tips**:
- Client handles SIGWINCH (window resize) automatically
- Connection remains stable for hours
- Use stats to monitor usage
- Clean exit with Ctrl+C or quit command

---

## Summary of Common Use Cases

| Use Case | Example | Key Features |
|----------|---------|--------------|
| **BBS** | Connect, read messages, download files | ANSI colors, character mode |
| **MUD** | Play text adventure games | Real-time updates, stats tracking |
| **Routers** | Configure network equipment | Line mode, copy-paste configs |
| **Testing** | Debug telnet servers | Protocol logging, debug output |
| **Automation** | Scripts for repeated connections | Shell integration, logging |

---

## Quick Command Reference

```bash
# Basic connection
mono bin/otelnet.exe <host> <port>

# Get help
mono bin/otelnet.exe --help

# Check version
mono bin/otelnet.exe --version

# While connected:
Ctrl+]          # Enter console mode
help            # Show console commands
stats           # Show statistics
ls [dir]        # List local files
pwd             # Show local directory
cd <dir>        # Change local directory
quit            # Disconnect and exit
[empty line]    # Return to client mode
Ctrl+C          # Disconnect immediately
```

---

**More Information**:
- [Quick Start Guide](QUICK_START.md)
- [User Manual](USER_MANUAL.md)
- [Troubleshooting](TROUBLESHOOTING.md)
- [README](../README.md)

**Version**: 1.0.0-mono
**Last Updated**: 2025-10-25
