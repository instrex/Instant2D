using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace Instant2D.Graphics;

/// <summary> 
/// Common interface for many SpriteFont implementations when you find <see cref="I2dFont"/> not cool enough.
/// </summary>
public interface ISpriteFont {
    /// <summary>
    /// Represents a generic font glyph.
    /// </summary>
    /// <param name="Sprite"> Sprite of this character. </param>
    /// <param name="Character"> Character entry. </param>
    /// <param name="Offset"> Applied offset when drawing this character. </param>
    /// <param name="AdvanceX"> Applied horizontal advancement after drawing this character. </param>
    public record struct Glyph(Sprite Sprite, char Character, Point Offset, int AdvanceX);

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
    void DrawString(DrawingContext drawing, ReadOnlySpan<char> text, Vector2 position, Color color, Vector2 scale, float rotation);

    /// <summary>
    /// Glyph information for this font.
    /// </summary>
    Dictionary<char, Glyph> Glyphs { get; }
}
