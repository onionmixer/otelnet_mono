using System;

namespace OtelnetMono.Interactive
{
    /// <summary>
    /// Operation modes for otelnet
    /// </summary>
    public enum OperationMode
    {
        /// <summary>
        /// Client mode - normal telnet operation
        /// </summary>
        Client,

        /// <summary>
        /// Console mode - command processing (triggered by Ctrl+])
        /// </summary>
        Console
    }

    /// <summary>
    /// Manages console mode state and transitions
    /// Based on original otelnet.c console mode implementation
    /// </summary>
    public class ConsoleMode
    {
        /// <summary>
        /// Console trigger key (Ctrl+])
        /// Same as original C implementation: CONSOLE_TRIGGER_KEY = 0x1D
        /// </summary>
        public const byte ConsoleTriggerKey = 0x1D;  // Ctrl+]

        /// <summary>
        /// Maximum length of console command buffer
        /// </summary>
        private const int ConsoleBufferSize = 1024;

        /// <summary>
        /// Current operation mode
        /// </summary>
        public OperationMode CurrentMode { get; private set; }

        /// <summary>
        /// Console command buffer
        /// </summary>
        private char[] consoleBuffer;

        /// <summary>
        /// Current position in console buffer
        /// </summary>
        private int consoleBufferPos;

        /// <summary>
        /// Event raised when mode changes
        /// </summary>
        public event EventHandler<ModeChangedEventArgs> ModeChanged;

        /// <summary>
        /// Constructor
        /// </summary>
        public ConsoleMode()
        {
            CurrentMode = OperationMode.Client;
            consoleBuffer = new char[ConsoleBufferSize];
            consoleBufferPos = 0;
        }

        /// <summary>
        /// Enter console mode
        /// Based on otelnet.c:504-517 (otelnet_enter_console_mode)
        /// </summary>
        public void Enter()
        {
            if (CurrentMode == OperationMode.Console)
            {
                return;  // Already in console mode
            }

            CurrentMode = OperationMode.Console;
            consoleBufferPos = 0;
            Array.Clear(consoleBuffer, 0, consoleBuffer.Length);

            System.Console.WriteLine("\r\n[Console Mode - Enter empty line to return, 'quit' to exit]");
            System.Console.Write("otelnet> ");
            System.Console.Out.Flush();

            // Raise mode changed event
            OnModeChanged(new ModeChangedEventArgs(OperationMode.Console));
        }

        /// <summary>
        /// Exit console mode and return to client mode
        /// Based on otelnet.c:522-533 (otelnet_exit_console_mode)
        /// </summary>
        public void Exit()
        {
            if (CurrentMode == OperationMode.Client)
            {
                return;  // Already in client mode
            }

            CurrentMode = OperationMode.Client;
            consoleBufferPos = 0;

            System.Console.WriteLine("\r\n[Back to client mode]");
            System.Console.Out.Flush();

            // Raise mode changed event
            OnModeChanged(new ModeChangedEventArgs(OperationMode.Client));
        }

        /// <summary>
        /// Add character to console buffer
        /// </summary>
        /// <param name="c">Character to add</param>
        /// <returns>True if character was added, false if buffer is full</returns>
        public bool AddChar(char c)
        {
            if (consoleBufferPos >= consoleBuffer.Length - 1)
            {
                return false;  // Buffer full
            }

            consoleBuffer[consoleBufferPos++] = c;
            return true;
        }

        /// <summary>
        /// Remove last character from console buffer (backspace)
        /// </summary>
        /// <returns>True if character was removed, false if buffer is empty</returns>
        public bool RemoveChar()
        {
            if (consoleBufferPos <= 0)
            {
                return false;  // Buffer empty
            }

            consoleBufferPos--;
            return true;
        }

        /// <summary>
        /// Get current console command and reset buffer
        /// </summary>
        /// <returns>Console command string</returns>
        public string GetCommand()
        {
            string command = new string(consoleBuffer, 0, consoleBufferPos);
            consoleBufferPos = 0;
            Array.Clear(consoleBuffer, 0, consoleBuffer.Length);
            return command;
        }

        /// <summary>
        /// Get current console buffer contents (without resetting)
        /// </summary>
        /// <returns>Current buffer contents</returns>
        public string GetCurrentBuffer()
        {
            return new string(consoleBuffer, 0, consoleBufferPos);
        }

        /// <summary>
        /// Check if a byte is the console trigger key (Ctrl+])
        /// </summary>
        /// <param name="b">Byte to check</param>
        /// <returns>True if byte is Ctrl+]</returns>
        public static bool IsConsoleTrigger(byte b)
        {
            return b == ConsoleTriggerKey;
        }

        /// <summary>
        /// Raise ModeChanged event
        /// </summary>
        /// <param name="e">Event arguments</param>
        protected virtual void OnModeChanged(ModeChangedEventArgs e)
        {
            ModeChanged?.Invoke(this, e);
        }
    }

    /// <summary>
    /// Event arguments for mode change events
    /// </summary>
    public class ModeChangedEventArgs : EventArgs
    {
        /// <summary>
        /// New operation mode
        /// </summary>
        public OperationMode NewMode { get; private set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="newMode">New operation mode</param>
        public ModeChangedEventArgs(OperationMode newMode)
        {
            NewMode = newMode;
        }
    }
}
