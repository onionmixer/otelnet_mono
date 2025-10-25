using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace Otelnet.Telnet;

/// <summary>
/// Telnet connection class - manages connection and protocol handling
/// </summary>
public class TelnetConnection : IDisposable
{
    // ====================================================================
    // Private Fields
    // ====================================================================

    private TcpClient client;
    private NetworkStream stream;

    private TelnetState state;
    private byte currentOption;

    private List<byte> sbBuffer;

    private bool[] localOptions;      // 로컬 옵션 상태
    private bool[] remoteOptions;     // 원격 옵션 상태
    private bool[] localSupported;    // 로컬 지원 옵션
    private bool[] remoteSupported;   // 원격 지원 옵션

    // ====================================================================
    // Public Properties
    // ====================================================================

    /// <summary>Remote host</summary>
    public string Host { get; private set; }

    /// <summary>Remote port</summary>
    public int Port { get; private set; }

    /// <summary>Is connected to server</summary>
    public bool IsConnected { get; private set; }

    // 양방향 모드 플래그
    /// <summary>Local binary mode - we send binary</summary>
    public bool BinaryLocal { get; set; }

    /// <summary>Remote binary mode - they send binary</summary>
    public bool BinaryRemote { get; set; }

    /// <summary>Local echo - we echo</summary>
    public bool EchoLocal { get; set; }

    /// <summary>Remote echo - they echo</summary>
    public bool EchoRemote { get; set; }

    /// <summary>Local SGA - we suppress GA</summary>
    public bool SgaLocal { get; set; }

    /// <summary>Remote SGA - they suppress GA</summary>
    public bool SgaRemote { get; set; }

    // 모드 상태
    /// <summary>Line mode vs character mode</summary>
    public bool IsLineMode { get; set; }

    /// <summary>Linemode option active</summary>
    public bool LinemodeActive { get; set; }

    /// <summary>Linemode local editing enabled</summary>
    public bool LinemodeEdit { get; set; }

    // 터미널 정보
    /// <summary>Terminal types (for cycling)</summary>
    public List<string> TerminalTypes { get; set; }

    /// <summary>Current terminal type index</summary>
    public int TerminalTypeIndex { get; set; }

    /// <summary>Terminal width in columns</summary>
    public int TerminalWidth { get; set; }

    /// <summary>Terminal height in rows</summary>
    public int TerminalHeight { get; set; }

    /// <summary>Terminal speed (e.g., "38400,38400")</summary>
    public string TerminalSpeed { get; set; }

    // 통계
    /// <summary>Total bytes sent to server</summary>
    public ulong BytesSent { get; private set; }

    /// <summary>Total bytes received from server</summary>
    public ulong BytesReceived { get; private set; }

    /// <summary>Connection start time (UTC)</summary>
    public DateTime ConnectionStartTime { get; private set; }

    /// <summary>Get connection duration</summary>
    public TimeSpan ConnectionDuration
    {
        get
        {
            if (ConnectionStartTime == DateTime.MinValue)
            {
                return TimeSpan.Zero;
            }
            return DateTime.UtcNow - ConnectionStartTime;
        }
    }

    // ====================================================================
    // Constructor
    // ====================================================================

    /// <summary>
    /// Initialize telnet connection
    /// </summary>
    public TelnetConnection()
    {
        client = null;
        stream = null;
        state = TelnetState.Data;
        currentOption = 0;

        sbBuffer = new List<byte>();

        localOptions = new bool[256];
        remoteOptions = new bool[256];
        localSupported = new bool[256];
        remoteSupported = new bool[256];

        IsConnected = false;

        // Initialize modes
        BinaryLocal = false;
        BinaryRemote = false;
        EchoLocal = false;
        EchoRemote = false;
        SgaLocal = false;
        SgaRemote = false;

        IsLineMode = true;  // Default to line mode
        LinemodeActive = false;
        LinemodeEdit = false;

        // Initialize terminal info
        TerminalTypes = new List<string> { "XTERM", "VT100", "ANSI" };
        TerminalTypeIndex = 0;
        TerminalWidth = 80;
        TerminalHeight = 24;
        TerminalSpeed = "38400,38400";

        // Initialize statistics
        BytesSent = 0;
        BytesReceived = 0;
        ConnectionStartTime = DateTime.MinValue;

        InitializeSupportedOptions();
    }

