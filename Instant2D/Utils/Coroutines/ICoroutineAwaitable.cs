namespace Instant2D.Coroutines {
    /// <summary>
    /// May be used to implement custom wait methods for <see cref="Coroutine"/>s.
    /// </summary>
    public interface ICoroutineAwaitable {
        /// <summary>
        /// Return <see langword="true"/> to make the underlying coroutine wait for next tick.
        /// </summary>
        bool ShouldWait(Coroutine coroutine);
    }
}
