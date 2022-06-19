using Instant2D.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D {
    public abstract class DrawingBackend {
        /// <summary>
        /// Transform matrix used when drawing.
        /// </summary>
        public Matrix TransformationMatrix { get; set; }

        /// <summary>
        /// Batch up a sprite for later rendering.
        /// </summary>
        public abstract void Draw(in Sprite sprite, Vector2 position, Color color, float rotation, Vector2 scale, SpriteEffects spriteEffects = SpriteEffects.None);

        /// <summary>
        /// Begins new batch with specified <see cref="Material"/>.
        /// </summary>
        public abstract void Push(in Material material);

        /// <summary>
        /// Ends current batch and flushes all the drawn stuff. <br/>
        /// If <paramref name="endCompletely"/> is set, the batch will not be restarted.
        /// </summary>
        public abstract void Pop(bool endCompletely = false);

        /// <inheritdoc cref="Draw(in Sprite, Vector2, Color, float, Vector2)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Draw(in Sprite sprite, Vector2 position, Color color, float rotation, float scale, SpriteEffects spriteEffects = SpriteEffects.None) => 
            Draw(sprite, position, color, rotation, new Vector2(scale), spriteEffects);
    }
}
