using System;
using System.Runtime.InteropServices;
using Mono.Unix;
using Mono.Unix.Native;

namespace OtelnetMono.Terminal
{
    /// <summary>
    /// Terminal control for raw mode, signal handling, and window size
    /// Uses P/Invoke for POSIX terminal operations
    /// </summary>
    public class TerminalControl : IDisposable
    {
        // ====================================================================
        // Private Fields
        // ====================================================================

        private Termios originalTermios;
        private bool termiosSaved;
        private bool isRawMode;
        private int originalFlags;
        private bool flagsSaved;

        // Signal flags
        private static volatile bool windowSizeChanged;
        private static volatile bool shouldExit;

        // Event for window size changes
        public event EventHandler<WindowSizeEventArgs> WindowSizeChanged;

        // ====================================================================
        // Properties
        // ====================================================================

        /// <summary>Is terminal in raw mode</summary>
        public bool IsRawMode => isRawMode;

        /// <summary>Should the application exit (SIGINT/SIGTERM received)</summary>
        public static bool ShouldExit => shouldExit;

        // ====================================================================
        // Constructor
        // ====================================================================

        /// <summary>
        /// Initialize terminal control
        /// </summary>
        public TerminalControl()
        {
            termiosSaved = false;
            isRawMode = false;
            flagsSaved = false;
            windowSizeChanged = false;
            shouldExit = false;
        }

        // ====================================================================
        // Public Methods
        // ====================================================================

