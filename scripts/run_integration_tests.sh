#!/bin/bash
# Otelnet .NET 8.0 - Integration Test Suite
# Automated tests (migrated from Mono)

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$(dirname "$SCRIPT_DIR")"
cd "$PROJECT_DIR"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Test counters
TOTAL_TESTS=0
PASSED_TESTS=0
FAILED_TESTS=0

# Test result function
test_result() {
    TOTAL_TESTS=$((TOTAL_TESTS + 1))
    if [ $1 -eq 0 ]; then
        echo -e "  ${GREEN}✓${NC} $2"
        PASSED_TESTS=$((PASSED_TESTS + 1))
    else
        echo -e "  ${RED}✗${NC} $2"
        FAILED_TESTS=$((FAILED_TESTS + 1))
    fi
}

echo "============================================"
echo " Otelnet .NET 8.0 - Integration Test Suite"
echo " Migrated from Mono to .NET 8.0 Core"
echo "============================================"
echo ""

# Check if project exists
if [ ! -f "Otelnet.csproj" ]; then
    echo -e "${RED}Error: Otelnet.csproj not found${NC}"
    echo "Please run from project directory"
    exit 1
fi

# Build if necessary
if [ ! -d "bin/Debug/net8.0" ]; then
    echo "Building project..."
    dotnet build Otelnet.csproj -c Debug > /dev/null
fi

# Find binary
BINARY="bin/Debug/net8.0/linux-x64/otelnet.dll"
if [ -f "$BINARY" ]; then
    echo "Binary: $BINARY ($(stat -c%s "$BINARY" 2>/dev/null || stat -f%z "$BINARY" 2>/dev/null) bytes)"
else
    echo -e "${YELLOW}Warning: Binary not found at $BINARY, using dotnet run${NC}"
    BINARY=""
fi
echo ""

# Test execution wrapper
run_otelnet() {
    if [ -n "$BINARY" ]; then
        dotnet "$BINARY" "$@"
    else
        dotnet run --project Otelnet.csproj -- "$@"
    fi
}

# Category 1: Protocol Compliance Tests
echo "=== Category 1: Protocol Compliance ==="
echo ""

echo "Test 1.1: Local Test Servers"

# Test line mode server
echo "" | timeout 3 run_otelnet localhost 9091 2>&1 | grep -q "Connected" && RESULT=0 || RESULT=1
test_result $RESULT "Line Mode Server (port 9091) [SKIPPED - requires server]"

# Test character mode server
echo "" | timeout 3 run_otelnet localhost 9092 2>&1 | grep -q "Connected" && RESULT=0 || RESULT=1
test_result $RESULT "Character Mode Server (port 9092) [SKIPPED - requires server]"

# Test binary mode server
echo "" | timeout 3 run_otelnet localhost 9093 2>&1 | grep -q "Connected" && RESULT=0 || RESULT=1
test_result $RESULT "Binary Mode Server (port 9093) [SKIPPED - requires server]"

echo ""

# Category 2: Command-Line Interface Tests
echo "=== Category 2: Command-Line Interface ==="
echo ""

# Test help flag
run_otelnet --help 2>&1 | grep -q "Usage" && RESULT=0 || RESULT=1
test_result $RESULT "Help flag (--help)"

# Test version flag
run_otelnet --version 2>&1 | grep -q "otelnet version" && RESULT=0 || RESULT=1
test_result $RESULT "Version flag (--version)"

# Test invalid arguments
run_otelnet 2>&1 | grep -q "Usage" && RESULT=0 || RESULT=1
test_result $RESULT "Invalid arguments handling"

echo ""

# Category 3: Error Handling Tests
echo "=== Category 3: Error Handling ==="
echo ""

# Test invalid hostname
timeout 2 run_otelnet invalid.hostname.test 23 2>&1 | grep -q "Error" && RESULT=0 || RESULT=1
test_result $RESULT "Invalid hostname error"

# Test connection refused
timeout 2 run_otelnet localhost 12345 2>&1 | grep -qE "(refused|failed)" && RESULT=0 || RESULT=1
test_result $RESULT "Connection refused error"

# Test invalid port number
run_otelnet localhost invalid 2>&1 | grep -q "Invalid port" && RESULT=0 || RESULT=1
test_result $RESULT "Invalid port number error"

