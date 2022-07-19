using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D.Collisions {

    /// <summary>
    /// Static class that contains implementations of various methods required for collisions. <br/>
    /// Feel free to use them if you want to make your own collision system.
    /// </summary>
    public static class CollisionMethods {
        /// <summary>
        /// Check if two unrotated rects collide and calculate the penetration vector.
        /// </summary>
        public static bool RectToRect(RectangleF a, RectangleF b, out Vector2 penetrationVector) {
            // get intersection between two boxes
            var intersection = a.GetIntersection(b);

            // no collision...
            if (intersection == default) {
                penetrationVector = Vector2.Zero;
                return false;
            }

            penetrationVector = intersection.Width < intersection.Height ?
                new(a.Center.X < b.Center.X ? intersection.Width : -intersection.Width, 0) :
                new(0, a.Center.Y < b.Center.Y ? intersection.Height : -intersection.Height);

            return true;
        }
    }
}
