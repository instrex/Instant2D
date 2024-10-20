using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D {
    public record struct RectangleF {
        public float X, Y, Width, Height;

        public RectangleF(Vector2 position, Vector2 size) : this(position.X, position.Y, size.X, size.Y) { }
        public RectangleF(float x, float y, float width, float height) {
            Width = width;
            Height = height;
            X = x;
            Y = y;
        }

        // idk why but doing this should be more performant..?
        static RectangleF _empty = new();

        /// <summary>
        /// Returns empty rectangle with all the components set to 0.
        /// </summary>
        public static RectangleF Empty => _empty;

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
        /// Constructs a new <see cref="RectangleF"/> instance using four rectangle points.
        /// </summary>
        public static RectangleF FromCoordinates(Vector2 min, Vector2 max) {
            return new RectangleF(min, max - min);
        }

        /// <inheritdoc cref="FromCenteredBox(Vector2, Vector2)"/>
        public static RectangleF FromCenteredBox(Vector2 position, float size) => FromCenteredBox(position, new Vector2(size));

        /// <summary>
        /// Constructs a new <see cref="RectangleF"/> instance representing a box with <paramref name="size"/> centered around <paramref name="position"/>.
        /// </summary>
        public static RectangleF FromCenteredBox(Vector2 position, Vector2 size) {
            return new(position - size * 0.5f, size);
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

        public float Left => X;
        public float Right => X + Width;
        public float Top => Y;
        public float Bottom => Y + Height;

        public Vector2 Center => new(X + Width * 0.5f, Y + Height * 0.5f);
        public Vector2 TopLeft => Position;
        public Vector2 TopRight => new(Right, Top);
        public Vector2 BottomLeft => new(Left, Bottom);
        public Vector2 BottomRight => new(Right, Bottom);


        /// <summary>
        /// Checks whether or not point <paramref name="value"/> lies in the rectangle.
        /// </summary>
        public bool Contains(Vector2 value) => (X <= value.X) && (value.X < (X + Width)) && (Y <= value.Y) && (value.Y < (Y + Height));

        /// <inheritdoc cref="Contains(Vector2)"/>
        public void Contains(ref Vector2 value, out bool result) {
            result = (X <= value.X) && (value.X < (X + Width)) && (Y <= value.Y) && (value.Y < (Y + Height));
        }

        /// <summary>
        /// Checks this rectangle contains another rectangle.
        /// </summary>
        public bool Contains(RectangleF other) => (X <= other.X) && ((other.X + other.Width) <= (X + Width)) && (Y <= other.Y) && ((other.Y + other.Height) <= (Y + Height));

        /// <inheritdoc cref="Contains(RectangleF)"/>
        public void Contains(ref RectangleF value, out bool result) {
            result = (X <= value.X) && (value.X < (X + Width)) && (Y <= value.Y) && (value.Y < (Y + Height));
        }

        /// <summary>
        /// Checks whether or not two rectangles intersect.
        /// </summary>
        public bool Intersects(RectangleF value) {
            return value.Left < Right &&
                   Left < value.Right &&
                   value.Top < Bottom &&
                   Top < value.Bottom;
        }

        /// <summary>
        /// Gets intersection between two rectangles.
        /// </summary>
        public RectangleF GetIntersection(RectangleF other) {
            var (minA, minB, maxA, maxB) = (
                TopLeft, other.TopLeft,
                BottomRight, other.BottomRight
            );

            // get min/max
            var min = new Vector2(minA.X > minB.X ? minA.X : minB.X, minA.Y > minB.Y ? minA.Y : minB.Y);
            var max = new Vector2(maxA.X < maxB.X ? maxA.X : maxB.X, maxA.Y < maxB.Y ? maxA.Y : maxB.Y);

            // uh...
            if ((max.X < min.X) || (max.Y < min.Y))
                return _empty;

            return FromCoordinates(min, max);
        }

        /// <summary>
        /// Gets the closes point on border, additionally calculating edge normal.
        /// </summary>
        public Vector2 GetClosestPoint(Vector2 point, out Vector2 edgeNormal) {
            edgeNormal = Vector2.Zero;

            // for each axis, if the point is outside the box clamp it to the box else leave it alone
            var res = new Vector2(MathHelper.Clamp(point.X, Left, Right), MathHelper.Clamp(point.Y, Top, Bottom));

            // if point is inside the rectangle we need to push res to the border since it will be inside the rect
            if (Contains(res)) {
                var dl = res.X - Left;
                var dr = Right - res.X;
                var dt = res.Y - Top;
                var db = Bottom - res.Y;

                var min = MathF.Min(dl, MathF.Min(dr, Math.Min(dt, db)));
                if (min == dt) {
                    res.Y = Top;
                    edgeNormal.Y = -1;
                } else if (min == db) {
                    res.Y = Bottom;
                    edgeNormal.Y = 1;
                } else if (min == dl) {
                    res.X = Left;
                    edgeNormal.X = -1;
                } else {
                    res.X = Right;
                    edgeNormal.X = 1;
                }
            } else {
                if (res.X == Left)
                    edgeNormal.X = -1;
                if (res.X == Right)
                    edgeNormal.X = 1;
                if (res.Y == Top)
                    edgeNormal.Y = -1;
                if (res.Y == Bottom)
                    edgeNormal.Y = 1;
            }

            return res;
        }

        public RectangleF Inflated(float amount) => Inflated(amount, amount);
        public RectangleF Inflated(Vector2 amount) => Inflated(amount.X, amount.Y);
        public RectangleF Inflated(float horizontalAmount, float verticalAmount) {
            var offset = new Vector2(horizontalAmount, verticalAmount);
            return this with {
                Position = Position - offset * 0.5f,
                Size = Size + offset
            };
        }
    }
}