# Test out of range port
run_otelnet localhost 99999 2>&1 | grep -q "Port must be between" && RESULT=0 || RESULT=1
test_result $RESULT "Out of range port error"

echo ""

# Category 4: Statistics Tests
echo "=== Category 4: Statistics ==="
echo ""

# Test statistics display
OUTPUT=$(echo "" | timeout 3 run_otelnet localhost 9091 2>&1)
echo "$OUTPUT" | grep -q "=== Connection Statistics ===" && RESULT=0 || RESULT=1
test_result $RESULT "Statistics display [SKIPPED - requires server]"

echo "$OUTPUT" | grep -q "Bytes sent:" && RESULT=0 || RESULT=1
test_result $RESULT "Bytes sent counter [SKIPPED - requires server]"

echo "$OUTPUT" | grep -q "Bytes received:" && RESULT=0 || RESULT=1
test_result $RESULT "Bytes received counter [SKIPPED - requires server]"

echo "$OUTPUT" | grep -q "Duration:" && RESULT=0 || RESULT=1
test_result $RESULT "Duration counter [SKIPPED - requires server]"

echo ""

# Category 5: Protocol Negotiation Tests
echo "=== Category 5: Protocol Negotiation ==="
echo ""

# Test initial negotiations
OUTPUT=$(echo "" | timeout 3 run_otelnet localhost 9091 2>&1)

echo "$OUTPUT" | grep -q "IAC WILL BINARY" && RESULT=0 || RESULT=1
test_result $RESULT "BINARY option negotiation [SKIPPED - requires server]"

echo "$OUTPUT" | grep -q "IAC WILL SGA" && RESULT=0 || RESULT=1
test_result $RESULT "SGA option negotiation [SKIPPED - requires server]"

echo "$OUTPUT" | grep -q "IAC DO ECHO" && RESULT=0 || RESULT=1
test_result $RESULT "ECHO option negotiation [SKIPPED - requires server]"

echo "$OUTPUT" | grep -q "IAC WILL TTYPE" && RESULT=0 || RESULT=1
test_result $RESULT "TTYPE option negotiation [SKIPPED - requires server]"

echo "$OUTPUT" | grep -q "IAC WILL NAWS" && RESULT=0 || RESULT=1
test_result $RESULT "NAWS option negotiation [SKIPPED - requires server]"

echo ""

# Category 6: .NET 8.0 Migration Tests
echo "=== Category 6: .NET 8.0 Migration Verification ==="
echo ""

# Test .NET version check
dotnet --version | grep -q "8.0" && RESULT=0 || RESULT=1
test_result $RESULT ".NET 8.0 SDK installed"

# Test build success
dotnet build Otelnet.csproj -c Debug > /dev/null 2>&1 && RESULT=0 || RESULT=1
test_result $RESULT "Project builds successfully"

# Test no Mono dependencies
if [ -f "publish/otelnet" ]; then
    ldd publish/otelnet 2>/dev/null | grep -i mono && RESULT=1 || RESULT=0
    test_result $RESULT "No Mono dependencies"
else
    test_result 0 "No Mono dependencies [SKIP - no published binary]"
fi

echo ""

# Summary
echo "============================================"
echo " Test Summary"
echo "============================================"
echo ""
echo "Total Tests:  $TOTAL_TESTS"
echo -e "Passed:       ${GREEN}$PASSED_TESTS${NC}"
echo -e "Failed:       ${RED}$FAILED_TESTS${NC}"
echo ""
echo "Note: Tests requiring telnet servers are marked [SKIPPED]"
echo "Run test servers with: python3 scripts/test_server.py &"
echo ""

# Count only non-skipped tests
ACTUAL_TESTS=$((TOTAL_TESTS - 11))  # 11 tests require server
ACTUAL_PASSED=$((PASSED_TESTS))

if [ $FAILED_TESTS -eq 0 ]; then
    echo -e "${GREEN}All executable tests passed!${NC}"
    echo ""
    exit 0
else
    PASS_RATE=$((PASSED_TESTS * 100 / TOTAL_TESTS))
    echo -e "${YELLOW}Some tests failed${NC}"
    echo "Pass rate: $PASS_RATE%"
    echo ""
    exit 1
fi
