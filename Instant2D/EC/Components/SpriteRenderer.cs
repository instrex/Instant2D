using Instant2D.Graphics;
using Instant2D.Utils;
using Instant2D.Utils.Math;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D.EC.Components {
    public class SpriteRenderer : RenderableComponent {
        RectangleF _bounds;
        bool _boundsDirty = true;
        SpriteEffects _spriteFx;

        /// <summary>
        /// The sprite to render.
        /// </summary>
        public Sprite Sprite { get; set; }

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

        #region Setters

        /// <inheritdoc cref="FlipY"/>
        public SpriteRenderer SetFlipY(bool flipY) {
            FlipY = flipY;
            return this;
        }

        /// <inheritdoc cref="FlipX"/>
        public SpriteRenderer SetFlipX(bool flipX) {
            FlipX = flipX;
            return this;
        }

        /// <inheritdoc cref="Sprite"/>
        public SpriteRenderer SetSprite(Sprite sprite) {
            Sprite = sprite;
            return this;
        }

        #endregion

        public override RectangleF Bounds {
            get {
                if (_boundsDirty) {
                    _boundsDirty = false;
                }

                return _bounds;
            }
        }

        public override void OnTransformUpdated(Transform.ComponentType components) {
            _boundsDirty = true;
        }

        public override void Draw(IDrawingBackend drawing, ICamera camera) {
            drawing.Draw(
                Sprite, 
                Entity.Transform.Position, 
                Color, 
                Entity.Transform.Rotation, 
                Entity.Transform.Scale,
                _spriteFx
            );
        }
    }
}
