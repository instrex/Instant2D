using Instant2D.Modules;
using Instant2D.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D.Coroutines;

/// <summary>
/// A system to run and manage continous tasks.
/// </summary>
public class CoroutineManager : IGameSystem, ICoroutineTarget {
    readonly static List<Coroutine> _activeCoroutines = [];

    /// <summary>
    /// May be used in conjuction with <see cref="Tick"/> to synchronize coroutine ticks to your custom game loop. <br/>
    /// Defaults to <see langword="false"/>, meaning active coroutines will tick each update.
    /// </summary>
    public bool IsManuallyControlled { get; set; } = false;

    /// <summary>
    /// Starts a new coroutine and returns its reference. Will continously tick it until it stops.
    /// </summary>
    public static Coroutine Start(IEnumerator<ICoroutineAwaitable> enumerator, ICoroutineTarget target = default) {
        var coroutine = new Coroutine(target);
        coroutine.Start(enumerator);

        // if coroutine finishes in one tick, leave it alone
        if (!coroutine.Tick()) 
            return coroutine;
        
        // else add it for later execution
        _activeCoroutines.Add(coroutine);
        return coroutine;
    }

    /// <summary>
    /// Reuses a coroutine reference, stopping it before starting if it's running.
    /// </summary>
    public static Coroutine Start(Coroutine coroutine, IEnumerator<ICoroutineAwaitable> enumerator) {
        if (coroutine.IsRunning)
            coroutine.Stop();

        coroutine.Start(enumerator);

        // if coroutine finishes in one tick, leave it alone
        if (!coroutine.Tick()) 
            return coroutine;
        
        // register the coroutine as active if haven't already
        if (!_activeCoroutines.Contains(coroutine))
            _activeCoroutines.Add(coroutine);

        return coroutine;
    }

    /// <inheritdoc cref="Defer(float, ICoroutineTarget, Action)">
    public static Coroutine Defer(float delay, Action action) => Defer(delay, null, action);

    /// <summary>
    /// Defers <paramref name="action"/> for <paramref name="delay"/> seconds.
    /// </summary>
    public static Coroutine Defer(float delay, ICoroutineTarget target, Action action) {
        static IEnumerator<ICoroutineAwaitable> DeferCoroutine(float delay, Action action) {
            yield return new WaitForSeconds(delay);
            action?.Invoke();
        }

        return Start(DeferCoroutine(delay, action), target);
    }

    /// <summary>
    /// Attempts to advance all active coroutines. May be used in conjuction with <see cref="IsManuallyControlled"/> to synchronize coroutine ticks to specific events.
    /// </summary>
    public void Tick() {
        if (_activeCoroutines.Count == 0)
            return;

        var buffer = ListPool<Coroutine>.Rent();
        buffer.AddRange(_activeCoroutines);

        var clearExpiredCoroutines = false;
        for (int i = 0; i < buffer.Count; i++) {
            if (!buffer[i].Tick())
                _activeCoroutines.Remove(buffer[i]);
        }

        buffer.Pool();

        if (clearExpiredCoroutines) _activeCoroutines.RemoveAll(c => !c.IsRunning);
    }

    float ICoroutineTarget.Timescale => 1.0f;
    float IGameSystem.UpdateOrder { get; }
    void IGameSystem.Initialize(InstantApp app) { }
    void IGameSystem.Update(InstantApp app, float deltaTime) {
        if (IsManuallyControlled || !app.IsActive)
            return;

        Tick();
    }
}
