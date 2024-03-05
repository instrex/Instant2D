using Instant2D.Modules;
using Instant2D.Utils;
using Microsoft.Xna.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Instant2D.Coroutines;

/// <summary>
/// A class to process coroutines. 
/// </summary>
public class CoroutineManager : IGameSystem {

    #region Target Handling

    internal static ConditionalWeakTable<ICoroutineTarget, List<CoroutineDriver>> TargetTable = new();
    internal static void AttachToTarget(ICoroutineTarget target, CoroutineDriver driver) {
        if (target is null)
            return;

        // init the pool
        if (!TargetTable.TryGetValue(target, out var list)) {
            list = ListPool<CoroutineDriver>.Rent();
            TargetTable.Add(target, list);
        }

        list.Add(driver);
    }
    
    /// <summary>
    /// Stops all running coroutines with <paramref name="target"/>.
    /// </summary>
    public static void StopByTarget(ICoroutineTarget target) {
        if (!TargetTable.TryGetValue(target, out var list))
            return;

        foreach (var coroutine in list) {
            coroutine.Stop(untrack: false);
        }

        // recycle the list
        TargetTable.Remove(target);
        list.Pool();
    }

    #endregion

    static readonly List<CoroutineDriver> _coroutines = new(32);

    /// <summary>
    /// Run pooled coroutine that will be recycled.
    /// </summary>
    /// <param name="completionHandler"> Optional action to trigger when it finishes. </param>
    public static void Run(IEnumerator enumerator, ICoroutineTarget target = default, Action completionHandler = default) {
        var driver = Pool<CoroutineDriver>.Shared.Rent();
        driver.ShouldRecycle = true;
        driver.Target = target;
        driver.Tracked = true;
        driver.Run(enumerator, completionHandler);
        _coroutines.Add(driver);
    }

    /// <summary>
    /// Run tracked coroutine that won't be recycled.
    /// </summary>
    public static CoroutineDriver RunTracked(IEnumerator enumerator, ICoroutineTarget target = default, Action completionHandler = default) {
        var driver = new CoroutineDriver { 
            Target = target,
            Tracked = true, 
        };

        driver.Run(enumerator, completionHandler);
        _coroutines.Add(driver);

        return driver;
    }

    public static void Tick(float dt = 1.0f / 60) {
        for (var i = _coroutines.Count - 1; i >= 0; i--) {
            var driver = _coroutines[i];
            if (driver.Tick(dt))
                continue;

            _coroutines.RemoveAt(i);
            if (driver.ShouldRecycle) {
                driver.Pool();
            }
        }
    }

    float IGameSystem.UpdateOrder { get; }
    void IGameSystem.Initialize(InstantApp app) { }
    void IGameSystem.Update(InstantApp app, float deltaTime) => Tick(deltaTime);
}