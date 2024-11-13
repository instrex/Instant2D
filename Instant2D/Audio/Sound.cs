using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using static FAudio;

namespace Instant2D.Audio {
    public record Sound : IDisposable {
        public AudioManager Manager { get; init; }

        // FAudio stuff
        internal FAudioWaveFormatEx _format;
        internal float _length;
        internal FAudioBuffer _buffer;
        bool _bufferCreated;

        // IDisposable
        bool _isDisposed;

        // instance tracking and pooling
        internal readonly Queue<StaticAudioInstance> _staticInstances = new();

        /// <summary>
        /// Raw sound file data which will be used for creating instances.
        /// </summary>
        public byte[] Data { get; init; }

        /// <summary>
        /// Creates a new streaming audio instance which will stream the contents of <see cref="Data"/>. Should be used for longer, continuous sounds or music.
        /// </summary>
        public StreamingAudioInstance CreateStreamingInstance() => new(Data, Manager);

        /// <summary>
        /// Creates a new static audio instance with audio buffer shared between instances. Should be used for frequent, short sound effects. <br/>
        /// Call <see cref="StaticAudioInstance.Pool"/> after using it to make this method reuse already created instances.
        /// </summary>
        public StaticAudioInstance CreateStaticInstance() {
            // try to get pooled instances first
            if (_staticInstances.TryDequeue(out var result)) {
                result.Pitch = 0;
                result.Pan = 0;
                result.Volume = 1;
                return result;
            }

            // or else just allocate new one...
            return new StaticAudioInstance(this);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1806:Do not ignore method results", Justification = "<Pending>")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal unsafe void PrepareBuffer() {
            if (_bufferCreated)
                return;

            fixed (byte* fileData = Data) {
                var ogg = stb_vorbis_open_memory((IntPtr)fileData, Data.Length, out var fileError, IntPtr.Zero);
                if (fileError != 0) {
                    stb_vorbis_close(ogg);
                    throw new InvalidOperationException("Invalid OGG file.");
                }

                var info = stb_vorbis_get_info(ogg);

                // allocate and populate the sample buffer
                var sampleCount = stb_vorbis_stream_length_in_samples(ogg);
                var buffer = new float[sampleCount * info.channels * 2];
                stb_vorbis_get_samples_float_interleaved(ogg, info.channels, buffer, buffer.Length);

                // allocate the buffer
                // TODO: dispose of pAudioData
                _buffer = new FAudioBuffer {
                    Flags = FAUDIO_END_OF_STREAM,
                    AudioBytes = (uint)(buffer.Length * sizeof(float)),
                    pAudioData = Marshal.AllocHGlobal(buffer.Length * sizeof(float))
                };
                
                // get length information
                _length = stb_vorbis_stream_length_in_seconds(ogg);

                // create the wave format struct
                _format = new FAudioWaveFormatEx {
                    wFormatTag = 3,
                    wBitsPerSample = sizeof(float) * 8,
                    nBlockAlign = (ushort)(4 * info.channels),
                    nChannels = (ushort)info.channels,
                    nSamplesPerSec = info.sample_rate
                };

                Marshal.Copy(buffer, 0, _buffer.pAudioData, buffer.Length);

                stb_vorbis_close(ogg);
            }

            _bufferCreated = true;
        }

        protected virtual void Dispose(bool disposing) {
            if (!_isDisposed) {
                if (_bufferCreated) {
                    Marshal.FreeHGlobal(_buffer.pAudioData);
                }
                
                _isDisposed = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        ~Sound() {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        public void Dispose() {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
