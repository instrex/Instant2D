using Instant2D.Core;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D.Utils.Coroutines {
    public class CoroutineManager : SubSystem {
        static readonly List<TimerInstance> _timers = new();

        /// <summary>
        /// Schedules a timer to invoke the callback at an interval. Use <see cref="TimerInstance.WithContext(object)"/> to provide information for the callback. <br/>
        /// If <see cref="TimerInstance.WithTarget(ICoroutineTarget)"/> is used, the timer will stop when <see cref="ICoroutineTarget.IsActive"/> returns <see langword="false"/>.
        /// </summary>
        public static TimerInstance Schedule(float duration, Action<TimerInstance> callback) {
            var timer = StaticPool<TimerInstance>.Get();
            timer.duration = duration;
            timer.callback = callback;

            // add the timer to active list
            _timers.Add(timer);

            return timer;
        }

        public override void Initialize() {
            IsUpdatable = true;
        }

        public override void Update(GameTime time) {
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
