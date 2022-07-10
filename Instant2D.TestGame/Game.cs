using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Instant2D.Assets.Loaders;
using Instant2D.Core;
using Instant2D.EC;
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
                    for (var i = 0; i < 32; i++) {
                        Entity.Schedule(Random.Shared.NextFloat(0.1f, 1f), _ => {
                            var animator = Scene.CreateEntity("explosion", Transform.Position + Random.Shared.NextDirection(Random.Shared.NextFloat(8, 32)))
                                .AddComponent<SpriteAnimationComponent>()
                                .Play(AssetManager.Instance.Get<SpriteAnimation>("sprites/explosion"))
                                .SetSpeed(Random.Shared.NextFloat(0.5f, 1.5f));

                            animator.SetZ(1000);
                            animator.SetRenderLayer("objects");
                            animator.OnAnimationComplete += anim => anim.Entity.Destroy();
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

        protected override void Initialize() {
            Window.AllowUserResizing = true;

            Title = "WawaGame";

            SceneManager.Instance.Current = new SimpleScene {
                OnInitialize = scene => {
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
                        });

                    // create burning hell
                    for (var i = 0; i < 24; i++) {
                        var entity = scene.CreateEntity($"fire_{i}", new Vector2(Random.Shared.Next(640), Random.Shared.Next(320)));
                        entity.Transform.Scale = new Vector2(0.5f + Random.Shared.NextSingle() * 5);
                        entity.AddComponent<SpriteAnimationComponent>()
                            .Play(AssetManager.Instance.Get<SpriteAnimation>("sprites/fire"), LoopType.Loop)
                            .SetSpeed(0.1f + Random.Shared.NextSingle() * 2)
                            .SetMaterial(Material.Default with { BlendState = BlendState.Additive })
                            .SetRenderLayer(objects)
                            .SetZ(Random.Shared.Next(-200, 200));
                    }

                    // create funny renderer
                    var wawaCat = scene.CreateEntity("wawa-cat", Vector2.Zero)
                        .SetLocalScale(0.25f);

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
                },

                OnUpdate = scene => {
                    scene.Camera.Transform.Rotation = 0;
                    if (InputManager.IsKeyDown(Keys.D))
                        scene.Camera.Entity.Transform.Position += new Vector2(2, 0);

                    if (InputManager.IsKeyDown(Keys.A))
                        scene.Camera.Entity.Transform.Position += new Vector2(-2, 0);

                    if (InputManager.IsKeyDown(Keys.W))
                        scene.Camera.Entity.Transform.Position += new Vector2(0, -2);

                    if (InputManager.IsKeyDown(Keys.S))
                        scene.Camera.Entity.Transform.Position += new Vector2(0, 2);
                }
            };
        }

        protected override void Draw(GameTime gameTime) {
            base.Draw(gameTime);
        }
    }
}
