using Instant2D.Utils;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Instant2D.Collision {
    internal class LinecastProcessor<T> where T: ICollider<T> {
        readonly HashSet<T> _colliderHash = new();

        // linecast properties
        Vector2 _origin, _end, _direction;
        int _layerMask, _hitLimit, _hits;
        List<LineCastResult<T>> _output;
        bool _ignoreStartingCollider;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Begin(Vector2 origin, Vector2 end, Vector2 direction, int layerMask, int hitLimit, bool ignoreStartingCollider) {
            _origin = origin;
            _direction = direction;
            _end = end;
            _hitLimit = hitLimit;
            _layerMask = layerMask;
            _hits = 0;
            _ignoreStartingCollider = ignoreStartingCollider;

            // obtain a pooled instance
            _output = ListPool<LineCastResult<T>>.Get();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool End(out List<LineCastResult<T>> results) {
            results = _output;

            _colliderHash.Clear();
            _output = null;

            return _hits > 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ProcessChunk(List<T> chunk) {
            for (var i = 0; i < chunk.Count; i++) {
                var collider = chunk[i];

                // the object has already been dealt with
                if (!_colliderHash.Add(collider))
                    continue;

                // check if layer flag is set
                if (!IntFlags.IsFlagSet(_layerMask, collider.LayerMask, false))
                    continue;

                var bounds = collider.Shape.Bounds;

                if (RayIntersects(bounds, out var distance) && distance <= 1.0f) {
                    if (collider.CollidesWithLine(_origin, _end, out var hit)) {
                        // l bozo
                        if (_ignoreStartingCollider && collider.Shape.ContainsPoint(_origin)) 
                            continue;
                        
                        _output.Add(hit);

                        // escape when limit is reached
                        if (++_hits >= _hitLimit) {
                            break;
                        }
                    }
                }
            }

            // target hit count reached
            return _hits >= _hitLimit;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool RayIntersects(in RectangleF bounds, out float distance) {
            distance = 0f;

            var maxValue = float.MaxValue;
            if (Math.Abs(_direction.X) < 1E-06f) {
                if ((_origin.X < bounds.X) || (_origin.X > bounds.Right))
                    return false;
            } else {
                var num11 = 1f / _direction.X;
                var num8 = (bounds.X - _origin.X) * num11;
                var num7 = (bounds.Right - _origin.X) * num11;
                if (num8 > num7) {
                    (num7, num8) = (num8, num7);
                }

                distance = MathHelper.Max(num8, distance);
                maxValue = MathHelper.Min(num7, maxValue);
                if (distance > maxValue)
                    return false;
            }

            if (Math.Abs(_direction.Y) < 1E-06f) {
                if ((_origin.Y < bounds.Y) || (_origin.Y > bounds.Bottom)) {
                    return false;
                }
            } else {
                var num10 = 1f / _direction.Y;
                var num6 = (bounds.Y - _origin.Y) * num10;
                var num5 = (bounds.Bottom - _origin.Y) * num10;
                if (num6 > num5) {
                    (num5, num6) = (num6, num5);
                }

                distance = MathHelper.Max(num6, distance);
                maxValue = MathHelper.Min(num5, maxValue);
                if (distance > maxValue)
                    return false;
            }

            return true;
        }
    }
}
