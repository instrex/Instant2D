using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.IO;

namespace Instant2D.Assets.Repositories {
    /// <summary>
    /// Asset repository which uses normal folder as source.
    /// </summary>
    public class FileSystemRepository : IAssetRepository {
        /// <summary>
        /// Directory at which assets will be stored.
        /// </summary>
        public readonly string WorkingDirectory;

        public FileSystemRepository(string workingDirectory) {
            WorkingDirectory = workingDirectory;
        }

        public IEnumerable<string> EnumerateFiles(string directoryPath, string extensionFilter = null, bool recursive = false) {
            var path = Path.Combine(WorkingDirectory, directoryPath);

            // directory doesn't exist, return
            if (!Directory.Exists(path)) {
                yield break;
            }

            // search the folder
            foreach (var file in Directory.EnumerateFiles(path, extensionFilter ?? "*.*", recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)) {
                var trimmedPath = file[(file.IndexOf(WorkingDirectory) + WorkingDirectory.Length)..];
                yield return trimmedPath.Replace('\\', '/');
            }
        }

        public bool Exists(string filename) => File.Exists(Path.Combine(WorkingDirectory, filename));

        public Stream OpenStream(string path) {
            var actualPath = Path.Combine(WorkingDirectory, path);

            // check if file exists first
            if (!File.Exists(actualPath)) {
                return null;
            }

            // open the funny stream
            return TitleContainer.OpenStream(actualPath);
        }
    }
}
