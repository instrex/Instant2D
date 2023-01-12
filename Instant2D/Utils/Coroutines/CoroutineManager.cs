using Instant2D.EC;
using Instant2D.Utils;
using Microsoft.Xna.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Instant2D.Coroutines {
    public class NewCoroutineManager : GameSystem {
        bool _isCurrentlyUpdating;

        internal readonly ConditionalWeakTable<ICoroutineTarget, List<Coroutine>> _targets = new();
        readonly List<Coroutine> _activeCoroutines = new(24),
            _nextFrameBuffer = new(24);

        public void Run(IEnumerator enumerator, ICoroutineTarget target, Coroutine container) {
            // if container is already running something,
            // stop it so we could replace with a new coroutine
            if (container._enumerator != null) {
                container.Stop();
            }

            container.Reset();

            // assign enumerator and append to target
            container._enumerator = enumerator;
            if (target != null) {
                container.Target = target;
                AppendToTarget(target, container);
            }

            // if ran during the update, use the next frame buffer instead
            (_isCurrentlyUpdating ? _nextFrameBuffer : _activeCoroutines).Add(container);
        }

        public void Update() {
            _isCurrentlyUpdating = true;
            for (var i = _activeCoroutines.Count - 1; i >= 0; i++) {
                var coroutine = _activeCoroutines[i];
                
                // coroutine was either stopped or something weird happened
                if (coroutine._enumerator == null) {
                    RemoveFromTarget(coroutine.Target, coroutine);
                    continue;
                }

                // coroutine has finished execution naturally,
                // trigger the completion handler and put it back into the pool
                if (!coroutine.Tick(TimeManager.DeltaTime)) {
                    coroutine._completionHandler?.Invoke(coroutine);

                    // clear the target
                    RemoveFromTarget(coroutine.Target, coroutine);
                    if (coroutine._shouldRecycle) {
                        // return coroutine to the pool 
                        StaticPool<Coroutine>.Return(coroutine);
                    }

                    continue;
                }

                _nextFrameBuffer.Add(coroutine);
            }

            _activeCoroutines.Clear();

            // 
            _activeCoroutines.AddRange(_nextFrameBuffer);
            _nextFrameBuffer.Clear();

            _isCurrentlyUpdating = false;
        }

        internal void StopCoroutine(Coroutine coroutine) {

        }

        void AppendToTarget(ICoroutineTarget target, Coroutine coroutine) {
            _targets.GetValue(target, _ => ListPool<Coroutine>.Get())
                .Add(coroutine);
        }

        void RemoveFromTarget(ICoroutineTarget target, Coroutine coroutine) {
            if (target == null || !_targets.TryGetValue(target, out var list))
                return;

            list.Remove(coroutine);
            if (list.Count == 0) {
                _targets.Remove(target);
                list.Pool();
            }
        }
    }

    public class CoroutineManager : GameSystem {
        public static CoroutineManager Instance { get; private set; }

        public override void Initialize() {
            IsUpdatable = true;
            Instance = this;
        }

        internal static bool _anyEntityBlockedCoroutines;

        readonly ConditionalWeakTable<ICoroutineTarget, List<Coroutine>>
            // target to coroutines lookup, used to quickly stop everything when the object is destroyed
            _coroutineTargets = new();

        readonly List<Coroutine> _coroutines = new(),
            // coroutines blocked by global fixedupdate
            _blockedByFixedUpdate = new();


        public override void Update(GameTime time) {
            // tick all running coroutines
            for (var i = _coroutines.Count - 1; i >= 0; i--) {
                var coroutine = _coroutines[i];
                TickCoroutine(coroutine);
            }
        }

        #region Starter methods

        /// <summary>
        /// Begins executing of a coroutine and returns its instance, optionally attaching it to an object. <br/>
        /// You can provide your own <paramref name="coroutineObject"/> if you intend to reuse the instance.
        /// </summary>
        public static Coroutine Run(IEnumerator enumerator, ICoroutineTarget target = default, Coroutine coroutineObject = default) {
            if (coroutineObject is null) {
                // get the object from the pool when not provided
                coroutineObject = StaticPool<Coroutine>.Get();
                coroutineObject._shouldRecycle = true;
            } else {
                // stop already running routine first
                if (coroutineObject.IsRunning) coroutineObject.Stop();
                coroutineObject.Reset();
            }

            coroutineObject._enumerator = enumerator;

            // register the target
            if (target != null) {
                coroutineObject._target = new(target);
                Instance._coroutineTargets.GetValue(target, _ => ListPool<Coroutine>.Get())
                    .Add(coroutineObject);
            }

            // register coroutine
            Instance._coroutines.Add(coroutineObject);

            return coroutineObject;
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
        internal void RemoveCoroutine(Coroutine coroutine) {
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

            // remove from fixedupdate list too
            _blockedByFixedUpdate.Remove(coroutine);

            // return coroutine to the pool
            if (coroutine._shouldRecycle) {
                StaticPool<Coroutine>.Return(coroutine);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void BlockByFixedUpdate(Coroutine coroutine) {
            _blockedByFixedUpdate.Add(coroutine);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void TickFixedUpdate(Scene scene) {
            // if CoroutineManager wasn't initialized or no blocked coroutines currently active
            if (Instance == null || Instance._blockedByFixedUpdate is not List<Coroutine> { Count: >0 } blockedCoroutines)
                return;

            _anyEntityBlockedCoroutines = false;

            for (int i = blockedCoroutines.Count - 1; i >= 0; i--) {
                Coroutine coroutine = blockedCoroutines[i];

                // how did it get here ???
                if (coroutine._awaiter is not WaitForFixedUpdate(var ignoreEntityTimeScale) { _beganAtFixedUpdate: var beganAtFixedUpdate, _entity: var entity }) {
                    blockedCoroutines.RemoveAt(i);
                    continue;
                }

                // if fixedupdate number changed, that means this coroutine should advance
                if (beganAtFixedUpdate < (ignoreEntityTimeScale ? scene._fixedUpdatesPassed : (entity?._fixedUpdatesPassed ?? scene._fixedUpdatesPassed))) {
                    blockedCoroutines.RemoveAt(i);

                    coroutine._awaiter = null;
                    Instance.TickCoroutine(coroutine);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void TickCoroutine(Coroutine coroutine) {
            if (!coroutine.Tick(TimeManager.DeltaTime)) {
                coroutine._completionHandler?.Invoke(coroutine);
                RemoveCoroutine(coroutine);
            }
        }
    }
}
