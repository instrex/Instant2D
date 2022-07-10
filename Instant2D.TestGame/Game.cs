using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Instant2D.Assets.Loaders;
using Instant2D.Core;
using Instant2D.EC;
using Instant2D.EC.Components;
using Instant2D.Graphics;
using Instant2D.Input;
using Instant2D.Utils;
using Instant2D.Utils.Coroutines;
using Instant2D.Utils.Math;
using Instant2D.Utils.ResolutionScaling;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using static System.Formats.Asn1.AsnWriter;

namespace Instant2D.TestGame {
    public class Game : InstantGame {
        protected override void SetupSystems() {
            AddSystem<AssetManager>(assets => {
                assets.SetupHotReload("./Instant2D.TestGame/Assets/");
                assets.AddLoader<DevSpriteLoader>();
            });

            AddSystem<InputManager>();
            AddSystem<CoroutineManager>();
            AddSystem<GraphicsManager>();
            AddSystem<SceneManager>(scene => {
                scene.SetResolutionScaler<DefaultResolutionScaler>()
                    .SetDesignResolution(640 / 2, 360 / 2)
                    .SetPixelPerfect()
                    .SetDisplayMode(DefaultResolutionScaler.ScreenDisplayMode.ShowAll);
            });
        }

        class WawaComponent : Component, IUpdatableComponent {
            Vector2 _targetPos;

            public void Update() {
                if (Instance.IsActive && InputManager.LeftMouseDown) {
                    Entity.Transform.Position = Scene.Camera.ScreenToWorldPosition(InputManager.MousePosition);
                }
                
                if (InputManager.RightMousePressed) {
                    Entity.Schedule(0.5f, _ => {
                        _targetPos = Scene.Camera.ScreenToWorldPosition(InputManager.MousePosition);
                    }).SetOverrideTimeScale(1.0f);
                }

                if (InputManager.MiddleMouseDown) {
                    Scene.TimeScale = 0.01f;
                }

                if (InputManager.IsKeyPressed(Keys.E)) {
                    Scene.FindComponentOfType<TextComponent>().Content = Random.Shared.Choose("Wawa!!!", "You suck.", "RJOIEJHTIOWjETEWIOJFIOWE EJRT WETJOWIEJ TO");

                    for (var i = 0; i < 6; i++) {
                        Entity.Schedule(Random.Shared.NextFloat(0.1f, 1f), _ => {
                            Scene.CreateEntity("explosion", Transform.Position + Random.Shared.NextDirection(Random.Shared.NextFloat(8, 32)))
                                .AddComponent<SpriteAnimationComponent>()
                                .Play(AssetManager.Instance.Get<SpriteAnimation>("sprites/explosion"))
                                .SetSpeed(Random.Shared.NextFloat(0.5f, 1.5f))
                                .SetCompletionHandler(animator => animator.Entity.Destroy())
                                .SetRenderLayer("objects")
                                .SetZ(1000);
                        });
                    }
                }

                if (Scene.TimeScale < 1f) {
                    Scene.TimeScale *= 1.1f;
                    if (Scene.TimeScale > 1f)
                        Scene.TimeScale = 1f;
                }

                // move the camera to the focus zone
                if (Vector2.Distance(Scene.Camera.Entity.Transform.Position, _targetPos) > 5) {
                    Scene.Camera.Entity.Transform.Position = Vector2.Lerp(Scene.Camera.Entity.Transform.Position, _targetPos, 0.1f * Scene.TimeScale);
                }
                
                if (InputManager.MouseWheelDelta != 0) {
                    Scene.Camera.Zoom += InputManager.MouseWheelDelta * 0.0005f;
                }

                Entity.Transform.Rotation += 0.1f * Scene.TimeScale;

                // rotate wawas
                for (var i = 0; i < Entity.ChildrenCount; i++) {
                    var entity = Entity[i];
                    entity.Transform.LocalRotation += (i % 2 == 0 ? -0.1f : 0.1f) * Scene.TimeScale;
                    entity.Transform.LocalPosition = entity.Transform.LocalPosition.SafeNormalize() * (1000 - 750 * MathF.Sin((Scene.TotalTime + i) * 4));
                }
            }
        }

        class FireComponent : RenderableComponent {
            public override void Initialize() {
                Material = Material.Default with { BlendState = BlendState.Additive };
            }

            public override void Draw(IDrawingBackend drawing, CameraComponent camera) {
                var anim = AssetManager.Instance.Get<SpriteAnimation>("sprites/fire");
                drawing.DrawAnimation(anim, Transform.Position,
                    Color, Transform.Rotation, Transform.Scale);
            }
        }

        class HitboxComponent : Component {
            internal static readonly List<HitboxComponent> _hitboxBuffer = new();

            public bool TryMove(Vector2 velocity, out HitboxComponent hit) {
                var futureHitbox = Hitbox with { Position = Hitbox.Position + velocity };

                hit = default;
                for (var i = 0; i < _hitboxBuffer.Count; i++) {
                    var other = _hitboxBuffer[i];

                    if (other == this || !other.IsActive)
                        continue;

                    if (futureHitbox.Intersects(other.Hitbox)) {
                        hit = other;
                        break;
                    }
                }

                return hit != null;
            }

            Vector2 _size = new(32);

