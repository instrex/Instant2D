using Instant2D.Assets.Sprites;
using Microsoft.Xna.Framework;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D.EC {
    public enum LoopType {
        /// <summary>
        /// The animation will be played once, and then remain on the last frame.
        /// </summary>
        OneShot,

        /// <summary> Animation will continously loop. </summary>
        /// <remarks>If you're using this, <see cref="AnimatorState"/> would never become <see cref="AnimatorState.Completed"/>. </remarks>
        Loop
    }

    public enum AnimatorState {
        /// <summary>
        /// Animator is paused or no animation has been assigned yet.
        /// </summary>
        Paused, 

        /// <summary>
        /// Animator is in process of displaying animations.
        /// </summary>
        Running, 

        /// <summary>
        /// Animator has finished the animation it was assigned.
        /// </summary>
        Completed
    }

    public class SpriteAnimationComponent : SpriteComponent, IUpdatableComponent {
        /// <summary>
        /// State of animation playback.
        /// </summary>
        public AnimatorState State { get; private set; }

        float _elapsedTime;
        float _timePerFrame, _duration;
        SpriteAnimation _animation;
        LoopType _loopType;
        int _frameIndex = -1;

        /// <summary>
        /// Sets the animation of this animator. Will not reset the time.
        /// </summary>
        public SpriteAnimation Animation {
            get => _animation;
            set {
                _animation = value;

                // calculate commonly used values
                _timePerFrame = 1.0f / _animation.Fps;
                _duration = _timePerFrame * _animation.Frames.Length;
            }
        }

        /// <summary>
        /// Current animation frame index.
        /// </summary>
        public int Frame {
            get => _frameIndex;
            set => SetFrame(value);
        }

        public delegate void AnimationEventHandler(SpriteAnimationComponent animator, string ev, object[] args);

        /// <summary>
        /// Called when <see cref="SpriteAnimation.Events"/> trigger.
        /// </summary>
        public event AnimationEventHandler OnAnimationEvent;

        /// <summary>
        /// Called when the animator finishes current animation, only if <see cref="LoopType"/> is set to <see cref="LoopType.OneShot"/>.
        /// </summary>
        public event Action<SpriteAnimationComponent> OnAnimationComplete;

        /// <summary>
        /// Controls how fast the animation should flow.
        /// </summary>
        public float Speed = 1.0f;

        /// <summary>
        /// Resets the time and plays new animation. This doesn't check if current animation is the same and restarts it.
        /// </summary>
        public SpriteAnimationComponent Play(SpriteAnimation animation, LoopType loop = LoopType.OneShot) {
            Animation = animation;

            // reset animation properties
            _elapsedTime = 0;
            _loopType = loop;

            _frameIndex = -1;
            State = AnimatorState.Running;
            Frame = 0;

            return this;
        }

        /// <summary>
        /// Resumes (or begins if <paramref name="restartAnimation"/> is set) the animation.
        /// </summary>
        public SpriteAnimationComponent Play(bool restartAnimation = false) {
            if (restartAnimation) {
                Play(_animation, _loopType);
                return this;
            }

            State = AnimatorState.Running;
            return this;
        }

        /// <summary>
        /// Pause the frame animation. Could be used to manually control frames.
        /// </summary>
        public SpriteAnimationComponent Pause() {
            State = AnimatorState.Paused;
            return this;
        }

        #region Setters

        /// <inheritdoc cref="Frame"/>
        public SpriteAnimationComponent SetFrame(int frame, bool triggerEvents = true) {
            var clampedIndex = Math.Clamp(frame, 0, _animation.Frames.Length - 1);

            // don't do anything if frames are the same
            if (clampedIndex == _frameIndex)
                return this;

            // set the frame
            Sprite = _animation.Frames[clampedIndex];
            _frameIndex = clampedIndex;

            // call the events (if there are any)
            if (triggerEvents && _animation.Events != null)
                for (var i = 0; i < _animation.Events.Length; i++) {
                    var ev = _animation.Events[i];

                    // it's not your time yet...
                    if (ev.frame != frame)
                        continue;

                    OnAnimationEvent?.Invoke(this, ev.key, ev.args);
                }

            return this;
        }

        /// <summary>
        /// Sets the animation without reseting it. As an example, this may be used for seamless transitions between aerial/grounded animations.
        /// </summary>
        public SpriteAnimationComponent SetAnimation(SpriteAnimation animation) {
            Animation = animation;
            return this;
        }

        /// <inheritdoc cref="Speed"/>
        public SpriteAnimationComponent SetSpeed(float speed) {
            Speed = speed;
            return this;
        }

        /// <inheritdoc cref="OnAnimationComplete"/>
        public SpriteAnimationComponent SetCompletionHandler(Action<SpriteAnimationComponent> handler) {
            OnAnimationComplete += handler;
            return this;
        }

        /// <inheritdoc cref="OnAnimationEvent"/>
        public SpriteAnimationComponent SetEventHandler(AnimationEventHandler handler) {
            OnAnimationEvent += handler;
            return this;
        }

        #endregion

        public override void PostInitialize() {
            base.PostInitialize();

            // if frame wasn't assigned, we set it to 0 (without invoking events)
            if (_frameIndex == -1 && _animation != default) {
                SetFrame(0, false);
            }
        }

        public void Update() {
            if (State != AnimatorState.Running)
                return;

            // advance the animation based on timescale and speed
            _elapsedTime += TimeManager.TimeDelta * Entity.TimeScale * Speed;
            if (_elapsedTime >= _duration) {
                _elapsedTime = 0;
                switch (_loopType) {
                    // end the animation
                    case LoopType.OneShot:
                        State = AnimatorState.Completed;
                        OnAnimationComplete?.Invoke(this);

                        return;
                }
            }

            Frame = (int)MathF.Floor(_elapsedTime / _timePerFrame);
        }
    }
}
