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
using Instant2D.Assets.Repositories;
using System.Runtime.CompilerServices;
using Instant2D.Assets.Containers;
using System.Text.RegularExpressions;
using Instant2D.EC;
using Instant2D.Coroutines;

namespace Instant2D {
    public class AssetManager : GameSystem, IAssetRepository {
        public static AssetManager Instance { get; set; }

        /// <summary>
        /// The folder at which assets should be loaded, relative to the app path. Defaults to 'Assets/'
        /// </summary>
        public string Folder => _assetFolder;

        /// <summary>
        /// Asset repository which could be used to enumerate or load files. Provides a nice way to plug in your own asset reading middleware. <br/>
        /// As an example: loading assets from packed '.bin' file in a unified way, without having to write new <see cref="IAssetLoader"/>s. 
        /// </summary>
        public IAssetRepository Repository;

        readonly List<(int order, IAssetLoader)> _loaders = new();
        readonly Dictionary<string, Asset> _assets = new();
        readonly Dictionary<string, Coroutine> _hotReloadTimers = new();
        FileSystemWatcher _hotReloadWatcher;
        string _assetFolder = "Assets/";
        bool _assetsLoaded;

        #region Repositories

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerable<string> EnumerateFiles(string directoryPath, string extensionFilter = null, bool recursive = false) =>
            Repository.EnumerateFiles(directoryPath, extensionFilter, recursive);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Stream OpenStream(string path) => Repository.OpenStream(path);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Exists(string filename) => Repository.Exists(filename);

        #endregion

        #region Loaders

        public T AddLoader<T>(int order = 0) where T: IAssetLoader, new() {
            if (_assetsLoaded) {
                throw new InvalidOperationException("Cannot add loaders after the assets has been loaded.");
            }

            var loader = new T();
            _loaders.Add((order, loader));

            return loader;
        }

        #endregion

        #region Hot Reloading

        /// <inheritdoc cref="Folder"/>
        public AssetManager SetAssetsFolder(string path) {
            var newPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);

            if (!Directory.Exists(newPath)) {
                InstantApp.Logger.Error($"Couldn't set AssetManager Folder to '{path}': directory does not exist.");
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
            if (devEnvEnabled) {
                InstantApp.Logger.Info($"Enabled hot reload for dev environment at: '{path}'");
            } else {
                InstantApp.Logger.Warn($"Failed to enable hot reload for dev environment, falling back to: '{path}'");
            }

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

            // stop existing timer
            if (_hotReloadTimers.TryGetValue(assetKey, out var timer))
                timer.Stop();

            // if we do it immediately, there's a chance the file could be used by something
            // adding a little bit of delay helps making sure that wouldn't happen
            _hotReloadTimers.AddOrSet(assetKey, CoroutineManager.Schedule(0.5f, () => {
                var format = Path.GetExtension(assetKey);
                foreach (var loader in _loaders.Select(p => p.Item2)
                    .OfType<IHotReloader>()) {

                    if (loader.TryReload(assetKey, out var updatedAssets)) {
                        InstantApp.Logger.Info($"Hot Reload: updated {updatedAssets.Count()} assets.");

                        // call events for EC to handle the asset change
                        if (SceneManager.Instance?.Current is Scene scene) {
                            scene.OnAssetsUpdated(updatedAssets);
                        }

                        break;
                    }
                }

                // clear the timer
                _hotReloadTimers.Remove(assetKey);
            }));
        }

        #endregion

        public override void Initialize() {
            Instance = this;

            // create repository
            Repository = new FileSystemRepository(_assetFolder);

            // load assets
            var progress = new LoadingProgress();
            _loaders.Sort((a, b) => a.order.CompareTo(b.order));
            foreach (var (_, loader) in _loaders) {
                if (loader is IHotReloader hotReloadable)
                    foreach (var pattern in hotReloadable.WatcherPatterns) {
                        _hotReloadWatcher?.Filters.Add(pattern);
                    }

                // run all loaders in order, saving assets
                foreach (var asset in loader.Load(this)) {
                    _assets.Add(asset.Key, asset);
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
        /// Get raw <see cref="Asset"/> container instance.
        /// </summary>
        public Asset GetContainer(string key) {
            if (_assets.TryGetValue(key, out var asset))
                return asset;

            return null;
        }

        /// <summary>
        /// Remove asset from the asset system.
        /// </summary>
        public void Remove(string key) {
            if (_assets.Remove(key, out var removedAsset) && removedAsset.Children != null) {
                foreach (var child in removedAsset.Children) {
                    Remove(child.Key);
                }
            }
        }
        

        /// <summary>
        /// Register an asset container manually.
        /// </summary>
        public void Register(string key, Asset asset, bool registerChildren = true) {
            if (!_assets.TryAdd(key, asset)) {
                InstantApp.Logger.Warn($"Asset with key '{key}' was already added, skipping.");
            }

            if (registerChildren && asset.Children != null) {
                foreach (var child in asset.Children) {
                    Register(child.Key, child);
                }
            }
        }

        /// <summary>
        /// Perform an asset search using the <paramref name="pattern"/>. For convenience purposes, provided pattern will be automatically transformed into a regex. <br/>
        /// You can set <paramref name="autoTransformPattern"/> to <see langword="true"/> in order to override that behaviour and provide your own regex pattern. <br/>
        /// Example: <c>"folder/sprite_*"</c> will yield <c>"folder/sprite_0"</c>, <c>"folder/sprite_1"</c> etc.
        /// </summary>
        public IEnumerable<T> Search<T>(string pattern, bool autoTransformPattern = true) { 
            static string Transform(string input) {
                var builder = new StringBuilder();
                for (var i = 0; i < input.Length; i++) {
                    switch (input[i]) {
                        default:
                            builder.Append(input[i]);
                            break;

                        // conveniently replace asterisk with mathcing pattern
                        case '*':
                            builder.Append("(.*)");
                            break;

                        // escape special characters, in case they're used in asset paths somehow
                        case '\\' or '+' or '?' or '|' or '^'
                            or '[' or ']' or '(' or ')' or '{' 
                            or '#' or '.' or '$':
                            builder.Append('\\');
                            builder.Append(input[i]);
                            break;
                    }
                }
                
                return builder.ToString();
            }

            var regex = new Regex(autoTransformPattern ? Transform(pattern) : pattern, RegexOptions.Singleline);
            foreach (var (key, asset) in _assets) {
                // do a type check first to quickly brush off uninteresting assets
                if (asset is IAssetContainer<T> container && regex.IsMatch(key)) {
                    yield return container.Content;
                }
            }
        }

        /// <summary>
        /// Attempts to get the asset content with given key and type. Will throw if no asset is to be found.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Get<T>(string key) {
            if (!TryGet<T>(key, out var asset)) {
                throw new InvalidOperationException($"Asset of type '{typeof(T).Name}' with key '{key}' wasn't found.");
            }

            return asset;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGet<T>(string key, out T value) {
            if (_assets.TryGetValue(key, out var asset)) {
                if (asset is not IAssetContainer<T> typedAsset)
                    throw new InvalidOperationException($"Cannot get asset '{key}' with type '{typeof(T).Name}', as its actual type is '{asset.GetType().Name}'.");

                value = typedAsset.Content;
                return true;
            }

            value = default;
            return false;
        }

        #endregion
    }
}
