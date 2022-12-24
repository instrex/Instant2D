using Instant2D.Utils;
using Instant2D.Utils.Math;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D.Collision {
    using Shapes;

    /// <summary>
    /// Collection of colliders in world-space batched by evenly sized chunks. <br/>
    /// This removes the need to iterate over each defined collider.
    /// </summary>
    public class SpatialHash<T> where T: ICollider<T> {
        public const int DEFAULT_CHUNK_SIZE = 32;

        /// <summary>
        /// Current world size. Expands when new colliders are added.
        /// </summary>
        public Rectangle Bounds { get; private set; }

        /// <summary>
        /// Size of each collider chunk.
        /// </summary>
        public int ChunkSize => _chunkSize;

        // chunk information
        // TODO: optimize _chunks indexing, as it somehow uses too much processing power to GetHashCode and Equals on
        internal readonly Dictionary<long, List<T>> _chunks = new();
        readonly float _invChunkSize;
        readonly int _chunkSize;

        // helper struct for processing linecasts
        readonly LinecastProcessor<T> _linecaster = new();

        // cached stuff
        readonly HashSet<T> _colliderHash = new();

        // dummy shapes
        readonly Box _overlapTestBox = new();

        /// <summary>
        /// Creates a new <see cref="SpatialHash"/> instance with the world batched to chunks of size <paramref name="chunkSize"/>. <br/>
        /// Different sizes may improve or worsen the collision detection performance, generally you should set <paramref name="chunkSize"/> to be of a size greater than your average collider.
        /// </summary>
        public SpatialHash(int chunkSize = DEFAULT_CHUNK_SIZE) {
            _chunkSize = chunkSize;
            _invChunkSize = 1f / _chunkSize;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        Point GetChunkCoords(float x, float y) => new((int)MathF.Floor(x * _invChunkSize), (int)MathF.Floor(y * _invChunkSize));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        List<T> GetChunk(int x, int y, bool createIfMissing = false) {
            var hash = unchecked((long)x << 32 | (uint)y);
            if (!_chunks.TryGetValue(hash, out var values) && createIfMissing)
                _chunks.Add(hash, values = new());

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
        public void AddCollider(T collider) {
            var bounds = collider.Shape.Bounds;
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
                    var chunk = GetChunk(x, y, true);
                    chunk?.Add(collider);
                }
            }

            // store registration region so it's easier to remove the object
            // when needed (when it moves for example)
            collider.SpatialHashRegion = new(topLeft.X, topLeft.Y, bottomRight.X - topLeft.X, bottomRight.Y - topLeft.Y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveCollider(T collider, bool forceRemove = false) {
            if (forceRemove) {
                // iterate every cell and remove the collider
                // may be inefficient, but I'm gonna bother with that later
                foreach (var (_, colliders) in _chunks) {
                    colliders.Remove(collider);
                }

                return;
            }

            var registrationRect = collider.SpatialHashRegion;

            // iterate the registration rect and remove the references
            for (var x = registrationRect.X; x <= registrationRect.Right; x++) {
                for (var y = registrationRect.Y; y <= registrationRect.Bottom; y++) {
                    var chunk = GetChunk(x, y);
                    chunk?.Remove(collider);
                }
            }
        }

        #region Collision Detection functions

        /// <summary>
        /// Gets all colliders close to <paramref name="bounds"/> and meeting <paramref name="layerMask"/> criteria.
        /// </summary>
        /// <remarks> Returned list is pooled, so don't forget to call <see cref="ListPool{T}.Return(List{T})"/> on it. </remarks>
        public bool Broadphase(RectangleF bounds, out List<T> results, int layerMask = -1) {
            _colliderHash.Clear();
            results = null;

            // iterate over each cell
            var (topLeft, bottomRight) = (GetChunkCoords(bounds.X, bounds.Y), GetChunkCoords(bounds.Right, bounds.Bottom));
            for (var x = topLeft.X; x <= bottomRight.X; x++) {
                for (var y = topLeft.Y; y <= bottomRight.Y; y++) {
                    // if it's not initialized, skip
                    if (GetChunk(x, y) is not List<T> chunk)
                        continue;

                    for (var i = 0; i < chunk.Count; i++) {
                        var collider = chunk[i];

                        // check if collider's layer is set in the layerMask
                        if (!layerMask.IsFlagSet(collider.LayerMask, false))
                            continue;

                        // if bounds intersect, try adding into the hash
                        // if successful, add it into the buffer
                        if (bounds.Intersects(collider.Shape.Bounds) && _colliderHash.Add(collider)) {
                            results ??= ListPool<T>.Get();
                            results.Add(collider);
                        }
                    }
                }
            }

            return results != null;
        }

        /// <summary>
        /// Performs a precise overlap check against <paramref name="bounds"/> using <see cref="Box.CheckOverlap(ICollisionShape)"/>.
        /// </summary>
        /// <remarks> Returned list is pooled, so don't forget to call <see cref="ListPool{T}.Return(List{T})"/> on it. </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool OverlapRect(RectangleF bounds, out List<T> results, int layerMask = -1) {
            // update the overlap test box
            _overlapTestBox.Position = bounds.Position;
            _overlapTestBox.Size = bounds.Size;

            // do a broadphase and narrow down overlapping colliders
            if (Broadphase(bounds, out var colliders, layerMask)) {
                for (var i = colliders.Count - 1; i >= 0; i--) {
                    // if a collider doesn't overlap, discard it
                    if (!colliders[i].Shape.CheckOverlap(_overlapTestBox)) {
                        colliders.RemoveAt(i);
                    }
                }

                // just reuse the pooled list from broadphase
                results = colliders;
                return results.Count > 0;
            }

            results = null;
            return false;
        }

        /// <summary>
        /// Perform a linecast, checking objects in a line from point <paramref name="origin"/> to <paramref name="end"/>.
        /// </summary>
        /// <param name="origin"> Origin point of the line. </param>
        /// <param name="end"> Ending point of the line. </param>
        /// <param name="hits"> List with hit results. Pool after use! </param>
        /// <param name="layerMask"> LayerMask of interest. </param>
        /// <param name="hitLimit"> How many collider hits should be processed. </param>
        /// <param name="ignoreOriginColliders"> When <see langword="true"/>, colliders with shapes containing <paramref name="origin"/> will be ignored. </param>
        /// <returns></returns>
        /// <remarks> Returned list is pooled, so don't forget to call <see cref="ListPool{T}.Return(List{T})"/> on it. </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Linecast(Vector2 origin, Vector2 end, out List<LineCastResult<T>> hits, int layerMask = -1, int hitLimit = int.MaxValue, bool ignoreOriginColliders = false) {
            var direction = end - origin;

            // get the current and target chunk coords
            var (currentPos, endPos) = (GetChunkCoords(origin.X, origin.Y), GetChunkCoords(end.X, end.Y));

            // determine direction of the ray
            var (stepX, stepY) = (Math.Sign(direction.X), Math.Sign(direction.Y));
            if (currentPos.X == endPos.X) stepX = 0;
            if (currentPos.Y == endPos.Y) stepY = 0;

            // calculate step boundaries
            var (xStep, yStep) = (stepX < 0 ? 0f : stepX, stepY < 0 ? 0f : stepY);
            var (nextBoundaryX, nextBoundaryY) = ((currentPos.X + xStep) * _chunkSize, (currentPos.Y + yStep) * _chunkSize);

            var (tMaxX, tMaxY) = (direction.X != 0 ? (nextBoundaryX - origin.X) / direction.X : float.MaxValue, direction.Y != 0 ? (nextBoundaryY - origin.Y) / direction.Y : float.MaxValue);
            var (tDeltaX, tDeltaY) = (direction.X != 0 ? _chunkSize / (direction.X * stepX) : float.MaxValue, direction.Y != 0 ? _chunkSize / (direction.Y * stepY) : float.MaxValue);

            var chunk = GetChunk(currentPos.X, currentPos.Y);
            _linecaster.Begin(origin, end, direction, layerMask, hitLimit, ignoreOriginColliders);

            //if hit limit is reached immediately, just return
            if (chunk != null && _linecaster.ProcessChunk(chunk)) 
                return _linecaster.End(out hits);
            
            // check chunks along the line
            while (currentPos.X != endPos.X || currentPos.Y != endPos.Y) {
                if (tMaxX < tMaxY) {
                    currentPos.X = (int)Approach(currentPos.X, endPos.X, Math.Abs(stepX));
                    tMaxX += tDeltaX;
                } else {
                    currentPos.Y = (int)Approach(currentPos.Y, endPos.Y, Math.Abs(stepY));
                    tMaxY += tDeltaY;
                }

                chunk = GetChunk(currentPos.X, currentPos.Y);
                if (chunk != null && _linecaster.ProcessChunk(chunk))
                    return _linecaster.End(out hits);
            }

            return _linecaster.End(out hits);
        }

        #endregion

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float Approach(float start, float end, float shift) {
            if (start < end)
                return Math.Min(start + shift, end);

            return Math.Max(start - shift, end);
        }
    }
}
