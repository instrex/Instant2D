namespace Instant2D.Coroutines;

public record struct WaitForSeconds(float Duration, bool UseTargetTimescale = true) : ICoroutineAwaiter {
    float _waitTimer;
    bool ICoroutineAwaiter.Tick(CoroutineDriver coroutine, float dt) {
        if (UseTargetTimescale && coroutine.Target != null) {
            dt *= coroutine.Target.TimeScale;
        }

        return (_waitTimer += dt) >= Duration;
    }
}
