namespace Instant2D.Coroutines;

/// <summary>
/// Enumeration of loop events which could be used to synchronize coroutines to Scene lifetime events.
/// </summary>
public enum SceneLoopEvent {
    /// <summary>
    /// Called after FixedUpdate loop.
    /// </summary>
    FixedUpdate,

    /// <summary>
    /// Called after each frame.
    /// </summary>
    Update,

    /// <summary>
    /// Called before rendering each frame.
    /// </summary>
    LateUpdate
}