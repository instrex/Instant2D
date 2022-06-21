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

        public override void Initialize() {
            Instance = this;
            ShouldUpdate = true;
        }
    }
}
