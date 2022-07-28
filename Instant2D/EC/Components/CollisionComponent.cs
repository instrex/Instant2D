using Instant2D.Collisions;
using Instant2D.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D.EC.Components {
    /// <summary>
    /// Base class for all collider components. Contains useful methods for overlap checking and moving with callback support.
    /// </summary>
    public abstract class CollisionComponent : Component {
        /// <summary>
        /// Internal collider used by collision detection system.
        /// </summary>
        public readonly BaseCollider<CollisionComponent> BaseCollider;

        /// <summary>
        /// Bitmask used to determine if object of different layers collide with each other.
        /// </summary>
        public int CollidesWith {
            get => BaseCollider.CollidesWith;
            set => BaseCollider.CollidesWith = value;
        }

        /// <summary>
        /// Flag used to determine this object's collision layer, meaning it can be shifted/unshifted to the <see cref="CollidesWith"/>.
        /// </summary>
        public int CollisionLayer {
            get => BaseCollider.CollisionLayer;
            set => BaseCollider.CollisionLayer = value;
        }

        public override void OnTransformUpdated(TransformComponentType components) {
            base.OnTransformUpdated(components);
        }

        public override void Initialize() {
            // check BaseCollider
            if (BaseCollider is null) {
                Logger.WriteLine($"BaseCollider of {GetType().Name} wasn't initialized, disabling.", Logger.Severity.Error);
                Entity.RemoveComponent(this);
                return;
            }

            // initialize the collision manager and notify the user
            // in case they would want to tweak the chunk size
            if (Scene.Collisions is null) {
                Logger.WriteLine($"Collider added to a scene without Collisions intitialized, defaulting to SpatialHash with grid size of {SpatialHash<CollisionComponent>.DEFAULT_CHUNK_SIZE}.");
                Scene.Collisions = new();
            }
        }

        public override void OnEnabled() {
            // register collider when this component is enabled
            Scene.Collisions.AddCollider(BaseCollider);
        }

        public override void OnDisabled() { 
            // deregister collider when this component is disabled
            Scene.Collisions.RemoveCollider(BaseCollider);
        }

    }
}
