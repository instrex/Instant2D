using Instant2D.TestGame.Scenes.CollisionRewrite.Shapes;
using Microsoft.Xna.Framework;

namespace Instant2D.TestGame.Scenes {
    public partial interface ICollisionShape {
        public Vector2 Position { get; set; }

        public RectangleF Bounds { get; }

        bool CheckOverlap(ICollisionShape other);

        bool CollidesWith(ICollisionShape other, out Vector2 normal, out Vector2 penetrationVector);

        bool CollidesWithLine(Vector2 start, Vector2 end, out LineCollisionResult result);

        bool ContainsPoint(Vector2 point);
    }

    // ICollisionShape.Lines.cs
    public partial interface ICollisionShape {
        public static bool LineToLine(Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2, out Vector2 intersection) {
            intersection = Vector2.Zero;

            var b = a2 - a1;
            var d = b2 - b1;
            var bDotDPerp = b.X * d.Y - b.Y * d.X;

            // if b dot d == 0, it means the lines are parallel so have infinite intersection points
            if (bDotDPerp == 0)
                return false;

            var c = b1 - a1;
            var t = (c.X * d.Y - c.Y * d.X) / bDotDPerp;
            if (t < 0 || t > 1)
                return false;

            var u = (c.X * b.Y - c.Y * b.X) / bDotDPerp;
            if (u < 0 || u > 1)
                return false;

            intersection = a1 + t * b;

            return true;
        }

        public static bool LineToPolygon(Vector2 start, Vector2 end, Polygon polygon, out float fraction, out float distance, out Vector2 intersectionPoint, out Vector2 normal) {
            normal = Vector2.Zero;
            intersectionPoint = Vector2.Zero;
            fraction = float.MaxValue;
            distance = 0;

            var hasIntersection = false;

            for (int j = polygon._vertices.Length - 1, i = 0; i < polygon._vertices.Length; j = i, i++) {
                var edge1 = polygon.Position + polygon.Vertices[j];
                var edge2 = polygon.Position + polygon.Vertices[i];
                Vector2 intersection;
                if (LineToLine(edge1, edge2, start, end, out intersection)) {
                    hasIntersection = true;

                    // TODO: is this the correct and most efficient way to get the fraction?
                    // check x fraction first. if it is NaN use y instead
                    var distanceFraction = (intersection.X - start.X) / (end.X - start.X);
                    if (float.IsNaN(distanceFraction) || float.IsInfinity(distanceFraction))
                        distanceFraction = (intersection.Y - start.Y) / (end.Y - start.Y);

                    if (distanceFraction < fraction) {
                        var edge = edge2 - edge1;
                        normal = new Vector2(edge.Y, -edge.X);
                        fraction = distanceFraction;
                        intersectionPoint = intersection;
                    }
                }
            }

            if (hasIntersection) {
                normal.Normalize();
                Vector2.Distance(ref start, ref intersectionPoint, out distance);
                //hit.SetValues(fraction, distance, intersectionPoint, normal);
                return true;
            }

            return false;
        }
    }
}
