using Instant2D.Utils;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D.Collision.Shapes {
    public class Box : ICollisionShape, IPooled {
        Vector2 _size, _position;
        float _rotation;

        RectangleF _bounds;
        bool _isPolygonDirty, _boundsDirty;
        Polygon _polygon;

        /// <summary>
        /// Size of the box.
        /// </summary>
        public Vector2 Size {
            get => _size;
            set {
                _size = value;
                _isPolygonDirty = true;
                _boundsDirty = true;
            }
        }

        /// <summary>
        /// Position in world space.
        /// </summary>
        public Vector2 Position {
            get => _position;
            set {
                _position = value;
                _boundsDirty = true;
            }
        }

        /// <summary>
        /// Rotation of this box. As an optimization, this value may be kept at 0 where applicable to avoid extra rotation overhead.
        /// </summary>
        public float Rotation {
            get => _rotation;
            set {
                _rotation = value;
                _isPolygonDirty = true;
                _boundsDirty = true;
            }
        }

        /// <summary>
        /// Internal collision shape used for linecasts and rotated collision detection. Created lazily only when you use those features.
        /// </summary>
        public Polygon Polygon {
            get {
                _polygon ??= StaticPool<Polygon>.Get();
                if (_isPolygonDirty) {
                    _polygon.SetBoxVertices(_size, _rotation);
                }

                return _polygon;
            }
        }

        #region ICollisionShape implementation

        public RectangleF Bounds {
            get {
                if (_boundsDirty) {
                    if (_polygon == null) {
                        _bounds.Position = _position - _size * 0.5f;
                        _bounds.Size = _size;
                        return _bounds;
                    }

                    _bounds = Polygon.Bounds;
                }

                return _bounds;
            }
        }

        public bool CheckOverlap(ICollisionShape other) {
            if (_rotation == 0f) {
                switch (other) {
                    // two unrotated boxes may use simple bounds check
                    case Box box when box._rotation == 0f:
                        return Bounds.Intersects(box.Bounds);

                    // TODO: box to circle overlap check
                }
            }

            // fallback to polygon when no optimizations could be made
            return Polygon.CheckOverlap(other);
        }

        public bool CollidesWith(ICollisionShape other, out Vector2 normal, out Vector2 penetrationVector) {
            if (_rotation == 0 && other is Box box && box._rotation == 0f) {
                return ICollisionShape.BoxToBox(this, box, out normal, out penetrationVector);
            }

            // fallback to polygon when no optimizations could be made
            return Polygon.CollidesWith(other, out normal, out penetrationVector);
        }

        public bool CollidesWithLine(Vector2 start, Vector2 end, out float fraction, out float distance, out Vector2 intersectionPoint, out Vector2 normal) {
            if (_rotation == 0) {
                // TODO: add an optimized linecast version?
            }

            return Polygon.CollidesWithLine(start, end, out fraction, out distance, out intersectionPoint, out normal);
        }
            

        public bool ContainsPoint(Vector2 point) {
            if (_rotation == 0) {
                return Bounds.Contains(point);
            }

            return Polygon.ContainsPoint(point);
        }

        #endregion

        public void Reset() {
            _boundsDirty = _isPolygonDirty = true;
            _size = _position = default;
            _rotation = 0;

            _polygon?.Reset();
            _polygon = null;
        }
    }

    public partial interface ICollisionShape {
        public static bool BoxToBox(Box a, Box b, out Vector2 normal, out Vector2 penetrationVector) {
            var diff = MinkowskiDifference(a, b);
            penetrationVector = default;
            normal = default;

            if (diff.Contains(Vector2.Zero)) {
                penetrationVector = GetClosestPointOnBoundsToOrigin(diff, Vector2.Zero);

                if (penetrationVector == Vector2.Zero) {
                    return false;
                }

                normal = penetrationVector * -1;
                normal.Normalize();

                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static RectangleF MinkowskiDifference(Box first, Box second) {
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
        }
    }
}
