# Integration Test Plan - Stage 13

**Date**: 2025-10-25
**Version**: 1.0.0-mono
**Purpose**: Comprehensive testing of otelnet_mono with real-world scenarios

---

## Test Objectives

1. Validate protocol implementation with real telnet servers
2. Verify console mode functionality in live sessions
3. Test error handling and edge cases
4. Measure performance and resource usage
5. Ensure UTF-8 and international character support
6. Validate signal handling and terminal control

---

## Test Categories

### Category 1: Protocol Compliance Tests

#### Test 1.1: Local Test Servers (Automated)
**Status**: ✅ COMPLETED
**Servers**:
- Line Mode Server (localhost:9091)
- Character Mode Server (localhost:9092)
- Binary Mode Server (localhost:9093)
- Subnegotiation Test Server (localhost:8882)

**Test Procedure**:
```bash
# Test line mode server
echo -e "test\nquit" | timeout 3 mono bin/otelnet.exe localhost 9091

# Test character mode server
echo -e "test\nquit" | timeout 3 mono bin/otelnet.exe localhost 9092

# Test binary mode server
echo -e "test\nquit" | timeout 3 mono bin/otelnet.exe localhost 9093
```

**Expected Results**:
- ✅ Connection established
- ✅ Option negotiation completed
- ✅ Mode detected correctly
- ✅ Data transmitted and received
- ✅ Statistics displayed on exit

#### Test 1.2: Public Telnet Servers (Manual)
**Status**: ⏳ MANUAL TESTING REQUIRED

**Test Servers**:
1. **towel.blinkenlights.nl:23** - Star Wars ASCII animation
   - Purpose: Stress test with continuous data stream
   - Expected: Smooth animation, no data loss
   - Duration: 2-3 minutes

2. **telehack.com:23** - Retro computing simulation
   - Purpose: Interactive command processing
   - Expected: Responsive input/output, proper echo handling
   - Duration: 5 minutes

3. **mud.arctic.org:2700** - MUD game server
   - Purpose: Real-time interactive text game
   - Expected: Real-time updates, proper character mode
   - Duration: 5 minutes

**Test Procedure**:
```bash
# Connect to server
mono bin/otelnet.exe <hostname> <port>

# Observe:
1. Connection establishment
2. Initial server messages
3. Input responsiveness
4. Output display quality
5. Character encoding (UTF-8)
6. Console mode (Ctrl+])
7. Statistics accuracy
```

**Success Criteria**:
- [ ] All servers connect successfully
- [ ] Data displays correctly without corruption
- [ ] Keyboard input works properly
- [ ] No crashes or freezes
- [ ] Console mode accessible via Ctrl+]
- [ ] Clean disconnection

---

### Category 2: Console Mode Tests

#### Test 2.1: Console Mode Activation
**Test Procedure**:
```bash
# 1. Connect to any telnet server
mono bin/otelnet.exe localhost 9091

# 2. Press Ctrl+]
# Expected: Console mode prompt appears

# 3. Type 'help' and press Enter
# Expected: Help menu displays

# 4. Press Enter (empty line)
# Expected: Returns to client mode
```

**Success Criteria**:
- [ ] Ctrl+] triggers console mode
- [ ] Prompt "otelnet> " appears
- [ ] Help command lists all commands
- [ ] Empty line returns to client mode

#### Test 2.2: Console Commands
**Commands to Test**:

1. **help** - Display help menu
   ```
   otelnet> help
   # Expected: Full command list with examples
   ```

2. **stats** - Show connection statistics
   ```
   otelnet> stats
   # Expected: Bytes sent/received, duration
   ```

3. **ls** - List current directory
   ```
   otelnet> ls
   # Expected: Directory contents
   ```

4. **pwd** - Print working directory
   ```
   otelnet> pwd
   # Expected: Current directory path
   ```

5. **cd** - Change directory
   ```
   otelnet> cd /tmp
   otelnet> pwd
   # Expected: /tmp
   ```

6. **quit** - Exit program
   ```
   otelnet> quit
   # Expected: Clean exit with statistics
   ```

**Success Criteria**:
- [ ] All commands execute correctly
- [ ] Error messages clear and helpful
- [ ] No crashes on invalid input
- [ ] Prompt reappears after each command

---

### Category 3: UTF-8 and Character Encoding Tests

#### Test 3.1: UTF-8 Display
**Test Procedure**:
```bash
# Create a test file with UTF-8 characters
cat > /tmp/utf8_test.txt << 'EOF'
English: Hello World
한국어: 안녕하세요
日本語: こんにちは
中文: 你好世界
Emoji: 🚀 ✨ 🎉
EOF

# Connect and test display
mono bin/otelnet.exe localhost 9091
# Type characters and observe echo
```

