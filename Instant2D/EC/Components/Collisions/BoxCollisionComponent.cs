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
                UpdateBoxCollider();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void UpdateBoxCollider() {
            // only update if properties change after initialization
            if (Entity == null)
                return;

            var size = ShouldScaleWithTransform ? _unscaledSize * Transform.Scale : _unscaledSize;
            _offset = size * _origin;

            // offset the position by origin amount
            BaseCollider.Position = Transform.Position - _offset;
            BaseCollider.Size = size;
            // TODO: BaseCollider.Rotation = Transform.Rotation;
            BaseCollider.Update();
        }

        public override void UpdateCollider() => UpdateBoxCollider();

        public override void OnTransformUpdated(TransformComponentType components) {
            base.OnTransformUpdated(components);

            // if scale changed, full update is required
            if ((components & TransformComponentType.Scale) != 0) {
                UpdateBoxCollider();
                return;
            }

            // else, just update the position
            BaseCollider.Position = Transform.Position - _offset;
            BaseCollider.Update();
        }

        public override void OnEnabled() {
            UpdateBoxCollider();

            // update values before registering the collider
            base.Initialize();
        }
    }
}
