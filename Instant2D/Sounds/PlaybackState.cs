namespace Instant2D.Sounds;

public enum PlaybackState {
    /// <summary>
    /// Sound is currently active and playing.
    /// </summary>
    Playing,

    /// <summary>
    /// Playback is paused and can be resumed.
    /// </summary>
    Paused,

    /// <summary>
    /// Playback is stopped or haven't begun yet.
    /// </summary>
    Stopped
}
