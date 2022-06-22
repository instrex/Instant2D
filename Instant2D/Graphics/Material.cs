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
    public readonly struct Material : IEquatable<Material> {
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

        public override bool Equals(object obj) {
            return obj is Material material && Equals(material);
        }

        public bool Equals(Material other) {
            return EqualityComparer<BlendState>.Default.Equals(BlendState, other.BlendState) &&
                   EqualityComparer<RasterizerState>.Default.Equals(RasterizerState, other.RasterizerState) &&
                   EqualityComparer<SamplerState>.Default.Equals(SamplerState, other.SamplerState) &&
                   EqualityComparer<DepthStencilState>.Default.Equals(DepthStencilState, other.DepthStencilState) &&
                   EqualityComparer<Effect>.Default.Equals(Effect, other.Effect);
        }

        public override int GetHashCode() {
            return HashCode.Combine(BlendState, RasterizerState, SamplerState, DepthStencilState, Effect);
        }

        public static bool operator ==(Material left, Material right) {
            return left.Equals(right);
        }

        public static bool operator !=(Material left, Material right) {
            return !(left == right);
        }
    }
}
