namespace Instant2D.Coroutines;

/// <summary>
/// Awaits for the next coroutine tick. In coroutines created with <see cref="NewCoroutineManager"/>, this will happen each scene update. <br/>
/// For manually ticked coroutines, this will differ.
/// </summary>
public record struct WaitForNextTick : ICoroutineAwaitable {
    readonly void ICoroutineAwaitable.Initialize(Coroutine coroutine) { }
    readonly bool ICoroutineAwaitable.Tick(Coroutine coroutine) {
        return true;
    }
}
