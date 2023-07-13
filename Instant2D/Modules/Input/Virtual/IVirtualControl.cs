namespace Instant2D.Input.Virtual;

public interface IVirtualControl {
    /// <summary>
    /// Called each frame, use it to cache input values.
    /// </summary>
    void Update(float dt);

    /// <summary>
    /// Called when control scheme changes.
    /// </summary>
    void Reset();
}
