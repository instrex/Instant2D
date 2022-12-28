using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Instant2D.Diagnostics {
    /// <summary>
    /// Generic interface for any logger implementation.
    /// </summary>
    public interface ILogger {
        /// <summary>
        /// Write information to the log.
        /// </summary>
        void Info(string message,
            [CallerFilePath] string callerFilePath = default,
            [CallerMemberName] string callerMemberName = default);

        /// <summary>
        /// Write warning to the log.
        /// </summary>
        void Warn(string message,
            [CallerFilePath] string callerFilePath = default,
            [CallerMemberName] string callerMemberName = default);

        /// <summary>
        /// Write an error to the log.
        /// </summary>
        void Error(string message,
            [CallerFilePath] string callerFilePath = default,
            [CallerMemberName] string callerMemberName = default);

        /// <summary>
        /// Called when the game exits.
        /// </summary>
        public void Close();
    }
}
