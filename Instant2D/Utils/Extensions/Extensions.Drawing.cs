using Instant2D.EC;
using Instant2D.Graphics;
using Instant2D.Utils;
using Instant2D.Utils.Math;
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
            var time = SceneManager.Instance?.Current?.TotalTime ?? TimeManager.TotalTime;
            drawing.Draw(animation.Frames[(int)(time / timePerFrame % animation.Frames.Length)], position, color, rotation, scale, spriteEffects);
        }

        public static void DrawLine(this IDrawingBackend drawing, Vector2 a, Vector2 b, Color color, float thickness = 1.0f) {
            var dir = b - a;
            drawing.Draw(GraphicsManager.Pixel, a, color, dir.ToAngle(), new Vector2(dir.Length(), thickness));
        }

        public static void DrawHollowRect(this IDrawingBackend drawing, RectangleF rect, Color color, float thickness = 1.0f) {
            DrawLine(drawing, rect.Position, new Vector2(rect.X + rect.Width, rect.Y), color, thickness);
            DrawLine(drawing, new Vector2(rect.X + rect.Width, rect.Y), new Vector2(rect.X + rect.Width, rect.Y + rect.Height), color, thickness);
            DrawLine(drawing, new Vector2(rect.X + rect.Width, rect.Y + rect.Height), new Vector2(rect.X, rect.Y + rect.Height), color, thickness);
            DrawLine(drawing, new Vector2(rect.X, rect.Y + rect.Height), rect.Position, color, thickness);
        }
    }
}
