using System.Linq;

namespace Instant2D.Assets {
    /// <summary>
    /// Base Asset class for use with <see cref="AssetManager"/>.
    /// </summary>
    public abstract class Asset {
        public string Key { get; init; }

        public override string ToString() {
            var type = GetType();
            if (type.IsGenericType) {
                return $"{type.Name}<{string.Join(", ", type.GenericTypeArguments.Select(t => t.Name))}>";
            }

            return type.Name;
        }
    }

    /// <summary>
    /// Simple typed Asset, is assumed to be loaded at the moment of instantiation.
    /// </summary>
    public class Asset<T> : Asset, IAssetContainer<T> {
        public Asset(string key, T content) {
            Content = content;
            Key = key;
        }

        public T Content { get; init; }
    }

    /// <summary>
    /// Asset base class that supports on-demand loading.
    /// </summary>
    public abstract class LazyAsset : Asset {
        readonly ILazyAssetLoader _loader;
        bool _isLoaded;

        /// <summary>
        /// Whether or not this asset was loaded yet.
        /// </summary>
        public bool IsLoaded => _isLoaded;

        /// <summary>
        /// The other <see cref="LazyAsset"/> that this content depends on, will be force loaded before this on-demand.
        /// </summary>
        public LazyAsset DependsOn { get; set; }

        /// <summary>
        /// Force load this asset.
        /// </summary>
        public void Load() {
            if (DependsOn != null) {
                DependsOn.Load();
            }

            _loader.LoadOnDemand(this);
            _isLoaded = true;
        }

        protected LazyAsset(ILazyAssetLoader loader) {
            _loader = loader;
        }
    }

    /// <summary>
    /// Lazily loaded Asset, will ask the <see cref="ILazyAssetLoader"/> to load stuff on-demand.
    /// </summary>
    public class LazyAsset<T> : LazyAsset, IAssetContainer<T> {
        private T _content;

        /// <summary>
        /// Accessing the content will automatically trigger the load.
        /// </summary>
        public T Content {
            set => _content = value;
            get {
                if (!IsLoaded) {
                    Load();
                }

                return _content;
            }
        }

        public LazyAsset(string key, ILazyAssetLoader loader, LazyAsset dependsOn = default) : base(loader) {
            DependsOn = dependsOn;
            Key = key;
        }
    }
}
