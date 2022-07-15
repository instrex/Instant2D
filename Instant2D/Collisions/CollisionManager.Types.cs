using Instant2D.Utils.Math;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D.Collisions {
    public partial class CollisionManager<T> where T: CollisionManager<T>.ICollider {

        /// <summary>
        /// Base collider class. Implement all members for it to be fully usable inside <see cref="CollisionManager{T}"/>.
        /// </summary>
        public interface ICollider {
            /// <summary>
            /// Bounds that this collider occupies. Used for spatial hashing.
            /// </summary>
            RectangleF Bounds { get; }

            /// <summary>
            /// When <see langword="true"/>, other colliders will not consider this one as obstacle and move through it. <br/>
            /// Will also invoke special events.
            /// </summary>
            bool IsTrigger { get; }

            IntFlags CollisionLayer { get; }

            /// <summary>
            /// Checks if two colliders overlap.
            /// </summary>
            bool CheckOverlap(ICollider other);

            bool CheckLineCollision(Vector2 start, Vector2 end, out LineHit result);

            bool CheckCollision(ICollider other, out CollisionHit result);
        }

        /// <summary>
        /// The hit event produced when <see cref="ICollider.CheckCollision(ICollider, out CollisionHit)"/> is called.
        /// </summary>
        public record struct CollisionHit {
            /// <summary>
            /// The collider with which collision has occured.
            /// </summary>
            public T Collider;

            /// <summary>
            /// Normal vector of the collision surface.
            /// </summary>
            public Vector2 Normal;

            /// <summary>
            /// Minimal movement required to push colliders apart.
            /// </summary>
            public Vector2 MinimumTranslationVector;

            /// <summary>
            /// The point of collision, may be <see langword="null"/> in some cases.
            /// </summary>
            public Vector2? Point;
        }

        /// <summary>
        /// Linecast hit event produced when <see cref="ICollider.CheckLineCollision(Vector2, Vector2, out LineHit)"/> is called.
        /// </summary>
        public record struct LineHit {
            /// <summary>
            /// The collider hit by the ray.
            /// </summary>
            public ICollider Collider;

            public float Fraction;

            public float Distance;

            public Vector2 Point;

            public Vector2 Normal;

            public Vector2 Centroid;
        }

        /// <summary>
        /// Collection of colliders in world-space batched by evenly sized chunks. <br/>
        /// This removes the need to iterate over each defined collider.
        /// </summary>
        public class SpatialHash {
            /// <summary>
            /// Current world size. Expands when new colliders are added.
            /// </summary>
            public Rectangle Bounds;

            // chunk information
            internal readonly Dictionary<Point, List<T>> _chunks = new();
            readonly float _invChunkSize;
            readonly int _chunkSize;

            // cached stuff
            readonly HashSet<T> _colliderHash = new();
            readonly List<T> _colliderBuffer = new();


            /// <summary>
            /// Creates a new <see cref="SpatialHash"/> instance with the world batched to chunks with the size of <paramref name="chunkSize"/>.
            /// </summary>
            public SpatialHash(int chunkSize = 100) {
                _chunkSize = chunkSize;
                _invChunkSize = 1f / _chunkSize;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            Point GetChunkCoords(float x, float y) => new((int)MathF.Floor(x * _invChunkSize), (int)MathF.Floor(y * _invChunkSize));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            List<T> GetChunk(Point point, bool createIfMissing = false) {
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

            public void AddCollider(T collider) {
                var bounds = collider.Bounds;
                var (topLeft, bottomRight) = (GetChunkCoords(bounds.X, bounds.Y), GetChunkCoords(bounds.X, bounds.Y));

                // expand bounds to the top left
                if (!Bounds.Contains(topLeft))
                    GrowBounds(topLeft);

                // expand bounds to the bottom right
                if (!Bounds.Contains(bottomRight))
                    GrowBounds(bottomRight);

                // add this collider to each chunk it touches
                for (var x = topLeft.X; x < bottomRight.X; x++) {
                    for (var y = topLeft.Y; y < bottomRight.Y; y++) {
                        var chunk = GetChunk(new(x, y), true);
                        chunk?.Add(collider);
                    }
                }
            }

            public void RemoveCollider(T collider) {
                // iterate every cell and remove the collider
                // may be inefficient, but I'm gonna bother with that later
                foreach (var (_, colliders) in _chunks) {
                    colliders.Remove(collider);
                }
            }

            #region Collision Detection functions

            /// <summary>
            /// Sweeps all colliders that fall close to <paramref name="bounds"/>. Note that this doesn't mean they collide, they just intersect with the bounds. <br/>
            /// For precise collisions, call specialized <see cref="ICollider"/> methods after.
            /// </summary>
            /// <remarks> Returned list is pooled, so avoid storing references to it or just copy it. </remarks>
            public List<T> Broadphase(RectangleF bounds, IntFlags layerMask) {
                _colliderBuffer.Clear();
                _colliderHash.Clear();

                // iterate over each cell
                var (topLeft, bottomRight) = (GetChunkCoords(bounds.X, bounds.Y), GetChunkCoords(bounds.X, bounds.Y));
                for (var x = topLeft.X; x < bottomRight.X; x++) {
                    for (var y = topLeft.Y; y < bottomRight.Y; y++) {
                        // if it's not initialized, skip
                        if (GetChunk(new(x, y)) is not List<T> chunk)
                            continue;

                        for (var i = 0; i < chunk.Count; i++) {
                            var collider = chunk[i];

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

            #endregion
        }
    }

    


}
