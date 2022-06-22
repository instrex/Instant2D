using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Runtime.CompilerServices;

namespace Instant2D {
    public static partial class Extensions {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Draw(this IDrawingBackend drawing, in Sprite sprite, Vector2 position, Color color, float rotation = 0f, float scale = 1f, SpriteEffects spriteEffects = SpriteEffects.None) =>
            drawing.Draw(sprite, position, color, rotation, new Vector2(scale), spriteEffects);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DrawAnimation(this IDrawingBackend drawing, in SpriteAnimation animation, Vector2 position, Color color, float rotation, Vector2 scale, SpriteEffects spriteEffects = SpriteEffects.None) {
            var timePerFrame = 1.0f / animation.Fps;
            drawing.Draw(animation.Frames[(int)(TimeManager.TotalTime / timePerFrame % animation.Frames.Length)], position, color, rotation, scale, spriteEffects);
        }
    }
}
