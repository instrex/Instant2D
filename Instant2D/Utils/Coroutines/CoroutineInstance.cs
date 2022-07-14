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
    /// <summary>
    /// An instance of running coroutine. A coroutine is a function which can run during multiple frames, allowing you to spread tasks across update frames. <br/>
    /// This may include awaiting for a player to pick up an item, changing the color until it's transparent and etc. <br/>
    /// You can control the flow by yielding values of different types:
    /// <list type="bullet">
    /// <item> <see cref="int"/>/<see cref="float"/> - the coroutine will wait for specified time period in seconds.  </item>
    /// <item> <see langword="null"/> - the coroutine will wait for the next frame. </item>
    /// <item> <see cref="ICoroutineObject"/> - the coroutine will wait for the nested coroutines to finish. <br/> 
    /// This includes values of type <see cref="TimerInstance"/> and <see cref="CoroutineInstance"/>, returned by <see cref="CoroutineManager"/>. </item>
    /// <item> <see cref="IEnumerator"/> - same as <see cref="ICoroutineObject"/>, but will start the coroutine using <see cref="CoroutineManager.Run(IEnumerator)"/>. <br/>
    /// Newly created coroutine would also inherit <see cref="target"/> (if not null).</item>
    /// </list>
    /// </summary>
    public class CoroutineInstance : IPooled, ICoroutineObject {
        public IEnumerator coroutine;
        public ICoroutineTarget target;
        public float? overrideTimeScale;
        public Action<bool> completionHandler;
        float _waitDuration, _timer;
        ICoroutineObject _waitForObject;
        bool _wasStopped, _isRunning = true;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Tick(GameTime gameTime) {
            if (_wasStopped || target?.IsActive == false) { 
                return false;
            }

            // if waiting is in progress,
            // don't tick the coroutine
            if (_waitDuration > 0) {
                _timer += (float)gameTime.ElapsedGameTime.TotalSeconds * (overrideTimeScale ?? target?.TimeScale ?? 1.0f);
                if (_timer < _waitDuration) {
                    return true;
                }

                _waitDuration = 0;
            }

            // wait for other coroutines
            if (_waitForObject != null) {
                if (_waitForObject.IsRunning) {
                    return true;
                }

                _waitForObject = null;
            }

            // cache the enumeration state in a variable
            // for later use and ICoroutineObject
            if (!(_isRunning = coroutine.MoveNext())) {
                completionHandler?.Invoke(false);
                return false;
            }

            switch (coroutine.Current) {
                default: throw new InvalidOperationException($"Yielding {coroutine.Current.GetType().Name} in coroutines is not allowed.");

                // enumerator was passed, start a new coroutine and wait for it
                // in case this instance has a target defined, pass it to the resulting coroutine
                case IEnumerator enumerator:
                    _waitForObject = CoroutineManager.Run(enumerator)
                        .SetTarget(target);
                    break;

                // if other coroutine object is passed,
                // this one will wait until !_waitForObject.IsRunning
                case ICoroutineObject coroutine:
                    _waitForObject = coroutine;
                    break;

                // if float or int is passed, set the waiting timer
                // awaited coroutines will wait for specified amount of seconds
                case float waitTimeInSeconds:
                    _waitDuration = waitTimeInSeconds;
                    _timer = 0;
                    break;

                case int waitTimeInSeconds:
                    _waitDuration = waitTimeInSeconds;
                    _timer = 0;
                    break;

                // if null was passed, simply wait for the next frame
                case null: break;
            }

            return true;
        }

        /// <summary>
        /// Stops execution of this coroutine, firing off <see cref="completionHandler"/>.
        /// </summary>
        public void Stop() {
            _wasStopped = true;
            completionHandler?.Invoke(true);
        }

        #region Setters

        /// <summary>
        /// Sets an optional event handler to trigger when this coroutine is completed or stopped. <br/>
        /// Takes <see cref="bool"/> as parameter, which signals if the coroutine was stopped manually.
        /// </summary>
        public CoroutineInstance SetCompletionHandler(Action<bool> handler) {
            completionHandler = handler;
            return this;
        }

        /// <summary>
        /// Sets the individual TimeScale for this coroutine. Useful for when you need an Entity as a target but want the coroutine to not slow down when the whole scene or the entity slows down.
        /// </summary>
        public CoroutineInstance SetOverrideTimeScale(float? value) {
            overrideTimeScale = value;
            return this;
        }

        /// <summary>
        /// Sets the <see cref="target"/> used to obtain TimeScale information and stop the coroutine when the target goes inactive.
        /// </summary>
        public CoroutineInstance SetTarget(ICoroutineTarget target) {
            this.target = target;
            return this;
        }

        #endregion

        public void Reset() {
            completionHandler = null;
            _waitForObject = null;
            target = null;
            overrideTimeScale = null;
            _waitDuration = 0;
            _timer = 0;
            _wasStopped = false;
            _isRunning = true;
        }

        // ICoroutineObject impl
        bool ICoroutineObject.IsRunning => _isRunning;
        ICoroutineTarget ICoroutineObject.Target => target;
        void ICoroutineObject.Stop() => Stop();
    }
}
