using Instant2D.EC;
using Instant2D.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D.Coroutines;

/// <summary>
/// Provides a way to run continous tasks via the <see cref="IEnumerator{T}"/> feature. <br/>
/// Each coroutine may have <see cref="ICoroutineTarget"/> attached to it in order to bind it to an object. Checks the class documentation for more info. <br/>
/// There are two ways to use coroutines:
/// <list type="bullet">
///     <item> <b>Automatic</b>: coroutine instances obtained through <see cref="CoroutineManager"/>.Start() functions will automatically tick each frame. </item>
///     <item> <b>Custom</b>: created coroutine instances will have to be manually ticked anywhere in your game loop. This allows you to synchronize specific coroutines to your custom gameplay logic. </item>
/// </list>
/// </summary>
public partial class Coroutine(ICoroutineTarget target = default) { 
    readonly WeakReference<ICoroutineTarget> _targetRef = new(target ?? InstantApp.Instance.GetModule<CoroutineManager>());
    IEnumerator<ICoroutineAwaitable> _enumerator;
    bool _isAttachedToTarget;

    /// <summary>
    /// Indicates if this coroutine is currently executing.
    /// </summary>
    public bool IsRunning => _enumerator != null;

    /// <summary>
    /// Awaitable this coroutine is currently blocked by.
    /// </summary>
    public ICoroutineAwaitable CurrentAwaitable { get; private set; }

    /// <summary>
    /// Allows Coroutines to be attached to an object and use its timescale and other properties, as well as stop when object gets destroyed.
    /// </summary>
    public ICoroutineTarget Target {
        // if target ref is unset or destroyed, return null
        get => _targetRef.TryGetTarget(out var target) && target is not CoroutineManager ? target : null;

        set {
            if (IsRunning) {
                InstantApp.Logger.Warn($"Attempted to change Coroutine target object during its execution. ({this})");
                return;
            }

            //// remove this coroutine reference from old target table
            //if (_isAttachedToTarget && _targetRef.TryGetTarget(out var oldTarget))
            //    DetachFrom(oldTarget);

            //if (value != null && IsRunning) {
            //    _isAttachedToTarget = true;
            //    AttachTo(value);
            //}

            // set target
            _targetRef.SetTarget(value ?? InstantApp.Instance.GetModule<CoroutineManager>());
        }
    }

    // adds coroutine to target table
    void AttachTo(ICoroutineTarget target) {
        if (target is null)
            return;

        // initialize the list if it doesn't exist yet
        if (!_coroutinesByTarget.TryGetValue(target, out var list)) {
            list = ListPool<Coroutine>.Rent();
            _coroutinesByTarget.Add(target, list);
        }

        // add coroutine to target table
        list.Add(this);
    }

    // removes coroutine and handles removing the list
    void DetachFrom(ICoroutineTarget target) {
        if (target is null)
            return;

        if (!_coroutinesByTarget.TryGetValue(target, out var list))
            return;

        list.Remove(this);
    }

    /// <summary>
    /// Sets this coroutine to scale time based on <see cref="Target"/>.
    /// </summary>
    public bool UseTargetTimescale { get; set; } = true;

    /// <summary>
    /// An optional delegate that will be triggered on coroutine completion.
    /// </summary>
    public Action CompletionHandler { get; set; }

    /// <summary>
    /// Starts the coroutine with specified enumerator. <br/>
    /// NOTE: this function will not automatically tick coroutines via <see cref="CoroutineManager"/>, use <see cref="CoroutineManager.Start(Coroutine, IEnumerator{ICoroutineAwaitable})"/>
    /// if you wish to reuse this coroutine instance.
    /// </summary>
    public Coroutine Start(IEnumerator<ICoroutineAwaitable> enumerator) {
        _enumerator = enumerator;
        _isAttachedToTarget = true;
        AttachTo(Target);
        return this;
    }

    /// <summary>
    /// Attempt to advance the coroutine, potentially resolving blocking awaitables.
    /// </summary>
    public bool Tick() {
        if (_enumerator == null) {
            return false;
        }

        // wait for current awaitable
        if (CurrentAwaitable != null && !CurrentAwaitable.Tick(this))
            return true;

        // advance the coroutine
        if (!_enumerator.MoveNext()) {
            Stop();
            return false;
        }

        // set and init awaitable
        CurrentAwaitable = _enumerator.Current;
        CurrentAwaitable?.Initialize(this);

        return true;
    }

