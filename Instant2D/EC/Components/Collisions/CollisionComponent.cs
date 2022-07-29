using Instant2D.Collisions;
using Instant2D.Utils;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D.EC.Components {
    public delegate void CollisionCallback(CollisionHit<CollisionComponent> collision);

    /// <summary>
    /// Base class for all collider components. Contains useful methods for overlap checking and moving with callback support.
    /// </summary>
    public abstract class CollisionComponent : Component {
        /// <summary>
        /// Internal collider used by collision detection system.
        /// </summary>
        public readonly BaseCollider<CollisionComponent> BaseCollider;

        /// <summary>
        /// When <see langword="true"/>, shape of the collider will be modified based on Entity's <see cref="Transform{T}.Scale"/>.
        /// </summary>
        public bool ShouldScaleWithTransform = true;

        /// <summary>
        /// When <see langword="true"/>, shape of the collider will be modified based on Entity's <see cref="Transform{T}.Rotation"/>. <br/>
        /// Could be set to <see langword="false"/> for box colliders to reduce the overhead introduced by rotation calculation.
        /// </summary>
        public bool ShouldRotateWithTransform = true;

        /// <summary>
        /// Constructs a new <see cref="CollisionComponent"/> with specified base collider.
        /// </summary>
        public CollisionComponent(BaseCollider<CollisionComponent> collider) {
            BaseCollider = collider;
            BaseCollider.Entity = this;
        }

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

        /// <summary>
        /// Is fired when two objects collide with each other.
        /// </summary>
        public event CollisionCallback OnCollisionOccured;

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

            BaseCollider._spatialHash = Scene.Collisions;
        }

        public override void OnEnabled() {
            // register collider when this component is enabled
            Scene.Collisions.AddCollider(BaseCollider);
        }

        public override void OnDisabled() { 
            // deregister collider when this component is disabled
            Scene.Collisions.RemoveCollider(BaseCollider);
        }

        /// <summary>
        /// Attempts to move the object by <paramref name="velocity"/> amount. If it collides into something, <paramref name="hit"/> will be populated with collision data,
        /// as well as <paramref name="velocity"/> will be recalculated to prevent the collision. <br/> 
        /// This method won't call <see cref="OnCollisionOccured"/>, so all of the collisions should be handled manually.
        /// </summary>
        public bool TryMove(ref Vector2 velocity, out CollisionHit<CollisionComponent> hit) {
            hit = new();

            // generate bounds updated by the movement
            var bounds = BaseCollider.Bounds;
            bounds.Position += velocity;

            // now move the collider in hopes it wont collide anything
            BaseCollider.Position += velocity;
            BaseCollider.Update();

            // do a broad sweep to find all the potential collisions
            var nearby = Scene.Collisions.Broadphase(bounds, CollidesWith);
            for (var i = 0; i < nearby.Count; i++) {
                var other = nearby[i];

                // check for collision
                if (other != BaseCollider && BaseCollider.CheckCollision(other, out var actualHit)) {
                    velocity -= actualHit.PenetrationVector;

                    // assign the first hit
                    // TODO: introduce a way to return multiple hits?
                    if (hit.Self == null) {
                        hit = actualHit;
                    }
                }
            }

            // now apply the motion to actual entity
            Entity.Transform.Position += velocity;

            return hit.Self != null;
        }
    }
}
