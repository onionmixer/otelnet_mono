using System;
using System.Collections.Generic;
using OtelnetMono.Telnet;

namespace OtelnetMono.Interactive
{
    /// <summary>
    /// Command processor for console mode
    /// Based on original otelnet.c console command implementation (lines 719-900)
    /// </summary>
    public class CommandProcessor
    {
        private TelnetConnection telnet;
        private ConsoleMode consoleMode;

        /// <summary>
        /// Event raised when quit command is executed
        /// </summary>
        public event EventHandler QuitRequested;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="telnet">Telnet connection</param>
        /// <param name="consoleMode">Console mode manager</param>
        public CommandProcessor(TelnetConnection telnet, ConsoleMode consoleMode)
        {
            this.telnet = telnet;
            this.consoleMode = consoleMode;
        }

        /// <summary>
        /// Process a console command
        /// Based on otelnet.c:719-900 (otelnet_process_console_command)
        /// </summary>
        /// <param name="command">Command string</param>
        /// <returns>True if command was processed successfully</returns>
        public bool ProcessCommand(string command)
        {
            if (command == null)
            {
                return false;
            }

            // Trim whitespace
            command = command.Trim();

            // Empty command - return to client mode
            if (command.Length == 0)
            {
                consoleMode.Exit();
                return true;
            }

            // Parse command and arguments
            string[] parts = ParseCommandLine(command);
            if (parts.Length == 0)
            {
                return false;
            }

            string cmd = parts[0].ToLower();
            string[] args = new string[parts.Length - 1];
            Array.Copy(parts, 1, args, 0, args.Length);

            // Process commands
            switch (cmd)
            {
                case "quit":
                case "exit":
                    return HandleQuit();

                case "help":
                case "?":
                    return HandleHelp();

                case "stats":
                    return HandleStats();

                case "ls":
                    return HandleLs(args);

                case "pwd":
                    return HandlePwd();

                case "cd":
                    return HandleCd(args);

                // File transfer commands (not yet implemented)
                case "sz":
                case "sy":
                case "sx":
                case "rz":
                case "ry":
                case "rx":
                case "kermit":
                    System.Console.WriteLine($"\r\n[File transfer command '{cmd}' not yet implemented]");
                    return true;

                default:
                    System.Console.WriteLine($"\r\n[Unknown command: {cmd}. Type 'help' for available commands]");
                    return false;
            }
        }

        /// <summary>
        /// Parse command line into program and arguments
        /// Based on otelnet.c:580-620 (otelnet_parse_command_args)
        /// </summary>
        /// <param name="commandLine">Command line string</param>
        /// <returns>Array of command parts</returns>
        private string[] ParseCommandLine(string commandLine)
        {
            List<string> parts = new List<string>();
            bool inQuotes = false;
            int start = 0;

            for (int i = 0; i < commandLine.Length; i++)
            {
                char c = commandLine[i];

                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if ((c == ' ' || c == '\t') && !inQuotes)
                {
                    if (i > start)
                    {
                        parts.Add(commandLine.Substring(start, i - start).Replace("\"", ""));
                    }
                    start = i + 1;
                }
            }

            // Add last part
            if (start < commandLine.Length)
            {
                parts.Add(commandLine.Substring(start).Replace("\"", ""));
            }

            return parts.ToArray();
        }

        /// <summary>
        /// Handle quit/exit command
        /// Based on otelnet.c:745-748
        /// </summary>
        private bool HandleQuit()
        {
            System.Console.WriteLine("\r\n[Exiting...]");
            QuitRequested?.Invoke(this, EventArgs.Empty);
            return true;
        }

