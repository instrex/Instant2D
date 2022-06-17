using System;
using System.Collections.Generic;

namespace Instant2D.Assets {
    public class Asset {
        public string Key { get; init; }
    }

    public class Asset<T> : Asset {
        public T Content { get; init; }
    }

    public interface IAssetLoader {
        /// <summary>
        /// Load all of the assets from game folder.
        /// </summary>
        IEnumerable<Asset> Load(AssetManager assets, LoadingProgress progress);

        /// <summary>
        /// Default implementation of <see cref="IAssetLoader"/> using the delegate.
        /// </summary>
        public struct DefaultLoader : IAssetLoader {
            public Func<IEnumerable<Asset>> loader;
            public IEnumerable<Asset> Load(AssetManager assets, LoadingProgress progress) {
                return loader();
            }
        }
    }
}
