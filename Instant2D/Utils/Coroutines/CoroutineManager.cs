using Instant2D.Core;
using Instant2D.Utils;
using Microsoft.Xna.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D.Coroutines {
    public class CoroutineManager : SubSystem {
        /// <summary>
        /// Contains all coroutines sorted by target. Do not modify.
        /// </summary>
        public static readonly Dictionary<ICoroutineTarget, List<ICoroutineObject>> CoroutinesByTarget = new();

        static readonly List<TimerInstance> _timers = new();
        static readonly List<CoroutineInstance> _coroutines = new();

        /// <summary>
        /// Schedules a timer to invoke the callback at an interval. Use <see cref="TimerInstance.SetContext(object)"/> to provide information for the callback. <br/>
        /// If <see cref="TimerInstance.SetTarget(ICoroutineTarget)"/> is used, the timer will stop when <see cref="ICoroutineTarget.IsActive"/> returns <see langword="false"/>.
        /// </summary>
        public static TimerInstance Schedule(float duration, Action<TimerInstance> callback, ICoroutineTarget target = default) {
            var timer = new TimerInstance {
                duration = duration,
                callback = callback,
                _target = target
            };

            // add the timer to active list
            _timers.Add(timer);

            RegisterToTarget(target, timer);

            return timer;
        }

        /// <summary>
        /// Runs an enumerator coroutine, optionally specifying <paramref name="completionHandler"/>. <br/>
        /// <see cref="CoroutineInstance.completionHandler"/> takes a <see cref="bool"/> as parameter, which signals if coroutine was stopped manually (<see langword="true"/>), 
        /// or it finished executing (<see langword="false"/>).
        /// </summary>
        public static CoroutineInstance Run(IEnumerator enumerator, Action<bool> completionHandler = default, ICoroutineTarget target = default) {
            var instance = new CoroutineInstance {
                completionHandler = completionHandler,
                enumerator = enumerator,
                _target = target
            };

            // active the coroutine
            _coroutines.Add(instance);

            RegisterToTarget(target, instance);

            return instance;
        }

        #region Target Tracking

        /// <summary>
        /// Stops all of the coroutines and timers assigned to <paramref name="target"/>.
        /// </summary>
        public static void StopByTarget(ICoroutineTarget target) {
            if (target != null && CoroutinesByTarget.TryGetValue(target, out var list)) {
                // stop all the coroutines
                for (var i = 0; i < list.Count; i++) {
                    list[i].Stop();
                }

                // clear out the key
                ListPool<ICoroutineObject>.Return(list);
                CoroutinesByTarget.Remove(target);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void RemoveFromTarget(ICoroutineTarget target, ICoroutineObject obj) {
            if (target == null || !CoroutinesByTarget.TryGetValue(target, out var list))
                return;

            if (list.Remove(obj) && list.Count == 0) {
                // if the list is empty now, return it
                ListPool<ICoroutineObject>.Return(list);
                CoroutinesByTarget.Remove(target);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void RegisterToTarget(ICoroutineTarget target, ICoroutineObject obj) {
            if (target == null)
                return;

            // make a new list if target wasn't registered yet
            if (!CoroutinesByTarget.TryGetValue(target, out var list)) {
                list = ListPool<ICoroutineObject>.Get();
            }

            list.Add(obj);
        }

        #endregion

        public override void Initialize() {
            IsUpdatable = true;
        }

        public override void Update(GameTime time) {
            // tick the coroutines
            for (var i = _coroutines.Count - 1; i >= 0; i--) {
                var coroutine = _coroutines[i];

                // remove the coroutine when it's done
                if (!coroutine.Tick(time)) {
                    coroutine._isRunning = false;
                    _coroutines.RemoveAt(i);

                    // clear the target
                    RemoveFromTarget(coroutine._target, coroutine);
                }
            }

            // tick the timers
            for (var i = _timers.Count - 1; i >= 0; i--) {
                var timer = _timers[i];

                // remove the timer when it's due
                if (!timer.Tick(time)) {
                    _timers.RemoveAt(i);

                    // clear the target
                    RemoveFromTarget(timer._target, timer);
                }
            }
        }
    }
}
