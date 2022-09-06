using Instant2D.Graphics;
using Instant2D.Utils.Math;
using Microsoft.Xna.Framework;
using System.Runtime.CompilerServices;

namespace Instant2D.EC.Components {
    public class TextComponent : RenderableComponent {
        ISpriteFont _font = GraphicsManager.DefaultFont;
        Vector2 _normalizedOrigin = new(0.5f);
        Vector2 _textSize, _offset;
        int _displayedCharacters = int.MaxValue;
        string _text = "";

        /// <summary>
        /// Changes the displayed text.
        /// </summary>
        public string Content {
            get => _text;
            set {
                if (_text == value)
                    return;

                _text = value;
                UpdatePositioning();
            }
        }

        /// <summary>
        /// Changes the font. Defaults to <see cref="GraphicsManager.DefaultFont"/>.
        /// </summary>
        public ISpriteFont Font {
            get => _font;
            set {
                _font = value;
                UpdatePositioning();
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
                UpdatePositioning();
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void UpdatePositioning() {
            _textSize = _font.MeasureString(_text);
            _offset = _textSize * _normalizedOrigin * -1f;
            _boundsDirty = true;
        }

        protected override void RecalculateBounds(ref RectangleF bounds) {
            bounds = CalculateBounds(Entity.Transform.Position, _offset, Vector2.Zero, _textSize, Entity.Transform.Rotation, Entity.Transform.Scale);
        }

        public override void Draw(DrawingContext drawing, CameraComponent camera) {
            drawing.DrawString(_text, (Entity.Transform.Position + _offset * Entity.Transform.Scale + Offset).Round(),
                Color, Entity.Transform.Scale, Entity.Transform.Rotation, _displayedCharacters);
        }
    }
}
