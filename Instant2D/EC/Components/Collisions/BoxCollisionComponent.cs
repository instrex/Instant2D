using Instant2D.Collision;
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

        // raw size of this collider,
        // as specified by user without any scale transformations
        Vector2 _rawSize;

        public BoxCollisionComponent() : this(new Vector2(32)) { }
        public BoxCollisionComponent(Vector2 size) : base(new BoxCollider<CollisionComponent>()) {
            BaseCollider = base.BaseCollider as BoxCollider<CollisionComponent>;
            _rawSize = size;
        }

        /// <summary>
        /// The unscaled size this collider must possess. To get an actual scaled size check <see cref="BoxCollider{T}.Size"/>.
        /// </summary>
        public Vector2 Size {
            get => _rawSize;
            set {
                _rawSize = value;
                _wasSizeSet = true;
                UpdateBoxCollider();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void UpdateBoxCollider() {
            // only update if properties change after initialization
            if (Entity == null)
                return;

            var size = ShouldScaleWithTransform ? _rawSize * Transform.Scale : _rawSize;
            var offset = ShouldScaleWithTransform ? _offset * Transform.Scale : _offset;

            // offset the position by corrected origin times size
            // -0.5 is required to correctly adjust the position with internal collision system
            // afterwards, apply offset to the end result
            BaseCollider.Position = Transform.Position - size * (_origin - new Vector2(0.5f)) + offset;
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

            // require update when position changes or any tracked transformations are detected
            var updateRequired = ((components & TransformComponentType.Position) != 0)
                || (ShouldScaleWithTransform && (components & TransformComponentType.Scale) != 0);

            if (updateRequired) {
                UpdateBoxCollider();
                return;
            }
        }

        /// <inheritdoc cref="Size"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BoxCollisionComponent SetSize(float size) => SetSize(new Vector2(size));

        /// <inheritdoc cref="Size"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BoxCollisionComponent SetSize(float width, float height) => SetSize(new Vector2(width, height));

        /// <inheritdoc cref="Size"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BoxCollisionComponent SetSize(Vector2 size) {
            Size = size;
            UpdateBoxCollider();

            return this;
        }
    }
}