**Success Criteria**:
- [ ] ASCII characters display correctly
- [ ] Korean characters display correctly
- [ ] Japanese characters display correctly
- [ ] Chinese characters display correctly
- [ ] Emoji display or graceful fallback
- [ ] No character corruption

#### Test 3.2: UTF-8 Input
**Test Procedure**:
- Type international characters at prompt
- Paste multi-byte UTF-8 text
- Verify proper transmission to server

**Success Criteria**:
- [ ] Multi-byte characters accepted
- [ ] Characters transmitted correctly
- [ ] No input corruption

---

### Category 4: Error Handling Tests

#### Test 4.1: Connection Errors
**Test Cases**:

1. **Invalid hostname**
   ```bash
   mono bin/otelnet.exe invalid.hostname.test 23
   # Expected: Clear error message, no crash
   ```

2. **Connection refused**
   ```bash
   mono bin/otelnet.exe localhost 12345
   # Expected: Connection refused error, clean exit
   ```

3. **Connection timeout**
   ```bash
   mono bin/otelnet.exe 10.255.255.1 23
   # Expected: Timeout after reasonable period
   ```

**Success Criteria**:
- [ ] Clear error messages
- [ ] No crashes or hangs
- [ ] Graceful exit
- [ ] Resources cleaned up properly

#### Test 4.2: Runtime Errors
**Test Cases**:

1. **Server disconnect during session**
   - Connect to server
   - Server kills connection
   - Expected: Clean disconnect message, statistics displayed

2. **Network interruption**
   - Connect to remote server
   - Disconnect network
   - Expected: Error detected, clean exit

3. **Invalid console commands**
   ```
   otelnet> invalidcommand
   # Expected: Error message, prompt returns
   ```

**Success Criteria**:
- [ ] Errors detected and reported
- [ ] No data corruption
- [ ] Clean recovery or exit
- [ ] No resource leaks

---

### Category 5: Signal Handling Tests

#### Test 5.1: Ctrl+C (SIGINT)
**Test Procedure**:
```bash
# 1. Connect to server
mono bin/otelnet.exe localhost 9091

# 2. Press Ctrl+C
# Expected: Clean exit with statistics
```

**Success Criteria**:
- [ ] Immediate response to Ctrl+C
- [ ] Statistics displayed
- [ ] Connection closed properly
- [ ] Terminal restored
- [ ] No error messages

#### Test 5.2: Window Resize (SIGWINCH)
**Test Procedure**:
```bash
# 1. Connect to server that supports NAWS
mono bin/otelnet.exe localhost 9091

# 2. Resize terminal window
# 3. Check debug output for NAWS updates
```

**Success Criteria**:
- [ ] Window resize detected
- [ ] NAWS update sent to server
- [ ] New size reported correctly
- [ ] No disruption to session

#### Test 5.3: SIGTERM
**Test Procedure**:
```bash
# 1. Connect in one terminal
mono bin/otelnet.exe localhost 9091 &
PID=$!

# 2. Send SIGTERM from another terminal
kill -TERM $PID

# Expected: Clean shutdown
```

**Success Criteria**:
- [ ] Signal caught
- [ ] Clean shutdown
- [ ] Statistics displayed
- [ ] No zombie processes

---

### Category 6: Performance Tests

#### Test 6.1: Data Throughput
**Test Procedure**:
```bash
# Use binary mode server with large data transfer
# Monitor bytes received over time
```

**Metrics to Collect**:
- Bytes per second throughput
- CPU usage during transfer
- Memory usage during transfer
- Latency (input to echo)

**Target Performance**:
- [ ] Throughput: > 1 MB/s
- [ ] CPU usage: < 10% (idle), < 30% (active)
- [ ] Memory usage: < 50 MB
- [ ] Latency: < 100ms for local connections

#### Test 6.2: Long-Running Sessions
**Test Procedure**:
```bash
# Connect and maintain session for extended period
# Monitor resource usage over time
```

**Duration**: 30 minutes minimum

**Success Criteria**:
- [ ] No memory leaks
- [ ] Stable CPU usage
- [ ] No performance degradation
- [ ] Session remains responsive

#### Test 6.3: Rapid Command Execution
**Test Procedure**:
- Enter console mode
- Execute commands rapidly
- Type text quickly in client mode
- Monitor responsiveness

**Success Criteria**:
- [ ] No input loss
- [ ] No output corruption
- [ ] Stable performance
- [ ] No buffer overflows

---

### Category 7: Edge Cases

