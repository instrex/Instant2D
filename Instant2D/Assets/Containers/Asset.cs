using Instant2D.Assets.Containers;
using System.Collections.Generic;
using System.Linq;

namespace Instant2D.Assets {
    /// <summary>
    /// Base Asset class for use within <see cref="AssetManager"/>.
    /// </summary>
    public abstract record Asset {
        public string Key;

        /// <summary>
        /// Parent asset.
        /// </summary>
        public Asset Parent;

        /// <summary>
        /// Children assets.
        /// </summary>
        public List<Asset> Children;

        /// <summary>
        /// Add specified Asset as a child asset. 
        /// </summary>
        public void AddChild(Asset child) {
            Children ??= new List<Asset>();

            // add the child and mark this as parent
            Children.Add(child);
            child.Parent = this;
        }

        /// <summary>
        /// User data.
        /// </summary>
        public object Data;
    }

    /// <summary>
    /// Simple typed Asset, is assumed to be loaded at the moment of instantiation.
    /// </summary>
    public record Asset<T> : Asset, IAssetContainer<T> {
        public Asset(string key, T content) {
            Content = content;
            Key = key;
        }

        public T Content { get; set; }
    }
}
