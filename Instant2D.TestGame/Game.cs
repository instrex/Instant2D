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
                assets.AddLoader<DevSpriteLoader>()
                    //.SetSpritesheetMode()
                    ;
            });

            AddSystem<InputManager>();
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
                if (InputManager.LeftMouseDown) {
                    Entity.Transform.Position = Scene.Camera.ScreenToWorldPosition(InputManager.MousePosition);
                }
                
                if (InputManager.RightMousePressed) {
                    _targetPos = Scene.Camera.ScreenToWorldPosition(InputManager.MousePosition);
                }

                if (InputManager.MiddleMouseDown) {
                    Scene.TimeScale = 0.01f;
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
                var anim = AssetManager.Instance.Get<SpriteAnimation>("fire");
                drawing.DrawAnimation(anim, Transform.Position,
                    Color, Transform.Rotation, Transform.Scale);
            }
        }

        SoundEffect _soundEffect;
        protected override void LoadContent() {
            base.LoadContent();
            
            // OGG loading test using FAudio
            unsafe {
                var data = File.ReadAllBytes(@"C:\Users\instrex\Desktop\OneShot.ogg");
                fixed (byte* ptr = data) {
                    var ogg = FAudio.stb_vorbis_open_memory((IntPtr)ptr, data.Length, out var err, IntPtr.Zero);
                    var info = FAudio.stb_vorbis_get_info(ogg);
                    var sampleCount = FAudio.stb_vorbis_stream_length_in_samples(ogg);

                    // allocate sample buffers 
                    var buffer = new byte[sampleCount * 2 * info.channels];
                    var samples = new float[buffer.Length / 2];
                    _ = FAudio.stb_vorbis_get_samples_float_interleaved(ogg, info.channels, samples, samples.Length);

                    // convert float to byte[] PCM data
                    for (var i = 0; i < samples.Length; i++) {
                        var val = (short)(samples[i] * short.MaxValue);
                        buffer[i * 2] = (byte)val;
                        buffer[i * 2 + 1] = (byte)(val >> 8);
                    }

                    // create the sound effect
                    _soundEffect = new SoundEffect(buffer, (int)info.sample_rate, (AudioChannels)info.channels);

                    // kill stream
                    FAudio.stb_vorbis_close(ogg);
                }
            }

            //_soundEffect.Play();

            Window.AllowUserResizing = true;

            SceneManager.Instance.Current = new SimpleScene {
                // Camera = new ScreenSpaceCameraComponent(),
                
                OnInitialize = scene => {
                    // setup layers
                    var bg = scene.CreateLayer("background");
                    bg.BackgroundColor = Color.DarkRed;

                    // create scaling test
                    scene.CreateEntity("scaling-test", Vector2.Zero)
                        .AddComponent<SpriteRenderer>()
                        .SetSprite(AssetManager.Instance.Get<Sprite>("scaling_test"))
                        .SetRenderLayer("background")
                        .SetDepth(1.0f)
                        .Entity.Transform.Rotation = 0.3f;

                    scene.CreateEntity("gardening-test", new(50))
                        .AddComponent(new SpriteRenderer {
                            Sprite = AssetManager.Instance.Get<Sprite>("gardening_vase"),
                            RenderLayer = bg
                        });

                    // create burning hell
                    for (var i = 0; i < 24; i++) {
                        var entity = scene.CreateEntity($"fire_{i}", new Vector2(Random.Shared.Next(640), Random.Shared.Next(320)));
                        entity.Transform.Scale = new Vector2(0.5f + Random.Shared.NextSingle() * 5);
                        entity.AddComponent<FireComponent>()
                            .SetRenderLayer(bg)
                            .SetZ(Random.Shared.Next(-200, 200));
                    }

                    // create funny renderer
                    var wawaCat = scene.CreateEntity("wawa-cat", Vector2.Zero)
                        .SetLocalScale(0.25f);

                    wawaCat.AddComponent<SpriteRenderer>()
                        .SetSprite(AssetManager.Instance.Get<Sprite>("wawa"))
                        .SetRenderLayer("background");

                    wawaCat.AddComponent<WawaComponent>();

                    // create Mini Wawas
                    var wawas = 12;
                    for (var i = 0; i < wawas; i++) {
                        var wawa = scene.CreateEntity($"mini-wawa-{i}", Vector2.Zero)
                            .SetParent(wawaCat)
                            .SetLocalPosition(new Vector2(250, 0).RotatedBy(i * (MathHelper.TwoPi / wawas)))
                            .SetLocalScale(0.25f)
                            .AddComponent(new SpriteRenderer {
                                Sprite = AssetManager.Instance.Get<Sprite>("wawa"),
                                RenderLayer = bg,
                                Depth = 0.5f
                            });
                    }
                },

                OnUpdate = scene => {
                    scene.Camera.Transform.Rotation = MathF.Sin(scene.TotalTime * 12) * 0.1f;
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

            var drawing = GraphicsManager.Backend;
            drawing.Push(Material.Default);

            var str = @"0123456789#&$%~_|!?.,:;'""^`+-=*()/\[]<>@{} ABC
ABCDEFGHIJKLMNOPQRSTUVWXYZ abcdefghijklmnopqrstuvwxyz
АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯ абвгдеёжзийклмнопрстуфхцчшщъыьэюя
".ReplaceLineEndings();

            drawing.Draw(GraphicsManager.Pixel, new(50), Color.Blue, MathF.Sin(TimeManager.TotalTime * 1) * 3.14f, new(50, 4));

            var measure = GraphicsManager.DefaultFont.MeasureString(str);
            drawing.Draw(GraphicsManager.Pixel, new Vector2(500), Color.Blue, 0, measure);
            GraphicsManager.DefaultFont.DrawString(drawing, str, new(500), Color.White, Vector2.One * 1, 0);

            drawing.Pop(true);
        }
    }
}
