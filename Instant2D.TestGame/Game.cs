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
                    .SetDesignResolution(640, 360)
                    .SetPixelPerfect()
                    .SetDisplayMode(DefaultResolutionScaler.ScreenDisplayMode.ShowAll);
            });
        }

        class MoveOnTouchComponent : Component, IUpdatableComponent {
            public void Update() {
                var mouse = InputManager.MousePosition;
                Entity.Transform.Position = new Vector2(mouse.X, mouse.Y);
                float deltaX = 0f;
                if (InputManager.LeftMousePressed) {
                    deltaX = mouse.X - Entity.Transform.Position.X;
                    
                }

                //if (mouse.ScrollWheelValue != 0) {
                //    Entity.Transform.Scale += new Vector2(mouse.ScrollWheelValue * 0.0001f);
                //}

                Entity.Transform.Rotation = MathHelper.Lerp(Entity.Transform.Rotation, deltaX * 0.025f, 0.5f);
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

            _soundEffect.Play();

            Window.AllowUserResizing = true;

            SceneManager.Instance.Current = new SimpleScene {
                Camera = new ScreenSpaceCameraComponent(),
                
                OnInitialize = scene => {
                    // setup layers
                    var bg = scene.CreateLayer("background");
                    bg.BackgroundColor = Color.DarkGreen;

                    // create scaling test
                    scene.CreateEntity("scaling-test", Vector2.Zero)
                        .AddComponent<SpriteRenderer>()
                        .SetSprite(AssetManager.Instance.Get<Sprite>("scaling_test"))
                        .SetRenderLayer("background")
                        .SetDepth(1.0f);

                    // create funny renderer
                    var renderer = scene.CreateEntity("sprite-entity", Vector2.Zero)
                        .AddComponent<SpriteRenderer>();

                    renderer.Entity.AddComponent<MoveOnTouchComponent>();
                    renderer.Sprite = AssetManager.Instance.Get<Sprite>("wawa");
                    renderer.RenderLayer = bg;
                },

                OnUpdate = scene => {

                }
            };
        }

        protected override void Draw(GameTime gameTime) {
            base.Draw(gameTime);

            var drawing = GraphicsManager.Backend;
            drawing.Push(Material.Default);

            var scaledRes = SceneManager.Instance.Current.Resolution;
            drawing.Draw(GraphicsManager.Pixel, scaledRes.offset, Color.Red * 0.2f, 0, scaledRes.renderTargetSize.ToVector2());

            drawing.Pop(true);
        }
    }
}
