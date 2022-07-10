﻿using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D {
    public static partial class Extensions {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 ToVector2(this Point point) => new(point.X, point.Y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Point RoundToPoint(this Vector2 vec) => new((int)MathF.Round(vec.X), (int)MathF.Round(vec.Y));

        /// <summary>
        /// Adds an item into a dictionary or sets it if the key already exists.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddOrSet<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value) {
            if (!dictionary.TryAdd(key, value))
                dictionary[key] = value;
        }
    }
}