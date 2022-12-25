using Instant2D.Graphics;
using Instant2D.Utils;
using Microsoft.Xna.Framework;
using System;
using System.Collections;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D.EC.Rendering {
    /// <summary>
    /// Base render layer interface for scene rendering.
    /// </summary>
    public interface IRenderLayer : IComparable<IRenderLayer> {
        // default comparable implementation for sorting by order
        int IComparable<IRenderLayer>.CompareTo(IRenderLayer other)
            => Order.CompareTo(other.Order);

        /// <summary>
        /// Order of this layer in scene hierarchy. Layers are processed in ascending order.
        /// </summary>
        float Order { get; init; }

        /// <summary>
        /// Name of this layer in scene hierarchy.
        /// </summary>
        string Name { get; init; }

        /// <summary>
        /// When <see langword="true"/>, <see cref="Prepare"/> method will be called.
        /// </summary>
        bool IsActive { get; set; }

        /// <summary>
        /// When <see langword="true"/>, <see cref="Present(DrawingContext)"/> method will be called.
        /// </summary>
        bool ShouldPresent { get; set; }

        /// <summary>
        /// Parent scene of this render layer.
        /// </summary>
        Scene Scene { get; init; }

        /// <summary>
        /// Happens before any layer is presented to screen. Good place to do some RenderTarget magic.
        /// </summary>
        void Prepare();

        /// <summary>
        /// Happens when the layer is being drawn on-screen after all layers <see cref="Prepare"/>.
        /// </summary>
        void Present(DrawingContext drawing);
    }

    /// <summary>
    /// Base interface for nested render layers. This is required for exposing internal layers to several systems.
    /// </summary>
    public interface INestedRenderLayer : IRenderLayer {
        /// <summary>
        /// Internal layer this nested render layer holds.
        /// </summary>
        IRenderLayer Content { get; set; }
    }
}
