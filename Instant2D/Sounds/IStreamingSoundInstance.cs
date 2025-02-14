namespace Instant2D.Sounds;

/// <summary>
/// Streaming sound instance, which supports changing the playback position and looping.
/// </summary>
public interface IStreamingSoundInstance : ISoundInstance {
    /// <summary>
    /// Audio playback won't stop when reaching the end and instead seek back to the start.
    /// </summary>
    bool IsLooping { get; set; }

    /// <summary>
    /// Sets the playback position to provided time in seconds.
    /// </summary>
    void Seek(float playbackPosition);
}
