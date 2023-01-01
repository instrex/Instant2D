using Microsoft.Xna.Framework;
using Instant2D;

namespace Instant2D.Utils.ResolutionScaling {
    public struct ScaledResolution {
        /// <summary>
        /// Final resolution, with all the scaler options applied.
        /// </summary>
        public Point renderTargetSize;

        /// <summary>
        /// Size of the screen, before scaling.
        /// </summary>
        public Point rawScreenSize;

        /// <summary>
        /// Scale of the screen, used for information purposes only.
        /// </summary>
        public float scaleFactor;

        /// <summary>
        /// The offset from top-left point of the screen if any.
        /// </summary>
        public Vector2 offset;

        public int Width => (int)(renderTargetSize.X * scaleFactor);

        public int Height => (int)(renderTargetSize.Y * scaleFactor);

        

        public override string ToString() => $"{Width}x{Height} (x{scaleFactor})";

        /// <summary>
        /// Gets the default resolution using GraphicsDevice's Viewport and scale factor of 1.0f.
        /// </summary>
        public static ScaledResolution Default => new() {
            renderTargetSize = new(InstantApp.Instance.GraphicsDevice.Viewport.Width, InstantApp.Instance.GraphicsDevice.Viewport.Height),
            scaleFactor = 1.0f,
        };
    }
}
