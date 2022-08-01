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
        /// Since most of colliders are centered, origin must be adjusted to fit that.
        /// </summary>
        protected Vector2 _origin;
        Vector2 _rawOrigin = new(0.5f);

        /// <summary>
        /// Indicate whether or not collider size was explicitly provided by user. If <see langword="true"/>, autosizing will take place. <br/>
        /// You should manually this to <see langword="true"/> when user requests changes to the size.
        /// </summary>
        protected bool _wasSizeSet;
        bool _scaleWithTransform = true, _rotateWithTransform = true;

        /// <summary>
        /// Origin of this collider. Defaults to {0.5, 0.5}.
        /// </summary>
        public Vector2 Origin {
            get => _rawOrigin;
            set {
                _origin = value - new Vector2(0.5f);
                _rawOrigin = value;

                // update internal values
                UpdateCollider();
            }
        }

        /// <summary>
        /// When <see langword="true"/>, shape of the collider will be modified based on Entity's <see cref="Transform{T}.Scale"/>.
        /// </summary>
        public bool ShouldScaleWithTransform {
            get => _scaleWithTransform;
            set {
                if (_scaleWithTransform == value)
                    return;

                _scaleWithTransform = value;
                UpdateCollider();
            }
        }

        /// <summary>
        /// When <see langword="true"/>, shape of the collider will be modified based on Entity's <see cref="Transform{T}.Rotation"/>. <br/>
        /// Could be set to <see langword="false"/> for box colliders to reduce the overhead introduced by rotation calculation.
        /// </summary>
        public bool ShouldRotateWithTransform {
            get => _rotateWithTransform;
            set {
                if (_rotateWithTransform == value)
                    return;

                _rotateWithTransform = value;
                UpdateCollider();
            }
        }

        /// <summary>
        /// Constructs a new <see cref="CollisionComponent"/> with specified base collider.
        /// </summary>
        public CollisionComponent(BaseCollider<CollisionComponent> collider) {
            BaseCollider = collider;
            BaseCollider.Entity = this;
        }

        /// <summary>
        /// Layer mask used to determine if object of different layers collide with each other.
        /// </summary>
        public int CollidesWithMask {
            get => BaseCollider.CollidesWithMask;
            set => BaseCollider.CollidesWithMask = value;
        }

        /// <summary>
        /// Layer mask used to determine this object's collision layer, meaning it can be shifted/unshifted to the <see cref="CollidesWithMask"/>. <br/>
        /// This could contain more than 1 flag for better flexibility. Use <see cref="IntFlags"/> extensions for more convenience when working with bit operations.
        /// </summary>
        public int LayerMask {
            get => BaseCollider.LayerMask;
            set => BaseCollider.LayerMask = value;
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

        public override void OnRemovedFromEntity() {
            Scene.Collisions.RemoveCollider(BaseCollider);
        }

        public override void PostInitialize() {
            if (!_wasSizeSet) {
                // attempt to automatically determine size
                // using RenderableComponents
                this.AutoResize();
            }
        }

        public override void OnEnabled() {
            // register collider when this component is enabled
            UpdateCollider();
        }

        public override void OnDisabled() { 
            // deregister collider when this component is disabled
            Scene.Collisions.RemoveCollider(BaseCollider);
        }

        /// <summary>
        /// Apply all of the settings and update the base collider.
        /// </summary>
        public virtual void UpdateCollider() { }

        /// <summary>
        /// Automatically assign size values using <see cref="RenderableComponent.Bounds"/> as reference.
        /// </summary>
        public virtual void AutoResize(RectangleF bounds) { }

        /// <summary>
        /// Attempts to move the object by <paramref name="velocity"/> amount. If it collides into something, <paramref name="hit"/> will be populated with collision data,
        /// as well as <paramref name="velocity"/> will be recalculated to prevent the collision. <br/> 
        /// This method won't call <see cref="OnCollisionOccured"/>, so all of the collisions should be handled manually.
        /// </summary>
        public bool TryMove(Vector2 velocity, out CollisionHit<CollisionComponent> hit) {
            hit = new();

            // generate bounds updated by the movement
            var bounds = BaseCollider.Bounds;
            bounds.Position += velocity;

            // now move the collider in hopes it wont collide with anything
            var oldPos = BaseCollider.Position;
            BaseCollider.Position += velocity;

            // do a broad sweep to find all the potential collisions
            var nearby = Scene.Collisions.Broadphase(bounds, CollidesWithMask);
            for (var i = 0; i < nearby.Count; i++) {
                var other = nearby[i];

                // check for collision
                if (other != BaseCollider && BaseCollider.CheckCollision(other, out var actualHit)) {
                    velocity -= actualHit.PenetrationVector;

                    // move the collider to prevent jittering
                    BaseCollider.Position = oldPos + velocity;

                    // assign the first hit
                    // TODO: introduce a way to return multiple hits?
                    if (hit.BaseSelf == null) {
                        hit = actualHit;
                    }
                }
            }

            // now apply the motion to actual entity
            Entity.Transform.Position += velocity;

            return hit.BaseSelf != null;
        }

        /// <summary>
        /// Checks if this collider overlaps another Entity. <see cref="Entity.GetComponents{T}"/> is used to find all of the colliders. <br/>
        /// If <paramref name="preciseCheck"/> is set to <see langword="false"/>, only the bounds check will take place.
        /// </summary>
        public bool Overlaps(Entity other, bool preciseCheck = true) {
            foreach (var collider in other.GetComponents<CollisionComponent>()) {
                // break early if overlap is found
                if (Overlaps(collider, preciseCheck)) {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if this collider overlaps <paramref name="other"/>. If <paramref name="preciseCheck"/> is set to <see langword="false"/>, only the bounds check will take place.
        /// </summary>
        public bool Overlaps(CollisionComponent other, bool preciseCheck = true) {
            if (!preciseCheck) {
                // simply check bounds for intersections, that's it
                return BaseCollider.Bounds.Contains(other.BaseCollider.Bounds);
            }

            // go for more precise approach and call stuff
            return BaseCollider.CheckOverlap(other.BaseCollider);
        }
    }
}
