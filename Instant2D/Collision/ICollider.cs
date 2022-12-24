using Instant2D.Collision.Shapes;
using Microsoft.Xna.Framework;
using System;

namespace Instant2D.Collision {
    public record struct CollisionResult<T>(T Self, T Other, Vector2 Normal, Vector2 PenetrationVector) where T: ICollider<T>;

    public record struct LineCastResult<T>(T Self, Vector2 LineOrigin, Vector2 LineEnd, float Distance, Vector2 Point, Vector2 Normal) : IComparable<LineCastResult<T>> where T : ICollider<T> {
        // sort by distance in default cases
        public int CompareTo(LineCastResult<T> other) => Distance.CompareTo(other.Distance);
    }

    /// <summary>
    /// Generic collider interface used in conjuction with <see cref="SpatialHash"/>.
    /// </summary>
    public interface ICollider<TSelf> where TSelf: ICollider<TSelf> {
        /// <summary>
        /// Collision shape used by this collider.
        /// </summary>
        ICollisionShape Shape { get; }

        /// <summary>
        /// Rectange to which this collider is registered in the spatial hash.
        /// </summary>
        Rectangle SpatialHashRegion { get; internal set; }

        /// <summary>
        /// Layer mask of this collider.
        /// </summary>
        int LayerMask { get; }

        /// <summary>
        /// Mask of other colliders which this one should not ignore.
        /// </summary>
        int CollidesWithMask { get; }

        /// <summary>
        /// Whether or not this collider collides with another.
        /// </summary>
        bool CollidesWith(TSelf other, out CollisionResult<TSelf> result);

        /// <summary>
        /// Whether or not this collider collides with a line. Used for line casts.
        /// </summary>
        bool CollidesWithLine(Vector2 start, Vector2 end, out LineCastResult<TSelf> result);
    }
}
