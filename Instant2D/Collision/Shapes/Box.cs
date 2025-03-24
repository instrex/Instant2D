using Instant2D.Utils;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D.Collision.Shapes {
    public class Box : ICollisionShape, IPooledInstance {
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
                if (_size == value)
                    return;

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
                _isPolygonDirty = true;
            }
        }

        /// <summary>
        /// Box rotation. A polygon shape will be created if modified to anything other than 0.
        /// </summary>
        public float Rotation {
            get => _rotation;
            set {
                if (_rotation == value)
                    return;

                _rotation = value;
                _isPolygonDirty = true;
                _boundsDirty = true;
            }
        }

        /// <summary>
        /// Polygon collision shape for when Rotation is modified or collision resolution with other polygon is requested. Will be initalized on-demand.
        /// </summary>
        public Polygon Polygon {
            get {
                _polygon ??= Pool<Polygon>.Shared.Rent();
                if (_isPolygonDirty) {
                    _polygon.SetBoxVertices(_size, _rotation);
                    _polygon.Position = _position;
                }

                return _polygon;
            }
        }
        
        /// <summary>
        /// Check if <see cref="Polygon"/> is <see langword="null"/> or not without instantiating one on-demand.
        /// </summary>
        public bool HasPolygon {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _polygon != null;
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
                return ICollisionShape.AABB.ToAABB(this, box, out normal, out penetrationVector);
            }

            // fallback to polygon when no optimizations could be made
            return Polygon.CollidesWith(other, out normal, out penetrationVector);
        }

        public bool CollidesWithLine(Vector2 start, Vector2 end, out float distance, out Vector2 intersectionPoint, out Vector2 normal) {
            if (_rotation == 0) {
                // use an optimized linecast for AABB colliders
                return ICollisionShape.Line.ToAABB(Bounds.TopLeft, Bounds.BottomRight, start, end, out distance, out intersectionPoint, out normal);
            }

            // fallback to expensive polygon check, which supports rotation and scale
            return Polygon.CollidesWithLine(start, end, out distance, out intersectionPoint, out normal);
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
}
