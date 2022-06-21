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
