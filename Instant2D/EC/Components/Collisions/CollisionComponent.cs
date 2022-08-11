using Instant2D.Collisions;
using Instant2D.Utils;
using Microsoft.Xna.Framework;
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
        /// Since most of colliders are centered, origin must be adjusted to fit that.
        /// </summary>
        protected Vector2 _origin;

        /// <summary>
        /// Indicate whether or not collider size was explicitly provided by user. If <see langword="true"/>, autosizing will take place. <br/>
        /// You should manually this to <see langword="true"/> when user requests changes to the size.
        /// </summary>
        protected bool _wasSizeSet;
        bool _scaleWithTransform = true, _rotateWithTransform = true;

        /// <summary>
        /// Origin of this collider. Defaults to the center of collider.
        /// </summary>
        public Vector2 Origin {
            get => _origin;
            set {
                _origin = value;

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
        /// Calculates movement of the object based on <paramref name="velocity"/>. If it collides into something, <paramref name="hits"/> will be populated with collision data,
        /// as well as <paramref name="velocity"/> will be recalculated to prevent the collision. <br/> 
        /// You'll have to return the <paramref name="hits"/> list back into the pool using <c><paramref name="hits"/>.Pool()</c> and move the entity by <paramref name="velocity"/> yourself.
        /// </summary>
        public bool CalculateMovementCollisions(ref Vector2 velocity, out List<CollisionHit<CollisionComponent>> hits) {
            hits = null;

            // generate bounds updated by the movement
            var bounds = BaseCollider.Bounds;
            bounds.Position += velocity;

            // do a broad sweep to find all the potential collisions
            var nearby = Scene.Collisions.Broadphase(bounds, CollidesWithMask);
            for (var i = 0; i < nearby.Count; i++) {
                var other = nearby[i];

                // check for collision
                if (other != BaseCollider && CollidesWith(other.Entity, velocity, out var hit)) {
                    velocity -= hit.PenetrationVector;

                    // add the hit to hits array
                    hits ??= ListPool<CollisionHit<CollisionComponent>>.Get();
                    hits.Add(hit);
                }
            }

            // return true only if we did something
            return hits != null;
        }

        /// <summary>
        /// Checks if two collision components collide and returns important collision information as <paramref name="hit"/>.
        /// </summary>
        public bool CollidesWith(CollisionComponent other, out CollisionHit<CollisionComponent> hit) => BaseCollider.CheckCollision(other.BaseCollider, out hit);

        /// <summary>
        /// Checks of two collision components could collider with <paramref name="velocity"/> applied to them.
        /// </summary>
        public bool CollidesWith(CollisionComponent other, Vector2 velocity, out CollisionHit<CollisionComponent> hit) {
            var oldPosition = BaseCollider.Position;
            BaseCollider.Position += velocity;

            // offset the BaseCollider and check for collision, then reset
            var result = BaseCollider.CheckCollision(other.BaseCollider, out hit);
            BaseCollider.Position = oldPosition;

            return result;
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
