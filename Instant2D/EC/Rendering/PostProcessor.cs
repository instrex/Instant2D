using Instant2D.Graphics;
using Instant2D.Utils.ResolutionScaling;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Instant2D.EC.Rendering {
    public abstract class PostProcessor {
        public MasteringLayer Parent;
        public bool IsActive = true;
        public float Order;

        /// <summary>
        /// <see cref="MasteringLayer.Scene"/> shortcut.
        /// </summary>
        public Scene Scene => Parent.Scene;

        /// <summary>
        /// Is automatically invoked by <see cref="MasteringLayer"/> after resolution changes.
        /// </summary>
        public virtual void OnResolutionChanged(ScaledResolution resolution, ScaledResolution previousResolution) { }

        /// <summary>
        /// Apply post processing effects to <paramref name="destination"/>.
        /// </summary>
        public abstract void Apply(RenderTarget2D source, RenderTarget2D destination);
    }

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
}
