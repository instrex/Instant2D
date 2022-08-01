using Instant2D.Collisions;
using System;
using System.Runtime.CompilerServices;

namespace Instant2D.EC.Components {
    /// <summary>
    /// Wrapper on <see cref="CircleCollider{T}"/>, provides useful methods to work with the collider. 
    /// Avoid changing <see cref="BaseCollider"/> fields directly, as it might cause some weirdness.
    /// </summary>
    public class CircleCollisionComponent : CollisionComponent {
        /// <summary>
        /// BaseCollider references casted as CircleCollider for convenient access.
        /// </summary>
        public readonly new CircleCollider<CollisionComponent> BaseCollider;

        float _unscaledRadius;

        public CircleCollisionComponent() : this(8) { }
        public CircleCollisionComponent(float radius) : base(new CircleCollider<CollisionComponent>()) {
            BaseCollider = base.BaseCollider as CircleCollider<CollisionComponent>;
            BaseCollider.Radius = radius;
        }

        public override void OnTransformUpdated(TransformComponentType components) {
            base.OnTransformUpdated(components);

            // resize the radius with scale when needed
            if (ShouldScaleWithTransform && (components & TransformComponentType.Scale) != 0) {
                UpdateCollider();
                return;
            }

            // move the collider
            if ((components & TransformComponentType.Position) != 0) {
                BaseCollider.Position = Entity.Transform.Position;
                BaseCollider.Update();
            }
        }

        public override void AutoResize(RectangleF bounds) {
            SetRadius(MathF.Min(bounds.Width / Entity.Transform.Scale.X, bounds.Height / Entity.Transform.Scale.X) * 0.5f);
        }

        public override void UpdateCollider() {
            if (Entity == null)
                return;

            var offset = _unscaledRadius * _origin;
            BaseCollider.Radius = ShouldScaleWithTransform ? _unscaledRadius * MathF.Max(Entity.Transform.Scale.X, Entity.Transform.Scale.Y) : _unscaledRadius;
            BaseCollider.Position = Entity.Transform.Position - offset;
            BaseCollider.Update();
        }

        /// <summary>
        /// Sets the radius of a circle used for collisions.
        /// </summary>
        public float Radius {
            get => _unscaledRadius;
            set {
                _wasSizeSet = true;
                _unscaledRadius = value;
                UpdateCollider();
            }
        }

        /// <inheritdoc cref="Radius"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CircleCollisionComponent SetRadius(float radius) {
            Radius = radius;
            return this;
        }
    }
}
