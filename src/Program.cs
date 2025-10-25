using System;
using System.Text;
using OtelnetMono.Telnet;
using OtelnetMono.Terminal;
using OtelnetMono.Logging;
using OtelnetMono.Interactive;

namespace OtelnetMono
{
    /// <summary>
    /// Main program entry point
    /// Based on original otelnet.c main application loop
    /// </summary>
    class Program
    {
        private const string VERSION = "1.0.0-mono";

        // Application state
        private static bool running = true;
        private static ConsoleMode consoleMode;
        private static CommandProcessor commandProcessor;
        private static TerminalControl terminal;
        private static TelnetConnection telnet;
        private static SessionLogger logger;
        private static bool echoStateLogged = false;

        static void Main(string[] args)
        {
            // Check for version flag first (before banner)
            if (args.Length > 0 && (args[0] == "--version" || args[0] == "-v"))
            {
                System.Console.WriteLine($"otelnet version {VERSION}");
                return;
            }

            // Print banner
            System.Console.WriteLine($"Otelnet Mono Version {VERSION}");
            System.Console.WriteLine();

            // Check for help flag
            if (args.Length > 0 && (args[0] == "--help" || args[0] == "-h"))
            {
                PrintUsage();
                return;
            }

            // Parse arguments
            if (args.Length < 2)
            {
                PrintUsage();
                return;
            }

            string host = args[0];
            int port;

            if (!int.TryParse(args[1], out port))
            {
                System.Console.WriteLine($"Error: Invalid port number: {args[1]}");
                return;
            }

            if (port < 1 || port > 65535)
            {
                System.Console.WriteLine($"Error: Port must be between 1 and 65535");
                return;
            }

            // Run main application
            RunApplication(host, port);
        }

        /// <summary>
        /// Main application logic
        /// Based on otelnet.c:1293-1410 (otelnet_run + main)
        /// </summary>
        static void RunApplication(string host, int port)
        {
            terminal = new TerminalControl();
            logger = new SessionLogger();
            telnet = new TelnetConnection();
            consoleMode = new ConsoleMode();
            commandProcessor = new CommandProcessor(telnet, consoleMode);

            // Register terminal restoration on process exit
            // This ensures terminal is restored even if Mono tries to override it
            AppDomain.CurrentDomain.ProcessExit += (sender, args) =>
            {
                if (terminal != null)
                {
                    terminal.DisableRawMode();
                }
            };

            try
            {
                // Install signal handlers (SIGINT, SIGTERM, SIGWINCH)
                terminal.InstallSignalHandlers();

                // Get initial window size
                int initialWidth, initialHeight;
                if (terminal.GetWindowSize(out initialWidth, out initialHeight))
                {
                    telnet.TerminalWidth = initialWidth;
                    telnet.TerminalHeight = initialHeight;
                    System.Console.Error.WriteLine($"[INFO] Terminal size: {initialWidth}x{initialHeight}");
                }

                // Enable session logging (optional - for testing)
                // Uncomment to enable logging to file
                // logger.Start("otelnet_session.log");

                // Connect to telnet server
                telnet.Connect(host, port);

                System.Console.WriteLine("Press Ctrl+] for console mode");
                System.Console.WriteLine("Press Ctrl+C to disconnect");
                System.Console.WriteLine();

                // Enable raw mode for character-at-a-time input
                terminal.EnableRawMode();

                // Setup event handlers
                commandProcessor.QuitRequested += OnQuitRequested;

                // Main event loop
                // Based on otelnet.c:1306-1364 (while loop)
                byte[] buffer = new byte[4096];
                StringBuilder consoleLineBuffer = new StringBuilder();

                while (running && !TerminalControl.ShouldExit)
                {
                    // Check for window size changes (SIGWINCH)
                    int newWidth, newHeight;
                    if (terminal.CheckWindowSizeChanged(out newWidth, out newHeight))
                    {
                        telnet.UpdateWindowSize(newWidth, newHeight);
                    }

                    // Process stdin (keyboard input)
                    if (System.Console.KeyAvailable)
                    {
                        ProcessStdin(consoleLineBuffer);
                    }

                    // Process telnet data (network input)
                    if (telnet.IsConnected)
                    {
                        int bytesRead = telnet.Receive(buffer);
                        if (bytesRead > 0)
                        {
                            ProcessTelnetData(buffer, bytesRead);
                        }
                        else if (bytesRead < 0)
                        {
                            // Connection closed or error
                            System.Console.Write("\r\n[Connection closed by remote host]\r\n");
                            break;
                        }
                    }
                    else
                    {
                        // Not connected - exit
                        break;
                    }

                    // Small sleep to prevent CPU spinning
                    System.Threading.Thread.Sleep(10);
                }

                // Show exit message
                if (TerminalControl.ShouldExit)
                {
                    System.Console.Write("\r\n[Exiting due to signal]\r\n");
                }

                // Restore terminal BEFORE printing statistics (otelnet.c:1936-1937)
                terminal.DisableRawMode();

                // Display statistics (after terminal restoration)
                telnet.PrintStatistics();
            }
            catch (Exception ex)
            {
                // Make sure terminal is restored on error first
                terminal.DisableRawMode();
                // Then print error message (after terminal restoration)
                System.Console.WriteLine($"\nError: {ex.Message}");
            }
            finally
            {
                // Ensure terminal is always restored
                if (terminal != null)
                {
                    terminal.DisableRawMode();
                }

                // Cleanup resources
                if (telnet != null)
                {
                    telnet.Dispose();
                }
                if (logger != null)
                {
                    logger.Dispose();
                }
                if (terminal != null)
                {
                    terminal.Dispose();
                }
            }
        }

