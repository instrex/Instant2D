using Instant2D.EC;
using Instant2D.Graphics;
using Instant2D.Input;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D.TestGame.Scenes {
    public class SATTest : Scene {
        Polygon a = new Polygon() {
            localVertices = new Vector2[] {
                new(-20f, 0f),
                new(0f, -20f),
                new(20f, 0f),
                new(0f, 20f)
            }
        }.Recalculate();

        Polygon b = new Polygon() {
            localVertices = new Vector2[] {
                new(-20f, -20f),
                new(20f, -20f),
                new(20f, 20f),
                new(-20f, 20f)
            }
        }.Recalculate();

        struct Polygon {
            public Vector2 position;
            public Vector2[] localVertices;
            public Vector2[] vertices;
            public Vector2 offset;

            public Polygon Recalculate() {
                vertices = new Vector2[localVertices.Length];
                for (var i = 0; i < localVertices.Length; i++) {
                    vertices[i] = localVertices[i] + position;
                }

                return this;
            }
        }

        public override void Initialize() {
            base.Initialize();
        }

        static Vector2 GetPerpendicularAxis(in Vector2[] vertices, int index) {
            var a = vertices[index];
            var b = index + 1 >= vertices.Length ? vertices[0] : vertices[index + 1];
            return new Vector2(-(b.Y - a.Y), b.X - a.X).SafeNormalize();
        }

        static Vector2 ProjectVertices(Vector2 axis, in Vector2[] vertices) {
            var min = Vector2.Dot(axis, vertices[0]);
            var max = min;

            for (var i = 1; i < vertices.Length; i++) {
                var value = Vector2.Dot(axis, vertices[i]);
                if (value < min) min = value;
                if (value > max) max = value;
            }

            return new Vector2(min, max);
        }

        static bool PolygonToPolygon(in Polygon a, in Polygon b, out Vector2 penetration) {
            var shortestDist = float.MaxValue;
            penetration = Vector2.Zero;

            var vOffset = new Vector2(a.position.X - b.position.X, a.position.Y - b.position.Y);

            for (var i = 0; i < a.vertices.Length; i++) {
                var axis = GetPerpendicularAxis(a.vertices, i);
                var rangeA = ProjectVertices(axis, a.vertices);
                var rangeB = ProjectVertices(axis, b.vertices);

                var scalarOffset = Vector2.Dot(axis, vOffset);
                rangeA += new Vector2(scalarOffset);

                // no collision
                if ((rangeA.X - rangeB.Y > 0) || (rangeB.X - rangeA.Y > 0)) {
                    return false;
                }

                var dist = (rangeB.Y - rangeA.X) * -1;
                if (Math.Abs(dist) < shortestDist) {
                    shortestDist = Math.Abs(dist);
                    penetration = axis * dist;
                }
            }

            return penetration != Vector2.Zero;
        }

        void DrawPolygon(Polygon polygon, Color color, DrawingContext drawing) {
            for (var i = 0; i < polygon.localVertices.Length; i++) {
                drawing.DrawLine(polygon.vertices[i] + polygon.offset, polygon.vertices[i + 1 >= polygon.vertices.Length ? 0 : i + 1] + polygon.offset, color);
            }
        }

        public override void Render() {
            base.Render();

            var drawing = GraphicsManager.Context;
            drawing.Push(Material.Default, SceneToScreenTransform);

            if (InputManager.LeftMouseDown) {
                a.position = Camera.MouseToWorldPosition();
                a.Recalculate();
            }

            if (InputManager.RightMouseDown) {
                b.position = Camera.MouseToWorldPosition();
                b.Recalculate();
            }

            var collisionOccured = PolygonToPolygon(a, b, out var penetration);

            DrawPolygon(a, collisionOccured ? Color.Green : Color.White, drawing);
            DrawPolygon(b, collisionOccured ? Color.Green : Color.White, drawing);

            drawing.DrawPoint(a.position, Color.White, 4);
            drawing.DrawPoint(b.position, Color.White, 4);

            if (collisionOccured) {
                drawing.DrawLine(b.position, b.position + penetration, Color.Yellow, 2);
                DrawPolygon(b with { offset = penetration }, Color.Yellow, drawing);
            }

            drawing.Pop();
        }
    }
}
