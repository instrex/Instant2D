using System;
using System.Collections.Generic;

namespace Instant2D.Assets {
    public interface ILazyAssetLoader {
        /// <summary>
        /// Called when <see cref="LazyAsset"/> requests the asset load.
        /// </summary>
        void LoadOnDemand(LazyAsset asset);
    }


    public interface IAssetLoader {
        /// <summary>
        /// Loads assets from game's repository.
        /// </summary>
        IEnumerable<Asset> Load(AssetManager assets);
    }
}
