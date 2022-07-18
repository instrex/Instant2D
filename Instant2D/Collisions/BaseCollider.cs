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
        /// Physics layer this collider belongs to. Other objects with its flag defined in <see cref="CollidesWith"/> will be able to collide with each other.
        /// </summary>
        public IntFlags CollisionLayer = -1;

        /// <summary>
        /// Bitmask of layers which should collide with this collider. See <see cref="CollisionLayer"/> for more info.
        /// </summary>
        public IntFlags CollidesWith = -1;

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
            _spatialHash?.RemoveCollider(this);
            _spatialHash?.AddCollider(this);
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

        #endregion
    }

    // TEMPORARY box collider for testing purposes
    public class BoxCollider<T> : BaseCollider<T> {
        public Vector2 _position, _size;

        /// <summary>
        /// Position of this collider in world-space.
        /// </summary>
        public Vector2 Position {
            get => _position;
            set {
                _position = value;
                Update();
            }
        }

        /// <summary>
        /// Size of this box.
        /// </summary>
        public Vector2 Size {
            get => _size;
            set {
                _size = value;
                Update();
            }
        }

        public float Rotation {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        protected override void RecalculateBounds(ref RectangleF bounds) {
            bounds = new RectangleF(_position - _size, _size);
        }

        public override bool CheckOverlap(BaseCollider<T> other) {
            if (other is BoxCollider<T> boxCollider) {
                return Bounds.Intersects(boxCollider.Bounds);
            }

            throw new System.NotImplementedException();
        }

        public override bool CheckCollision(BaseCollider<T> other, out CollisionHit<T> hit) {
            hit = new CollisionHit<T> { Self = this, Other = other };

            // box to box collision (unrotated)
            if (other is BoxCollider<T> boxCollider) {
                var (a, b) = (Bounds, other.Bounds);

                // get intersection between two boxes
                var intersection = a.GetIntersection(b);

                // no collision...
                if (intersection == default)
                    return false;

                hit.PenetrationVector = intersection.Width < intersection.Height ?
                    new(a.Center.X < b.Center.X ? intersection.Width : -intersection.Width, 0) :
                    new(0, a.Center.Y < b.Center.Y ? intersection.Height : -intersection.Height);

                hit.Normal = hit.PenetrationVector.SafeNormalize() * -1;

                return true;
            }

            throw new System.NotImplementedException();
        }
    }

}
