using System;

namespace Instant2D.Modules;

/// <summary>
/// Game module for renderable systems.
/// </summary>
public interface IRenderableGameSystem : IComparable<IRenderableGameSystem> {
    int IComparable<IRenderableGameSystem>.CompareTo(Instant2D.Modules.IRenderableGameSystem other) => PresentOrder.CompareTo(other.PresentOrder);

    /// <summary>
    /// The order in which <see cref="Present(InstantApp)"/> functions are ran.
    /// </summary>
    float PresentOrder { get; }

    /// <summary>
    /// Draw the contents of this system to screen.
    /// </summary>
    /// <param name="app"></param>
    void Present(InstantApp app);
}