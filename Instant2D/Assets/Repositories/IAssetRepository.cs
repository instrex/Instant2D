using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D.Assets {
    /// <summary>
    /// Base interface for asset repositories.
    /// </summary>
    public interface IAssetRepository {
        /// <summary>
        /// Returns all normalized file paths in specified directory.
        /// </summary>
        IEnumerable<string> EnumerateFiles(string directoryPath, string extensionFilter = default, bool recursive = false);

        /// <summary>
        /// Opens the file stream with specified path or returns <see langword="null"/> if it doesn't exist.
        /// </summary>
        Stream OpenStream(string path);
    }
}