        /// <summary>
        /// Handle help command
        /// Based on otelnet.c:751-788
        /// </summary>
        private bool HandleHelp()
        {
            System.Console.WriteLine("\r\n");
            System.Console.WriteLine("=== Console Commands ===");
            System.Console.WriteLine("  [empty]       - Return to client mode");
            System.Console.WriteLine("  quit, exit    - Disconnect and exit program");
            System.Console.WriteLine("  help, ?       - Show this help message");
            System.Console.WriteLine("  stats         - Show connection statistics");
            System.Console.WriteLine();
            System.Console.WriteLine("=== File Transfer Commands ===");
            System.Console.WriteLine("Send Files:");
            System.Console.WriteLine("  sz [options] <files...> - Send via ZMODEM (default)");
            System.Console.WriteLine("  sy <files...>           - Send via YMODEM");
            System.Console.WriteLine("  sx <file>               - Send via XMODEM (single file)");
            System.Console.WriteLine("  kermit [args]           - Run kermit file transfer");
            System.Console.WriteLine();
            System.Console.WriteLine("Receive Files:");
            System.Console.WriteLine("  rz [options]  - Receive via ZMODEM (default)");
            System.Console.WriteLine("  ry            - Receive via YMODEM");
            System.Console.WriteLine("  rx            - Receive via XMODEM (single file)");
            System.Console.WriteLine();
            System.Console.WriteLine("=== File Management ===");
            System.Console.WriteLine("  ls [dir]      - List files in directory");
            System.Console.WriteLine("  pwd           - Print working directory");
            System.Console.WriteLine("  cd <dir>      - Change directory");
            System.Console.WriteLine();
            System.Console.WriteLine("=== Examples ===");
            System.Console.WriteLine("  sz file.txt              - Send via ZMODEM");
            System.Console.WriteLine("  rz                       - Receive via ZMODEM");
            System.Console.WriteLine("  ls /tmp                  - List /tmp directory");
            System.Console.WriteLine("========================");

            return true;
        }

        /// <summary>
        /// Handle stats command
        /// Based on otelnet.c:791-794
        /// </summary>
        private bool HandleStats()
        {
            if (telnet != null)
            {
                telnet.PrintStatistics();
            }
            else
            {
                System.Console.WriteLine("\r\n[No active connection]");
            }

            return true;
        }

        /// <summary>
        /// Handle ls command
        /// Based on otelnet.c:797-820
        /// </summary>
        private bool HandleLs(string[] args)
        {
            try
            {
                string path = args.Length > 0 ? args[0] : ".";

                // Use System.IO.Directory to list files
                if (System.IO.Directory.Exists(path))
                {
                    System.Console.WriteLine($"\r\n[Directory listing: {System.IO.Path.GetFullPath(path)}]");

                    string[] entries = System.IO.Directory.GetFileSystemEntries(path);
                    foreach (string entry in entries)
                    {
                        string name = System.IO.Path.GetFileName(entry);
                        if (System.IO.Directory.Exists(entry))
                        {
                            System.Console.WriteLine($"  {name}/");
                        }
                        else
                        {
                            long size = new System.IO.FileInfo(entry).Length;
                            System.Console.WriteLine($"  {name} ({size} bytes)");
                        }
                    }
                    System.Console.WriteLine($"\r\n[Total: {entries.Length} entries]");
                }
                else
                {
                    System.Console.WriteLine($"\r\n[Error: Directory not found: {path}]");
                    return false;
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"\r\n[Error listing directory: {ex.Message}]");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Handle pwd command
        /// Based on otelnet.c:840-855
        /// </summary>
        private bool HandlePwd()
        {
            try
            {
                string cwd = System.IO.Directory.GetCurrentDirectory();
                System.Console.WriteLine($"\r\n{cwd}");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"\r\n[Error getting current directory: {ex.Message}]");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Handle cd command
        /// Based on otelnet.c:822-838
        /// </summary>
        private bool HandleCd(string[] args)
        {
            if (args.Length == 0)
            {
                System.Console.WriteLine("\r\n[Error: cd requires a directory argument]");
                return false;
            }

            try
            {
                string path = args[0];

                // Expand ~ to home directory
                if (path.StartsWith("~"))
                {
                    string home = Environment.GetEnvironmentVariable("HOME");
                    if (string.IsNullOrEmpty(home))
                    {
                        home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                    }
                    path = path.Replace("~", home);
                }

                System.IO.Directory.SetCurrentDirectory(path);
                System.Console.WriteLine($"\r\n[Changed to: {System.IO.Directory.GetCurrentDirectory()}]");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"\r\n[Error changing directory: {ex.Message}]");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Show prompt
        /// </summary>
        public void ShowPrompt()
        {
            System.Console.Write("otelnet> ");
            System.Console.Out.Flush();
        }
    }
}
