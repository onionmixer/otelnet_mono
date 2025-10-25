# Otelnet Mono - Troubleshooting Guide

**Solutions to common problems**

---

## Table of Contents

1. [Installation Issues](#installation-issues)
2. [Connection Problems](#connection-problems)
3. [Display Issues](#display-issues)
4. [Input Problems](#input-problems)
5. [Console Mode Issues](#console-mode-issues)
6. [Performance Problems](#performance-problems)
7. [Error Messages](#error-messages)
8. [Getting More Help](#getting-more-help)

---

## Installation Issues

### Problem: "mono: command not found"

**Symptom**:
```bash
$ mono bin/otelnet.exe
bash: mono: command not found
```

**Cause**: Mono runtime is not installed.

**Solution**:

Install Mono for your system:

**Ubuntu/Debian**:
```bash
sudo apt-get update
sudo apt-get install mono-complete
```

**Fedora**:
```bash
sudo dnf install mono-complete
```

**Verify installation**:
```bash
mono --version
# Should show Mono JIT compiler version
```

---

### Problem: Build fails with "mcs: command not found"

**Symptom**:
```bash
$ make build
mcs: command not found
```

**Cause**: Mono C# compiler not in PATH.

**Solution**:

Ensure `mono-complete` is installed (includes mcs):
```bash
sudo apt-get install mono-complete  # Ubuntu/Debian
sudo dnf install mono-devel         # Fedora
```

**Verify**:
```bash
which mcs
mcs --version
```

---

### Problem: Compilation errors about missing types

**Symptom**:
```
error CS0518: The predefined type 'System.Object' is not defined
```

**Cause**: Missing reference to mscorlib.

**Solution**:

This should not happen with the current build configuration. If it does:

1. Check `OtelnetMono.csproj` contains:
   ```xml
   <Reference Include="mscorlib" />
   <Reference Include="System" />
   ```

2. Clean and rebuild:
   ```bash
   make clean
   make build
   ```

---

## Connection Problems

### Problem: "Connection refused"

**Symptom**:
```
Error: Connection to localhost:23 refused
```

**Causes**:
1. Server not running
2. Wrong port number
3. Firewall blocking connection
4. Server not accepting connections

**Solutions**:

**1. Check if server is running**:
```bash
netstat -tln | grep :23
# Should show LISTEN on port 23
```

**2. Try different port**:
```bash
# Maybe telnet runs on different port
mono bin/otelnet.exe localhost 2323
```

**3. Check firewall**:
```bash
# Ubuntu/Debian
sudo ufw status

# Fedora
sudo firewall-cmd --list-all
```

**4. Test with standard telnet**:
```bash
telnet localhost 23
# If this fails too, problem is server-side
```

---

### Problem: "Invalid hostname" error

**Symptom**:
```
Error: Could not resolve hostname: invalid.example.test
```

**Causes**:
1. Hostname doesn't exist
2. DNS issue
3. Typo in hostname

**Solutions**:

**1. Verify hostname**:
```bash
ping example.com
# Or
nslookup example.com
```

**2. Use IP address instead**:
```bash
mono bin/otelnet.exe 192.168.1.100 23
```

**3. Check /etc/hosts** (for local names):
```bash
cat /etc/hosts | grep myserver
```

---

### Problem: Connection timeout

**Symptom**:
Connection hangs and eventually times out.

**Causes**:
1. Server not responding
2. Network issue
3. Firewall dropping packets

**Solutions**:

**1. Check network connectivity**:
```bash
ping example.com
```

**2. Try with timeout**:
```bash
timeout 10 mono bin/otelnet.exe example.com 23
# Times out after 10 seconds
```

**3. Check if port is open**:
```bash
nc -zv example.com 23
# or
telnet example.com 23
```

---

## Display Issues

### Problem: Garbled or corrupted output

**Symptom**:
Text appears as garbage characters or boxes.

**Causes**:
1. Character encoding mismatch
2. Terminal doesn't support UTF-8
3. Binary data in output

**Solutions**:

**1. Set terminal to UTF-8**:
```bash
export LANG=en_US.UTF-8
export LC_ALL=en_US.UTF-8
```

**2. Check terminal settings**:
- Right-click terminal → Preferences → Encoding → UTF-8

**3. Try different terminal**:
```bash
# Try in xterm
xterm -e mono bin/otelnet.exe localhost 23

# Try in gnome-terminal
gnome-terminal -- mono bin/otelnet.exe localhost 23
```

---

### Problem: "[ERROR] Failed to get terminal attributes"

**Symptom**:
```
[ERROR] Failed to get terminal attributes
```

**Cause**: Not running in a proper TTY (e.g., piped input/output).

**Impact**: Raw mode cannot be enabled, but client still functions.

**Solutions**:

**1. Run from actual terminal** (recommended):
```bash
# In a real terminal window:
mono bin/otelnet.exe localhost 23
```

**2. Accept warning** (safe to ignore):
The client will work without raw mode for most use cases.

**3. Avoid pipes/redirects**:
```bash
# DON'T do this:
echo "data" | mono bin/otelnet.exe localhost 23
```

---

### Problem: "[WARNING] Failed to get window size"

**Symptom**:
```
[WARNING] Failed to get window size
```

**Cause**: Terminal doesn't support TIOCGWINSZ ioctl.

**Impact**: NAWS (window size) not sent to server.

**Solution**:

**Safe to ignore** - most servers don't require NAWS.

If server requires window size:
1. Use a different terminal emulator
2. Or manually edit code to set fixed size in `src/Program.cs`

---

### Problem: Colors not displayed

**Symptom**:
ANSI color codes shown as `^[[31m` instead of colors.

**Cause**: Terminal not interpreting ANSI escape codes.

**Solutions**:

**1. Enable color support**:
```bash
export TERM=xterm-256color
```

**2. Use different terminal**:
```bash
# Use xterm
xterm -e mono bin/otelnet.exe localhost 23
```

**3. Check server**: Some servers don't send color codes unless terminal type is correct (client sends XTERM by default).

---

## Input Problems

### Problem: Keystrokes not appearing

**Symptom**:
You type but nothing appears on screen.

**Causes**:
1. Server echo disabled
2. Character mode active
3. Terminal in wrong mode

**Solutions**:

**1. Check if it's server-side**:
Your input might be reaching the server even if not displayed. Try typing a command you know works (e.g., `help` and press Enter).

**2. This is often normal**: In character mode, the server controls echo. If server doesn't echo, you won't see your typing.

**3. Exit and reconnect**:
```
Ctrl+C
# Reconnect
mono bin/otelnet.exe localhost 23
```

---

### Problem: Double characters appearing

**Symptom**:
Every character you type appears twice: `hheelllloo`

**Cause**: Both local and remote echo enabled.

**Solution**:

**Normal behavior** during initial connection while negotiating. Should stop after 1-2 seconds.

If persists:
1. Wait for negotiation to complete
2. Or disconnect and reconnect
3. Or check server configuration

---

### Problem: Ctrl+] doesn't work

**Symptom**:
Pressing Ctrl+] doesn't enter console mode.

**Causes**:
1. Terminal doesn't send Ctrl+] correctly
2. Key binding conflict
3. Client not recognizing key

**Solutions**:

**1. Test key detection**:
Try pressing Ctrl+] - you should see console mode prompt.

**2. Check terminal key bindings**:
Some terminals may intercept Ctrl+].

**3. Alternative**: Use Ctrl+C to exit, no console mode needed for basic use.

---

### Problem: Backspace doesn't work

**Symptom**:
Backspace prints `^H` or `^?` instead of deleting.

**Cause**: Server expects different backspace character.

**Solutions**:

**1. In console mode**: Backspace should work correctly.

**2. In client mode**: This is controlled by the server. Try:
- Delete key instead of Backspace
- Ctrl+H
- Ctrl+Backspace

**3. Terminal settings**:
```bash
# Set backspace to send ^H
stty erase ^H
```

---

## Console Mode Issues

### Problem: Console commands not working

**Symptom**:
```
otelnet> help
[Unknown command: help...]
```

**Cause**: Typo or command doesn't exist.

**Solutions**:

**1. List available commands**:
```
otelnet> help
```

**2. Check spelling**:
- `help` (not `Help` or `HELP`)
- `stats` (not `stat` or `statistics`)
- `quit` (not `exit` - wait, `exit` works too!)

**3. Check you're in console mode**:
You should see `otelnet>` prompt. If not, press Ctrl+].

---

### Problem: Can't return to client mode

**Symptom**:
After entering console mode, can't get back to telnet session.

**Solution**:

**Press Enter with empty line**:
```
otelnet>
[Back to client mode]
```

Don't type anything, just press Enter.

---

### Problem: "ls" shows wrong directory

**Symptom**:
`ls` command shows unexpected directory contents.

**Cause**: Current working directory is not what you think.

**Solution**:

**1. Check current directory**:
```
otelnet> pwd
/unexpected/path
```

**2. Change to desired directory**:
```
otelnet> cd /home/user
otelnet> ls
```

**3. Remember**: Console commands run locally, not on the telnet server!

---

## Performance Problems

### Problem: Slow response time

**Symptom**:
Noticeable delay between typing and seeing characters.

**Causes**:
1. Network latency
2. Slow server
3. High CPU usage

**Solutions**:

**1. Check network latency**:
```bash
ping -c 10 server.example.com
# Look at avg time
```

**2. Check CPU usage**:
```bash
top
# Look for otelnet.exe process
```

**3. Try local server**:
```bash
# Should be instant
mono bin/otelnet.exe localhost 23
```

---

### Problem: High CPU usage

**Symptom**:
`otelnet.exe` using 50%+ CPU.

**Cause**: Polling loop when no data available.

**Impact**: Normal during active use, should be <5% when idle.

**Solutions**:

**1. Check if actually a problem**:
```bash
top -p $(pgrep -f otelnet.exe)
# Monitor CPU over time
```

**2. Normal during active transfer**: High CPU is expected when receiving lots of data.

**3. If high when idle**: This might indicate a bug. Try:
- Disconnect and reconnect
- Check for updates
- Report issue

---

### Problem: Memory usage increasing

**Symptom**:
Memory usage grows over time.

**Expected**: Should stay under 50 MB.

**Solutions**:

**1. Monitor memory**:
```bash
watch -n 1 "ps aux | grep otelnet.exe"
```

**2. If growing continuously**: Possible memory leak (shouldn't happen with current code).

**3. Disconnect and reconnect**: Resets state.

---

## Error Messages

### "Error: Invalid port number"

**Cause**: Port argument is not numeric.

**Example**:
```bash
# Wrong:
mono bin/otelnet.exe localhost telnet

# Right:
mono bin/otelnet.exe localhost 23
```

**Solution**: Use numeric port (1-65535).

---

### "Error: Port must be between 1 and 65535"

**Cause**: Port number out of valid range.

**Example**:
```bash
# Wrong:
mono bin/otelnet.exe localhost 99999

# Right:
mono bin/otelnet.exe localhost 8080
```

**Solution**: Use port between 1 and 65535.

---

### "[ERROR] Failed to get terminal attributes"

**Already covered** - see [Display Issues](#problem-error-failed-to-get-terminal-attributes).

---

### "Connection closed by remote host"

**Symptom**:
```
[Connection closed by remote host]

=== Connection Statistics ===
...
```

**Causes**:
1. Server terminated connection (normal)
2. Server crash
3. Network interruption
4. Idle timeout

**Solutions**:

**1. Normal disconnect**: If you logged out, this is expected.

**2. Check server logs**: See why server closed connection.

**3. Reconnect**:
```bash
mono bin/otelnet.exe server.example.com 23
```

---

## Getting More Help

### Diagnostic Information

When reporting issues, include:

**1. Version information**:
```bash
mono bin/otelnet.exe --version
mono --version
uname -a
```

**2. Connection details**:
- Server hostname/IP
- Port number
- Error messages (exact text)

**3. Test results**:
```bash
bash scripts/run_integration_tests.sh
```

**4. Server accessibility**:
```bash
telnet server.example.com 23
# Does standard telnet work?
```

### Debug Mode

Enable debug output by observing messages:

```
[DEBUG] Sent: IAC WILL BINARY
[DEBUG] Received: IAC DO BINARY
[INFO] Terminal size: 80x24
[WARNING] Failed to get window size
[ERROR] Connection refused
```

**Levels**:
- `[DEBUG]` - Protocol details (normal)
- `[INFO]` - Informational (normal)
- `[WARNING]` - Warning (usually safe to ignore)
- `[ERROR]` - Error (action needed)

### Testing with Local Servers

**Start test servers** (for developers):
```bash
# In separate terminals:
python3 scripts/test_server_subneg.py 8882
```

**Test connection**:
```bash
mono bin/otelnet.exe localhost 8882
```

If this works but real server doesn't, the issue is server-specific.

### Resources

- **Quick Start**: [QUICK_START.md](QUICK_START.md)
- **User Manual**: [USER_MANUAL.md](USER_MANUAL.md)
- **README**: [../README.md](../README.md)
- **Project Status**: [PROJECT_STATUS.md](PROJECT_STATUS.md)

### Common Solutions Summary

**90% of issues are solved by**:

1. ✅ Verify Mono is installed: `mono --version`
2. ✅ Rebuild: `make clean && make build`
3. ✅ Test with local server first
4. ✅ Check server is actually running
5. ✅ Use standard telnet to compare
6. ✅ Check firewall rules
7. ✅ Verify network connectivity: `ping server`
8. ✅ Run from proper terminal (not pipes)
9. ✅ Use UTF-8 encoding
10. ✅ Read error messages carefully

---

**Still having problems?**

1. Run automated tests: `bash scripts/run_integration_tests.sh`
2. Check if standard `telnet` works with your server
3. Review the [User Manual](USER_MANUAL.md)
4. Check the [Quick Start Guide](QUICK_START.md)

---

**Version**: 1.0.0-mono
**Last Updated**: 2025-10-25
