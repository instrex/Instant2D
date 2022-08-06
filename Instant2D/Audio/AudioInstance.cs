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
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1806:Do not ignore method results", Justification = "<Pending>")]
	public abstract class AudioInstance : IDisposable {
        internal IntPtr _instanceHandle;
        internal FAudioWaveFormatEx _format;
        internal AudioManager _manager;

		// playback values
		protected F3DAUDIO_DSP_SETTINGS _dspSettings;
		protected PlaybackState _playbackState = PlaybackState.Stopped;
        float _volume = 1, _pan, _pitch = 0;
        bool _isDisposed;

		// hacky way to fix streaming position issues
		protected ulong _positionOffset;

        /// <summary>
        /// The volume of this sound.
        /// </summary>
        public float Volume {
			get => _volume;
			set {
				_volume = value;
				if (_instanceHandle != IntPtr.Zero) {
					FAudioVoice_SetVolume(_instanceHandle, _volume, 0);
				}
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
					FAudioVoice_SetOutputMatrix(
						_instanceHandle,
						_manager.MasteringVoice,
						_dspSettings.SrcChannelCount,
						_dspSettings.DstChannelCount,
						_dspSettings.pMatrixCoefficients,
						0
					);
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
		public virtual PlaybackState PlaybackState => _playbackState;

		/// <summary>
		/// Length of this sound in seconds.
		/// </summary>
		public float Length { get; protected set; }

		/// <summary>
		/// Gets the playback position in seconds.
		/// </summary>
		public float Position {
			get {
				if (_instanceHandle == IntPtr.Zero)
					return 0f;

				FAudioSourceVoice_GetState(
					_instanceHandle,
					out var state,
					0
				);

				return (state.SamplesPlayed - _positionOffset) / (float)_format.nSamplesPerSec;
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
		/// Stop the playback. <paramref name="immediate"/> indicates whether or not the instance should wait before the loop is done.
		/// </summary>
		public abstract void Stop(bool immediate = true);

		/// <summary>
		/// Adjust playback position to amount in <paramref name="seconds"/>.
		/// </summary>
		public abstract void Seek(float seconds);

		protected void CreateSourceVoice() {
			FAudio_CreateSourceVoice(
				_manager.AudioHandle,
				out _instanceHandle,
				ref _format,
				FAUDIO_VOICE_USEFILTER,
				FAUDIO_DEFAULT_FREQ_RATIO,
				IntPtr.Zero,
				IntPtr.Zero,
				IntPtr.Zero
			);

			// guh..
			if (_instanceHandle == IntPtr.Zero) {
				throw new InvalidOperationException("AudioInstance failed to initialize.");
			}

			InitDSPSettings(_format.nChannels);
			_playbackState = PlaybackState.Stopped;
        }

        #region FNA methods

        internal void InitDSPSettings(uint srcChannels) {
            _dspSettings = new F3DAUDIO_DSP_SETTINGS {
                DopplerFactor = 1.0f,
                SrcChannelCount = srcChannels,
                DstChannelCount = _manager.DeviceDetails.OutputFormat.Format.nChannels
            };

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

		#region Disposal

		protected virtual void OnDisposed() { }

        protected virtual void Dispose(bool disposing) {
            if (!_isDisposed) {
				// stop the playback so no bad stuff happens (not a threat)
				Stop(true);

				if (_instanceHandle != IntPtr.Zero) {
					// free the resources
					FAudioVoice_DestroyVoice(_instanceHandle);
					Marshal.FreeHGlobal(_dspSettings.pMatrixCoefficients);
				}

				// invoke callbacks
				OnDisposed();

                _isDisposed = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        ~AudioInstance() {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        public void Dispose() {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
