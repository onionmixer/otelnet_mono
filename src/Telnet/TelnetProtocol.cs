using System;

namespace Otelnet.Telnet;

/// <summary>
/// Telnet protocol constants and definitions (RFC 854)
/// </summary>
public static class TelnetProtocol
{
    // ====================================================================
    // IAC Commands (RFC 854)
    // ====================================================================

    /// <summary>Interpret As Command - 모든 명령의 시작</summary>
    public const byte IAC = 255;

    /// <summary>Don't use option - 상대에게 옵션 사용 금지 요청</summary>
    public const byte DONT = 254;

    /// <summary>Do use option - 상대에게 옵션 사용 요청</summary>
    public const byte DO = 253;

    /// <summary>Won't use option - 옵션 사용 거부</summary>
    public const byte WONT = 252;

    /// <summary>Will use option - 옵션 사용 의사</summary>
    public const byte WILL = 251;

    /// <summary>Subnegotiation Begin - 서브협상 시작</summary>
    public const byte SB = 250;

    /// <summary>Go Ahead - 입력 가능 신호</summary>
    public const byte GA = 249;

    /// <summary>Erase Line - 라인 지우기</summary>
    public const byte EL = 248;

    /// <summary>Erase Character - 문자 지우기</summary>
    public const byte EC = 247;

    /// <summary>Are You There - 연결 확인</summary>
    public const byte AYT = 246;

    /// <summary>Abort Output - 출력 중단</summary>
    public const byte AO = 245;

    /// <summary>Interrupt Process - 프로세스 중단</summary>
    public const byte IP = 244;

    /// <summary>Break - 브레이크 신호</summary>
    public const byte BREAK = 243;

    /// <summary>Data Mark - 데이터 마크 (Urgent 데이터)</summary>
    public const byte DM = 242;

    /// <summary>No Operation - 무동작</summary>
    public const byte NOP = 241;

    /// <summary>Subnegotiation End - 서브협상 종료</summary>
    public const byte SE = 240;

    /// <summary>End of Record - 레코드 끝 (RFC 885)</summary>
    public const byte EOR = 239;

    // ====================================================================
    // Telnet Options
    // ====================================================================

    /// <summary>Binary Transmission (RFC 856)</summary>
    public const byte TELOPT_BINARY = 0;

    /// <summary>Echo (RFC 857)</summary>
    public const byte TELOPT_ECHO = 1;

    /// <summary>Reconnection</summary>
    public const byte TELOPT_RCP = 2;

    /// <summary>Suppress Go Ahead (RFC 858)</summary>
    public const byte TELOPT_SGA = 3;

    /// <summary>Approx Message Size Negotiation</summary>
    public const byte TELOPT_NAMS = 4;

    /// <summary>Status (RFC 859)</summary>
    public const byte TELOPT_STATUS = 5;

    /// <summary>Timing Mark (RFC 860)</summary>
    public const byte TELOPT_TIMING_MARK = 6;

    /// <summary>Remote Controlled Trans and Echo (RFC 726)</summary>
    public const byte TELOPT_RCTE = 7;

    /// <summary>Output Line Width (RFC 1073)</summary>
    public const byte TELOPT_NAOL = 8;

    /// <summary>Output Page Size (RFC 1073)</summary>
    public const byte TELOPT_NAOP = 9;

    /// <summary>Output Carriage-Return Disposition (RFC 652)</summary>
    public const byte TELOPT_NAOCRD = 10;

    /// <summary>Output Horizontal Tab Stops (RFC 653)</summary>
    public const byte TELOPT_NAOHTS = 11;

    /// <summary>Output Horizontal Tab Disposition (RFC 654)</summary>
    public const byte TELOPT_NAOHTD = 12;

    /// <summary>Output Formfeed Disposition (RFC 655)</summary>
    public const byte TELOPT_NAOFFD = 13;

    /// <summary>Output Vertical Tabstops (RFC 656)</summary>
    public const byte TELOPT_NAOVTS = 14;

    /// <summary>Output Vertical Tab Disposition (RFC 657)</summary>
    public const byte TELOPT_NAOVTD = 15;

    /// <summary>Output Linefeed Disposition (RFC 658)</summary>
    public const byte TELOPT_NAOLFD = 16;

    /// <summary>Extended ASCII (RFC 698)</summary>
    public const byte TELOPT_XASCII = 17;

    /// <summary>Logout (RFC 727)</summary>
    public const byte TELOPT_LOGOUT = 18;

    /// <summary>Byte Macro (RFC 735)</summary>
    public const byte TELOPT_BM = 19;

    /// <summary>Data Entry Terminal (RFC 1043, RFC 732)</summary>
    public const byte TELOPT_DET = 20;

    /// <summary>SUPDUP (RFC 736, RFC 734)</summary>
    public const byte TELOPT_SUPDUP = 21;

    /// <summary>SUPDUP Output (RFC 749)</summary>
    public const byte TELOPT_SUPDUPOUTPUT = 22;

    /// <summary>Send Location (RFC 779)</summary>
    public const byte TELOPT_SNDLOC = 23;

    /// <summary>Terminal Type (RFC 1091)</summary>
    public const byte TELOPT_TTYPE = 24;

    /// <summary>End of Record (RFC 885)</summary>
    public const byte TELOPT_EOR = 25;

    /// <summary>TACACS User Identification (RFC 927)</summary>
    public const byte TELOPT_TUID = 26;

