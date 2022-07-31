using Microsoft.Xna.Framework;
using System;
using System.Text.RegularExpressions;

namespace Instant2D.Collisions {
    /// <summary>
    /// Box collider with a centered origin. When rotated, will behave like a polygon.
    /// </summary>
    public class BoxCollider<T> : BaseCollider<T> {
        /// <summary>
        /// Size of this box.
        /// </summary>
        public Vector2 Size;

        public float Rotation {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        protected override void RecalculateBounds(ref RectangleF bounds) {
            bounds = new RectangleF(Position - Size * 0.5f, Size);
        }

        public override bool CheckOverlap(BaseCollider<T> other) {
            switch (other) {
                default: throw new NotImplementedException();

                case BoxCollider<T> box:
                    return Bounds.Intersects(box.Bounds);

                // https://yal.cc/rectangle-circle-intersection-test/
                case CircleCollider<T> circle:
                    var dX = circle.Position.X - MathF.Max(Bounds.X, MathF.Min(circle.Position.X, Bounds.Right));
                    var dY = circle.Position.Y - MathF.Max(Bounds.Y, MathF.Min(circle.Position.Y, Bounds.Bottom));
                    return (dX * dX + dY * dY) < (circle.Radius * circle.Radius);
            }
        }

        public override bool CheckCollision(BaseCollider<T> other, out CollisionHit<T> hit) {
            hit = new CollisionHit<T> { BaseSelf = this, BaseOther = other };

            switch (other) {
                default: throw new NotImplementedException();

                // unrotated box-to-box collision
                case BoxCollider<T> box: {
                    var (a, b) = (new RectangleF(Position - Size * 0.5f, Size), new RectangleF(other.Position - box.Size * 0.5f, box.Size));

                    // get intersection between two boxes
                    var intersection = a.GetIntersection(b);

                    // no collision...
                    if (intersection == default) {
                        return false;
                    }

                    // calculate penetration vector
                    hit.PenetrationVector = intersection.Width < intersection.Height ?
                        new(a.Center.X < b.Center.X ? intersection.Width : -intersection.Width, 0) :
                        new(0, a.Center.Y < b.Center.Y ? intersection.Height : -intersection.Height);
                    hit.Normal = hit.PenetrationVector.SafeNormalize() * -1;

                    return hit.PenetrationVector != default;
                }

                // unrotated box-to-circle collision
                case CircleCollider<T> circle: {
                    if (circle.CheckCollision(this, out hit)) {
                        Vector2.Negate(ref hit.PenetrationVector, out hit.PenetrationVector);
                        Vector2.Negate(ref hit.Normal, out hit.Normal);
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
