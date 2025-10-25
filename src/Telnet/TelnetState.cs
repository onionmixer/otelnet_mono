using System;

namespace OtelnetMono.Telnet
{
    /// <summary>
    /// Telnet protocol state machine states
    /// </summary>
    public enum TelnetState
    {
        /// <summary>
        /// Normal data state - processing regular data bytes
        /// </summary>
        Data,

        /// <summary>
        /// Received IAC (255) - next byte is a command
        /// </summary>
        IAC,

        /// <summary>
        /// Received IAC WILL - next byte is option code
        /// </summary>
        Will,

        /// <summary>
        /// Received IAC WONT - next byte is option code
        /// </summary>
        Wont,

        /// <summary>
        /// Received IAC DO - next byte is option code
        /// </summary>
        Do,

        /// <summary>
        /// Received IAC DONT - next byte is option code
        /// </summary>
        Dont,

        /// <summary>
        /// In subnegotiation (after IAC SB) - collecting subnegotiation data
        /// </summary>
        SB,

        /// <summary>
        /// Received IAC in subnegotiation - could be IAC IAC (escaped) or IAC SE (end)
        /// </summary>
        SBIAC,

        /// <summary>
        /// Received CR in non-binary mode - next byte determines CR handling (RFC 854)
        /// CR NUL = carriage return, CR LF = newline
        /// </summary>
        SeenCR
    }
}
