using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D.Assets.Sprites;

public partial record struct SpriteDefinition {
    /// <summary>
    /// Animation event description. <paramref name="Args"/> will be passed to <see cref="EC.SpriteAnimationComponent"/>s during playback.
    /// </summary>
    public readonly record struct AnimationEvent(int FrameIndex, string Key, object[] Args);

    /// <summary>
    /// Animation description, used to determine its fps and triggering events.
    /// </summary>
    public readonly record struct AnimationDefinition(int Fps, AnimationEvent[] Events);
}
