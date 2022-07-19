using Microsoft.Xna.Framework;

namespace Instant2D.Collisions {
    /// <summary>
    /// A structure with linecast hit information.
    /// </summary>
    public record struct LineCastHit<T> {
        /// <summary>
        /// Represents the collider which has been hit by the linecast.
        /// </summary>
        public BaseCollider<T> Self;

        /// <summary>
        /// Origin of the line cast.
        /// </summary>
        public Vector2 Origin;

        /// <summary>
        /// Ending point of the line cast.
        /// </summary>
        public Vector2 End;

        /// <summary>
        /// Distance from the line origin to the point of impact.
        /// </summary>
        public float Distance;

        /// <summary>
		/// The point in world space where the ray hit the collider's surface.
		/// </summary>
		public Vector2 Point;

        /// <summary>
        /// The normal vector of the surface hit by the ray.
        /// </summary>
        public Vector2 Normal;
    }

    


}
