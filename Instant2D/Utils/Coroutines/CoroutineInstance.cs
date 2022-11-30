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
    /// This may include awaiting for a player to pick up an item, changing the color until it's transparent and etc. <br/> <br/>
    /// You can control the flow by yielding values of different types:
    /// <list type="bullet">
    /// 
    /// <item> <see cref="int"/>/<see cref="float"/> - the coroutine will wait for specified time period in seconds. </item>
    /// <code>
    /// yield return 0.5f; // wait for half a second
    /// </code>
    /// 
    /// <item> <see langword="null"/> - the coroutine will wait for the next frame. </item>
    /// <code>
    /// // increase frameCount by 1 each frame (FPS-dependent)
    /// while (frameCount &lt; 60) {
    ///     frameCount++;
    ///     yield return null; 
    /// } 
    /// </code>
    /// 
    /// <item> 
    /// <see cref="ICoroutineObject"/> - the coroutine will wait for the nested coroutines to finish. <br/> 
    /// This includes values of type <see cref="TimerInstance"/> and <see cref="CoroutineInstance"/>, returned by <see cref="CoroutineManager"/>. 
    /// </item>
    /// <code>
    /// yield return CoroutineManager.Schedule(0.5f, () => "Hello, world!");
    /// yield return Entity.RunCoroutine(Greet());
    /// </code>
    /// 
    /// <item> <see cref="IEnumerator"/> - same as <see cref="ICoroutineObject"/>, but will start the coroutine using <see cref="CoroutineManager.Run(IEnumerator)"/>. <br/>
    /// Newly created coroutine would also inherit <see cref="ICoroutineObject.Target"/> (if not null).
    /// </item>
    /// <code>
    /// yield return Greet(); // Greet() is IEnumerator Greet() { yield return null; }
    /// </code>
    /// 
    /// <item> <see langword="break"/> - stop the execution of this coroutine. Does not stop nested coroutines. </item>
    /// <code>
    /// yield break; // end the coroutine
    /// </code>
    /// </list>
    /// </summary>
    public class CoroutineInstance : IPooled, ICoroutineObject {
        public IEnumerator enumerator;
        public float? overrideTimeScale;
        public Action<CoroutineInstance, bool> completionHandler;

        float _waitDuration, _timer;
        ICoroutineObject _waitForObject;
        object _contextObj;
        internal bool _wasStopped, _isRunning = true;
        internal ICoroutineTarget _target;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Tick(GameTime gameTime) {
            if (_wasStopped || _target?.IsActive == false) {
                return false;
            }

            // if waiting is in progress,
            // don't tick the coroutine
            if (_waitDuration > 0) {
                _timer += (float)gameTime.ElapsedGameTime.TotalSeconds * (overrideTimeScale ?? _target?.TimeScale ?? 1.0f);
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
            if (!(_isRunning = enumerator.MoveNext())) {
                completionHandler?.Invoke(this, false);
                return false;
            }

            switch (enumerator.Current) {
                default: throw new InvalidOperationException($"Yielding {enumerator.Current.GetType().Name} in coroutines is not allowed.");

                // enumerator was passed, start a new coroutine and wait for it
                // in case this instance has a target defined, pass it to the resulting coroutine
                case IEnumerator enumerator:
                    _waitForObject = CoroutineManager.Run(enumerator, target: _target);
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
            _isRunning = false;
            completionHandler?.Invoke(this, true);
        }

        /// <summary>
        /// Gets associated context object, casted to appropriate type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T Context<T>() => (T)_contextObj;

        #region Setters

        /// <summary>
        /// Attach an optional object to reduce the allocation when used with <see cref="completionHandler"/>.
        /// </summary>
        public CoroutineInstance SetContext(object context) {
            _contextObj = context;
            return this;
        }

        /// <summary>
        /// Sets an optional event handler to trigger when this coroutine is completed or stopped. <br/>
        /// Takes <see cref="bool"/> as parameter, which signals if the coroutine was stopped manually.
        /// </summary>
        public CoroutineInstance SetCompletionHandler(Action<CoroutineInstance, bool> handler) {
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

        #endregion

        public void Reset() {
            completionHandler = null;
            _waitForObject = null;
            _target = null;
            overrideTimeScale = null;
            _waitDuration = 0;
            _timer = 0;
            _wasStopped = false;
            _isRunning = true;
            _contextObj = null;
        }

        // ICoroutineObject impl
        public bool IsRunning => _isRunning;
        ICoroutineTarget ICoroutineObject.Target => _target;
        void ICoroutineObject.Stop() => Stop();
    }
}
