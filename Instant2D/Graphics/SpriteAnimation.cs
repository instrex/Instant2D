using Instant2D.Assets.Sprites;

namespace Instant2D {
    public readonly struct SpriteAnimation {
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
        public AnimationEvent[] Events { get; init; }

        public SpriteAnimation(float fps, Sprite[] frames, AnimationEvent[] events = default, string key = default) {
            Fps = fps;
            Frames = frames;
            Events = events;
            Key = key;
        }
    }
}
