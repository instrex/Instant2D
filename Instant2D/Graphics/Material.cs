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
    public record Material {
        public Material(BlendState blendState = default, SamplerState samplerState = default, Effect effect = default, RasterizerState rasterizerState = default, DepthStencilState depthStencilState = default) {
            BlendState = blendState ?? BlendState.NonPremultiplied;
            RasterizerState = rasterizerState ?? RasterizerState.CullNone;
            SamplerState = samplerState ?? SamplerState.PointClamp;
            DepthStencilState = depthStencilState ?? DepthStencilState.None;
            Effect = effect;
        }

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
            SamplerState = SamplerState.PointClamp
        };

        /// <summary>
        /// Material with Opaque <see cref="BlendState"/>.
        /// </summary>
        public static readonly Material Opaque = new() {
            BlendState = BlendState.Opaque,
            SamplerState = SamplerState.PointClamp
        };
    }
}
