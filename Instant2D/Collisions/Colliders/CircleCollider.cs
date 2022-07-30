using Microsoft.Xna.Framework;
using System;

namespace Instant2D.Collisions {
    /// <summary>
    /// Circle collider with a centered origin.
    /// </summary>
    public class CircleCollider<T> : BaseCollider<T> {
        /// <summary>
        /// Radius of the circle.
        /// </summary>
        public float Radius;

        protected override void RecalculateBounds(ref RectangleF bounds) {
            bounds = new RectangleF(Position - new Vector2(Radius), new Vector2(Radius * 2));
        }

        public override bool CheckOverlap(BaseCollider<T> other) {
            switch (other) {
                default: throw new NotImplementedException();

                case BoxCollider<T> box:
                    return box.CheckOverlap(this);

                case CircleCollider<T> circle:
                    return Vector2.DistanceSquared(Position, circle.Position) < (Radius + circle.Radius) * (Radius + circle.Radius);
            }
        }

        public override bool CheckCollision(BaseCollider<T> other, out CollisionHit<T> hit) {
            hit = new CollisionHit<T> { Self = this, Other = other };

            // if shapes don't overlap, there should be collision
            if (!CheckOverlap(other))
                return false;

            switch (other) {
                default: throw new NotImplementedException();

                // circle-to-circle collision
                case CircleCollider<T> circle: {
                    var distSq = Vector2.DistanceSquared(Position, circle.Position);
                    var radiusSum = Radius + circle.Radius;

                    // check if circles even collide
                    if (distSq < radiusSum * radiusSum) {
                        hit.Normal = VectorUtils.SafeNormalize(Position - circle.Position);
                        hit.PenetrationVector = hit.Normal * (radiusSum - MathF.Sqrt(distSq)) * -1;
                        hit.Point = circle.Position + hit.Normal * circle.Radius;

                        return true;
                    }

                    return false;
                }

                // circle-to-box collision
                case BoxCollider<T> box: {
                    var closestPoint = box.Bounds.GetClosestPoint(Position, out hit.Normal);

                    // the circle is contained inside the box, easy win
                    if (box.Bounds.Contains(closestPoint)) {
                        hit.PenetrationVector = Position - (closestPoint + hit.Normal * Radius);
                        hit.Point = closestPoint;

                        return true;
                    }

                    var dist = Vector2.DistanceSquared(closestPoint, Position);

                    if (dist == 0) {
                        hit.PenetrationVector = hit.Normal * Radius;
                    } 
                    
                    else if (dist <= Radius * Radius) {
                        hit.Point = closestPoint;
                        hit.Normal = Position - closestPoint;

                        // calculate penetraition
                        var penetration = hit.Normal.Length() - Radius;
                        hit.Normal = VectorUtils.SafeNormalize(hit.Normal);
                        hit.PenetrationVector = penetration * hit.Normal;

                        return true;
                    }

                    return false;
                }
            }
        }

        public override bool CheckLineCast(Vector2 start, Vector2 end, out LineCastHit<T> hit) {
            throw new NotImplementedException();
        }


    }
}
