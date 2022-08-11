using Instant2D.Collisions;
using Microsoft.Xna.Framework;
using System.Runtime.CompilerServices;

namespace Instant2D.EC.Components {
    /// <summary>
    /// Wrapper on <see cref="BoxCollider{T}"/>, provides useful methods to work with the collider. 
    /// Avoid changing <see cref="BaseCollider"/> fields directly, as it might cause some weirdness.
    /// </summary>
    public class BoxCollisionComponent : CollisionComponent {
        /// <summary>
        /// BaseCollider references casted as BoxCollider for convenient access.
        /// </summary>
        public readonly new BoxCollider<CollisionComponent> BaseCollider;

        Vector2 _unscaledSize, _offset;

        public BoxCollisionComponent() : this(new Vector2(32)) { }
        public BoxCollisionComponent(Vector2 size) : base(new BoxCollider<CollisionComponent>()) {
            BaseCollider = base.BaseCollider as BoxCollider<CollisionComponent>;
            _unscaledSize = size;
        }

        /// <summary>
        /// The unscaled size this collider must possess. To get an actual scaled size check <see cref="BoxCollider{T}.Size"/>.
        /// </summary>
        public Vector2 Size {
            get => _unscaledSize;
            set {
                _unscaledSize = value;
                _wasSizeSet = true;
                UpdateBoxCollider();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void UpdateBoxCollider() {
            // only update if properties change after initialization
            if (Entity == null)
                return;

            var size = ShouldScaleWithTransform ? _unscaledSize * Transform.Scale : _unscaledSize;
            _offset = _origin * Transform.Scale;

            // offset the position by origin amount
            BaseCollider.Position = Transform.Position + _offset;
            BaseCollider.Size = size;
            // TODO: BaseCollider.Rotation = Transform.Rotation;
            BaseCollider.Update();
        }

        public override void UpdateCollider() => UpdateBoxCollider();

        public override void AutoResize(RectangleF bounds) {
            SetSize(bounds.Size / Entity.Transform.Scale);
        }

        public override void OnTransformUpdated(TransformComponentType components) {
            base.OnTransformUpdated(components);

            // if scale changed, full update is required
            if (ShouldScaleWithTransform && (components & TransformComponentType.Scale) != 0) {
                UpdateBoxCollider();
                return;
            }

            // else, just update the position
            if ((components & TransformComponentType.Position) != 0) {
                BaseCollider.Position = Entity.Transform.Position + _offset;
                BaseCollider.Update();
            }
        }

        /// <inheritdoc cref="Size"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BoxCollisionComponent SetSize(float size, bool centerOrigin = true) => SetSize(new Vector2(size), centerOrigin);

        /// <inheritdoc cref="Size"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BoxCollisionComponent SetSize(float width, float height, bool centerOrigin = true) => SetSize(new Vector2(width, height), centerOrigin);

        /// <inheritdoc cref="Size"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BoxCollisionComponent SetSize(Vector2 size, bool centerOrigin = true) {
            if (centerOrigin) {
                // use backing field to avoid unnecessary updates
                _origin = size * 0.5f;
            }

            Size = size;
            UpdateBoxCollider();

            return this;
        }
    }
}
