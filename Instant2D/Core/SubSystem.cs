using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D.Core {
    public abstract class SubSystem : IComparable<SubSystem> {
        public int CompareTo(SubSystem other) {
            return UpdateOrder.CompareTo(other.UpdateOrder);
        }

        bool _shouldUpdate;

        /// <summary>
        /// The Game instance this system is attached to.
        /// </summary>
        public InstantGame Game { get; init; }

        /// <summary>
        /// Determines which systems will run first.
        /// </summary>
        public int UpdateOrder { get; set; }

        /// <summary>
        /// When <see langword="true"/>, this system's <see cref="Update(GameTime)"/> will be run each frame. By default, it is set to <see langword="false"/>.
        /// </summary>
        /// <remarks> NOTE: avoid changing this too frequently, only do so when there's absolutely no work to be done in some time. </remarks>
        public bool ShouldUpdate { 
            get => _shouldUpdate; 
            set {
                if (_shouldUpdate == value)
                    return;

                // update the values
                _shouldUpdate = value;
                if (_shouldUpdate) {
                    Game.UpdateSystem(this);
                }
            }
        }

        /// <summary>
        /// Happens right when all of the subsystems were added.
        /// </summary>
        public virtual void Initialize() { }

        /// <summary>
        /// Happens each frame if <see cref="ShouldUpdate"/> is set.
        /// </summary>
        public virtual void Update(GameTime time) { }

        
    }
}
