using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D.Assets.Sprites;

public partial record struct SpriteDefinition {

    /// <summary>
    /// A sprite point definition. May include frame index/key or not.
    /// </summary>
    /// <param name="Position"> Point offset from the top left corner of the sprite. </param>
    public record struct PointDefinition(string Key, Point Position) {
        /// <summary>
        /// Optional frame index.
        /// </summary>
        public int? FrameIndex { get; set; }

        /// <summary>
        /// Optional frame key for when SplitType.BySubSprites is used.
        /// </summary>
        public string FrameKey { get; set; }
    }
}