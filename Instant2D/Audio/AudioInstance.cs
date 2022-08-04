using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using static FAudio;

namespace Instant2D.Audio {
    /// <summary>
    /// An audio instance.
    /// </summary>
    public abstract class AudioInstance {
        internal IntPtr _instanceHandle;
        internal FAudioWaveFormatEx _format;
        internal AudioManager _manager;

		// playback values
		protected F3DAUDIO_DSP_SETTINGS _dspSettings;
		protected PlaybackState _playbackState = PlaybackState.Stopped;
        float _volume = 1, _pan, _pitch = 1;

		/// <summary>
		/// The volume of this sound.
		/// </summary>
		public float Volume {
			get => _volume;
			set {
				_volume = value;
				FAudioVoice_SetVolume(_instanceHandle, _volume, 0);
            }
        }

        /// <summary>
        /// Panning value of this sound. Clamped between -1 and 1.
        /// </summary>
        public float Pan {
            get => _pan;
            set {
                _pan = Math.Clamp(value, -1.0f, 1.0f);
				if (_instanceHandle != IntPtr.Zero) {
					SetPanMatrixCoefficients();
				}
			}
        }

		/// <summary>
		/// Pitch value of the sound. Clamped between -1 and 1.
		/// </summary>
		public float Pitch {
			get => _pitch;
			set {
				_pitch = Math.Clamp(value, -1.0f, 1.0f);
				if (_instanceHandle != IntPtr.Zero) {
					UpdatePitch();
				}
			}
        }

        /// <summary>
        /// State of this audio instance.
        /// </summary>
        public abstract PlaybackState PlaybackState { get; }

		/// <summary>
		/// Gets the playback position in seconds.
		/// </summary>
		public float Position {
			get {
				FAudioSourceVoice_GetState(
					_instanceHandle,
					out var state,
					0
				);

				return state.SamplesPlayed / (float)_format.nSamplesPerSec;
			}

			set => Seek(value);
        }

        /// <summary>
        /// Should this audio instance loop.
        /// </summary>
        public bool IsLooping { get; protected set; }

		/// <summary>
		/// Begin playback of the sound.
		/// </summary>
		public abstract void Play(bool isLooping = false);

		/// <summary>
		/// Pause playback of the sound.
		/// </summary>
		public abstract void Pause();

		/// <summary>
		/// Stop the playback.
		/// </summary>
		public abstract void Stop(bool immediate = false);

		/// <summary>
		/// Adjust playback position to amount in <paramref name="seconds"/>.
		/// </summary>
		public abstract void Seek(float seconds);

        #region FNA methods

        internal void InitDSPSettings(uint srcChannels) {
			_dspSettings = new F3DAUDIO_DSP_SETTINGS();
			_dspSettings.DopplerFactor = 1.0f;
			_dspSettings.SrcChannelCount = srcChannels;
			_dspSettings.DstChannelCount = _manager.DeviceDetails.OutputFormat.Format.nChannels;

			int memsize = (4 *
				(int)_dspSettings.SrcChannelCount *
				(int)_dspSettings.DstChannelCount
			);
			_dspSettings.pMatrixCoefficients = Marshal.AllocHGlobal(memsize);
			unsafe {
				byte* memPtr = (byte*)_dspSettings.pMatrixCoefficients;
				for (int i = 0; i < memsize; i += 1) {
					memPtr[i] = 0;
				}
			}
			SetPanMatrixCoefficients();
		}

		void UpdatePitch() {
			float doppler;
			float dopplerScale = _manager.DopplerScale;
			if (dopplerScale == 0.0f) {
				doppler = 1.0f;
			} else {
				doppler = _dspSettings.DopplerFactor * dopplerScale;
			}

			FAudioSourceVoice_SetFrequencyRatio(
				_instanceHandle,
				(float)Math.Pow(2.0, _pitch) * doppler,
				0
			);
		}

		unsafe void SetPanMatrixCoefficients() {
			/* Two major things to notice:
			 * 1. The spec assumes any speaker count >= 2 has Front Left/Right.
			 * 2. Stereo panning is WAY more complicated than you think.
			 *    The main thing is that hard panning does NOT eliminate an
			 *    entire channel; the two channels are blended on each side.
			 * Aside from that, XNA is pretty naive about the output matrix.
			 * -flibit
			 */
			float* outputMatrix = (float*)_dspSettings.pMatrixCoefficients;
			if (_dspSettings.SrcChannelCount == 1) {
				if (_dspSettings.DstChannelCount == 1) {
					outputMatrix[0] = 1.0f;
				} else {
					outputMatrix[0] = (_pan > 0.0f) ? (1.0f - _pan) : 1.0f;
					outputMatrix[1] = (_pan < 0.0f) ? (1.0f + _pan) : 1.0f;
				}
			} else {
				if (_dspSettings.DstChannelCount == 1) {
					outputMatrix[0] = 1.0f;
					outputMatrix[1] = 1.0f;
				} else {
					if (_pan <= 0.0f) {
						// Left speaker blends left/right channels
						outputMatrix[0] = 0.5f * _pan + 1.0f;
						outputMatrix[1] = 0.5f * -_pan;
						// Right speaker gets less of the right channel
						outputMatrix[2] = 0.0f;
						outputMatrix[3] = _pan + 1.0f;
					} else {
						// Left speaker gets less of the left channel
						outputMatrix[0] = -_pan + 1.0f;
						outputMatrix[1] = 0.0f;
						// Right speaker blends right/left channels
						outputMatrix[2] = 0.5f * _pan;
						outputMatrix[3] = 0.5f * -_pan + 1.0f;
					}
				}
			}
		}

        #endregion
    }
}
