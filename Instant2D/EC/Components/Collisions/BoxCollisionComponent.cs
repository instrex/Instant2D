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

        Vector2 _unscaledSize = new(32), _origin = new(0.5f);
        Vector2 _correctedOrigin, _offset;

        public BoxCollisionComponent() : base(new BoxCollider<CollisionComponent>()) {
            BaseCollider = base.BaseCollider as BoxCollider<CollisionComponent>;
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

        /// <summary>
        /// Origin of this collider. Defaults to {0.5, 0.5}.
        /// </summary>
        public Vector2 Origin {
            get => _origin;
            set {
                // calculate corrected origin as BoxCollider is centered by default 
                _correctedOrigin = value - new Vector2(0.5f);
                _origin = value;

                UpdateBoxCollider();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void UpdateBoxCollider() {
            // only update if properties change after initialization
            if (Entity == null)
                return;

            var size = ShouldScaleWithTransform ? _unscaledSize * Transform.Scale : _unscaledSize;
            _offset = size * _correctedOrigin;

            // offset the position by origin amount
            BaseCollider.Position = Transform.Position - _offset;
            BaseCollider.Size = size;
            // TODO: BaseCollider.Rotation = Transform.Rotation;
            BaseCollider.Update();
        }

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