    // ====================================================================
    // Initialization
    // ====================================================================

    /// <summary>
    /// Initialize supported options
    /// </summary>
    private void InitializeSupportedOptions()
    {
        // Local options we support (we can send)
        localSupported[TelnetProtocol.TELOPT_BINARY] = true;
        localSupported[TelnetProtocol.TELOPT_SGA] = true;
        localSupported[TelnetProtocol.TELOPT_TTYPE] = true;
        localSupported[TelnetProtocol.TELOPT_NAWS] = true;
        localSupported[TelnetProtocol.TELOPT_TSPEED] = true;
        localSupported[TelnetProtocol.TELOPT_ENVIRON] = true;
        localSupported[TelnetProtocol.TELOPT_LINEMODE] = true;

        // Remote options we support (they can send)
        remoteSupported[TelnetProtocol.TELOPT_BINARY] = true;
        remoteSupported[TelnetProtocol.TELOPT_SGA] = true;
        remoteSupported[TelnetProtocol.TELOPT_ECHO] = true;
    }

    // ====================================================================
    // Connection Management
    // ====================================================================

    /// <summary>
    /// Connect to telnet server
    /// </summary>
    public void Connect(string host, int port)
    {
        if (IsConnected)
        {
            throw new InvalidOperationException("Already connected");
        }

        Host = host;
        Port = port;

        try
        {
            client = new TcpClient();
            client.Connect(host, port);
            stream = client.GetStream();
            IsConnected = true;

            // Set connection start time for statistics
            ConnectionStartTime = DateTime.UtcNow;

            System.Console.Error.WriteLine($"Connected to {host}:{port}");

            // Send initial option negotiations
            SendInitialNegotiations();
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to connect to {host}:{port}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Disconnect from telnet server
    /// </summary>
    public void Disconnect()
    {
        if (!IsConnected)
        {
            return;
        }

        if (stream != null)
        {
            stream.Close();
            stream = null;
        }

        if (client != null)
        {
            client.Close();
            client = null;
        }

        IsConnected = false;
        state = TelnetState.Data;

        // Disconnected
    }

    /// <summary>
    /// Send initial option negotiations
    /// </summary>
    private void SendInitialNegotiations()
    {
        // Offer BINARY
        SendNegotiate(TelnetProtocol.WILL, TelnetProtocol.TELOPT_BINARY);

        // Offer SGA
        SendNegotiate(TelnetProtocol.WILL, TelnetProtocol.TELOPT_SGA);
        SendNegotiate(TelnetProtocol.DO, TelnetProtocol.TELOPT_SGA);

        // Request ECHO
        Console.Error.WriteLine("[DEBUG] Sending: DO ECHO");
        SendNegotiate(TelnetProtocol.DO, TelnetProtocol.TELOPT_ECHO);

        // Offer TERMINAL-TYPE
        SendNegotiate(TelnetProtocol.WILL, TelnetProtocol.TELOPT_TTYPE);

        // Offer NAWS
        SendNegotiate(TelnetProtocol.WILL, TelnetProtocol.TELOPT_NAWS);

        // Offer TSPEED
        SendNegotiate(TelnetProtocol.WILL, TelnetProtocol.TELOPT_TSPEED);

        // Offer ENVIRON
        SendNegotiate(TelnetProtocol.WILL, TelnetProtocol.TELOPT_ENVIRON);

        // Offer LINEMODE
        SendNegotiate(TelnetProtocol.WILL, TelnetProtocol.TELOPT_LINEMODE);
    }

    // ====================================================================
    // Send/Receive
    // ====================================================================

    /// <summary>
    /// Send data to telnet server
    /// </summary>
    public int Send(byte[] data)
    {
        if (!IsConnected || stream == null)
        {
            throw new InvalidOperationException("Not connected");
        }

        stream.Write(data, 0, data.Length);

        // Update statistics
        BytesSent += (ulong)data.Length;

        return data.Length;
    }

    /// <summary>
    /// Receive data from telnet server
    /// </summary>
    public int Receive(byte[] buffer)
    {
        if (!IsConnected || stream == null)
        {
            throw new InvalidOperationException("Not connected");
        }

        if (!stream.DataAvailable)
        {
            return 0;
        }

        int bytesRead = stream.Read(buffer, 0, buffer.Length);

        // Update statistics
        if (bytesRead > 0)
        {
            BytesReceived += (ulong)bytesRead;
        }

        return bytesRead;
    }

    // ====================================================================
    // Protocol Methods
    // ====================================================================

    /// <summary>
    /// Send IAC command (2 bytes: IAC + command)
    /// </summary>
    public void SendCommand(byte command)
    {
        byte[] data = new byte[] { TelnetProtocol.IAC, command };
        Send(data);
        // [DEBUG] Sent: IAC command
    }

    /// <summary>
    /// Send IAC negotiate command (3 bytes: IAC + cmd + option)
    /// </summary>
    public void SendNegotiate(byte cmd, byte option)
    {
        byte[] data = new byte[] { TelnetProtocol.IAC, cmd, option };
        Send(data);
        // [DEBUG] Sent: IAC negotiate
    }

    /// <summary>
    /// Handle option negotiation (RFC 855 compliant with loop prevention)
    /// </summary>
    private void HandleNegotiate(byte cmd, byte option)
    {
        // [DEBUG] Received: IAC negotiation

        switch (cmd)
        {
            case TelnetProtocol.WILL:
                // Server will use option - only respond if state changes (RFC 855)
                if (option == TelnetProtocol.TELOPT_BINARY ||
                    option == TelnetProtocol.TELOPT_SGA ||
                    option == TelnetProtocol.TELOPT_ECHO)
                {
                    if (!remoteOptions[option])  // State change check - loop prevention
                    {
                        remoteOptions[option] = true;
                        SendNegotiate(TelnetProtocol.DO, option);

                        if (option == TelnetProtocol.TELOPT_BINARY)
                        {
                            BinaryRemote = true;
                            // [INFO] Remote BINARY mode enabled
                        }
                        else if (option == TelnetProtocol.TELOPT_SGA)
                        {
                            SgaRemote = true;
                            // [INFO] Remote SGA enabled
                        }
                        else if (option == TelnetProtocol.TELOPT_ECHO)
                        {
                            EchoRemote = true;
                            Console.Error.WriteLine("[INFO] Remote ECHO enabled (server will echo)");
                        }
                    }
                }
                else
                {
                    // Reject unsupported options - send DONT (RFC 855)
                    // [DEBUG] Rejecting unsupported option
                    SendNegotiate(TelnetProtocol.DONT, option);
                    // Note: remoteOptions[option] remains false (not supported)
                }
                UpdateMode();
                break;

            case TelnetProtocol.WONT:
                // Server won't use option - only respond if state changes
                if (remoteOptions[option])
                {
                    remoteOptions[option] = false;
                    SendNegotiate(TelnetProtocol.DONT, option);

                    if (option == TelnetProtocol.TELOPT_BINARY)
                    {
                        BinaryRemote = false;
                        Console.Error.WriteLine("[WARNING] Server rejected BINARY mode - multibyte characters (UTF-8, EUC-KR) may be corrupted!");
                    }
                    else if (option == TelnetProtocol.TELOPT_SGA)
                    {
                        SgaRemote = false;
                    }
                    else if (option == TelnetProtocol.TELOPT_ECHO)
                    {
                        EchoRemote = false;
                        Console.Error.WriteLine("[INFO] Remote ECHO disabled (client will echo locally)");
                    }
                    else if (option == TelnetProtocol.TELOPT_LINEMODE)
                    {
                        LinemodeActive = false;
                    }
                }
                UpdateMode();
                break;

            case TelnetProtocol.DO:
                // Server wants us to use option - only respond if state changes
                if (option == TelnetProtocol.TELOPT_BINARY ||
                    option == TelnetProtocol.TELOPT_SGA ||
                    option == TelnetProtocol.TELOPT_TTYPE ||
                    option == TelnetProtocol.TELOPT_NAWS ||
                    option == TelnetProtocol.TELOPT_TSPEED ||
                    option == TelnetProtocol.TELOPT_ENVIRON ||
                    option == TelnetProtocol.TELOPT_LINEMODE)
                {
                    if (!localOptions[option])  // State change check - loop prevention
                    {
                        localOptions[option] = true;
                        SendNegotiate(TelnetProtocol.WILL, option);

                        if (option == TelnetProtocol.TELOPT_BINARY)
                        {
                            BinaryLocal = true;
                            // [INFO] Local BINARY mode enabled
                        }
                        else if (option == TelnetProtocol.TELOPT_SGA)
                        {
                            SgaLocal = true;
                            // [INFO] Local SGA enabled
                        }
                        else if (option == TelnetProtocol.TELOPT_TTYPE)
                        {
                            // [INFO] TERMINAL-TYPE negotiation accepted
                            // Server will send SB TTYPE SEND to request type
                        }
                        else if (option == TelnetProtocol.TELOPT_NAWS)
                        {
                            // [INFO] NAWS negotiation accepted
                            // Send initial window size
                            SendNAWS(TerminalWidth, TerminalHeight);
                        }
                        else if (option == TelnetProtocol.TELOPT_TSPEED)
                        {
                            // [INFO] TSPEED negotiation accepted
                            // Server will send SB TSPEED SEND to request speed
                        }
                        else if (option == TelnetProtocol.TELOPT_ENVIRON)
                        {
                            // [INFO] ENVIRON negotiation accepted
                            // Server will send SB ENVIRON SEND to request variables
                        }
                        else if (option == TelnetProtocol.TELOPT_LINEMODE)
                        {
                            LinemodeActive = true;
                            // [INFO] LINEMODE negotiation accepted
                            // Server may send MODE subnegotiation
                        }
                    }
                }
                else
                {
                    // Reject unsupported options - send WONT (RFC 855)
                    // [DEBUG] Rejecting unsupported option
                    SendNegotiate(TelnetProtocol.WONT, option);
                    // Note: localOptions[option] remains false (not supported)
                }
                UpdateMode();
                break;

            case TelnetProtocol.DONT:
                // Server doesn't want us to use option - only respond if state changes
                if (localOptions[option])
                {
                    localOptions[option] = false;
                    SendNegotiate(TelnetProtocol.WONT, option);

                    if (option == TelnetProtocol.TELOPT_BINARY)
                    {
                        BinaryLocal = false;
                        Console.Error.WriteLine("[WARNING] Server rejected local BINARY mode - multibyte characters may be corrupted on send!");
                    }
                    else if (option == TelnetProtocol.TELOPT_SGA)
                    {
                        SgaLocal = false;
                    }
                    else if (option == TelnetProtocol.TELOPT_LINEMODE)
                    {
                        LinemodeActive = false;
                    }
                }
                UpdateMode();
                break;

            default:
                // [WARNING] Unknown negotiation command
                break;
        }
    }

    /// <summary>
    /// Update line mode vs character mode based on current options
    /// </summary>
    private void UpdateMode()
    {
        bool oldLineMode = IsLineMode;

        // Update deprecated combined flags for compatibility
        bool binaryMode = BinaryLocal || BinaryRemote;
        bool sgaMode = SgaLocal || SgaRemote;
        bool echoMode = EchoRemote;

        // Character mode: Server echoes (WILL ECHO) and SGA enabled
        // Line mode: Client echoes (WONT ECHO) or no echo negotiation
        // LINEMODE overrides ECHO/SGA detection if active
        if (LinemodeActive)
        {
            IsLineMode = LinemodeEdit;  // LINEMODE MODE controls
        }
        else if (EchoRemote && SgaRemote)
        {
            // Character mode - server handles echo
            IsLineMode = false;
            // [INFO] Telnet mode: CHARACTER MODE
        }
        else
        {
            // Line mode - client handles echo
            IsLineMode = true;
            // [INFO] Telnet mode: LINE MODE
        }
    }

    /// <summary>
    /// Send NAWS (Negotiate About Window Size) subnegotiation (RFC 1073)
    /// Format: IAC SB NAWS WIDTH[1] WIDTH[0] HEIGHT[1] HEIGHT[0] IAC SE
    /// </summary>
    private void SendNAWS(int width, int height)
    {
        if (width < 0 || height < 0 || width > 65535 || height > 65535)
        {
            // [ERROR] Invalid window size
            return;
        }

        List<byte> data = new List<byte>();

        // Build subnegotiation data: NAWS WIDTH[1] WIDTH[0] HEIGHT[1] HEIGHT[0]
        data.Add(TelnetProtocol.TELOPT_NAWS);
        data.Add((byte)((width >> 8) & 0xFF));   // Width high byte
        data.Add((byte)(width & 0xFF));           // Width low byte
        data.Add((byte)((height >> 8) & 0xFF));  // Height high byte
        data.Add((byte)(height & 0xFF));          // Height low byte

        // [INFO] Sending NAWS

        SendSubnegotiation(data.ToArray());
    }

    /// <summary>
    /// Update terminal window size and send NAWS if negotiated
    /// Returns true if size changed
    /// </summary>
    public bool UpdateWindowSize(int newWidth, int newHeight)
    {
        // Check if size actually changed
        if (newWidth != TerminalWidth || newHeight != TerminalHeight)
        {
            // [INFO] Window size changed

            // Update stored size
            TerminalWidth = newWidth;
            TerminalHeight = newHeight;

            // Send NAWS if negotiated and connected
            if (localOptions[TelnetProtocol.TELOPT_NAWS] && IsConnected)
            {
                SendNAWS(newWidth, newHeight);
            }

            return true;
        }

        return false;
    }

    /// <summary>
    /// Print connection statistics
    /// Based on original C implementation (otelnet.c:1379-1390)
    /// </summary>
    public void PrintStatistics()
    {
        // Use explicit \n because terminal might not be fully restored yet
        Console.Write("\n");
        Console.Write("=== Connection Statistics ===\n");
        Console.Write($"Bytes sent:     {BytesSent}\n");
        Console.Write($"Bytes received: {BytesReceived}\n");

        if (ConnectionStartTime != DateTime.MinValue)
        {
            TimeSpan duration = ConnectionDuration;
            Console.Write($"Duration:       {(int)duration.TotalSeconds} seconds\n");
        }

        Console.Write("============================\n");
        Console.Out.Flush();
    }

    /// <summary>
    /// Send subnegotiation (helper function)
    /// Format: IAC SB <data...> IAC SE
    /// </summary>
    private void SendSubnegotiation(byte[] data)
    {
        if (data == null || data.Length == 0)
        {
            return;
        }

        List<byte> output = new List<byte>();

        // Build: IAC SB <data...> IAC SE
        output.Add(TelnetProtocol.IAC);
        output.Add(TelnetProtocol.SB);

        // Escape IAC in subnegotiation data (RFC 854)
        foreach (byte b in data)
        {
            if (b == TelnetProtocol.IAC)
            {
                output.Add(TelnetProtocol.IAC);
                output.Add(TelnetProtocol.IAC);
            }
            else
            {
                output.Add(b);
            }
        }

        output.Add(TelnetProtocol.IAC);
        output.Add(TelnetProtocol.SE);

        // [DEBUG] Sending subnegotiation

        Send(output.ToArray());
    }

    /// <summary>
    /// Handle subnegotiation (RFC 1091, 1079, 1572, 1184)
    /// Processes TTYPE, TSPEED, ENVIRON, and LINEMODE subnegotiations
    /// </summary>
    private void HandleSubnegotiation()
    {
        if (sbBuffer.Count < 1)
        {
            return;
        }

        byte option = sbBuffer[0];
        // [DEBUG] Received subnegotiation

        switch (option)
        {
            case TelnetProtocol.TELOPT_TTYPE:
                // TERMINAL-TYPE subnegotiation (RFC 1091) with multi-type support
                if (sbBuffer.Count >= 2 && sbBuffer[1] == TelnetProtocol.TTYPE_SEND)
                {
                    // Server requests terminal type - cycle through supported types
                    string currentType = TerminalTypes[TerminalTypeIndex % TerminalTypes.Count];

                    // Prepare response: TELOPT_TTYPE + IS + terminal_type
                    List<byte> response = new List<byte>();
                    response.Add(TelnetProtocol.TELOPT_TTYPE);
                    response.Add(TelnetProtocol.TTYPE_IS);
                    response.AddRange(Encoding.ASCII.GetBytes(currentType));

                    // [INFO] Sending TERMINAL-TYPE
                    SendSubnegotiation(response.ToArray());

                    // Advance to next type for next request
                    TerminalTypeIndex++;

                    // RFC 1091: After cycling through all types, repeat the cycle
                    // This allows the server to detect when we've looped
                }
                break;

            case TelnetProtocol.TELOPT_TSPEED:
                // TERMINAL-SPEED subnegotiation (RFC 1079)
                if (sbBuffer.Count >= 2 && sbBuffer[1] == TelnetProtocol.TTYPE_SEND)  // SEND = 1
                {
                    // Server requests terminal speed - send IS response
                    List<byte> response = new List<byte>();
                    response.Add(TelnetProtocol.TELOPT_TSPEED);
                    response.Add(TelnetProtocol.TTYPE_IS);  // IS = 0
                    response.AddRange(Encoding.ASCII.GetBytes(TerminalSpeed));

                    // [INFO] Sending TSPEED
                    SendSubnegotiation(response.ToArray());
                }
                break;

            case TelnetProtocol.TELOPT_ENVIRON:
                // ENVIRON subnegotiation (RFC 1572)
                if (sbBuffer.Count >= 2 && sbBuffer[1] == TelnetProtocol.ENV_SEND)
                {
                    // Server requests environment variables - send IS response
                    List<byte> response = new List<byte>();
                    response.Add(TelnetProtocol.TELOPT_ENVIRON);
                    response.Add(TelnetProtocol.ENV_IS);

                    // Send USER variable if available
                    string user = Environment.GetEnvironmentVariable("USER");
                    if (!string.IsNullOrEmpty(user) && user.Length < 64)
                    {
                        response.Add(TelnetProtocol.ENV_VAR);
                        response.AddRange(Encoding.ASCII.GetBytes("USER"));
                        response.Add(TelnetProtocol.ENV_VALUE);
                        response.AddRange(Encoding.ASCII.GetBytes(user));
                        // [DEBUG] Sending ENVIRON: USER
                    }

                    // Send DISPLAY variable if available (for X11)
                    string display = Environment.GetEnvironmentVariable("DISPLAY");
                    if (!string.IsNullOrEmpty(display) && display.Length < 64)
                    {
                        response.Add(TelnetProtocol.ENV_VAR);
                        response.AddRange(Encoding.ASCII.GetBytes("DISPLAY"));
                        response.Add(TelnetProtocol.ENV_VALUE);
                        response.AddRange(Encoding.ASCII.GetBytes(display));
                        // [DEBUG] Sending ENVIRON: DISPLAY
                    }

                    if (response.Count > 2)  // If we added any variables
                    {
                        // [INFO] Sending ENVIRON
                        SendSubnegotiation(response.ToArray());
                    }
                    else
                    {
                        // [INFO] No environment variables
                    }
                }
                break;

            case TelnetProtocol.TELOPT_LINEMODE:
                // LINEMODE subnegotiation (RFC 1184)
                if (sbBuffer.Count >= 2 && sbBuffer[1] == TelnetProtocol.LM_MODE)
                {
                    // MODE subnegotiation
                    if (sbBuffer.Count >= 3)
                    {
                        byte mode = sbBuffer[2];
                        bool oldEdit = LinemodeEdit;

                        LinemodeEdit = (mode & TelnetProtocol.MODE_EDIT) != 0;

                        // [INFO] LINEMODE MODE

                        // Send ACK if MODE_ACK bit is set (RFC 1184 mode synchronization)
                        if ((mode & TelnetProtocol.MODE_ACK) != 0)
                        {
                            byte[] response = new byte[3];
                            response[0] = TelnetProtocol.TELOPT_LINEMODE;
                            response[1] = TelnetProtocol.LM_MODE;
                            response[2] = mode;  // Echo back the same mode

                            // [DEBUG] Sending LINEMODE MODE ACK
                            SendSubnegotiation(response);
                        }

                        // Update mode if edit flag changed
                        if (oldEdit != LinemodeEdit)
                        {
                            UpdateMode();
                        }
                    }
                }
                else if (sbBuffer.Count >= 2 && sbBuffer[1] == TelnetProtocol.LM_FORWARDMASK)
                {
                    // FORWARDMASK - acknowledge but don't implement for now
                    // [DEBUG] Received LINEMODE FORWARDMASK
                }
                else if (sbBuffer.Count >= 2 && sbBuffer[1] == TelnetProtocol.LM_SLC)
                {
                    // SLC (Set Local Characters) - acknowledge but don't implement for now
                    // [DEBUG] Received LINEMODE SLC
                }
                break;

            default:
                // [DEBUG] Unhandled subnegotiation
                break;
        }
    }

    /// <summary>
    /// Process input data from telnet server (RFC 854 state machine)
    /// Removes IAC sequences and returns clean data
    /// </summary>
    public byte[] ProcessInput(byte[] input)
    {
        if (input == null || input.Length == 0)
        {
            return new byte[0];
        }

        List<byte> output = new List<byte>();

        for (int i = 0; i < input.Length; i++)
        {
            byte c = input[i];

            switch (state)
            {
                case TelnetState.Data:
                    if (c == TelnetProtocol.IAC)
                    {
                        state = TelnetState.IAC;
                    }
                    else if (c == (byte)'\r' && !BinaryRemote)
                    {
                        // CR in non-binary mode - need to check next byte (RFC 854)
                        state = TelnetState.SeenCR;
                    }
                    else
                    {
                        // Regular data
                        output.Add(c);
                    }
                    break;

                case TelnetState.IAC:
                    if (c == TelnetProtocol.IAC)
                    {
                        // Escaped IAC - output single IAC
                        output.Add(TelnetProtocol.IAC);
                        state = TelnetState.Data;
                    }
                    else if (c == TelnetProtocol.WILL)
                    {
                        state = TelnetState.Will;
                    }
                    else if (c == TelnetProtocol.WONT)
                    {
                        state = TelnetState.Wont;
                    }
                    else if (c == TelnetProtocol.DO)
                    {
                        state = TelnetState.Do;
                    }
                    else if (c == TelnetProtocol.DONT)
                    {
                        state = TelnetState.Dont;
                    }
                    else if (c == TelnetProtocol.SB)
                    {
                        state = TelnetState.SB;
                        sbBuffer.Clear();
                    }
                    else if (c == TelnetProtocol.GA)
                    {
                        // Go Ahead - silently ignore (RFC 858)
                        // [DEBUG] Received IAC GA
                        state = TelnetState.Data;
                    }
                    else if (c == TelnetProtocol.NOP)
                    {
                        // No Operation - silently ignore (RFC 854)
                        // [DEBUG] Received IAC NOP
                        state = TelnetState.Data;
                    }
                    else if (c == TelnetProtocol.AYT)
                    {
                        // Are You There - respond with confirmation (RFC 854)
                        // [DEBUG] Received IAC AYT
                        byte[] response = System.Text.Encoding.ASCII.GetBytes("\r\n[Otelnet: Yes, I'm here]\r\n");
                        Send(response);
                        state = TelnetState.Data;
                    }
                    else if (c == TelnetProtocol.IP)
                    {
                        // Interrupt Process - log but don't act (RFC 854)
                        // [INFO] Received IAC IP
                        state = TelnetState.Data;
                    }
                    else if (c == TelnetProtocol.AO)
                    {
                        // Abort Output - log but don't act (RFC 854)
                        // [INFO] Received IAC AO
                        state = TelnetState.Data;
                    }
                    else if (c == TelnetProtocol.BREAK)
                    {
                        // Break - log but don't act (RFC 854)
                        // [INFO] Received IAC BREAK
                        state = TelnetState.Data;
                    }
                    else if (c == TelnetProtocol.EL)
                    {
                        // Erase Line - log but don't act (RFC 854)
                        // [DEBUG] Received IAC EL
                        state = TelnetState.Data;
                    }
                    else if (c == TelnetProtocol.EC)
                    {
                        // Erase Character - log but don't act (RFC 854)
                        // [DEBUG] Received IAC EC
                        state = TelnetState.Data;
                    }
                    else if (c == TelnetProtocol.DM)
                    {
                        // Data Mark - marks end of urgent data (RFC 854)
                        // [DEBUG] Received IAC DM
                        state = TelnetState.Data;
                    }
                    else if (c == TelnetProtocol.EOR)
                    {
                        // End of Record - log but don't act (RFC 885)
                        // [DEBUG] Received IAC EOR
                        state = TelnetState.Data;
                    }
                    else
                    {
                        // Unknown IAC command - log and ignore
                        // [WARNING] Received unknown IAC command
                        state = TelnetState.Data;
                    }
                    break;

                case TelnetState.Will:
                    currentOption = c;
                    HandleNegotiate(TelnetProtocol.WILL, c);
                    state = TelnetState.Data;
                    break;

                case TelnetState.Wont:
                    currentOption = c;
                    HandleNegotiate(TelnetProtocol.WONT, c);
                    state = TelnetState.Data;
                    break;

                case TelnetState.Do:
                    currentOption = c;
                    HandleNegotiate(TelnetProtocol.DO, c);
                    state = TelnetState.Data;
                    break;

                case TelnetState.Dont:
                    currentOption = c;
                    HandleNegotiate(TelnetProtocol.DONT, c);
                    state = TelnetState.Data;
                    break;

                case TelnetState.SB:
                    if (c == TelnetProtocol.IAC)
                    {
                        state = TelnetState.SBIAC;
                    }
                    else
                    {
                        // Accumulate subnegotiation data
                        sbBuffer.Add(c);
                    }
                    break;

                case TelnetState.SBIAC:
                    if (c == TelnetProtocol.SE)
                    {
                        // End of subnegotiation
                        HandleSubnegotiation();
                        sbBuffer.Clear();
                        state = TelnetState.Data;
                    }
                    else if (c == TelnetProtocol.IAC)
                    {
                        // Escaped IAC in subnegotiation
                        sbBuffer.Add(TelnetProtocol.IAC);
                        state = TelnetState.SB;
                    }
                    else
                    {
                        // Invalid sequence - return to SB state
                        sbBuffer.Add(c);
                        state = TelnetState.SB;
                    }
                    break;

                case TelnetState.SeenCR:
                    // RFC 854: CR must be followed by NUL or LF in non-binary mode
                    // CR NUL means carriage return only
                    // CR LF means newline
                    // CR <other> is illegal, treat as CR followed by the character
                    if (c == 0)
                    {
                        // CR NUL - output just CR
                        output.Add((byte)'\r');
                        // [DEBUG] Received CR NUL
                    }
                    else if (c == (byte)'\n')
                    {
                        // CR LF - output CR LF (newline)
                        output.Add((byte)'\r');
                        output.Add((byte)'\n');
                        // [DEBUG] Received CR LF
                    }
                    else if (c == TelnetProtocol.IAC)
                    {
                        // CR IAC - output CR and process IAC
                        output.Add((byte)'\r');
                        state = TelnetState.IAC;
                        continue; // Don't reset state at end
                    }
                    else
                    {
                        // CR followed by other character - output CR and process character normally
                        output.Add((byte)'\r');
                        output.Add(c);
                        // [DEBUG] Received CR followed by other
                    }
                    state = TelnetState.Data;
                    break;

                default:
                    // [WARNING] Invalid telnet state
                    state = TelnetState.Data;
                    break;
            }
        }

        // [DEBUG] Telnet processed bytes

        return output.ToArray();
    }

    /// <summary>
    /// Prepare data for sending to telnet server (escape IAC bytes)
    /// RFC 854: IAC (255) must be doubled: 255 -> 255 255
    /// </summary>
    public byte[] PrepareOutput(byte[] input)
    {
        if (input == null || input.Length == 0)
        {
            return new byte[0];
        }

        List<byte> output = new List<byte>();

        foreach (byte c in input)
        {
            if (c == TelnetProtocol.IAC)
            {
                // Escape IAC by doubling it
                output.Add(TelnetProtocol.IAC);
                output.Add(TelnetProtocol.IAC);
            }
            else
            {
                // Regular character
                output.Add(c);
            }
        }

        // [DEBUG] Telnet prepared output (IAC escaped if needed)

        return output.ToArray();
    }

    // ====================================================================
    // IDisposable
    // ====================================================================

    public void Dispose()
    {
        Disconnect();
    }
}
