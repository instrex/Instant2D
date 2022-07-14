using Instant2D.Core;
using Instant2D.Assets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using Instant2D.Utils;
using Instant2D.Coroutines;

namespace Instant2D {
    public interface IHotReloader {
        /// <summary>
        /// Gets the file formats that this loader will look for.
        /// </summary>
        IEnumerable<string> GetFileFormats();

        /// <summary>
        /// Happens whenever something changes in the asset folder. Will run on each loader implementing <see cref="IHotReloader"/> until one of them returns <see langword="true"/>.
        /// </summary>
        bool TryReload(string assetKey, string filename);
    }

    public class AssetManager : SubSystem {
        public static AssetManager Instance { get; set; }

        /// <summary>
        /// The folder at which assets should be loaded, relative to the app path. Defaults to 'Assets/'
        /// </summary>
        public string Folder => _assetFolder;

        readonly List<(int order, IAssetLoader)> _loaders = new();
        readonly Dictionary<string, Asset> _assets = new();
        readonly Dictionary<string, TimerInstance> _hotReloadTimers = new();
        FileSystemWatcher _hotReloadWatcher;
        string _assetFolder = "Assets/";
        bool _assetsLoaded;

        #region Loaders

        public T AddLoader<T>(int order = 0) where T: IAssetLoader, new() {
            if (_assetsLoaded) {
                throw new InvalidOperationException("Cannot add loaders after the assets has been loaded.");
            }

            var loader = new T();
            _loaders.Add((order, loader));

            return loader;
        }

        public void AddLoader(int order, Func<IEnumerable<Asset>> loader) {
            if (_assetsLoaded) {
                throw new InvalidOperationException("Cannot add loaders after the assets has been loaded.");
            }

            _loaders.Add((order, new IAssetLoader.DefaultLoader { loader = loader }));
        }

        #endregion

        #region Hot Reloading

        /// <inheritdoc cref="Folder"/>
        public AssetManager SetAssetsFolder(string path) {
            var newPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);

            if (!Directory.Exists(newPath)) {
                Logger.WriteLine($"Couldn't set AssetManager Folder to '{path}': directory does not exist.", Logger.Severity.Error);
                return this;
            }

            _assetFolder = path;
            return this;
        }

        /// <summary>
        /// Enables hot reloading for AssetLoaders supporting it. 
        /// You may optionally specify <paramref name="devEnvironmentPath"/> for it to also react when you edit assets inside the project. <br/>
        /// By default, it only reacts to changed assets inside the build folder. ('bin/...')
        /// </summary>
        /// <remarks>
        /// If your project's name is 'MyGame' and you store dev assets inside 'Assets' folder, set <paramref name="devEnvironmentPath"/> to './MyGame/Assets/'.
        /// </remarks>
        public AssetManager SetupHotReload(string devEnvironmentPath = default) {
            var path = Folder;
            var devEnvEnabled = false;

            // if absolute path is provided, set it and that's it
            if (devEnvironmentPath != null && Directory.Exists(devEnvironmentPath)) {
                path = devEnvironmentPath;
                devEnvEnabled = true;
            }

            // if not, try to get dev path automatically
            if (!devEnvEnabled) {
                var normalizedPath = AppDomain.CurrentDomain.BaseDirectory.Replace('\\', '/');
                var index = normalizedPath.LastIndexOf("bin/");

                // dev env was found!
                if (index != -1) {
                    var devAssetPath = Path.Combine(normalizedPath[..index], Folder);
                    if (Directory.Exists(devAssetPath)) {
                        path = devAssetPath;
                        devEnvEnabled = true;
                    }
                }
            }

            // notify the user
            Logger.WriteLine(devEnvEnabled ? $"Enabled hot reload for dev environment at: '{path}'" : $"Failed to enable hot reload for dev environment, falling back to: '{path}'",
                    devEnvEnabled ? Logger.Severity.None : Logger.Severity.Warning);

            if (devEnvEnabled) {
                _assetFolder = path;

                // setup FileSystemWatcher
                _hotReloadWatcher = new FileSystemWatcher(path) {
                    //Filter = "*.*",
                    IncludeSubdirectories = true,
                    NotifyFilter = NotifyFilters.Attributes
                        | NotifyFilters.CreationTime
                        | NotifyFilters.DirectoryName
                        | NotifyFilters.FileName
                        | NotifyFilters.LastAccess
                        | NotifyFilters.LastWrite
                        | NotifyFilters.Security
                        | NotifyFilters.Size
                };

                _hotReloadWatcher.Changed += OnFileEdited;
                _hotReloadWatcher.Created += OnFileEdited;
                _hotReloadWatcher.Deleted += OnFileEdited;
                _hotReloadWatcher.Renamed += OnFileEdited;
            }

            return this;
        }

        void OnFileEdited(object sender, FileSystemEventArgs e) {
            var assetKey = e.Name.Replace('\\', '/');
            var fullPath = e.FullPath.Replace('\\', '/');

            // stop existing timer
            if (_hotReloadTimers.TryGetValue(assetKey, out var timer))
                timer.Stop();

            // if we do it immediately, there's a chance the file could be used by something
            // adding a little bit of delay helps making sure that wouldn't happen
            _hotReloadTimers.AddOrSet(assetKey, CoroutineManager.Schedule(0.5f, timer => {
                foreach (var loader in _loaders.Select(p => p.Item2).OfType<IHotReloader>()) {
                    if (loader.TryReload(assetKey, fullPath)) {
                        Logger.WriteLine($"Hot Reload: handled '{assetKey}'");
                        break;
                    }
                }
            }));
        }

        #endregion

        public override void Initialize() {
            Instance = this;

            // load assets
            var progress = new LoadingProgress();
            _loaders.Sort((a, b) => a.order.CompareTo(b.order));
            foreach (var (_, loader) in _loaders) {
                if (loader is IHotReloader hotReloadable)
                    foreach (var format in hotReloadable.GetFileFormats()) {
                        _hotReloadWatcher?.Filters.Add(format);
                    }

                // run all loaders in order, saving assets
                foreach (var asset in loader.Load(this, progress)) {
                    _assets.Add(asset.Key, asset);
                    InstantGame.Instance.Logger.Info($"+ {asset} '{asset.Key}'");
                }
            }

            // begin to watch for file changes
            if (_hotReloadWatcher != null) {
                _hotReloadWatcher.EnableRaisingEvents = true;
            }

            // lock the assets
            _assetsLoaded = true;
        }

        #region Asset Access

        /// <summary>
        /// Get raw <see cref="Asset"/> instance.
        /// </summary>
        public Asset GetAsset(string key) {
            if (_assets.TryGetValue(key, out var asset))
                return asset;

            return null;
        }

        /// <summary>
        /// Attempts to get the asset content with given key and type. Will throw if no asset is to be found.
        /// </summary>
        public T Get<T>(string key) {
            if (_assets.TryGetValue(key, out var asset)) {
                if (asset is not IAssetContainer<T> typedAsset)
                    throw new InvalidOperationException($"Cannot get asset '{key}' with type '{typeof(T).Name}', as its actual type is '{asset.GetType().Name}'.");

                return typedAsset.Content;
            }

            throw new InvalidOperationException($"Asset of type '{typeof(T).Name}' with key '{key}' wasn't found.");
        }

        #endregion
    }
}
