using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D.Assets.Sprites;

public partial record struct SpriteDefinition {
    public enum SplitType {
        None,

        /// <summary>
        /// Split options were defined as dimensions of the frame.
        /// </summary>
        BySize,

        /// <summary>
        /// Split options were defined as frame count.
        /// </summary>
        ByCount,

        /// <summary>
        /// Split options were defined manually.
        /// </summary>
        BySubSprites
    }

    /// <summary>
    /// Sub sprite definition used with <see cref="SplitType.BySubSprites"/>.
    /// </summary>
    public record struct SubSprite(string Key, Rectangle Region, OriginDefinition Origin);

    /// <summary>
    /// Sprite split options to use when slicing up the sprite.
    /// </summary>
    public record struct SpriteSplitOptions(SplitType Type, int WidthOrFrameCount, int Height, SubSprite[] SubSprites);
}
