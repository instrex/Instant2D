using Microsoft.Xna.Framework;

namespace Instant2D.Collisions {
    /// <summary>
    /// Helper struct for calculating collisions between polygons using Separating Axis Theorem algorithm. <br/>
    /// May also be used outside of collision detection system.
    /// </summary>
    public record struct Polygon {
        /// <summary>
        /// Centered vertices of the polygon in local space.
        /// </summary>
        public Vector2[] Vertices;

        /// <summary>
        /// Cached edge normals.
        /// </summary>
        public Vector2[] Normals {
            get {
                if (_normalsDirty) {
                    _normalsDirty = false;
                }

                return _edgeNormals;
            }
        }

        /// <summary>
        /// Average position of all vertices.
        /// </summary>
        public Vector2 Center {
            get {
                var sum = Vector2.Zero;
                for (var i = 0; i < Vertices.Length; i++) {
                    sum += Vertices[i];
                }

                return sum / Vertices.Length;
            }
        }

        Vector2[] _rawVertices;
        Vector2[] _edgeNormals;
        bool _normalsDirty;

        public void Transform() {

        }

        public void CenterVertices() {
            var center = Center;
            for (var i = 0; i < Vertices.Length; i++) {
                Vertices[i] -= center;
            }
        }

        void RebuildNormals() {

        }
    }
}
