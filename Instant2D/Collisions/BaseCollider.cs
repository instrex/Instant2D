using Instant2D.Utils.Math;
using Microsoft.Xna.Framework;
using System;
using System.Runtime.CompilerServices;

namespace Instant2D.Collisions {
    /// <summary>
    /// Base collider class that handles bounds, updating and collision operations between colliders. <br/>
    /// May also have implementation specific Entity attached for later identification and ease of use.
    /// </summary>
    public abstract class BaseCollider<T> {
        /// <summary>
        /// Custom data variable. Use it to store implementation-specific entity data, for example: 
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
        /// Physics layer this collider belongs to. Other objects with its flag defined in <see cref="CollidesWith"/> will be able to collide with each other.
        /// </summary>
        public int CollisionLayer = 0;

        /// <summary>
        /// Bitmask of layers which should collide with this collider. See <see cref="CollisionLayer"/> for more info.
        /// </summary>
        public int CollidesWith = -1;

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

    /// <summary>
    /// Box collider with a centered origin. When rotated, will behave like a polygon.
    /// </summary>
    public class BoxCollider<T> : BaseCollider<T> {
        /// <summary>
        /// Size of this box.
        /// </summary>
        public Vector2 Size;

        public float Rotation {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        protected override void RecalculateBounds(ref RectangleF bounds) {
            bounds = new RectangleF(Position - Size, Size);
        }

        public override bool CheckOverlap(BaseCollider<T> other) {
            if (other is BoxCollider<T> boxCollider) {
                return Bounds.Intersects(boxCollider.Bounds);
            }

            throw new System.NotImplementedException();
        }

        public override bool CheckCollision(BaseCollider<T> other, out CollisionHit<T> hit) {
            hit = new CollisionHit<T> { Self = this, Other = other };

            if (other is BoxCollider<T>) {

                // box to box collision (unrotated)
                if (CollisionMethods.RectToRect(Bounds, other.Bounds, out var penetration)) {
                    hit.PenetrationVector = penetration;
                    hit.Normal = hit.PenetrationVector.SafeNormalize() * -1;
                    return true;
                }

                return false;
            }

            throw new System.NotImplementedException();
        }

        public override bool CheckLineCast(Vector2 start, Vector2 end, out LineCastHit<T> hit) {


            throw new NotImplementedException();
        }
    }

}
