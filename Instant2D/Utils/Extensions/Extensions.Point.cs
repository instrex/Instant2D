using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D.Utils {
    public static partial class Extensions {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 ToVector2(this Point point) => new(point.X, point.Y);

        /// <summary>
        /// Rounds both X and Y components using <see cref="MathF.Round(float, int)"/> function.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Round(this Vector2 vector, int digits = 0) => new(MathF.Round(vector.X, digits), MathF.Round(vector.Y, digits));

        /// <summary>
        /// Converts this Vector2 into rotation (in radians).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ToRotation(this Vector2 vector) => MathF.Atan2(vector.Y, vector.X);

        /// <summary>
        /// Converts this rotation (in radians) into normalized Vector2.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 ToVector(this float angle) => new(MathF.Cos(angle), MathF.Sin(angle));

        /// <summary>
        /// Rotate the provided Vector around <paramref name="center"/> by <paramref name="rotation"/> radians.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 RotatedBy(this Vector2 vector, float rotation, Vector2 center = default) {
            var (x, y) = (MathF.Cos(rotation), MathF.Sin(rotation));
            var dir = vector - center;
            return new(center.X + dir.X * x - dir.Y * y, center.Y + dir.X * y - dir.Y * x);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Point RoundToPoint(this Vector2 vec) => new((int)MathF.Round(vec.X), (int)MathF.Round(vec.Y));
    }
}
