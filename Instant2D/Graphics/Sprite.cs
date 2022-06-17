using Instant2D.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D {
    /// <summary>
    /// Wrapper for <see cref="Texture2D"/>s, SourceRects and Origins.
    /// </summary>
    public readonly struct Sprite {
        public Texture2D Texture { get; init; }
        public Rectangle SourceRect { get; init; }
        public string Key { get; init; }

        readonly Point _origin;

        /// <summary>
        /// Direct access to sprite's origin.
        /// </summary>
        public Point Origin { 
            get => _origin;
            init => _origin = value;
        }

        /// <summary>
        /// Access to normalized origin, uses values ranging [0.0 - 1.0]
        /// </summary>
        public Vector2 NormalizedOrigin {
            get => _origin.ToVector2();
            init => Origin = (new Vector2(SourceRect.Width, SourceRect.Height) * value).RoundToPoint();
        }

        /// <summary>
        /// Initializes <see cref="SourceRect"/> to <paramref name="texture"/>'s size and <see cref="Origin"/> to center.
        /// </summary>
        /// <param name="texture"></param>
        public Sprite(Texture2D texture, string key = "") {
            Texture = texture;
            SourceRect = new(0, 0, Texture.Width, Texture.Height);
            Key = key;

            _origin = new(Texture.Width / 2, Texture.Height / 2);
        }

        public Sprite(Texture2D texture, Rectangle sourceRect, Point origin, string key = "") {
            Texture = texture;
            SourceRect = sourceRect;
            Key = key;

            _origin = origin;
        }

        public override string ToString() => $"{SourceRect}, {Origin}";
    }
}
