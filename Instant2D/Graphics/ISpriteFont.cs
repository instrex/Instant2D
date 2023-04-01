using Microsoft.Xna.Framework;
using System;

namespace Instant2D.Graphics {
    /// <summary> 
    /// Common interface for many SpriteFont implementations when you find <see cref="InstantFont"/> not cool enough.
    /// </summary>
    public interface ISpriteFont {
        /// <summary>
        /// Amount of space inserted between text lines.
        /// </summary>
        int LineSpacing { get; }

        /// <summary>
        /// Measures maximum line width and heigh of <paramref name="text"/>.
        /// </summary>
        Vector2 MeasureString(ReadOnlySpan<char> text);

        /// <summary>
        /// Renders the <paramref name="text"/> into <paramref name="drawing"/>.
        /// </summary>
        void DrawString(DrawingContext drawing, ReadOnlySpan<char> text, Vector2 position, Color color, Vector2 scale, float rotation, int maxDisplayedCharacters = int.MaxValue);

        /// <summary>
        /// Get the sprite of a text character.
        /// </summary>
        Sprite? this[char character] { get; } 
    }
}
