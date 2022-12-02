using Instant2D.Utils;
using Instant2D.Utils.Math;
using Microsoft.Xna.Framework;
using System.Runtime.CompilerServices;

namespace Instant2D.Collisions {
    /// <summary>
    /// Base collider class that handles bounds, updating and collision operations between colliders. <br/>
    /// May also have implementation specific Entity attached for later identification and ease of use.
    /// </summary>
    public abstract class BaseCollider<T> {
        /// <summary>
        /// Custom data variable. Use it to store implementation-specific entity data.
        /// </summary>
        public T Entity;

        /// <summary>
        /// Approximate bounds this collider occupies. Used for broadphase.
        /// </summary>
        public RectangleF Bounds {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get {
                if (_areBoundsDirty) {
                    RecalculateBounds(ref _bounds);
                    _areBoundsDirty = false;
                }

                return _bounds;
            }
        }

        /// <summary>
        /// The position component used by this collider.
        /// </summary>
        public Vector2 Position;

        /// <summary>
        /// Physics layer mask this collider belongs to. Other objects with its flags defined in <see cref="CollidesWithMask"/> will be able to collide with each other. <br/>
        /// Operate on it using <see cref="IntFlags"/> extensions. Defaults to <c>IntFlags.SetFlagExclusive(0)</c>.
        /// </summary>
        public int LayerMask = IntFlags.SetFlagExclusive(0);

        /// <summary>
        /// Layer mask for this collider. See <see cref="LayerMask"/> for more info. Defaults to all layers.
        /// </summary>
        public int CollidesWithMask = -1;

        internal SpatialHash<T> _spatialHash;
        internal Rectangle _registrationRect;
        internal bool _areBoundsDirty = true;
        internal RectangleF _bounds;

        /// <summary>
        /// Marks this collider for bounds update.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Update() {
            _areBoundsDirty = true;

            // update the collider in spatial hash if present
            if (_spatialHash != null) {
                _spatialHash.RemoveCollider(this);
                _spatialHash.AddCollider(this);
            }
        }

        /// <summary>
        /// Marks the collider for bounds update.
        /// </summary>
        public void MarkDirty() {
            _areBoundsDirty = true;
        }

        /// <summary>
        /// Update the value of <see cref="Bounds"/>. Called only when needed to increase performance.
        /// </summary>
        protected abstract void RecalculateBounds(ref RectangleF bounds);

        #region Collision Detection

        /// <summary>
        /// Checks if this collider overlaps another.
        /// </summary>
        public abstract bool CheckOverlap(BaseCollider<T> other);

        /// <summary>
        /// Check if two colliders collide with each other, calculating the penetration vector.
        /// </summary>
        public abstract bool CheckCollision(BaseCollider<T> other, out CollisionHit<T> hit);

        /// <summary>
        /// Check if this collider intersects a line created by linecast. In case if <see langword="true"/>, returns some information into as <see cref="LineCastHit{T}"/>.
        /// </summary>
        public abstract bool CheckLineCast(Vector2 start, Vector2 end, out LineCastHit<T> hit);

        #endregion
    }

}
