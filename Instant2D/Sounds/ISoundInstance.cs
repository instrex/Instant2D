namespace Instant2D.Sounds;

/// <summary>
/// Simple sound instance, capable of adjusting pitch, volume, pan and getting playback position.
/// </summary>
public interface ISoundInstance {
    /// <summary>
    /// Parent sound of this instance.
    /// </summary>
    public Sound Sound { get; }

    /// <summary>
    /// Volume scale of this sound instance from 0.0 to 1.0
    /// </summary>
    float Volume { get; set; }

    /// <summary> 
    /// Pitch scale of this sound instance. 
    /// <list type="bullet">
    /// <item> Less than 1.0 will slow down the audio instance, be careful with lower values as they can stretch the audio indefinetely! </item>
    /// <item> Higher than 1.0 will increase audio speed. </item>
    /// </list>
    /// </summary>
    float Pitch { get; set; }

    /// <summary>
    /// Direction of the sound, with -1.0 being fully in left speaker and 1.0 in the right.
    /// </summary>
    float Pan { get; set; }

    /// <summary>
    /// Playback position in seconds.
    /// </summary>
    float PlaybackPosition { get; }

    /// <summary>
    /// Current playback state of this instance.
    /// </summary>
    PlaybackState State { get; }

    /// <summary>
    /// Begins sound playback.
    /// </summary>
    void Play();

    /// <summary>
    /// Stops the playback.
    /// </summary>
    void Stop();

    /// <summary>
    /// Pauses the playback.
    /// </summary>
    void Pause();

    /// <summary>
    /// Resumes the playback.
    /// </summary>
    void Resume();
}
