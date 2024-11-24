using Instant2D.Assets.Sprites;
using System.Linq;

namespace Instant2D;

/// <summary>
/// Collection of sprites with optional events array.
/// </summary>
public readonly record struct SpriteAnimation {
    public string Key { get; init; }

    /// <summary>
    /// Animation speed in frames per second.
    /// </summary>
    public float Fps { get; init; }

    /// <summary>
    /// All of the sprite references used in this animation.
    /// </summary>
    public Sprite[] Frames { get; init; }

    /// <summary>
    /// Array of animation events, may be null if no events are specified.
    /// </summary>
    public SpriteDefinition.AnimationEvent[] Events { get; init; }

    public SpriteAnimation(float fps, Sprite[] frames, SpriteDefinition.AnimationEvent[] events = default, string key = default) {
        Fps = fps;
        Frames = frames;
        Events = events;
        Key = key;
    }

    // smooth conversion to Sprite for easier prototyping and asset resolution
    public static implicit operator Sprite(SpriteAnimation animation) => animation.Frames.FirstOrDefault();
}
