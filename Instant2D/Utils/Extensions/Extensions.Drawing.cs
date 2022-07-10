using Instant2D.EC;
using Instant2D.Graphics;
using Instant2D.Utils;
using Instant2D.Utils.Math;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Runtime.CompilerServices;

namespace Instant2D {
    public static partial class Extensions {
        /// <inheritdoc cref="IDrawingBackend.Draw(in Sprite, Vector2, Color, float, Vector2, SpriteEffects)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Draw(this IDrawingBackend drawing, in Sprite sprite, Vector2 position, Color color, float rotation = 0f, float scale = 1f, SpriteEffects spriteEffects = SpriteEffects.None) =>
            drawing.Draw(sprite, position, color, rotation, new Vector2(scale), spriteEffects);

        /// <summary>
        /// Draws a looped animation using <see cref="TimeManager.TotalTime"/> or <see cref="Scene.TotalTime"/> when available.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DrawAnimation(this IDrawingBackend drawing, in SpriteAnimation animation, Vector2 position, Color color, float rotation, Vector2 scale, SpriteEffects spriteEffects = SpriteEffects.None) {
            var timePerFrame = 1.0f / animation.Fps;
            var time = SceneManager.Instance?.Current?.TotalTime ?? TimeManager.TotalTime;
            drawing.Draw(animation.Frames[(int)(time / timePerFrame % animation.Frames.Length)], position, color, rotation, scale, spriteEffects);
        }

        /// <summary>
        /// Draws a line between two points.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DrawLine(this IDrawingBackend drawing, Vector2 a, Vector2 b, Color color, float thickness = 1.0f) {
            var dir = b - a;
            drawing.Draw(GraphicsManager.Pixel, a, color, dir.ToAngle(), new Vector2(dir.Length(), thickness));
        }

        /// <summary>
        /// Draws a rectangle with optional outline.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DrawRectangle(this IDrawingBackend drawing, RectangleF rect, Color fillColor, Color outlineColor = default, float thickness = 1.0f) {
            drawing.Draw(GraphicsManager.Pixel, rect.Position, fillColor, 0, rect.Size);

            // draw outline
            if (outlineColor != default) {
                DrawLine(drawing, rect.Position, new Vector2(rect.X + rect.Width, rect.Y), outlineColor, thickness);
                DrawLine(drawing, new Vector2(rect.X + rect.Width, rect.Y), new Vector2(rect.X + rect.Width, rect.Y + rect.Height), outlineColor, thickness);
                DrawLine(drawing, new Vector2(rect.X + rect.Width, rect.Y + rect.Height), new Vector2(rect.X, rect.Y + rect.Height), outlineColor, thickness);
                DrawLine(drawing, new Vector2(rect.X, rect.Y + rect.Height), rect.Position, outlineColor, thickness);
            }
        }

        /// <summary>
        /// Draws text using specified font.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DrawString(this IDrawingBackend drawing, ISpriteFont font, string text, Vector2 position, Color color, Vector2 scale, float rotation, int maxDisplayedCharacters = int.MaxValue, bool drawOutline = false) {
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
        public static void DrawString(this IDrawingBackend drawing, string text, Vector2 position, Color color, Vector2 scale, float rotation, int maxDisplayedCharacters = int.MaxValue, bool drawOutline = false) {
            DrawString(drawing, GraphicsManager.DefaultFont, text, position, color, scale, rotation, maxDisplayedCharacters, drawOutline);
        }
    }
}
