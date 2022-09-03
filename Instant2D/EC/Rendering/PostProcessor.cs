using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D.EC.Rendering {
    public abstract class PostProcessor {
        
        public abstract void Apply(RenderTarget2D source, RenderTarget2D destination);
    }
}
