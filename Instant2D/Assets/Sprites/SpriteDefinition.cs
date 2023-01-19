using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace Instant2D.Assets.Sprites {
    public readonly partial record struct SpriteDefinition {
        public required string Key { get; init; }

        /// <summary>
        /// Signalizes if this definition has inherited properties from another definition. <br/>
        /// Represents <see cref="Key"/> of another definition.
        /// </summary>
        public string Inherit { get; init; }

        /// <summary>
        /// Collection of points in the sprite space. Is only valid for definitions with no animation defined.
        /// </summary>
        public Dictionary<string, Point> Points { get; init; }

        /// <summary>
        /// Signalizes if this definition was not defined in any manifest, thus its entry was created automatically.
        /// </summary>
        public bool IsAutomaticallyGenerated { get; init; }

        /// <summary>
        /// Sprite origin description. In case if this is not set, manifest's default origin value would be used.
        /// </summary>
        public OriginDefinition Origin { get; init; }

        /// <summary>
        /// Optional split options to produce multiple sprite assets out of one sheet.
        /// </summary>
        public SpriteSplitOptions? SplitOptions { get; init; }

        /// <summary>
        /// Optional animation playback options.
        /// </summary>
        public AnimationDefinition? Animation { get; init; }
    }
}
