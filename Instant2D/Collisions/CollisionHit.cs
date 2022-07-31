using Microsoft.Xna.Framework;

namespace Instant2D.Collisions {
    /// <summary>
    /// A structure with collision information.
    /// </summary>
    public record struct CollisionHit<T> {
        /// <summary>
        /// Represents the collider which has been hit by <see cref="BaseOther"/>.
        /// </summary>
        public BaseCollider<T> BaseSelf;

        /// <summary>
        /// Represents the collider which hit <see cref="BaseSelf"/>.
        /// </summary>
        public BaseCollider<T> BaseOther;

        /// <summary>
        /// Shortcut to <see cref="BaseSelf"/>'s entity.
        /// </summary>
        public T Self => BaseSelf.Entity;

        /// <summary>
        /// Shortcut to <see cref="BaseOther"/>'s entity.
        /// </summary>
        public T Other => BaseOther.Entity;

        /// <summary>
        /// Normal vector of the collision surface.
        /// </summary>
        public Vector2 Normal;

        /// <summary>
        /// Minimal movement required to push colliders apart.
        /// </summary>
        public Vector2 PenetrationVector;

        /// <summary>
        /// The point of collision, may be <see langword="null"/> in some cases.
        /// </summary>
        public Vector2? Point;
    }

    


}
