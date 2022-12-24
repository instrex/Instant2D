using Instant2D.Collision;
using Instant2D.Collision.Shapes;
using Instant2D.EC;
using Instant2D.EC.Components;
using Instant2D.EC.Components.Collisions;
using Instant2D.Graphics;
using Instant2D.Input;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D.TestGame.Scenes {
    public class CollisionRewriteTest : Scene {
        CollisionComponent _controlledBox;

        // line cast stuff
        List<LineCastResult<CollisionComponent>> _lineHits;
        Vector2 _lineStart, _lineEnd;

        public override void Initialize() {
            base.Initialize();

            Collisions = new(32);

            for (var i = 0; i < 12; i++) {
                var collider = CreateEntity($"entity_{i}")
                    .AddComponent<BoxCollider>()
                    .SetSize(i == 0 ? 50 : Random.Shared.NextFloat(5, 64), i == 0 ? 10 : Random.Shared.NextFloat(5, 64));

                collider.Move(Vector2.Zero);

                if (i == 0) {
                    _controlledBox = collider;
                }
            }

            var poly = new Vector2[] { new(2.5f, 1.2f), new(0f, 2.5f), new(-2.5f, 1.2f), new(-1.25f, -1.2f) };
            Polygon.Scale(poly, new Vector2(100f));

            CreateEntity("big_poly", new(50))
                .AddComponent(new PolygonCollider(poly));

            for (var i = 0; i < 5; i++)
                PushColliders();
        }

        public override void Update() {
            base.Update();

            if (_controlledBox != null) {
                if (_lineHits != null) {
                    // transfer consciousness
                    if (InputManager.RightMouseReleased) {
                        for (var i = 0; i < _lineHits.Count; i++) {
                            if (_lineHits[i].Self != _controlledBox) {
                                _controlledBox = _lineHits[i].Self;
                                break;
                            }
                        }
                    }

                    // return the hits
                    _lineHits.Pool();
                    _lineHits = null;
                }

                Camera.Transform.Position = _controlledBox.Transform.Position;
                Camera.ForceUpdate();

                if (InputManager.LeftMouseDown) {
                    // move in the direction of cursor
                    _controlledBox.Move(_controlledBox.Transform.Position.DirectionTo(Camera.MouseToWorldPosition()) * 4);
                }

                if (InputManager.RightMouseDown) {
                    _lineStart = _controlledBox.Transform.Position;
                    _lineEnd = _controlledBox.Transform.Position + _controlledBox.Transform.Position.DirectionTo(Camera.MouseToWorldPosition()) * 128;

                    // sort the rays by distance on success
                    if (Collisions.Linecast(_lineStart, _lineEnd, out _lineHits, ignoreOriginColliders: true))
                        _lineHits.Sort();
                }

                if (InputManager.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.A) || InputManager.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.D)) {
                    var rotation = InputManager.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.A) ? -0.1f : 0.1f;
                    _controlledBox.Transform.Rotation += rotation;
                    _controlledBox.UpdateCollider();
                    _controlledBox.Move(Vector2.Zero);
                }
            }

            if (InputManager.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.Q)) {
                PushColliders();
            }

            if (InputManager.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.Escape)) {
                SceneManager.Switch<Game.MainScene>();
            }
        }

        void PushColliders() {
            foreach (var collider in FindComponentsOfType<CollisionComponent>()) {
                collider.Move(Vector2.Zero);
            }
        }

        static void VisualizeCollider(CollisionComponent collider, Color color, Vector2 offset, bool drawBounds = true) {
            if (drawBounds) {
                // draw bounds
                GraphicsManager.Context.DrawRectangle(collider.Shape.Bounds, Color.Transparent, Color.Gray, 2);
                GraphicsManager.Context.DrawLine(collider.Shape.Bounds.TopLeft, collider.Shape.Bounds.BottomRight, Color.Gray, 2);

                // draw position
                GraphicsManager.Context.DrawPoint(collider.Transform.Position, Color.Yellow, 4);
            }
            
            // get polygon vertices when possible
            var polygonVertices = collider.Shape is Box rotatedBox && rotatedBox.HasPolygon ? rotatedBox.Polygon.Vertices : (collider.Shape is Polygon poly ? poly.Vertices : null);

            if (polygonVertices != null) {
                for (var i = 0; i < polygonVertices.Length; i++) {
                    GraphicsManager.Context.DrawLine(collider.Shape.Position + polygonVertices[i] + offset, collider.Shape.Position + polygonVertices[i + 1 >= polygonVertices.Length ? 0 : i + 1] + offset, color, 2);
                }

                return;
            }

            switch (collider.Shape) {
                case Box box:
                    var bounds = box.Bounds;
                    bounds.Position += offset;
                    GraphicsManager.Context.DrawRectangle(bounds, Color.Transparent, color, 2);
                    break;
            }
        }

        public override void Render() {
            base.Render();

            Camera.Zoom = 0.5f;

            var drawing = GraphicsManager.Context;

            drawing.Push(Material.Default, SceneToScreenTransform);

            var bounds = Collisions.Bounds;

            // draw spatial hash bounds
            drawing.DrawRectangle(new(bounds.Location.ToVector2() * 32, new Vector2(bounds.Width, bounds.Height) * 32), Color.Transparent, Color.Cyan * (0.5f + 0.25f * MathF.Sin(TotalTime * 4)), 2);

            // visualize colliders
            foreach (var collider in FindComponentsOfType<CollisionComponent>()) {
                VisualizeCollider(collider, collider == _controlledBox ? Color.Blue : Color.Red, Vector2.Zero);
            }

            // visualize line cast
            if (_lineHits != null) {
                if (_lineHits.Count >= 1) drawing.DrawLine(_lineHits[0].LineOrigin, _lineHits[0].Point, Color.Yellow);

                // highlight colliders
                for (int i = 0; i < _lineHits.Count; i++) {
                    var hit = _lineHits[i];
                    VisualizeCollider(hit.Self, Color.Yellow, Vector2.Zero, false);
                }

                // draw lines
                for (int i = 0; i < _lineHits.Count; i++) {
                    var hit = _lineHits[i];

                    drawing.DrawPoint(hit.Point, Color.Yellow, 8);
                    drawing.DrawString(i.ToString(), hit.Point - new Vector2(2, 3.5f), Color.Black, new Vector2(1), 0);
                }
            }

            drawing.Pop();

            drawing.Push(Material.Default, Matrix.Identity);

            drawing.DrawString($"Spatial Hash region: {Collisions.Bounds.Width}x{Collisions.Bounds.Height} ({Collisions.Bounds.Width * Collisions.Bounds.Height} chunks)\n" +
                $"LMB: Move\n" +
                $"RMB: Switch object\n" +
                $"A / D: Rotate\n" +
                $"Q: Push objects apart\n" +
                $"ESC: Main menu", new(10), Color.White, new Vector2(2), 0, drawOutline: true);

            drawing.End();
        }
    }
}
