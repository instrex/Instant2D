using Microsoft.Xna.Framework;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D.Utils.Math {
    public struct RectangleF : IEquatable<RectangleF> {
        public float X, Y, Width, Height;

        public RectangleF(Vector2 position, Vector2 size) : this(position.X, position.Y, size.X, size.Y) { }
        public RectangleF(float x, float y, float width, float height) {
            Width = width;
            Height = height;
            X = x;
            Y = y;
        }

        /// <summary>
        /// Returns empty rectangle with all the components set to 0.
        /// </summary>
        public static RectangleF Empty => new();

        /// <summary>
        /// Constructs a new <see cref="RectangleF"/> instance using four rectangle points.
        /// </summary>
        public static RectangleF FromCoordinates(Vector2 a, Vector2 b, Vector2 c, Vector2 d) {
            // ugly as hecc but what can you do ...
            var minX = MathF.Min(MathF.Min(MathF.Min(a.X, b.X), c.X), d.X);
            var maxX = MathF.Max(MathF.Max(MathF.Max(a.X, b.X), c.X), d.X);
            var minY = MathF.Min(MathF.Min(MathF.Min(a.Y, b.Y), c.Y), d.Y);
            var maxY = MathF.Max(MathF.Max(MathF.Max(a.Y, b.Y), c.Y), d.Y);
            return new RectangleF(minX, minY, maxX - minX, maxY - minY);
        }

        /// <summary>
        /// Top-left coordinates of the rectangle.
        /// </summary>
        public Vector2 Position {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(X, Y);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set {
                X = value.X;
                Y = value.Y;
            }
        }

        /// <summary>
        /// Returns the size of this rectangle as <see cref="Vector2"/>.
        /// </summary>
        public Vector2 Size {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(Width, Height);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set {
                Width = value.X;
                Height = value.Y;
            }
        }

        /// <summary>
        /// Checks whether or not point <paramref name="value"/> lies in the rectangle.
        /// </summary>
        public bool Contains(Vector2 value) => (X <= value.X) && (value.X < (X + Width)) && (Y <= value.Y) && (value.Y < (Y + Height));

        /// <inheritdoc cref="Contains(Vector2)"/>
        public void Contains(ref Vector2 value, out bool result) {
            result = (X <= value.X) && (value.X < (X + Width)) && (Y <= value.Y) && (value.Y < (Y + Height));
        }

        /// <summary>
        /// Checks if two rectangles intersect each other.
        /// </summary>
        public bool Intersects(RectangleF other) => (X <= other.X) && ((other.X + other.Width) <= (X + Width)) && (Y <= other.Y) && ((other.Y + other.Height) <= (Y + Height));

        /// <inheritdoc cref="Intersects(RectangleF)"/>
        public void Intersects(ref RectangleF value, out bool result) {
            result = (X <= value.X) && (value.X < (X + Width)) && (Y <= value.Y) && (value.Y < (Y + Height));
        }

        #region Equality

        public override int GetHashCode() => HashCode.Combine(X, Y, Width, Height);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals([NotNullWhen(true)] object obj) {
            return obj is RectangleF other && Equals(other);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(RectangleF other) => other.X == X
            && other.Y == Y
            && other.Width == Width
            && other.Height == Height;

        public static bool operator ==(RectangleF left, RectangleF right) => left.Equals(right);

        public static bool operator !=(RectangleF left, RectangleF right) => !(left == right);

        #endregion
    }
}
