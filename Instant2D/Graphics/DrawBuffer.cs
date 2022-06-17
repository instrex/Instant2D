using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D.Graphics {
    /// <summary>
    /// Simple DrawBuffer implementation using <see cref="SpriteBatch"/>, will probably be replaced with something more performant in the future.
    /// </summary>
    public class DrawBuffer {
        /// <summary>
        /// Vertex count for vertex buffer initialization. Defaults to 6144.
        /// </summary>
        public static int DefaultVertexCount { get; set; } = 8192;

        readonly DynamicVertexBuffer _vertices;
        readonly IndexBuffer _indices;

        public DrawBuffer(GraphicsDevice graphicsDevice) {
            _vertices = new DynamicVertexBuffer(graphicsDevice, typeof(VertexPositionColorTexture), DefaultVertexCount, BufferUsage.WriteOnly);
            
        }
    }
}
