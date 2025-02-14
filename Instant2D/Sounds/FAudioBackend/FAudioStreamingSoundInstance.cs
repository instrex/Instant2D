using Microsoft.Xna.Framework.Audio;
using System;
using System.Buffers;

using static FAudio;

namespace Instant2D.Sounds.FAudioBackend;

public class FAudioStreamingSoundInstance : FAudioSoundInstance, IStreamingSoundInstance {
    readonly DynamicSoundEffectInstance _dynamicInstance;
    readonly nint _vorbisHandle;
    readonly int _bufferSize;

    internal FAudioStreamingSoundInstance(Sound sound) : base(new DynamicSoundEffectInstance(sound.SampleRate, (AudioChannels)sound.Channels), sound) {
        _dynamicInstance = _instance as DynamicSoundEffectInstance;
        _dynamicInstance.BufferNeeded += (_, _) => SubmitBuffer();

        // read vorbis info using streaming handle
        // this avoids opening vorbis stream twice
        _vorbisHandle = sound.CreateVorbisHandle();
        sound.ReadVorbisInfo(_vorbisHandle);

        // initialize the buffer
        _bufferSize = Sound.SampleRate * Sound.Channels;

        // create vorbis handle and submit the initial buffer
        SubmitBuffer();
    }

    void SubmitBuffer() {
        var buffer = ArrayPool<float>.Shared.Rent(_bufferSize);
        var decodedSamplesCount = stb_vorbis_get_samples_float_interleaved(_vorbisHandle, Sound.Channels, buffer, _bufferSize);
        _dynamicInstance.SubmitFloatBufferEXT(buffer, 0, decodedSamplesCount * Sound.Channels);

        // return shared buffer
        ArrayPool<float>.Shared.Return(buffer);

        // loop upon reaching the end
        if (decodedSamplesCount <= 0 && IsLooping) {
            // we avoid adjusting position offset here
            // since its not a trivial task to find out when specific buffer starts playing
            // correcting it is taken care by FAudioSoundInstance.PlaybackPosition already
            _ = stb_vorbis_seek_start(_vorbisHandle);
        }
    }

    public bool IsLooping { get; set; }

    public void Seek(float playbackPosition) {
        ObjectDisposedException.ThrowIf(IsDisposed, this);

        // jump to the sample index
        var samplePosition = (uint)(Sound.SampleRate * playbackPosition);
        _ = stb_vorbis_seek(_vorbisHandle, samplePosition);

        // restart the playback
        Stop();
        Play();

        // shift offset to have correct playback position
        AdjustSamplePositionOffset(samplePosition);
    }

    void AdjustSamplePositionOffset(uint newSamplePosition) {
        // get the voice to fix playback position offset
        var sourceVoiceHandle = GetVoiceHandle(_instance);
        if (sourceVoiceHandle == IntPtr.Zero)
            return;

        FAudioSourceVoice_GetState(
            sourceVoiceHandle,
            out var state,
            0
        );

        // since sound effect instances retain SamplesPlayed, we need to remember at which point we seeked position
        // then it is automatically subtracted from SamplesPlayed value in FAudioSoundInstance.cs
        _playbackPositionOffset = state.SamplesPlayed - newSamplePosition;
    }

    protected override void Dispose(bool disposing) {
        if (!IsDisposed) {
            // close the vorbis handle when disposing
            stb_vorbis_close(_vorbisHandle);
        }

        base.Dispose(disposing);
    }
}
