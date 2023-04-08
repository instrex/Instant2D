namespace Instant2D.Coroutines;

/// <summary>
/// Optional interface for objects that may be tracked for coroutine usage. // TODO: how to use
/// </summary>
public interface ICoroutineTarget {
    /// <summary>
    /// Current time scale of this object. Used by WaitForSeconds.
    /// </summary>
    float TimeScale { get; }
}
