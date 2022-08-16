using Instant2D.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D {
    public interface IDrawingBackend {
        /// <summary>
        /// Batch up a sprite for later rendering.
        /// </summary>
        void Draw(in Sprite sprite, Vector2 position, Color color, float rotation, Vector2 scale, SpriteEffects spriteEffects = SpriteEffects.None);

        /// <summary>
        /// Batch up a texture for later rendering.
        /// </summary>
        void DrawTexture(Texture2D texture, Vector2 position, Color color, float rotation, Vector2 scale, Vector2 origin, SpriteEffects spriteEffects = SpriteEffects.None, Rectangle? sourceRect = default);

        /// <summary>
        /// Begins new batch with specified <see cref="Material"/>.
        /// </summary>
        void Push(in Material material, Matrix transformMatrix = default);

        /// <summary>
        /// Ends current batch and flushes all the drawn stuff. <br/>
        /// If <paramref name="endCompletely"/> is set, the batch will not be restarted.
        /// </summary>
        void Pop(bool endCompletely = false);
    }
}
