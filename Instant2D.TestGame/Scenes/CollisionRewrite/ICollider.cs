using Microsoft.Xna.Framework;

namespace Instant2D.TestGame.Scenes {
    public interface ICollider {
        /// <summary>
        /// Collision shape used by this collider.
        /// </summary>
        ICollisionShape Shape { get; }

        /// <summary>
        /// Rectange to which this collider is registered in the spatial hash.
        /// </summary>
        Rectangle SpatialHashRegion { get; internal set; }

        /// <summary>
        /// Layer mask of this collider.
        /// </summary>
        int LayerMask { get; }

        /// <summary>
        /// Mask of other colliders which this one should not ignore.
        /// </summary>
        int CollidesWithMask { get; }
    }
}
