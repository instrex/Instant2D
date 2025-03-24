using Instant2D.Collision.Shapes;
using Microsoft.Xna.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D.Collision.Shapes;

public partial interface ICollisionShape {
    /// <summary>
    /// Contains intersection checks against different shapes.
    /// </summary>
    public static class Line {
        /// <summary>
        /// Intersection check between two lines.
        /// </summary>
        /// <param name="a1"> Line A starting position. </param>
        /// <param name="a2"> Line A end position. </param>
        /// <param name="b1"> Line B starting position.</param>
        /// <param name="b2"> Line B end position. </param>
        /// <param name="intersection"> Intersection point, if lines collide. </param>
        /// <returns> Whether or not intersection occured. </returns>
        public static bool ToLine(Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2, out Vector2 intersection) {
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

        /// <summary>
        /// Line intersection check against a polygon.
        /// </summary>
        /// <param name="polygon"> Shape to check collision for. </param>
        /// <param name="lineOrigin"> Origin of the line. </param>
        /// <param name="lineDestination"> End point of the line. </param>
        /// <param name="distance"> Distance from <paramref name="lineOrigin"/> to <paramref name="intersectionPoint"/>. </param>
        /// <param name="intersectionPoint"> Point of intersection. </param>
        /// <param name="normal"> Normal vector of the collision. </param>
        /// <returns> Whether or not intersection occured. </returns>
        public static bool ToPolygon(Polygon polygon, Vector2 lineOrigin, Vector2 lineDestination, out float distance, out Vector2 intersectionPoint, out Vector2 normal) {
            normal = Vector2.Zero;
            intersectionPoint = Vector2.Zero;
            distance = 0;

            var fraction = float.MaxValue;
            var hasIntersection = false;

            for (int j = polygon._vertices.Length - 1, i = 0; i < polygon._vertices.Length; j = i, i++) {
                var edge1 = polygon.Position + polygon.Vertices[j];
                var edge2 = polygon.Position + polygon.Vertices[i];
                if (ToLine(edge1, edge2, lineOrigin, lineDestination, out Vector2 intersection)) {
                    hasIntersection = true;

                    // TODO: is this the correct and most efficient way to get the fraction?
                    // check x fraction first. if it is NaN use y instead
                    var distanceFraction = (intersection.X - lineOrigin.X) / (lineDestination.X - lineOrigin.X);
                    if (float.IsNaN(distanceFraction) || float.IsInfinity(distanceFraction))
                        distanceFraction = (intersection.Y - lineOrigin.Y) / (lineDestination.Y - lineOrigin.Y);

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
                Vector2.Distance(ref lineOrigin, ref intersectionPoint, out distance);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Ray intersection test against an AABB box.
        /// </summary>
        /// <param name="boxTopLeft"> Top-left position of the box. </param>
        /// <param name="boxBottomRight"> Bottom-right position of the box. </param>
        /// <param name="rayOrigin"> Starting point of the ray. </param>
        /// <param name="rayDirection"> Direction of the ray. </param>
        /// <param name="entryDist"> Distance at which ray enters the box. </param>
        /// <param name="exitDist"> Distance at which ray exits the box. </param>
        /// <param name="normal"> Normal vector. </param>
        /// <returns> Whether or not intersection occured. </returns>
        public static bool RayToAABB(Vector2 boxTopLeft, Vector2 boxBottomRight, Vector2 rayOrigin, Vector2 rayDirection, out float entryDist, out float exitDist, out Vector2 normal) {
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

        /// <summary>
        /// Line intersection test against an AABB.
        /// </summary>
        /// <param name="boxTopLeft"> Top-left position of the box. </param>
        /// <param name="boxBottomRight"> Bottom-right position of the box. </param>
        /// <param name="lineOrigin"> Origin of the line. </param>
        /// <param name="lineDestination"> Destination point of the line. </param>
        /// <param name="distance"> Intersection distance (from the origin). </param>
        /// <param name="intersectionPoint"> Intersection position. </param>
        /// <param name="normal"> Normal vector of the intersection. </param>
        /// <returns> Whether or not intersection occured. </returns>
        public static bool ToAABB(Vector2 boxTopLeft, Vector2 boxBottomRight, Vector2 lineOrigin, Vector2 lineDestination, out float distance, out Vector2 intersectionPoint, out Vector2 normal) {
            var direction = lineOrigin.DirectionTo(lineDestination);

            // check ray intersection first
            if (!RayToAABB(boxTopLeft, boxBottomRight, lineOrigin, direction, out var inDist, out _, out normal)) {
                intersectionPoint = Vector2.Zero;
                distance = 0;

                return false;
            }

            var lengthSq = Vector2.DistanceSquared(lineOrigin, lineDestination);

            // check if too far away
            if (lengthSq < inDist * inDist) {
                intersectionPoint = Vector2.Zero;
                distance = 0;

                return false;
            }

            // calculate intersection point based on inDist
            intersectionPoint = lineOrigin + direction * inDist;
            distance = inDist;

            return true;
        }
    }
}
