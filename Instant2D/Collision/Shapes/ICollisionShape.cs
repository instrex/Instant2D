using Microsoft.Xna.Framework;

namespace Instant2D.Collision.Shapes {
    public partial interface ICollisionShape {
        /// <summary>
        /// Position of the shape in world space.
        /// </summary>
        Vector2 Position { get; set; }

        /// <summary>
        /// Bounds occupied by this shape used for broadphasing.
        /// </summary>
        RectangleF Bounds { get; }

        /// <summary>
        /// Check if the shape overlaps another. 
        /// </summary>
        bool CheckOverlap(ICollisionShape other);

        /// <summary>
        /// Checks if the shape collides another. <paramref name="penetrationVector"/> represents the amount of motion that needs to be done to make the shapes not collide.
        /// </summary>
        bool CollidesWith(ICollisionShape other, out Vector2 normal, out Vector2 penetrationVector);

        /// <summary>
        /// Checks if shape is intersected by a line.
        /// </summary>
        bool CollidesWithLine(Vector2 start, Vector2 end, out float fraction, out float distance, out Vector2 intersectionPoint, out Vector2 normal);

        /// <summary>
        /// Checks if the shape contains a single point.
        /// </summary>
        bool ContainsPoint(Vector2 point);
    }
}
