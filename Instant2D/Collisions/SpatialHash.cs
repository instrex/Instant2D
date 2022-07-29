using Instant2D.Utils;
using Instant2D.Utils.Math;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D.Collisions {
    /// <summary>
    /// Collection of colliders in world-space batched by evenly sized chunks. <br/>
    /// This removes the need to iterate over each defined collider.
    /// </summary>
    public class SpatialHash<T> {
        public const int DEFAULT_CHUNK_SIZE = 100;

        /// <summary>
        /// Current world size. Expands when new colliders are added.
        /// </summary>
        public Rectangle Bounds;

        // chunk information
        // TODO: optimize _chunks indexing, as it somehow uses too much processing power to GetHashCode and Equals on
        internal readonly Dictionary<Point, List<BaseCollider<T>>> _chunks = new();
        readonly float _invChunkSize;
        readonly int _chunkSize;

        // cached stuff
        readonly HashSet<BaseCollider<T>> _colliderHash = new();
        readonly List<BaseCollider<T>> _colliderBuffer = new();

        // dummy colliders
        readonly BoxCollider<T> _overlapTestBox = new();

        /// <summary>
        /// Creates a new <see cref="SpatialHash"/> instance with the world batched to chunks with the size of <paramref name="chunkSize"/>.
        /// </summary>
        public SpatialHash(int chunkSize = DEFAULT_CHUNK_SIZE) {
            _chunkSize = chunkSize;
            _invChunkSize = 1f / _chunkSize;
        }

        /// <summary>
        /// Readonly list of all the chunks. Used for debugging purposes.
        /// </summary>
        public IReadOnlyDictionary<Point, List<BaseCollider<T>>> Chunks => _chunks;

        /// <summary>
        /// Readonly size of a single chunk.
        /// </summary>
        public int ChunkSize => _chunkSize;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        Point GetChunkCoords(float x, float y) => new((int)MathF.Floor(x * _invChunkSize), (int)MathF.Floor(y * _invChunkSize));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        List<BaseCollider<T>> GetChunk(Point point, bool createIfMissing = false) {
            if (!_chunks.TryGetValue(point, out var values) && createIfMissing)
                _chunks.Add(point, values = new());

            return values;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void GrowBounds(Point point) {
            Rectangle result;
            result.X = Math.Min(Bounds.X, point.X);
            result.Y = Math.Min(Bounds.Y, point.Y);
            result.Width = Math.Max(Bounds.Right, point.X) - result.X;
            result.Height = Math.Max(Bounds.Bottom, point.Y) - result.Y;

            // assign bounds
            Bounds = result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddCollider(BaseCollider<T> collider) {
            var bounds = collider.Bounds;
            var (topLeft, bottomRight) = (GetChunkCoords(bounds.X, bounds.Y), GetChunkCoords(bounds.Right, bounds.Bottom));

            // expand bounds to the top left
            if (!Bounds.Contains(topLeft))
                GrowBounds(topLeft);

            // expand bounds to the bottom right
            if (!Bounds.Contains(bottomRight))
                GrowBounds(bottomRight);

            // add this collider to each chunk it touches
            for (var x = topLeft.X; x <= bottomRight.X; x++) {
                for (var y = topLeft.Y; y <= bottomRight.Y; y++) {
                    var chunk = GetChunk(new(x, y), true);
                    chunk?.Add(collider);
                }
            }

            // store the reference for later retrieval
            // also the registration rect so it's easier to remove the object
            // when needed (when it moves for example)
            collider._registrationRect = new(topLeft.X, topLeft.Y, bottomRight.X - topLeft.X, bottomRight.Y - topLeft.Y);
            collider._spatialHash = this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveCollider(BaseCollider<T> collider, bool forceRemove = false) {
            if (forceRemove) {
                // iterate every cell and remove the collider
                // may be inefficient, but I'm gonna bother with that later
                foreach (var (_, colliders) in _chunks) {
                    colliders.Remove(collider);
                }

                return;
            }
            
            // iterate the registration rect and remove the references
            for (var x = collider._registrationRect.X; x <= collider._registrationRect.Right; x++) {
                for (var y = collider._registrationRect.Y; y <= collider._registrationRect.Bottom; y++) {
                    var chunk = GetChunk(new(x, y));
                    chunk?.Remove(collider);
                }
            }
        }

        #region Collision Detection functions

        /// <summary>
        /// Sweeps all colliders that fall close to <paramref name="bounds"/>. Note that this doesn't mean they collide, they just intersect with the bounds. <br/>
        /// For precise collisions, call specialized <see cref="ICollider"/> methods after or <see cref="OverlapAll(RectangleF, IntFlags)"/>.
        /// </summary>
        /// <remarks> Returned list is pooled, so avoid storing references to it or just copy it. </remarks>
        public List<BaseCollider<T>> Broadphase(RectangleF bounds, int layerMask = -1) {
            _colliderBuffer.Clear();
            _colliderHash.Clear();

            // iterate over each cell
            var (topLeft, bottomRight) = (GetChunkCoords(bounds.X, bounds.Y), GetChunkCoords(bounds.Right, bounds.Bottom));
            for (var x = topLeft.X; x <= bottomRight.X; x++) {
                for (var y = topLeft.Y; y <= bottomRight.Y; y++) {
                    // if it's not initialized, skip
                    if (GetChunk(new(x, y)) is not List<BaseCollider<T>> chunk)
                        continue;

                    for (var i = 0; i < chunk.Count; i++) {
                        var collider = chunk[i];

                        // check if collider's layer is set in the layerMask
                        if (!layerMask.IsFlagSet(collider.CollisionLayer, false))
                            continue;

                        // if bounds intersect, try adding into the hash
                        // if successful, add it into the buffer
                        if (bounds.Intersects(collider.Bounds) && _colliderHash.Add(collider))
                            _colliderBuffer.Add(collider);
                    }
                }
            }

            return _colliderBuffer;
        }

        /// <summary>
        /// Performs a narrow overlap check and returns the first collider that meets <paramref name="layerMask"/> criteria. To get all of the results, call <see cref="OverlapAll(RectangleF, int)"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BaseCollider<T> Overlap(RectangleF bounds, int layerMask = -1) {
            var overlap = OverlapAll(bounds, layerMask);
            return overlap.Count < 1 ? null : overlap[0];
        }

        /// <summary>
        /// Performs a narrow overlap check against <paramref name="bounds"/> calling <see cref="BaseCollider{T}.CheckOverlap(BaseCollider{T})"/>.
        /// </summary>
        /// <remarks> Returned list is pooled, so avoid storing references to it or just copy it. </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public List<BaseCollider<T>> OverlapAll(RectangleF bounds, int layerMask = -1) {
            // update the overlap test box
            _overlapTestBox.Position = bounds.Position;
            _overlapTestBox.Size = bounds.Size;
            _overlapTestBox.Update();

            // do a broadphase and narrow down overlapping colliders
            var broadphase = Broadphase(bounds, layerMask);
            for (var i = broadphase.Count - 1; i >= 0; i--) {
                // if a collider doesn't overlap, discard it
                if (!broadphase[i].CheckOverlap(_overlapTestBox)) {
                    broadphase.RemoveAt(i);
                }
            }

            return _colliderBuffer;
        }



        #endregion
    }
}
