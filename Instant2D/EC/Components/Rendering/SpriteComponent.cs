using Instant2D.Graphics;
using Instant2D.Utils;
using Instant2D.Utils.Math;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D.EC {
    public class SpriteComponent : RenderableComponent, IPooled {
        bool _isSpriteSet;
        protected SpriteEffects _spriteFx;
        Sprite _sprite;

        /// <summary>
        /// The sprite to render.
        /// </summary>
        public Sprite Sprite { 
            get => _sprite;
            set {
                _sprite = value;
                _isSpriteSet = true;
                _boundsDirty = true;
            } 
        }

        /// <summary>
        /// Whether or not this sprite should be horizontally flipped. Transformations will be applied to the origin.
        /// </summary>
        public bool FlipX {
            get => (_spriteFx & SpriteEffects.FlipHorizontally) == 0;
            set => _spriteFx = value ? _spriteFx | SpriteEffects.FlipHorizontally : _spriteFx & ~SpriteEffects.FlipHorizontally;
        }

        /// <summary>
        /// Whether or not this sprite should be vertically flipped. Transformations will be applied to the origin.
        /// </summary>
        public bool FlipY {
            get => (_spriteFx & SpriteEffects.FlipVertically) == 0;
            set => _spriteFx = value ? _spriteFx | SpriteEffects.FlipVertically : _spriteFx & ~SpriteEffects.FlipVertically;
        }

        /// <summary>
        /// Gets flip information of this sprite.
        /// </summary>
        public SpriteEffects SpriteEffects {
            get => _spriteFx;
            set => _spriteFx = value;
        }

        protected override void RecalculateBounds(ref RectangleF bounds) {
            if (!_isSpriteSet) {
                bounds = RectangleF.Empty;
                return;
            }

            bounds = CalculateBounds(Transform.Position, Vector2.Zero, Sprite.Origin, new(Sprite.SourceRect.Width, Sprite.SourceRect.Height),
                Transform.Rotation, Transform.Scale);
        }

        public override void Draw(IDrawingBackend drawing, CameraComponent camera) {
            if (!_isSpriteSet)
                return;

            drawing.Draw(
                Sprite, 
                Entity.Transform.Position + Offset, 
                Color, 
                Entity.Transform.Rotation, 
                Entity.Transform.Scale,
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
