using Instant2D.Utils;
using Instant2D.Utils.Math;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Instant2D {
    public static partial class Extensions {
        /// <summary>
        /// A shortcut to <see cref="Random.NextSingle"/>, because who the hell knows why it's named like that.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float NextFloat(this Random random) => random.NextSingle();

        /// <summary>
        /// Gets a random float value in the range [0, <paramref name="maxValue"/>].
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float NextFloat(this Random random, float maxValue) => NextFloat(random) * maxValue;

        /// <summary>
        /// Gets a random float value in the range [<paramref name="min"/>, <paramref name="max"/>].
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float NextFloat(this Random random, float min, float max) => min + NextFloat(random) * (max - min);

        /// <summary>
        /// Gets a random float value in the range [0, <see cref="MathHelper.TwoPi"/>].
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float NextAngle(this Random random) => NextFloat(random, MathHelper.TwoPi);

        /// <summary>
        /// Gets a random normalized vector multiplied by <paramref name="length"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 NextDirection(this Random random, float length = 1.0f) => NextAngle(random).ToVector2() * length;

        /// <summary>
        /// Gets a random point from rectangle.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 NextRectanglePoint(this Random random, RectangleF rectangle) => 
            new(NextFloat(random, rectangle.Left, rectangle.Right), NextFloat(random, rectangle.Top, rectangle.Bottom));

        /// <summary>
        /// Randomly chooses a value between <paramref name="first"/> and <paramref name="second"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Choose<T>(this Random random, T first, T second) => random.Next(2) switch {
            0 => first,
            _ => second
        };

        /// <summary>
        /// Randomly chooses a value between <paramref name="first"/>, <paramref name="second"/> and <paramref name="third"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Choose<T>(this Random random, T first, T second, T third) => random.Next(3) switch {
            0 => first,
            1 => second,
            _ => third
        };

        /// <summary>
        /// Randomly chooses a value between multiple arguments.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Choose<T>(this Random random, params T[] values) => NextItem(random, values);

        /// <summary>
        /// Randomly selects a value from <paramref name="collection"/>, does not check emptiness!
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T NextItem<T>(this Random random, IReadOnlyList<T> collection) => collection[random.Next(collection.Count)];

        /// <summary>
        /// Randomly selects a value from <paramref name="collection"/> based on weights obtained from <paramref name="weightSelector"/>, does not check emptiness!
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T NextItemWeighted<T>(this Random random, IReadOnlyList<T> collection, Func<T, float> weightSelector) {
            var itemPool = ListPool<(T item, float weight)>.Get();
            itemPool.AddRange(collection.Select(i => (i, weightSelector(i))));

            // pick a random float value based on maxWeight
            var pick = NextFloat(random, itemPool.Sum(p => p.weight));
            for (var i = 0; i < itemPool.Count; i++) {
                var (item, weight) = itemPool[i];
 
                // free the pool and return an item
                if (weight > pick) {
                    itemPool.Pool();
                    return item;
                }

                pick -= weight;
            }

            return default;
        }

        /// <summary>
        /// Randomly selects multiple distinct values from the <paramref name="collection"/>. Will throw if <paramref name="count"/> is greater than list count.
        /// </summary>
        /// <remarks> Returned list may be pooled, so when you're done, call <see cref="ListPool{T}.Return(List{T})"/> on it. </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<T> NextItems<T>(this Random random, IReadOnlyList<T> collection, int count) {
            if (count > collection.Count)
                throw new InvalidOperationException();

            // pool a backing list for values
            var values = ListPool<T>.Get();
            values.AddRange(collection);

            // gather enough items
            var list = ListPool<T>.Get();
            while (list.Count < count) {
                var item = NextItem(random, values);
                values.Remove(item);
                list.Add(item);
            }

            values.Pool();

            return list;
        }
    }
}
