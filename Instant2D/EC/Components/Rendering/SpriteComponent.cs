using Instant2D.Graphics;
using Instant2D.Utils;
using Instant2D.Utils.Math;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D.EC {
    public class SpriteComponent : RenderableComponent, IPooled {
        bool _isSpriteSet, _autocorrectOrigin = true;
        protected SpriteEffects _spriteFx;

        Vector2 _origin;
        Sprite _sprite;

        /// <summary>
        /// The sprite to render.
        /// </summary>
        public Sprite Sprite { 
            get => _sprite;
            set {
                _sprite = value;
                _origin = _sprite.Origin;
                _isSpriteSet = true;
                _boundsDirty = true;
            } 
        }

        /// <summary>
        /// Whether or not this sprite should be horizontally flipped. Transformations will be applied to the origin.
        /// </summary>
        public bool FlipX {
            get => (_spriteFx & SpriteEffects.FlipHorizontally) == 0;
            set {
                _spriteFx = value ? _spriteFx | SpriteEffects.FlipHorizontally : _spriteFx & ~SpriteEffects.FlipHorizontally;
                if (_autocorrectOrigin) {
                    _origin.X = value ? _sprite.SourceRect.Width - _sprite.Origin.X : _sprite.Origin.X;
                }
            }
        }

        /// <summary>
        /// Whether or not this sprite should be vertically flipped. Transformations will be applied to the origin.
        /// </summary>
        public bool FlipY {
            get => (_spriteFx & SpriteEffects.FlipVertically) == 0;
            set {
                _spriteFx = value ? _spriteFx | SpriteEffects.FlipVertically : _spriteFx & ~SpriteEffects.FlipVertically;
                if (_autocorrectOrigin) {
                    _origin.Y = value ? _sprite.SourceRect.Height - _sprite.Origin.Y : _sprite.Origin.Y;
                }
            } 
        }

        /// <summary>
        /// Gets flip information of this sprite.
        /// </summary>
        public SpriteEffects SpriteEffects {
            get => _spriteFx;
            set => _spriteFx = value;
        }

        #region Point handling

        /// <summary>
        /// Attempts to get the sprite point with Entity tranformations applied. If it fails, Entity position is returned instead. <br/>
        /// If you need to get a raw point in sprite space, set <paramref name="dontApplyTransform"/> to <see langword="true"/>.
        /// </summary>
        public virtual bool TryGetPoint(string key, out Vector2 point, bool dontApplyTransform = false) {
            if (!_isSpriteSet || _sprite.Points == null || !_sprite.Points.TryGetValue(key, out var rawPoint)) {
                point = Transform.Position;
                return false;
            }

            var offset = rawPoint.ToVector2() - _sprite.Origin;
            point = Entity.Transform.Position + (dontApplyTransform ? offset : TransformPointOffset(offset));
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected Vector2 TransformPointOffset(Vector2 offset) => 
            offset.RotatedBy(Entity.TransformState.Rotation) * Entity.TransformState.Scale
            * new Vector2(FlipX ? 1 : -1, FlipY ? 1 : -1)
            + Offset;

        #endregion

        protected override void RecalculateBounds(ref RectangleF bounds) {
            if (!_isSpriteSet) {
                bounds = RectangleF.Empty;
                return;
            }

            bounds = CalculateBounds(Transform.Position, Vector2.Zero, Sprite.Origin, new(Sprite.SourceRect.Width, Sprite.SourceRect.Height),
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

        void IPooled.Reset() {
            _spriteFx = SpriteEffects.None;
            _boundsDirty = true;
            _isSpriteSet = false;
            _sprite = default;
            Color = Color.White;
            _material = Material.Default;
            _depth = 0;
            _z = 0;
        }
    }
}
