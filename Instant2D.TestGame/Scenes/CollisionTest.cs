using Instant2D.Collisions;
using Instant2D.Coroutines;
using Instant2D.EC;
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
        static SpatialHash<Entity> _spatialHash = new(12);

        class ColliderComponent : Component, IUpdatableComponent {
            public BoxCollider<Entity> Collider;
            public Vector2 Velocity;
            bool _firstUpdate = true;

            public override void Initialize() {
                Collider.Entity = this;
                _spatialHash.AddCollider(Collider);
            }

            public override void OnTransformUpdated(TransformComponentType components) {
                Collider.Position = Transform.Position;
                _spatialHash.RemoveCollider(Collider);
                _spatialHash.AddCollider(Collider);
            }

            public void Update() {
                var nearby = _spatialHash.Broadphase(Collider.Bounds, Collider.CollidesWith);
                for (var i = 0; i < nearby.Count; i++) {
                    var other = nearby[i];
                    if (Collider != other && Collider.CheckCollision(other, out var hit)) {
                        Collider.Position -= hit.PenetrationVector;
                    }
                }

                if (Velocity == Vector2.Zero)
                    return;

                Collider.Position += Velocity;

                var nearbyColliders = _spatialHash.Broadphase(Collider.Bounds, Collider.CollidesWith);
                for (var i = 0; i < nearbyColliders.Count; i++) {
                    var other = nearbyColliders[i];
                    if (Collider != other && Collider.CheckCollision(other, out var hit)) {
                        Collider.Position -= hit.PenetrationVector;
                        Velocity = Vector2.Reflect(Velocity, hit.Normal);
                    }
                }
            }
        }

        public override void Initialize() {
            for (var i = 0; i < 1000; i++) {
                var pos = Random.Shared.NextRectanglePoint(new(-300, -300, 1300, 1300));
                CreateEntity($"collider_{i}", pos)
                    .AddComponent(new ColliderComponent {
                        Collider = new BoxCollider<Entity> {
                            Position = pos,
                            Size = new Vector2(Random.Shared.Next(6, 32))
                        }
                    });
            }
        }

        public override void Update() {
            if (InputManager.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.R)) {
                _spatialHash = new SpatialHash<Entity>(_spatialHash.ChunkSize);
                SceneManager.Instance.Current = new CollisionTest();
            }

            if (InputManager.RightMousePressed) {
                this.RunCoroutine(TestBroadphase(Camera.MouseToWorldPosition()));
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

        IEnumerator DragCollider(ColliderComponent entity) {
            while (InputManager.LeftMouseDown) {
                entity.Transform.Position = Camera.MouseToWorldPosition();
                yield return null;
            }
        }

        List<BaseCollider<Entity>> _broadphased;
        RectangleF _broadrect;

        IEnumerator TestBroadphase(Vector2 position) {
            _broadrect = new(position - new Vector2(16), new(32));
            _broadphased = _spatialHash.Broadphase(_broadrect, -1);

            yield return 2;

            _broadrect = RectangleF.Empty;
            _broadphased = null;
        }

        public override void Render(IDrawingBackend drawing) {
            drawing.Push(Material.Default, SceneToScreenTransform);

            // draw spatial hash chunks
            //foreach (var (coords, chunk) in _spatialHash.Chunks) {
            //    drawing.DrawRectangle(new(coords.ToVector2() * _spatialHash.ChunkSize, new(_spatialHash.ChunkSize)),
            //        Color.Red * (0.15f * chunk.Count),
            //        Color.Crimson * (0.2f + 0.1f * chunk.Count));
            //}

            if (_broadphased != null) {
                drawing.DrawRectangle(_broadrect, Color.Transparent, Color.Blue);
            }

            // draw individual colliders
            foreach (var entity in FindComponentsOfType<ColliderComponent>()) {
                var isBroadphased = _broadphased != null && _broadphased.Contains(entity.Collider);
                drawing.DrawRectangle(entity.Collider.Bounds, isBroadphased ? Color.Green * 0.5f : Color.Transparent, Color.Red);
                if (entity.Collider.Bounds.Contains(Camera.MouseToWorldPosition())) {
                    if (InputManager.LeftMousePressed) {
                        this.RunCoroutine(DragCollider(entity));
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
