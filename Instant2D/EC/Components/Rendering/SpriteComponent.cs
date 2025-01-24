using Instant2D.Graphics;
using Instant2D.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D.EC {
    public class SpriteComponent : RenderableComponent, IPooledInstance {
        protected bool _isSpriteSet;
        protected SpriteEffects _spriteFx;

        protected Vector2 _origin;
        protected Sprite _sprite;

        /// <summary>
        /// The sprite to render.
        /// </summary>
        public Sprite Sprite { 
            get => _sprite;
            set {
                _sprite = value;
                _origin = _sprite.Origin;
                CalculateOrigin();
                _isSpriteSet = true;
                _boundsDirty = true;
            } 
        }

        /// <summary>
        /// Whether or not this sprite should be horizontally flipped. Transformations will be applied to the origin.
        /// </summary>
        public bool FlipX {
            get => _spriteFx.HasFlag(SpriteEffects.FlipHorizontally);
            set => SpriteEffects = value ? _spriteFx | SpriteEffects.FlipHorizontally : _spriteFx & ~SpriteEffects.FlipHorizontally;
        }

        /// <summary>
        /// Whether or not this sprite should be vertically flipped. Transformations will be applied to the origin.
        /// </summary>
        public bool FlipY {
            get => _spriteFx.HasFlag(SpriteEffects.FlipVertically);
            set => SpriteEffects = value ? _spriteFx | SpriteEffects.FlipVertically : _spriteFx & ~SpriteEffects.FlipVertically; 
        }

        void CalculateOrigin() {
            _origin.X = FlipX ? _sprite.SourceRect.Width - _sprite.Origin.X : _sprite.Origin.X;
            _origin.Y = FlipY ? _sprite.SourceRect.Height - _sprite.Origin.Y : _sprite.Origin.Y;
        }

        /// <summary>
        /// Gets flip information of this sprite.
        /// </summary>
        public SpriteEffects SpriteEffects {
            get => _spriteFx;
            set {
                _spriteFx = value;
                CalculateOrigin();
            }
        }

        #region Point handling

        /// <summary>
        /// Attempts to get the sprite point with Entity tranformations applied. If it fails, Entity position is returned instead. <br/>
        /// If you need to get a raw point in sprite space, set <paramref name="dontApplyTransform"/> to <see langword="true"/>.
        /// </summary>
        public virtual bool TryGetPoint(string key, out Vector2 point, bool dontApplyTransform = false) {
            if (!_isSpriteSet || _sprite.Points == null || !_sprite.Points.TryGetValue(key, out var rawPoint)) {
                point = Entity.TransformState.Position;
                return false;
            }

            var offset = rawPoint.ToVector2() - _origin;
            point = Entity.TransformState.Position + (dontApplyTransform ? offset : TransformPointOffset(offset));
            return true;
        }

        /// <summary>
        /// Gets a sprite point using <see cref="TryGetPoint(string, out Vector2)"/>. If point with such key doesn't exist, <c>Transform.Position</c> is returned.
        /// </summary>
        public Vector2 this[string key] {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get {
                TryGetPoint(key, out var point);
                return point;
            }
        }

        // TODO: this seems pretty wacky, replace it with matrix multiplication maybe?
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected Vector2 TransformPointOffset(Vector2 offset) => 
            offset.RotatedBy(Entity.Transform.Rotation * (FlipX ? 1 : -1)) * Entity.Transform.Scale
            * new Vector2(FlipX ? 1 : -1, FlipY ? 1 : -1)
            + Offset;

        #endregion

        protected override void RecalculateBounds(ref RectangleF bounds) {
            if (!_isSpriteSet) {
                bounds = RectangleF.Empty;
                return;
            }

            bounds = CalculateBounds(Transform.Position + Offset, Vector2.Zero, _origin, new(Sprite.SourceRect.Width, Sprite.SourceRect.Height),
                Transform.Rotation, Transform.Scale);
        }

        public override void Draw(DrawingContext drawing, CameraComponent camera) {
            if (!_isSpriteSet)
                return;

            drawing.DrawTexture(
                Sprite.Texture,
                Entity.TransformState.Position + Offset,
                Sprite.SourceRect,
                Color,
                Entity.TransformState.Rotation,
                _origin,
                Entity.TransformState.Scale,
                _spriteFx
            );
        }

        void IPooledInstance.Reset() {
            _spriteFx = SpriteEffects.None;
            _boundsDirty = true;
            _isSpriteSet = false;
            _sprite = default;
            Color = Color.White;
            _material = Material.AlphaBlend;
            _depth = 0;
            _z = 0;
        }
    }
}
