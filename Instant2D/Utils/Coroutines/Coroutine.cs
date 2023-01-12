using Instant2D.Coroutines;
using Instant2D.EC;
using Instant2D.Utils;
using Microsoft.Xna.Framework;
using System;
using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D.Coroutines {
    /// <summary>
    /// Coroutine instance. Avoid storing references or null them out on completion, as they are pooled and may be replaced by another completely irrelevant coroutine.
    /// </summary>
    public class Coroutine : IPooled {
        internal WeakReference<ICoroutineTarget> _target;
        internal Action<Coroutine> _completionHandler;
        internal object _awaiter, _context;
        internal IEnumerator _enumerator;
        internal bool _isPaused, _shouldRecycle;

        // internal timer used for WaitForSeconds
        float _waitTimer;

        /// <summary>
        /// Is <see langword="true"/> when coroutine is still executing.
        /// </summary>
        public bool IsRunning => _enumerator != null;

        /// <summary>
        /// When <see langword="true"/>, coroutine will remain 'running' but will wait until <see cref="Resume"/>.
        /// </summary>
        public bool IsPaused {
            get => _isPaused;
            set {
                if (value) Pause();
                else Resume();
            }
        }

        /// <summary>
        /// Target object of this coroutine.
        /// </summary>
        public ICoroutineTarget Target {
            get {
                if (_target == null || !_target.TryGetTarget(out var target))
                    return null;

                return target;
            }

            internal set {
                if (_target == null) {
                    _target = new(value);
                    return;
                }

                _target.SetTarget(value);
            }
        }

        /// <summary>
        /// Manually stops coroutine before its completion.
        /// </summary>
        public void Stop(bool invokeCompletionHandler = true) {
            _enumerator = null;
            _awaiter = null;

            if (invokeCompletionHandler) {
                // notify that coroutine has finished
                _completionHandler?.Invoke(this);
            }

            // remove this coroutine
            CoroutineManager.Instance.RemoveCoroutine(this);
        }

        /// <summary>
        /// Pauses the coroutine, suspending its execution until <see cref="Resume"/> is called.
        /// </summary>
        public Coroutine Pause() {
            _isPaused = true;
            return this;
        }
        
        /// <summary>
        /// Resumes execution of a paused coroutine.
        /// </summary>
        public Coroutine Resume() {
            _isPaused = false;
            return this;
        }

        /// <summary>
        /// Sets up a callback that will be triggered when this coroutine finishes execution.
        /// </summary>
        public Coroutine SetCompletionHandler(Action<Coroutine> handler) {
            _completionHandler = handler;
            return this;
        }

        /// <inheritdoc cref="SetCompletionHandler(Action{Coroutine})"/>
        public Coroutine SetCompletionHandler(object context, Action<Coroutine> handler) {
            _completionHandler = handler;
            _context = context;
            return this;
        }

        /// <summary>
        /// Sets optional context object used in conjuction with <see cref="SetCompletionHandler(Action{Coroutine})"/>.
        /// </summary>
        public Coroutine SetContext(object context) {
            _context = context;
            return this;
        }

        /// <summary>
        /// Gets untyped context object.
        /// </summary>
        public object Context => _context;

        /// <summary>
        /// Gets typed context object.
        /// </summary>
        public T GetContext<T>() => (T)_context;

        /// <summary>
        /// Advance the coroutine forward.
        /// </summary>
        public bool Tick(float dt) {
            var timeScale = 1.0f;
            
            if (_target != null) {
                // if the target is lost, stop the coroutine
                if (!_target.TryGetTarget(out var target))
                    return false;

                timeScale = target.TimeScale;
            }

            // coroutine was stopped
            if (_enumerator == null) {
                return false;
            }

            // we wait until it's unpaused
            if (_isPaused) {
                return true;
            }

            // handle different awaiters
            if (_awaiter != null) {
                switch (_awaiter) {
                    default:
                        InstantApp.Logger.Error($"Unknown coroutine awaiter: {_awaiter?.GetType()}");
                        break;

                    // custom awaiter function
                    case ICoroutineAwaitable customAwaiter:
                        if (customAwaiter.ShouldWait(this))
                            return true;

                        _awaiter = null;

                        break;

                    case WaitForSeconds(var duration, bool ignoreTimescale):
                        _waitTimer += dt * (ignoreTimescale ? 1.0f : timeScale);

                        // tick the routine next frame
                        if (_waitTimer < duration) {
                            return true;
                        }

                        // reset the awaiter
                        _awaiter = null;
                        _waitTimer = 0;

                        break;

                    // wait for another coroutine/tween/timer to finish
                    case WaitForCoroutine(Coroutine other):
                        if (other._enumerator != null)
                            return true;

                        _awaiter = null;
                        break;

                    // wait for the next frame
                    case WaitForUpdate:
                        _awaiter = null;
                        break;
                }
            }

            // yield break was called or end has been reached
            if (!_enumerator.MoveNext()) {
                _enumerator = null;
                return false;
            }

            var yield = _enumerator?.Current;
            switch (yield) {
                default: 
                    _awaiter = yield; 
                    break;

                case null:
                    _awaiter = new WaitForUpdate();
                    break;

                case Coroutine coroutine:
                    _awaiter = new WaitForCoroutine(coroutine);
                    break;

                //case WaitForFixedUpdate waitForFixedUpdate:
                //    if (_target == null || !_target.TryGetTarget(out var target)) {
                //        InstantApp.Logger.Warn("WaitForFixedUpdate may only be used when coroutine's target is set to Scene or Entity, skipping.");
                //        return true;
                //    }

                //    switch (target) {
                //        default:
                //            InstantApp.Logger.Warn("WaitForFixedUpdate may only be used when coroutine's target is set to Scene or Entity, skipping.");
                //            return true;

                //        case Scene scene:
                //            waitForFixedUpdate._beganAtFixedUpdate = scene._fixedUpdatesPassed;
                //            break;

                //        case Entity entity:
                //            waitForFixedUpdate._beganAtFixedUpdate = entity._fixedUpdatesPassed;
                //            waitForFixedUpdate._entity = entity;

                //            if (entity._timescale != 1.0f) {
                //                // mark that entities with non-global timescale should
                //                // try and tick the blocked coroutines as well
                //                CoroutineManager._anyEntityBlockedCoroutines = true;
                //            }

                //            break;
                //    }

                //    // mark the beginning update cycle and set the awaiter
                //    CoroutineManager.Instance.BlockByFixedUpdate(this);
                //    _awaiter = waitForFixedUpdate;

                //    break;
            }

            return true;
        }

        public void Reset() {
            _target?.SetTarget(null);
            _completionHandler = null;
            _enumerator = null;
            _isPaused = false;
            _context = null;
            _awaiter = null;
            _waitTimer = 0;
        }
    }
}
