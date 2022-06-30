using Microsoft.Xna.Framework;
using System;
using System.Runtime.CompilerServices;

namespace Instant2D.Utils {
    /// <summary>
    /// Collection of common and useful methods to help working with Vector2.
    /// </summary>
    public static class VectorUtils {
        /// <summary>
        /// Rounds both X and Y components using <see cref="MathF.Round(float, int)"/> function.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Round(this Vector2 vector, int digits = 0) => new(MathF.Round(vector.X, digits), MathF.Round(vector.Y, digits));

        /// <summary>
        /// Converts this Vector2 into rotation (in radians).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ToAngle(this Vector2 vector) => MathF.Atan2(vector.Y, vector.X);

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

        /// <summary>
        /// Checks whether or not this vector have any NaNs in its components.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNaN(this Vector2 vector) => float.IsNaN(vector.X) || float.IsNaN(vector.Y);

        /// <summary>
        /// Attempts to normalize the vector. If any component is <see cref="float.NaN"/>, returns 0.
        /// </summary>
        public static Vector2 SafeNormalize(this Vector2 vector) {
            vector.Normalize();

            // check for nans, they tend to happen when you try to
            // normalize vectors with 0 length
            if (vector.IsNaN())
                return Vector2.Zero;

            return vector;
        }

        /// <summary>
        /// Finds the normalized direction vector between <paramref name="from"/> and <paramref name="to"/> multiplied by optional <paramref name="length"/>. <br/>
        /// May return <see cref="Vector2.Zero"/> if two positions are the same.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 DirectionTo(this Vector2 from, Vector2 to, float length = 1.0f) {
            return SafeNormalize(to - from) * length;
        }

        /// <summary>
        /// Calculates the angle between <paramref name="from"/> and <paramref name="to"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float AngleTo(this Vector2 from, Vector2 to) => DirectionTo(from, to).ToAngle();
    }
}
