using Instant2D.Coroutines;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D.Coroutines;

/// <summary>
/// Awaits for specified amount of seconds when yielded. <br/>
/// Unset <paramref name="UseTargetTimescale"/> to toggle awaiting in realtime seconds. <br/>
/// NOTE: in manually ticked coroutines <paramref name="Duration"/> will be clamped to minimum tick rate.
/// </summary>
public record struct WaitForSeconds(float Duration, bool UseTargetTimescale = true) : ICoroutineAwaitable {
    float _waitTimer, _lastTickTime;
    void ICoroutineAwaitable.Initialize(Coroutine coroutine) {
        _lastTickTime = Time.Total;
    }

    bool ICoroutineAwaitable.Tick(Coroutine coroutine) {
        var delta = Time.Total - _lastTickTime;
        _lastTickTime = Time.Total;

        // scale time delta with target's timescale when needed
        if (coroutine.UseTargetTimescale && UseTargetTimescale && coroutine.Target is ICoroutineTarget target)
            delta *= target.Timescale;

        _waitTimer += delta;
        if (_waitTimer >= Duration) {
            return true;
        }

        return false;
    }
}
