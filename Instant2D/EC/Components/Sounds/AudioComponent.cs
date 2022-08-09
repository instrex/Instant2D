using Instant2D.Audio;
using Microsoft.Xna.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using static Instant2D.EC.Components.AudioComponent;

namespace Instant2D.EC.Components {
    /// <summary>
    /// The main component for playing sounds or music. If you're interested in one-shot sounds, check <see cref="Scene.PlaySound(Sound, Vector2?, AudioRolloff?, float, float, float, Entity)"/>
    /// or <see cref="OneShotSound(StaticAudioInstance, Vector2?, AudioRolloff, float, Entity)"/>.
    /// </summary>
    public class AudioComponent : Component, IUpdatableComponent {
        Sound _sound;
        AudioInstance _instance;

        // audio variables
        float _volumeScale = 1.0f;
        float _pitch = 0f;
        float _pan = 0f;

        /// <summary>
        /// When <see langword="true"/>, the sound will be decoded in chunks instead of all at once. <br/>
        /// Should be set before starting playback.
        /// </summary>
        public bool IsStreaming;

        /// <summary>
        /// Whether or not this audio should repeat. Must be set <b>before</b> calling <see cref="Play"/>.
        /// </summary>
        public bool IsLooped;

        /// <summary>
        /// The sound for this component to play. Will interrupt playback if changed mid-flight.
        /// </summary>
        public Sound Sound {
            get => _sound;
            set {
                if (value == _sound) {
                    return;
                }

                _sound = value;
                Dispose();
            }
        }

        /// <summary>
        /// When <see langword="true"/>, the audio will start playback immediately after initializing or changing <see cref="Sound"/>.
        /// </summary>
        public bool Autoplay = true;

        /// <summary>
        /// Provides readonly access to <see cref="AudioInstance"/> which this component wraps upon.
        /// </summary>
        public AudioInstance Instance => _instance;

        /// <summary>
        /// Playback state of the audio. If <see cref="Sound"/> is <see langword="null"/>, returns <see cref="PlaybackState.Stopped"/>.
        /// </summary>
        public PlaybackState State => _instance?.PlaybackState ?? PlaybackState.Stopped;

        /// <summary>
        /// Time position of the sound (in seconds). If nothing is playing, returns <c>0</c>. <br/>
        /// Will call <see cref="Seek(float)"/> when changed.
        /// </summary>
        public float Time {
            get => _instance?.Position ?? 0f;
            set {
                if (_instance != null)
                    Seek(value);
            }
        } 

        /// <summary>
        /// Length of the audio (in seconds). If <see cref="Sound"/> is <see langword="null"/>, return <c>0</c>.
        /// </summary>
        public float Length => _instance?.Length ?? 0f;

        /// <summary>
        /// Sets the volume scale for this component.
        /// </summary>
        public float Volume {
            get => _volumeScale;
            set {
                _volumeScale = value;
                if (_instance != null && !Rolloff.Enabled) {
                    _instance.Volume = _volumeScale;
                }
            }
        }

        /// <summary>
        /// Sets the pan value for this component.
        /// </summary>
        public float Pan {
            get => _pan;
            set {
                _pan = value;
                if (_instance != null && !Rolloff.Enabled) {
                    _instance.Pan = _pan;
                }
            }
        }

        /// <summary>
        /// Sets the pitch value for this component.
        /// </summary>
        public float Pitch {
            get => _pitch;
            set {
                _pitch = value;
                if (_instance != null) {
                    _instance.Pitch = _pitch;
                }
            }
        }

        /// <summary>
        /// Audio rolloff settings used for this component. By default, the sound will fade linearly with max distance of 100. <br/>
        /// Assign this to <see langword="default"/> in order to remove the rolloff effect entirely.
        /// </summary>
        public AudioRolloff Rolloff = new();

        /// <summary>
        /// Starts the playback with optional <paramref name="position"/> offset (in seconds).
        /// </summary>
        public AudioComponent Play(float position = 0f) {
            PrepareInstance();

            // begin!
            _instance.Play(IsLooped);

            // seek if position was provided
            if (position != 0f) {
                _instance.Seek(position);
            }

            return this;
        }

        /// <summary>
        /// Stops the playback. If <paramref name="stopImmediately"/> is set to <see langword="true"/>, will wait for the loop to finish first.
        /// </summary>
        public AudioComponent Stop(bool stopImmediately = false) {
            if (_instance == null)
                return this;

            _instance.Stop(stopImmediately);
            return this;
        }

        /// <summary>
        /// Seek to a position (in seconds). Has no effect when used before <see cref="Play(float)"/>.
        /// </summary>
        public AudioComponent Seek(float position) {
            if (_instance == null)
                return this;

            _instance.Seek(position);
            return this;
        }

        #region Helper Methods

        void Dispose() {
            switch (_instance) {
                default: return;

                // dispose of any stray streaming instances
                case StreamingAudioInstance:
                    _instance.Dispose();
                    break;

                // return static sounds to the pool for reuse
                case StaticAudioInstance staticInstance:
                    staticInstance.Pool();
                    break;
            }

            _instance = null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void PrepareInstance() {
            if (_instance != null)
                return;

            _instance = IsStreaming ? _sound.CreateStreamingInstance() : _sound.CreateStaticInstance();
            _instance.Pitch = _pitch;
            _instance.Volume = _volumeScale;
            _instance.Pan = _pan;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void HandleAutoplay() {
            if (!Autoplay)
                return;

            Play();
        }

        #endregion

        public void Update() {
            if (_instance == null || State != PlaybackState.Playing) {
                // pool back static instances automatically
                if (_instance is StaticAudioInstance staticInstance && State == PlaybackState.Stopped) {
                    staticInstance.Pool();
                    _instance = null;
                }

                return;
            }

            // no business there...
            if (!Rolloff.Enabled)
                return;

            // calculate volume and  based on distance
            var listenerPosition = Scene.Listener != null ? Scene.Listener.Transform.Position : Vector2.Zero;
            Console.WriteLine(listenerPosition);
            (_instance.Volume, _instance.Pan) = Rolloff.Calculate(listenerPosition, Transform.Position, _volumeScale);
        }

        public override void OnDisabled() {
            _instance?.Pause();
        }

        public override void OnEnabled() => HandleAutoplay();

        public override void OnRemovedFromEntity() {
            Dispose();
        }

        /// <summary>
        /// Useful coroutine for when you want to play a one-shot sound but feel lazy to add a whole component onto the entity. <br/>
        /// You should call <see cref="StaticAudioInstance.Pool"/> in completion handler to ensure the instance will be reused.
        /// </summary>
        public static IEnumerator OneShotSound(StaticAudioInstance instance, Vector2? position, AudioRolloff rolloff, float volume, Entity followEntity) {
            instance.Play(false);
            while (instance.PlaybackState != PlaybackState.Stopped) {
                // set position to follow the entity
                if (followEntity != null && !followEntity.IsDestroyed) {
                    position = Vector2.Lerp(position.Value, followEntity.Transform.Position, 0.5f);
                }

                // apply rolloff volume calculations
                if (rolloff.Enabled && position is Vector2 pos) {
                    (instance.Volume, instance.Pan) = rolloff.Calculate(SceneManager.Instance.Current.Listener?.Transform.Position ?? Vector2.Zero, pos, volume);
                    Console.WriteLine(instance.Pan);
                }

                yield return null;
            }

            // instance pooling is handled by completion handler
        }
    }
}
