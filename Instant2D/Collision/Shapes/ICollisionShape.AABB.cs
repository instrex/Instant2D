using Instant2D.Collision.Shapes;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D.Collision.Shapes;

public partial interface ICollisionShape {
    /// <summary>
    /// Contains intersection checks of AABB against different shapes.
    /// </summary>
    public static class AABB {
        /// <summary>
        /// Calculates AABB to AABB intersection. Note that both boxes should have zero rotation, otherwise polygon-to-polygon collision check should be used.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="normal"></param>
        /// <param name="penetrationVector"></param>
        /// <returns></returns>
        public static bool ToAABB(Box a, Box b, out Vector2 normal, out Vector2 penetrationVector) {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static RectangleF MinkowskiDifference(Box first, Box second) {
                // we need the top-left of our first box but it must include our motion. Collider only modifies position with the motion so we
                // need to figure out what the motion was using just the position.
                var positionOffset = first.Position - (first.Bounds.Position + first.Bounds.Size / 2f);
                var topLeft = first.Bounds.Position + positionOffset - second.Bounds.BottomRight;
                var fullSize = first.Bounds.Size + second.Bounds.Size;

                return new RectangleF(topLeft.X, topLeft.Y, fullSize.X, fullSize.Y);
            }

            var diff = MinkowskiDifference(a, b);
            penetrationVector = default;
            normal = default;

            if (diff.Contains(Vector2.Zero)) {
                penetrationVector = diff.GetClosestPoint(Vector2.Zero, out normal);
                if (penetrationVector == Vector2.Zero) {
                    return false;
                }

                return true;
            }

            return false;
        }
    }
}
