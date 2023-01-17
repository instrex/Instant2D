using Instant2D.EC;
using Instant2D.Utils;
using Microsoft.Xna.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Instant2D.Coroutines {
    public class CoroutineManager : GameSystem {
        bool _isCurrentlyUpdating;

        internal readonly ConditionalWeakTable<ICoroutineTarget, List<Coroutine>> _targets = new();
        readonly HashSet<Coroutine> _stoppedCoroutines = new(12);
        readonly List<Coroutine> _activeCoroutines = new(24),
            _nextFrameBuffer = new(24);

        /// <summary>
        /// Runs a coroutine and returns its instance. Optionally, <paramref name="target"/> may be provided.
        /// </summary>
        public Coroutine Run(IEnumerator enumerator, ICoroutineTarget target = default) {
            var container = new Coroutine();

            // assign enumerator and append to target
            container._enumerator = enumerator;
            if (target != null) {
                container.Target = target;
                AppendToTarget(target, container);
            }

            // if ran during the update, use the next frame buffer instead
            (_isCurrentlyUpdating ? _nextFrameBuffer : _activeCoroutines).Add(container);
            return container;
        }

        public void StopAll(ICoroutineTarget target) {
            if (_targets.TryGetValue(target, out var list)) {
                foreach (var coroutine in list) {
                    // ignore unregistering from targets since we're iterating the list
                    coroutine.Stop(unregisterFromTargets: false);
                }

                _targets.Remove(target);
                list.Pool();
            }
        }

        #region Timers

        /// <summary>
        /// Run a simple timer which may repeat if you return <see langword="true"/> in <paramref name="handler"/>.
        /// </summary>
        public Coroutine Schedule<T>(float delay, T context, Func<T, bool> handler) => Run(TimerCoroutine(delay, false, context, handler));

        /// <summary>
        /// Run a simple timer which may repeat if you return <see langword="true"/> in <paramref name="handler"/>.
        /// </summary>
        public Coroutine Schedule(float delay, Action action) =>
            Schedule(delay, action, act => {
                act?.Invoke();
                return false;
            });

        // used for Schedule
        static IEnumerator TimerCoroutine<T>(float duration, bool ignoreTimeScale, T context, Func<T, bool> handler) {
            do {
                // continuosly invoke the callback until it returns false
                yield return new WaitForSeconds(duration, ignoreTimeScale);
            } while (handler(context));
        }

        #endregion

        internal void Stop(Coroutine coroutine, bool detachFromTarget = true) {
            _stoppedCoroutines.Add(coroutine);
            _nextFrameBuffer.Remove(coroutine);
            if (detachFromTarget) RemoveFromTarget(coroutine.Target, coroutine);
        }

        #region GameSystem implementation

        public static CoroutineManager Instance { get; private set; }

        public override void Initialize() {
            IsUpdatable = true;
            Instance = this;
        }

        public override void Update(GameTime gameTime) {
            var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            _isCurrentlyUpdating = true;
            for (var i = _activeCoroutines.Count - 1; i >= 0; i--) {
                var coroutine = _activeCoroutines[i];

                // coroutine was stopped
                if (_stoppedCoroutines.Contains(coroutine)) {
                    _stoppedCoroutines.Remove(coroutine);
                    continue;
                }

                // coroutine has finished execution naturally,
                // trigger the completion handler and put it back into the pool
                if (!coroutine.Tick(dt)) {
                    coroutine._completionHandler?.Invoke(coroutine);

                    // clear the target
                    RemoveFromTarget(coroutine.Target, coroutine);
                    if (coroutine._shouldRecycle) {
                        // return coroutine to the pool 
                        Pool<Coroutine>.Shared.Return(coroutine);
                    }

                    continue;
                }

                _nextFrameBuffer.Add(coroutine);
            }

            _activeCoroutines.Clear();

            // push next frame coroutines to the active stack
            _activeCoroutines.AddRange(_nextFrameBuffer);
            _nextFrameBuffer.Clear();

            _isCurrentlyUpdating = false;
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

        #endregion
    }
}