        /// <summary>
        /// Set terminal to raw mode (character-at-a-time, no echo)
        /// Based on original C implementation
        /// </summary>
        public bool EnableRawMode()
        {
            if (isRawMode)
            {
                return true;  // Already in raw mode
            }

            try
            {
                // Save original terminal settings
                if (!termiosSaved)
                {
                    if (tcgetattr(0, ref originalTermios) != 0)
                    {
                        Console.WriteLine("[ERROR] Failed to get terminal attributes");
                        return false;
                    }
                    termiosSaved = true;
                }

                // Create raw mode settings based on original
                Termios raw = originalTermios;

                // Input modes: no break, no CR to NL, no parity check, no strip char,
                // no start/stop output control
                raw.c_iflag &= ~(BRKINT | ICRNL | INPCK | ISTRIP | IXON);

                // Output modes: disable post processing
                raw.c_oflag &= ~(OPOST);

                // Control modes: set 8 bit chars
                raw.c_cflag |= CS8;

                // Local modes: echoing off, canonical off, no extended functions,
                // no signal chars (^Z,^C)
                raw.c_lflag &= ~(ECHO | ICANON | IEXTEN | ISIG);

                // Control chars: set return condition: min number of bytes and timer
                raw.c_cc[VMIN] = 0;   // Return each byte, or zero for timeout
                raw.c_cc[VTIME] = 1;  // 100ms timeout

                // Apply terminal settings
                if (tcsetattr(0, TCSAFLUSH, ref raw) != 0)
                {
                    Console.WriteLine("[ERROR] Failed to set terminal attributes");
                    return false;
                }

                // Set stdin to non-blocking
                if (!flagsSaved)
                {
                    originalFlags = fcntl(0, F_GETFL, 0);
                    flagsSaved = true;
                }

                if (originalFlags >= 0)
                {
                    fcntl(0, F_SETFL, originalFlags | O_NONBLOCK);
                }

                isRawMode = true;
                Console.WriteLine("[DEBUG] Terminal setup complete (raw mode)");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to enable raw mode: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Restore terminal to original settings
        /// </summary>
        public void DisableRawMode()
        {
            if (!isRawMode || !termiosSaved)
            {
                return;
            }

            try
            {
                // Restore original terminal settings
                tcsetattr(0, TCSAFLUSH, ref originalTermios);

                // Restore blocking mode
                if (flagsSaved && originalFlags >= 0)
                {
                    fcntl(0, F_SETFL, originalFlags & ~O_NONBLOCK);
                }

                isRawMode = false;
                Console.WriteLine("[DEBUG] Terminal restored");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to restore terminal: {ex.Message}");
            }
        }

        /// <summary>
        /// Install signal handlers for SIGINT, SIGTERM, and SIGWINCH
        /// </summary>
        public void InstallSignalHandlers()
        {
            try
            {
                // Handle SIGINT (Ctrl+C)
                UnixSignal sigint = new UnixSignal(Signum.SIGINT);

                // Handle SIGTERM (termination)
                UnixSignal sigterm = new UnixSignal(Signum.SIGTERM);

                // Handle SIGWINCH (window size change)
                UnixSignal sigwinch = new UnixSignal(Signum.SIGWINCH);

                // Start background thread to monitor signals
                System.Threading.Thread signalThread = new System.Threading.Thread(() =>
                {
                    UnixSignal[] signals = new[] { sigint, sigterm, sigwinch };

                    while (!shouldExit)
                    {
                        // Wait for any signal (with timeout)
                        int index = UnixSignal.WaitAny(signals, 1000);

                        if (index >= 0 && index < signals.Length)
                        {
                            if (index == 0 || index == 1)  // SIGINT or SIGTERM
                            {
                                Console.WriteLine($"\n[INFO] Received signal {signals[index].Signum}, exiting...");
                                shouldExit = true;
                            }
                            else if (index == 2)  // SIGWINCH
                            {
                                Console.WriteLine("[DEBUG] Window size changed (SIGWINCH)");
                                windowSizeChanged = true;
                            }
                        }
                    }
                });

                signalThread.IsBackground = true;
                signalThread.Start();

                Console.WriteLine("[DEBUG] Signal handlers installed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to install signal handlers: {ex.Message}");
            }
        }

        /// <summary>
        /// Get current terminal window size
        /// Returns true if size retrieved successfully
        /// </summary>
        public bool GetWindowSize(out int width, out int height)
        {
            width = 80;   // Default
            height = 24;  // Default

            try
            {
                // Get window size using ioctl TIOCGWINSZ
                Winsize ws = new Winsize();

                if (ioctl(0, TIOCGWINSZ, ref ws) == 0)
                {
                    width = ws.ws_col;
                    height = ws.ws_row;
                    return true;
                }
                else
                {
                    Console.WriteLine("[WARNING] Failed to get window size");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WARNING] Exception getting window size: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Check if window size changed and handle it
        /// Returns true if size changed
        /// </summary>
        public bool CheckWindowSizeChanged(out int newWidth, out int newHeight)
        {
            newWidth = 80;
            newHeight = 24;

            if (windowSizeChanged)
            {
                if (GetWindowSize(out newWidth, out newHeight))
                {
                    windowSizeChanged = false;

                    // Fire event
                    WindowSizeChanged?.Invoke(this, new WindowSizeEventArgs(newWidth, newHeight));

                    return true;
                }

                windowSizeChanged = false;
            }

            return false;
        }

        /// <summary>
        /// Reset should exit flag
        /// </summary>
        public static void ResetExitFlag()
        {
            shouldExit = false;
        }

        // ====================================================================
        // IDisposable
        // ====================================================================

        public void Dispose()
        {
            DisableRawMode();
        }

        // ====================================================================
        // P/Invoke - Termios structures and functions
        // ====================================================================

        private const int NCCS = 32;

        [StructLayout(LayoutKind.Sequential)]
        private struct Termios
        {
            public uint c_iflag;    // Input modes
            public uint c_oflag;    // Output modes
            public uint c_cflag;    // Control modes
            public uint c_lflag;    // Local modes
            public byte c_line;     // Line discipline
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = NCCS)]
            public byte[] c_cc;     // Control characters

            public Termios(bool init)
            {
                c_iflag = 0;
                c_oflag = 0;
                c_cflag = 0;
                c_lflag = 0;
                c_line = 0;
                c_cc = new byte[NCCS];
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Winsize
        {
            public ushort ws_row;
            public ushort ws_col;
            public ushort ws_xpixel;
            public ushort ws_ypixel;
        }

        // termios c_iflag bits
        private const uint BRKINT = 0x0002;
        private const uint ICRNL = 0x0100;
        private const uint INPCK = 0x0010;
        private const uint ISTRIP = 0x0020;
        private const uint IXON = 0x0400;

        // termios c_oflag bits
        private const uint OPOST = 0x0001;

        // termios c_cflag bits
        private const uint CS8 = 0x0030;

        // termios c_lflag bits
        private const uint ECHO = 0x0008;
        private const uint ICANON = 0x0002;
        private const uint IEXTEN = 0x8000;
        private const uint ISIG = 0x0001;

        // termios c_cc indices
        private const int VMIN = 6;
        private const int VTIME = 5;

        // tcsetattr actions
        private const int TCSAFLUSH = 2;

        // fcntl commands
        private const int F_GETFL = 3;
        private const int F_SETFL = 4;

        // fcntl flags
        private const int O_NONBLOCK = 0x0800;

        // ioctl requests
        private const uint TIOCGWINSZ = 0x5413;  // Linux value

        [DllImport("libc", SetLastError = true)]
        private static extern int tcgetattr(int fd, ref Termios termios);

        [DllImport("libc", SetLastError = true)]
        private static extern int tcsetattr(int fd, int optional_actions, ref Termios termios);

        [DllImport("libc", SetLastError = true)]
        private static extern int fcntl(int fd, int cmd, int arg);

        [DllImport("libc", SetLastError = true)]
        private static extern int ioctl(int fd, uint request, ref Winsize ws);
    }

    /// <summary>
    /// Event args for window size changes
    /// </summary>
    public class WindowSizeEventArgs : EventArgs
    {
        public int Width { get; private set; }
        public int Height { get; private set; }

        public WindowSizeEventArgs(int width, int height)
        {
            Width = width;
            Height = height;
        }
    }
}
