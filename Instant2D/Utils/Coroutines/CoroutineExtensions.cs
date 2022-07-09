using System;

namespace Instant2D.Utils.Coroutines {
    public static class CoroutineExtensions {
        /// <summary>
        /// Schedules a timer, automatically calling <see cref="TimerInstance.WithTarget(ICoroutineTarget)"/> as current target.
        /// </summary>
        public static TimerInstance Schedule(this ICoroutineTarget target, float duration, Action<TimerInstance> callback) =>
            CoroutineManager.Schedule(duration, callback).WithTarget(target);
    }
}
