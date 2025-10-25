using System;
using System.IO;
using System.Text;

namespace Otelnet.Logging;

/// <summary>
/// Hexadecimal dump formatter for logging binary data
/// Formats data as hex bytes + ASCII representation (16 bytes per line)
/// </summary>
public static class HexDumper
{
    /// <summary>
    /// Format data as hex dump with ASCII representation
    /// Based on original C implementation (otelnet.c:459-498)
    /// </summary>
    /// <param name="data">Data to format</param>
    /// <param name="prefix">Prefix for each line (e.g., timestamp + direction)</param>
    /// <returns>Formatted hex dump string</returns>
    public static string FormatHexDump(byte[] data, string prefix = "")
    {
        if (data == null || data.Length == 0)
        {
            return string.Empty;
        }

        StringBuilder sb = new StringBuilder();
        int len = data.Length;

        // Write first line prefix
        sb.Append(prefix);

        // Write data as hex and ASCII (16 bytes per line)
        for (int i = 0; i < len; i++)
        {
            // Write hex byte
            sb.AppendFormat("{0:x2} ", data[i]);

            // Every 16 bytes, write ASCII representation and start new line
            if ((i + 1) % 16 == 0 && i < len - 1)
            {
                sb.Append(" | ");

                // Write ASCII for this line
                for (int j = i - 15; j <= i; j++)
                {
                    char c = (char)data[j];
                    if (char.IsControl(c) || c > 126)
                    {
                        sb.Append('.');
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }

                // Start new line
                sb.AppendLine();
                sb.Append(prefix);
            }
        }

        // Write remaining ASCII for last line
        int remaining = len % 16;
        if (remaining > 0 || len < 16)
        {
            // Pad with spaces to align ASCII column
            int spaces = (16 - remaining) * 3;
            if (len < 16)
            {
                spaces = (16 - len) * 3;
            }

            for (int i = 0; i < spaces; i++)
            {
                sb.Append(' ');
            }

            sb.Append(" | ");

            // Write remaining ASCII
            int start = len - remaining;
            if (len < 16)
            {
                start = 0;
                remaining = len;
            }

            for (int i = start; i < start + remaining; i++)
            {
                char c = (char)data[i];
                if (char.IsControl(c) || c > 126)
                {
                    sb.Append('.');
                }
                else
                {
                    sb.Append(c);
                }
            }
        }

        sb.AppendLine();
        return sb.ToString();
    }

    /// <summary>
    /// Write hex dump to stream
    /// </summary>
    /// <param name="writer">Text writer to write to</param>
    /// <param name="data">Data to dump</param>
    /// <param name="prefix">Prefix for each line</param>
    public static void WriteHexDump(TextWriter writer, byte[] data, string prefix = "")
    {
        if (writer == null || data == null || data.Length == 0)
        {
            return;
        }

        string dump = FormatHexDump(data, prefix);
        writer.Write(dump);
        writer.Flush();
    }

    /// <summary>
    /// Write hex dump to console
    /// </summary>
    /// <param name="data">Data to dump</param>
    /// <param name="prefix">Prefix for each line</param>
    public static void DumpToConsole(byte[] data, string prefix = "")
    {
        WriteHexDump(Console.Out, data, prefix);
    }
}
