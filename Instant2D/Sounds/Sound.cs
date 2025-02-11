using Instant2D.Sounds.FAudioBackend;
using Microsoft.Xna.Framework.Audio;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using static FAudio;

namespace Instant2D.Sounds;

public class Sound : IDisposable {
    readonly nint _fileDataHandle;
    readonly int _fileDataLength;

    // used for keeping track of created instances
    internal readonly List<WeakReference<ISoundInstance>> Instances = [];

    byte[] _byteSamples;
    float[] _samples;

    // basic info
    bool _hasInfo;
    int _sampleRate, _channels;
    float _duration;

    /// <summary>
    /// Sample rate of this sound.
    /// </summary>
    public int SampleRate {
        get {
            if (!_hasInfo) ReadInfo(IntPtr.Zero);
            return _sampleRate;
        }
    }

    /// <summary>
    /// Gets number of channels of this sound.
    /// </summary>
    public int Channels {
        get {
            if (!_hasInfo) ReadInfo(IntPtr.Zero);
            return _channels;
        }
    }

    /// <summary>
    /// Gets duration of this sound in seconds.
    /// </summary>
    public float Duration {
        get {
            if (!_hasInfo) ReadInfo(IntPtr.Zero);
            return _duration;
        }
    }

    // used when creating basic FAudioSoundInstances
    SoundEffect _FAudioSoundEffect;

    public Sound(byte[] fileData) : this(fileData, 0, fileData.Length) { }
    public Sound(byte[] fileData, int offset, int length) {
        // allocate unmanaged file data buffer used for decoding or streaming
        _fileDataHandle = Marshal.AllocHGlobal(_fileDataLength = length);
        Marshal.Copy(fileData, offset, _fileDataHandle, length);
    }

    public bool IsDisposed { get; private set; }

    /// <summary>
    /// Identifier of this sound.
    /// </summary>
    public string Key { get; init; }

    /// <summary>
    /// When <see langword="true"/>, will always create streaming instances. <br/>
    /// Is automatically set for sounds in music folder.
    /// </summary>
    public bool PreferStreaming { get; init; }

    /// <summary>
    /// Decodes and returns float samples for playback of this sound.
    /// </summary>
    public float[] GetFloatSamples() {
        ObjectDisposedException.ThrowIf(IsDisposed, this);

        if (_samples != null)
            return _samples;

        var vorbis = CreateVorbisHandle();

        // get basic file info
        var totalSamples = stb_vorbis_stream_length_in_samples(vorbis);
        ReadInfo(vorbis);

        _samples = new float[totalSamples * _channels];

        // populate the samples array with float samples
        var readSamples = stb_vorbis_get_samples_float_interleaved(vorbis, _channels, _samples, _samples.Length);
        if (readSamples * 2 < _samples.Length) {
            InstantApp.Logger.Warn($"Sample buffer read underflow at {Key}.");
        }

        return _samples;
    }

    /// <summary>
    /// Decodes and returns byte samples for playback of this sound.
    /// </summary>
    public byte[] GetByteSamples() {
        ObjectDisposedException.ThrowIf(IsDisposed, this);

        var samples = GetFloatSamples();
        _byteSamples = new byte[samples.Length * 2];

        // unpack the float values into bytes
        for (int i = 0; i < samples.Length; i++) {
            short val = (short)(samples[i] * short.MaxValue);
            _byteSamples[i * 2] = (byte)val;
            _byteSamples[i * 2 + 1] = (byte)(val >> 8);
        }

        return _byteSamples;
    }

    /// <summary>
    /// Opens a new vorbis stream from this sound file.
    /// </summary>
    public nint CreateVorbisHandle() {
        ObjectDisposedException.ThrowIf(IsDisposed, this);

        var vorbis = stb_vorbis_open_memory(_fileDataHandle, _fileDataLength, out var fileError, IntPtr.Zero);
        if (fileError != 0) {
            InstantApp.Logger.Error($"Failed to open a vorbis handle for sound {Key} with error code {fileError}.");

            // free unmanaged resources
            stb_vorbis_close(vorbis);
            return IntPtr.Zero;
        }

        return vorbis;
    }

    public ISoundInstance CreateInstance(bool streaming = false) {
        ObjectDisposedException.ThrowIf(IsDisposed, this);

        if (PreferStreaming || streaming) {
            // return streaming instance...
            throw new NotImplementedException();
        }

        // initialize the FAudioSoundEffect
        _FAudioSoundEffect ??= new(GetByteSamples(), _sampleRate, (AudioChannels)_channels);

        // create the instance
        return new FAudioSoundInstance(_FAudioSoundEffect.CreateInstance(), _sampleRate, this);
    }

    void ReadInfo(nint vorbisHandle) {
        if (_hasInfo) return;

        var oneShotHandle = vorbisHandle == IntPtr.Zero;

        // create vorbis handle if one haven't been passed
        if (oneShotHandle) vorbisHandle = CreateVorbisHandle();

        var info = stb_vorbis_get_info(vorbisHandle);
        _sampleRate = (int)info.sample_rate;
        _channels = info.channels;
        _hasInfo = true;

        // get duration in seconds as well
        _duration = stb_vorbis_stream_length_in_seconds(vorbisHandle);

        // close the handle if its local
        if (oneShotHandle) stb_vorbis_close(vorbisHandle);
    }

    #region IDisposable

    protected virtual void Dispose(bool disposing) {
        if (IsDisposed)
            return;

        if (disposing) {
            // dispose of each instance first
            foreach (var instanceRef in Instances) {
                if (instanceRef.TryGetTarget(out var instance) && instance is IDisposable disposable)
                    disposable.Dispose();
            }

            Instances.Clear();

            // dispose of FAudioSoundEffect when required
            _FAudioSoundEffect?.Dispose();
        }

        Marshal.FreeHGlobal(_fileDataHandle);

        _byteSamples = null;
        _samples = null;

        IsDisposed = true;
    }

    ~Sound() => Dispose(disposing: false);

    public void Dispose() {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    #endregion
}
