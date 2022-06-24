using Instant2D.Core;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D.EC {
    /// <summary>
    /// The meat of entity system. 
    /// </summary>
    public class SceneManager : SubSystem {
        public static SceneManager Instance { get; set; }

        readonly Stack<Scene> _sceneStack = new(8);

        Scene _current;
        public Scene Current {
            get => _current;
            set {
                _current = value;
            }
        }

        public override void Initialize() {
            Instance = this;
            ShouldUpdate = true;

            // setup the client size change callback for resizing RTs and stuff
            InstantGame.Instance.Window.ClientSizeChanged += ClientSizeChangedCallback;
        }

        public override void Update(GameTime time) {
            _current?.InternalUpdate();
        }

        private void ClientSizeChangedCallback(object sender, EventArgs e) {
            _current?.ResizeRenderTargets();
        }
    }
}
 