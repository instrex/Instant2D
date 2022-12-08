using Instant2D.Core;
using Instant2D.Utils;
using Microsoft.Xna.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Instant2D.Coroutines {
    public class CoroutineManager : SubSystem {
        public static CoroutineManager Instance { get; private set; }

        public override void Initialize() {
            IsUpdatable = true;
            Instance = this;
        }

        static internal bool HasObjectsBlockedByNonGlobalTimeScale;

        internal readonly ConditionalWeakTable<ICoroutineTarget, List<Coroutine>>
            // stores coroutines blocked by fixed update tied to individual objects
            _blockedByObjectFixedUpdates = new(),

            // target to coroutines lookup, used to quickly stop everything if the object destroys
            _coroutineTargets = new();

        readonly List<Coroutine>
            _coroutines = new(),

            // coroutines that should be removed next frame
            _markedForDeletion = new(),

            // coroutines blocked by global fixedupdate
            _blockedByFixedUpdate = new();

        public override void Update(GameTime time) {
            HasObjectsBlockedByNonGlobalTimeScale = false;

            // clear all marked coroutines
            if (_markedForDeletion.Count > 0) {
                for (int i = 0; i < _markedForDeletion.Count; i++) {
                    Coroutine coroutine = _markedForDeletion[i];
                    _coroutines.Remove(coroutine);

                    var target = coroutine.Target;

                    // unassign target
                    if (target != null && _coroutineTargets.TryGetValue(target, out var list)) {
                        list.Remove(coroutine);

                        if (list.Count == 0) {
                            // pool the list and remove it when it's empty
                            _coroutineTargets.Remove(coroutine.Target);
                            list.Pool();
                        }
                    }

                    // return coroutine to the pool
                    StaticPool<Coroutine>.Return(coroutine);
                }

                // clear the buffer
                _markedForDeletion.Clear();
            }

            // tick all running coroutines
            for (var i = 0; i < _coroutines.Count; i++) {
                var coroutine = _coroutines[i];
                TickCoroutine(coroutine);
            }
        }

        #region Starter methods

        /// <summary>
        /// Begins executing of a coroutine and returns its instance, optionally attaching it to an object.
        /// </summary>
        public static Coroutine Run(IEnumerator enumerator, ICoroutineTarget target = default) {
            var coroutine = StaticPool<Coroutine>.Get();
            coroutine._enumerator = enumerator;

            // register the target
            if (target != null) {
                coroutine._target = new(target);
                Instance._coroutineTargets.GetValue(target, _ => ListPool<Coroutine>.Get())
                    .Add(coroutine);
            }

            // register coroutine
            Instance._coroutines.Add(coroutine);

            return coroutine;
        }

        /// <summary>
        /// Starts a simple timer with specified <paramref name="handler"/>. Use <see cref="Coroutine.Stop"/> to interrupt it before completion.
        /// </summary>
        public static Coroutine Schedule(float delay, Action handler) =>
            Run(SimpleTimer(delay, true, handler));

        /// <inheritdoc cref="Schedule(float, Action)"/>
        public static Coroutine Schedule(float delay, bool ignoreTimescale, ICoroutineTarget target, Action handler) =>
            Run(SimpleTimer(delay, ignoreTimescale, handler), target);

        /// <summary>
        /// Starts an optionally looping timer with specified <paramref name="handler"/> and attached <paramref name="context"/>. Return <see langword="true"/> inside <paramref name="handler"/> to continue looping.
        /// </summary>
        public static Coroutine Schedule<T>(float delay, T context, Func<T, bool> handler) =>
            Run(AdvancedTimer(delay, true, context, handler));

        /// <inheritdoc cref="Schedule{T}(float, T, Func{T, bool})"/>
        public static Coroutine Schedule<T>(float delay, bool ignoreTimescale, ICoroutineTarget target, T context, Func<T, bool> handler) =>
            Run(AdvancedTimer(delay, ignoreTimescale, context, handler), target);

        #endregion

        /// <summary>
        /// Manually stop all coroutines using specified target.
        /// </summary>
        public static void StopAll(ICoroutineTarget target) {
            if (Instance._coroutineTargets.TryGetValue(target, out var list)) {
                foreach (var coroutine in list) 
                    coroutine.Stop();

                // clear the target
                Instance._coroutineTargets.Remove(target);
                list.Pool();
            }
        }

        // used in Schedule, simple timer without looping or context
        static IEnumerator SimpleTimer(float duration, bool ignoreTimeScale, Action result) {
            yield return new WaitForSeconds(duration, ignoreTimeScale);
            result();
        }

        // used in Schedule, advanced timer with context and looping support
        static IEnumerator AdvancedTimer<T>(float duration, bool ignoreTimeScale, T context, Func<T, bool> handler) {
            do {
                // continuosly invoke the callback until it returns false
                yield return new WaitForSeconds(duration, ignoreTimeScale);
            } while (handler(context));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void BlockByFixedUpdate(Coroutine coroutine, bool useOwnTimescale) {
            // we can ignore individual timescales if it's actually 1.0f or coroutine doesn't have it at all
            if (useOwnTimescale && coroutine.Target != null && coroutine.Target.TimeScale != 1.0f) {
                _blockedByObjectFixedUpdates.GetValue(coroutine.Target, _ => ListPool<Coroutine>.Get()).Add(coroutine);
                HasObjectsBlockedByNonGlobalTimeScale = true;
                return;
            }

            _blockedByFixedUpdate.Add(coroutine);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void TickFixedUpdateGlobal() {
            if (_blockedByFixedUpdate.Count == 0) return;
            for (int i = _blockedByFixedUpdate.Count - 1; i >= 0; i--) {
                Coroutine coroutine = _blockedByFixedUpdate[i];
                if (coroutine._awaiter is not WaitForFixedUpdate(false))
                    continue;

                // clear the block
                _blockedByFixedUpdate.RemoveAt(i);

                // advance the routine
                coroutine._awaiter = null;
                TickCoroutine(coroutine);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void TickFixedUpdate(ICoroutineTarget target) {
            if (!HasObjectsBlockedByNonGlobalTimeScale) return;
            if (_blockedByObjectFixedUpdates.TryGetValue(target, out var list)) {
                for (int i = list.Count - 1; i >= 0; i--) {
                    Coroutine coroutine = list[i];
                    list.RemoveAt(i);

                    // advance
                    coroutine._awaiter = null;
                    TickCoroutine(coroutine);
                }

                // pool the list when not needed anymore
                _blockedByObjectFixedUpdates.Remove(target);
                list.Pool();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void TickCoroutine(Coroutine coroutine) {
            if (!coroutine.Tick(TimeManager.DeltaTime)) {
                coroutine._completionHandler?.Invoke(coroutine);
                _markedForDeletion.Add(coroutine);
            }
        }
    }
}
