using Instant2D.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
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
        /// Backend used for all drawing operations in the game.
        /// </summary>
        public static IDrawingBackend Backend { get; private set; }

        static Sprite? _pixelSprite;

        /// <summary>
        /// White pixel sprite with 1x1 dimensions. Initialized on-demand.
        /// </summary>
        public static Sprite Pixel { 
            get {
                if (_pixelSprite is null) {
                    var tex = new Texture2D(InstantGame.Instance.GraphicsDevice, 1, 1);
                    tex.SetData(new[] { Color.White });

                    // create a sprite with default stuff
                    _pixelSprite = new(tex, new Rectangle(0, 0, 1, 1), Vector2.Zero, ".instant-2d/pixel");
                }

                return _pixelSprite.Value;
            }
        }

        /// <summary>
        /// Sets the <see cref="Backend"/> property.
        /// </summary>
        public static void SetBackend<T>() where T: IDrawingBackend, new() {
            Backend = new T();
        }

        public override void Initialize() {
            Instance = this;

            if (Backend == null) {
                InstantGame.Instance.Logger.Warning("No GraphicsManager backend specified, defaulting to SpriteBatchBackend...");
                SetBackend<SpriteBatchBackend>();
            }
        }

    }
}
