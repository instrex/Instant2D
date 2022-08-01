using Instant2D.Assets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D.EC.Events {
    /// <summary>
    /// An event that triggers when something is updated via <see cref="IHotReloader"/>.
    /// </summary>
    public record AssetReloadedEvent {
        /// <summary>
        /// Collection of updated assets.
        /// </summary>
        public Asset UpdatedAsset;
    }
}