        /// <summary>
        /// Process stdin input (keyboard)
        /// Based on otelnet.c:1026-1080 (otelnet_process_stdin)
        /// </summary>
        static void ProcessStdin(StringBuilder consoleLineBuffer)
        {
            ConsoleKeyInfo keyInfo = System.Console.ReadKey(true);
            char c = keyInfo.KeyChar;
            byte b = (byte)c;

            if (consoleMode.CurrentMode == OperationMode.Client)
            {
                // Client mode - check for console trigger (Ctrl+])
                // Based on otelnet.c:1053-1060
                if (b == ConsoleMode.ConsoleTriggerKey)
                {
                    // Enter console mode
                    consoleMode.Enter();
                    return;
                }

                // Local echo if server doesn't echo
                // Based on otelnet.c:1474-1517
                bool needLocalEcho = !telnet.EchoRemote;

                // DEBUG: Log echo state on first character
                // if (!echoStateLogged)
                // {
                //     System.Console.Error.WriteLine($"[DEBUG] EchoRemote={telnet.EchoRemote}, needLocalEcho={needLocalEcho}");
                //     echoStateLogged = true;
                // }

                if (needLocalEcho)
                {
                    // Echo input locally - support multibyte characters
                    if (c == '\r')
                    {
                        // CR - echo as CR+LF
                        System.Console.Write("\r\n");
                    }
                    else if (c == '\b' || b == 0x7F)
                    {
                        // Backspace/Delete - echo backspace sequence
                        System.Console.Write("\b \b");
                    }
                    else if (b >= 0x20)
                    {
                        // Printable ASCII character or multibyte sequence byte (0x80-0xFF)
                        System.Console.Write(c);
                    }
                    // Control characters (< 0x20) are not echoed
                }

                // Send to telnet server
                byte[] data = new byte[] { b };

                // Prepare output (IAC escaping)
                byte[] preparedData = telnet.PrepareOutput(data);
                telnet.Send(preparedData);

                // Log sent data
                if (logger.IsEnabled)
                {
                    logger.LogSent(preparedData);
                }
            }
            else
            {
                // Console mode - process console command
                // Based on otelnet.c:1082-1155

                if (c == '\r' || c == '\n')
                {
                    // Process command
                    System.Console.Write("\r\n");  // Echo newline
                    string command = consoleLineBuffer.ToString();
                    consoleLineBuffer.Clear();

                    commandProcessor.ProcessCommand(command);

                    // Show prompt if still in console mode
                    if (consoleMode.CurrentMode == OperationMode.Console)
                    {
                        commandProcessor.ShowPrompt();
                    }
                }
                else if (c == '\b' || c == 0x7F)  // Backspace or DEL
                {
                    if (consoleLineBuffer.Length > 0)
                    {
                        consoleLineBuffer.Length--;
                        // Echo backspace
                        System.Console.Write("\b \b");
                    }
                }
                else if (b == 0x04)  // Ctrl+D - quit (otelnet.c:1564-1567)
                {
                    System.Console.Write("\r\n[Ctrl+D detected, exiting...]\r\n");
                    running = false;
                    return;
                }
                else if (c >= 32 && c < 127)  // Printable character
                {
                    consoleLineBuffer.Append(c);
                    // Echo character
                    System.Console.Write(c);
                }
                // Ignore other control characters
            }
        }

        /// <summary>
        /// Process telnet data (network input)
        /// Based on otelnet.c:999-1024 (otelnet_process_telnet)
        /// </summary>
        static void ProcessTelnetData(byte[] buffer, int bytesRead)
        {
            // Extract received data
            byte[] rawData = new byte[bytesRead];
            Array.Copy(buffer, rawData, bytesRead);

            // Log received data
            if (logger.IsEnabled)
            {
                logger.LogReceived(rawData);
            }

            // Process telnet protocol (IAC commands, option negotiation, etc.)
            byte[] processedData = telnet.ProcessInput(rawData);

            // Display data to console (only in client mode)
            if (processedData.Length > 0 && consoleMode.CurrentMode == OperationMode.Client)
            {
                string text = Encoding.UTF8.GetString(processedData);
                System.Console.Write(text);
            }
        }

        /// <summary>
        /// Handle quit request from command processor
        /// </summary>
        static void OnQuitRequested(object sender, EventArgs e)
        {
            running = false;
        }

        /// <summary>
        /// Print usage information
        /// </summary>
        static void PrintUsage()
        {
            System.Console.WriteLine("Usage: otelnet <host> <port> [options]");
            System.Console.WriteLine();
            System.Console.WriteLine("Arguments:");
            System.Console.WriteLine("  host              Remote host (IP address or hostname)");
            System.Console.WriteLine("  port              Remote port number");
            System.Console.WriteLine();
            System.Console.WriteLine("Options:");
            System.Console.WriteLine("  -c <config>       Configuration file");
            System.Console.WriteLine("  -h, --help        Show this help message");
            System.Console.WriteLine("  -v, --version     Show version information");
            System.Console.WriteLine();
            System.Console.WriteLine("Example:");
            System.Console.WriteLine("  otelnet localhost 23");
            System.Console.WriteLine("  otelnet 192.168.1.100 8881");
        }
    }
}
