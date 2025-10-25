#!/usr/bin/env python3
"""
Simple telnet test server for testing otelnet RFC 854 implementation
Tests IAC processing, state machine, and basic commands
"""

import socket
import sys
import time

def main():
    port = 8881
    if len(sys.argv) > 1:
        port = int(sys.argv[1])

    # Telnet protocol constants
    IAC = 255  # Interpret As Command
    WILL = 251
    WONT = 252
    DO = 253
    DONT = 254
    SB = 250
    SE = 240
    NOP = 241
    AYT = 246
    GA = 249

    # Options
    TELOPT_BINARY = 0
    TELOPT_ECHO = 1
    TELOPT_SGA = 3

    server = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    server.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
    server.bind(('0.0.0.0', port))
    server.listen(1)

    print(f"Telnet test server listening on port {port}")
    print("Waiting for connection...")

    conn, addr = server.accept()
    print(f"Connection from {addr}")

    try:
        # Test 1: Send welcome message
        welcome = b"Welcome to Telnet Test Server\r\n"
        conn.send(welcome)
        print(f"[SENT] Welcome message: {welcome}")

        # Test 2: Send IAC NOP (should be ignored by client)
        iac_nop = bytes([IAC, NOP])
        conn.send(iac_nop)
        print(f"[SENT] IAC NOP: {list(iac_nop)}")

        # Test 3: Send regular data with embedded IAC IAC (escaped)
        # "Hello" + IAC IAC + "World"
        data_with_iac = b"Hello" + bytes([IAC, IAC]) + b"World\r\n"
        conn.send(data_with_iac)
        print(f"[SENT] Data with IAC IAC: {list(data_with_iac)}")
        print("  -> Client should see: 'HelloWorld' with single IAC(255) byte")

        # Test 4: Send IAC AYT (Are You There)
        iac_ayt = bytes([IAC, AYT])
        conn.send(iac_ayt)
        print(f"[SENT] IAC AYT: {list(iac_ayt)}")
        print("  -> Client should respond")

        # Test 5: Send option negotiation
        # IAC WILL ECHO
        iac_will_echo = bytes([IAC, WILL, TELOPT_ECHO])
        conn.send(iac_will_echo)
        print(f"[SENT] IAC WILL ECHO: {list(iac_will_echo)}")

        # Test 6: Send CR NUL (carriage return only)
        cr_nul = b"Line1" + bytes([13, 0]) + b"Line2\r\n"
        conn.send(cr_nul)
        print(f"[SENT] CR NUL sequence: {list(cr_nul)}")

        # Test 7: Send CR LF (newline)
        cr_lf = b"Line3\r\nLine4\r\n"
        conn.send(cr_lf)
        print(f"[SENT] CR LF sequence: {list(cr_lf)}")

        # Wait for client responses
        print("\nWaiting for client responses...")
        time.sleep(2)

        # Read any client data
        conn.settimeout(1.0)
        try:
            while True:
                data = conn.recv(1024)
                if not data:
                    break
                print(f"[RECV] {len(data)} bytes: {list(data)}")
                print(f"       ASCII: {data}")
        except socket.timeout:
            pass

        print("\nTest server finished")

    except Exception as e:
        print(f"Error: {e}")
    finally:
        conn.close()
        server.close()

if __name__ == "__main__":
    main()
