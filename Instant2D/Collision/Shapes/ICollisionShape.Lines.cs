using Instant2D.Collision.Shapes;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D.Collision.Shapes {
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
                if (LineToLine(edge1, edge2, start, end, out Vector2 intersection)) {
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
                return true;
            }

            return false;
        }

        public static bool LineToBox(Vector2 boxTopLeft, Vector2 boxBottomRight, Vector2 rayOrigin, Vector2 rayDirection, out float entryDist, out float exitDist, out Vector2 normal) {
            entryDist = 0f;
            exitDist = float.MaxValue;
            normal = Vector2.Zero;

            var hitAxis = -1;
            for (int i = 0; i < 2; i++) {
                var isX = i == 0;
                float origin = isX ? rayOrigin.X : rayOrigin.Y;
                float min = isX ? boxTopLeft.X : boxTopLeft.Y;
                float max = isX ? boxBottomRight.X : boxBottomRight.Y;
                float direction = isX ? rayDirection.X : rayDirection.Y;

                if (Math.Abs(direction) < 1e-6f) {
                    if (origin < min || origin > max)
                        return false;
                } else {
                    float invD = 1.0f / direction;
                    float t0 = (min - origin) * invD;
                    float t1 = (max - origin) * invD;
                    if (t0 > t1) (t0, t1) = (t1, t0);
                    if (t0 > entryDist) {
                        entryDist = t0;
                        hitAxis = i;
                    }
                    exitDist = Math.Min(exitDist, t1);
                }
            }

            if (exitDist < entryDist)
                return false;

            if (hitAxis == 0)
                normal = new Vector2(rayOrigin.X < boxTopLeft.X ? -1 : 1, 0);
            else if (hitAxis == 1)
                normal = new Vector2(0, rayOrigin.Y < boxTopLeft.Y ? -1 : 1);

            return true;
        }
    }
}
