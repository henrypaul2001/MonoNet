using Microsoft.SqlServer.Server;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkingLibrary
{
    public enum LoggingMode
    {
        OVERWRITE,
        APPEND
    }

    public enum LoggingFormat
    {
        DATETIMEANDMESSAGE,
        TIMEANDMESSAGE,
        DATEANDMESSAGE,
        JUSTMESSAGE
    }

    public class Logger
    {
        string filepath;
        LoggingFormat format;
        LoggingMode mode;

        bool americanDateFormat;

        public Logger(string filepath, LoggingMode mode, LoggingFormat format, bool americanDateFormat)
        {
            this.filepath = filepath;
            this.mode = mode;
            this.format = format;
            this.americanDateFormat = americanDateFormat;
        }

        public Logger(string filepath, LoggingMode mode, LoggingFormat format)
        {
            this.filepath = filepath;
            this.mode = mode;
            this.format = format;
            americanDateFormat = false;
        }

        public void Log(string message, LoggingMode mode, LoggingFormat format)
        {
            this.mode = mode;
            this.format = format;
            WriteLine(message);
        }

        public void Log(string message, LoggingFormat format)
        {
            this.format = format;
            WriteLine(message);
        }

        public void Log(string message, LoggingMode mode)
        {
            this.mode = mode;
            WriteLine(message);
        }

        public void Log(string message)
        {
            WriteLine(message);
        }

        void WriteLine(string message)
        {
            bool append = false;
            if (mode == LoggingMode.APPEND) { append = true; }
            StreamWriter sw = new StreamWriter(filepath, append);

            string output = "";
            DateTime now = DateTime.Now;
            string date;
            if (americanDateFormat) { date = $"{now:MM/dd/yy}"; }
            else { date = $"{now:dd/MM/yy}"; }

            switch (format)
            {
                case LoggingFormat.DATETIMEANDMESSAGE:
                    output = $"[{date} | {now:HH:mm:ss}] {message}";
                    break;
                case LoggingFormat.TIMEANDMESSAGE:
                    output = $"[{now:HH:mm:ss}] {message}";
                    break;
                case LoggingFormat.DATEANDMESSAGE:
                    output = $"[{date}] {message}";
                    break;
                case LoggingFormat.JUSTMESSAGE:
                    output = message;
                    break;
            }

            sw.WriteLine(output);
            sw.Close();
        }
    }
}
