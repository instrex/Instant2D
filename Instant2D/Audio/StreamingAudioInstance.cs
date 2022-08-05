using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using static FAudio;

namespace Instant2D.Audio {
    /// <summary>
    /// An audio instance used to stream long playback. Uses <see cref="FAudio"/> and OGG format exclusively.
    /// </summary>
    public class StreamingAudioInstance : AudioInstance {
        const int MAX_BUFFER_SIZE = 1024 * 128;
        const int BUFFER_THRESHOLD = 3;

        /// <summary>
        /// Struct used to store pending buffer information.
        /// </summary>
        protected record struct StreamingBuffer(IntPtr Pointer, uint Size); 

        // decoded bytes
        float[] _buffer;

        // file handles
        stb_vorbis_info _vorbisInfo;
        IntPtr _vorbisHandle;
        IntPtr _rawDataHandle;

        // buffers we'll send to FAudio 
        protected Queue<StreamingBuffer> _bufferQueue = new();

        /// <summary>
        /// Creates a streaming audio instance using OGG file stream.
        /// </summary>
        public StreamingAudioInstance(Stream dataStream, AudioManager manager) {
            // read the data from stream first
            var data = new byte[dataStream.Length];
            dataStream.Read(data);

            // then do some black magic to allocate unmanaged stuff
            _rawDataHandle = Marshal.AllocHGlobal(data.Length);
            Marshal.Copy(data, 0, _rawDataHandle, data.Length);

            // now open the vorbis stream
            _vorbisHandle = stb_vorbis_open_memory(_rawDataHandle, data.Length, out var vorbisError, IntPtr.Zero);
            if (vorbisError != 0) {
                throw new InvalidOperationException($"Invalid OGG file.");
            }

            // get info for later stuff
            _vorbisInfo = stb_vorbis_get_info(_vorbisHandle);

            // allocate buffer and determine the format
            _buffer = new float[MAX_BUFFER_SIZE];
            _format = new FAudioWaveFormatEx {
                wFormatTag = 3,
                wBitsPerSample = sizeof(float) * 8,
                nBlockAlign = (ushort) (4 * _vorbisInfo.channels),
                nChannels = (ushort) _vorbisInfo.channels,
                nSamplesPerSec = _vorbisInfo.sample_rate
            };

            // register the instance
            manager._streamingInstances.Add(new(this));
            _manager = manager;

            // create the voice
            CreateSourceVoice();
        }

        internal void Update() {
            if (PlaybackState != PlaybackState.Playing) 
                return;

            FAudioSourceVoice_GetState(
                _instanceHandle,
                out var voiceState,
                FAUDIO_VOICE_NOSAMPLESPLAYED
            );

            // free passed buffers
            while (_bufferQueue.Count > voiceState.BuffersQueued) {
                var buffer = _bufferQueue.Dequeue();
                Marshal.FreeHGlobal(buffer.Pointer);
            }

            QueueBuffers();
        }

        void FreeBuffers() {
            var count = _bufferQueue.Count;
            for (var i = 0; i < count; i++) {
                var buffer = _bufferQueue.Dequeue();
                Marshal.FreeHGlobal(buffer.Pointer);
            }
        }

        void QueueBuffers() {
            for (var i = BUFFER_THRESHOLD - _bufferQueue.Count; i > 0; i--) {
                // decode samples
                var interleavedSamples = stb_vorbis_get_samples_float_interleaved(_vorbisHandle, _vorbisInfo.channels, _buffer, _buffer.Length);
                var sampleCount = (uint)(interleavedSamples * _vorbisInfo.channels);

                // if we allocate less than allowed, that means this is the end...
                var isEnding = sampleCount < _buffer.Length;

                var byteLength = sampleCount * sizeof(float);
                
                // alocate new buffer
                var nextBuffer = Marshal.AllocHGlobal((int)byteLength);
                Marshal.Copy(_buffer, 0, nextBuffer, (int)sampleCount);

                // and enqueue it
                _bufferQueue.Enqueue(new(nextBuffer, byteLength));
                
                // submit the buffer to FAudio
                if (PlaybackState != PlaybackState.Stopped) {
                    var audioBuffer = new FAudioBuffer {
                        AudioBytes = byteLength,
                        pAudioData = nextBuffer,
                        PlayLength = byteLength / _format.nChannels / (uint)(_format.wBitsPerSample / 8)
                    };

                    FAudioSourceVoice_SubmitSourceBuffer(_instanceHandle, ref audioBuffer, IntPtr.Zero);
                }

                // if it's the end, either seek to start when looping
                // or end the playback
                if (isEnding) {
                    if (IsLooping) stb_vorbis_seek_start(_vorbisHandle);
                    else Stop(false);
                }
            }
        }

        #region SoundInstance Implementation

        protected override void OnDisposed() {
            Stop(true);

            // close the vorbis stream
            stb_vorbis_close(_vorbisHandle);
            Marshal.FreeHGlobal(_rawDataHandle);
        }

        public override void Pause() {
            if (PlaybackState != PlaybackState.Playing)
                return;

            FAudioSourceVoice_Stop(_instanceHandle, 0, 0);
            _playbackState = PlaybackState.Paused;
        }

        public override void Play(bool isLooping = false) {
            if (PlaybackState == PlaybackState.Playing) {
                Stop(true);
            }

            IsLooping = isLooping;
            _playbackState = PlaybackState.Playing;

            Update();

            // start the voice
            FAudioSourceVoice_Start(_instanceHandle, 0, 0);
        }

        public override void Seek(float seconds) {
            var samplePosition = (uint)(_vorbisInfo.sample_rate * seconds);

            // stop the playback first
            if (PlaybackState == PlaybackState.Playing) {
                FAudioSourceVoice_Stop(_instanceHandle, 0, 0);
            }

            // free buffers
            FAudioSourceVoice_FlushSourceBuffers(_instanceHandle);
            FreeBuffers();
            Update();

            // and then queue new buffers
            stb_vorbis_seek(_vorbisHandle, samplePosition);

            // restart the playback
            if (PlaybackState == PlaybackState.Playing) {
                Play(IsLooping);
            }
        }

        public override void Stop(bool immediate = true) {
            if (PlaybackState != PlaybackState.Stopped && immediate) {
                FAudioSourceVoice_Stop(_instanceHandle, 0, 0);
                FAudioSourceVoice_FlushSourceBuffers(_instanceHandle);
                FreeBuffers();
            }

            _playbackState = PlaybackState.Stopped;
        }

        #endregion
    }
}
