using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Instant2D;
using Instant2D.Assets.Loaders;
using Instant2D.Core;

namespace Instant2D.TestGame {
    public class Game : InstantGame {
        protected override void SetupSystems() {
            AddSystem<AssetManager>(assets => {
                assets.AddLoader<DebugSpriteLoader>();
            });
        }
    }
}