#### Test 7.1: Very Long Lines
**Test Procedure**:
- Type or paste lines > 1024 characters
- Verify proper handling

**Success Criteria**:
- [ ] Long lines handled correctly
- [ ] No truncation or corruption
- [ ] No crashes

#### Test 7.2: Binary Data
**Test Procedure**:
- Connect to binary mode server
- Transmit binary data (all byte values 0-255)
- Verify correct transmission

**Success Criteria**:
- [ ] All byte values transmitted
- [ ] IAC (255) properly escaped
- [ ] No data corruption

#### Test 7.3: Rapid Option Negotiation
**Test Procedure**:
- Connect to server that sends many WILL/WONT/DO/DONT
- Verify no negotiation loops
- Check state machine stability

**Success Criteria**:
- [ ] Loop prevention working
- [ ] All negotiations complete
- [ ] No infinite loops
- [ ] Correct final state

---

## Test Environment

### Required Setup
- Linux system with Mono installed
- Network connectivity
- Local test servers running (ports 9091-9093, 8882)
- Terminal emulator (e.g., gnome-terminal, xterm)
- Root access (for signal testing)

### Test Data
- UTF-8 test file with international characters
- Binary test data (0-255 byte values)
- Large text file (> 1 MB) for throughput testing

---

## Test Execution

### Automated Tests
Location: `scripts/run_integration_tests.sh`

```bash
#!/bin/bash
# Run automated integration tests

set -e

echo "=== Otelnet Mono Integration Tests ==="
echo ""

# Test 1: Local servers
echo "Test 1.1: Line Mode Server"
echo "test" | timeout 3 mono bin/otelnet.exe localhost 9091 > /dev/null
echo "  ✓ Line mode test passed"

echo "Test 1.1: Character Mode Server"
echo "test" | timeout 3 mono bin/otelnet.exe localhost 9092 > /dev/null
echo "  ✓ Character mode test passed"

echo "Test 1.1: Binary Mode Server"
echo "test" | timeout 3 mono bin/otelnet.exe localhost 9093 > /dev/null
echo "  ✓ Binary mode test passed"

# Test 2: Error handling
echo ""
echo "Test 4.1: Invalid hostname"
mono bin/otelnet.exe invalid.test 23 2>&1 | grep -q "Error" && echo "  ✓ Error handling works"

# Test 3: Help command
echo ""
echo "Test: Help display"
mono bin/otelnet.exe --help | grep -q "Usage" && echo "  ✓ Help display works"

echo ""
echo "=== Automated tests completed ==="
```

### Manual Test Checklist

**Before Testing**:
- [ ] Build project (`make build`)
- [ ] Start local test servers
- [ ] Verify network connectivity
- [ ] Prepare test terminal

**During Testing**:
- [ ] Document any unexpected behavior
- [ ] Take screenshots of issues
- [ ] Record error messages
- [ ] Note performance observations

**After Testing**:
- [ ] Stop test servers
- [ ] Review logs
- [ ] Compile test results
- [ ] Update STAGE13_COMPLETION.md

---

## Expected Results Summary

### Must Pass (Critical)
- ✅ Connection to all local test servers
- ✅ Basic data transmission
- ✅ Console mode activation (Ctrl+])
- ✅ All console commands functional
- ✅ Clean exit (quit command, Ctrl+C)
- ✅ Statistics display
- ✅ No crashes or hangs

### Should Pass (Important)
- ⏳ Public server connectivity
- ⏳ UTF-8 character display
- ⏳ Error handling (all cases)
- ⏳ Signal handling (SIGWINCH, SIGTERM)
- ⏳ Performance targets met
- ⏳ Long-running session stability

### Nice to Have (Optional)
- ⏳ Emoji display
- ⏳ Very long line handling (> 10KB)
- ⏳ Extreme stress testing
- ⏳ Memory profiling

---

## Test Results Documentation

Results should be documented in `STAGE13_COMPLETION.md` with:
- Test execution date/time
- Test environment details
- Pass/fail status for each test
- Performance measurements
- Screenshots (if applicable)
- Known issues discovered
- Recommendations for fixes

---

## Risk Assessment

### High Risk Areas
1. **Raw terminal mode** - May fail in non-TTY environments
2. **Signal handling** - Platform-dependent behavior
3. **UTF-8 encoding** - Terminal must support UTF-8
4. **Public servers** - May be unavailable or changed

### Mitigation Strategies
1. Document TTY requirement clearly
2. Test on multiple Linux distributions
3. Provide UTF-8 configuration guide
4. Test with local fallback servers

---

**Test Plan Version**: 1.0
**Created**: 2025-10-25
**Updated**: 2025-10-25
**Status**: READY FOR EXECUTION