            public RectangleF Hitbox { get; private set; }

            public Vector2 Size {
                get => _size;
                set {
                    _size = value;
                    UpdateHitbox();
                }
            }

            public delegate void CollisionEventHandler(HitboxComponent a, HitboxComponent b);

            public event CollisionEventHandler OnCollision;

            void UpdateHitbox() {
                Hitbox = new RectangleF(Entity.Transform.Position - _size * 0.5f, _size);
                for (var i = 0; i < _hitboxBuffer.Count; i++) {
                    var other = _hitboxBuffer[i];

                    if (other == this || !other.IsActive)
                        continue;

                    if (Hitbox.Intersects(other.Hitbox)) {
                        OnCollision?.Invoke(this, other);
                    }
                }
            }

            public override void Initialize() {
                _hitboxBuffer.Add(this);
                UpdateHitbox();
            }

            public override void OnRemovedFromEntity() {
                _hitboxBuffer.Remove(this);
            }

            public override void OnTransformUpdated(TransformComponentType components) {
                if (components.HasFlag(TransformComponentType.Position) || components.HasFlag(TransformComponentType.Scale))
                    UpdateHitbox();
            }
        }

        class WawaScene : Scene {
            public override void Initialize() {
                var scene = this;

                // setup layers
                var bg = scene.CreateLayer("background");
                bg.BackgroundColor = Color.DarkRed;

                var objects = scene.CreateLayer("objects");

                // create scaling test
                scene.CreateEntity("scaling-test", Vector2.Zero)
                    .AddComponent<SpriteComponent>()
                    .SetSprite(AssetManager.Instance.Get<Sprite>("sprites/scaling_test"))
                    .SetRenderLayer("background")
                    .SetDepth(1.0f)
                    .Entity.Transform.Rotation = 0.3f;

                scene.CreateEntity("gardening-test", new(50))
                    .AddComponent(new SpriteComponent {
                        Sprite = AssetManager.Instance.Get<Sprite>("sprites/gardening_vase"),
                        RenderLayer = bg
                    })
                    .Entity.AddComponent<HitboxComponent>();

                // create burning hell
                for (var i = 0; i < 0; i++) {
                    var entity = scene.CreateEntity($"fire_{i}", new Vector2(Random.Shared.Next(640), Random.Shared.Next(320)));
                    entity.Transform.Scale = new Vector2(0.5f + Random.Shared.NextSingle() * 5);
                    entity.AddComponent<SpriteAnimationComponent>()
                        .Play(AssetManager.Instance.Get<SpriteAnimation>("sprites/fire"), LoopType.Loop)
                        .SetSpeed(0.1f + Random.Shared.NextSingle() * 2)
                        .SetMaterial(Material.Default with { BlendState = BlendState.Additive })
                        .SetRenderLayer(objects)
                        .SetZ(Random.Shared.Next(-200, 200));
                }

                // text
                var text = CreateEntity("text-test", new Vector2(-10, 10))
                    .AddComponent<TextComponent>();

                text.Content = "Wawa!!!";

                // create funny renderer
                var wawaCat = scene.CreateEntity("wawa-cat", Vector2.Zero)
                    .SetLocalScale(0.25f);

                wawaCat.AddComponent<HitboxComponent>().OnCollision += (a, b) => {
                    var dir = a.Transform.Position.DirectionTo(b.Transform.Position);
                    var dist = Vector2.Distance(a.Transform.Position, b.Transform.Position);
                    a.Transform.Position -= dir * dist * 0.5f;
                    b.Transform.Position += dir * dist * 0.5f;

                    Logger.WriteLine($"{a.Entity.Name} collided with {b.Entity.Name}.");
                };

                wawaCat.AddComponent<SpriteComponent>()
                    .SetSprite(AssetManager.Instance.Get<Sprite>("sprites/wawa"))
                    .SetRenderLayer("objects");
                
                wawaCat.AddComponent<WawaComponent>();

                // create Mini Wawas
                var wawas = 12;
                for (var i = 0; i < wawas; i++) {
                var wawa = scene.CreateEntity($"mini-wawa-{i}", Vector2.Zero)
                        .SetParent(wawaCat)
                        .SetLocalPosition(new Vector2(250, 0).RotatedBy(i * (MathHelper.TwoPi / wawas)))
                        .SetLocalScale(0.25f)
                        .AddComponent(new SpriteComponent {
                            Sprite = AssetManager.Instance.Get<Sprite>("sprites/wawa"),
                            RenderLayer = objects,
                            Depth = 0.5f
                        });
                }
            }

            public override void Render(IDrawingBackend drawing) {
                var matrix = Camera.TransformMatrix;

                matrix = Matrix2D.Multiply(matrix, Resolution.scaleFactor);

                drawing.Push(Material.Default, matrix);

                foreach (var hitbox in HitboxComponent._hitboxBuffer) {
                    drawing.DrawRectangle(hitbox.Hitbox, Color.Blue * 0.5f, Color.Black);
                }

                drawing.Pop();
            }
        }

        protected override void Initialize() {
            Window.AllowUserResizing = true;

            Title = "WawaGame";

            SceneManager.Instance.Current = new WawaScene();
        }

        protected override void Draw(GameTime gameTime) {
            base.Draw(gameTime);

        }
    }
}
