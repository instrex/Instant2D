using Instant2D.Core;
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
        public DrawingBackend Backend { get; private set; }

        /// <summary>
        /// Sets the <see cref="Backend"/> property.
        /// </summary>
        public void SetBackend<T>() where T: DrawingBackend, new() {
            Backend = new T();
        }

        public override void Initialize() {
            Instance = this;

            if (Backend == null) {
                InstantGame.Instance.Logger.Warning("No GraphicsManager backend specified, setting to SpriteBatchBackend...");
                SetBackend<SpriteBatchBackend>();
            }
        }

    }
}
