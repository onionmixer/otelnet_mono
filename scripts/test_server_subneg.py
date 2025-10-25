#!/usr/bin/env python3
"""
Comprehensive telnet test server for testing subnegotiations
Tests TTYPE, TSPEED, ENVIRON, NAWS, and LINEMODE
"""

import socket
import sys
import time

def main():
    port = 8882
    if len(sys.argv) > 1:
        port = int(sys.argv[1])

    # Telnet protocol constants
    IAC = 255
    WILL = 251
    WONT = 252
    DO = 253
    DONT = 254
    SB = 250
    SE = 240

    # Options
    TELOPT_BINARY = 0
    TELOPT_ECHO = 1
    TELOPT_SGA = 3
    TELOPT_TTYPE = 24
    TELOPT_NAWS = 31
    TELOPT_TSPEED = 32
    TELOPT_ENVIRON = 36
    TELOPT_LINEMODE = 34

    # Subnegotiation commands
    TTYPE_SEND = 1
    TTYPE_IS = 0
    ENV_SEND = 1
    LM_MODE = 1
    MODE_EDIT = 0x01

    server = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    server.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
    server.bind(('0.0.0.0', port))
    server.listen(1)

    print(f"Telnet subnegotiation test server listening on port {port}")
    print("Waiting for connection...")

    conn, addr = server.accept()
    print(f"Connection from {addr}")

    try:
        # Welcome message
        welcome = b"Welcome to Subnegotiation Test Server\\r\\n\\r\\n"
        conn.send(welcome)
        print(f"[SENT] Welcome message")

        # Negotiate options that support subnegotiation

        # Request TTYPE
        iac_do_ttype = bytes([IAC, DO, TELOPT_TTYPE])
        conn.send(iac_do_ttype)
        print(f"[SENT] IAC DO TTYPE")

        # Request TSPEED
        iac_do_tspeed = bytes([IAC, DO, TELOPT_TSPEED])
        conn.send(iac_do_tspeed)
        print(f"[SENT] IAC DO TSPEED")

        # Request ENVIRON
        iac_do_environ = bytes([IAC, DO, TELOPT_ENVIRON])
        conn.send(iac_do_environ)
        print(f"[SENT] IAC DO ENVIRON")

        # Request NAWS
        iac_do_naws = bytes([IAC, DO, TELOPT_NAWS])
        conn.send(iac_do_naws)
        print(f"[SENT] IAC DO NAWS")

        # Request LINEMODE
        iac_do_linemode = bytes([IAC, DO, TELOPT_LINEMODE])
        conn.send(iac_do_linemode)
        print(f"[SENT] IAC DO LINEMODE")

        # Wait for client responses
        time.sleep(0.5)

        # Read initial responses
        conn.settimeout(0.5)
        try:
            data = conn.recv(1024)
            if data:
                print(f"[RECV] {len(data)} bytes: {list(data)}")
        except socket.timeout:
            pass

        # Send TTYPE SEND subnegotiation
        ttype_send = bytes([IAC, SB, TELOPT_TTYPE, TTYPE_SEND, IAC, SE])
        conn.send(ttype_send)
        print(f"[SENT] IAC SB TTYPE SEND IAC SE")

        # Wait for response
        time.sleep(0.3)
        try:
            data = conn.recv(1024)
            if data:
                print(f"[RECV TTYPE] {len(data)} bytes: {list(data)}")
                # Parse TTYPE IS response
                if len(data) > 5 and data[0] == IAC and data[1] == SB and data[2] == TELOPT_TTYPE and data[3] == TTYPE_IS:
                    # Find SE
                    se_idx = -1
                    for i in range(4, len(data)-1):
                        if data[i] == IAC and data[i+1] == SE:
                            se_idx = i
                            break
                    if se_idx > 4:
                        term_type = data[4:se_idx].decode('ascii', errors='ignore')
                        print(f"  -> Terminal type: {term_type}")
        except socket.timeout:
            print("  -> No response to TTYPE SEND")

        # Send TSPEED SEND subnegotiation
        tspeed_send = bytes([IAC, SB, TELOPT_TSPEED, TTYPE_SEND, IAC, SE])
        conn.send(tspeed_send)
        print(f"[SENT] IAC SB TSPEED SEND IAC SE")

        # Wait for response
        time.sleep(0.3)
        try:
            data = conn.recv(1024)
            if data:
                print(f"[RECV TSPEED] {len(data)} bytes: {list(data)}")
                # Parse TSPEED IS response
                if len(data) > 5 and data[0] == IAC and data[1] == SB and data[2] == TELOPT_TSPEED and data[3] == TTYPE_IS:
                    se_idx = -1
                    for i in range(4, len(data)-1):
                        if data[i] == IAC and data[i+1] == SE:
                            se_idx = i
                            break
                    if se_idx > 4:
                        speed = data[4:se_idx].decode('ascii', errors='ignore')
                        print(f"  -> Terminal speed: {speed}")
        except socket.timeout:
            print("  -> No response to TSPEED SEND")

        # Send ENVIRON SEND subnegotiation
        environ_send = bytes([IAC, SB, TELOPT_ENVIRON, ENV_SEND, IAC, SE])
        conn.send(environ_send)
        print(f"[SENT] IAC SB ENVIRON SEND IAC SE")

        # Wait for response
        time.sleep(0.3)
        try:
            data = conn.recv(1024)
            if data:
                print(f"[RECV ENVIRON] {len(data)} bytes: {list(data)}")
                # Parse ENVIRON IS response
                if len(data) > 5 and data[0] == IAC and data[1] == SB and data[2] == TELOPT_ENVIRON:
                    print(f"  -> Environment variables received")
        except socket.timeout:
            print("  -> No response to ENVIRON SEND")

        # Send LINEMODE MODE subnegotiation
        linemode_mode = bytes([IAC, SB, TELOPT_LINEMODE, LM_MODE, MODE_EDIT, IAC, SE])
        conn.send(linemode_mode)
        print(f"[SENT] IAC SB LINEMODE MODE EDIT IAC SE")

        # Wait for response
        time.sleep(0.3)
        try:
            data = conn.recv(1024)
            if data:
                print(f"[RECV LINEMODE] {len(data)} bytes: {list(data)}")
        except socket.timeout:
            print("  -> No response to LINEMODE MODE")

        # Send final message
        final = b"\\r\\nAll subnegotiations sent!\\r\\n"
        conn.send(final)
        print("[SENT] Final message")

        # Wait for any remaining data
        time.sleep(1)
        try:
            while True:
                data = conn.recv(1024)
                if not data:
                    break
                print(f"[RECV] {len(data)} bytes: {list(data)}")
        except socket.timeout:
            pass

        print("\\nTest complete!")

    except Exception as e:
        print(f"Error: {e}")
        import traceback
        traceback.print_exc()
    finally:
        conn.close()
        server.close()

if __name__ == "__main__":
    main()
