using Instant2D.Graphics;
using Instant2D.Utils.Math;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D.EC.Components {
    public class TextComponent : RenderableComponent {
        RectangleF _bounds;
        bool _boundsDirty, _textDirty;
        ISpriteFont _font = GraphicsManager.DefaultFont;
        Vector2 _normalizedOrigin = new(0.5f);
        Vector2 _textSize, _offset;
        int _displayedCharacters = int.MaxValue;
        string _text;

        public override RectangleF Bounds {
            get {
                if (_boundsDirty) {
                    _bounds = CalculateBounds(Entity.Transform.Position, _offset, Vector2.Zero, _textSize, Entity.Transform.Rotation, Entity.Transform.Scale);
                    _boundsDirty = false;
                }

                return _bounds;
            }
        }

        /// <summary>
        /// Changes the displayed text.
        /// </summary>
        public string Content {
            get => _text;
            set {
                if (_text == value)
                    return;

                _text = value;
                _textDirty = true;
            }
        }

        /// <summary>
        /// Changes the font. Defaults to <see cref="GraphicsManager.DefaultFont"/>.
        /// </summary>
        public ISpriteFont Font {
            get => _font;
            set {
                _font = value;
                _textDirty = true;
            }
        }

        /// <summary>
        /// Changes how many characters are rendered on screen. Defaults to <see cref="int.MaxValue"/>.
        /// </summary>
        public int DisplayedCharacters {
            get => _displayedCharacters;
            set => _displayedCharacters = value;
        }

        /// <summary>
        /// Changes how the text is aligned. (0, 0) is top-left, (1, 1) is bottom-right. Defaults to centered (0.5, 0.5).
        /// </summary>
        public Vector2 Origin {
            get => _normalizedOrigin;
            set {
                _normalizedOrigin = value;
                _textDirty = true;
            }
        }

        #region Setters

        /// <inheritdoc cref="Content"/>
        public TextComponent SetContent(string content) {
            Content = content;
            return this;
        }

        /// <inheritdoc cref="Font"/>
        public TextComponent SetFont(ISpriteFont font) {
            Font = font;
            return this;
        }

        /// <inheritdoc cref="DisplayedCharacters"/>
        public TextComponent SetDisplayedCharacters(int displayedCharacters) {
            DisplayedCharacters = displayedCharacters;
            return this;
        }

        /// <inheritdoc cref="Origin"/>
        public TextComponent SetOrigin(Vector2 origin) {
            Origin = origin;
            return this;
        }

        #endregion

        public override void Draw(IDrawingBackend drawing, CameraComponent camera) {
            if (_textDirty) {
                _textSize = _font.MeasureString(_text);
                _offset = (_textSize * _normalizedOrigin * -1f).Round();
                _textDirty = false;
                _boundsDirty = true;
            }

            drawing.DrawString(_text, Entity.Transform.Position + _offset * Entity.Transform.Scale, Color, Entity.Transform.Scale, Entity.Transform.Rotation, _displayedCharacters);
        }
    }
}