    /// <summary>
    /// Halt coroutine's execution and call its completion handlers.
    /// </summary>
    public void Stop(bool triggerCompletionHandlers = true) {
        if (_isAttachedToTarget) {
            _isAttachedToTarget = false;
            DetachFrom(Target);
        }

        if (!IsRunning)
            return;

        if (triggerCompletionHandlers) CompletionHandler?.Invoke();
        if (CurrentAwaitable is WaitForSceneEvent waitForSceneEvent) {
            if (_sceneBlockedCoroutines.TryGetValue(waitForSceneEvent.EventType, out var list)) {
                // pool and clear the slot if last
                if (list.Count == 1) {
                    _sceneBlockedCoroutines.Remove(waitForSceneEvent.EventType);
                    list.Pool();

                    // else, just remove this coroutine
                } else list.Remove(this);
            }
        }

        _enumerator = null;
    }

    public override string ToString() {
        var enumeratorStr = _enumerator?.ToString() ?? "<None>";
        return $"{Target?.ToString() ?? "NullTarget"}.{enumeratorStr[(enumeratorStr.IndexOf('<') + 1)..enumeratorStr.IndexOf('>')]}({GetHashCode():X6})";
    }

    #region Setters

    /// <inheritdoc cref="CompletionHandler"/>
    public Coroutine SetCompletionHandler(Action completionHandler) {
        CompletionHandler = completionHandler;
        return this;
    }

    /// <inheritdoc cref="UseTargetTimescale"/>
    public Coroutine SetUseTargetTimescale(bool useTargetTimescale) {
        UseTargetTimescale = useTargetTimescale;
        return this;
    }

    #endregion

    #region Global Target Pool

    static readonly ConditionalWeakTable<ICoroutineTarget, List<Coroutine>> _coroutinesByTarget = [];

    // globally blocked coroutines
    static readonly Dictionary<SceneLoopEvent, List<Coroutine>> _sceneBlockedCoroutines = [];

    // adds blocked coroutines into dictionary
    public static void RegisterSceneLoopBlockedCoroutine(Coroutine coroutine, SceneLoopEvent eventType) {
        if (!_sceneBlockedCoroutines.TryGetValue(eventType, out var list)) 
            _sceneBlockedCoroutines.Add(eventType, list = ListPool<Coroutine>.Rent());
        
        list.Add(coroutine);
    }


    public static void TickSceneLoopBlockedCoroutines(SceneLoopEvent type, Scene scene) {
        if (!_sceneBlockedCoroutines.TryGetValue(type, out var list))
            return;

        var buffer = ListPool<Coroutine>.Rent();
        buffer.AddRange(list);
        list.Clear();

        // iterate and tick all coroutines
        for (var i = 0; i < buffer.Count; i++) {
            var coroutine = buffer[i];
            if (coroutine.CurrentAwaitable is WaitForSceneEvent) {
                coroutine.CurrentAwaitable = null;
                coroutine.Tick();
            }
        }

        // clear the entry
        if (list.Count == 0) {
            _sceneBlockedCoroutines.Remove(type);
            list.Pool();
        }

        // return the buffer
        buffer.Pool();
    }

    /// <summary>
    /// Gets all active coroutines.
    /// </summary>
    public static List<Coroutine> GetCoroutines() {
        var list = ListPool<Coroutine>.Rent();
        list.AddRange(_coroutinesByTarget
            .SelectMany(e => e.Value));

        return list;
    }

    /// <summary>
    /// Gets all active coroutines with specified target.
    /// </summary>
    public static List<Coroutine> GetCoroutinesWithTarget(ICoroutineTarget target) {
        var list = GetCoroutines();
        list.RemoveAll(c => c.Target != target);

        return list;
    }

    /// <summary>
    /// Ends execution of all coroutines attached to <paramref name="target"/>.
    /// </summary>
    public static void StopAllWithTarget(ICoroutineTarget target, bool invokeCompletionHandlers = true) {
        if (!_coroutinesByTarget.TryGetValue(target, out var list))
            return;

        if (list.Count > 0) {
            // save entries into a temporary buffer
            var buffer = ListPool<Coroutine>.Rent();
            buffer.AddRange(list);

            // clear all entries from actual list before stopping
            list.Clear();

            // stop every entry coroutine
            foreach (var coroutine in buffer) coroutine.Stop(invokeCompletionHandlers);

            // dispose of the buffer
            buffer.Pool();
        }

        // check if the slot is actually empty now, then pool and remove it
        // this may not happen if any of the coroutines start new coroutines from their CompletionHandlers
        if (list.Count == 0) {
            _coroutinesByTarget.Remove(target);
            list.Pool();
        }
    }

    #endregion
}