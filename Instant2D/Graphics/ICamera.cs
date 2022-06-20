using Instant2D.Utils.Math;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D.Graphics {
    /// <summary>
    /// Interface for all your camera needs.
    /// </summary>
    public interface ICamera {
        /// <summary>
        /// The main transformation matrix used for drawing.
        /// </summary>
        Matrix2D TransformMatrix { get; }

        /// <summary>
        /// Transform world position to screen coordinates.
        /// </summary>
        Vector2 WorldToScreenPosition(Vector2 worldPosition);

        /// <summary>
        /// Transform screen position to world coordinates.
        /// </summary>
        Vector2 ScreenToWorldPosition(Vector2 screenPosition);
    }
}
