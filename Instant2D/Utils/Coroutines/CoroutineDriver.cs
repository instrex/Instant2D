using Instant2D.Utils;
using System;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Instant2D.Coroutines;

/// <summary>
/// Provides a reusable wrapper which can control how and where coroutines run. Call <see cref="Tick(float)"/> in places of your preference. <br/>
/// Used internally by CoroutineManager.
/// </summary>
public class CoroutineDriver : ICoroutineAwaiter, IPooledInstance {
    WeakReference<ICoroutineTarget> _targetRef;
    ICoroutineAwaiter _awaiter;

    // changing Target of CoroutineManager-produced drivers would be weird
    // so it's best to notify user that it shouldn't happen in the first place
    internal bool Tracked;

    // a flag for CoroutineManager to pool instances
    // note that no reference to such object should ever be exposed
    internal bool ShouldRecycle;

    /// <summary>
    /// Internal enumerator this wrapper iterates over.
    /// </summary>
    public IEnumerator Enumerator { get; private set; }

    /// <summary>
    /// Return <see langword="true"/> if coroutine is currently being executed.
    /// </summary>
    public bool IsRunning => Enumerator != null;

    /// <summary>
    /// Optional target object of this coroutine. Should not be modified when using CoroutineManager.
    /// </summary>
    public ICoroutineTarget Target {
        get {
            // if target is not yet set or unavalaible, return null
            if (_targetRef is null || !_targetRef.TryGetTarget(out var target))
                return null;

            return target;
        }

        set {
            if (Tracked)
                throw new InvalidOperationException("Couldn't modify Target of CoroutineDriver, as it is managed by CoroutineManager.");

            if (_targetRef is null) {
                if (value is null)
                    return;

                // create new WeakReference to the target object
                _targetRef = new WeakReference<ICoroutineTarget>(value);
                CoroutineManager.AttachToTarget(value, this);

                return;
            }

            // detach from previous target
            if (_targetRef.TryGetTarget(out var prevTarget) && CoroutineManager.TargetTable.TryGetValue(prevTarget, out var list)) {
                list.Remove(this);
            }

            _targetRef.SetTarget(value);
            CoroutineManager.AttachToTarget(value, this);
        }
    }

    /// <summary>
    /// A callback invoked after coroutine finishes.
    /// </summary>
    public Action CompletionHandler { get; set; }

    /// <summary>
    /// Advance this coroutine. Returns <see langword="true"/> if coroutine is still active and should be ticked next frame.
    /// </summary>
    public bool Tick(float dt = 1.0f / 60) {
        if (!IsRunning)
            return false;

        // wait for current awaiter to finish
        if (_awaiter is ICoroutineAwaiter awaiter && !awaiter.Tick(this, dt)) {
            return true;
        }

        // end of the internet
        if (!Enumerator.MoveNext()) {
            Stop();
            return false;
        }

        // assign some common awaiter shortcuts
        _awaiter = Enumerator.Current switch {
            null => new WaitForNextFrame(),
            float duration => new WaitForSeconds(duration),
            int durationWhole => new WaitForSeconds(durationWhole),
            ICoroutineAwaiter customAwaiter => customAwaiter,
            _ => throw new InvalidOperationException($"Invalid coroutine awaiter: expected null, int, float or ICoroutineAwaiter. ({_awaiter})")
        };

        return true;
    }

    public void Run(IEnumerator enumerator, Action completionHandler = default) {
        CompletionHandler = completionHandler;
        Enumerator = enumerator;
    }

    /// <summary>
    /// Immediately stops the coroutine and invokes completion handler.
    /// </summary>
    /// <param name="untrack"> Set to <see langword="true"/> to remove the coroutine from target tables. </param>
    public void Stop(bool invokeCompletionHandler = true, bool untrack = true) {
        if (invokeCompletionHandler) CompletionHandler?.Invoke();
        Enumerator = null;

        // remove from target pool ref
        if (untrack && Target is ICoroutineTarget target && CoroutineManager.TargetTable.TryGetValue(target, out var list)) {
            list.Remove(this);
        }
    }

    public void Reset() {
        CompletionHandler = null;
        ShouldRecycle = false;
        Enumerator = null;
        Tracked = false;

        // reset private fields
        _targetRef?.SetTarget(null);
        _awaiter = null;
    }

    bool ICoroutineAwaiter.Tick(CoroutineDriver coroutine, float dt) => !IsRunning;

    /// <summary>
    /// Shorthand for timer coroutines.
    /// </summary>
    public static IEnumerator Timer(float durationInSeconds, Action action, bool useTargetTimescale = true) {
        yield return new WaitForSeconds(durationInSeconds, useTargetTimescale);
        action.Invoke();
    }
}