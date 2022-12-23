using Instant2D.Utils;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Instant2D.Collision.Shapes {
    public class Polygon : ICollisionShape, IPooled {
        internal Vector2[] _vertices, _edgeNormals;
        bool _edgeNormalsDirty = true, _boundsDirty = true;
        RectangleF _bounds;
        Vector2 _position;

        // if this is true, small optimizations may be performed
        internal bool _isBox;

        public Polygon() : this(Array.Empty<Vector2>()) { }

        /// <summary>
        /// Construct a new polygon from provided <paramref name="vertices"/>. Vertices should be in clockwise order and centered around {0, 0}.
        /// </summary>
        public Polygon(params Vector2[] vertices) {
            _vertices = vertices;
        }
        
        /// <summary>
        /// Construct a new polygon in the shape of a box. 
        /// </summary>
        public Polygon(float boxWidth, float boxHeight) {
            _vertices = new Vector2[4];
            SetBoxVertices(new(boxWidth, boxHeight));
        }

        /// <summary>
        /// Position of the polygon in world space.
        /// </summary>
        public Vector2 Position {
            get => _position;
            set {
                _position = value;
                _boundsDirty = true;
            }
        }

        /// <summary>
        /// Bounds that this shape occupies.
        /// </summary>
        public RectangleF Bounds {
            get {
                if (_boundsDirty) {
                    CalculateBounds();
                }

                return _bounds;
            }
        }

        /// <summary>
        /// Vertices of this polygon. Do not modify directly, use indexers.
        /// </summary>
        public Vector2[] Vertices {
            get => _vertices;
        }

        /// <summary>
        /// Automatically generated edge normals used for collision detection algorithm.
        /// </summary>
        public Vector2[] EdgeNormals {
            get {
                if (_edgeNormalsDirty) {
                    CalculateEdgeNormals();
                }

                return _edgeNormals;
            }
        }

        /// <summary>
        /// Set the vertex array of this polygon. Vertices must be centered around the point {0, 0} in clockwise order. <br/>
        /// Note that this method undoes any box optimizations.
        /// </summary>
        public void SetVertices(Vector2[] vertices) {
            _vertices = vertices;
            _edgeNormalsDirty = true;
            _boundsDirty = true;
            _isBox = false;
        }

        /// <summary>
        /// Sets the vertices to those of a box.
        /// </summary>
        public void SetBoxVertices(Vector2 boxSize, float rotation = 0f) {
            Array.Resize(ref _vertices, 4);

            // initialize the points to the size of the box
            _vertices[0] = new Vector2(boxSize.X * 0.5f, boxSize.Y * -0.5f);
            _vertices[1] = new Vector2(boxSize.X * 0.5f, boxSize.Y * 0.5f);
            _vertices[2] = new Vector2(boxSize.X * -0.5f, boxSize.Y * 0.5f);
            _vertices[3] = new Vector2(boxSize.X * -0.5f, boxSize.Y * -0.5f);

            // rotate verts immediately
            if (rotation != 0) {
                Rotate(_vertices, rotation);
            }

            // update stuff
            _edgeNormalsDirty = true;
            _boundsDirty = true;
            _isBox = true;
        }

        /// <summary>
        /// Get or set any specific vertex. Using the setter undoes box optimizations.
        /// </summary>
        public Vector2 this[int index] {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _vertices[index];

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set {
                _vertices[index] = value;
                _edgeNormalsDirty = true;
                _boundsDirty = true;
                _isBox = false;
            }
        }

        #region Transformation methods & helpers 

        public void Scale(Vector2 scale) {
            Scale(_vertices, scale);
            _edgeNormalsDirty = true;
            _boundsDirty = true;
        }

        public void Rotate(float rotation, Vector2 center = default) {
            Rotate(_vertices, rotation, center);
            _edgeNormalsDirty = true;
            _boundsDirty = true;
        }

        public static void Scale(Vector2[] vertices, Vector2 scale) {
            for (var i = 0; i < vertices.Length; i++) {
                vertices[i] *= scale;
            }
        }

        public static void Rotate(Vector2[] vertices, float rotation, Vector2 center = default) {
            for (var i = 0; i < vertices.Length; i++) {
                vertices[i] = vertices[i].RotatedBy(rotation, center);
            }
        }

        /// <summary>
        /// Get closest point to <paramref name="position"/> on polygon's edge.
        /// </summary>
        public static Vector2 GetClosestPoint(Vector2[] vertices, Vector2 position, out float distanceSq, out Vector2 edgeNormal) {
            Vector2 result = default;
            distanceSq = float.MaxValue;
            edgeNormal = default;

            for (var i = 0; i < vertices.Length; i++) {
                var a = vertices[i];
                var b = vertices[i + 1 == vertices.Length ? 0 : i + 1];

                var closestLine = VectorUtils.GetClosestPointOnLine(a, b, position);
                Vector2.DistanceSquared(ref position, ref closestLine, out var dist);

                if (dist < distanceSq) {
                    distanceSq = dist;
                    result = closestLine;

                    var line = b - a;
                    edgeNormal = new(-line.Y, line.X);
                }
            }

            return result;
        }

        #endregion

        #region ICollisionShape implementation

        public bool CheckOverlap(ICollisionShape other) {
            if (other is Polygon otherPolygon) {
                return ICollisionShape.PolygonToPolygon(this, otherPolygon, out _, out _);
            }

            throw new NotImplementedException();
        }

        public bool CollidesWith(ICollisionShape other, out Vector2 normal, out Vector2 penetrationVector) {
            return other switch {
                Polygon polygon => ICollisionShape.PolygonToPolygon(this, polygon, out penetrationVector, out normal),
                Box box => ICollisionShape.PolygonToPolygon(this, box.Polygon, out penetrationVector, out normal),
                _ => throw new NotImplementedException()
            };
        }

        public bool CollidesWithLine(Vector2 start, Vector2 end, out float fraction, out float distance, out Vector2 intersectionPoint, out Vector2 normal) =>
            ICollisionShape.LineToPolygon(start, end, this, out fraction, out distance, out intersectionPoint, out normal);

        public bool ContainsPoint(Vector2 point) {
            point -= _position;

            var isInside = false;
            for (int i = 0, j = _vertices.Length - 1; i < _vertices.Length; j = i++) {
                if (((_vertices[i].Y > point.Y) != (_vertices[j].Y > point.Y)) &&
                    (point.X < (_vertices[j].X - _vertices[i].X) * (point.Y - _vertices[i].Y) / (_vertices[j].Y - _vertices[i].Y) +
                     _vertices[i].X)) {
                    isInside = !isInside;
                }
            }

            return isInside;
        }

        #endregion

        void CalculateBounds() {
            var minX = float.MaxValue;
            var maxX = float.MinValue;
            var minY = float.MaxValue;
            var maxY = float.MinValue;

            for (var i = 0; i < _vertices.Length; i++) {
                if (_vertices[i].X < minX) minX = _vertices[i].X;
                if (_vertices[i].X > maxX) maxX = _vertices[i].X;
                if (_vertices[i].Y < minY) minY = _vertices[i].Y;
                if (_vertices[i].Y > maxY) maxY = _vertices[i].Y;
            }

            _bounds.X = _position.X + minX;
            _bounds.Y = _position.Y + minY;
            _bounds.Width = maxX - minX;
            _bounds.Height = maxY - minY;

            _boundsDirty = false;
        }

        void CalculateEdgeNormals() {
            var edges = _isBox ? 2 : _vertices.Length;
            Array.Resize(ref _edgeNormals, edges);

            for (var i = 0; i < edges; i++) {
                var p1 = Vertices[i];
                var p2 = Vertices[i + 1 >= _vertices.Length ? 0 : i + 1];
                _edgeNormals[i] = new Vector2(-1f * (p2.Y - p1.Y), p2.X - p1.X).SafeNormalize();
            }

            _edgeNormalsDirty = false;
        }

        public void Reset() {
            _boundsDirty = _edgeNormalsDirty = true;
            _vertices = _edgeNormals = null;
            _position = Vector2.Zero;
            _isBox = false;
        }
    }

    // Polygon collision implementations
    public partial interface ICollisionShape {
        public static bool PolygonToPolygon(Polygon a, Polygon b, out Vector2 penetrationVector, out Vector2 normal) {
            penetrationVector = Vector2.Zero;
            normal = Vector2.Zero;

            var firstEdges = a.EdgeNormals;
            var secondEdges = b.EdgeNormals;
            var minIntervalDistance = float.PositiveInfinity;
            var translationAxis = new Vector2();
            var polygonOffset = a.Position - b.Position;

            for (var edgeIndex = 0; edgeIndex < firstEdges.Length + secondEdges.Length; edgeIndex++) {
                // 1. Find if the polygons are currently intersecting
                // Polygons have the normalized axis perpendicular to the current edge cached for us
                var axis = edgeIndex < firstEdges.Length ? firstEdges[edgeIndex] : secondEdges[edgeIndex - firstEdges.Length];

                // Find the projection of the polygon on the current axis
                float minA = 0, minB = 0, maxA = 0, maxB = 0;
                GetInterval(axis, a, ref minA, ref maxA);
                GetInterval(axis, b, ref minB, ref maxB);

                // get our interval to be space of the second Polygon. Offset by the difference in position projected on the axis.
                Vector2.Dot(ref polygonOffset, ref axis, out float relativeIntervalOffset);
                minA += relativeIntervalOffset;
                maxA += relativeIntervalOffset;

                // check if the polygon projections are currentlty intersecting
                float intervalDist = IntervalDistance(minA, maxA, minB, maxB);

                // If the polygons are not intersecting and won't intersect, exit the loop
                if (intervalDist > 0) {
                    return false;
                }

                // Check if the current interval distance is the minimum one. If so store the interval distance and the current distance.
                // This will be used to calculate the minimum translation vector
                intervalDist = Math.Abs(intervalDist);
                if (intervalDist < minIntervalDistance) {
                    minIntervalDistance = intervalDist;
                    translationAxis = axis;

                    if (Vector2.Dot(translationAxis, polygonOffset) < 0)
                        translationAxis = -translationAxis;
                }
            }

            penetrationVector = -translationAxis * minIntervalDistance;
            normal = translationAxis;

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float IntervalDistance(float minA, float maxA, float minB, float maxB) {
            if (minA < minB)
                return minB - maxA;

            return minA - maxB;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void GetInterval(Vector2 axis, Polygon polygon, ref float min, ref float max) {
            // To project a point on an axis use the dot product
            float dot;
            Vector2.Dot(ref polygon._vertices[0], ref axis, out dot);
            min = max = dot;

            for (var i = 1; i < polygon._vertices.Length; i++) {
                Vector2.Dot(ref polygon._vertices[i], ref axis, out dot);
                if (dot < min)
                    min = dot;
                else if (dot > max)
                    max = dot;
            }
        }
    }
}