using Instant2D.EC;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D.Coroutines {
    public static class Extensions {
        /// <summary>
        /// Begins a coroutine and automatically sets its target.
        /// </summary>
        public static Coroutine RunCoroutine(this ICoroutineTarget target, IEnumerator enumerator) {
            return CoroutineManager.Run(enumerator, target);
        }

        /// <summary>
        /// Schedules a timer that will end when the target dies.
        /// </summary>
        public static Coroutine Schedule(this ICoroutineTarget target, float delay, Action handler, bool ignoreTimescale = false) {
            return CoroutineManager.Schedule(delay, ignoreTimescale, target, handler);
        }
    }
}


