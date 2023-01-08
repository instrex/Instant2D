using Instant2D.Collision.Shapes;
using Instant2D.Graphics;
using Microsoft.Xna.Framework;
using System;
using System.Linq;

namespace Instant2D.EC.Components.Collisions {
    /// <summary>
    /// Polygon-shaped collider using array of Vector2[] as vertices of the polygon. Supports rotation, scale and repositioning.
    /// </summary>
    public class PolygonCollider : CollisionComponent {
        readonly Polygon _polygonShape;
        Vector2[] _originalVertices, _transformedVertices;

        public PolygonCollider() => Shape = _polygonShape = new();
        public PolygonCollider(Vector2[] vertices) {
            Shape = _polygonShape = new Polygon(vertices);
            _originalVertices = vertices;
        }

        /// <summary>
        /// Read-only access to polygon vertices. Do not modify directly.
        /// </summary>
        public Vector2[] Vertices => _polygonShape.Vertices;

        public Vector2[] TransformedVertices => _transformedVertices ?? Vertices;

        /// <summary>
        /// Initializes empty vertices array.
        /// </summary>
        public PolygonCollider SetVertices(int count) => SetVertices(new Vector2[count]);

        /// <summary>
        /// Set vertices and store original, untransformed points in the array.
        /// </summary>
        public PolygonCollider SetVertices(params Vector2[] vertices) {
            // copy the vertices here 
            _originalVertices = vertices.ToArray();
            UpdateCollider();

            return this;
        }

        /// <summary>
        /// Sets a vertex of polygon to <paramref name="position"/>.
        /// </summary>
        public PolygonCollider SetVertex(int index, Vector2 position) {
            _polygonShape[index] = position;
            return this;
        }

        public override void UpdateCollider() {
            if (_originalVertices is null || Transform is null)
                return;

            var offset = _offset;
            var origin = _origin - new Vector2(0.5f);

            if (!ShouldRotateWithTransform && !ShouldScaleWithTransform) {
                // set the original vertices
                _polygonShape.SetVertices(_originalVertices);
            } else {
                // copy the original vertices into transformed array
                Array.Resize(ref _transformedVertices, _originalVertices.Length);
                Array.Copy(_originalVertices, _transformedVertices, _originalVertices.Length);

                // apply scale
                if (ShouldScaleWithTransform) {
                    offset *= Entity.Transform.Scale;
                    Polygon.Scale(_transformedVertices, Entity.Transform.Scale);
                }

                // apply rotation
                if (ShouldRotateWithTransform) {
                    offset = offset.RotatedBy(Entity.Transform.Rotation);
                    Polygon.Rotate(_transformedVertices, Entity.Transform.Rotation);
                }

                // submit new vertices
                _polygonShape.SetVertices(_transformedVertices);
            }

            var size = _polygonShape.Bounds.Size;
            _polygonShape.Position = Entity.Transform.Position
                - (ShouldRotateWithTransform ? (size * origin).RotatedBy(Entity.Transform.Rotation) : size * origin)
                + offset;

            base.UpdateCollider();
        }
    }
}
