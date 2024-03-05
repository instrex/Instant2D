using Instant2D.EC;
using Instant2D.Utils;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
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

        /// <summary>
        /// Returns the list to <see cref="ListPool{T}"/>. Call <see cref="ListPool{T}.Rent"/> to get a reused list.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Pool<T>(this List<T> value) {
            ListPool<T>.Return(value);
        }

        /// <summary>
        /// Returns the value to shared pool of the type.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Pool<T>(this T value) where T: IPooledInstance, new() {
            Utils.Pool<T>.Shared.Return(value);
        }

        /// <summary>
        /// Enumerates entity children.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<Entity> GetChildren(this Entity entity) {
            for (var i = 0; i < entity.ChildrenCount; i++) {
                yield return entity[i];
            }
        }

        /// <summary>
        /// Reads all bytes and optionally disposes of <paramref name="stream"/>.
        /// </summary>
        public static byte[] ReadBytes(this Stream stream, bool dispose = false) {
            var buffer = new byte[stream.Length];
            stream.Read(buffer);

            if (dispose) {
                stream.Dispose();
            }

            return buffer;
        }
    }
}
