using Instant2D.Utils;
using Microsoft.Xna.Framework;
using System;
using System.Runtime.CompilerServices;

namespace Instant2D {
    /// <summary>
    /// Collection of common and useful methods to ease the use of Vector2.
    /// </summary>
    public static class VectorUtils {
        /// <summary> Rounds both X and Y components using <see cref="MathF.Round(float, int)"/> function. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Round(this Vector2 vector, int digits = 0) => new(MathF.Round(vector.X, digits), MathF.Round(vector.Y, digits));

        /// <summary> Floors both X and Y components using <see cref="MathF.Floor(float)"/> function. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Floor(this Vector2 vector) => new(MathF.Floor(vector.X), MathF.Floor(vector.Y));

        /// <summary> Floors both X and Y components using <see cref="MathF.Floor(float)"/> function. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Ceil(this Vector2 vector) => new(MathF.Ceiling(vector.X), MathF.Ceiling(vector.Y));

        /// <summary> Converts this Vector2 into rotation (in radians). </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ToAngle(this Vector2 vector) => MathF.Atan2(vector.Y, vector.X);

        /// <summary> Converts this rotation (in radians) into normalized Vector2. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 ToVector2(this float angle) => new(MathF.Cos(angle), MathF.Sin(angle));

        /// <summary> Rotate the provided Vector around <paramref name="center"/> by <paramref name="rotation"/> radians. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 RotatedBy(this Vector2 vector, float rotation, Vector2 center = default) {
            var (x, y) = (MathF.Cos(rotation), MathF.Sin(rotation));
            var dir = vector - center;
            return new(center.X + dir.X * x - dir.Y * y, center.Y + dir.X * y + dir.Y * x);
        }

        /// <summary> Checks whether or not this vector have any NaNs in its components. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNaN(this Vector2 vector) => float.IsNaN(vector.X) || float.IsNaN(vector.Y);

        /// <summary> Attempts to normalize the vector. If any component is <see cref="float.NaN"/>, returns 0. </summary>
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

        /// <summary> Calculates the angle between <paramref name="from"/> and <paramref name="to"/>. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float AngleTo(this Vector2 from, Vector2 to) => DirectionTo(from, to).ToAngle();

        /// <summary> Applies transformation onto a Vector2. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Transform(this Vector2 vector, Matrix2D matrix) {
            Transform(ref vector, ref matrix, out var result);
            return result;
        }

        /// <inheritdoc cref="Transform(Vector2, Matrix2D)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Transform(ref Vector2 vector, ref Matrix2D matrix, out Vector2 result) {
            (result.X, result.Y) = (
                (vector.X * matrix.M11) + (vector.Y * matrix.M21) + matrix.M31, 
                (vector.X * matrix.M12) + (vector.Y * matrix.M22) + matrix.M32
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float Repeat(float t, float length) => t - MathF.Floor(t / length) * length;

        /// <summary>
        /// Lerps an angle, automatically handling wrapping and taking shortest difference.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float LerpAngle(float value1, float value2, float amount) {
            float num = Repeat(value2 - value1, MathHelper.TwoPi);
            if (num > MathHelper.Pi) {
                num -= MathHelper.TwoPi;
            }

            return value1 + num * amount;
        }

        /// <summary>
        /// Finds the point closest to <paramref name="position"/> on the line ab.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 GetClosestPointOnLine(Vector2 a, Vector2 b, Vector2 position) {
            var v = b - a;
            var w = position - a;
            var t = Vector2.Dot(w, v) / Vector2.Dot(v, v);
            t = MathHelper.Clamp(t, 0, 1);

            return a + v * t;
        }
    }
}
