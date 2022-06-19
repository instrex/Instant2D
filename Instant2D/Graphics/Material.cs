using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D.Graphics {
    /// <summary>
    /// Wrapper for values used during rendering.
    /// </summary>
    public readonly struct Material {
        public BlendState BlendState { get; init; }
        public RasterizerState RasterizerState { get; init; }
        public SamplerState SamplerState { get; init; }
        public DepthStencilState DepthStencilState { get; init; }
        public Effect Effect { get; init; }

        /// <summary>
        /// Default <see cref="Material"/> used when nothing has been provided.
        /// </summary>
        public static readonly Material Default = new() {
            BlendState = BlendState.NonPremultiplied,
            SamplerState = SamplerState.PointClamp,
        };
    }
}
