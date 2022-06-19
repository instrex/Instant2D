using System;
using System.Collections.Generic;

namespace Instant2D.Assets {
    public interface ILazyAssetLoader {
        /// <summary>
        /// Load the asset on-demand.
        /// </summary>
        /// <param name="asset"></param>
        void LoadOnDemand(LazyAsset asset);
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
