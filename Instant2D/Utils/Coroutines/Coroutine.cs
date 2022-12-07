using Instant2D.Coroutines;
using Instant2D.Utils;
using System;
using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D.Coroutines {
    /// <summary>
    /// Coroutine instance. Avoid storing references or null them out on completion, as they are pooled and may be replaced by another completely irrelevant coroutine.
    /// </summary>
    public class Coroutine : IPooled {
        internal WeakReference<ICoroutineTarget> _target;
        internal IEnumerator _enumerator;
        internal object _awaiter;

        float _waitTimer;

        /// <summary>
        /// Is <see langword="true"/> when coroutine is still executing.
        /// </summary>
        public bool IsRunning => _enumerator != null;

        /// <summary>
        /// Target object of this coroutine.
        /// </summary>
        public ICoroutineTarget Target {
            get {
                if (_target == null || !_target.TryGetTarget(out var target))
                    return null;

                return target;
            }
        }

        /// <summary>
        /// Manually stops coroutine before its completion.
        /// </summary>
        public void Stop() {
            _enumerator = null;
        }

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

            // handle different awaiters
            if (_awaiter != null) {
                switch (_awaiter) {
                    default:
                        Logger.WriteLine($"Unknown coroutine awaiter: {_awaiter?.GetType()}", Logger.Severity.Warning);
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

                    // will be ticked by scene/object automatically
                    case WaitForFixedUpdate(bool useIndividualTimescale):
                        if (useIndividualTimescale) {
                            // this is required since when objects switch to global timescale, 
                            // the method used to tick them will never be called
                            if (timeScale == 1.0f) {
                                if (_target.TryGetTarget(out var target) && CoroutineManager.Instance._blockedByObjectFixedUpdates.TryGetValue(target, out var blockedList)) {
                                    blockedList.Remove(this);
                                }

                                // switch back to global
                                _awaiter = new WaitForFixedUpdate();
                                return true;
                            }

                            // i set this to avoid doing too much work when it's not needed
                            CoroutineManager.HasObjectsBlockedByNonGlobalTimeScale = true;
                        }

                        return true;
                }
            }

            // yield break was called or end has been reached
            if (!_enumerator.MoveNext()) {
                _enumerator = null;
                return false;
            }

            var yield = _enumerator.Current;
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

                case WaitForFixedUpdate(bool useIndividualTimescale):
                    CoroutineManager.Instance.BlockByFixedUpdate(this, useIndividualTimescale);
                    break;
            }

            return true;
        }

        public void Reset() {
            _target = null;
            _enumerator = null;
            _awaiter = null;
            _waitTimer = 0;
        }
    }
}
