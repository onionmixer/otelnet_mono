#!/bin/bash
# Otelnet Mono - Integration Test Suite
# Automated tests for Stage 13

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
echo " Otelnet Mono - Integration Test Suite"
echo " Stage 13: Comprehensive Testing"
echo "============================================"
echo ""

# Check if binary exists
if [ ! -f "bin/otelnet.exe" ]; then
    echo -e "${RED}Error: bin/otelnet.exe not found${NC}"
    echo "Please run 'make build' first"
    exit 1
fi

echo "Binary: bin/otelnet.exe ($(stat -f%z bin/otelnet.exe 2>/dev/null || stat -c%s bin/otelnet.exe) bytes)"
echo ""

# Category 1: Protocol Compliance Tests
echo "=== Category 1: Protocol Compliance ==="
echo ""

echo "Test 1.1: Local Test Servers"

# Test line mode server
echo "" | timeout 3 mono bin/otelnet.exe localhost 9091 2>&1 | grep -q "Connected" && RESULT=0 || RESULT=1
test_result $RESULT "Line Mode Server (port 9091)"

# Test character mode server
echo "" | timeout 3 mono bin/otelnet.exe localhost 9092 2>&1 | grep -q "Connected" && RESULT=0 || RESULT=1
test_result $RESULT "Character Mode Server (port 9092)"

# Test binary mode server
echo "" | timeout 3 mono bin/otelnet.exe localhost 9093 2>&1 | grep -q "Connected" && RESULT=0 || RESULT=1
test_result $RESULT "Binary Mode Server (port 9093)"

echo ""

# Category 2: Command-Line Interface Tests
echo "=== Category 2: Command-Line Interface ==="
echo ""

# Test help flag
mono bin/otelnet.exe --help 2>&1 | grep -q "Usage" && RESULT=0 || RESULT=1
test_result $RESULT "Help flag (--help)"

# Test version flag
mono bin/otelnet.exe --version 2>&1 | grep -q "otelnet version" && RESULT=0 || RESULT=1
test_result $RESULT "Version flag (--version)"

# Test invalid arguments
mono bin/otelnet.exe 2>&1 | grep -q "Usage" && RESULT=0 || RESULT=1
test_result $RESULT "Invalid arguments handling"

echo ""

# Category 3: Error Handling Tests
echo "=== Category 3: Error Handling ==="
echo ""

# Test invalid hostname
timeout 2 mono bin/otelnet.exe invalid.hostname.test 23 2>&1 | grep -q "Error" && RESULT=0 || RESULT=1
test_result $RESULT "Invalid hostname error"

# Test connection refused
timeout 2 mono bin/otelnet.exe localhost 12345 2>&1 | grep -qE "(refused|failed)" && RESULT=0 || RESULT=1
test_result $RESULT "Connection refused error"

# Test invalid port number
mono bin/otelnet.exe localhost invalid 2>&1 | grep -q "Invalid port" && RESULT=0 || RESULT=1
test_result $RESULT "Invalid port number error"

# Test out of range port
mono bin/otelnet.exe localhost 99999 2>&1 | grep -q "Port must be between" && RESULT=0 || RESULT=1
test_result $RESULT "Out of range port error"

echo ""

# Category 4: Statistics Tests
echo "=== Category 4: Statistics ==="
echo ""

# Test statistics display
OUTPUT=$(echo "" | timeout 3 mono bin/otelnet.exe localhost 9091 2>&1)
echo "$OUTPUT" | grep -q "=== Connection Statistics ===" && RESULT=0 || RESULT=1
test_result $RESULT "Statistics display"

echo "$OUTPUT" | grep -q "Bytes sent:" && RESULT=0 || RESULT=1
test_result $RESULT "Bytes sent counter"

echo "$OUTPUT" | grep -q "Bytes received:" && RESULT=0 || RESULT=1
test_result $RESULT "Bytes received counter"

echo "$OUTPUT" | grep -q "Duration:" && RESULT=0 || RESULT=1
test_result $RESULT "Duration counter"

echo ""

# Category 5: Protocol Negotiation Tests
echo "=== Category 5: Protocol Negotiation ==="
echo ""

# Test initial negotiations
OUTPUT=$(echo "" | timeout 3 mono bin/otelnet.exe localhost 9091 2>&1)

echo "$OUTPUT" | grep -q "IAC WILL BINARY" && RESULT=0 || RESULT=1
test_result $RESULT "BINARY option negotiation"

echo "$OUTPUT" | grep -q "IAC WILL SGA" && RESULT=0 || RESULT=1
test_result $RESULT "SGA option negotiation"

echo "$OUTPUT" | grep -q "IAC DO ECHO" && RESULT=0 || RESULT=1
test_result $RESULT "ECHO option negotiation"

echo "$OUTPUT" | grep -q "IAC WILL TTYPE" && RESULT=0 || RESULT=1
test_result $RESULT "TTYPE option negotiation"

echo "$OUTPUT" | grep -q "IAC WILL NAWS" && RESULT=0 || RESULT=1
test_result $RESULT "NAWS option negotiation"

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

if [ $FAILED_TESTS -eq 0 ]; then
    echo -e "${GREEN}All tests passed!${NC}"
    echo ""
    exit 0
else
    PASS_RATE=$((PASSED_TESTS * 100 / TOTAL_TESTS))
    echo -e "${YELLOW}Some tests failed${NC}"
    echo "Pass rate: $PASS_RATE%"
    echo ""
    exit 1
fi
