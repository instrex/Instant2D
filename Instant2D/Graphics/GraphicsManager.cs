using Instant2D.Assets.Fonts;
using Instant2D.Assets.Loaders;
using Instant2D.Modules;
using Instant2D.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D.Graphics;

/// <summary>
/// System that handles rendering. Feel free to subclass it and modify however you wish if it doesn't match your needs!
/// </summary>
public class GraphicsManager : IGameSystem {
    public static GraphicsManager Instance { get; set; }

    /// <summary>
    /// A <see cref="DrawingContext"/> instance used for all drawing operations. 
    /// </summary>
    public static DrawingContext Context { get; private set; }

    static Sprite? _pixelSprite;
    static I2dFont _defaultFont;

    /// <summary>
    /// White pixel sprite with 1x1 dimensions. Initialized on-demand.
    /// </summary>
    public static Sprite Pixel { 
        get {
            if (_pixelSprite is null) {
                var tex = new Texture2D(InstantApp.Instance.GraphicsDevice, 1, 1);
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
    public static I2dFont DefaultFont {
        get {
            if (_defaultFont is null) {
                const string NAMESPACE = "Instant2D.Resources.";

                // perform black magic ritual to load json description and texture
                using var textureStream = typeof(GraphicsManager).Assembly.GetManifestResourceStream(NAMESPACE + "default_font.png");
                var texture = Texture2D.FromStream(InstantApp.Instance.GraphicsDevice, textureStream);

                using var descStream = typeof(GraphicsManager).Assembly.GetManifestResourceStream(NAMESPACE + "default_font.fnt");
                using var reader = new StreamReader(descStream);

                try {
                    _defaultFont = FontParser.LoadFnt(reader.ReadToEnd(), new[] { texture });
                    InstantApp.Logger.Info("Created default font.");
                } catch (Exception ex) {
                    InstantApp.Logger.Error($"Error parsing default font: {ex.Message}");
                    throw;
                }
            }

            return _defaultFont;
        }
    }

    float IGameSystem.UpdateOrder { get; }
    void IGameSystem.Update(InstantApp app, float deltaTime) { }
    void IGameSystem.Initialize(InstantApp app) {
        Instance = this;

        // initialize drawing context
        Context = new();
    }
}
