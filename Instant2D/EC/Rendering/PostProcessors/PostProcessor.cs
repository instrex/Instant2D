using Instant2D.Utils.ResolutionScaling;
using Microsoft.Xna.Framework.Graphics;

namespace Instant2D.EC.Rendering;

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
