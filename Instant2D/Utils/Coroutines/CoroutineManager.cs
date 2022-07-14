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
        static readonly List<TimerInstance> _timers = new();
        static readonly List<CoroutineInstance> _coroutines = new();

        /// <summary>
        /// Schedules a timer to invoke the callback at an interval. Use <see cref="TimerInstance.SetContext(object)"/> to provide information for the callback. <br/>
        /// If <see cref="TimerInstance.SetTarget(ICoroutineTarget)"/> is used, the timer will stop when <see cref="ICoroutineTarget.IsActive"/> returns <see langword="false"/>.
        /// </summary>
        public static TimerInstance Schedule(float duration, Action<TimerInstance> callback) {
            var timer = StaticPool<TimerInstance>.Get();
            timer.duration = duration;
            timer.callback = callback;

            // add the timer to active list
            _timers.Add(timer);

            return timer;
        }

        /// <summary>
        /// Runs an enumerator coroutine, optionally specifying <paramref name="completionHandler"/>. <br/>
        /// <see cref="CoroutineInstance.completionHandler"/> takes a <see cref="bool"/> as parameter, which signals if coroutine was stopped manually (<see langword="true"/>), 
        /// or it finished executing (<see langword="false"/>).
        /// </summary>
        public static CoroutineInstance Run(IEnumerator enumerator, Action<bool> completionHandler = default) {
            var instance = StaticPool<CoroutineInstance>.Get();
            instance.completionHandler = completionHandler;
            instance.coroutine = enumerator;

            // active the coroutine
            _coroutines.Add(instance);

            return instance;
        }

        public override void Initialize() {
            IsUpdatable = true;
        }

        public override void Update(GameTime time) {
            // tick the coroutines
            for (var i = _coroutines.Count - 1; i >= 0; i--) {
                var coroutine = _coroutines[i];

                // remove the coroutine when it's done
                if (!coroutine.Tick(time)) {
                    StaticPool<CoroutineInstance>.Return(coroutine);
                    _coroutines.RemoveAt(i);
                }
            }

            // tick the timers
            for (var i = _timers.Count - 1; i >= 0; i--) {
                var timer = _timers[i];

                // remove the timer when it's due
                if (!timer.Tick(time)) {
                    StaticPool<TimerInstance>.Return(timer);
                    _timers.RemoveAt(i);
                }
            }
        }
    }
}
