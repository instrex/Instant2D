using Instant2D.Assets.Loaders;
using Instant2D.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D.Graphics;

/// <summary>
/// Default font renderer.
/// </summary>
public class I2dFont : ISpriteFont {
    public Dictionary<char, ISpriteFont.Glyph> Glyphs { get; init; }
    public int LineSpacing { get; init; }

    /// <summary>
    /// Default character used when unknown entry is encountered.
    /// </summary>
    public char DefaultCharacter { get; init; } = '?';

    public void DrawString(DrawingContext drawing, ReadOnlySpan<char> text, Vector2 position, Color color, Vector2 scale, float rotation) {
        var currentPos = new Vector2();
        for (var i = 0; i < text.Length; i++) {
            var currentChar = text[i];

            // check glyph
            if (!Glyphs.TryGetValue(currentChar, out var glyph) || glyph.Sprite.Texture is null) {
                // try fallback to default char
                if (!Glyphs.TryGetValue(DefaultCharacter, out glyph) || glyph.Sprite.Texture is null) {
                    InstantApp.Logger.Error($"The font is missing the character '{currentChar}' and default is not set.");
                    break;
                }
            }

            // insert linebreaks
            if (currentChar == '\n') {
                currentPos.Y += LineSpacing;
                currentPos.X = 0;
                continue;
            }

            // handle drawing
            var glyphPos = (currentPos + glyph.Offset.ToVector2()) * scale;
            drawing.DrawSprite(glyph.Sprite, position + (rotation != 0 ? glyphPos.RotatedBy(rotation) : glyphPos), color, rotation, scale);

            // advance the rendering
            currentPos.X += glyph.AdvanceX;
        }
    }

    public Vector2 MeasureString(ReadOnlySpan<char> text) {
        // empty string passed...
        if (text.IsEmpty) {
            return Vector2.Zero;
        }

        var result = Vector2.Zero;
        var lineWidth = 0;
        for (var i = 0; i < text.Length; i++) {
            var currentChar = text[i];
            if (currentChar == '\r') {
                continue;
            }

            // reset on newline
            if (currentChar == '\n') {
                result = new(Math.Max(lineWidth, result.X), result.Y + LineSpacing);
                lineWidth = 0;
                continue;
            }

            // get the glyph
            if (!Glyphs.TryGetValue(currentChar, out var glyph)) {
                if (!Glyphs.TryGetValue(DefaultCharacter, out glyph)) {
                    InstantApp.Logger.Error($"The font is missing the character '{currentChar}' and default is not set.");
                    break;
                }
            }

            lineWidth += glyph.AdvanceX;
        }

        // add the final line
        if (lineWidth > 0) {
            result = new(Math.Max(lineWidth, result.X), result.Y + LineSpacing);
        }

        return result;
    }
}
