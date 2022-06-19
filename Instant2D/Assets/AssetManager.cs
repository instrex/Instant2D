using Instant2D.Core;
using Instant2D.Assets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D {
    public class AssetManager : SubSystem {
        public static AssetManager Instance { get; set; }

        readonly List<(int order, IAssetLoader)> _loaders = new();
        readonly Dictionary<string, Asset> _assets = new();
        bool _assetsLoaded;

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

        public override void Initialize() {
            Instance = this;

            // load assets
            var progress = new LoadingProgress();
            _loaders.Sort((a, b) => a.order.CompareTo(b.order));
            foreach (var (_, loader) in _loaders) {
                // run all loaders in order, saving assets
                foreach (var asset in loader.Load(this, progress)) {
                    _assets.Add(asset.Key, asset);
                    InstantGame.Instance.Logger.Info($"+ {asset.ToString()} '{asset.Key}'");
                }
            }

            // lock the assets
            _assetsLoaded = true;
        }

        /// <summary>
        /// Iterates over all loaded assets and returns all currently loaded assets of type <typeparamref name="T"/>.
        /// </summary>
        public IEnumerable<T> GetAll<T>() {
            foreach (var asset in _assets.Values.OfType<T>())
                yield return asset;
        }

        /// <summary>
        /// Attempts to get the asset with given key and type. Will throw if no asset is to be found.
        /// </summary>
        public T Get<T>(string key) {
            if (_assets.TryGetValue(key, out var asset)) {
                if (asset is not IAssetContainer<T> typedAsset)
                    throw new InvalidOperationException($"Cannot get asset '{key}' with type '{typeof(T).Name}', as its actual type is '{asset.GetType().Name}'.");

                return typedAsset.Content;
            }

            throw new InvalidOperationException($"Asset of type '{typeof(T).Name}' with key '{key}' wasn't found.");
        }
    }
}
