using System.Collections.Generic;

namespace Instant2D.Assets {
    /// <summary>
    /// Special interface used to implement in-game asset reloading. The way it works is:
    /// <list type="number">
    /// <item> When initializing loaders, <see cref="WatcherPatterns"/> will be registered for <see cref="System.IO.FileSystemWatcher"/> to look for. </item>
    /// <item> Upon receiving file change/rename event, <see cref="AssetManager"/> will wait for 0.5s before firing <see cref="TryReload(string, out IEnumerable{Asset})"/> on each <see cref="IHotReloader"/>. </item>
    /// <item> If one of the loaders return <see langword="true"/>, further events (for example, for Scenes to update components) will be fired. </item>
    /// </list>
    /// </summary>
    public interface IHotReloader {
        /// <summary>
        /// Patterns to determine to which files the reloader should react. Passed directly to <see cref="System.IO.FileSystemWatcher"/>.
        /// </summary>
        IEnumerable<string> WatcherPatterns { get; }

        /// <summary>
        /// Happens whenever something changes in the asset folder. Will run on each loader implementing <see cref="IHotReloader"/> until one of them returns <see langword="true"/>.
        /// </summary>
        bool TryReload(string assetName, out IEnumerable<Asset> updatedAssets);
    }
}
