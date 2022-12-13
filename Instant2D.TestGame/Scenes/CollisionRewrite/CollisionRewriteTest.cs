using Instant2D.EC;
using Instant2D.Graphics;
using Instant2D.Input;
using Instant2D.TestGame.Scenes.CollisionRewrite.Shapes;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D.TestGame.Scenes {
    public record struct LineCollisionResult {

    }


    public class CollisionRewriteTest : Scene {
        Polygon polyA, polyB;
        float rayAngle;

        public override void Initialize() {
            base.Initialize();

            polyA = new Polygon(new(-50, -50), new(50, -50), new(50, 50), new(-50, 50)) {
                Position = new Vector2(25, 0)
            };

            polyB = new Polygon(new(100, -100), new(100, 100), new(-100, -100)) {
                Position = new Vector2(-50, 0),
            };

            Scale(polyB, 0.75f);
        }

        void Scale(Polygon polygon, float scale) {
            for (var i = 0; i < polygon.Vertices.Count; i++) {
                polygon[i] = polygon.Vertices[i] * scale;
            }
        }

        void Rotate(Polygon polygon, float rotation) {
            for (var i = 0; i < polygon.Vertices.Count; i++) {
                polygon[i] = polygon.Vertices[i].RotatedBy(rotation);
            }
        }

        void DrawPoly(Polygon polygon, Color color, Vector2 offset = default) {
            var drawing = GraphicsManager.Context;
            for (var i = 0; i < polygon.Vertices.Count - 1; i++) {
                drawing.DrawLine(polygon.Position + offset + polygon.Vertices[i], polygon.Position + offset + polygon.Vertices[i + 1], color);
            }

            drawing.DrawLine(polygon.Position + offset + polygon.Vertices[^1], polygon.Position + offset + polygon.Vertices[0], color);
        }

        public override void Render() {
            base.Render();

            var drawing = GraphicsManager.Context;

            drawing.Push(Material.Default, SceneToScreenTransform);

            var rayOrigin = new Vector2(60, -100);
            var rayHit = ICollisionShape.LineToPolygon(rayOrigin, rayOrigin + rayAngle.ToVector2() * 200, polyA, out var fraction, out var dist, out var intersect, out var n);
            drawing.DrawLine(rayOrigin, rayHit ? intersect : rayOrigin + rayAngle.ToVector2() * 200, rayHit ? Color.Orange : Color.Green);



            if (InputManager.RightMouseDown) {
                var pos = Camera.MouseToWorldPosition();
                rayAngle = rayOrigin.AngleTo(pos);
            }

            var collision = ICollisionShape.PolygonToPolygon(polyA, polyB, out var penetration, out var normal);

            DrawPoly(polyA, collision ? Color.Red : Color.Blue);
            DrawPoly(polyB, collision ? Color.Red : Color.Blue);

            if (collision) {
                drawing.DrawLine(polyA.Position, polyA.Position - penetration, Color.Yellow, 2);
                DrawPoly(polyA, Color.Yellow, -penetration);

                polyA.Position -= penetration;
            }

            var movementA = new Vector2(
                InputManager.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.A) ? -2 : InputManager.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.D) ? 2 : 0,
                InputManager.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.W) ? -2 : InputManager.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.S) ? 2 : 0
            );

            if (InputManager.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Q)) {
                Rotate(polyA, 0.1f);
            }

            if (InputManager.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.E)) {
                Rotate(polyA, -0.1f);
            }

            polyA.Position += movementA;

            drawing.End();
        }
    }
}
