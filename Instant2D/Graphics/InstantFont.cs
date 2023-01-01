using Instant2D.Assets.Loaders;
using Instant2D.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D.Graphics {
    /// <summary>
    /// Very simple font implementation that supports string measuring and rendering. <br/>
    /// If you need any features not implemented there, feel free to exteng it using <see cref="ISpriteFont"/>.
    /// </summary>
    public class InstantFont : ISpriteFont {
        public struct Glyph {
            public Sprite sprite;
            public char character;
            public Point offset;
            public int advanceX;
        }

        readonly Dictionary<char, Glyph> _glyphs;
        readonly char? _defaultChar;
        readonly int _lineSpacing;

        /// <summary>
        /// Creates an instance of SpriteFont. Note that all character sprites should have an origin of {0, 0}.
        /// </summary>
        public InstantFont(IEnumerable<Glyph> glyphs, int lineSpacing, char? defaultChar = default) {
            _glyphs = glyphs.ToDictionary(g => g.character);
            _lineSpacing = lineSpacing;
            _defaultChar = defaultChar;
        }

        public int LineSpacing => _lineSpacing;

        public Sprite? this[char character] {
            get => _glyphs.TryGetValue(character, out var glyph) ? glyph.sprite : null;
        }

        public void DrawString(DrawingContext drawing, string text, Vector2 position, Color color, Vector2 scale, float rotation, int maxDisplayedCharacters = int.MaxValue) {
            var length = Math.Min(text.Length, maxDisplayedCharacters);
            var currentPos = new Vector2();
            for (var i = 0; i < length; i++) {
                var currentChar = text[i];

                // check glyph
                if (!_glyphs.TryGetValue(currentChar, out var glyph)) {
                    if (_defaultChar is char Default && !_glyphs.TryGetValue(Default, out glyph)) {
                        InstantApp.Logger.Error($"The font is missing the character '{currentChar}' and default is not set.");
                        break;
                    }
                }

                // insert linebreaks
                if (currentChar == '\n') {
                    currentPos.X = 0;
                    currentPos.Y += _lineSpacing;
                    continue;
                }

                // handle drawing
                var glyphPos = (currentPos + glyph.offset.ToVector2()) * scale;
                drawing.DrawSprite(glyph.sprite, position + (rotation != 0 ? glyphPos.RotatedBy(rotation) : glyphPos), color, rotation, scale);

                // advance the rendering
                currentPos.X += glyph.advanceX;
            }
        }

        public Vector2 MeasureString(string text) {
            // empty string passed...
            if (string.IsNullOrEmpty(text)) {
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
                    result = new(Math.Max(lineWidth, result.X), result.Y + _lineSpacing);
                    lineWidth = 0;
                    continue;
                }

                // get the glyph
                if (!_glyphs.TryGetValue(currentChar, out var glyph)) {
                    if (_defaultChar is char Default && !_glyphs.TryGetValue(Default, out glyph)) {
                        InstantApp.Logger.Error($"The font is missing the character '{currentChar}' and default is not set.");
                        break;
                    }
                }

                lineWidth += glyph.advanceX;
            }

            // add the final line
            if (lineWidth > 0) {
                result = new(Math.Max(lineWidth, result.X), result.Y + _lineSpacing);
            }

            return result;
        }
    }
}
