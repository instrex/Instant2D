using Instant2D.Collisions;
using Instant2D.Coroutines;
using Instant2D.EC;
using Instant2D.EC.Components;
using Instant2D.Graphics;
using Instant2D.Input;
using Instant2D.Utils;
using Microsoft.Xna.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instant2D.TestGame.Scenes {
    // TEMPORARY scene for collision testing
    public class CollisionTest : Scene {
        public override void Initialize() {
            // initialize the spatial hash
            Collisions = new SpatialHash<CollisionComponent>(32);

            // create some funny boxes
            for (var i = 0; i < 10000; i++) {
                var pos = Random.Shared.NextRectanglePoint(new(-1000, -1000, 1000, 1000));
                CreateEntity($"collider_{i}", pos)
                    .AddComponent<FunnyMovingBox>();
            }
        }

        class FunnyMovingBox : Component, IUpdatableComponent {
            public BoxCollisionComponent Collider;
            public Vector2 Velocity;

            public override void Initialize() {
                Entity.AddComponent(Collider = new BoxCollisionComponent {
                    Size = new(Random.Shared.Next(4, 16))
                });
            }

            public void Update() {
                if (Velocity == default)
                    return;

                var velocity = Velocity * Entity.TimeScale * (TimeManager.TimeDelta / (1.0f / 60));
                if (Collider.TryMove(velocity, out var hit)) {
                    // push other box
                    if (hit.Other.Entity.TryGetComponent<FunnyMovingBox>(out var otherBox)) {
                        otherBox.Velocity = Velocity;
                    }

                    Velocity = Vector2.Reflect(Velocity, hit.Normal);
                }
            }
        }

        public override void Update() {
            if (InputManager.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.R)) {
                SceneManager.Switch<CollisionTest>();
            }

            if (InputManager.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.W))
                Camera.Transform.Position += new Vector2(0, -2);

            if (InputManager.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.S))
                Camera.Transform.Position += new Vector2(0, 2);

            if (InputManager.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.A))
                Camera.Transform.Position += new Vector2(-2, 0);

            if (InputManager.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.D))
                Camera.Transform.Position += new Vector2(2, 0);
        }

        IEnumerator DragBox(FunnyMovingBox entity) {
            while (InputManager.LeftMouseDown) {
                entity.Transform.Position = Camera.MouseToWorldPosition();
                yield return null;
            }
        }

        public override void Render(IDrawingBackend drawing) {
            drawing.Push(Material.Default, SceneToScreenTransform);

            // draw spatial hash chunks
            //foreach (var (coords, chunk) in Collisions.Chunks) {
            //    drawing.DrawRectangle(new(coords.ToVector2() * Collisions.ChunkSize, new(Collisions.ChunkSize)),
            //        Color.Red * (0.15f * chunk.Count),
            //        Color.Crimson * (0.2f + 0.1f * chunk.Count));
            //}

            // draw individual colliders
            foreach (var entity in FindComponentsOfType<FunnyMovingBox>()) {
                var bounds = entity.Collider.BaseCollider.Bounds;

                if (!Camera.Bounds.Intersects(bounds))
                    continue;

                drawing.DrawRectangle(bounds, Color.Transparent, Color.Red);
                if (entity.Collider.BaseCollider.Bounds.Contains(Camera.MouseToWorldPosition())) {
                    if (InputManager.LeftMousePressed) {
                        this.RunCoroutine(DragBox(entity));
                    }

                    if (InputManager.MiddleMousePressed) {
                        entity.Velocity = Random.Shared.NextDirection(1);
                    }
                }
            }

            drawing.Pop();
        }
    }
}
