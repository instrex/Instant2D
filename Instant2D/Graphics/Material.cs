using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D.Graphics {
    /// <summary>
    /// Wrapper for values used during sprite drawing.
    /// </summary>
    public class Material {
        public BlendState BlendState { get; set; }
        public RasterizerState RasterizerState { get; set; }
        public SamplerState SamplerState { get; set; }
        public Effect Effect { get; set; }

        /// <summary>
        /// Default <see cref="Material"/> used when nothing has been provided.
        /// </summary>
        public static readonly Material Default = new() {
            BlendState = BlendState.NonPremultiplied,
            RasterizerState = RasterizerState.CullNone,
            SamplerState = SamplerState.PointClamp,
        };
    }
}
