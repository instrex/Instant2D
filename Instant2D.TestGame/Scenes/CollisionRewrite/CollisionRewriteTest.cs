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
        Box _box1, _box2;

        public override void Initialize() {
            base.Initialize();

            Collisions = new(32);

            for (var i = 0; i < 12; i++) {
                var collider = CreateEntity($"entity_{i}")
                    .AddComponent<BoxCollider>()
                    .SetSize(Random.Shared.NextFloat(5, 64), Random.Shared.NextFloat(5, 64));

                collider.Move(Vector2.Zero);
            }
        }

        public override void Update() {
            base.Update();

            if (InputManager.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.W)) {
                var collider = FindEntityByName("entity_0").GetComponent<BoxCollider>();
                collider.Move(collider.Transform.Position.DirectionTo(Camera.MouseToWorldPosition()) * 4);
            }

            if (InputManager.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.Q)) {
                foreach (var collider in FindComponentsOfType<CollisionComponent>()) {
                    collider.Move(Vector2.Zero);
                }
            }
        }

        public override void Render() {
            base.Render();

            Camera.Zoom = 0.5f;

            var drawing = GraphicsManager.Context;

            drawing.Push(Material.Default, SceneToScreenTransform);

            var bounds = Collisions.Bounds;
            drawing.DrawRectangle(new(bounds.Location.ToVector2() * 32, new Vector2(bounds.Width, bounds.Height) * 32), Color.Transparent, Color.Cyan, 4);

            foreach (var collider in FindComponentsOfType<CollisionComponent>()) {
                collider.DrawDebugShape(drawing);
            }

            drawing.End();
        }
    }
}
