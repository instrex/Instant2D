using Instant2D.Utils.ResolutionScaling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D.EC.Events {
    /// <summary>
    /// An event that triggers when user resizes the game window.
    /// </summary>
    public record SceneResolutionChangedEvent {
        /// <summary>
        /// Previously applied resolution.
        /// </summary>
        public ScaledResolution PreviousResolution { get; init; }

        /// <summary>
        /// New resolution
        /// </summary>
        public ScaledResolution Resolution { get; init; }
    }
}
