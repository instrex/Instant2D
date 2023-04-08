namespace Instant2D.Coroutines;

public record struct WaitForNextFrame : ICoroutineAwaiter {
    bool ICoroutineAwaiter.Tick(CoroutineDriver coroutine, float dt) => true;
}
