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
        /// <param name="vector"></param>
        /// <param name="digits"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Round(this Vector2 vector, int digits = 0) => new(MathF.Round(vector.X, digits), MathF.Round(vector.Y, digits));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Point RoundToPoint(this Vector2 vec) => new((int)MathF.Round(vec.X), (int)MathF.Round(vec.Y));
    }
}
