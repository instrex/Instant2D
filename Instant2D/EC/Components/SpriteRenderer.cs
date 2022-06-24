using Instant2D.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D.EC.Components {
    public class SpriteRenderer : RenderableComponent {
        /// <summary>
        /// The sprite to render.
        /// </summary>
        public Sprite Sprite { get; set; }

        public override void Draw(IDrawingBackend drawing, ICamera camera) {
            drawing.Draw(Sprite, Entity.Transform.Position, Color, Entity.Transform.Rotation, Entity.Transform.Scale, Microsoft.Xna.Framework.Graphics.SpriteEffects.None);
        }
    }
}
