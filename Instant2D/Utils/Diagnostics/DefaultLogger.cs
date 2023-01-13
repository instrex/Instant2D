using Instant2D.Coroutines;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace Instant2D.Diagnostics {
    public class DefaultLogger : ILogger {
        record struct LogEntry(string Message, int Severity, DateTime Time);

        readonly List<LogEntry> _entries = new();
        readonly StringBuilder _builder = new();

        StreamWriter _writer;
        FileStream _file;

        void LogMessage(string message, string callerFilePath, string callerMemberName, DateTime date, int severity) {
            _builder.Clear();

            _builder.Append('[');

            // log time
            _builder.Append(date.ToString("HH:mm:ss"));

            _builder.Append('/');

            // log severity
            _builder.Append(severity switch {
                0 => "INFO",
                1 => "WARN",
                2 => "ERROR",
                _ => "???"
            });

            _builder.Append(']');

            // if any caller info is available, append it as well
            if (callerMemberName != null || callerFilePath != null) {
                _builder.Append(" [");
                
                // write the filename when available
                if (callerFilePath != null) {
                    var file = Path.GetFileName(callerFilePath);
                    _builder.Append(file);

                    // separate it with '/'
                    if (callerMemberName != null) {
                        _builder.Append('/');
                    }
                }

                // write method name when available
                if (callerMemberName != null) {
                    _builder.Append(callerMemberName);
                }

                _builder.Append(']');
            }

            _builder.Append(": ");

            // append the message
            _builder.Append(message);

            var entry = new LogEntry(_builder.ToString(), severity, date);

            // show the message
            DisplayMessage(in entry);

            // save the message for writing later
            _entries.Add(entry);
        }

        static void DisplayMessage(in LogEntry entry) {
            var oldForegroundColor = Console.ForegroundColor;
            switch (entry.Severity) {
                case 1:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;

                case 2:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
            }

            Console.WriteLine(entry.Message);

            // reset colors
            Console.ForegroundColor = oldForegroundColor;
        }
        
        void Flush() {
            foreach (var entry in _entries) {
                // write all the entries
                _writer.WriteLine(entry.Message);
            }

            // send the data
            _writer.Flush();

            // then clear
            _entries.Clear();
        }

        /// <summary>
        /// Sets the output file to flush the contents of this logger in specific intervals.
        /// </summary>
        public void SetOutputFile(string filename, float flushInterval = 10.0f) {
            // get file info to generate numbered log files
            var directory = Path.GetDirectoryName(filename);
            var name = Path.GetFileNameWithoutExtension(filename);
            var extension = Path.GetExtension(filename);

            for (var i = 0; i < 10; i++) {
                var path = Path.Combine(directory, $"{name}{(i != 0 ? i.ToString() : "")}{extension}");

                // in some cases multiple game windows may be opened,
                // this makes it possible for two loggers to coexist

                try {
                    _file = File.OpenWrite(path);
                    _writer = new StreamWriter(_file);
                    CoroutineManager.Instance.Schedule(flushInterval, this, logger => {
                        logger.Flush();
                        return logger._file != null;
                    });

                    return;

                } catch { }
            }

            Warn($"Could not open log stream at '{filename}'");
        }

        #region ILogger implementation

        public void Close() {
            if (_writer == null)
                return;

            Flush();

            // dispose of files
            _writer.Dispose();
            _file.Dispose();

            // and of references
            _writer = null;
            _file = null;
        }

        public void Info(string message, [CallerFilePath] string callerFilePath = default, [CallerMemberName] string callerMemberName = default) =>
            LogMessage(message, callerFilePath, callerMemberName, DateTime.Now, 0);

        public void Warn(string message, [CallerFilePath] string callerFilePath = default, [CallerMemberName] string callerMemberName = default) =>
            LogMessage(message, callerFilePath, callerMemberName, DateTime.Now, 1);

        public void Error(string message, [CallerFilePath] string callerFilePath = default, [CallerMemberName] string callerMemberName = default) =>
            LogMessage(message, callerFilePath, callerMemberName, DateTime.Now, 2);

        #endregion
    }
}
