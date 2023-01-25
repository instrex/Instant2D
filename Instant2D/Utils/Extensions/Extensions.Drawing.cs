using Instant2D.EC;
using Instant2D.Graphics;
using Instant2D.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Runtime.CompilerServices;

namespace Instant2D {
    public static partial class Extensions {
        /// <inheritdoc cref="IDrawingBackend.Draw(in Sprite, Vector2, Color, float, Vector2, SpriteEffects)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DrawSprite(this DrawingContext drawing, in Sprite sprite, Vector2 position, Color color, float rotation = 0f, float scale = 1f, SpriteEffects spriteEffects = SpriteEffects.None) =>
            drawing.DrawSprite(sprite, position, color, rotation, new Vector2(scale), spriteEffects);

        /// <summary>
        /// Draws a looped animation using <see cref="TimeManager.TotalTime"/> or <see cref="Scene.TotalTime"/> when available.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DrawAnimation(this DrawingContext drawing, in SpriteAnimation animation, Vector2 position, Color color, float rotation, Vector2 scale, SpriteEffects spriteEffects = SpriteEffects.None) {
            var timePerFrame = 1.0f / animation.Fps;
            var time = SceneManager.Instance?.Current?.TotalTime ?? TimeManager.TotalTime;
            drawing.DrawSprite(animation.Frames[(int)(time / timePerFrame % animation.Frames.Length)], position, color, rotation, scale, spriteEffects);
        }

        /// <summary>
        /// Draws a simple line between two points.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DrawLine(this DrawingContext drawing, Vector2 a, Vector2 b, Color color, float thickness = 1.0f) {
            var dir = b - a;

            // here we need to supress rounding operations,
            // since the resulting line may appear incorrect
            var oldRounding = drawing.EnableRounding;
            drawing.EnableRounding = false;

            drawing.DrawSprite(GraphicsManager.Pixel, a, color, dir.ToAngle(), new Vector2(dir.Length(), thickness));

            // restore the original value there
            drawing.EnableRounding = oldRounding;
        }

        /// <summary>
        /// Draws a circle with specified <paramref name="radius"/> and <paramref name="color"/>. 
        /// <paramref name="resolution"/> can be used to control how many steps will be taken.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DrawCircle(this DrawingContext drawing, Vector2 position, float radius, Color color, float thickness = 1f, int resolution = 12) {
            var step = MathHelper.TwoPi / resolution;
            for (var i = 1; i <= resolution; i++) {
                var (from, to) = (
                    position + VectorUtils.ToVector2(step * (i - 1)) * radius,
                    position + VectorUtils.ToVector2(step * i) * radius
                );

                DrawLine(drawing, from, to, color, thickness);
            }
        }

        /// <summary>
        /// Draws a rectangle with optional outline.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DrawRectangle(this DrawingContext drawing, RectangleF rect, Color fillColor, Color outlineColor = default, float thickness = 1.0f) {
            // here we need to supress rounding operations,
            // since the resulting rect may appear incorrect
            var oldRounding = drawing.EnableRounding;
            drawing.EnableRounding = false;

            drawing.DrawSprite(GraphicsManager.Pixel, rect.Position, fillColor, 0, rect.Size);

            // draw outline
            if (outlineColor != default) {
                DrawLine(drawing, rect.Position, new Vector2(rect.X + rect.Width, rect.Y), outlineColor, thickness);
                DrawLine(drawing, new Vector2(rect.X + rect.Width, rect.Y), new Vector2(rect.X + rect.Width, rect.Y + rect.Height), outlineColor, thickness);
                DrawLine(drawing, new Vector2(rect.X + rect.Width, rect.Y + rect.Height), new Vector2(rect.X, rect.Y + rect.Height), outlineColor, thickness);
                DrawLine(drawing, new Vector2(rect.X, rect.Y + rect.Height), rect.Position, outlineColor, thickness);
            }

            drawing.EnableRounding = oldRounding;
        }

        /// <summary>
        /// Draw a pixel point of specified color and size.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DrawPoint(this DrawingContext drawing, Vector2 position, Color color, float scale = 1f) {
            // here we need to supress rounding operations,
            // since the resulting point may appear incorrectly
            var oldRounding = drawing.EnableRounding;
            drawing.EnableRounding = false;

            drawing.DrawSprite(GraphicsManager.Pixel, position - new Vector2(scale * 0.5f), color, 0, scale);

            // restore the original value there
            drawing.EnableRounding = oldRounding;
        }

        /// <summary>
        /// Draws text using specified font.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DrawString(this DrawingContext drawing, ISpriteFont font, string text, Vector2 position, Color color, Vector2 scale, float rotation, int maxDisplayedCharacters = int.MaxValue, bool drawOutline = false) {
            if (drawOutline) {
                for (var i = 0; i < 4; i++) {
                    font.DrawString(drawing, text, position + new Vector2(1, 0).RotatedBy(i * MathHelper.PiOver2) * scale, Color.Black, scale, rotation, maxDisplayedCharacters);
                }
            }

            font.DrawString(drawing, text, position, color, scale, rotation, maxDisplayedCharacters);
        }

        /// <summary>
        /// Draws text using <see cref="GraphicsManager.DefaultFont"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DrawString(this DrawingContext drawing, string text, Vector2 position, Color color, Vector2 scale, float rotation, int maxDisplayedCharacters = int.MaxValue, bool drawOutline = false) {
            DrawString(drawing, GraphicsManager.DefaultFont, text, position, color, scale, rotation, maxDisplayedCharacters, drawOutline);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Color Multiply(this Color a, in Color b, float? alpha = default) => new(
            a.R / 255f * (b.R / 255f),
            a.G / 255f * (b.G / 255f),
            a.B / 255f * (b.B / 255f),
            alpha ?? a.A / 255f * (b.A / 255f));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Color WithAlpha(this Color color, float alpha) => new(color.R, color.G, color.B, (byte)(255 * alpha));
    }
}
