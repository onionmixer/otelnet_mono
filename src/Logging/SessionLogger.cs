using System;
using System.IO;

namespace OtelnetMono.Logging
{
    /// <summary>
    /// Session logger for telnet connections
    /// Logs all data sent/received with timestamps and hex dump format
    /// </summary>
    public class SessionLogger : IDisposable
    {
        // ====================================================================
        // Private Fields
        // ====================================================================

        private StreamWriter logWriter;
        private string logFilePath;
        private bool isEnabled;
        private bool isDisposed;

        // ====================================================================
        // Properties
        // ====================================================================

        /// <summary>Is logging enabled</summary>
        public bool IsEnabled => isEnabled && logWriter != null;

        /// <summary>Log file path</summary>
        public string LogFilePath => logFilePath;

        // ====================================================================
        // Constructor
        // ====================================================================

        /// <summary>
        /// Initialize session logger (disabled by default)
        /// </summary>
        public SessionLogger()
        {
            logWriter = null;
            logFilePath = null;
            isEnabled = false;
            isDisposed = false;
        }

        /// <summary>
        /// Initialize session logger with file path
        /// </summary>
        /// <param name="filePath">Path to log file</param>
        /// <param name="append">Append to existing file (default true)</param>
        public SessionLogger(string filePath, bool append = true)
        {
            logWriter = null;
            logFilePath = filePath;
            isEnabled = false;
            isDisposed = false;

            if (!string.IsNullOrEmpty(filePath))
            {
                Start(filePath, append);
            }
        }

        // ====================================================================
        // Public Methods
        // ====================================================================

        /// <summary>
        /// Start logging to file
        /// Based on original C implementation (otelnet.c:392-410)
        /// </summary>
        /// <param name="filePath">Path to log file</param>
        /// <param name="append">Append to existing file (default true)</param>
        /// <returns>True if logging started successfully</returns>
        public bool Start(string filePath, bool append = true)
        {
            if (isEnabled)
            {
                Console.WriteLine("[WARNING] Logging already started");
                return false;
            }

            try
            {
                logFilePath = filePath;
                logWriter = new StreamWriter(filePath, append);
                isEnabled = true;

                // Write session start marker
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                logWriter.WriteLine();
                logWriter.WriteLine($"[{timestamp}] === Session started ===");
                logWriter.Flush();

                Console.WriteLine($"[INFO] Session logging started: {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to start logging: {ex.Message}");
                logWriter = null;
                isEnabled = false;
                return false;
            }
        }

        /// <summary>
        /// Stop logging and close file
        /// Based on original C implementation (otelnet.c:416-434)
        /// </summary>
        public void Stop()
        {
            if (!isEnabled || logWriter == null)
            {
                return;
            }

            try
            {
                // Write session end marker
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                logWriter.WriteLine($"[{timestamp}] === Session ended ===");
                logWriter.WriteLine();
                logWriter.Flush();

                logWriter.Close();
                logWriter = null;
                isEnabled = false;

                Console.WriteLine("[INFO] Session logging stopped");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to stop logging: {ex.Message}");
            }
        }

        /// <summary>
        /// Log data sent to server
        /// </summary>
        /// <param name="data">Data bytes</param>
        public void LogSent(byte[] data)
        {
            LogData("SENT", data);
        }

        /// <summary>
        /// Log data received from server
        /// </summary>
        /// <param name="data">Data bytes</param>
        public void LogReceived(byte[] data)
        {
            LogData("RECV", data);
        }

        /// <summary>
        /// Log data with direction indicator
        /// Based on original C implementation (otelnet.c:439-499)
        /// </summary>
        /// <param name="direction">Direction ("SENT" or "RECV")</param>
        /// <param name="data">Data bytes</param>
        public void LogData(string direction, byte[] data)
        {
            if (!isEnabled || logWriter == null || data == null || data.Length == 0)
            {
                return;
            }

            try
            {
                // Get timestamp
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                // Create prefix for each line
                string prefix = $"[{timestamp}][{direction}] ";

                // Write hex dump
                HexDumper.WriteHexDump(logWriter, data, prefix);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to write log: {ex.Message}");
            }
        }

        /// <summary>
        /// Log a message
        /// </summary>
        /// <param name="message">Message to log</param>
        public void LogMessage(string message)
        {
            if (!isEnabled || logWriter == null)
            {
                return;
            }

            try
            {
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                logWriter.WriteLine($"[{timestamp}] {message}");
                logWriter.Flush();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to write log message: {ex.Message}");
            }
        }

        // ====================================================================
        // IDisposable
        // ====================================================================

        public void Dispose()
        {
            if (!isDisposed)
            {
                Stop();
                isDisposed = true;
            }
        }
    }
}
