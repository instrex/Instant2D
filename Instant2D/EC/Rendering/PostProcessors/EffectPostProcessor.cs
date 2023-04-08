using Instant2D.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Instant2D.EC.Rendering;

public class EffectPostProcessor : PostProcessor {
    readonly Material _material;
    public EffectPostProcessor(Material material) {
        _material = material;
    }

    public override void Apply(RenderTarget2D source, RenderTarget2D destination) {
        InstantApp.Instance.GraphicsDevice.SetRenderTarget(destination);

        GraphicsManager.Context.Begin(_material, Matrix.Identity);

        GraphicsManager.Context.DrawTexture(
            source,
            Vector2.Zero,
            null,
            Color.White,
            0,
            Vector2.Zero,
            Vector2.One
        );

        GraphicsManager.Context.End();
    }
}
