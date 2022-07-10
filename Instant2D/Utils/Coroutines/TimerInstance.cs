using Microsoft.Xna.Framework;
using System;
using System.Runtime.CompilerServices;

namespace Instant2D.Utils.Coroutines {
    /// <summary>
    /// Represents a basic timer which invokes <see cref="callback"/> after <see cref="duration"/> seconds. <br/>
    /// Can also have <see cref="ICoroutineTarget"/> attached to tweak the timescale and stop when appropriate.
    /// </summary>
    public class TimerInstance : IPooled {
        public float time, duration;
        public ICoroutineTarget target;
        public object context;
        public bool shouldRepeat;
        public Action<TimerInstance> callback;
        public float? overrideTimeScale;

        bool _wasStopped;

        /// <summary>
        /// Advance this timer forward by some time affected by <see cref="ICoroutineTarget.TimeScale"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Tick(GameTime gameTime) {
            // stop the timer when the target becomes inactive
            if (_wasStopped || target?.IsActive == false) {
                return false;
            }

            // advance the timer by a scaled interval
            time += (float)gameTime.ElapsedGameTime.TotalSeconds * (overrideTimeScale ?? target?.TimeScale ?? 1.0f);

            if (time >= duration) {
                callback?.Invoke(this);
                if (!shouldRepeat) {
                    return false;
                }

                // go onto the second lap
                time = default;
            }

            return true;
        }

        /// <summary> Stops the execution of this timer, possibly invoking the callback. </summary>
        public void Stop(bool invokeCallback = false) {
            _wasStopped = true;
            if (invokeCallback) {
                callback?.Invoke(this);
            }
        }

        #region Setters

        /// <summary>
        /// Sets the optional <see cref="context"/> field which you can access from the callback.
        /// </summary>
        public TimerInstance WithContext(object context) {
            this.context = context;
            return this;
        }

        /// <summary>
        /// Sets the <see cref="target"/> used to obtain TimeScale information and stop the timer when the target goes inactive.
        /// </summary>
        public TimerInstance WithTarget(ICoroutineTarget target) {
            this.target = target;
            return this;
        }

        /// <summary>
        /// Sets <see cref="shouldRepeat"/> to <see langword="true"/>, making the timer restart upon reaching its duration. 
        /// Set the field to <see langword="false"/> during callback to prevent it from running again.
        /// </summary>
        public TimerInstance SetRepeat(bool repeat = true) {
            shouldRepeat = repeat;
            return this;
        }

        /// <summary>
        /// Sets the individual TimeScale for this timer. Useful for when you need an Entity as a target but want the timer to not slow down when the whole scene or the entity slows down.
        /// </summary>
        public TimerInstance SetOverrideTimeScale(float? value) {
            overrideTimeScale = value;
            return this;
        }

        #endregion

        public void Reset() {
            _wasStopped = false;
            duration = time = 0;
            shouldRepeat = false;
            overrideTimeScale = null;
            callback = null;
            context = null;
            target = null;
        }
    }
}
