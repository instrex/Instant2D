using Instant2D.Core;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D.Utils {
    public class Logger : SubSystem, IDisposable {
        public enum Severity {
            None,
            Warning,
            Error
        }

        readonly TextWriter _writer;
        readonly FileStream _stream;

        public Logger() {
            _stream = File.OpenWrite(".log");
            _writer = new StreamWriter(_stream, Encoding.Unicode);

            _writer.WriteLine($"======= {DateTime.Now} =======");
        }

        void IDisposable.Dispose() {
            _writer.WriteLine($"======= THE END =======");
            _writer.Flush();

            _stream?.Dispose();
            _writer?.Dispose();

            GC.SuppressFinalize(this);
        }

        void Write(string message) {
            Console.Write(message);
            _writer.Write(message);
        }

        public override void Initialize() {
            IsUpdatable = true;
            Game._logger = this;
        }

        public override void Update(GameTime time) {
            // flush the filestream every 2 seconds
            if (TimeManager.FrameCount % 120 == 0) {
                _writer.Flush();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteLine(object message, Severity severity = Severity.None) {
            switch (severity) {
                case Severity.None:
                    InstantGame.Instance.Logger.Info(message);
                    break;

                case Severity.Error:
                    InstantGame.Instance.Logger.Error(message);
                    break;

                case Severity.Warning:
                    InstantGame.Instance.Logger.Warning(message);
                    break;
            }
        }

        public void Info(object message) => Write($"[{DateTime.Now}] {message} \n");

        public void Warning(object message) {
            Write($"[{DateTime.Now}] ");

            var prevColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Write($"WARNING");
            Console.ForegroundColor = prevColor;

            Write($" {message}\n");
        }

        public void Error(object message) {
            Write($"[{DateTime.Now}] ");

            var prevColor = Console.ForegroundColor;
            var prevColorBg = Console.BackgroundColor;
            Console.ForegroundColor = ConsoleColor.Black;
            Console.BackgroundColor = ConsoleColor.Red;
            Write($"ERROR");
            Console.ForegroundColor = prevColor;
            Console.BackgroundColor = prevColorBg;

            Write($" {message}\n");
        }
    }
}
