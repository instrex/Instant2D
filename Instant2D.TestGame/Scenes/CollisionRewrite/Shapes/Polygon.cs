using Instant2D.TestGame.Scenes.CollisionRewrite.Shapes;
using Instant2D.Utils;
using Microsoft.Xna.Framework;
using System.Runtime.CompilerServices;

namespace Instant2D.TestGame.Scenes.CollisionRewrite.Shapes {
    public class Polygon : ICollisionShape, IPooled {
        IReadOnlyList<Vector2> _readOnlyVertices;
        internal Vector2[] _vertices, _edgeNormals;
        bool _edgeNormalsDirty = true, _boundsDirty = true;
        RectangleF _bounds;
        Vector2 _position;

        // if this is true, small optimizations may be performed
        internal bool _isBox;

        public Polygon(params Vector2[] vertices) {
            _vertices = vertices;
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
        /// Read-only access to vertices of this polygon.
        /// </summary>
        public IReadOnlyList<Vector2> Vertices {
            get {
                _readOnlyVertices ??= Array.AsReadOnly(_vertices);
                return _readOnlyVertices; 
            }
        }

        public Vector2[] EdgeNormals {
            get {
                if (_edgeNormalsDirty) {
                    CalculateEdgeNormals();
                }

                return _edgeNormals;
            }
        }

        public void SetVertices(Vector2[] vertices) {
            _readOnlyVertices = null;
            _vertices = vertices;
            _edgeNormalsDirty = true;
            _boundsDirty = true;
        }

        public Vector2 this[int index] {
            get => _vertices[index];
            set {
                _vertices[index] = value;
                _edgeNormalsDirty = true;
                _boundsDirty = true;
            }
        }

        #region ICollisionShape implementation

        public bool CheckOverlap(ICollisionShape other) {
            if (other is Polygon otherPolygon) {
                return ICollisionShape.PolygonToPolygon(this, otherPolygon, out _, out _);
            }

            throw new NotImplementedException();
        }

        public bool CollidesWith(ICollisionShape other, out Vector2 normal, out Vector2 penetrationVector) {
            if (other is Polygon otherPolygon) {
                return ICollisionShape.PolygonToPolygon(this, otherPolygon, out normal, out penetrationVector);
            }

            throw new NotImplementedException();
        }

        public bool CollidesWithLine(Vector2 start, Vector2 end, out LineCollisionResult result) {
            throw new NotImplementedException();
        }

        public bool ContainsPoint(Vector2 point) {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }
    }
    
}

namespace Instant2D.TestGame.Scenes {
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
