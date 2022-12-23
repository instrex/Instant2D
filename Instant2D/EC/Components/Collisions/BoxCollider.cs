using Instant2D.Collision.Shapes;
using Instant2D.Graphics;
using Instant2D.Utils;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D.EC.Components.Collisions {
    /// <summary>
    /// Box-shaped collider. If precise collision calculation for rotated boxes is not required, <see cref="CollisionComponent.ShouldRotateWithTransform"/> may be set to <see langword="false"/> for some optimizations.
    /// </summary>
    public class BoxCollider : CollisionComponent {
        readonly Box _boxShape;
        Vector2 _size;

        public BoxCollider() : this(new(32)) { }
        public BoxCollider(Vector2 size) {
            Shape = _boxShape = new Box {
                Size = _size = size
            };
        }

        /// <summary>
        /// Controls the size of the box.
        /// </summary>
        public Vector2 Size {
            get => _size;
            set {
                _size = value;
                UpdateCollider();
            }
        }

        public override void Initialize() {
            base.Initialize();

            // set initial values
            UpdateCollider();
        }

        public override void UpdateCollider() {
            var size = ShouldScaleWithTransform ? _size * Transform.Scale : _size;
            var offset = ShouldScaleWithTransform ? _offset * Transform.Scale : _offset;

            // set box values
            _boxShape.Position = Transform.Position - size * (_origin - new Vector2(0.5f)) + offset;
            _boxShape.Rotation = ShouldRotateWithTransform ? Transform.Rotation : 0f;
            _boxShape.Size = size;

            // move collider in spatial hash
            base.UpdateCollider();
        }

        public override void DrawDebugShape(DrawingContext drawing) {
            base.DrawDebugShape(drawing);

            if (_boxShape.HasPolygon) {
                // draw polygon
                var vertices = _boxShape.Polygon.Vertices;
                for (var i = 0; i < vertices.Length; i++) {
                    drawing.DrawLine(_boxShape.Position + vertices[i], _boxShape.Position + vertices[i + 1 >= vertices.Length ? 0 : i + 1], Color.Red, 2);
                }

                return;
            }

            // draw box shape
            drawing.DrawRectangle(_boxShape.Bounds, Color.Transparent, Color.Red, 2);
        }

        /// <inheritdoc cref="Size"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BoxCollider SetSize(float size) => SetSize(new Vector2(size));

        /// <inheritdoc cref="Size"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BoxCollider SetSize(float width, float height) => SetSize(new Vector2(width, height));

        /// <inheritdoc cref="Size"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BoxCollider SetSize(Vector2 size) {
            Size = size;
            return this;
        }
    }
}
