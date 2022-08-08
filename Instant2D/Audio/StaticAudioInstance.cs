using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

using static FAudio;

namespace Instant2D.Audio {
    /// <summary>
    /// Represents an audio instance used to play sounds that are processed all at once. Use this for quick and short sound effects.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1806:Do not ignore method results", Justification = "<Pending>")]
    public class StaticAudioInstance : AudioInstance {
        /// <summary>
        /// Sound data this instance uses.
        /// </summary>
        public readonly Sound Sound;

        public StaticAudioInstance(Sound sound) {
            Sound = sound;

            // prepare the buffer to access length and format!!
            Sound.PrepareBuffer();

            // inherit Sound's properties
            Length = sound._length;
            _format = sound._format;
            _manager = sound.Manager;

            // now just create the voice!
            CreateSourceVoice();
        }

        /// <summary>
        /// Returns the instance back into <see cref="Sound"/>'s pool. This may be used to avoid allocating multiple instances for the same sound.
        /// </summary>
        public void Pool() {
            Sound._staticInstances.Enqueue(this);
            Stop(true);
        }

        #region AudioInstance implementation

        public override PlaybackState PlaybackState {
            get {
                FAudioSourceVoice_GetState(
                    _sourceVoiceHandle,
                    out var state,
                    FAUDIO_VOICE_NOSAMPLESPLAYED
                );

                // stop when no buffers :(
                if (state.BuffersQueued == 0) {
                    Stop(true);
                }

                return _playbackState;
            }
        }

        public override void Pause() {
            // we don't wanna pause Stopped sounds... right?
            if (PlaybackState == PlaybackState.Playing) {
                FAudioSourceVoice_Stop(_sourceVoiceHandle, 0, 0);
                _playbackState = PlaybackState.Paused;
            }
        }

        public override void Play(bool isLooping = false) {
            if (PlaybackState == PlaybackState.Playing) {
                Stop(true);
            }

            // TODO: add LoopBegin and LoopLength when needed?
            Sound._buffer.LoopCount = (IsLooping = isLooping) ? 255u : 0;
            FAudioSourceVoice_SubmitSourceBuffer(
                _sourceVoiceHandle,
                ref Sound._buffer,
                IntPtr.Zero
            );

            // play the sound!
            FAudioSourceVoice_Start(_sourceVoiceHandle, 0, 0);
            _playbackState = PlaybackState.Playing;
        }

        public override void Seek(float seconds) {
            if (_playbackState == PlaybackState.Playing) {
                FAudioSourceVoice_Stop(_sourceVoiceHandle, 0, 0);
                FAudioSourceVoice_FlushSourceBuffers(_sourceVoiceHandle);
            }

            var oldPlayBegin = Sound._buffer.PlayBegin;
            Sound._buffer.PlayBegin = (uint)(_format.nSamplesPerSec * seconds);
            Play(IsLooping);

            // here we have to reset it to not mess up other instances
            Sound._buffer.PlayBegin = oldPlayBegin;
        }

        public override void Stop(bool immediate = true) {
            if (_playbackState == PlaybackState.Stopped)
                return;

            if (!immediate) {
                FAudioSourceVoice_ExitLoop(_sourceVoiceHandle, 0);
                return;
            }

            // immediately stop the sound
            FAudioSourceVoice_Stop(_sourceVoiceHandle, 0, 0);
            FAudioSourceVoice_FlushSourceBuffers(_sourceVoiceHandle);
            _playbackState = PlaybackState.Stopped;
        }

        #endregion
    }
}
