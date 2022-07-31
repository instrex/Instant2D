using Instant2D.Assets.Containers;

namespace Instant2D.Assets {
    /// <summary>
    /// Asset base class that supports on-demand loading.
    /// </summary>
    public abstract record LazyAsset : Asset {
        readonly ILazyAssetLoader _loader;
        bool _isLoaded;

        /// <summary>
        /// Whether or not this asset was loaded yet.
        /// </summary>
        public bool IsLoaded => _isLoaded;

        /// <summary>
        /// Force load this asset.
        /// </summary>
        public void Load() {
            if (Parent is LazyAsset lazyParent && !lazyParent.IsLoaded) {
                // load lazy parent
                lazyParent.Load();
            }

            _loader.LoadOnDemand(this);
            _isLoaded = true;
        }

        /// <summary>
        /// Unsets the loaded flag, causing the asset to load again on the next request.
        /// </summary>
        public void Unload() {
            _isLoaded = false;

            if (Children != null) {
                foreach (var child in Children) {
                    // unload lazy children
                    if (child is LazyAsset lazyChild && lazyChild.IsLoaded)
                        lazyChild.Unload();
                }
            }
        }

        protected LazyAsset(ILazyAssetLoader loader) {
            _loader = loader;
        }
    }

    /// <summary>
    /// Lazily loaded Asset, will ask the <see cref="ILazyAssetLoader"/> to load stuff on-demand.
    /// </summary>
    public record LazyAsset<T> : LazyAsset, IAssetContainer<T> {
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

        public LazyAsset(string key, ILazyAssetLoader loader) : base(loader) {
            Key = key;
        }
    }
}