    /// <summary>Output Marking (RFC 933)</summary>
    public const byte TELOPT_OUTMRK = 27;

    /// <summary>Terminal Location Number (RFC 946)</summary>
    public const byte TELOPT_TTYLOC = 28;

    /// <summary>Telnet 3270 Regime (RFC 1041)</summary>
    public const byte TELOPT_3270REGIME = 29;

    /// <summary>X.3 PAD (RFC 1053)</summary>
    public const byte TELOPT_X3PAD = 30;

    /// <summary>Negotiate About Window Size (RFC 1073)</summary>
    public const byte TELOPT_NAWS = 31;

    /// <summary>Terminal Speed (RFC 1079)</summary>
    public const byte TELOPT_TSPEED = 32;

    /// <summary>Remote Flow Control (RFC 1372)</summary>
    public const byte TELOPT_LFLOW = 33;

    /// <summary>Linemode (RFC 1184)</summary>
    public const byte TELOPT_LINEMODE = 34;

    /// <summary>X Display Location (RFC 1096)</summary>
    public const byte TELOPT_XDISPLOC = 35;

    /// <summary>Environment Variables (RFC 1572)</summary>
    public const byte TELOPT_ENVIRON = 36;

    /// <summary>Authentication (RFC 2941)</summary>
    public const byte TELOPT_AUTHENTICATION = 37;

    /// <summary>Encryption (RFC 2946)</summary>
    public const byte TELOPT_ENCRYPT = 38;

    /// <summary>New Environment (RFC 1572)</summary>
    public const byte TELOPT_NEW_ENVIRON = 39;

    /// <summary>Extended Options List (RFC 861)</summary>
    public const byte TELOPT_EXOPL = 255;

    // ====================================================================
    // Terminal-Type Subnegotiation (RFC 1091)
    // ====================================================================

    /// <summary>Terminal Type IS - 타입 응답</summary>
    public const byte TTYPE_IS = 0;

    /// <summary>Terminal Type SEND - 타입 요청</summary>
    public const byte TTYPE_SEND = 1;

    // ====================================================================
    // Environment Subnegotiation (RFC 1572)
    // ====================================================================

    /// <summary>Environment IS - 환경변수 응답</summary>
    public const byte ENV_IS = 0;

    /// <summary>Environment SEND - 환경변수 요청</summary>
    public const byte ENV_SEND = 1;

    /// <summary>Environment Variable</summary>
    public const byte ENV_VAR = 0;

    /// <summary>Environment Value</summary>
    public const byte ENV_VALUE = 1;

    /// <summary>Environment Escape</summary>
    public const byte ENV_ESC = 2;

    /// <summary>Environment User Variable</summary>
    public const byte ENV_USERVAR = 3;

    // ====================================================================
    // Linemode Subnegotiation (RFC 1184)
    // ====================================================================

    /// <summary>Linemode MODE</summary>
    public const byte LM_MODE = 1;

    /// <summary>Linemode FORWARDMASK</summary>
    public const byte LM_FORWARDMASK = 2;

    /// <summary>Linemode SLC (Set Local Characters)</summary>
    public const byte LM_SLC = 3;

    // Linemode MODE bits
    /// <summary>Local editing</summary>
    public const byte MODE_EDIT = 0x01;

    /// <summary>Trap signals</summary>
    public const byte MODE_TRAPSIG = 0x02;

    /// <summary>Acknowledge mode change</summary>
    public const byte MODE_ACK = 0x04;

    /// <summary>Soft tab</summary>
    public const byte MODE_SOFT_TAB = 0x08;

    /// <summary>Literal echo</summary>
    public const byte MODE_LIT_ECHO = 0x10;

    // ====================================================================
    // Helper Methods
    // ====================================================================

    /// <summary>
    /// Get command name for debugging
    /// </summary>
    public static string GetCommandName(byte cmd)
    {
        switch (cmd)
        {
            case IAC: return "IAC";
            case DONT: return "DONT";
            case DO: return "DO";
            case WONT: return "WONT";
            case WILL: return "WILL";
            case SB: return "SB";
            case GA: return "GA";
            case EL: return "EL";
            case EC: return "EC";
            case AYT: return "AYT";
            case AO: return "AO";
            case IP: return "IP";
            case BREAK: return "BREAK";
            case DM: return "DM";
            case NOP: return "NOP";
            case SE: return "SE";
            case EOR: return "EOR";
            default: return string.Format("UNKNOWN({0})", cmd);
        }
    }

    /// <summary>
    /// Get option name for debugging
    /// </summary>
    public static string GetOptionName(byte option)
    {
        switch (option)
        {
            case TELOPT_BINARY: return "BINARY";
            case TELOPT_ECHO: return "ECHO";
            case TELOPT_SGA: return "SGA";
            case TELOPT_STATUS: return "STATUS";
            case TELOPT_TIMING_MARK: return "TIMING_MARK";
            case TELOPT_TTYPE: return "TTYPE";
            case TELOPT_NAWS: return "NAWS";
            case TELOPT_TSPEED: return "TSPEED";
            case TELOPT_LFLOW: return "LFLOW";
            case TELOPT_LINEMODE: return "LINEMODE";
            case TELOPT_XDISPLOC: return "XDISPLOC";
            case TELOPT_ENVIRON: return "ENVIRON";
            case TELOPT_NEW_ENVIRON: return "NEW_ENVIRON";
            default: return string.Format("OPTION_{0}", option);
        }
    }
}
