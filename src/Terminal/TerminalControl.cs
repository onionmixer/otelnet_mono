using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Diagnostics;

namespace Otelnet.Terminal;

/// <summary>
/// Terminal control for raw mode, signal handling, and window size
/// Uses P/Invoke for POSIX terminal operations (.NET 8.0 compatible)
/// </summary>
[SupportedOSPlatform("linux")]
[SupportedOSPlatform("macos")]
public sealed class TerminalControl : IDisposable
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

    // Signal registrations (dispose to unregister)
    private IDisposable? sigintHandler;
    private IDisposable? sigtermHandler;
    private IDisposable? sigwinchHandler;

    // Event for window size changes
    public event EventHandler<WindowSizeEventArgs>? WindowSizeChanged;

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
                    Console.Error.WriteLine("[ERROR] Failed to get terminal attributes");
                    return false;
                }
                termiosSaved = true;
            }

            // Create raw mode settings based on original
            // IMPORTANT: Deep copy to avoid modifying originalTermios.c_cc array
            Termios raw = new Termios(true);
            raw.c_iflag = originalTermios.c_iflag;
            raw.c_oflag = originalTermios.c_oflag;
            raw.c_cflag = originalTermios.c_cflag;
            raw.c_lflag = originalTermios.c_lflag;
            raw.c_line = originalTermios.c_line;
            Array.Copy(originalTermios.c_cc, raw.c_cc, NCCS);

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
                Console.Error.WriteLine("[ERROR] Failed to set terminal attributes");
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
            // [DEBUG] Terminal setup complete (raw mode)
            return true;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[ERROR] Failed to enable raw mode: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Restore terminal to original settings
    /// </summary>
    public void DisableRawMode()
    {
        if (!termiosSaved)
        {
            return;
        }

        try
        {
            // Flush all output before restoring terminal
            Console.Out.Flush();
            Console.Error.Flush();

            // Drain output for all fds
            tcdrain(0);
            tcdrain(1);
            tcdrain(2);

            // Restore original terminal settings for stdin (fd 0)
            // Use TCSADRAIN to wait for output to be transmitted
            tcsetattr(0, TCSADRAIN, ref originalTermios);

            // Also restore for stdout (fd 1) and stderr (fd 2)
            // In case they are separate ttys
            tcsetattr(1, TCSADRAIN, ref originalTermios);
            tcsetattr(2, TCSADRAIN, ref originalTermios);

            // Force flush again after tcsetattr
            Console.Out.Flush();
            Console.Error.Flush();

            // Restore blocking mode
            if (flagsSaved && originalFlags >= 0)
            {
                fcntl(0, F_SETFL, originalFlags & ~O_NONBLOCK);
            }

            isRawMode = false;

            // As a last resort on Linux/Unix/macOS, run 'stty sane' to ensure terminal is restored
            if (IsUnixPlatform())
            {
                try
                {
                    RunSttyCommand("sane");
                }
                catch
                {
                    // Ignore errors - this is best-effort
                }
            }
        }
        catch
        {
            // Ignore errors during cleanup
        }
    }

    /// <summary>
    /// Check if running on Unix/Linux/macOS platform
    /// </summary>
    private static bool IsUnixPlatform()
    {
        return OperatingSystem.IsLinux() || OperatingSystem.IsMacOS() || OperatingSystem.IsFreeBSD();
    }

    /// <summary>
    /// Run stty command with given arguments
    /// Cross-platform safe - uses Process instead of P/Invoke
    /// IMPORTANT: Does NOT redirect stdin/stdout/stderr so stty can access the real terminal
    /// </summary>
    private static void RunSttyCommand(string args)
    {
        try
        {
            // Run stty in background with a delay to ensure it runs AFTER app fully exits
            var psi = new ProcessStartInfo
            {
                FileName = "/bin/sh",
                Arguments = $"-c \"(sleep 0.1; stty {args} < /dev/tty 2>/dev/null) &\"",
                // DO NOT redirect - stty needs access to the actual terminal
                RedirectStandardOutput = false,
                RedirectStandardError = false,
                RedirectStandardInput = false,
                UseShellExecute = false,
                CreateNoWindow = false
            };

            using (var process = Process.Start(psi))
            {
                // Don't wait - let it run in background
                process?.WaitForExit(100); // Just wait for shell to spawn background job
            }
        }
        catch
        {
            // Ignore errors - this is best-effort
        }
    }

    /// <summary>
    /// Install signal handlers for SIGINT, SIGTERM, and SIGWINCH
    /// Uses .NET 8.0 PosixSignalRegistration
    /// </summary>
    public void InstallSignalHandlers()
    {
        try
        {
            // Handle SIGINT (Ctrl+C)
            sigintHandler = PosixSignalRegistration.Create(PosixSignal.SIGINT, context =>
            {
                context.Cancel = true;  // Prevent default termination
                Console.Error.WriteLine($"\r\n[INFO] Received SIGINT, exiting...");
                shouldExit = true;
            });

            // Handle SIGTERM (termination)
            sigtermHandler = PosixSignalRegistration.Create(PosixSignal.SIGTERM, context =>
            {
                context.Cancel = true;  // Prevent default termination
                Console.Error.WriteLine($"\r\n[INFO] Received SIGTERM, exiting...");
                shouldExit = true;
            });

            // Handle SIGWINCH (window size change)
            // Note: SIGWINCH might not be available on all platforms
            try
            {
                sigwinchHandler = PosixSignalRegistration.Create(PosixSignal.SIGWINCH, context =>
                {
                    // [DEBUG] Window size changed (SIGWINCH)
                    windowSizeChanged = true;
                });
            }
            catch (PlatformNotSupportedException)
            {
                // SIGWINCH not supported on this platform
                Console.Error.WriteLine("[WARNING] SIGWINCH not supported on this platform");
            }

            Console.Error.WriteLine("[DEBUG] Signal handlers installed");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[ERROR] Failed to install signal handlers: {ex.Message}");
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
                Console.Error.WriteLine("[WARNING] Failed to get window size");
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[WARNING] Exception getting window size: {ex.Message}");
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

        // Unregister signal handlers
        sigintHandler?.Dispose();
        sigtermHandler?.Dispose();
        sigwinchHandler?.Dispose();

        sigintHandler = null;
        sigtermHandler = null;
        sigwinchHandler = null;
    }

    // ====================================================================
    // P/Invoke - Termios structures and constants
    // ====================================================================

    private const int NCCS = 32;

    // Pack=1 to match C struct layout exactly (no padding)
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct Termios
    {
        public uint c_iflag;    // Input modes (offset 0)
        public uint c_oflag;    // Output modes (offset 4)
        public uint c_cflag;    // Control modes (offset 8)
        public uint c_lflag;    // Local modes (offset 12)
        public byte c_line;     // Line discipline (offset 16)
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = NCCS)]
        public byte[] c_cc;     // Control characters (offset 17)

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
    private const int TCSANOW = 0;      // Change immediately
    private const int TCSADRAIN = 1;    // Change after output is drained
    private const int TCSAFLUSH = 2;    // Change after flushing I/O

    // fcntl commands
    private const int F_GETFL = 3;
    private const int F_SETFL = 4;

    // fcntl flags
    private const int O_NONBLOCK = 0x0800;

    // ioctl requests
    private const uint TIOCGWINSZ = 0x5413;  // Linux value

    // ====================================================================
    // P/Invoke - Using DllImport (LibraryImport doesn't support byte arrays)
    // ====================================================================

    [DllImport("libc", SetLastError = true)]
    private static extern int tcgetattr(int fd, ref Termios termios);

    [DllImport("libc", SetLastError = true)]
    private static extern int tcsetattr(int fd, int optional_actions, ref Termios termios);

    [DllImport("libc", SetLastError = true)]
    private static extern int fcntl(int fd, int cmd, int arg);

    [DllImport("libc", SetLastError = true)]
    private static extern int ioctl(int fd, uint request, ref Winsize ws);

    [DllImport("libc", SetLastError = true)]
    private static extern int tcdrain(int fd);
}

/// <summary>
/// Event args for window size changes
/// </summary>
public sealed record WindowSizeEventArgs(int Width, int Height);
