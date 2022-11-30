using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D.Coroutines {
    public static class Extensions {
        /// <summary>
        /// Schedules a timer, automatically calling <see cref="TimerInstance.SetTarget(ICoroutineTarget)"/> as current target.
        /// </summary>
        public static TimerInstance Schedule(this ICoroutineTarget target, float duration, Action<TimerInstance> callback) =>
            CoroutineManager.Schedule(duration, callback, target);

        /// <summary>
        /// Runs a coroutine, automatically calling <see cref="CoroutineManager.SetTarget(ICoroutineTarget)"/> as current target.
        /// </summary>
        public static CoroutineInstance RunCoroutine(this ICoroutineTarget target, IEnumerator enumerator, Action<CoroutineInstance, bool> completionHandler = default) =>
            CoroutineManager.Run(enumerator, completionHandler, target);
    }
}
