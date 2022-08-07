using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using static FAudio;

namespace Instant2D.Audio {
    public record Sound {
        public AudioManager Manager { get; init; }

        // FAudio stuff
        stb_vorbis_info _vorbisInfo;
        FAudioBuffer? _buffer;

        /// <summary>
        /// Raw sound file data which will be used for creating instances.
        /// </summary>
        public byte[] Data { get; init; }

        /// <summary>
        /// Allocated audio buffer for this sound. <br/>
        /// WARNING: if you intend on streaming this sound, this property should not be accessed,
        /// as it will process the entire file at once.
        /// </summary>
        public FAudioBuffer AudioBuffer {
            get {
                // allocate the buffer
                if (_buffer == null) {
                    LoadBuffer();
                }

                return _buffer.Value;
            }
        }

        unsafe void LoadBuffer() {
            fixed (byte* fileData = Data) {
                var ogg = stb_vorbis_open_memory((IntPtr)fileData, Data.Length, out var fileError, IntPtr.Zero);
                if (fileError != 0) {
                    stb_vorbis_close(ogg);
                    throw new InvalidOperationException("Invalid OGG file.");
                }

                _vorbisInfo = stb_vorbis_get_info(ogg);

                // allocate and populate the sample buffer
                var sampleCount = stb_vorbis_stream_length_in_samples(ogg);
                var buffer = new float[sampleCount * _vorbisInfo.channels];
                stb_vorbis_get_samples_float_interleaved(ogg, _vorbisInfo.channels, buffer, buffer.Length);

                // allocate the buffer
                // TODO: dispose of pAudioData
                _buffer = new FAudioBuffer {
                    Flags = FAUDIO_END_OF_STREAM,
                    AudioBytes = (uint)(buffer.Length * sizeof(float)),
                    pAudioData = Marshal.AllocHGlobal(buffer.Length * sizeof(float))
                };

                Marshal.Copy(buffer, 0, _buffer.Value.pAudioData, buffer.Length);

                stb_vorbis_close(ogg);
            }
        }
    }
}
