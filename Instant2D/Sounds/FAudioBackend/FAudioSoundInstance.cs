using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq.Expressions;
using System.Reflection;

using static FAudio;

namespace Instant2D.Sounds.FAudioBackend;

public class FAudioSoundInstance : ISoundInstance, IDisposable {
    readonly WeakReference<ISoundInstance> _selfReference;
    protected readonly SoundEffectInstance _instance;
    protected readonly int _sampleRate;

    internal FAudioSoundInstance(SoundEffectInstance instance, int sampleRate, Sound sound) {
        _sampleRate = sampleRate;
        _instance = instance;
        Sound = sound;

        // attach the instance to parent
        sound.Instances.Add(_selfReference = new(this));
    }

    public bool IsDisposed { get; private set; }

    // use reflection to access hidden voice handle for:
    // - bypassing pitch limit
    // - getting played sample count
    // - optionally manipulating filters
    static Func<SoundEffectInstance, nint> _voiceHandleGetter;
    protected static Func<SoundEffectInstance, nint> GetVoiceHandle {
        get {
            if (_voiceHandleGetter is null) {
                var fieldInfo = typeof(SoundEffectInstance).GetField("handle", BindingFlags.NonPublic | BindingFlags.Instance);
                var instanceParam = Expression.Parameter(typeof(SoundEffectInstance));
                var body = Expression.Field(instanceParam, fieldInfo);
                _voiceHandleGetter = Expression.Lambda<Func<SoundEffectInstance, nint>>(body, instanceParam).Compile();
            }

            return _voiceHandleGetter;
        }
    }

    public Sound Sound { get; init; }

    public float Volume {
        get => _instance.Volume;
        set => _instance.Volume = value;
    }

    public float Pan {
        get => _instance.Pan;
        set => _instance.Pan = value;
    }

    float _pitch = 1.0f;
    public float Pitch {
        get => _pitch;
        set {
            _pitch = value;
            UpdatePitch();
        }
    }

    void UpdatePitch() {
        var handle = GetVoiceHandle(_instance);
        if (handle == nint.Zero)
            return;

        _ = FAudioSourceVoice_SetFrequencyRatio(
            handle,
            Math.Clamp(_pitch, 0.001f, 100.0f),
            0
        );
    }

    public float PlaybackPosition {
        get {
            var handle = GetVoiceHandle(_instance);
            if (handle == nint.Zero)
                return 0.0f;

            FAudioSourceVoice_GetState(handle, out var state, 0);
            return (float)state.SamplesPlayed / _sampleRate;
        }
    }

    public PlaybackState State => _instance.State switch {
        SoundState.Playing => PlaybackState.Playing,
        SoundState.Paused => PlaybackState.Paused,
        _ => PlaybackState.Stopped,
    };

    public void Pause() {
        _instance.Pause();
    }

    public void Play() {
        _instance.Play();
        UpdatePitch();
    }

    public void Resume() {
        _instance.Resume();
    }

    public void Stop() {
        _instance.Stop();
    }

    #region IDisposable

    protected virtual void Dispose(bool disposing) {
        if (!IsDisposed) {
            Stop();

            // detach the instance from parent
            Sound.Instances.Remove(_selfReference);
            _selfReference.SetTarget(null);
            IsDisposed = true;
        }
    }

    ~FAudioSoundInstance() => Dispose(disposing: false);

    public void Dispose() {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    #endregion
}
