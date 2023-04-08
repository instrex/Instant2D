namespace Instant2D.Coroutines;

/// <summary>
/// Implement this interface in order to make any class/struct yieldable inside coroutines.
/// </summary>
public interface ICoroutineAwaiter {
    /// <summary>
    /// Return <see langword="true"/> to advance the coroutine, otherwise this method will be run again when the coroutine is ticked.
    /// </summary>
    bool Tick(CoroutineDriver coroutine, float dt);
}
