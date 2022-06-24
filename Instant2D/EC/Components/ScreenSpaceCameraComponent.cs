using Instant2D.Graphics;
using Instant2D.Utils.Math;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D.EC.Components {
    public class ScreenSpaceCameraComponent : Component, ICamera {
        public Matrix2D TransformMatrix => Matrix2D.Identity;

        public RectangleF Bounds => new(0, 0, 1000, 1000);

        public Vector2 ScreenToWorldPosition(Vector2 screenPosition) {
            throw new NotImplementedException();
        }

        public Vector2 WorldToScreenPosition(Vector2 worldPosition) {
            throw new NotImplementedException();
        }
    }
}
