using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D.Utils {
    public delegate float EaseFunction(float t);

    /// <summary>
    /// Commonly used easing functions.
    /// </summary>
    public static class Easings {
        #region Default easing functions 

        // Taken from: https://gist.github.com/Kryzarel/bba64622057f21a1d6d44879f9cd7bd4
        // Thanks, Kryzarel!

        public static float Linear(float t) => t;
        public static float QuadIn(float t) => t * t;
        public static float QuadOut(float t) => 1 - QuadIn(1 - t);
        public static float QuadInOut(float t) {
            if (t < 0.5) return QuadIn(t * 2) / 2;
            return 1 - QuadIn((1 - t) * 2) / 2;
        }

        public static float CubicIn(float t) => t * t * t;
        public static float CubicOut(float t) => 1 - CubicIn(1 - t);
        public static float CubicInOut(float t) {
            if (t < 0.5) return CubicIn(t * 2) / 2;
            return 1 - CubicIn((1 - t) * 2) / 2;
        }

        public static float QuartIn(float t) => t * t * t * t;
        public static float QuartOut(float t) => 1 - QuartIn(1 - t);
        public static float QuartInOut(float t) {
            if (t < 0.5) return QuartIn(t * 2) / 2;
            return 1 - QuartIn((1 - t) * 2) / 2;
        }

        public static float QuintIn(float t) => t * t * t * t * t;
        public static float QuintOut(float t) => 1 - QuintIn(1 - t);
        public static float QuintInOut(float t) {
            if (t < 0.5) return QuintIn(t * 2) / 2;
            return 1 - QuintIn((1 - t) * 2) / 2;
        }

        public static float SineIn(float t) => -MathF.Cos(t * MathF.PI / 2);
        public static float SineOut(float t) => MathF.Sin(t * MathF.PI / 2);
        public static float SineInOut(float t) => (MathF.Cos(t * MathF.PI) - 1) / -2;

        public static float ExpoIn(float t) => (float)MathF.Pow(2, 10 * (t - 1));
        public static float ExpoOut(float t) => 1 - ExpoIn(1 - t);
        public static float ExpoInOut(float t) {
            if (t < 0.5) return ExpoIn(t * 2) / 2;
            return 1 - ExpoIn((1 - t) * 2) / 2;
        }

        public static float CircIn(float t) => -(MathF.Sqrt(1 - t * t) - 1);
        public static float CircOut(float t) => 1 - CircIn(1 - t);
        public static float CircInOut(float t) {
            if (t < 0.5) return CircIn(t * 2) / 2;
            return 1 - CircIn((1 - t) * 2) / 2;
        }

        public static float ElasticIn(float t) => 1 - ElasticOut(1 - t);
        public static float ElasticOut(float t) {
            float p = 0.3f;
            return MathF.Pow(2, -10 * t) * MathF.Sin((t - p / 4) * (2 * MathF.PI) / p) + 1;
        }

        public static float ElasticInOut(float t) {
            if (t < 0.5) return ElasticIn(t * 2) / 2;
            return 1 - ElasticIn((1 - t) * 2) / 2;
        }

        public static float BackIn(float t) {
            float s = 1.70158f;
            return t * t * ((s + 1) * t - s);
        }

        public static float BackOut(float t) => 1 - BackIn(1 - t);
        public static float BackInOut(float t) {
            if (t < 0.5) return BackIn(t * 2) / 2;
            return 1 - BackIn((1 - t) * 2) / 2;
        }

        public static float BounceIn(float t) => 1 - BounceOut(1 - t);
        public static float BounceOut(float t) {
            float div = 2.75f;
            float mult = 7.5625f;

            if (t < 1 / div) {
                return mult * t * t;
            } else if (t < 2 / div) {
                t -= 1.5f / div;
                return mult * t * t + 0.75f;
            } else if (t < 2.5 / div) {
                t -= 2.25f / div;
                return mult * t * t + 0.9375f;
            } else {
                t -= 2.625f / div;
                return mult * t * t + 0.984375f;
            }
        }

        public static float BounceInOut(float t) {
            if (t < 0.5) return BounceIn(t * 2) / 2;
            return 1 - BounceIn((1 - t) * 2) / 2;
        }

        #endregion

        /// <summary>
        /// Apply provided <paramref name="ease"/> function to interpolate value between <paramref name="from"/> and <paramref name="to"/>. <br/>
        /// <paramref name="t"/> should be in the range of <c>[0.0, 1.0]</c>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Tween(EaseFunction ease, float from, float to, float t) {
            return from + (to - from) * ease(System.Math.Clamp(t, 0, 1));
        }

        /// <inheritdoc cref="Tween(EaseFunction, float, float, float)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Tween(EaseFunction ease, Vector2 from, Vector2 to, float t) {
            return new Vector2(Tween(ease, from.X, to.X, t), Tween(ease, from.Y, to.Y, t));
        }

        /// <inheritdoc cref="Tween(EaseFunction, float, float, float)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Color Tween(EaseFunction ease, Color from, Color to, float t) {
            return new Color(
                Tween(ease, from.R / 255f, to.R / 255f, t),
                Tween(ease, from.G / 255f, to.G / 255f, t),
                Tween(ease, from.B / 255f, to.B / 255f, t),
                Tween(ease, from.A / 255f, to.A / 255f, t)
            );
        }
    }
}
