using Instant2D.Graphics;
using Instant2D.Utils.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D.EC.Components {
    public class SpriteRenderer : RenderableComponent {
        RectangleF _bounds;
        bool _boundsDirty = true;

        /// <summary>
        /// The sprite to render.
        /// </summary>
        public Sprite Sprite { get; set; }

        public override RectangleF Bounds {
            get {
                if (_boundsDirty) {
                    _boundsDirty = false;
                }

                return _bounds;
            }
        }

        public override void OnTransformUpdated(Transform.ComponentType components) {
            base.OnTransformUpdated(components);
        }


        public override void Draw(IDrawingBackend drawing, ICamera camera) {
            drawing.Draw(Sprite, Entity.Transform.Position, Color, Entity.Transform.Rotation, Entity.Transform.Scale, Microsoft.Xna.Framework.Graphics.SpriteEffects.None);
        }
    }
}
