using Microsoft.Xna.Framework;
using System;
using System.Runtime.CompilerServices;
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
                    var diff = MinkowskiDifference(this, box);
                    if (diff.Contains(Vector2.Zero)) {
                        hit.PenetrationVector = GetClosestPointOnBoundsToOrigin(diff, Vector2.Zero);

                        // no penetration = no collision
                        if (hit.PenetrationVector == Vector2.Zero)
                            return false;

                        hit.Normal = hit.PenetrationVector * -1;
                        hit.Normal.Normalize();

                        return true;
                    }

                    return false;
                }

                // unrotated box-to-circle collision
                case CircleCollider<T> circle: {
                    if (circle.CheckCollision(this, out hit)) {
                        Vector2.Negate(ref hit.PenetrationVector, out hit.PenetrationVector);
                        Vector2.Negate(ref hit.Normal, out hit.Normal);
                        return true;
                    }

                    return false;
                }
            }
        }

        public override bool CheckLineCast(Vector2 start, Vector2 end, out LineCastHit<T> hit) {


            throw new NotImplementedException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static RectangleF MinkowskiDifference(BoxCollider<T> first, BoxCollider<T> second) {
            // we need the top-left of our first box but it must include our motion. Collider only modifies position with the motion so we
            // need to figure out what the motion was using just the position.
            var positionOffset = first.Position - (first.Bounds.Position + first.Bounds.Size / 2f);
            var topLeft = first.Bounds.Position + positionOffset - second.Bounds.BottomRight;
            var fullSize = first.Bounds.Size + second.Bounds.Size;

            return new RectangleF(topLeft.X, topLeft.Y, fullSize.X, fullSize.Y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Vector2 GetClosestPointOnBoundsToOrigin(RectangleF rect, Vector2 point) {
            var minDist = Math.Abs(point.X - rect.X);
            var boundsPoint = new Vector2(rect.X, point.Y);

            if (Math.Abs(rect.Right - point.X) < minDist) {
                minDist = Math.Abs(rect.Right);
                boundsPoint.X = rect.Right;
                boundsPoint.Y = 0f;
            }

            if (Math.Abs(rect.Bottom - point.Y) < minDist) {
                minDist = Math.Abs(rect.Bottom);
                boundsPoint.X = 0f;
                boundsPoint.Y = rect.Bottom;
            }

            if (Math.Abs(rect.Y - point.Y) < minDist) {
                minDist = Math.Abs(rect.Position.Y);
                boundsPoint.X = 0;
                boundsPoint.Y = rect.Y;
            }

            return boundsPoint;

            //var max = rect.BottomRight;
            //var minDist = Math.Abs(rect.Position.X);
            //var boundsPoint = new Vector2(rect.Position.X, 0);

            //if (Math.Abs(max.X) < minDist) {
            //    minDist = Math.Abs(max.X);
            //    boundsPoint.X = max.X;
            //    boundsPoint.Y = 0f;
            //}

            //if (Math.Abs(max.Y) < minDist) {
            //    minDist = Math.Abs(max.Y);
            //    boundsPoint.X = 0f;
            //    boundsPoint.Y = max.Y;
            //}

            //if (Math.Abs(rect.Y) < minDist) {
            //    minDist = Math.Abs(rect.Position.Y);
            //    boundsPoint.X = 0;
            //    boundsPoint.Y = rect.Y;
            //}

            //return boundsPoint;
        }


    }
}
