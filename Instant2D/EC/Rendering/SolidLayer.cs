using Instant2D.Graphics;
using Microsoft.Xna.Framework;

namespace Instant2D.EC.Rendering {
    public class SolidLayer : IRenderLayer {
        /// <summary>
        /// Color to fill the layer with.
        /// </summary>
        public Color Color;

        /// <inheritdoc cref="Color"/>
        public SolidLayer SetColor(Color color) {
            Color = color;
            return this;
        }

        // IRenderLayer impl
        public bool IsActive { get; set; } = true;
        public bool ShouldPresent { get; set; } = true;
        public Scene Scene { get; init; }
        public float Order { get; init; }
        public string Name { get; init; }

        public void Prepare() {
            // no preparation required
        }

        public void Present(DrawingContext drawing) {
            drawing.DrawRectangle(new(Vector2.Zero, Scene.Resolution.renderTargetSize.ToVector2()), Color);
        }
    }
}
