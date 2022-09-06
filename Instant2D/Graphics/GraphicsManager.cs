using Instant2D.Assets.Loaders;
using Instant2D.Core;
using Instant2D.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D.Graphics {
    /// <summary>
    /// System that handles rendering. Feel free to subclass it and modify however you wish if it doesn't match your needs!
    /// </summary>
    public class GraphicsManager : SubSystem {
        public static GraphicsManager Instance { get; set; }

        /// <summary>
        /// A <see cref="DrawingContext"/> instance used for all drawing operations. 
        /// </summary>
        public static DrawingContext Context { get; private set; }

        static Sprite? _pixelSprite;
        static InstantFont _defaultFont;

        /// <summary>
        /// White pixel sprite with 1x1 dimensions. Initialized on-demand.
        /// </summary>
        public static Sprite Pixel { 
            get {
                if (_pixelSprite is null) {
                    var tex = new Texture2D(InstantGame.Instance.GraphicsDevice, 1, 1);
                    tex.SetData(new[] { Color.White });

                    // create a sprite with default stuff
                    _pixelSprite = new(tex, new Rectangle(0, 0, 1, 1), Vector2.Zero, "i2d/pixel");
                }

                return _pixelSprite.Value;
            }
        }

        /// <summary>
        /// Default sprite font instance. Initialized on-demand.
        /// </summary>
        public static InstantFont DefaultFont {
            get {
                if (_defaultFont is null) {
                    const string NAMESPACE = "Instant2D.Resources.";

                    // perform black magic ritual to load json description and texture
                    using var textureStream = typeof(GraphicsManager).Assembly.GetManifestResourceStream(NAMESPACE + "default_font.png");
                    var texture = Texture2D.FromStream(InstantGame.Instance.GraphicsDevice, textureStream);

                    using var descStream = typeof(GraphicsManager).Assembly.GetManifestResourceStream(NAMESPACE + "default_font.json");
                    using var reader = new StreamReader(descStream);
                    if (!LegacyFontLoader.TryParse(reader.ReadToEnd(), out var fontDesc)) {
                        Logger.WriteLine("Couldn't parse default font, something has gone very wrong...", Logger.Severity.Error);
                        return null;
                    }

                    _defaultFont = new InstantFont(fontDesc.CreateGlyphs(new[] { texture }), fontDesc.lineSpacing, fontDesc.defaultChar);
                }

                return _defaultFont;
            }
        }

        public override void Initialize() {
            Instance = this;

            // initialize drawing context
            Context = new();
        }

    }
}
