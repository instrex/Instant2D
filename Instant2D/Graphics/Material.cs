using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D.Graphics; 

/// <summary>
/// Wrapper for values used for rendering.
/// </summary>
public readonly struct Material {
    public BlendState BlendState { get; init; }
    public RasterizerState RasterizerState { get; init; }
    public SamplerState SamplerState { get; init; }
    public DepthStencilState DepthStencilState { get; init; }
    public Effect Effect { get; init; }

    public Material() {
        BlendState = BlendState.AlphaBlend;
        RasterizerState = RasterizerState.CullNone;
        SamplerState = SamplerState.PointClamp;
        DepthStencilState = DepthStencilState.None;
        Effect = null;
    }

    public Material(BlendState blendState = default, SamplerState samplerState = default, Effect effect = default, RasterizerState rasterizerState = default, DepthStencilState depthStencilState = default) : this() {
        if (blendState is not null) 
            BlendState = blendState;
        if (rasterizerState is not null) 
            RasterizerState = rasterizerState;
        if (samplerState is not null) 
            SamplerState = samplerState;
        if (depthStencilState is not null) 
            DepthStencilState = depthStencilState;
        if (Effect is not null) 
            Effect = effect;
    }

    #region Equality

    public override int GetHashCode() => HashCode.Combine(BlendState, RasterizerState, SamplerState, DepthStencilState, Effect);

    public override bool Equals([NotNullWhen(true)] object obj) => obj is Material other && this == other;

    public static bool operator ==(Material left, Material right) {
        return left.BlendState == right.BlendState &&
            left.RasterizerState == right.RasterizerState &&
            left.SamplerState == right.SamplerState &&
            left.DepthStencilState == right.DepthStencilState &&
            left.Effect == right.Effect;
    }

    public static bool operator !=(Material left, Material right) {
        return !(left == right);
    }

    #endregion

    #region Material presets

    /// <summary>
    /// Material with AlphaBlend blend state.
    /// </summary>
    public static readonly Material AlphaBlend = new(BlendState.AlphaBlend);

    /// <summary>
    /// Material with Opaque blend state.
    /// </summary>
    public static readonly Material Opaque = new(BlendState.Opaque);

    /// <summary>
    /// Material with Additive blend state.
    /// </summary>
    public static readonly Material Additive = new(BlendState.Additive);

    

    #endregion
}
