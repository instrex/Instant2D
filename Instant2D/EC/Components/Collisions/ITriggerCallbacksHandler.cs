using Instant2D.EC.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D.EC.Collisions {
    /// <summary>
    /// Handler interface for trigger callbacks.
    /// </summary>
    public interface ITriggerCallbacksHandler {
        /// <summary>
        /// Is called when <paramref name="self"/> enters <paramref name="other"/>.
        /// </summary>
        void OnTriggerEnter(CollisionComponent self, CollisionComponent other);

        /// <summary>
        /// Is called when <paramref name="self"/> exits <paramref name="other"/>.
        /// </summary>
        void OnTriggerExit(CollisionComponent self, CollisionComponent other);
    }
}
