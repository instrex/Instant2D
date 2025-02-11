using Microsoft.Xna.Framework.Audio;
using System;

namespace Instant2D.Sounds.FAudioBackend;

public class FAudioStreamingSoundInstance : FAudioSoundInstance, IStreamingSoundInstance {
    internal FAudioStreamingSoundInstance(Sound sound) : base(
        new DynamicSoundEffectInstance(sound.SampleRate, (AudioChannels)sound.Channels),
        sound.SampleRate, sound) { }

    public bool Looped { 
        get => throw new NotImplementedException();
        set => throw new NotImplementedException(); 
    }

    public void Seek(float playbackPosition) {
        throw new NotImplementedException();
    }
}
