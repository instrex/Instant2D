using Instant2D.Core;
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
        

        public override void Initialize() {
            Instance = this;
            ShouldUpdate = true;

            // setup the client size change callback for resizing RTs and stuff
            InstantGame.Instance.Window.ClientSizeChanged += ClientSizeChangedCallback;
        }

        private void ClientSizeChangedCallback(object sender, EventArgs e) {
            throw new NotImplementedException();
        }
    }
}
 